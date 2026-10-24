using Kopi.Core;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Kopi.Tests;

public sealed class EmailWorkflowTests(ITestOutputHelper output) {
    [Fact] public Task RecordsAndSnapshotsReadOnlyEmailInsideRelatedFrame()=>Run(false);
    [LocalModelFact] public Task RealGemmaLearnsEmailToSheetAndAppendsDifferentContact()=>Run(true);

    async Task Run(bool realModel) {
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(7));
        using var bridge=new BrowserBridge();bridge.Start();
        using var pw=await Playwright.CreateAsync();
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        var extension=Path.Combine(root,"extension");
        var profile=Path.Combine(Path.GetTempPath(),"kopi-email-"+Guid.NewGuid());
        var receiptDir=Path.Combine(Path.GetTempPath(),"kopi-receipts-"+Guid.NewGuid());
        await using var context=await pw.Chromium.LaunchPersistentContextAsync(profile,new(){
            Headless=true,Channel="chromium",ExecutablePath=Environment.GetEnvironmentVariable("KOPI_CHROMIUM"),
            IgnoreDefaultArgs=["--disable-extensions"],
            Args=["--enable-unsafe-extension-debugging",$"--disable-extensions-except={extension}",$"--load-extension={extension}"]
        });
        try {
            var cdp=await context.NewCDPSessionAsync(context.Pages[0]);string? extensionUrl=null;
            for(var i=0;i<40&&extensionUrl==null;i++){
                var targets=await cdp.SendAsync("Target.getTargets");
                extensionUrl=targets!.Value.GetProperty("targetInfos").EnumerateArray().Select(t=>t.GetProperty("url").GetString()).FirstOrDefault(u=>u?.StartsWith("chrome-extension://")==true);
                if(extensionUrl==null)await Task.Delay(200,timeout.Token);
            }
            Assert.NotNull(extensionUrl);
            var popup=await context.NewPageAsync();await popup.GotoAsync(new Uri(new Uri(extensionUrl),"popup.html").ToString());
            await popup.FillAsync("#code",bridge.PairCode);await popup.ClickAsync("#pair");
            await popup.WaitForFunctionAsync("()=>document.querySelector('#status').textContent.includes('Connected')");
            await popup.CloseAsync();
            const string mailOrigin="https://mail.fixture.test",sheetUrl="https://docs.google.com/spreadsheets/d/email-fixture/edit";
            await context.GrantPermissionsAsync(["clipboard-read","clipboard-write"],new(){Origin=mailOrigin});
            await context.GrantPermissionsAsync(["clipboard-read","clipboard-write"],new(){Origin="https://docs.google.com"});
            await context.RouteAsync(mailOrigin+"/**",async route=>{
                var second=route.Request.Url.EndsWith("/second");
                var message=second
                    ? "<div contenteditable='false'><h2>Partnership enquiry</h2><p>Please reply to <span id='address'>nora@harbour.example</span>.</p><blockquote>I'm <strong id='person'>Nora Vale</strong>. Could we discuss a new project?</blockquote></div>"
                    : "<div contenteditable='false'><p>Hi, my name is <span id='person'>Alina Reid</span>.</p><p>Can we discuss a project? Contact me at <span id='address'>alina@cedar.example</span>.</p></div>";
                await route.FulfillAsync(new(){ContentType="text/html",Body="<title>Email inbox</title><nav>Inbox Sent Drafts</nav><main><h1>Open message</h1><iframe title='Message body' style='width:900px;height:350px;border:0' srcdoc=\""+WebUtility.HtmlEncode(message)+"\"></iframe></main>"});
            });
            var rows=new List<string[]>{new[]{"Name","Contact"}};
            await context.RouteAsync("https://docs.google.com/**",async route=>{
                if(route.Request.Url.Contains("/gviz/tq?")){
                    await route.FulfillAsync(new(){ContentType="text/csv",Body=string.Join("\n",rows.Select(r=>string.Join(",",r)))});return;
                }
                if(route.Request.Url.EndsWith("/rows")){
                    rows=Json.Read<List<string[]>>(route.Request.PostData!);await route.FulfillAsync(new(){Body="ok"});return;
                }
                await route.FulfillAsync(new(){ContentType="text/html",Body="""
                <title>Contacts spreadsheet</title><main><h1>Contacts</h1><p>Active tab: Contacts. Columns A:B: Name, Contact email.</p>
                <input class="waffle-name-box" aria-label="Name box"><textarea id="cells" aria-label="Spreadsheet cells"></textarea>
                <label>Name, cell A2<input id="a2"></label><label>Contact, cell B2<input id="b2"></label><button id="save">Save row</button><p id="saved" hidden>Row saved in Contacts</p></main>
                <script>
                window.rows=[['Name','Contact']];
                const persist=()=>fetch('/rows',{method:'POST',body:JSON.stringify(rows)});
                document.querySelector('.waffle-name-box').onkeydown=e=>{if(e.key==='Enter')document.querySelector('#cells').focus()};
                document.querySelector('#cells').onpaste=e=>{e.preventDefault();rows.push(e.clipboardData.getData('text/plain').split('\t'));persist()};
                document.querySelector('#save').onclick=()=>{rows.push([document.querySelector('#a2').value,document.querySelector('#b2').value]);persist();document.querySelector('#saved').hidden=false};
                </script>
                """});
            });
            var trace=new ConcurrentQueue<Evidence>();bridge.Received+=trace.Enqueue;
            bridge.Observing=true;
            var mail=await context.NewPageAsync();await mail.GotoAsync(mailOrigin+"/first");
            var sheet=await context.NewPageAsync();await sheet.GotoAsync(sheetUrl);
            await Task.Delay(1800,timeout.Token);
            var frame=mail.Frames.Single(f=>f.ParentFrame!=null);
            async Task CopyToCell(string source,string target){
                await mail.BringToFrontAsync();await frame.Locator(source).ClickAsync();
                await frame.EvaluateAsync("selector=>{const r=document.createRange();r.selectNodeContents(document.querySelector(selector));const s=getSelection();s.removeAllRanges();s.addRange(r)}",source);
                await mail.Keyboard.PressAsync("Control+C");
                await sheet.BringToFrontAsync();await sheet.FocusAsync(target);await sheet.Keyboard.PressAsync("Control+V");
            }
            await CopyToCell("#person","#a2");await CopyToCell("#address","#b2");
            await sheet.ClickAsync("#save");await Task.Delay(800,timeout.Token);bridge.Observing=false;
            Assert.Equal("Alina Reid",await sheet.InputValueAsync("#a2"));
            Assert.Equal("alina@cedar.example",await sheet.InputValueAsync("#b2"));
            Assert.Contains(trace,e=>e.Kind=="copy"&&e.Value.Contains("Alina Reid")&&e.Url.StartsWith(mailOrigin));
            Assert.Contains(trace,e=>e.Kind=="paste"&&e.Value.Contains("alina@cedar.example"));
            await mail.BringToFrontAsync();
            var first=Json.Read<Evidence>(await bridge.Send("snapshot",new{},timeout.Token));
            Assert.Contains("Alina Reid",first.Text);Assert.Contains("alina@cedar.example",first.Text);
            output.WriteLine("Actual paired extension captured copies in a related frame and read its message body.");
            if(!realModel)return;

            using var runtime=new ModelRuntime(new LocalStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Kopi")));
            using var model=new LlamaModel();model.Authenticate(runtime.ApiKey);
            await runtime.Start(new Progress<string>(),timeout.Token);
            var ai=new Intelligence(model);
            var learned=await ai.Discover(trace,timeout.Token);
            output.WriteLine("Discovered: "+Json.Write(learned));
            if(learned.Questions.Length>0||learned.Columns.Length!=2||learned.Columns.Any(c=>!learned.Variables.Any(v=>v.Name==c))){
                learned=await ai.Clarify(learned,"The exact spreadsheet tab is Contacts. Column A is the contact's full name; column B is their email address. Use two variables and append one new row per email. Keep the observed spreadsheet URL.",timeout.Token);
                output.WriteLine("After clarification: "+Json.Write(learned));
            }
            learned.Validate();Assert.Equal("Contacts",learned.SheetName);Assert.Equal(2,learned.Columns.Length);
            Assert.DoesNotContain(learned.Variables,v=>v.Description.Contains("Alina"));
            learned.Approved=true;
            await mail.GotoAsync(mailOrigin+"/second");await mail.BringToFrontAsync();await Task.Delay(500,timeout.Token);
            var secondPage=Json.Read<Evidence>(await bridge.Send("snapshot",new{},timeout.Token));
            Assert.Contains("Nora Vale",secondPage.Text);
            var values=await ai.Extract(learned,secondPage,timeout.Token);
            Assert.Contains("Nora Vale",values.Values);Assert.Contains("nora@harbour.example",values.Values);
            Assert.DoesNotContain("Alina Reid",values.Values);
            var result=await new Executor(bridge,new LocalStore(receiptDir)).Run(learned,values,new Progress<string>(),timeout.Token);
            Assert.Contains("Completed",result);
            Assert.Equal(3,rows.Count);Assert.Equal(new[]{"Nora Vale","nora@harbour.example"},rows[2]);
            output.WriteLine("Real Gemma learned from recorded browser actions, extracted a different framed email and verified an appended row through the real extension.");
        } finally {
            await context.CloseAsync();
            if(Directory.Exists(profile))Directory.Delete(profile,true);
            if(Directory.Exists(receiptDir))Directory.Delete(receiptDir,true);
        }
    }
}
