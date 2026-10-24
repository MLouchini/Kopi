# Learned verification workflows (preview)

Kopi can learn a narrow, generic operation: request a fresh numeric value, wait for it in an identified message source, enter it in an identified verification page, and check the result. There is no branch that recognises a particular university, Microsoft sender or WhatsApp URL. The local model infers the task from a recording; the compiler binds the observed URLs, controls and identity labels.

## Teach and enable once

1. Start observing on your verification page after you have signed in normally. Passwords are not recorded or automated.
2. Demonstrate **requesting/resending a new code**, opening the correct conversation, copying the incoming code, entering it in the verification field, and completing verification.
3. Stop observing. Review the identified sender, destination organisation/account, request and submit controls, message timestamps and success state. Correct unresolved details before saving.
4. Enable **Run this scoped fresh-value workflow automatically** only for the specific learned workflow, then approve/save it once. This also enables page monitoring in Kopi. Subsequent matching challenge pages can run without another approval dialog. Ordinary workflows continue to ask before writing.

The browser and conversation must already be signed in. Re-pair the extension when restarting the desktop app. Keep Kopi running for suggestions/automatic execution.

## What happens on a later challenge

The saved destination URL and local model's semantic trigger must match. The extension binds the actual observed page instance, opens/checks the approved conversation identity, records existing message identities, requests a fresh code, and waits up to the configured limit. Only a new message within the approved source scope and request time window is eligible. Existing codes, missing timestamps, multiple candidates and mismatched accounts/senders stop execution. The code goes directly from the extension's memory to the destination field; it is not sent to Gemma, returned to the desktop, saved as a workflow variable, written in a receipt or copied into the clipboard by Kopi.

The success locator must be independent of the request/submit buttons. A visible button is not proof that login succeeded. Receipts prevent repeated attempts for the same challenge instance. Cancelling stops waiting and prevents later submissions at the next cancellation check; an action already submitted can still finish.

## Current limits that matter for your university/WhatsApp test

This is a numeric-code, DOM-based preview, not a universal authentication agent. It requires a usable request/resend control, a readable message timestamp and a visible organisation/account identity. Supported timestamp shapes include ISO, Unix timestamps and day/month or month/day message timestamps. A WhatsApp-style `data-pre-plain-text` timestamp layout is represented in tests; the actual WhatsApp Web client and your university account have not been accessed. If a site hides the needed information, changes its layout, or blocks automation, Kopi should ask for correction or stop rather than invent it.

Minute-resolution timestamps have weaker freshness evidence than exact timestamps; the existing-message baseline and message identity checks are still required. Concurrent unrelated codes from the same sender can remain ambiguous. Do not treat matching display names as cryptographic sender verification. An unsupported or uncertain recording must not be approved as if it were verified.

If source/account checks fail, correct the source and start a new challenge (or preview manually). A run that may have requested or submitted a code is not automatically retried.

## Development/runtime distinction

The application uses C# / WPF on modern .NET 10, packaged self-contained. It does not target legacy .NET Framework 4.8; that migration would require replacing ASP.NET Core and several modern runtime APIs.
