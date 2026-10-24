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
    public static JsonElement Value => JsonDocument.Parse("""
    {"type":"object","additionalProperties":false,"required":["name","goal","trigger","sourceHosts","variables","steps","columns","sheetName","questions"],"properties":{
    "name":{"type":"string"},"goal":{"type":"string"},"trigger":{"type":"string"},"sourceHosts":{"type":"array","items":{"type":"string"}},
    "variables":{"type":"array","minItems":1,"maxItems":20,"items":{"type":"object","additionalProperties":false,"required":["name","description","required"],"properties":{"name":{"type":"string"},"description":{"type":"string"},"required":{"type":"boolean"}}}},
    "steps":{"type":"array","minItems":1,"maxItems":30,"items":{"type":"object","additionalProperties":false,"required":["kind","url","selector","value","verifySelector"],"properties":{"kind":{"type":"string","enum":["navigate","fill","click","appendRow"]},"url":{"type":"string"},"selector":{"type":"string"},"value":{"type":"string"},"verifySelector":{"type":"string"}}}},
    "columns":{"type":"array","items":{"type":"string"}},"sheetName":{"type":"string"},"questions":{"type":"array","items":{"type":"string"}}}}
    """).RootElement.Clone();
}
