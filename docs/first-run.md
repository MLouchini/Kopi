# First run and real-world acceptance test

1. Install Kopi or extract the full ZIP; launch Kopi.exe. Keep its extension directory in place.
2. In Chrome, open `chrome://extensions` (Edge: `edge://extensions`), enable Developer mode, choose **Load unpacked**, select the `extension` folder beside Kopi.exe. Pin the extension. This is a browser restriction; the installer does not bypass it.
3. In Kopi → Setup, copy the pairing code into the extension popup and connect. Re-pair after each desktop restart. Only one browser is paired at a time.
4. Click **Download & install local AI**. Allow about 8 GB temporary disk space. This downloads roughly 3.4 GB with visible progress and verifies pinned SHA-256 digests. Model loading/inference can take minutes on CPU. Cancellation is available.
5. Open the intended source page. In Kopi, click **Start observing**. Perform one short task in your usual browser and return to **Stop & discover**. Avoid unrelated browsing. Never demonstrate a password or payment task.
6. Review the inferred goal, variables and steps. Uncertain details appear as questions. Answer questions or give corrections in the clarification box, then apply them. An advanced JSON editor is also available for precise changes. For Sheets, confirm `sheetName` and the `columns` list in exact left-to-right order; the model cannot reliably infer them from a canvas alone. You may need to fill these in manually. Then approve and save.
7. Open a *different* source page. In Automations, choose the workflow and **Preview from current browser tab**. Review/edit new values in the input table and destination. Approve the run. Keep the browser open and do not edit its target while the run is active.
8. Check the destination yourself. Kopi reports completion only after action-specific checks pass; verify real-service results before relying on this preview.

## Internship → Google Sheets acceptance test

Use a disposable spreadsheet and dummy opportunity first. Make headers in columns A:E: Company, Role, Location, Salary, URL. Name the tab `Tracker`. Avoid other data/formulas below the tracker. Demonstrate copying a TSV row and pasting it into row 2. When reviewing the inferred workflow, ensure it has an `appendRow` action using the spreadsheet URL, tab `Tracker`, and column variables in the exact header order.

Open a different company's listing with an unfamiliar layout and no salary. Preview: company, role, location and URL must come from the new page, salary should be blank. Approve. A browser debugger banner appears during Sheets keyboard input; the clipboard is replaced with the reviewed row. The adapter locates the Sheets name box, jumps to the next row based on an authenticated CSV export, pastes TSV, then re-exports to check the complete row. Read access and edit access are both required. Export may be unavailable for some enterprise policies. The first five columns must be the configured tracker columns; append is not a general spreadsheet editing engine.

Repeat the exact run: Kopi must block a duplicate attempt. Try another listing with missing location: it should ask for correction if required. Change the page layout: verify extraction again. An ambiguous or failed run must not be presented as success.

If a run times out or is cancelled, a browser action already sent may finish. Inspect the destination before doing anything else. Duplicate-prevention receipts intentionally block automatic retries after uncertain results. There is no automatic repair/retry for destructive actions. In this preview, receipts can only be cleared manually in the local data folder after verifying the destination.

## Optional suggestions

Enable the explicit active-page suggestion checkbox. It sends condensed page context to the local model when a new page is seen. Suggestions do not write anything: open Kopi, preview and approve. Suggestions are evaluated once per page URL per desktop session. Dynamic page changes at the same URL may require manual preview.

## Current UX constraints

Extension loading/pairing is manual. Detailed workflow edits and value corrections use JSON. This preview has a tray icon and suggestion notifications, but is not registered to start at login. Closing Kopi stops the local model. No cloud fallback exists.
