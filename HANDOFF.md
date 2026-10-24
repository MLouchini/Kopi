# Local Windows continuation

## Normal email workflow fixes — 24 October 2026

- All temporary UI-demo code, hooks, flags, fixed contacts, sheet targets and artificial delays were removed. The saved temporary workflow was removed from the local library. Runtime source and compiled output were checked; only genuine model-driven discovery and execution remain. The obsolete static demo pages were removed as well.
- A real recorded email-to-Sheets request failed because 9,554 tokens exceeded the 8,192-token context. Spreadsheet discovery now drops unused DOM locator/context metadata while retaining page evidence, clipboard values and column labels. Model rejection messages now include the backend's reason.
- Clarification used to overwrite a rewritten proposal with questions calculated from the old proposal. It now returns the revised workflow and its own current questions together. Genuine unresolved questions still block approval.
- Observation and snapshots share a rendered-text reader: read-only content is retained, while hidden text, editable drafts and sensitive inputs remain excluded. Related frames are observed and included in snapshots; events retain the enclosing tab URL.
- Suggestions account for message text changes at the same URL and revisit the latest page after a busy operation. Preview now shows the source page and a short excerpt before approval.
- Verified: desktop Release build passed with zero warnings/errors; 29 relevant regressions passed (one opt-in real-model test skipped in that run); a separate real Gemma test passed recorded framed-email discovery, user review/correction, extraction from a second contact and verified append through the paired extension into a Sheets fixture.
- A separate read-only probe used the user's actual signed-in Outlook tab and local Gemma. Both expected contact fields were extracted correctly. No write to the user's actual Google Sheet was sent. The supplied Outlook DOM itself contained no iframe and was readable by the original snapshot too; frame support is a generic robustness fix, not a proven explanation of that live attempt.
- Model proposals can still need correction, including extra inferred columns. Rehearse the actual account-to-Sheets flow before presenting. No preset answers or timers substitute for inference.

## Local setup verified — 24 October 2026

- Installed Microsoft .NET SDK **10.0.402** with the .NET, ASP.NET Core and Windows Desktop **10.0.13** runtimes. Existing .NET 8 remains installed.
- `dotnet build src/Kopi.Desktop -c Release --no-restore` passed with zero warnings/errors after restore. Launched the WPF app and verified its four tabs, pairing control and loopback bridge on port 47653.
- Downloaded the manifest-pinned model and CPU runtime to the user's Kopi data directory and verified their SHA-256 digests. Completed installation through the app's Setup screen. Status: **Local model ready · CPU · 8K context**. One small real JSON inference request passed in approximately one second; this is a startup smoke check, not a workflow benchmark.
- Browser extension loading/pairing and real-account workflow acceptance still require the user's browser. No personal account was used for the startup check.
- User explicitly declined adding OpenRouter; keep inference local.
- GitHub Pages is live with working public release downloads; see the updated hosting note below. The expanded Windows model QA finished with a workflow-clarification failure, also recorded below.

## User direction

- Build a real local-AI browser workflow learner with C#/.NET and WPF. Core workflow functionality comes first.
- Keep the user's **existing signed-in Chrome/Edge session**. Do not introduce a separate browser profile requiring another login.
- Learn from a demonstration; do not hardcode an internship tracker or the university/WhatsApp example.
- The new priority scenario is university verification: request/receive a code in WhatsApp Web, transfer it to the university challenge, and verify sign-in. After enabling that particular learned workflow once, the user explicitly wants it to run **without per-run approval**.
- User requested regular GitHub commits; keep doing so.
- User asked about “.NET Framework,” with an exception for a costly migration. Explained that this is modern **.NET 10 + WPF**, and legacy .NET Framework 4.8 would require a substantial rewrite. Keep the current stack unless the user explicitly changes that decision.
- Latest instruction: finish the in-progress QA step, commit, and stop so work can continue in a local chat. Do not start another feature/release cycle as part of this handoff.

## Repository and current build

Repository: https://github.com/MLouchini/Kopi (`main`). Implementation checkpoint tested by the full Windows runs: `0e76dc9`. The final handoff commit additionally aborts fresh-value work if the desktop heartbeat disappears, with a three-case Node regression check; it does not introduce another feature.

