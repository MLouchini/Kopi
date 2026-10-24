using System.Diagnostics;
using System.Security.Cryptography;
using System.IO.Compression;

namespace Kopi.Core;
public sealed record DownloadAsset(string Url, string Sha256, string FileName);
public sealed record RuntimeManifest(string ModelName, DownloadAsset Runtime, DownloadAsset Model);
public sealed class ModelRuntime(LocalStore store) : IDisposable {
    Process? process;
    public string ApiKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public async Task Download(string manifestPath, IProgress<string> progress, CancellationToken ct) {
        var m = Json.Read<RuntimeManifest>(await File.ReadAllTextAsync(manifestPath, ct));
        if (!m.ModelName.Contains("gemma", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a Gemma model manifest.");
        using var client = new HttpClient { Timeout = TimeSpan.FromHours(2) };
        foreach (var asset in new[] { m.Runtime, m.Model }) {
            if (!Uri.TryCreate(asset.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || asset.Sha256.Length != 64 || Path.GetFileName(asset.FileName) != asset.FileName) throw new InvalidDataException("Manifest requires HTTPS URLs, SHA-256 digests and simple file names.");
            var path = store.PathFor(asset.FileName); var temp = path + ".download";
            if (File.Exists(path) && await Hash(path, ct) == asset.Sha256.ToUpperInvariant()) continue;
            try {
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct); response.EnsureSuccessStatusCode();
                await using (var input = await response.Content.ReadAsStreamAsync(ct))
                await using (var output = File.Create(temp)) {
                    var buffer = new byte[1024 * 1024]; long total = 0; int read;
                    while ((read = await input.ReadAsync(buffer, ct)) > 0) { await output.WriteAsync(buffer.AsMemory(0, read), ct); total += read; progress.Report($"Downloading {asset.FileName}: {total / 1048576} MB" ); }
                }
                if (await Hash(temp, ct) != asset.Sha256.ToUpperInvariant()) throw new InvalidDataException("Download checksum mismatch. Artifact rejected.");
                File.Move(temp, path, true);
            } finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        var runtimeDir = store.PathFor("runtime"); Directory.CreateDirectory(runtimeDir);
        ZipFile.ExtractToDirectory(store.PathFor(m.Runtime.FileName), runtimeDir, true);
        File.Copy(store.PathFor(m.Model.FileName), store.PathFor("model.gguf"), true);
        File.WriteAllText(store.PathFor("installed-model.txt"), m.ModelName);
    }
    static async Task<string> Hash(string p, CancellationToken ct) { await using var s = File.OpenRead(p); return Convert.ToHexString(await SHA256.HashDataAsync(s, ct)); }
    public async Task Start(IProgress<string> progress, CancellationToken ct) {
        if (process is { HasExited: false }) return;
        var exe = Directory.Exists(store.PathFor("runtime")) ? Directory.GetFiles(store.PathFor("runtime"), "llama-server.exe", SearchOption.AllDirectories).FirstOrDefault() : null;
        if (exe == null || !File.Exists(store.PathFor("model.gguf"))) throw new InvalidOperationException("Install the local model from a verified model manifest in Setup first.");
        var info = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var arg in new[] { "-m", store.PathFor("model.gguf"), "--host", "127.0.0.1", "--port", "47654", "-c", "8192", "-np", "1", "-t", Math.Clamp(Environment.ProcessorCount / 2, 2, 6).ToString(), "-ngl", "0", "--jinja", "--api-key", ApiKey }) info.ArgumentList.Add(arg);
        process = Process.Start(info) ?? throw new IOException("Could not start local inference.");
        var log = new Queue<string>();
        void Capture(object sender, DataReceivedEventArgs e) { if (e.Data is null) return; lock (log) { log.Enqueue(e.Data); while (log.Count > 50) log.Dequeue(); } }
        process.ErrorDataReceived += Capture; process.OutputDataReceived += Capture; process.BeginErrorReadLine(); process.BeginOutputReadLine();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",ApiKey);
        for (var i = 0; i < 180; i++) {
            ct.ThrowIfCancellationRequested(); if (process.HasExited) { lock(log) throw new IOException("Local model failed: " + string.Join('\n',log)); }
            try { using var response = await client.GetAsync("http://127.0.0.1:47654/health", ct); if (response.IsSuccessStatusCode) { progress.Report("Local model ready · CPU · 8K context"); return; } } catch (HttpRequestException) {} catch (TaskCanceledException) when (!ct.IsCancellationRequested) {}
            progress.Report("Loading local model…"); await Task.Delay(1000, ct);
        }
        throw new TimeoutException("Model loading exceeded three minutes. Try a smaller quantization.");
    }
    public void Dispose() { if (process is { HasExited: false }) process.Kill(true); process?.Dispose(); }
}
