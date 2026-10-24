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