**Published release v0.1.1 is older than these changes.** It does not include the new automatic verification capability or downloader fixes. No new release tag was created in this QA session. The Windows build artifact for the current implementation is under:
https://github.com/MLouchini/Kopi/actions/runs/40097950576

Use that run's **Kopi-Windows** artifact for the tested implementation, or build locally from main to include the final heartbeat-loss correction. The Inno installer version string has not yet been bumped from 0.1.1; identify this QA artifact by its commit/run, not just the installer label.

Developer commands on Windows:

```powershell
git pull origin main
dotnet restore Kopi.sln
dotnet build Kopi.sln
dotnet run --project src/Kopi.Desktop
```

Development requires a .NET 10 SDK. End users do not need an SDK, Node, Python or Ollama. The CI package is self-contained and bundles app-local Visual C++ DLLs. A raw local `dotnet run` does not create that bundled native-runtime directory: use the current packaged artifact for the first end-user test, or ensure the native prerequisites are available. `scripts/package-native.ps1` uses the Visual Studio C++ redistributable directory and therefore needs that build-tool installation when run outside GitHub's Windows runner.

Load the repo's `extension` folder (or the matching folder beside packaged Kopi.exe) using browser Developer mode → Load unpacked. Reload the extension after code changes. Pair it using Setup. Re-pair after each desktop restart. Do not run a separate llama-server on port 47654 while Kopi is running its own server.

## Implementation

- `src/Kopi.Desktop`: WPF UI, recording, review/clarification, library, editable run values, setup, tray notifications and optional automatic verification.
- `src/Kopi.Core/ModelRuntime.cs`, `VerifiedDownload.cs`: pinned HTTPS downloads with SHA-256 checks, Range-based resume, safe restart when Range is ignored, one installed weight copy, native startup/readiness and shutdown cleanup.
- `model-manifest.json`: official Google Gemma 4 E2B QAT Q4_0 (about 3.35 GB), pinned llama.cpp b11852 Windows CPU runtime and hashes. CPU only, 8K context, 1800 output-token cap, temperature 0 / seed 0. Weight files are downloaded, **not in Git**.
- `BrowserBridge.cs`: authenticated, paired extension-origin Kestrel bridge on loopback 47653. The desktop model uses loopback 47654 with a per-process API key.
- `Intelligence.cs`: local-model discovery, semantic matching, extraction and clarification. Clarification now separately determines which questions remain, before rewriting the workflow, to avoid repeated confirmation loops.
- `TaskUnderstanding.cs`, `FreshCompiler.cs`: the model identifies fresh/sensitive task semantics; compilation binds observed source/destination URLs, identity labels, timestamped message selectors, request/submit controls and an independently observed success state. No university, Microsoft or WhatsApp branch exists in application code.
- `extension/fresh.js` and background coordinator: generic scoped `transferFresh` action. Establishes the existing-message baseline, requests a fresh code, reads a new unambiguous timestamped message, fills/submits, and verifies success. Codes stay in extension memory/target DOM; they are not returned to the desktop/model or persisted in workflow variables/receipts.
- Automatic execution is opt-in per approved fresh-value workflow. Exact destination path, page instance, identity guards and model matching are required. Normal workflows still require approval. Receipts prevent repeated attempts for the same challenge instance.
- Recorder and snapshot redaction retain OTP field metadata/code-length hints but exclude values. Copy/label/context channels are tested too. Redaction is heuristic, not a general PII detector.

## QA completed locally

