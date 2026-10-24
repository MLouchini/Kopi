"""Build Kopi's editable hackathon pitch with standard-library OOXML.

Run from any directory: py docs/presentation/build_deck.py
No model weights, secrets, or third-party presentation packages are required.
"""

from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from xml.sax.saxutils import escape
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / 'Kopi-Hackathon-Pitch.pptx'
PAPER, INK, ORANGE, PEACH, MUTED, LINE = 'FFFEFA', '201D1D', 'FF7A1A', 'FFDDBE', '776B62', 'E7DDD3'
FONT = 'Segoe UI'
NS = 'xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"'
REL = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/'
REPO = 'https://github.com/MLouchini/Kopi'
MODEL = 'https://huggingface.co/google/gemma-4-E2B-it-qat-q4_0-gguf'


def emu(px):
    return str(round(px * 9525))


def color(value):
    return f'<a:solidFill><a:srgbClr val="{value}"/></a:solidFill>' if value else '<a:noFill/>'


def xfrm(x, y, w, h):
    return f'<a:xfrm><a:off x="{emu(x)}" y="{emu(y)}"/><a:ext cx="{emu(w)}" cy="{emu(h)}"/></a:xfrm>'


def paragraph(text, size=24, fill=INK, bold=False, align='l'):
    # Geometry is in 96-DPI pixels. DrawingML font size uses hundredths of a point.
    props = f'lang="en-GB" sz="{round(size * 75)}" b="{int(bold)}"'
    return (f'<a:p><a:pPr algn="{align}"><a:lnSpc><a:spcPct val="110000"/></a:lnSpc></a:pPr>'
            f'<a:r><a:rPr {props}>{color(fill)}<a:latin typeface="{FONT}"/><a:ea typeface="{FONT}"/><a:cs typeface="{FONT}"/></a:rPr>'
            f'<a:t xml:space="preserve">{escape(text)}</a:t></a:r><a:endParaRPr {props}/></a:p>')


