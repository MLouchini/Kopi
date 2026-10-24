Kopi's Windows preview (0.1.1): demonstrate a browser task, review a locally inferred workflow, then approve execution on new inputs in your existing signed-in Chrome or Edge session.

Download **Kopi-Setup.exe** for the per-user installer, or extract **Kopi-win-x64.zip** in full before launching Kopi.exe. SHA256SUMS.txt contains package hashes. These binaries are unsigned; Windows may display SmartScreen warnings.

First run: load the included extension using Developer mode, pair it in Setup, then download the local AI (approximately 3.4 GB). The installer includes .NET and the required app-local native runtime prerequisites. No separate Ollama, Node, Python or developer tools are needed.

Includes explicit recording, local Gemma 4 E2B discovery/extraction, plain-language clarification, editable run-value review, approved same-browser execution, duplicate retry guards and optional local workflow suggestions. Initial execution primitives cover text forms and Google Sheets row appends.

Validation includes real Gemma inference on a synthetic tracker demonstration and unseen source layouts, browser recorder/action tests, real extension/bridge integration, simulated Sheets clipboard/CSV round-trip, native runtime startup and WPF navigation checks. Simulations are not real Google Sheets certification. Final verification on your actual Google account and Windows laptop remains required. Avoid concurrent spreadsheet edits, filtered tables and important production data until that test passes.

See the repository's docs/first-run.md and docs/qa.md for setup, acceptance steps and exact limitations. The detailed workflow editor uses JSON; model inference can be uncertain and must be reviewed. Closing Kopi stops its model and bridge.

0.1.1 corrects startup sizing on smaller displays; the UI smoke test now checks that the window fits within the usable screen area.
