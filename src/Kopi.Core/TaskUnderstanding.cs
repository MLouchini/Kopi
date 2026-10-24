using System.Text.Json;
namespace Kopi.Core;
public sealed record TaskUnderstanding(string Goal,string Trigger,string Source,string Destination,bool RequiresFreshValues,bool RequiresSensitiveValues,string[] Questions);
public static class TaskUnderstandingSchema {
    public static JsonElement Value=>JsonDocument.Parse("""
    {"type":"object","additionalProperties":false,"required":["goal","trigger","source","destination","requiresFreshValues","requiresSensitiveValues","questions"],"properties":{"goal":{"type":"string"},"trigger":{"type":"string"},"source":{"type":"string"},"destination":{"type":"string"},"requiresFreshValues":{"type":"boolean"},"requiresSensitiveValues":{"type":"boolean"},"questions":{"type":"array","items":{"type":"string"}}}}
    """).RootElement.Clone();
}
