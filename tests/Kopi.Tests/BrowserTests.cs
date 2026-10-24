using Kopi.Core;
using Microsoft.Playwright;
using Xunit;
namespace Kopi.Tests;
public sealed class BrowserTests {
    static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
    [Fact] public async Task SharedPrimitivesFillAndVerifyRealForm() {
        using var pw=await Playwright.CreateAsync(); await using var browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM")});var page=await browser.NewPageAsync();
        await page.SetContentAsync("<label for='full'>Full name</label><input id='full'><button id='save' onclick=\"document.querySelector('#result').hidden=false\">Save</button><p id='result' hidden>Saved</p>");
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
        await page.SetContentAsync("<label for='name'>Name</label><input id='name'><input id='secret' type='password'>");
        await page.EvaluateAsync("()=>{window.recorded=[];window.chrome={runtime:{onMessage:{addListener:f=>window.receive=f},sendMessage:m=>{recorded.push(m.event);return Promise.resolve();}}};}");
        await page.AddScriptTagAsync(new(){Path=Path.Combine(Root,"extension/capture.js")});await page.EvaluateAsync("()=>receive({state:{observing:true,suggestions:false}})");
        await page.FillAsync("#name","New Person");await page.Locator("#name").BlurAsync();await page.FillAsync("#secret","NEVER_CAPTURE");await page.Locator("#secret").BlurAsync();
        var events=await page.EvaluateAsync<string>("()=>JSON.stringify(recorded)");Assert.Contains("New Person",events);Assert.DoesNotContain("NEVER_CAPTURE",events);
    }
}
