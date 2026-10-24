using Kopi.Core;
using Xunit;
namespace Kopi.Tests;
public sealed class EngineTests {
    sealed class ClarificationModel(Workflow revised) : ILocalModel {
        public Task<string> Complete(string instruction,string evidence,CancellationToken ct,System.Text.Json.JsonElement? schema=null)
            =>Task.FromResult(instruction.StartsWith("Identify ONLY")?"{\"questions\":[\"What is the source URL?\"]}":Json.Write(revised));
    }
    [Fact] public async Task ClarificationDoesNotRestoreQuestionsAboutRemovedColumns() {
        var revised=new Workflow{Name="Save contact",Goal="Append name and email",Trigger="A contact email",
            Variables=[new("name","Full name"),new("email","Email address")],Columns=["name","email"],SheetName="Contacts",
            Steps=[new("appendRow","https://docs.google.com/spreadsheets/d/test/edit")]};
        var original=Json.Read<Workflow>(Json.Write(revised));
        original.Variables.Add(new("source_url","Source URL",false));original.Columns=["name","email","source_url"];
        original.Questions=["What is the source URL?"];
        var result=await new Intelligence(new ClarificationModel(revised)).Clarify(original,"Only name and email belong in the sheet; remove the source URL column.",default);
        Assert.Empty(result.Questions);Assert.Equal(2,result.Columns.Length);result.Validate();Assert.False(result.Approved);
    }
    sealed class NonPumpingContext : SynchronizationContext {
        public override void Post(SendOrPostCallback callback,object? state) { }
    }
    [Fact] public async Task BridgeShutdownDoesNotWaitForTheWindowDispatcher() {
        await Task.Run(()=>{
            var bridge=new BrowserBridge();bridge.Start();
            var previous=SynchronizationContext.Current;
            try{SynchronizationContext.SetSynchronizationContext(new NonPumpingContext());bridge.Dispose();}
            finally{SynchronizationContext.SetSynchronizationContext(previous);}
        }).WaitAsync(TimeSpan.FromSeconds(10));
    }
    static Workflow Sample() => new() { Name="Save contact",Goal="Transfer contact details",Trigger="A contact profile",Approved=true,Variables=[new("name","Full name")],Steps=[new("navigate","https://example.org/form"),new("fill",Selector:"#name",Value:"{{name}}"),new("click",Selector:"#save",VerifySelector:"#success")] };
    [Fact] public void PrivacyRemovesSensitiveValuesAndUrlTokens() {
        var e=Privacy.Sanitize(new("fill","https://example.org/path?access_token=SECRET#token","Login",Value:"SECRET",FieldType:"password"));
        Assert.Equal("",e.Value); Assert.Equal("https://example.org/path",e.Url);
    }
    [Fact] public void CondensationRetainsDestinationAndBoundsInput() {
        var recording=Enumerable.Range(0,150).Select(i=>new Evidence("page","https://example.org/"+i,"Page",new string('x',4000)));
        var input=Intelligence.Condense(recording);Assert.True(input.Length<=16000);Assert.Contains("/149",input);
    }
    [Fact] public void RejectUnsupportedOrUnverifiedActions() {
        var w=Sample();w.Steps=[new("executeCode")];Assert.Throws<InvalidDataException>(w.Validate);
        w.Steps=[new("click",Selector:"#submit")];Assert.Throws<InvalidDataException>(w.Validate);
        w.Steps=[new("navigate","file:///etc/passwd")];Assert.Throws<InvalidDataException>(w.Validate);
    }
    [Fact] public void RequiresClarificationBeforeApproval() {
        var w=Sample();w.Questions=["Which spreadsheet?"];Assert.Throws<InvalidDataException>(w.Validate);
    }
    sealed class Browser(bool fail=false) : IBrowserSession {
        public int Writes;
        public Task<string> Send(string kind,object payload,CancellationToken ct) { Writes++; if(fail)throw new IOException("Lost browser connection");return Task.FromResult("{}"); }
    }
    [Theory][InlineData(false)][InlineData(true)] public async Task BlockDuplicateAndUncertainRetries(bool fail) {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());var store=new LocalStore(dir);var browser=new Browser(fail);var executor=new Executor(browser,store);var w=Sample();
        try { if(fail)await Assert.ThrowsAsync<IOException>(()=>executor.Run(w,new(){{"name","New Person"}},new Progress<string>(),default)); else await executor.Run(w,new(){{"name","New Person"}},new Progress<string>(),default);
            var count=browser.Writes;await Assert.ThrowsAsync<InvalidOperationException>(()=>executor.Run(w,new(){{"name","New Person"}},new Progress<string>(),default));Assert.Equal(count,browser.Writes);
        }finally{Directory.Delete(dir,true);}
    }
    sealed class InvalidModel : ILocalModel { public Task<string> Complete(string instruction,string evidence,CancellationToken ct,System.Text.Json.JsonElement? schema=null)=>Task.FromResult("not JSON"); }
    [Fact] public async Task InvalidModelOutputFailsClosed()=>await Assert.ThrowsAsync<System.Text.Json.JsonException>(()=>new Intelligence(new InvalidModel()).Discover([new("page","https://example.org","Test")],default));
    [Fact] public async Task UnapprovedWorkflowDoesNotWrite() {
        var b=new Browser();var w=Sample();w.Approved=false;var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        try{await Assert.ThrowsAsync<InvalidOperationException>(()=>new Executor(b,new LocalStore(dir)).Run(w,new(){{"name","X"}},new Progress<string>(),default));Assert.Equal(0,b.Writes);}finally{Directory.Delete(dir,true);}
    }
}