- 19 supported core/browser/privacy tests passed on Linux; Windows-only model QA was explicitly skipped there. The managed cloud Chromium blocks unpacked extensions, so full extension tests run in Windows CI instead; its policy was not weakened.
- Three genuine local-Gemma integration tests passed: internship discovery/extraction on new layouts, generic fresh-code task learning, and learning a WhatsApp-style `data-pre-plain-text` day/month/year timestamp layout. These used real inference, not canned model responses.
- Earlier full Windows cold/warm model run `40093722552` passed using the actual .NET downloader and model startup. Runner: approximately 16 GiB RAM, AMD EPYC, 4 logical processors. One download/check took 41.3 seconds; one cache-reinstall/restart/inference pass took 79.1 seconds; measured native peak working set was about 2746 MiB. These are **not Intel-laptop benchmarks**.
- Expanded Windows browser tests cover success, stale/multiple/missing codes, wrong sender/account, already-completed and changed challenges, cancellation, and duplicate prevention.
- Final in-progress two-pass run at handoff preparation:
  https://github.com/MLouchini/Kopi/actions/runs/40097950425
  It tests cold download/load/real inference, model-learned extension execution, saved-rule persistence, warm restart with the **same saved rule**, and a second WhatsApp-style learned fixture. **Final result: failed.** Download/checksum and initial inference ran, but spreadsheet workflow clarification still asked for the tab name and `Workflow.Validate()` rejected it with "Review spreadsheet column order and tab name." The later saved-rule/browser passes were not reached; do not infer their success from earlier narrower runs.

## Exact next local acceptance test

1. Start the current app and extension in the user's normal signed-in browser. Test first with a disposable account/challenge where practical.
2. Verify app-driven model download and startup on the actual 16 GB, 11th-gen i7 laptop. Resuming/cancelling must preserve partial download progress; checksum failures must block use.
3. Observe a real challenge, including **request/resend**, opening the correct conversation, copying the code, filling and submitting it, and reaching a distinct success state.
4. Inspect the proposed sender/organisation/account identities, locators, message timestamp attribute/format and success marker. No literal code may be saved. Correct genuine missing bindings; do not clear questions or bypass assertions just to claim readiness.
5. Enable automatic execution once for this exact workflow. Open a **new challenge** and verify it uses a genuinely new message/code without asking for another approval. Try wrong sender/account and stale/ambiguous messages; they must not submit.
6. Close/reopen app, re-pair extension, and verify the same saved workflow still works without regenerating it.

## Real limitations / unverified work

- **No real WhatsApp Web, university account, Google Sheets account or personal laptop was accessed.** The messaging fixture is not the actual WhatsApp client; a live account test is the main next step.
- The current fresh transfer requires a readable numeric 4–8 digit code, usable request/resend control, an account/organisation identity and machine-readable message timestamp. Existing codes are deliberately not treated as fresh merely because they are visible.
- Inspect actual WhatsApp DOM and locale before claiming compatibility. DMY/MDY timestamp tests cover a 24-hour, four-digit-year shape. AM/PM or two-digit-year timestamp variants have not been validated. Virtualised messages, locale differences and DOM changes may require a generic parser/selector fix.
- A title/display-name match is not cryptographic sender authentication. Multiple fresh matching codes stop the run. Minute-resolution timestamps have weaker timing evidence than exact timestamps, so message identities and baseline checks remain essential.
- Cancellation prevents later submission at a cancellation check; an action already submitted can finish. Uncertain-run receipts block blind retry. After correcting a source mismatch, start a new challenge or inspect the destination before retrying.
- Download time on the laptop depends on its network. Native memory figures above exclude the rest of the OS/browser, and the cloud CPU differs from Intel hardware.
- Modern .NET/WPF is retained; do not accidentally migrate to legacy 4.8 under the assumption the app is currently a web frontend.
- GitHub Pages is enabled and live at https://mlouchini.github.io/Kopi/. Deployment `40100116315` succeeded. Installer, portable ZIP and SHA-256 links were verified publicly with HTTP 200. Downloads are explicitly labelled **v0.1.1** and do not include the newer automatic-verification work.

## Packaging after local validation

Bump installer version and release notes, rerun Windows build + deep model QA, then push a new `v*` tag to produce installer/ZIP release assets. Keep v0.1.1 immutable. Code signing and browser-store distribution remain future work.

## Final shutdown correction

Reviewing the handoff found that heartbeat network errors were ignored: closing Kopi during an active fresh-value wait could leave the extension waiting. The final commit aborts its command lease when the desktop disconnects. `node tests/browser/lease.test.cjs` passed desktop-disconnect, explicit-cancellation and active-lease cases. The standard build workflow now runs this check. The Windows runs linked above test the preceding implementation commit; the heartbeat-only correction will receive its normal push CI independently.
