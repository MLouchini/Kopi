# Kopi ↻

A Windows WPF application that observes an explicitly demonstrated browser task, asks a local Gemma model to generalise it, and runs approved browser actions on new inputs in the **same signed-in Chrome/Edge session**.

**Early preview.** See [QA evidence and limitations](docs/qa.md) before using it for important data. Generic forms and Google Sheets row append are the initial execution primitives. This is not a universal web agent.

## Use

Download **Kopi-Setup.exe** or **Kopi-win-x64.zip** from [Releases](https://github.com/MLouchini/Kopi/releases). The ZIP is self-contained: extract it before running Kopi.exe. Before a release is published, builds are under Actions → Windows build and QA → artifacts.

Follow the [first-run guide](docs/first-run.md). No Python, Node, Ollama or .NET installation is required for end users. The app downloads the pinned Gemma 4 E2B QAT Q4_0 and llama.cpp CPU runtime (about 3.4 GB), verifies SHA-256, and starts local inference. The extension currently requires manual Load unpacked; browser-store distribution is not configured.

## Develop

Install .NET 10 SDK; on Linux cross-compilation requires the Windows targeting pack (restored automatically). WPF itself runs only on Windows.

```sh
dotnet restore Kopi.sln
dotnet build Kopi.sln
# Install test Chromium (PowerShell; or use your installed Chromium via KOPI_CHROMIUM):
pwsh tests/Kopi.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test tests/Kopi.Tests
```

For real inference tests, start the pinned llama-server CPU binary with the model in `model-manifest.json`, on `127.0.0.1:47654`, context 8192, one slot and 2–6 threads. Set `KOPI_REAL_MODEL=1` and optionally `KOPI_REPORT_DIR`, then run `dotnet test --filter FullyQualifiedName~RealModelTests`. Real-model tests are explicitly skipped without opt-in; ordinary tests never pretend to exercise AI.

On Windows: `dotnet run --project src/Kopi.Desktop`. CI builds and tests on Windows, runs a WPF UI Automation smoke test, produces a self-contained ZIP and Inno Setup installer. Push a `v*` tag to attach packages to a GitHub release. The static download page is in `site/`; enable GitHub Pages with **GitHub Actions** as its source to deploy it. GitHub Releases hosts binaries; Pages hosts the page, not multi-gigabyte weights.

[Architecture and decisions](docs/architecture.md) · [Privacy and permissions](docs/privacy.md) · [First-run test](docs/first-run.md)
