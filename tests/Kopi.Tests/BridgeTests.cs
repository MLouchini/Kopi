using Kopi.Core;
using Microsoft.Playwright;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization=true)]
namespace Kopi.Tests;
public sealed class BridgeTests {
    [Fact] public async Task RealExtensionPairsRecordsAndExecutesInSameBrowser() {
        using var bridge=new BrowserBridge();bridge.Start();
        using var pw=await Playwright.CreateAsync();
        var extension=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../extension"));
        var profile=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        await using var context=await pw.Chromium.LaunchPersistentContextAsync(profile,new(){Headless=true,Channel="chromium",ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM"),IgnoreDefaultArgs=["--disable-extensions"],Args=["--enable-unsafe-extension-debugging",$"--disable-extensions-except={extension}",$"--load-extension={extension}"]});
        var cdp=await context.NewCDPSessionAsync(context.Pages[0]);
        string? extensionUrl=null;
        for(var i=0;i<30&&extensionUrl==null;i++) {
            var targets=await cdp.SendAsync("Target.getTargets");
            extensionUrl=targets!.Value.GetProperty("targetInfos").EnumerateArray().Select(t=>t.GetProperty("url").GetString()).FirstOrDefault(u=>u?.StartsWith("chrome-extension://")==true);
            if(extensionUrl==null)await Task.Delay(200);
        }
        Assert.NotNull(extensionUrl);
        var popup=await context.NewPageAsync();await popup.GotoAsync(new Uri(new Uri(extensionUrl),"popup.html").ToString());
        await popup.FillAsync("#code",bridge.PairCode);await popup.ClickAsync("#pair");
        await popup.WaitForFunctionAsync("()=>document.querySelector('#status').textContent.includes('Connected')");await popup.CloseAsync();
        await context.RouteAsync("http://kopi.test/**",r=>r.FulfillAsync(new(){ContentType="text/html",Body="<title>Test profile</title><main><h1>Profile</h1><label for='name'>Name</label><input id='name'><button id='save' onclick=\"document.querySelector('#success').hidden=false\">Save</button><p id='success' hidden>Saved</p></main>"}));
        var page=await context.NewPageAsync();await page.GotoAsync("http://kopi.test/profile");
        bridge.Observing=true;var recorded=new List<Evidence>();bridge.Received+=e=>recorded.Add(e);
        await page.WaitForTimeoutAsync(2200);await page.FillAsync("#name","Someone New");await page.Locator("#name").BlurAsync();await page.WaitForTimeoutAsync(500);
        Assert.Contains(recorded,e=>e.Value=="Someone New");bridge.Observing=false;
        var snapshot=Json.Read<Evidence>(await bridge.Send("snapshot",new{},default));Assert.Equal("Test profile",snapshot.Title);
        await bridge.Send("step",new{step=new Step("fill",Selector:"#name",Value:"Different Person")},default);
        await bridge.Send("step",new{step=new Step("click",Selector:"#save",VerifySelector:"#success")},default);
        Assert.Equal("Different Person",await page.InputValueAsync("#name"));Assert.True(await page.IsVisibleAsync("#success"));
        const string sheetUrl="https://docs.google.com/spreadsheets/d/simulated-tracker/edit";
        await context.GrantPermissionsAsync(["clipboard-read","clipboard-write"],new(){Origin="https://docs.google.com"});
        List<string[]> sheetRows=[["Name","Location"]];
        await context.RouteAsync("https://docs.google.com/**",async route=>{
            if(route.Request.Url.EndsWith("/update")){sheetRows=Json.Read<List<string[]>>(route.Request.PostData!);await route.FulfillAsync(new(){Body="ok"});return;}
            if(route.Request.Url.Contains("/gviz/tq?")) {
                var csv=string.Join("\n",sheetRows.Select(r=>string.Join(",",r)));
                await route.FulfillAsync(new(){ContentType="text/csv",Body=csv});
            }else await route.FulfillAsync(new(){ContentType="text/html",Body="<title>Simulated tracker</title><input class='waffle-name-box'><textarea id='cells'></textarea><script>window.rows=[['Name','Location']];document.querySelector('input').addEventListener('keydown',e=>{if(e.key==='Enter')document.querySelector('textarea').focus()});document.querySelector('textarea').addEventListener('paste',e=>{e.preventDefault();rows.push(e.clipboardData.getData('text/plain').split('\\t'));fetch('/update',{method:'POST',body:JSON.stringify(rows)});});</script>"});
        });
        var target=await context.NewPageAsync();await target.GotoAsync(sheetUrl);
        await bridge.Send("step",new{step=new Step("appendRow",sheetUrl),columns=new[]{"name","location"},sheetName="Tracker",values=new Dictionary<string,string>{{"name","Unseen Company"},{"location","Bristol"}}},default);
        Assert.Equal("Unseen Company",await target.EvaluateAsync<string>("()=>rows[1][0]"));
        Assert.Equal("Bristol",await target.EvaluateAsync<string>("()=>rows[1][1]"));
        await context.CloseAsync();Directory.Delete(profile,true);
    }
}