class Slide:
    def __init__(self, title, notes, bg=PAPER):
        self.title, self.notes, self.bg = title, notes, bg
        self.hidden = False
        self.items, self.images, self.counter = [], [], 1

    def shape(self, x, y, w, h, fill=None, stroke=None, geom='rect', width=1):
        self.counter += 1
        i = self.counter
        self.items.append(f'<p:sp><p:nvSpPr><p:cNvPr id="{i}" name="Shape {i}"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>{xfrm(x,y,w,h)}<a:prstGeom prst="{geom}"><a:avLst/></a:prstGeom>{color(fill)}<a:ln w="{emu(width)}">{color(stroke)}</a:ln></p:spPr></p:sp>')

    def text(self, text, x, y, w, h, size=24, fill=INK, bold=False, align='l'):
        self.counter += 1
        i = self.counter
        paras = ''.join(paragraph(p, size, fill, bold, align) for p in text.split('\n'))
        self.items.append(f'<p:sp><p:nvSpPr><p:cNvPr id="{i}" name="{escape(text[:55], {chr(34): "&quot;"})}"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr><p:spPr>{xfrm(x,y,w,h)}<a:prstGeom prst="rect"><a:avLst/></a:prstGeom><a:noFill/><a:ln><a:noFill/></a:ln></p:spPr><p:txBody><a:bodyPr wrap="square" lIns="0" tIns="0" rIns="0" bIns="0" anchor="t"/><a:lstStyle/>{paras}</p:txBody></p:sp>')

    def image(self, name, x, y, w, h):
        self.counter += 1
        i = self.counter
        rid = f'rId{len(self.images)+3}'
        self.images.append((rid, name))
        self.items.append(f'<p:pic><p:nvPicPr><p:cNvPr id="{i}" name="Kopi brand artwork" descr="Original Kopi brand asset: {name}"/><p:cNvPicPr><a:picLocks noChangeAspect="1"/></p:cNvPicPr><p:nvPr/></p:nvPicPr><p:blipFill><a:blip r:embed="{rid}"/><a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr>{xfrm(x,y,w,h)}<a:prstGeom prst="rect"><a:avLst/></a:prstGeom></p:spPr></p:pic>')

    def heading(self, title, number, appendix=False):
        self.hidden = appendix
        self.text('Kopi' + ('  /  Appendix' if appendix else ''), 64, 30, 600, 24, 17, MUTED)
        self.text(str(number), 1160, 30, 56, 24, 17, MUTED, align='r')
        self.text(title, 64, 92, 1152, 122, 48, INK, True)

    def rule(self, x, y, w=1152, fill=LINE):
        self.shape(x, y, w, 1.3, fill)

    def table(self, rows, widths, x, y, row_heights):
        self.counter += 1
        i = self.counter
        grid = ''.join(f'<a:gridCol w="{emu(v)}"/>' for v in widths)
        body = ''
        for n, (row, height) in enumerate(zip(rows, row_heights)):
            cells = ''
            for j, txt in enumerate(row):
                bg = INK if n == 0 else (PEACH if j == 1 else PAPER)
                fg = PAPER if n == 0 else INK
                cells += f'<a:tc><a:txBody><a:bodyPr lIns="{emu(16)}" rIns="{emu(16)}" tIns="{emu(16)}" bIns="{emu(10)}"/><a:lstStyle/>{paragraph(txt, 22 if j != 2 else 21, fg, n == 0 or j == 1)}</a:txBody><a:tcPr>{color(bg)}<a:lnB w="9525">{color(LINE)}</a:lnB></a:tcPr></a:tc>'
            body += f'<a:tr h="{emu(height)}">{cells}</a:tr>'
        self.items.append(f'<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id="{i}" name="Judging criteria"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr><p:xfrm><a:off x="{emu(x)}" y="{emu(y)}"/><a:ext cx="{emu(sum(widths))}" cy="{emu(sum(row_heights))}"/></p:xfrm><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/table"><a:tbl><a:tblPr firstRow="1" bandRow="0"/><a:tblGrid>{grid}</a:tblGrid>{body}</a:tbl></a:graphicData></a:graphic></p:graphicFrame>')

    def xml(self):
        group = '<p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>'
        return f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><p:sld {NS} show="{0 if self.hidden else 1}"><p:cSld name="{escape(self.title, {chr(34): "&quot;"})}"><p:bg><p:bgPr>{color(self.bg)}<a:effectLst/></p:bgPr></p:bg><p:spTree>{group}{"".join(self.items)}</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>'


def notes(timing, script, sources):
    return f'{timing}\n\n{script}\n\nSources\n' + '\n'.join(sources)


