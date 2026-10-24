# Privacy and permissions

Kopi records only while explicitly enabled. Optional suggestions separately permit condensed active-page snapshots. Page text can contain personal data; observe only pages you intend to process. No browser history scraping or cloud-model fallback.

The extension has broad website access because demonstrations cross websites. It does not read browser cookie databases. Execution uses your browser's existing session; normal requests to destination sites still reach those services. Password/payment/OTP fields are excluded by type and common name/autocomplete patterns; URLs lose query strings/fragments in evidence to reduce token leakage. This may remove task-relevant query parameters and is not comprehensive redaction. User-visible text and copied strings may still contain sensitive information.

Recordings are held in desktop memory, capped at 200 events, and cleared after successful discovery. They are lost on restart. Workflows and run receipts are local under `%LOCALAPPDATA%\Kopi`, protected by the Windows account's normal file permissions, not additionally encrypted. Workflows can contain destination URLs and constants. Don't put secrets in workflow definitions. The model never receives browser cookies or raw credentials.

Pairing authorizes an extension origin and uses a random bearer token. Each desktop launch requires re-pairing. Chrome's debugger permission is requested visibly for Sheets input and detached afterwards. Sheets entry changes clipboard contents. No third-party analytics or telemetry are implemented in Kopi; .NET development SDK telemetry is independent of the shipped app.

First-run downloads contact GitHub and Hugging Face over verified HTTPS with pinned SHA-256 checksums. Models run locally. Do not disable TLS or checksum verification to work around downloads. Remove extension access in the browser to revoke it; close Kopi to stop its bridge/model. Use Setup → Open local data folder to inspect or remove saved data after closing the app.

For explicitly enabled fresh-value workflows, the extension can read a new code from the approved conversation and submit it to the approved challenge without per-run approval. Teaching evidence omits the code; runtime codes remain inside the extension and destination DOM, and are not sent to Gemma or stored in workflow files/receipts. Sender/account identity and timestamp checks limit the saved rule. See verification-workflows.md for limits and cancellation behaviour.
