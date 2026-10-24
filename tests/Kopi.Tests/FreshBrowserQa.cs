using Kopi.Core;
using Microsoft.Playwright;
using System.Security.Cryptography;
using Xunit;
namespace Kopi.Tests;
public static class FreshBrowserQa {
    public static async Task Run(Workflow workflow,string mode="success") {
        using var bridge=new BrowserBridge();bridge.Start();using var pw=await Playwright.CreateAsync();
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));var extension=Path.Combine(root,"extension");
        var profile=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());var receipts=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        await using var context=await pw.Chromium.LaunchPersistentContextAsync(profile,new(){Headless=true,Channel="chromium",ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM"),IgnoreDefaultArgs=["--disable-extensions"],Args=["--enable-unsafe-extension-debugging",$"--disable-extensions-except={extension}",$"--load-extension={extension}"]});
        var cdp=await context.NewCDPSessionAsync(context.Pages[0]);string? extensionUrl=null;
        for(int i=0;i<40&&extensionUrl==null;i++){var result=await cdp.SendAsync("Target.getTargets");extensionUrl=result!.Value.GetProperty("targetInfos").EnumerateArray().Select(t=>t.GetProperty("url").GetString()).FirstOrDefault(u=>u?.StartsWith("chrome-extension://")==true);if(extensionUrl==null)await Task.Delay(200);}
        Assert.NotNull(extensionUrl);var popup=await context.NewPageAsync();await popup.GotoAsync(new Uri(new Uri(extensionUrl),"popup.html").ToString());await popup.FillAsync("#code",bridge.PairCode);await popup.ClickAsync("#pair");await popup.WaitForFunctionAsync("()=>document.querySelector('#status').textContent.includes('Connected')");await popup.CloseAsync();
        workflow.Id="fresh-verification-test";
        var code=RandomNumberGenerator.GetInt32(100000,1000000).ToString();int requests=0,submits=0;
        var messages=new System.Collections.Concurrent.ConcurrentQueue<object>(new object[]{new{id="old-message",time=DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O"),text="Use this one-time code: 111111"}});
        await context.RouteAsync("https://messages.test/**",async r=>{
            if(r.Request.Url.EndsWith("/poll")){await r.FulfillAsync(new(){ContentType="application/json",Body=Json.Write(messages)});return;}
            var sender=mode=="wrongSender"?"Another Sender":"University Identity Service";
            await r.FulfillAsync(new(){ContentType="text/html",Body="<title>Messages</title><main><h1 id='sender'>"+sender+"</h1><section id='thread'></section></main><script>setInterval(async()=>{const rows=await(await fetch('/poll')).json();for(const r of rows){if(document.getElementById(r.id))continue;const e=document.createElement('p');e.className='message';e.id=r.id;e.dataset.time=r.time;e.textContent=r.text;document.querySelector('#thread').append(e)}},150)</script>"});
        });
        await context.RouteAsync("https://portal.test/**",async r=>{
            if(r.Request.Url.EndsWith("/request")){
                requests++;
                if(mode=="cancel"){_ = Task.Run(async()=>{await Task.Delay(2500);messages.Enqueue(new{id="late-message",time=DateTimeOffset.UtcNow.ToString("O"),text="Use this one-time code: "+code});});}
                else if(mode!="timeout"){
                    var time=(mode=="stale"?DateTimeOffset.UtcNow.AddMinutes(-10):DateTimeOffset.UtcNow).ToString("O");
                    messages.Enqueue(new{id="new-message",time,text="Use this one-time code: "+code});
                    if(mode=="ambiguous")messages.Enqueue(new{id="other-message",time,text="Use this one-time code: 222222"});
                }
                await r.FulfillAsync(new(){Body="ok"});return;
            }
            if(r.Request.Url.EndsWith("/submit")) {submits++;await r.FulfillAsync(new(){ContentType="application/json",Body=Json.Write(new{ok=r.Request.PostData==code})});return;}
            await r.FulfillAsync(new(){ContentType="text/html",Body="<title>Verify identity</title><main><h1 id='organisation'>"+(mode=="wrongAccount"?"Another University":"Example University")+"</h1><p>Enter your verification code.</p><input id='otp' autocomplete='one-time-code' aria-label='Verification code'><button id='resend' onclick=\"fetch('/request',{method:'POST'})"+(mode=="changedChallenge"?".then(()=>location.reload())":"")+"\">Send a new code</button><button id='submit' onclick=\"fetch('/submit',{method:'POST',body:document.querySelector('#otp').value}).then(r=>r.json()).then(r=>{document.querySelector('#success').hidden=!r.ok})\">Verify</button><h2 id='success' "+(mode=="alreadyComplete"?"":"hidden")+">Welcome to your account</h2></main>"});
        });
        try {
            var source=await context.NewPageAsync();await source.GotoAsync(FreshModelQaTests.Inbox);await source.WaitForSelectorAsync("#old-message");
            var destination=await context.NewPageAsync();await destination.GotoAsync(FreshModelQaTests.Portal);await destination.BringToFrontAsync();await destination.WaitForTimeoutAsync(1200);
            var page=Json.Read<Evidence>(await bridge.Send("snapshot",new{},default));Assert.NotEmpty(page.InstanceId);
            workflow.Approved=true;workflow.AutoRun=true;workflow.Steps[0]=workflow.Steps[0] with{Transfer=workflow.Steps[0].Transfer! with{WaitSeconds=5}};workflow.Validate();
            var store=new LocalStore(receipts);var executor=new Executor(bridge,store);
            if(mode=="cancel"){
                using var cancel=new CancellationTokenSource(1200);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>executor.Run(workflow,new(),new Progress<string>(),cancel.Token,page.InstanceId));
                await Task.Delay(3500);Assert.Equal(0,submits);
            }else if(mode=="success"){
                var result=await executor.Run(workflow,new(),new Progress<string>(),default,page.InstanceId);Assert.Contains("Completed",result);
                Assert.True(await destination.IsVisibleAsync("#success"));Assert.Equal(1,submits);
                await Assert.ThrowsAsync<InvalidOperationException>(()=>executor.Run(workflow,new(),new Progress<string>(),default,page.InstanceId));Assert.Equal(1,requests);
            }else{
                await Assert.ThrowsAsync<InvalidOperationException>(()=>executor.Run(workflow,new(),new Progress<string>(),default,page.InstanceId));Assert.Equal(0,submits);
                if(mode is "wrongSender" or "wrongAccount" or "alreadyComplete")Assert.Equal(0,requests);
            }
            var stored=string.Join("",Directory.GetFiles(receipts).Select(File.ReadAllText))+Json.Write(workflow);
            Assert.DoesNotContain(code,stored);Assert.DoesNotContain("111111",stored);
        }finally{await context.CloseAsync();if(Directory.Exists(profile))Directory.Delete(profile,true);if(Directory.Exists(receipts))Directory.Delete(receipts,true);}
    }
    public static Workflow FixedRule()=>new(){Name="Verify university sign-in",Goal="Transfer a fresh message verification code",Trigger="Example University asks for a verification code",Variables=[],Steps=[new("transferFresh",FreshModelQaTests.Portal,"#otp",VerifySelector:"#success",Transfer:new(FreshModelQaTests.Inbox,".message","#sender","University Identity Service","#organisation","Example University","#resend","#submit","data-time","iso"))]};
}
public sealed class FreshBridgeTests {
    [Theory][InlineData("success")][InlineData("wrongSender")][InlineData("stale")][InlineData("ambiguous")][InlineData("timeout")][InlineData("cancel")][InlineData("wrongAccount")][InlineData("alreadyComplete")][InlineData("changedChallenge")]
    public Task FreshCodeTransferNeverGuesses(string mode)=>FreshBrowserQa.Run(FreshBrowserQa.FixedRule(),mode);
}
