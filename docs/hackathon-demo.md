# Hackathon demo: real email to Google Sheets

Use the actual Gmail or Outlook browser session you already use. The user chose real email instead of the optional demo website.

1. Create a Google Sheet, name its tab **Leads**, and put these headers in A1:D1: **Name | Email | Request | Budget**. Keep rows below empty.
2. Send yourself the first sample email below. Open that message in your browser and pair the Kopi extension.
3. In Kopi, start recording. Copy the lead details into A2:D2 of the sheet, in header order. Return to Kopi and choose **Stop & discover**.
4. Review the proposed workflow. Confirm the spreadsheet URL, tab **Leads**, and column variables **name, email, request, budget** in exactly that order. It should append a row, rather than replay canvas clicks. Answer genuine clarification questions.
5. Approve and save. Open a second message containing different lead details, select the workflow and preview it. Confirm the values came from the second message, approve, then show the new sheet row.

Start the local model before presenting and rehearse once. Gmail/Outlook-to-Sheets has not been tested against the user's actual account in this session. The expanded model QA recorded a spreadsheet clarification failure; do not promise that learning will need no corrections. A previously rehearsed and reviewed workflow is a reasonable live-demo fallback if you disclose it.

## First email

Subject: New enquiry — website refresh

Hi, I'm Maya Chen. I'd like a landing page redesign for our studio. The budget is £2,400. You can reach me at maya@aster.example.

Thanks,
Maya

## Second email

Subject: Could you help with our launch?

Hello, my name is Sam Rivera. We need a product launch microsite and have £4,800 available. Please contact me at sam@fieldnote.example.

Best,
Sam

## Presentation

“I show Kopi one example of moving a lead from my email into my tracker. This is my normal browser, and the model runs locally. Now I open a different email. Kopi extracts its new details, I review them, and it adds the next row.”

Google Sheets uses the clipboard and a visible browser debugger banner. Check the resulting row before presenting it as successful. Use a new message for the second run; uncertain or duplicate attempts can be blocked by receipts.
