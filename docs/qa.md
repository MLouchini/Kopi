# Validation status

This file distinguishes real inference, real browser integration, simulated websites and untested personal-account behavior. Test logs and screenshots are generated as artifacts, not fabricated.

## Pass 1 — functional checks

- Shared .NET core and WPF desktop cross-compile on Linux with Windows targeting enabled.
- Unit checks exercise workflow validation, clarification blocking, invalid JSON, password redaction, evidence bounds, approval gating, duplicate and uncertain-result protection.
- Playwright Chromium drives real DOM forms using the same production browser primitives and recorder script; tests fill fields, verify success, reject missing targets and exclude password values.
- An integration test loads the actual extension, pairs it with Kestrel, observes a user input and executes a changed value in the same context. The cloud's preinstalled Chromium is managed and blocks every unpacked extension; that test is blocked locally. Windows CI uses a separately downloaded Playwright Chromium and runs this test without weakening the managed browser policy.
- Real Gemma inference tests are opt-in; see measured results below when available. Fixture-based tests are never described as model validation.

## Pass 2 — UX and regression

- WPF flow: Observe → Review/clarify → Save → Preview new values → Approve → Verified result. Input editing is a table; detailed workflow JSON remains available in an expander.
- Proposed workflows are unapproved even when model JSON claims otherwise. Name/goal edits and plain-language clarification are supported. Questions block approval. Re-selection invalidates the preview. Errors/cancellation preserve uncertain-run receipts.
- Static website checked at 1280 and 390 pixel widths for visible primary action, release link and horizontal overflow. Screenshots are emitted for visual review.
- Windows UI Automation smoke tests launch the actual published WPF app, navigate all four tabs and confirm a real pairing code; screenshot is retained. This must run on Windows and is not replaced by a Linux compilation claim.

## Essential remaining real-world tests

No personal Google account or Windows laptop was available in this task. Real Google Sheets canvas interaction, export policies, clipboard access, enterprise restrictions, Windows notification appearance and performance on the 11th-gen i7 laptop require the [first-run acceptance test](first-run.md). The simulated DOM tests do not validate those services. Concurrent spreadsheet editors and filtered/formula-filled sheets are not supported safely in this preview.

A download page and installer workflow do not establish publication. Check the repository's Actions run and Releases for the actual downloadable artifact. Windows builds are unsigned.

## Measured results (24 October 2026)

- First Windows CI run `40088874589`: **success**. Actual extension pairing/capture/same-session form execution, WPF launch/navigation, ZIP and installer production passed. This was before the final adapter-grounded inference update; the updated regression run is recorded below.
- Local regression: **11 passed**, covering core safety, recorder/browser primitives and desktop/mobile download-page layout. The managed-browser extension test was explicitly excluded locally and run on Windows instead.
- Real official Google Gemma 4 E2B QAT Q4_0 on llama.cpp b11852, CPU-only, four threads, 8K context: **one discovery + two unseen-page extractions passed**. Inference took approximately 42 seconds combined in one measured run; resident process memory was about 4.5 GiB after inference (weights, runtime and cache combined). This is a 33 GiB cloud machine, not the user's 16 GB laptop. Initial prompt evaluation was around 100 tokens/s and generation around 23 tokens/s for the first prompt; no dedicated GPU was used. A weight file of about 3.35 GB does not mean total memory is 3.35 GB.
- Initial model trials failed semantically: it invented unsupported copy/paste actions, then produced a literal source navigation and inappropriate fill. No failures were converted into passes with fixtures. Adapter-grounded schema and a shorter task-specific prompt corrected the tested case. This is narrow empirical evidence, not a claim of reliable arbitrary workflow learning.
- Both unseen layouts yielded Nova Robotics / Research Intern / Bristol, the correct new source URL and blank optional salary. No demonstrated Acme values leaked into extracted results. Structured recordings were synthetic; no Google account was used.
- GitHub Pages deployment failed because Pages is not enabled; the available GitHub integration cannot change that repository setting (HTTP 403). The static site and deployment workflow are committed. Repository owner action: Settings → Pages → Source → GitHub Actions, then rerun Download website.

- Second full Windows QA run `40090296694`: **success**. Actual extension observation and same-session form execution, simulated Sheets trusted clipboard paste and authenticated CSV read-back, native llama-server startup with bundled CRT DLLs, WPF startup/four-tab navigation, and ZIP/installer packaging all passed. The final tag build reruns these checks before publishing release assets.
- The simulated spreadsheet test initially failed because extension-created navigation preceded Playwright routing. Preloading the fixture under routing corrected the test; no assertions were disabled. The discovered timeout also prompted bounded reads and a background-worker heartbeat.