def build():
    slides = []
    def new(title, timing, script, sources, bg=PAPER):
        s = Slide(title, notes(timing, script, sources), bg)
        slides.append(s)
        return s

    s = new('Kopi', '00:00–00:15 / 15 seconds',
        'Kopi learns a browser routine from a demonstration, then helps repeat it with new inputs in the browser you already use. The local Gemma model supplies the understanding. You review what it learns.',
        [REPO, REPO+'/blob/main/README.md', 'Brand: site/style.css and site/assets. Segoe UI is the site’s installed fallback font.'])
    s.image('kopi-wordmark.png', 53, 35, 256, 96)
    s.shape(64, 344, 465, 59, PEACH)
    s.text('Show it once.\nLet Kopi repeat.', 64, 235, 700, 205, 76, INK, True)
    s.text('Browser routines, learned locally', 68, 472, 620, 44, 28, MUTED)
    s.text('Your existing session. Your reusable workflow.', 68, 531, 650, 36, 23, MUTED)
    s.image('kopi-logo.png', 740, 159, 490, 490)
    s.text('github.com/MLouchini/Kopi', 68, 652, 650, 28, 18, MUTED)

    s = new('The same small tasks keep coming back', '00:15–00:40 / 25 seconds',
        'A lot of browser work is small but repetitive: find the right page, transfer information and complete the same sequence again. Traditional scripts need someone to encode those steps. Our goal is to let the user teach a supported task by doing it. This is a problem statement, not a measured time-saving claim.',
        [REPO+'/blob/main/README.md'])
    s.heading('The same small tasks\nkeep coming back', 2)
    items = [('01', 'Find the context', 'The right tab, conversation\nor record.'), ('02', 'Move the information', 'Read a value, switch pages\nand enter it again.'), ('03', 'Repeat the sequence', 'The process stays familiar.\nThe details change.')]
    for x, (num, title, body) in zip([64, 461, 858], items):
        s.rule(x, 285, 350)
        s.text(num, x, 308, 350, 82, 65, ORANGE)
        s.text(title, x, 414, 350, 45, 29, INK, True)
        s.text(body, x, 482, 350, 110, 25, MUTED)
    s.text('A useful shortcut starts with a task you already know how to do.', 64, 638, 1152, 40, 25)

    s = new('A demonstration becomes a reusable workflow', '00:40–01:10 / 30 seconds',
        'The user opts into recording and completes a task. Gemma proposes a structured workflow, separates changing inputs from fixed steps, and asks about missing details. The user reviews and approves it. A later run uses fresh values. Normal runs require approval. A supported fresh-value workflow can run automatically after the user explicitly enables it once.',
        [REPO+'/blob/main/HANDOFF.md', REPO+'/blob/main/docs/verification-workflows.md'])
    s.heading('A demonstration becomes\na reusable workflow', 3)
    for x, num, title, body in [(64,'01','Observe','Demonstrate a complete task\nin your signed-in browser.'),(461,'02','Understand','Gemma proposes inputs and\nsteps. You resolve uncertainty.'),(858,'03','Reuse','Review the workflow, then\nrun it with new details.')]:
        s.shape(x, 285, 56, 56, PEACH, geom='ellipse')
        s.text(num, x, 296, 56, 36, 26, INK, True, 'ctr')
        s.text(title, x, 376, 350, 50, 35, INK, True)
        s.text(body, x, 450, 350, 120, 25, MUTED)
    s.rule(64, 595)
    s.text('Normal runs need approval. Automatic verification needs one explicit enablement.', 64, 625, 1152, 55, 23)

    s = new('Demo: transferring a fresh verification code', '01:10–01:55 / 45 seconds',
        'Use the tested local browser fixture for the demo. Start with a saved workflow and a new challenge. Show the source and destination identities, request a new code, then show a new matching timestamped message, submission and an independent success marker. Show stale or ambiguous messages stopping a run if time permits. Codes remain in extension memory and the destination DOM. They are excluded from model evidence, saved variables and receipts. Actual WhatsApp Web and university accounts remain unvalidated, so do not narrate this storyboard as a live-account result. This slide is an editable process illustration, not an application screenshot.',
        [REPO+'/blob/main/HANDOFF.md', REPO+'/blob/main/extension/fresh.js', REPO+'/blob/main/tests/Kopi.Tests/BrowserTests.cs'])
    s.heading('Demo: a fresh code,\na familiar browser', 4)
    s.text('Target scenario: university sign-in and a messaging conversation', 64, 222, 1152, 42, 24, MUTED)
    for x, num, title, body in [(64,'1','New challenge','Confirm the destination\nand account identity.'),(362,'2','Fresh message','Request a code and find\na new matching message.'),(660,'3','Transfer','Fill and submit the\ntransient value.'),(958,'4','Verified result','Check a distinct\nsuccess state.')]:
        s.shape(x, 308, 260, 7, ORANGE)
        s.text(num, x, 345, 260, 86, 68, ORANGE)
        s.text(title, x, 453, 260, 44, 27, INK, True)
        s.text(body, x, 515, 260, 100, 22, MUTED)
    s.text('Browser-fixture demo. Real WhatsApp and university validation remains next.', 64, 653, 1152, 31, 21, MUTED)

    s = new('A local model proposes. Guarded actions execute.', '01:55–02:45 / 50 seconds',
        'The browser extension is the production adapter because it preserves the user’s existing signed-in Chrome or Edge session. It sends bounded evidence through an authenticated loopback bridge to the .NET desktop. Local Gemma through llama.cpp handles task discovery, clarification, extraction and semantic matching. Typed validation and a compiler bind the proposal to observed controls, identities, timestamps and success markers. Execution uses fixed browser primitives, not arbitrary code generated by the model. Actual verification codes stay in the extension and target DOM. There are no cloud inference calls. Browser websites still need their usual network access.',
        [REPO+'/blob/main/docs/architecture.md', REPO+'/blob/main/src/Kopi.Core/FreshCompiler.cs', REPO+'/blob/main/model-manifest.json'])
    s.heading('Local understanding\nGuarded browser actions', 5)
    boxes = [(64,275,338,156,PEACH,'Signed-in browser','Extension records evidence\nand performs fixed actions.'),(471,275,338,156,'F4EDE5','Kopi desktop','C# / .NET 10 / WPF\nReview, compile and validate.'),(878,275,338,156,INK,'Gemma 4 E2B','QAT Q4_0 with llama.cpp\nCPU inference on this device.')]
    for x,y,w,h,bg,ti,bo in boxes:
        s.shape(x,y,w,h,bg)
        fg = PAPER if bg == INK else INK
        s.text(ti,x+22,y+22,w-44,45,27,fg,True)
        s.text(bo,x+22,y+82,w-44,67,21,fg)
    s.shape(414, 349, 45, 3, ORANGE)
    s.shape(821, 349, 45, 3, ORANGE)
    s.text('Authenticated loopback bridge', 64, 453, 745, 30, 21, MUTED)
    s.rule(64, 514)
    s.text('Execution checks', 64, 548, 275, 40, 29, INK, True)
    s.text('Observed targets. Identity guards. Freshness checks. Success evidence.', 365, 549, 851, 76, 25)
    s.text('Verification codes stay in extension memory and the target page.', 64, 651, 1152, 32, 22, MUTED)

    s = new('The technical work around the model', '02:45–03:30 / 45 seconds',
        'Our original work is the harness around the model. Discovery and fresh-task semantics compile into executable bindings grounded in a demonstration. The fresh-transfer coordinator establishes a baseline, requests a new value and rejects stale or ambiguous messages. Receipts block blind duplicate retries after an uncertain result. The runtime downloads pinned weights and native binaries with hash verification, resumes partial downloads and handles startup and shutdown. We built these integrations around Gemma and llama.cpp. We did not train the foundation model.',
        [REPO+'/blob/main/src/Kopi.Core/FreshCompiler.cs', REPO+'/blob/main/src/Kopi.Core/VerifiedDownload.cs', REPO+'/blob/main/extension/fresh.js', REPO+'/blob/main/HANDOFF.md'])
    s.heading('What we built around Gemma', 6)
    for x,y,num,ti,bo in [(64,250,'01','Grounded compilation','Turn a demonstration into typed steps,\nobserved targets and blocking questions.'),(661,250,'02','Fresh-value coordination','Baseline messages, request a new value\nand reject stale or ambiguous results.'),(64,462,'03','Conservative recovery','Track uncertain attempts, stop cancelled\nwork and prevent blind duplicate retries.'),(661,462,'04','Verified local runtime','Pin and hash-check downloads, resume\npartials and manage native startup.')]:
        s.rule(x,y,555)
        s.text(num,x,y+21,65,45,30,ORANGE,True)
        s.text(ti,x+78,y+21,477,45,29,INK,True)
        s.text(bo,x+78,y+87,477,108,23,MUTED)

    s = new('Evidence and the next validation step', '03:30–04:10 / 40 seconds',
        'The handoff records 19 supported core, browser and privacy tests and three real local-Gemma integration tests. These are different suites and should not be added into a universal success rate. Windows build and QA has passed. The expanded two-pass saved-rule and WhatsApp-style model run 40097950425 completed with failure, checked on 24 October 2026 while preparing this presentation. Earlier successful native and browser tests do not establish that this expanded pipeline passes. Real WhatsApp, university and Google Sheets accounts and the personal laptop still need acceptance testing. No adoption or time-saving data is claimed.',
        [REPO+'/blob/main/HANDOFF.md', REPO+'/blob/main/docs/qa.md', REPO+'/actions/runs/40102091450', REPO+'/actions/runs/40097950425'])
    s.heading('Tested paths. Visible limits', 7)
    s.text('19',64,253,270,114,93,ORANGE,True)
    s.text('Core, browser and\nprivacy checks passed',64,382,450,84,28,INK,True)
    s.text('3',664,253,270,114,93,ORANGE,True)
    s.text('Real local-Gemma\nintegration tests passed',664,382,500,84,28,INK,True)
    s.text('Reported in the implementation handoff',64,487,1152,30,20,MUTED)
    s.rule(64,539)
    s.text('Still to prove',64,571,240,44,28,INK,True)
    s.text('Expanded saved-rule model QA failed.\nLive accounts and laptop performance need validation.',345,568,871,86,25)

    s = new('Less setup for routines you already know', '04:10–04:40 / 30 seconds',
        'Kopi targets people repeating browser tasks who do not want to write a script or move into a new browser profile. The same signed-in session reduces setup friction. Local inference avoids sending browser evidence to a cloud AI service. A CPU runtime broadens the hardware target, though the initial download is about 3.4 GB and laptop performance is still unmeasured. Our creative contribution is combining demonstration-based learning, a small open-weight model and constrained execution. Potential use cases include supported form transfers and structured record capture. We have not measured time saved or real-world adoption.',
        [REPO+'/blob/main/README.md', REPO+'/blob/main/docs/architecture.md', REPO+'/blob/main/model-manifest.json'])
    s.heading('Less setup for routines\nyou already know',8)
    for y,ti,bo in [(274,'Your existing session','Chrome or Edge keeps the sign-ins you already have.'),(395,'Local understanding','Local inference keeps browser evidence on your computer.'),(516,'Reusable learning','Demonstrate a supported task, review it and change the inputs.')]:
        s.text(ti,64,y,390,70,32,INK,True)
        s.text(bo,495,y+6,721,83,26,MUTED)
        s.rule(64,y+97)
    s.text('Designed to reduce repetitive work. User impact is still to be measured.',64,652,1152,30,21,MUTED)

    s = new('Kopi: show it once, let Kopi repeat', '04:40–05:00 / 20 seconds',
        'Kopi turns a demonstrated browser task into a reviewed, reusable workflow using local open-weight AI. The repository contains the desktop, model harness, extension, tests and packaging. Our next milestone is a complete acceptance run on the real laptop and live services, followed by resolving the expanded QA failure. Invite the judges to inspect the repo or see the fixture demo. Stop the five-minute pitch here. Slides 10–12 are backup slides, not part of the timed talk.',
        [REPO, REPO+'/blob/main/HANDOFF.md'], PEACH)
    s.image('kopi-wordmark.png',53,35,288,108)
    s.text('Show it once.\nLet Kopi repeat.',64,228,800,200,76,INK,True)
    s.text('Local AI for the browser\nyou already use.',68,469,730,100,32)
    s.text('github.com/MLouchini/Kopi',68,633,900,44,28,INK,True)
    s.image('kopi-loop.png',910,275,285,285)

    s = new('How the pitch maps to the judging criteria', 'Appendix / untimed',
        'The weights and judging process come from the event brief supplied in chat. Implementation receives 30%, creativity 25%, impact 25% and technical depth 20%. Explain what Kopi does and show evidence without claiming an unverified score. Round one uses videos and determines the top three finalists and MLH prizes. The live five-minute final decides first, second and third for Best Overall Hack using the same criteria. Four judges assess the entries.',
        ['User-supplied event rules and judging rubric, 24 October 2026.'])
    s.heading('The judging case for Kopi',10,True)
    s.table([
        ['Criterion','Weight','Evidence to show'],
        ['Implementation','30%','Desktop, extension, recorded task, fresh run and verified outcome'],
        ['Creativity','25%','Demonstration learning with local AI in an existing browser session'],
        ['Impact','25%','A clear repetitive-work problem and a realistic user scenario'],
        ['Technical depth','20%','Typed harness, grounded bindings, local runtime and failure handling'],
    ],[285,140,727],64,232,[66,84,84,84,84])

    s = new('Prize requirements and current fit', 'Appendix / untimed',
        'Best Overall Hack is open to every project, subject to event rules. Best Open-Source AI Project is the closest technical fit: open-weight AI is essential and the original model harness provides meaningful implementation. Public source alone is insufficient. No project LICENSE was present in the inspected checkout. Add an open-source project license and explicit model/license links in the README before asserting eligibility. The Google model card labels this checkpoint Apache-2.0. Cite and retain the applicable upstream license. This is a model-harness entry, not an Agent Skills submission, so the Agent Skills standard applies only if a separate skill is entered. Best Use of Gemma 4 in the supplied brief specifically requires the Gemini API. Current local llama.cpp inference does not satisfy that stated API condition. No API integration is claimed or added for this deck.',
        ['User-supplied prize requirements.', REPO, MODEL, MODEL+'/blob/main/LICENSE', 'https://agentskills.io/'])
    s.heading('Prize fit and eligibility gaps',11,True)
    for y, label, status, detail, fg in [(245,'Best Overall Hack','OPEN CATEGORY','Show the working project and confirm event-rule compliance.',INK),(373,'Best Open-Source AI','STRONG FIT, LICENSE GAP','Public repo and original harness. Add a project license and model links.',INK),(501,'Best Use of Gemma 4','API REQUIREMENT UNMET','The brief requires Gemini API use. Kopi currently runs Gemma locally.',INK)]:
        s.rule(64,y-14)
        s.text(label,64,y+6,435,45,28,fg,True)
        s.text(status,525,y+2,691,35,20,ORANGE,True)
        s.text(detail,525,y+44,691,76,23,MUTED)
    s.text('Model-harness entry. Agent Skills compliance applies if submitting a separate skill.',64,652,1152,33,20,MUTED)

    s = new('Submission checklist and technologies', 'Appendix / untimed',
        'Technologies used: C#, modern .NET 10, WPF, ASP.NET Core/Kestrel, JavaScript Chrome/Edge extension APIs, Google Gemma 4 E2B IT QAT Q4_0 GGUF, llama.cpp b11852 CPU, Microsoft.Playwright for integration testing, xUnit, GitHub Actions and Inno Setup. List any AI development tools actually used, including Codex for this presentation, in the final submission. Keep the local inference product distinct from development tooling. Model card: '+MODEL+'. Upstream license: '+MODEL+'/blob/main/LICENSE. Event checklist: at most four team members, each registered and checked in, bring laptop/charger/GitHub/student ID, confirm all project work happened during the event after 10:00, follow MLH Code of Conduct, and stay until finalists at 16:15. Top three must present live for five minutes. MLH awards are separate from CS Soc prizes. Organisers have the final decision. No video length was supplied, so none is invented. Ensure the video shows a genuine working demo and explains original work, model, dependencies and repository.',
        ['User-supplied event rules.', REPO+'/blob/main/src/Kopi.Desktop/Kopi.Desktop.csproj', REPO+'/blob/main/tests/Kopi.Tests/Kopi.Tests.csproj', REPO+'/blob/main/model-manifest.json', MODEL, MODEL+'/blob/main/LICENSE', 'https://mlh.io/code-of-conduct'])
    s.heading('Submission readiness',12,True)
    s.text('Technologies used',64,241,500,44,31,INK,True)
    s.text('C# / .NET 10 / WPF\nJavaScript browser extension\nKestrel loopback bridge\nGemma 4 E2B / llama.cpp CPU\nPlaywright / xUnit\nGitHub Actions / Inno Setup',64,309,520,264,25,MUTED)
    s.text('Before submitting',661,241,555,44,31,INK,True)
    s.text('Add the project license and model links.\nShow a genuine working demo in the video.\nConfirm event-only work after 10:00.\nRegister and check in every member, up to 4.\nStay for finalists at 16:15.\nPrepare the five-minute live final.',661,309,555,282,24,MUTED)
    s.rule(64,613)
    s.text('Bring laptop, charger, GitHub and student ID. Follow the MLH Code of Conduct.',64,642,1152,35,21,MUTED)
    return slides


