using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Kopi.Core;
public sealed class Executor(IBrowserSession browser, LocalStore store) {
    readonly SemaphoreSlim gate = new(1);
    public async Task<string> Run(Workflow w, Dictionary<string,string> values, IProgress<string> progress, CancellationToken ct) {
        await gate.WaitAsync(ct);
        try {
            w.Validate(); if (!w.Approved) throw new InvalidOperationException("Approve and save this workflow first.");
            foreach (var v in w.Variables) if (v.Required && string.IsNullOrWhiteSpace(values.GetValueOrDefault(v.Name))) throw new InvalidDataException($"Missing {v.Name}");
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(w.Id + Json.Write(values.OrderBy(x => x.Key)))));
            var receipt = store.PathFor(key + ".receipt");
            if (File.Exists(receipt)) throw new InvalidOperationException("This exact run was already attempted. Check its destination before repeating it; Kopi blocks automatic duplicate retries.");
            // Persist before sending any action. A crash or uncertain response must not blindly repeat writes.
            await File.WriteAllTextAsync(receipt, "pending", ct);
            try {
                for (int i = 0; i < w.Steps.Count; i++) {
                    ct.ThrowIfCancellationRequested(); var s = w.Steps[i];
                    progress.Report($"Step {i + 1} of {w.Steps.Count} · {s.Kind}");
                    var expanded = Regex.Replace(s.Value, @"\{\{(\w+)\}\}", m => values.GetValueOrDefault(m.Groups[1].Value, ""));
                    await browser.Send("step", new { step = s with { Value = expanded }, columns = w.Columns, sheetName = w.SheetName, values }, ct);
                }
                await File.WriteAllTextAsync(receipt, "verified", CancellationToken.None);
                return "Completed. Destination checks passed.";
            } catch { await File.WriteAllTextAsync(receipt, "uncertain — inspect the destination before retrying", CancellationToken.None); throw; }
        } finally { gate.Release(); }
    }
}
