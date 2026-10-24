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
        Stop();
        foreach (var asset in new[] { m.Runtime, m.Model }) {
            if (Path.GetFileName(asset.FileName) != asset.FileName || string.IsNullOrWhiteSpace(asset.FileName)) throw new InvalidDataException("Manifest has an invalid file name.");
            var path = store.PathFor(asset==m.Model?"model.gguf":asset.FileName);
            var legacy=store.PathFor(asset.FileName);
            if(asset==m.Model&&!File.Exists(path)&&File.Exists(legacy)&&await VerifiedDownload.Hash(legacy,ct)==asset.Sha256.ToUpperInvariant())File.Move(legacy,path);
            await VerifiedDownload.Fetch(client,asset,path,progress,ct);
            if(asset==m.Model&&File.Exists(legacy)&&legacy!=path&&await VerifiedDownload.Hash(legacy,ct)==asset.Sha256.ToUpperInvariant())File.Delete(legacy);
        }
        var runtimeDir = store.PathFor("runtime"); Directory.CreateDirectory(runtimeDir);
        ZipFile.ExtractToDirectory(store.PathFor(m.Runtime.FileName), runtimeDir, true);
        var native=Path.Combine(AppContext.BaseDirectory,"native-runtime");
        if(Directory.Exists(native))foreach(var dll in Directory.GetFiles(native,"*.dll"))File.Copy(dll,Path.Combine(runtimeDir,Path.GetFileName(dll)),true);

        File.WriteAllText(store.PathFor("installed-model.txt"), m.ModelName);
    }
    public async Task Start(IProgress<string> progress, CancellationToken ct) {
        if (process is { HasExited: false }) {
            using var probe=new HttpClient{Timeout=TimeSpan.FromSeconds(3)};probe.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",ApiKey);
            try{using var health=await probe.GetAsync("http://127.0.0.1:47654/health",ct);if(health.IsSuccessStatusCode)return;}catch(HttpRequestException){}
            Stop();
        }
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
        try {
        for (var i = 0; i < 180; i++) {
            ct.ThrowIfCancellationRequested(); if (process.HasExited) { lock(log) throw new IOException("Local model failed: " + string.Join('\n',log)); }
            try { using var response = await client.GetAsync("http://127.0.0.1:47654/health", ct); if (response.IsSuccessStatusCode) { progress.Report("Local model ready · CPU · 8K context"); return; } } catch (HttpRequestException) {} catch (TaskCanceledException) when (!ct.IsCancellationRequested) {}
            progress.Report("Loading local model…"); await Task.Delay(1000, ct);
        }
        throw new TimeoutException("Model loading exceeded three minutes. Check available RAM and retry.");
        }catch{Stop();throw;}
    }
    public void Stop() { if (process is { HasExited: false }) process.Kill(true);process?.Dispose();process=null; }
    public void Dispose() => Stop();
}
