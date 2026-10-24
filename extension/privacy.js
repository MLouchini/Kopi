globalThis.KopiPageInstance ??= Array.from(crypto.getRandomValues(new Uint8Array(16)),b=>b.toString(16).padStart(2,'0')).join('');
// Shared by observation and execution snapshots. Authentication values are not model evidence.
globalThis.KopiPrivacy={
  snapshotText(limit=4000){
    const root=document.querySelector('main,article,[role=main]')||document.body;
    if(!root)return '';
    const parts=[];let length=0;
    const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT);
    for(let node;length<limit&&(node=walker.nextNode());){
      const el=node.parentElement;
      if(!el||el.isContentEditable||el.closest('script,style,input,textarea,nav,footer,[hidden],[aria-hidden="true"]'))continue;
      const style=getComputedStyle(el);
      if(style.display==='none'||style.visibility==='hidden'||style.visibility==='collapse'||!el.getClientRects().length)continue;
      const text=(node.nodeValue||'').replace(/[\u200B-\u200D\uFEFF]/g,'').replace(/\s+/g,' ').trim();
      if(text){parts.push(text);length+=text.length+1;}
    }
    return this.text(parts.join(' ').slice(0,limit),document.title);
  },
  authenticationContext(text){return /verification|security\s*code|one[ -]?time|two[ -]?factor|authenticat|sign[ -]?in\s*code|\b2fa\b/i.test(text);},
  text(text,context=''){return this.authenticationContext(context+' '+text)?text.replace(/\b\d{4,8}\b/g,'[temporary code omitted]'):text;},
  value(value,context=''){return /^\d{4,8}$/.test(value.trim())||this.authenticationContext(context)?'[temporary value omitted]':this.text(value,context);}
};
