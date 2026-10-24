# Hackathon demonstration: enquiry → lead inbox

Use the fictional demo workspace at [the hosted demo](https://mlouchini.github.io/Kopi/demo/) or, while the local preview is running, [localhost](http://127.0.0.1:4173/demo/). No messaging service, email access, login or spreadsheet permission is needed.

## Prepare

1. Open the demo guide, enquiry A and lead inbox in your usual browser. Load and pair Kopi's extension. Start the local model before presenting.
2. Clear the inbox's demo rows. Make a fresh saved workflow for each rehearsal: clearing website rows does not clear Kopi's separate duplicate-prevention receipts.
3. Rehearse learning, review and execution before going on stage. Local inference time and clarification questions can vary. The demo pages working does not establish that a model-learned run has passed.

## Show the demonstration

1. On **Enquiry A**, start observing in Kopi.
2. Select and copy the four labelled values with ordinary Ctrl+C, then paste each into the corresponding inbox field:
   - Contact name: Maya Chen
   - Email: maya@northstar.example
   - Project: Website refresh
   - Budget: £2,400
3. Click **Save lead**. Let Kopi observe the green **Lead saved** state and the new row.
4. Stop and discover. Review the proposed workflow. It should use variables for the four values, navigate to this inbox, fill its observed fields, click its save button and check the observed success marker. Answer genuine questions; do not clear them just to make the demonstration pass.
5. Approve and save.
6. Switch to **Enquiry B**. It has a different design and field order. Preview the saved workflow. Confirm the new values are **Sam Rivera**, **sam@fieldnote.example**, **Product launch microsite**, **£4,800**.
7. Approve the run, then show both rows in the inbox.

The app must infer these mappings from the demonstration. The pages do not configure Kopi, pre-load a workflow, or return canned model answers.

## A short presentation

“I show Kopi one complete browser task: taking an enquiry and adding it to my lead inbox. It runs in the browser I'm already using, and the model stays on my computer.

“Now here's a different enquiry. The layout and order are different. Kopi extracts the new details. I can inspect and correct them before it does anything.

“I approve, and the second lead is added. It's learned a reusable task from my demonstration.”

Allow around 90 seconds of narration, plus the real inference time measured during your rehearsal. Do not promise a fixed runtime. If the model needs clarification, explain that review rather than hiding it.

## Honest fallback

If learning needs further correction, show the recorded demonstration and editable proposed workflow, then explain what remains. You can save a previously rehearsed workflow for the live run, but disclose that it was learned and reviewed before presenting. Do not present a manual form submission as an autonomous run.

## Behaviour and privacy

All examples are fictional. Leads are stored only in this browser's local storage on the page's origin; no external form submission, email or message is sent. The destination uses normal labelled text/email inputs and visible success state. Source B deliberately differs from source A. These are generic browser fixtures, not a lead-specific path in application code.
