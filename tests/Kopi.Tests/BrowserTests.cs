using Kopi.Core;
using Microsoft.Playwright;
using Xunit;
namespace Kopi.Tests;
public sealed class BrowserTests {
    [Fact] public async Task SnapshotReadsReadOnlyMessageAndExcludesHiddenAndDraftText() {
        using var pw=await Playwright.CreateAsync();
        await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});
        var page=await browser.NewPageAsync();
        await page.SetContentAsync("<main><div contenteditable='false'>Hi, I am Alina Reid. Contact alina@cedar.example.</div><div style='display:none'>HIDDEN_PRIVATE_TEXT</div><div contenteditable='true'>UNSENT_DRAFT</div><input type='password' value='PASSWORD_SECRET'></main>");
        foreach(var file in new[]{"privacy.js","browser.js"})await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension",file)});
        var text=await page.EvaluateAsync<string>("()=>KopiBrowser.snapshot().text");
        Assert.Contains("Alina Reid",text);Assert.Contains("alina@cedar.example",text);
        Assert.DoesNotContain("HIDDEN_PRIVATE_TEXT",text);Assert.DoesNotContain("UNSENT_DRAFT",text);Assert.DoesNotContain("PASSWORD_SECRET",text);
    }
    static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
    [Fact] public async Task SharedPrimitivesFillAndVerifyRealForm() {
        using var pw=await Playwright.CreateAsync(); await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});var page=await browser.NewPageAsync();
        await page.SetContentAsync("<label for='full'>Full name</label><input id='full'><button id='save' onclick=\"document.querySelector('#result').hidden=false\">Save</button><p id='result' hidden>Saved</p>");
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/privacy.js")});
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/browser.js")});
        await page.EvaluateAsync("p=>KopiBrowser.action(JSON.parse(p))",Json.Write(new{step=new Step("fill",Selector:"#full",Value:"Unseen Person")}));
        await page.EvaluateAsync("p=>KopiBrowser.action(JSON.parse(p))",Json.Write(new{step=new Step("click",Selector:"#save",VerifySelector:"#result")}));
        Assert.Equal("Unseen Person",await page.InputValueAsync("#full"));Assert.True(await page.IsVisibleAsync("#result"));
        await Assert.ThrowsAsync<PlaywrightException>(()=>page.EvaluateAsync("p=>KopiBrowser.action(JSON.parse(p))",Json.Write(new{step=new Step("fill",Selector:"#missing",Value:"x")})));
    }
    [Fact] public async Task RecorderCapturesUserActionsButNotPasswords() {
        using var pw=await Playwright.CreateAsync();await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});var page=await browser.NewPageAsync();
        await page.RouteAsync("http://example.test/**", route=>route.FulfillAsync(new(){Body="<html></html>",ContentType="text/html"}));
        await page.GotoAsync("http://example.test");
        await page.SetContentAsync("<label for='name'>Name</label><input id='name'><input id='secret' type='password'><div id='editor' contenteditable='true'></div>");
        await page.EvaluateAsync("()=>{window.recorded=[];window.chrome={runtime:{onMessage:{addListener:f=>window.receive=f},sendMessage:m=>{recorded.push(m.event);return Promise.resolve();}}};}");
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/privacy.js")});
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/capture.js")});await page.EvaluateAsync("()=>receive({state:{observing:true,suggestions:false}})");
        await page.FillAsync("#name","New Person");await page.Locator("#name").BlurAsync();await page.FillAsync("#secret","NEVER_CAPTURE");await page.Locator("#secret").BlurAsync();
        await page.Locator("#editor").FillAsync("Edited cell content");await page.WaitForTimeoutAsync(350);
        var events=await page.EvaluateAsync<string>("()=>JSON.stringify(recorded)");Assert.Contains("Edited cell content",events);Assert.Contains("New Person",events);Assert.DoesNotContain("NEVER_CAPTURE",events);
    }
    [Fact] public async Task AuthenticationEvidencePreservesRoleButNotCode() {
        using var pw=await Playwright.CreateAsync();await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});var page=await browser.NewPageAsync();
        await page.SetContentAsync("<title>Verify identity</title><main>Microsoft security code: 739281</main><input id='otp' autocomplete='one-time-code' aria-label='Verification code'>");
        await page.EvaluateAsync("()=>{window.recorded=[];window.chrome={runtime:{onMessage:{addListener:f=>window.receive=f},sendMessage:m=>{recorded.push(m.event);return Promise.resolve();}}};}");
        foreach(var file in new[]{"privacy.js","capture.js","browser.js"})await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension",file)});
        await page.EvaluateAsync("()=>receive({state:{observing:true,suggestions:false}})");
        await page.FillAsync("#otp","739281");await page.Locator("#otp").BlurAsync();
        var events=await page.EvaluateAsync<string>("()=>JSON.stringify(recorded)");Assert.DoesNotContain("739281",events);Assert.Contains("one-time-code",events);Assert.Contains("#otp",events);
        var snapshot=await page.EvaluateAsync<string>("()=>JSON.stringify(KopiBrowser.snapshot())");Assert.DoesNotContain("739281",snapshot);Assert.Contains("security code",snapshot);
    }
}
