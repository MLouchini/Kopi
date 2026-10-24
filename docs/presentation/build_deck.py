"""Build Kopi's editable hackathon pitch with standard-library OOXML.

Run from any directory: py docs/presentation/build_deck.py
No model weights, secrets, or third-party presentation packages are required.
"""

from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from xml.sax.saxutils import escape
import xml.etree.ElementTree as ET
import argparse

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

    def heading(self, title, number):
        self.text(title, 64, 65, 1152, 122, 48, INK, True)

    def rule(self, x, y, w=1152, fill=LINE):
        self.shape(x, y, w, 1.3, fill)

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
        'Kopi is designed to handle repeated browser work in the background. The idea started with something I kept doing myself: logging into university and going through verification every day. Once a supported verification workflow is learned and enabled, Kopi can recognise a matching challenge and execute it automatically.',
        ['Origin story supplied by the user in this chat.', REPO+'/blob/main/README.md', 'Brand assets: site/assets. Palette: site/style.css.'])
    s.image('kopi-wordmark.png', 48, 154, 640, 240)
    s.text('A local AI assistant that handles\nbrowser routines in the background', 72, 454, 690, 132, 34)
    s.image('kopi-logo.png', 770, 168, 440, 440)

    s = new('The problem', '00:15–00:55 / 40 seconds',
        'Every day I log into my university account, and every day I have to verify again. That means requesting a code, finding it in the right conversation, going back to the university page and entering it. The verification protects the account, but manually moving through the same browser steps is repetitive. That repeated routine gave me a concrete problem to build around. I wanted to reduce the manual steps while keeping the verification itself.',
        ['Personal motivation supplied by the user in this chat.', REPO+'/blob/main/HANDOFF.md'])
    s.heading('The problem', 2)
    s.text('Every day, I log into university.\nEvery day, I have to verify again.', 64, 231, 1152, 143, 45, INK, True)
    for x, num, title in [(64,'01','Request a code'),(461,'02','Find the message'),(858,'03','Return and verify')]:
        s.rule(x, 456, 350)
        s.text(num, x, 485, 350, 65, 48, ORANGE)
        s.text(title, x, 574, 350, 60, 29, INK, True)

    s = new('The idea', '00:55–01:35 / 40 seconds',
        'My first thought was to automate that login routine. But a script written for one university page would only solve that one case. The wider idea was a local assistant that recognises repeated browser work and handles it in the background. It should understand which steps stay the same and which information changes, so I can carry on with my day. Current automatic execution covers explicitly enabled learned verification workflows. Broader background learning and automation remain the product direction.',
        ['User-provided motivation.', REPO+'/blob/main/README.md', REPO+'/blob/main/src/Kopi.Core/Intelligence.cs'])
    s.heading('The idea', 3)
    s.shape(64, 258, 9, 229, ORANGE)
    s.text('What if my computer recognised\nthe routine and handled it\nwhile I carried on?', 102, 247, 1090, 241, 49, INK, True)
    s.text('Start with my university login.\nMake the same approach reusable for other browser tasks.', 102, 552, 1090, 105, 29, MUTED)

    s = new('What Kopi actually does', '01:35–02:15 / 40 seconds',
        'Kopi stays running with the paired browser extension. With page monitoring enabled, it receives page context and uses local Gemma to match that context to an approved learned workflow. When I reach a matching new verification challenge, Kopi runs the enabled workflow automatically, transfers a fresh code and checks the result. I do not have to open the app, start a recording or approve every repeat. Setup and enablement happen beforehand. Current automatic execution is scoped to supported fresh-value workflows. Ordinary workflows still require approval, and fully passive learning of arbitrary routines is not implemented.',
        [REPO+'/blob/main/HANDOFF.md', REPO+'/blob/main/docs/verification-workflows.md'])
    s.heading('What Kopi actually does', 4)
    for y, num, title, body in [
        (220,'01','Watch','Notice page context while Kopi runs in the background.'),
        (329,'02','Recognise','Match the current task to a learned routine with Gemma.'),
        (438,'03','Act','Run an enabled verification workflow automatically.'),
        (547,'04','Check','Confirm the result and stop if anything is uncertain.')]:
        s.text(num, 64, y, 84, 60, 40, ORANGE)
        s.text(title, 175, y+4, 225, 58, 32, INK, True)
        s.text(body, 440, y+8, 776, 77, 28)
        s.rule(64, y+89)

    s = new('The university login example', '02:15–02:55 / 40 seconds',
        'After initial setup and enablement, reaching a matching new university challenge is the trigger. Kopi detects the page context in the background, requests a fresh code, finds the new matching message in the approved conversation, transfers the code, submits it and checks for a separate success state. No new recording or per-run approval is needed for this enabled workflow. The implementation checks freshness, source identity and destination identity, and stops if the new message is stale, missing or ambiguous. Browser tests exercise this flow on test pages. Real WhatsApp and university accounts still need validation.',
        [REPO+'/blob/main/extension/fresh.js', REPO+'/blob/main/src/Kopi.Core/FreshCompiler.cs', REPO+'/blob/main/HANDOFF.md'])
    s.heading('The university login example', 5)
    for x, num, title, body in [
        (64,'01','Detect','Recognise a matching\nnew sign-in challenge.'),
        (461,'02','Fetch','Request a code and read\nthe new matching message.'),
        (858,'03','Verify','Fill, submit and check\nthe result automatically.')]:
        s.shape(x, 232, 350, 7, ORANGE)
        s.text(num, x, 282, 350, 100, 76, ORANGE)
        s.text(title, x, 418, 350, 70, 39, INK, True)
        s.text(body, x, 517, 350, 119, 29)

    s = new('Why I built it this way', '02:55–03:45 / 50 seconds',
        'The problem shaped the architecture. First, I was already signed into my normal browser. Using an extension lets Kopi work there without another profile or another login. Second, browser evidence can contain personal information, so Gemma runs locally through llama.cpp rather than sending that evidence to cloud inference. Third, an AI proposal can be wrong. Kopi uses a reviewed workflow and fixed actions, with observed targets and checks around identity, freshness and success. The model interprets the task, while the execution code controls what can actually happen. Verification values stay transient in the extension and destination page.',
        [REPO+'/blob/main/docs/architecture.md', REPO+'/blob/main/docs/privacy.md', REPO+'/blob/main/src/Kopi.Core/FreshCompiler.cs'])
    s.heading('Why I built it this way', 6)
    for y, title, reason in [
        (234,'Existing browser','I was already signed in, so Kopi works\ninside my current Chrome or Edge session.'),
        (383,'Local AI','Browser evidence can be personal,\nso Gemma runs on my computer.'),
        (532,'Checked actions','The model can misunderstand a task,\nso execution checks targets and results.')]:
        s.text(title, 64, y, 352, 68, 35, INK, True)
        s.text(reason, 460, y+1, 756, 111, 29)
        if y < 532:
            s.rule(64, y+120)

    s = new('How I built it', '03:45–04:30 / 45 seconds',
        'The JavaScript browser extension monitors page context and performs browser actions. The desktop is C# with .NET 10 and WPF. An authenticated Kestrel loopback bridge connects it to the extension. The open-weight model is Google Gemma 4 E2B IT QAT Q4_0, running locally with llama.cpp on the CPU. It interprets task evidence, extracts changing values, resolves questions and matches the current page to a learned workflow. My application code supplies the observation adapter, workflow contracts and compiler, page-trigger matching, fresh-code coordinator and runtime manager. Model weights and llama.cpp are upstream dependencies. This product uses local inference rather than the Gemini API.',
        [REPO+'/blob/main/model-manifest.json', MODEL, REPO+'/blob/main/src/Kopi.Core/Intelligence.cs', REPO+'/blob/main/src/Kopi.Core/ModelRuntime.cs', REPO+'/blob/main/src/Kopi.Core/FreshCompiler.cs', REPO+'/blob/main/docs/architecture.md'])
    s.heading('How I built it', 7)
    for x, bg, title, body in [
        (64,PEACH,'Browser extension','JavaScript\nMonitors and acts'),
        (471,'F4EDE5','Desktop app','C# / .NET 10 / WPF\nCoordinates and validates'),
        (878,INK,'Local model','Gemma 4 E2B\nllama.cpp / CPU')]:
        s.shape(x, 234, 338, 177, bg)
        fg = PAPER if bg == INK else INK
        s.text(title, x+22, 258, 294, 52, 28, fg, True)
        s.text(body, x+22, 329, 294, 72, 23, fg)
    s.shape(414, 320, 45, 3, ORANGE)
    s.shape(821, 320, 45, 3, ORANGE)
    s.text('Gemma recognises the task context\nand matches it to a learned workflow.', 64, 463, 1152, 105, 33, INK, True)
    s.text('I built the page triggers, workflow compiler, code-transfer logic and runtime manager.', 64, 606, 1152, 72, 27)

    s = new('What works and what comes next', '04:30–05:00 / 30 seconds',
        'The current implementation includes page monitoring, local task matching and automatic execution of explicitly enabled learned verification workflows. Fresh-code checks have browser-fixture coverage. General background learning and automation remain the wider goal, rather than a completed capability for arbitrary browser work. Next comes real university and WhatsApp account testing, performance measurement on the actual laptop and resolving the remaining expanded workflow QA issues. The aim is to let Kopi handle routine work while I carry on. No time-saving or adoption metrics have been measured.',
        [REPO, REPO+'/blob/main/HANDOFF.md', REPO+'/blob/main/docs/qa.md', REPO+'/actions/runs/40097950425'])
    s.heading('What works and what comes next', 8)
    s.text('Implemented', 64, 237, 540, 60, 35, INK, True)
    s.text('Page monitoring and local matching\nAutomatic enabled verification runs\nLocal Gemma task understanding\nFresh-code checks on test pages', 64, 330, 540, 255, 28)
    s.text('Next', 674, 237, 542, 60, 35, INK, True)
    s.text('Broaden background learning and automation\nTest real university and WhatsApp flows\nMeasure performance on my laptop\nFix remaining end-to-end QA issues', 674, 330, 542, 255, 28)
    s.rule(64, 615)
    s.text('github.com/MLouchini/Kopi', 64, 644, 1152, 42, 25, ORANGE, True)
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
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUT)
    args = parser.parse_args()
    deck = build()
    package(deck, args.output)
    print(f'Created {args.output} ({len(deck)} slides, editable text, speaker notes and source links)')
