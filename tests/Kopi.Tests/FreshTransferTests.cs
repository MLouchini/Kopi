using Kopi.Core;
using Microsoft.Playwright;
using Xunit;
namespace Kopi.Tests;
public sealed class FreshTransferTests {
    static readonly FreshTransfer Rule=new("https://messages.test/inbox",".message","#sender","University Identity Service","#organisation","Example University","#resend","#submit","data-time","iso");
    static string Root=>Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
    [Fact]public void RuleCannotEnableUnscopedOrPersistedCodeAutomation(){
        var w=new Workflow{Name="Verify login",Goal="Transfer new code",Trigger="Verification challenge",Steps=[new("transferFresh","https://portal.test/verify","#otp",VerifySelector:"#success",Transfer:Rule)],Approved=true,AutoRun=true};
        w.Validate();w.Steps[0]=w.Steps[0] with{Value="123456"};Assert.Throws<InvalidDataException>(w.Validate);
        w.Steps[0]=w.Steps[0] with{Value="",Transfer=Rule with{SourceIdentity=""}};Assert.Throws<InvalidDataException>(w.Validate);
    }
    [Fact]public async Task ScopedBrowserCandidatesRejectWrongSenderAndMissingTimestamp(){
        using var pw=await Playwright.CreateAsync();await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});var page=await browser.NewPageAsync();
        await page.SetContentAsync("<main><h1 id='sender'>University Identity Service</h1><p class='message' data-id='old' data-time='2026-10-24T12:00:00Z'>Your code is 123456</p><p class='message' data-id='ambiguous'>Use 111111 or 222222</p></main><aside><p class='message'>Other sender: 999999</p></aside>");
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/fresh.js")});
        var candidates=Json.Read<List<Candidate>>(await page.EvaluateAsync<string>("p=>JSON.stringify(KopiFresh.candidates(JSON.parse(p)))",Json.Write(new{rule=Rule})));
        Assert.Equal(2,candidates.Count);Assert.Equal("123456",candidates[0].Value);Assert.True(candidates[0].Time>0);Assert.True(candidates[1].Ambiguous);Assert.Null(candidates[1].Time);
        await page.Locator("#sender").EvaluateAsync("e=>e.textContent='Someone Else'");
        await Assert.ThrowsAsync<PlaywrightException>(()=>page.EvaluateAsync("p=>KopiFresh.candidates(JSON.parse(p))",Json.Write(new{rule=Rule})));
    }
    sealed record Candidate(string Key,double? Time,string? Value,bool Ambiguous);
}
