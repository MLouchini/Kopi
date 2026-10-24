using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace Kopi.Core;
public static class FreshCompiler {
    sealed record Proposal(FreshTransfer Transfer,string VerifySelector,string[] Questions);
    public static async Task<Workflow> Compile(ILocalModel model,Evidence[] events,TaskUnderstanding task,CancellationToken ct){
        var target=events.LastOrDefault(e=>e.FieldType=="one-time-code");
        var source=events.LastOrDefault(e=>e.Kind=="copy"&&e.Url!=target?.Url);
        var w=new Workflow{Name="Complete verification",Goal=task.Goal,Trigger=task.Trigger,Variables=[],Columns=[]};
        if(target==null||source==null){w.Questions=["Demonstrate the verification input and copying the temporary value from its source."];return w;}
        var digitsMatch=Regex.Match(target.Value,@"\[([4-8])-digit");
        var attrs=new HashSet<string>();
        foreach(var e in events.Where(e=>e.Url==source.Url))try {
            using var context=JsonDocument.Parse(e.Context);
            if(context.RootElement.ValueKind==JsonValueKind.Array)foreach(var item in context.RootElement.EnumerateArray())if(item.TryGetProperty("timestamps",out var ts)&&ts.ValueKind==JsonValueKind.Object)foreach(var attr in ts.EnumerateObject())if(Regex.IsMatch(attr.Name,"^[a-zA-Z][a-zA-Z0-9_-]{0,50}$"))attrs.Add(attr.Name);
        }catch(JsonException){}
        if(!digitsMatch.Success||attrs.Count==0){w.Questions=["The recording needs a code-length hint and readable message timestamp metadata before a fresh transfer can be enabled."];return w;}
        var digits=int.Parse(digitsMatch.Groups[1].Value);
        var schema=JsonNode.Parse(WorkflowSchema.ForFreshTransfer().GetRawText())!["properties"]!["steps"]!["items"]!["properties"]!["transfer"]!.DeepClone();
        var p=schema["properties"]!;
        p["sourceUrl"]!["enum"]=new JsonArray(source.Url);p["digits"]!["enum"]=new JsonArray(digits);
        p["sourceSelector"]!["enum"]=new JsonArray(attrs.Select(a=>JsonValue.Create("["+a+"]")).ToArray());
        p["timestampAttribute"]!["enum"]=new JsonArray(attrs.Select(a=>JsonValue.Create(a)).ToArray());
        var root=new JsonObject{
            ["type"]="object",["additionalProperties"]=false,["required"]=new JsonArray("transfer","verifySelector","questions"),
            ["properties"]=new JsonObject{["transfer"]=schema,["verifySelector"]=new JsonObject{["type"]="string"},["questions"]=new JsonObject{["type"]="array",["items"]=new JsonObject{["type"]="string"}}}
        };
        const string prompt="""
        Compile ONLY the browser bindings for a demonstrated fresh temporary-value transfer.
        Runtime policy is already decided: codes are fresh for EACH run, never saved; observed source and organisation/account identities are fixed scope guards approved once by the user. Do NOT ask again about those policies or whether observed identity labels are constants.
        Choose sourceGuardSelector and its exact sourceIdentity from the observed message conversation label. Choose destinationGuardSelector and exact destinationIdentity from the observed organisation/account label on the code-entry page. They must identify the sender and intended account, not generic page text.
        requestSelector is the observed send/resend button on the DESTINATION. submitSelector is its verify button. verifySelector is the observed successful-login element. timestampFormat matches the observed timestamp attribute. openSourceSelector must be empty unless the recording actually shows clicking a chat-opening control. waitSeconds=35.
        The source URL, message selector candidates, timestamp attributes and code length are constrained by recorded evidence. Never copy a sample secret. Ask a question ONLY when a required binding is absent or ambiguous; leave that missing binding empty. No arbitrary code, no site-specific recipe.
        """;
        var proposal=Json.Read<Proposal>(await model.Complete(prompt,Intelligence.Condense(events),ct,JsonDocument.Parse(root.ToJsonString()).RootElement.Clone()));
        w.SourceHosts=[new Uri(target.Url).Host];w.Steps=[new("transferFresh",target.Url,target.Selector,VerifySelector:proposal.VerifySelector,Transfer:proposal.Transfer)];w.Questions=proposal.Questions;
        return w;
    }
}
