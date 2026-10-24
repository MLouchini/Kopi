using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kopi.Core;
public static class Json {
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Empty JSON response.");
}
public sealed record Evidence(string Kind, string Url, string Title, string Text = "", string Label = "", string Selector = "", string Value = "", string FieldType = "", string[]? Headers = null);
public sealed record Variable(string Name, string Description, bool Required = true);
public sealed record Step(string Kind, string Url = "", string Selector = "", string Value = "", string VerifySelector = "");
public sealed class Workflow {
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New workflow";
    public string Goal { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string[] SourceHosts { get; set; } = [];
    public List<Variable> Variables { get; set; } = [];
    public List<Step> Steps { get; set; } = [];
    public string[] Columns { get; set; } = [];
    public string SheetName { get; set; } = "Sheet1";
    public string[] Questions { get; set; } = [];
    public bool Approved { get; set; }
    public void Validate() {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Goal) || string.IsNullOrWhiteSpace(Trigger)) throw new InvalidDataException("Name, goal and trigger are required.");
        if (Questions.Length > 0) throw new InvalidDataException("Resolve the clarification questions before approval.");
        if (Variables.Count is 0 or > 20 || Variables.Select(v => v.Name).Distinct().Count() != Variables.Count || Variables.Any(v => !Regex.IsMatch(v.Name, "^[a-zA-Z][a-zA-Z0-9_]{0,39}$"))) throw new InvalidDataException("Use 1–20 uniquely named variables.");
        if (Steps.Count is 0 or > 30) throw new InvalidDataException("A workflow needs 1–30 steps.");
        foreach (var s in Steps) {
            if (!new[] { "navigate", "fill", "click", "appendRow" }.Contains(s.Kind)) throw new InvalidDataException($"Unsupported action: {s.Kind}");
            if (s.Kind is "navigate" or "appendRow") SafeUrl(s.Url);
            if (s.Kind is "fill" or "click" && string.IsNullOrWhiteSpace(s.Selector)) throw new InvalidDataException("An action is missing its locator.");
            if (s.Kind == "click" && string.IsNullOrWhiteSpace(s.VerifySelector)) throw new InvalidDataException("Clicks require a visible success locator; clarify it before approval.");
            foreach (Match m in Regex.Matches(s.Value, @"\{\{(\w+)\}\}")) if (!Variables.Any(v => v.Name == m.Groups[1].Value)) throw new InvalidDataException("Unknown variable in step.");
            if (s.Kind == "appendRow" && (Columns.Length == 0 || Columns.Any(c => !Variables.Any(v => v.Name == c)) || string.IsNullOrWhiteSpace(SheetName))) throw new InvalidDataException("Review spreadsheet column order and tab name.");
        }
    }
    public static Uri SafeUrl(string value) {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var u) || u.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(u.UserInfo)) throw new InvalidDataException("Only HTTP(S) URLs without embedded credentials are supported.");
        return u;
    }
}
public static class Privacy {
    public static string RedactText(string text,string context="") => Regex.IsMatch(context+" "+text,@"verification|security\s*code|one[ -]?time|two[ -]?factor|authenticat|sign[ -]?in\s*code|\b2fa\b",RegexOptions.IgnoreCase)?Regex.Replace(text,@"\b\d{4,8}\b","[temporary code omitted]"):text;
    public static Evidence Sanitize(Evidence e) {
        var u = Workflow.SafeUrl(e.Url);
        var safeUrl = u.GetLeftPart(UriPartial.Path); // query/fragment may contain tokens
        var verification=Regex.IsMatch(e.FieldType+" "+e.Label+" "+e.Selector,"one-time-code|verification|otp|otc",RegexOptions.IgnoreCase);
        if(verification)return e with {Url=safeUrl,Title=RedactText(e.Title,e.Text),Text=RedactText(e.Text,e.Title),Value="[temporary value omitted]",FieldType="one-time-code"};
        if (Regex.IsMatch(e.FieldType + " " + e.Label + " " + e.Selector, "password|secret|token|credit.?card|cc-number|one.?time|otp|cvc|cvv", RegexOptions.IgnoreCase)) return e with { Url = safeUrl, Text = "", Value = "", Label = "[sensitive field omitted]", Selector = "" };
        return e with { Url = safeUrl, Title = RedactText(Limit(e.Title, 200),e.Text), Text = RedactText(Limit(e.Text, 4000),e.Title), Value = Regex.IsMatch(e.Value,@"^\d{4,8}$")&&e.Kind is "copy" or "paste"?"[temporary value omitted]":RedactText(Limit(e.Value, 2000),e.Title+" "+e.Label+" "+e.Text), Label = Limit(e.Label, 200), Selector = Limit(e.Selector, 400) };
    }
    public static string Limit(string s, int length) => s.Length <= length ? s : s[..length];
}
public sealed class LocalStore {
    public string Root { get; }
    public LocalStore(string root) { Root = root; Directory.CreateDirectory(root); }
    public string PathFor(string file) => Path.Combine(Root, file);
    public List<Workflow> Load() => File.Exists(PathFor("workflows.json")) ? Json.Read<List<Workflow>>(File.ReadAllText(PathFor("workflows.json"))) : [];
    public void Save(List<Workflow> workflows) { var tmp = PathFor("workflows.json.tmp"); File.WriteAllText(tmp, Json.Write(workflows)); File.Move(tmp, PathFor("workflows.json"), true); }
}
