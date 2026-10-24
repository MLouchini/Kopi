(()=>{
let observing=false,suggestions=false,lastPage='',timer;
const sensitive=el=>/password|secret|token|cc-number|one-time-code|credit.?card|otp|cvv|cvc/i.test([el.type,el.name,el.autocomplete,el.id].join(' '));
const safeUrl=()=>location.origin+location.pathname;
function snapshot(){const root=document.querySelector('main,article,[role=main]')||document.body;const c=root.cloneNode(true);c.querySelectorAll('input,textarea,[contenteditable],script,style,nav,footer,[hidden]').forEach(e=>e.remove());return (c.innerText||c.textContent||'').replace(/\s+/g,' ').slice(0,4000);}
function selector(el){if(el.id)return '#'+CSS.escape(el.id);if(el.name)return `${el.tagName.toLowerCase()}[name="${CSS.escape(el.name)}"]`;const parts=[];for(let n=el;n&&n!==document.body;n=n.parentElement){let s=n.tagName.toLowerCase();if(n.parentElement)s+=`:nth-child(${[...n.parentElement.children].indexOf(n)+1})`;parts.unshift(s);}return 'body>'+parts.join('>');}
function emit(kind,el,value=''){if(!observing||!el||sensitive(el))return;const label=el.labels?.[0]?.innerText||el.getAttribute('aria-label')||el.innerText||el.name||'';chrome.runtime.sendMessage({event:{kind,url:safeUrl(),title:document.title,text:kind==='page'?snapshot():'',label:label.slice(0,200),selector:selector(el),value:value.slice(0,2000),fieldType:el.type||''}}).catch(()=>{});}
chrome.runtime.onMessage.addListener(m=>{if(m.state){const started=!observing&&m.state.observing;observing=m.state.observing;suggestions=m.state.suggestions;if(started&&document.visibilityState==='visible')emit('page',document.body);}});
document.addEventListener('click',e=>emit('click',e.target.closest('button,a,input,[role=button]')||e.target),true);
document.addEventListener('change',e=>emit('fill',e.target,e.target.value||''),true);
document.addEventListener('copy',()=>emit('copy',document.activeElement,window.getSelection()?.toString()||''),true);
document.addEventListener('paste',e=>emit('paste',e.target,e.clipboardData?.getData('text/plain')||''),true);
setInterval(()=>{if(!observing&&!suggestions)return;if(document.visibilityState!=='visible')return;const key=location.href+'|'+document.title;if(key===lastPage)return;lastPage=key;clearTimeout(timer);timer=setTimeout(()=>chrome.runtime.sendMessage({event:{kind:'page',url:safeUrl(),title:document.title,text:snapshot()}}).catch(()=>{}),700);},1500);
})();
