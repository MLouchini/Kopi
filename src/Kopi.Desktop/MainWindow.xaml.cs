using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Kopi.Core;
using Microsoft.Win32;

namespace Kopi.Desktop;
public sealed class RunValue { public string Name {get;set;}=""; public string Value {get;set;}=""; public bool Required {get;set;} }
public partial class MainWindow : Window {
    readonly LocalStore store = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kopi"));
    readonly BrowserBridge bridge = new();
    readonly LlamaModel model = new();
    readonly ModelRuntime runtime;
    readonly Intelligence ai;
    readonly List<Evidence> recording = new();
    List<Workflow> workflows = new();
    Workflow? draft, previewWorkflow;
    CancellationTokenSource? work;
    bool busy;
    readonly System.Windows.Forms.NotifyIcon tray = new() { Icon = System.Drawing.SystemIcons.Application, Visible = true, Text = "Kopi · local workflow assistant" };
    readonly HashSet<string> suggested = new();
    public MainWindow() {
        InitializeComponent();
        var area=SystemParameters.WorkArea;
        MinWidth=Math.Min(850,Math.Max(300,area.Width-16));MinHeight=Math.Min(650,Math.Max(300,area.Height-16));
        Width=Math.Min(1120,area.Width-16);Height=Math.Min(860,area.Height-16);Left=area.Left+8;Top=area.Top+8;
        runtime = new(store); model.Authenticate(runtime.ApiKey); ai = new(model);
        try { workflows = store.Load(); bridge.Start(); } catch (Exception e) { StatusText.Text = e.Message; }
        PairCodeBox.Text = bridge.PairCode; Refresh();
        if(!File.Exists(store.PathFor("model.gguf")))Tabs.SelectedIndex=3;
        bridge.Received += e => Dispatcher.InvokeAsync(() => OnEvidence(e));
        tray.DoubleClick += (_,_) => Dispatcher.Invoke(() => { Show(); WindowState = WindowState.Normal; Activate(); });
        var menu = new System.Windows.Forms.ContextMenuStrip(); menu.Items.Add("Open Kopi", null, (_,_)=>Dispatcher.Invoke(()=>{Show();WindowState=WindowState.Normal;Activate();}));
        menu.Items.Add("Quit", null, (_,_) => Dispatcher.Invoke(Close)); tray.ContextMenuStrip=menu;
        Closed += (_,_) => { work?.Cancel(); bridge.Dispose(); runtime.Dispose(); model.Dispose(); tray.Dispose(); };
    }
    void Refresh() { WorkflowList.ItemsSource = null; WorkflowList.ItemsSource = workflows; }
    async Task Guard(Func<CancellationToken,Task> action) {
        if (busy) return; busy = true; work = new(); CancelButton.Visibility = Visibility.Visible; Tabs.IsEnabled = false;
        try { await action(work.Token); }
        catch (OperationCanceledException) { StatusText.Text = "Stopped waiting. An action already sent to the browser may finish; inspect the destination before retrying."; }
        catch (Exception ex) { StatusText.Text = ex.Message; MessageBox.Show(this, ex.Message, "Kopi needs your attention", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally { busy = false; work.Dispose(); work = null; CancelButton.Visibility = Visibility.Collapsed; Tabs.IsEnabled = true; }
    }
    IProgress<string> Progress() => new Progress<string>(s => StatusText.Text = s);
    async Task EnsureModel(CancellationToken ct) => await runtime.Start(Progress(),ct);
    void StartObserving(object sender, RoutedEventArgs e) {
        if (DateTime.UtcNow - bridge.LastSeen > TimeSpan.FromSeconds(10)) { Tabs.SelectedIndex = 3; StatusText.Text = "Pair your browser extension first."; return; }
        recording.Clear(); bridge.Observing = true; RecordButton.IsEnabled = false; StopButton.IsEnabled = true;
        StatusText.Text = "Observing explicitly · perform one task, then return and stop.";
    }
    async void StopObserving(object sender, RoutedEventArgs e) {
        bridge.Observing = false; RecordButton.IsEnabled = true; StopButton.IsEnabled = false;
        var trace = recording.ToArray();
        await Guard(async ct => { await EnsureModel(ct); StatusText.Text="Understanding your demonstration locally…"; draft = await ai.Discover(trace,ct); ShowDraft(draft); StatusText.Text = "Discovery complete. Review the proposed workflow."; recording.Clear(); CaptureCount.Text="Recording cleared from memory."; });
    }
    void ShowDraft(Workflow w) {
        draft = w; NameBox.Text = w.Name; GoalBox.Text = w.Goal; WorkflowJson.Text = Json.Write(w);
        ReviewSummary.Text = "Review what Kopi inferred. Correct uncertain details before approving.";
        QuestionsText.Text = w.Questions.Length > 0 ? "Needs clarification: " + string.Join(" · ",w.Questions) : "No open clarification questions. Check the destination and success conditions.";
        StepsText.Text = string.Join("\n",w.Steps.Select((s,i)=>$"{i+1}. {s.Kind}  {s.Url} {s.Selector}")) + "\n\nInputs:\n" + string.Join("\n",w.Variables.Select(v=>$"{v.Name}: {v.Description}{(v.Required ? " (required)" : " (optional)")}"));
        Tabs.SelectedIndex = 1;
    }
    async void ClarifyWorkflow(object sender,RoutedEventArgs e) => await Guard(async ct=>{
        if(draft==null||string.IsNullOrWhiteSpace(ClarificationBox.Text))throw new InvalidOperationException("Enter your correction or answer first.");
        await EnsureModel(ct); StatusText.Text="Updating your workflow locally…"; var current=Json.Read<Workflow>(WorkflowJson.Text);current.Name=NameBox.Text;current.Goal=GoalBox.Text;current.Id=draft.Id;
        ShowDraft(await ai.Clarify(current,ClarificationBox.Text,ct));ClarificationBox.Clear();StatusText.Text="Updated proposal. Review it before approval.";
    });
    void SaveWorkflow(object sender,RoutedEventArgs e) {
        try {
            var w = Json.Read<Workflow>(WorkflowJson.Text); w.Name = NameBox.Text.Trim(); w.Goal = GoalBox.Text.Trim();
            w.Validate(); w.Approved = true; w.Id = draft?.Id ?? Guid.NewGuid().ToString("N");
            workflows.RemoveAll(x=>x.Id==w.Id); workflows.Add(w); store.Save(workflows); Refresh(); WorkflowList.SelectedItem=w; Tabs.SelectedIndex=2; StatusText.Text="Saved. Open a new source page and preview this workflow.";
        } catch(Exception ex) { StatusText.Text=ex.Message; }
    }
    Workflow Selected() => WorkflowList.SelectedItem as Workflow ?? throw new InvalidOperationException("Choose a saved workflow first.");
    void WorkflowSelected(object sender,SelectionChangedEventArgs e) { previewWorkflow=null; RunButton.IsEnabled=false; ValuesJson.Clear(); ValuesGrid.ItemsSource=null; }
    void EditWorkflow(object sender,RoutedEventArgs e) { try { ShowDraft(Selected()); } catch(Exception ex){StatusText.Text=ex.Message;} }
    void DeleteWorkflow(object sender,RoutedEventArgs e) {
        try { var w=Selected(); if(MessageBox.Show(this,$"Delete '{w.Name}'?", "Delete workflow",MessageBoxButton.YesNo)==MessageBoxResult.Yes){workflows.Remove(w);store.Save(workflows);Refresh();} } catch(Exception ex){StatusText.Text=ex.Message;}
    }
    async void PreviewRun(object sender,RoutedEventArgs e) => await Guard(async ct=>{
        var w=Selected(); RunButton.IsEnabled=false; previewWorkflow=null;
        await EnsureModel(ct); var page=Json.Read<Evidence>(await bridge.Send("snapshot",new{},ct));
        StatusText.Text="Extracting new values locally…"; var values=await ai.Extract(w,page,ct); ValuesJson.Text=Json.Write(values); ValuesGrid.ItemsSource=w.Variables.Select(v=>new RunValue{Name=v.Name,Value=values.GetValueOrDefault(v.Name,""),Required=v.Required}).ToList(); previewWorkflow=w;
        RunDestination.Text="You are approving: "+w.Goal+"\nDestination(s): "+string.Join(", ",w.Steps.Where(s=>s.Url.Length>0).Select(s=>s.Url));
        RunButton.IsEnabled=true; StatusText.Text="Review the extracted values, then approve this run.";
    });
    async void RunWorkflow(object sender,RoutedEventArgs e) => await Guard(async ct=>{
        var w=previewWorkflow??throw new InvalidOperationException("Preview this run first.");
        ValuesGrid.CommitEdit(DataGridEditingUnit.Cell,true);ValuesGrid.CommitEdit(DataGridEditingUnit.Row,true);
        var values=((List<RunValue>)ValuesGrid.ItemsSource).ToDictionary(x=>x.Name,x=>x.Value); RunButton.IsEnabled=false; previewWorkflow=null;
        var executor=new Executor(bridge,store);
        RunProgress.Text=await executor.Run(w,values,new Progress<string>(s=>RunProgress.Text=s),ct);
        StatusText.Text=RunProgress.Text;
    });
    void SuggestionChanged(object sender,RoutedEventArgs e) { bridge.Suggestions=SuggestToggle.IsChecked==true; }
    async void OnEvidence(Evidence e) {
        PairCodeBox.Text=bridge.PairCode;
        if(bridge.Observing) { if(recording.Count<200){recording.Add(e);CaptureCount.Text=$"{recording.Count} observations · recording in memory";}else{bridge.Observing=false;RecordButton.IsEnabled=true;StopButton.IsEnabled=true;StatusText.Text="Recording limit reached. Stop and discover, or start a shorter demonstration.";} return; }
        if(!bridge.Suggestions||busy||e.Kind!="page"||workflows.Count==0)return;
        var key=e.Url; if(suggested.Contains(key))return; suggested.Add(key);
        await Guard(async ct=>{
            await EnsureModel(ct);
            foreach(var w in workflows.Where(w=>w.Approved)) if(await ai.Relevant(w,e,ct)) {
                WorkflowList.SelectedItem=w; Tabs.SelectedIndex=2;
                tray.ShowBalloonTip(5000,"Kopi found a shortcut",$"{w.Name} may fit this page. Open Kopi to preview it.",System.Windows.Forms.ToolTipIcon.Info);
                StatusText.Text=$"{w.Name} may fit this page. Preview it to review the new values."; break;
            }
        });
    }
    async void InstallModel(object sender,RoutedEventArgs e) {
        var manifest=Path.Combine(AppContext.BaseDirectory,"model-manifest.json");
        await Guard(async ct=>{await runtime.Download(manifest,Progress(),ct);await runtime.Start(Progress(),ct);});
    }
    async void StartModel(object sender,RoutedEventArgs e) => await Guard(EnsureModel);
    void OpenExtension(object sender,RoutedEventArgs e) => OpenFolder(Path.Combine(AppContext.BaseDirectory,"extension"));
    void OpenData(object sender,RoutedEventArgs e) => OpenFolder(store.Root);
    void OpenFolder(string path) { try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});}catch(Exception ex){StatusText.Text=ex.Message;} }
    void CancelWork(object sender,RoutedEventArgs e) => work?.Cancel();
}
