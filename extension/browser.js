/* Shared browser primitives. Loaded by both extension and Playwright integration tests. */
globalThis.KopiBrowser = {
  snapshot() {
    const root = document.querySelector('main,article,[role=main]') || document.body;
    const clone = root.cloneNode(true);
    clone.querySelectorAll('script,style,input,textarea,[contenteditable],nav,footer,[hidden]').forEach(x=>x.remove());
    return {kind:'page',url:location.href,title:document.title,text:(clone.innerText||clone.textContent||'').replace(/\s+/g,' ').slice(0,4000)};
  },
  async action({step}) {
    if (!['fill','click'].includes(step.kind)) throw Error('Unsupported DOM action');
    const nodes = document.querySelectorAll(step.selector);
    if (nodes.length !== 1) throw Error(`Expected one target, found ${nodes.length}. Review this workflow's locator.`);
    const el = nodes[0];
    if (/password|secret|token|cc-number|one-time-code/i.test([el.type,el.name,el.autocomplete].join(' '))) throw Error('Sensitive field blocked');
    if (!el.getClientRects().length || el.disabled) throw Error('Target is hidden or disabled');
    el.scrollIntoView({block:'center'});
    if (step.kind === 'fill') {
      if (!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) || el instanceof HTMLInputElement && !['text','email','search','url','tel','number'].includes(el.type)) throw Error('Target is not a supported text input');
      const setter = Object.getOwnPropertyDescriptor(el instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype,'value').set;
      setter.call(el,step.value); el.dispatchEvent(new Event('input',{bubbles:true})); el.dispatchEvent(new Event('change',{bubbles:true}));
      if (el.value !== step.value) throw Error('Field value verification failed');
    } else {
      el.click();
      for(let i=0;i<50;i++){const success=document.querySelector(step.verifySelector);if(success?.getClientRects().length)return {verified:true};await new Promise(r=>setTimeout(r,100));}
      throw Error('Success marker did not appear. Check the destination before retrying.');
    }
    return {verified:true};
  },
  async csv({url}) {
    const response = await fetch(url,{credentials:'include',cache:'no-store',signal:AbortSignal.timeout(15000)});
    if(!response.ok)throw Error('Cannot read spreadsheet. Check access and login.');
    const text=await response.text();
    if (/^\s*<!doctype|^\s*<html/i.test(text))throw Error('Spreadsheet returned a login page.');
    return text;
  }
};