def relationships(items):
    return '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' + ''.join(f'<Relationship Id="{rid}" Type="{REL}{kind}" Target="{target}"/>' for rid,kind,target in items) + '</Relationships>'


def package(slides, path):
    group = '<p:nvGrpSpPr><p:cNvPr id="1" name=""/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/><a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm></p:grpSpPr>'
    cmap = '<p:clrMap accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" bg1="lt1" bg2="lt2" folHlink="folHlink" hlink="hlink" tx1="dk1" tx2="dk2"/>'
    files = {}
    files['_rels/.rels'] = relationships([('rId1','officeDocument','ppt/presentation.xml'),('rId2','metadata/core-properties','docProps/core.xml'),('rId3','extended-properties','docProps/app.xml')]).replace(REL+'metadata/core-properties','http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties')
    files['docProps/core.xml'] = '<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>Kopi — Hackathon Pitch</dc:title><dc:creator>Kopi</dc:creator><dc:subject>Local AI browser workflow learner</dc:subject></cp:coreProperties>'
    files['docProps/app.xml'] = f'<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"><Application>Kopi</Application><PresentationFormat>On-screen Show (16:9)</PresentationFormat><Slides>{len(slides)}</Slides><Notes>{len(slides)}</Notes></Properties>'
    ids = ''.join(f'<p:sldId id="{255+i}" r:id="rId{i+2}"/>' for i in range(1,len(slides)+1))
    files['ppt/presentation.xml'] = f'<p:presentation {NS}><p:sldMasterIdLst><p:sldMasterId id="2147483648" r:id="rId1"/></p:sldMasterIdLst><p:notesMasterIdLst><p:notesMasterId r:id="rId2"/></p:notesMasterIdLst><p:sldIdLst>{ids}</p:sldIdLst><p:sldSz cx="12192000" cy="6858000" type="screen16x9"/><p:notesSz cx="6858000" cy="9144000"/><p:defaultTextStyle><a:defPPr><a:defRPr lang="en-GB"><a:latin typeface="{FONT}"/></a:defRPr></a:defPPr></p:defaultTextStyle></p:presentation>'
    files['ppt/_rels/presentation.xml.rels'] = relationships([('rId1','slideMaster','slideMasters/slideMaster1.xml'),('rId2','notesMaster','notesMasters/notesMaster1.xml')]+[(f'rId{i+2}','slide',f'slides/slide{i}.xml') for i in range(1,len(slides)+1)])
    files['ppt/slideMasters/slideMaster1.xml'] = f'<p:sldMaster {NS}><p:cSld><p:spTree>{group}</p:spTree></p:cSld>{cmap}<p:sldLayoutIdLst><p:sldLayoutId id="2147483649" r:id="rId1"/></p:sldLayoutIdLst><p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles></p:sldMaster>'
    files['ppt/slideMasters/_rels/slideMaster1.xml.rels'] = relationships([('rId1','slideLayout','../slideLayouts/slideLayout1.xml'),('rId2','theme','../theme/theme1.xml')])
    files['ppt/slideLayouts/slideLayout1.xml'] = f'<p:sldLayout {NS} type="blank" preserve="1"><p:cSld name="Blank"><p:spTree>{group}</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>'
    files['ppt/slideLayouts/_rels/slideLayout1.xml.rels'] = relationships([('rId1','slideMaster','../slideMasters/slideMaster1.xml')])
    files['ppt/notesMasters/notesMaster1.xml'] = f'<p:notesMaster {NS}><p:cSld><p:spTree>{group}</p:spTree></p:cSld>{cmap}<p:notesStyle/></p:notesMaster>'
    files['ppt/notesMasters/_rels/notesMaster1.xml.rels'] = relationships([('rId1','theme','../theme/theme1.xml')])
    cols = [('dk1',INK),('lt1',PAPER),('dk2',MUTED),('lt2',PEACH),('accent1',ORANGE),('accent2',PEACH),('accent3',INK),('accent4','BD6933'),('accent5','F4EDE5'),('accent6',MUTED),('hlink','B24E00'),('folHlink','776B62')]
    scheme=''.join(f'<a:{name}><a:srgbClr val="{val}"/></a:{name}>' for name,val in cols)
    files['ppt/theme/theme1.xml'] = f'<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Kopi"><a:themeElements><a:clrScheme name="Kopi">{scheme}</a:clrScheme><a:fontScheme name="Kopi"><a:majorFont><a:latin typeface="{FONT}"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont><a:minorFont><a:latin typeface="{FONT}"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont></a:fontScheme><a:fmtScheme name="Kopi"><a:fillStyleLst>{color(PAPER)}{color(ORANGE)}{color(PEACH)}</a:fillStyleLst><a:lnStyleLst>'+''.join(f'<a:ln w="{w}">{color(INK)}<a:prstDash val="solid"/></a:ln>' for w in [9525,19050,28575])+'</a:lnStyleLst><a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst><a:bgFillStyleLst>'+color(PAPER)*3+'</a:bgFillStyleLst></a:fmtScheme></a:themeElements></a:theme>'
    for n,s in enumerate(slides,1):
        files[f'ppt/slides/slide{n}.xml'] = s.xml()
        files[f'ppt/slides/_rels/slide{n}.xml.rels'] = relationships([('rId1','slideLayout','../slideLayouts/slideLayout1.xml'),('rId2','notesSlide',f'../notesSlides/notesSlide{n}.xml')]+[(rid,'image',f'../media/{name}') for rid,name in s.images])
        paras=''.join(paragraph(line,16) for line in s.notes.split('\n'))
        body=f'<p:sp><p:nvSpPr><p:cNvPr id="2" name="Speaker notes"/><p:cNvSpPr txBox="1"/><p:nvPr><p:ph type="body" idx="1"/></p:nvPr></p:nvSpPr><p:spPr>{xfrm(48,100,624,780)}</p:spPr><p:txBody><a:bodyPr/><a:lstStyle/>{paras}</p:txBody></p:sp>'
        files[f'ppt/notesSlides/notesSlide{n}.xml'] = f'<p:notes {NS}><p:cSld><p:spTree>{group}{body}</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:notes>'
        files[f'ppt/notesSlides/_rels/notesSlide{n}.xml.rels'] = relationships([('rId1','notesMaster','../notesMasters/notesMaster1.xml'),('rId2','slide',f'../slides/slide{n}.xml')])
    cts={'ppt/presentation.xml':'presentation.main','ppt/slideMasters/slideMaster1.xml':'slideMaster','ppt/slideLayouts/slideLayout1.xml':'slideLayout','ppt/notesMasters/notesMaster1.xml':'notesMaster','ppt/theme/theme1.xml':'theme'}
    for n in range(1,len(slides)+1):
        cts[f'ppt/slides/slide{n}.xml']='slide'
        cts[f'ppt/notesSlides/notesSlide{n}.xml']='notesSlide'
    types=''.join(f'<Override PartName="/{path}" ContentType="application/vnd.openxmlformats-officedocument.{"theme+xml" if kind == "theme" else "presentationml."+kind+"+xml"}"/>' for path,kind in cts.items())
    files['[Content_Types].xml'] = '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="png" ContentType="image/png"/>'+types+'<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/></Types>'
    path.parent.mkdir(parents=True,exist_ok=True)
    with ZipFile(path,'w',ZIP_DEFLATED) as z:
        for name,content in files.items():
            ET.fromstring(content)
            z.writestr(name,content.encode('utf-8'))
        for name in sorted({name for s in slides for _,name in s.images}):
            z.write(ROOT/'site'/'assets'/name, f'ppt/media/{name}')


if __name__ == '__main__':
    deck = build()
    package(deck, OUT)
    print(f'Created {OUT} ({len(deck)} slides, editable text, speaker notes and source links)')
