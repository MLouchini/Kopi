using Kopi.Core;
using Xunit;
namespace Kopi.Tests;
public sealed class FreshModelQaTests {
    public const string Portal="https://portal.test/verify", Inbox="https://messages.test/inbox";
    public static Evidence[] Demonstration=>[
        new("page",Portal,"Verify identity","Example University. Request a new verification code, then enter it here.",Context:"[{\"selector\":\"#organisation\",\"tag\":\"h1\",\"label\":\"Example University\"},{\"selector\":\"#otp\",\"tag\":\"input\",\"label\":\"Verification code\",\"type\":\"one-time-code\"},{\"selector\":\"#resend\",\"tag\":\"button\",\"label\":\"Send a new code\"},{\"selector\":\"#submit\",\"tag\":\"button\",\"label\":\"Verify\"}]"),
        new("click",Portal,"Verify identity",Label:"Send a new code",Selector:"#resend"),
        new("page",Inbox,"Messages","University Identity Service. A new sign-in code arrived.",Context:"[{\"selector\":\"#sender\",\"tag\":\"h1\",\"label\":\"University Identity Service\"},{\"selector\":\"#message-1\",\"classes\":\"message\",\"label\":\"Use this one-time code: [temporary code omitted]\",\"timestamps\":{\"data-time\":\"2026-10-24T12:00:01Z\"}},{\"selector\":\"main\",\"classes\":\"conversation\"}]"),
        new("copy",Inbox,"Messages",Label:"Use this one-time code",Selector:"#message-1",Value:"[temporary value omitted]",Context:"Messages with class message and data-time ISO timestamps are inside main; sender label #sender is University Identity Service."),
        new("fill",Portal,"Verify identity",Label:"Verification code",Selector:"#otp",Value:"[6-digit temporary value omitted]",FieldType:"one-time-code"),
        new("click",Portal,"Verify identity",Label:"Verify",Selector:"#submit"),
        new("page",Portal,"Signed in","Example University. Welcome to your account.",Context:"[{\"selector\":\"#success\",\"tag\":\"h2\",\"label\":\"Welcome to your account\"}]")
    ];
    [LocalModelFact]public async Task LearnsTransientTransferFromDemonstration()=>await Learn(new LlamaModel(),Environment.GetEnvironmentVariable("KOPI_REPORT_DIR")??Path.GetTempPath());
    public static async Task<Workflow> Learn(ILocalModel model,string reportDir) {
        var ai=new Intelligence(model);var trace=Demonstration;
        var understanding=await ai.Understand(trace,default);Assert.True(understanding.RequiresFreshValues);Assert.True(understanding.RequiresSensitiveValues);
        var w=await ai.Discover(trace,default);Directory.CreateDirectory(reportDir);await File.WriteAllTextAsync(Path.Combine(reportDir,"learned-fresh-transfer.json"),Json.Write(w));
        w.Validate();Assert.True(w.IsFreshTransfer);Assert.Empty(w.Variables);Assert.Equal(Portal,w.Steps[0].Url);Assert.Equal(Inbox,w.Steps[0].Transfer!.SourceUrl);Assert.Equal(6,w.Steps[0].Transfer!.Digits);
        Assert.False(w.Approved);Assert.False(w.AutoRun);
        Assert.True(await ai.Relevant(w,new("page",Portal,"Verify identity","Example University. Enter the new verification code sent to your messages."),default));
        Assert.False(await ai.Relevant(w,new("page","https://news.test/story","Campus news","The university opened a new library. It has no sign-in requirement."),default));
        return w;
    }
}
