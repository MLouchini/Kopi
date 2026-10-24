using Kopi.Core;
using Xunit;
using System.Diagnostics;
namespace Kopi.Tests;
public sealed class LocalModelFactAttribute : FactAttribute { public LocalModelFactAttribute(){if(Environment.GetEnvironmentVariable("KOPI_REAL_MODEL")!="1")Skip="Opt-in: start the real Gemma llama-server and set KOPI_REAL_MODEL=1.";} }
public sealed class RealModelTests {
    [LocalModelFact] public async Task GemmaGeneralisesTrackerAndExtractsUnseenLayouts() {
        using var model=new LlamaModel(); await VerifyTracker(model,Environment.GetEnvironmentVariable("KOPI_REPORT_DIR")??Path.GetTempPath());
    }
    public static async Task VerifyTracker(ILocalModel model,string reportDir) {
        var ai=new Intelligence(model);var watch=Stopwatch.StartNew();
        Evidence[] trace=[
            new("page","https://careers.example.org/jobs/100","Acme internship","Acme Labs. Software Engineering Intern. London. Salary £25,000. Apply: https://careers.example.org/jobs/100"),
            new("copy","https://careers.example.org/jobs/100","Acme internship",Value:"Acme Labs\tSoftware Engineering Intern\tLondon\t£25,000\thttps://careers.example.org/jobs/100"),
            new("page","https://docs.google.com/spreadsheets/d/test-tracker/edit","Internship tracker","Active spreadsheet tab: Tracker. Columns A:E: Company, Role, Location, Salary (optional), URL."),
            new("paste","https://docs.google.com/spreadsheets/d/test-tracker/edit","Internship tracker",Label:"Tracker!A2:E2",Value:"Acme Labs\tSoftware Engineering Intern\tLondon\t£25,000\thttps://careers.example.org/jobs/100"),
            new("page","https://docs.google.com/spreadsheets/d/test-tracker/edit","Internship tracker","Row 2 saved in Tracker. Company=Acme Labs, Role=Software Engineering Intern, Location=London, Salary=£25,000, URL=https://careers.example.org/jobs/100")];
        var w=await ai.Discover(trace,default);
        Directory.CreateDirectory(reportDir);
        await File.WriteAllTextAsync(Path.Combine(reportDir,"gemma-discovery.json"),Json.Write(w));
        if(w.Questions.Length>0){
            w=await ai.Clarify(w,"Yes. The exact tab name is Tracker. The column order is Company, Role, Location, Salary, URL. Salary may be blank. Each run uses the new listing. Keep the configured destination and append one new row.",default);
            await File.WriteAllTextAsync(Path.Combine(reportDir,"gemma-after-user-clarification.json"),Json.Write(w));
        }
        Assert.Contains(w.Steps,s=>s.Kind=="appendRow");Assert.Equal(5,w.Columns.Length);w.Validate();Assert.DoesNotContain(w.Variables,v=>v.Description.Contains("Acme Labs"));
        foreach(var page in new[]{new Evidence("page","https://jobs.other.test/role/77","Opportunity at Nova","<Overview> Nova Robotics is hiring a Research Intern. Office: Bristol. Application: https://jobs.other.test/role/77. Compensation not disclosed."),new Evidence("page","https://work.third.test/vacancy/42","Careers","Position: Research Intern | Organisation: Nova Robotics | Work location: Bristol | Apply here: https://work.third.test/vacancy/42")}) {
            var values=await ai.Extract(w,page,default);
            Assert.Contains(values.Values,v=>v.Contains("Nova Robotics"));Assert.Contains(values.Values,v=>v.Contains("Research Intern"));Assert.Contains(values.Values,v=>v.Contains("Bristol"));Assert.DoesNotContain(values.Values,v=>v.Contains("Acme")||v.Contains("25,000"));
            await File.WriteAllTextAsync(Path.Combine(reportDir,"gemma-values-"+new Uri(page.Url).Host+".json"),Json.Write(values));
        }
        Assert.True(await ai.Relevant(w,new Evidence("page","https://jobs.new.test/role/9","Hiring","Nova Robotics is hiring a Research Intern in Bristol. Apply at this page."),default));
        Assert.False(await ai.Relevant(w,new Evidence("page","https://news.test/privacy","Privacy policy","This page describes website cookies, retention policies and data protection contacts."),default));
        await File.WriteAllTextAsync(Path.Combine(reportDir,"gemma-timing.txt"),$"Discovery + two unseen-layout extractions + positive/negative trigger recognition: {watch.Elapsed.TotalSeconds:F1} seconds. Cloud CPU, not laptop benchmark.");
    }
}
