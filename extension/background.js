const base='http://127.0.0.1:47653';
let state={observing:false,suggestions:false},busy=false,runTab=null;
async function api(path,body={},extra={}){const {token}=await chrome.storage.local.get('token');const r=await fetch(base+path,{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+(token||''),...extra},body:JSON.stringify(body)});if(!r.ok)throw Error(r.status===401?'Pair Kopi with the desktop app.':`Kopi connection failed (${r.status})`);return r.json();}
chrome.runtime.onMessage.addListener((m,sender,reply)=>{
 if(m.pair){api('/pair',{}, {'X-Kopi-Pair':m.pair}).then(async r=>{await chrome.storage.local.set({token:r.token});reply({ok:true});}).catch(e=>reply({error:e.message}));return true;}
 if(m.event){if(state.observing||state.suggestions&&m.event.kind==='page')api('/event',m.event).catch(()=>{});}
});
async function active(){const [tab]=await chrome.tabs.query({active:true,lastFocusedWindow:true});if(!tab?.id||!/^https?:/.test(tab.url))throw Error('Open a normal website tab first.');return tab.id;}
async function loaded(id){for(let i=0;i<100;i++){const t=await chrome.tabs.get(id);if(t.status==='complete')return;await new Promise(r=>setTimeout(r,100));}throw Error('Page loading timed out.');}
async function inject(id,method,payload){await chrome.scripting.executeScript({target:{tabId:id},files:['browser.js']});const [r]=await chrome.scripting.executeScript({target:{tabId:id},func:async (m,p)=>{try{return {value:await globalThis.KopiBrowser[m](p)};}catch(e){return {error:e.message};}},args:[method,payload]});if(r?.result?.error)throw Error(r.result.error);return r?.result?.value;}
function parseCsv(text){let rows=[],row=[],field='',quoted=false;for(let i=0;i<text.length;i++){const c=text[i];if(c==='"'){if(quoted&&text[i+1]==='"'){field+='"';i++;}else quoted=!quoted;}else if(!quoted&&(c===','||c==='\n')){row.push(field.replace(/\r$/,''));field='';if(c==='\n'){rows.push(row);row=[];}}else field+=c;}row.push(field.replace(/\r$/,''));if(row.some(Boolean))rows.push(row);return rows;}
async function appendRow(tabId,p){
 const u=new URL(p.step.url);if(u.hostname!=='docs.google.com'||!/^\/spreadsheets\/d\/[\w-]+/.test(u.pathname))throw Error('appendRow currently supports Google Sheets only.');
 const id=u.pathname.split('/')[3],url=`https://docs.google.com/spreadsheets/d/${id}/gviz/tq?tqx=out:csv&sheet=${encodeURIComponent(p.sheetName)}`;
 const read=async()=>parseCsv(await inject(tabId,'csv',{url}));
 const rows=await read(),values=p.columns.map(k=>p.values[k]||'');
 if(values.some(v=>/[\t\r\n]/.test(v)||/^[=+@-]/.test(v)))throw Error('Spreadsheet value contains control characters or a possible formula. Review the data.');
 if(rows.some(r=>values.every((v,i)=>r[i]===v)))return {verified:true,duplicate:true};
 const next=rows.length+1;
 await chrome.debugger.attach({tabId},'1.3');
 try {
  const cmd=(method,params={})=>chrome.debugger.sendCommand({tabId},method,params);
  const name=`'${p.sheetName.replaceAll("'","''")}'!A${next}`;
  const focus=await cmd('Runtime.evaluate',{expression:`(()=>{const e=document.querySelector('input.waffle-name-box, input#t-name-box, #t-name-box input, input[aria-label="Name box"]');if(!e)return false;e.focus();e.select();return true})()`,returnByValue:true});
  if(!focus.result.value)throw Error('Cannot locate Sheets name box. Open the spreadsheet in editing mode.');
  await cmd('Input.insertText',{text:name});
  await cmd('Input.dispatchKeyEvent',{type:'keyDown',key:'Enter',code:'Enter',windowsVirtualKeyCode:13});await cmd('Input.dispatchKeyEvent',{type:'keyUp',key:'Enter',code:'Enter',windowsVirtualKeyCode:13});
  const clipboard=await cmd('Runtime.evaluate',{expression:`navigator.clipboard.writeText(${JSON.stringify(values.join('\t'))})`,awaitPromise:true,userGesture:true});
  if(clipboard.exceptionDetails)throw Error('Clipboard permission denied. Allow clipboard access for Sheets.');
  await cmd('Input.dispatchKeyEvent',{type:'keyDown',key:'v',code:'KeyV',windowsVirtualKeyCode:86,modifiers:2});await cmd('Input.dispatchKeyEvent',{type:'keyUp',key:'v',code:'KeyV',windowsVirtualKeyCode:86,modifiers:2});
 } finally {await chrome.debugger.detach({tabId});}
 for(let i=0;i<15;i++){await new Promise(r=>setTimeout(r,1000));const after=await read();if(after.some(r=>values.every((v,j)=>r[j]===v)))return {verified:true};}
 throw Error('Could not verify the new row. Check the spreadsheet before retrying.');
}
async function execute(c){
 if(c.kind==='snapshot'){runTab=await active();return inject(runTab,'snapshot',{});}
 if(c.kind!=='step')throw Error('Unknown command');
 const p=c.payload,s=p.step;
 if(s.kind==='navigate'||s.kind==='appendRow'){
  if(!/^https?:\/\//.test(s.url))throw Error('Unsupported URL');
  const tabs=await chrome.tabs.query({});const existing=tabs.find(t=>t.url?.split('#')[0]===s.url.split('#')[0]);
  runTab=existing?.id||(await chrome.tabs.create({url:s.url,active:true})).id;
  await chrome.tabs.update(runTab,{active:true});await loaded(runTab);
 }
 if(!runTab)throw Error('No execution tab selected. Preview the current page first.');
 if(s.kind==='navigate')return {verified:true};
 if(s.kind==='appendRow')return appendRow(runTab,p);
 return inject(runTab,'action',p);
}
async function poll(){if(busy)return;busy=true;try{const r=await api('/poll');state={observing:r.observing,suggestions:r.suggestions};const tabs=await chrome.tabs.query({});for(const t of tabs)chrome.tabs.sendMessage(t.id,{state}).catch(()=>{});if(r.command){const c=r.command;const heartbeat=setInterval(()=>api('/ping').catch(()=>{}),5000);try{const value=await Promise.race([execute(c),new Promise((_,reject)=>setTimeout(()=>reject(Error('Browser action exceeded 60 seconds. Check the destination before retrying.')),60000))]);await api('/result',{id:c.id,result:JSON.stringify(value)});}catch(e){await api('/result',{id:c.id,error:e.message});}finally{clearInterval(heartbeat);}}}catch{}finally{busy=false;}}
setInterval(poll,1000);chrome.alarms.create('kopi',{periodInMinutes:0.5});chrome.alarms.onAlarm.addListener(poll);poll();
