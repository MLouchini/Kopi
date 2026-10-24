using Kopi.Core;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;
namespace Kopi.Tests;
public sealed class WindowsModelQaFactAttribute:FactAttribute {
    public WindowsModelQaFactAttribute(){if(!OperatingSystem.IsWindows()||Environment.GetEnvironmentVariable("KOPI_WINDOWS_MODEL_QA")!="1")Skip="Deep QA: downloads and runs real model on Windows (opt-in).";}
}
public sealed class WindowsModelQaTests(ITestOutputHelper output) {
    [WindowsModelQaFact]public async Task CleanDownloadRealInferenceThenWarmRestart() {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
        var store=new LocalStore(Path.Combine(root,"artifacts/windows-model-qa"));
        using var runtime=new ModelRuntime(store);using var model=new LlamaModel();model.Authenticate(runtime.ApiKey);
        var watch=Stopwatch.StartNew();var lastProgress=DateTime.MinValue;var progress=new Progress<string>(s=>{if(DateTime.UtcNow-lastProgress>TimeSpan.FromSeconds(10)){lastProgress=DateTime.UtcNow;output.WriteLine(s);}});
        var manifest=Path.Combine(root,"model-manifest.json");
        await runtime.Download(manifest,progress,default);
        Assert.True(new FileInfo(store.PathFor("model.gguf")).Length>3_000_000_000L);
        Assert.False(File.Exists(store.PathFor(Json.Read<RuntimeManifest>(await File.ReadAllTextAsync(manifest)).Model.FileName)));
        output.WriteLine($"PASS 1: first download/checksum completed in {watch.Elapsed.TotalSeconds:F1}s");
        await runtime.Start(progress,default);
        await RealModelTests.VerifyTracker(model,Path.Combine(store.Root,"pass1"));
        await FreshBrowserQa.Run(await FreshModelQaTests.Learn(model,Path.Combine(store.Root,"pass1")));
        var process=Process.GetProcessesByName("llama-server").Single();
        output.WriteLine($"Native peak working set={process.PeakWorkingSet64/1048576} MiB; GC available memory budget={GC.GetGCMemoryInfo().TotalAvailableMemoryBytes/1048576} MiB");
        runtime.Stop();
        var stamp=File.GetLastWriteTimeUtc(store.PathFor("model.gguf"));watch.Restart();
        await runtime.Download(manifest,progress,default);Assert.Equal(stamp,File.GetLastWriteTimeUtc(store.PathFor("model.gguf")));
        await runtime.Start(progress,default);
        await RealModelTests.VerifyTracker(model,Path.Combine(store.Root,"pass2"));
        await FreshBrowserQa.Run(await FreshModelQaTests.Learn(model,Path.Combine(store.Root,"pass2")));
        output.WriteLine($"PASS 2: verified-cache reinstall, restart and real inference completed in {watch.Elapsed.TotalSeconds:F1}s");
    }
}
