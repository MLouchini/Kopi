using System.Text.Json;
namespace Kopi.Core;
public static class WorkflowSchema {
    public static JsonElement ForSpreadsheet(string url) {
        var node=System.Text.Json.Nodes.JsonNode.Parse(Value.GetRawText())!;
        var props=node["properties"]!;
        props["sourceHosts"]!["maxItems"]=0;
        props["steps"]!["minItems"]=1; props["steps"]!["maxItems"]=1;
        props["steps"]!["items"]!["properties"]!["kind"]!["enum"]=new System.Text.Json.Nodes.JsonArray("appendRow");
        props["steps"]!["items"]!["properties"]!["url"]!["enum"]=new System.Text.Json.Nodes.JsonArray(url);
        foreach(var field in new[]{"selector","value","verifySelector"})props["steps"]!["items"]!["properties"]![field]!["maxLength"]=0;
        return JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();
    }
    public static JsonElement ForFreshTransfer() {
        var node=System.Text.Json.Nodes.JsonNode.Parse(Value.GetRawText())!;var p=node["properties"]!;
        p["variables"]!["maxItems"]=0;p["steps"]!["minItems"]=1;p["steps"]!["maxItems"]=1;
        p["steps"]!["items"]!["properties"]!["kind"]!["enum"]=new System.Text.Json.Nodes.JsonArray("transferFresh");
        p["steps"]!["items"]!["required"]!.AsArray().Add("transfer");p["steps"]!["items"]!["properties"]!["value"]!["maxLength"]=0;
        return JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();
    }
    public static JsonElement Value {
        get {
            var node=System.Text.Json.Nodes.JsonNode.Parse(Base.GetRawText())!;
            var p=node["properties"]!;p["variables"]!["minItems"]=0;
            p["steps"]!["items"]!["properties"]!["kind"]!["enum"]!.AsArray().Add("transferFresh");
            p["steps"]!["items"]!["properties"]!["transfer"]=System.Text.Json.Nodes.JsonNode.Parse("""
            {"type":"object","additionalProperties":false,"required":["sourceUrl","sourceSelector","sourceGuardSelector","sourceIdentity","destinationGuardSelector","destinationIdentity","requestSelector","submitSelector","timestampAttribute","timestampFormat","openSourceSelector","digits","waitSeconds"],"properties":{"sourceUrl":{"type":"string"},"sourceSelector":{"type":"string"},"sourceGuardSelector":{"type":"string"},"sourceIdentity":{"type":"string"},"destinationGuardSelector":{"type":"string"},"destinationIdentity":{"type":"string"},"requestSelector":{"type":"string"},"submitSelector":{"type":"string"},"timestampAttribute":{"type":"string"},"timestampFormat":{"type":"string","enum":["iso","unixSeconds","unixMilliseconds","dmy","mdy"]},"openSourceSelector":{"type":"string"},"digits":{"type":"integer","minimum":4,"maximum":8},"waitSeconds":{"type":"integer","minimum":5,"maximum":40}}}
            """);
            return JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();
        }
    }
    static JsonElement Base => JsonDocument.Parse("""
    {"type":"object","additionalProperties":false,"required":["name","goal","trigger","sourceHosts","variables","steps","columns","sheetName","questions"],"properties":{
    "name":{"type":"string"},"goal":{"type":"string"},"trigger":{"type":"string"},"sourceHosts":{"type":"array","items":{"type":"string"}},
    "variables":{"type":"array","minItems":1,"maxItems":20,"items":{"type":"object","additionalProperties":false,"required":["name","description","required"],"properties":{"name":{"type":"string"},"description":{"type":"string"},"required":{"type":"boolean"}}}},
    "steps":{"type":"array","minItems":1,"maxItems":30,"items":{"type":"object","additionalProperties":false,"required":["kind","url","selector","value","verifySelector"],"properties":{"kind":{"type":"string","enum":["navigate","fill","click","appendRow"]},"url":{"type":"string"},"selector":{"type":"string"},"value":{"type":"string"},"verifySelector":{"type":"string"}}}},
    "columns":{"type":"array","items":{"type":"string"}},"sheetName":{"type":"string"},"questions":{"type":"array","items":{"type":"string"}}}}
    """).RootElement.Clone();
}
