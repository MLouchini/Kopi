const assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
const scope={AbortController,URL,Map,Set,console,setTimeout:()=>0,clearTimeout:()=>{},setInterval:()=>0,clearInterval:()=>{},chrome:{storage:{local:{get:async()=>({token:'test'})}},runtime:{onMessage:{addListener(){}}},alarms:{create(){},onAlarm:{addListener(){}}},tabs:{query:async()=>[]}},fetch:async()=>({ok:true,json:async()=>({observing:false,suggestions:false,command:null})})};
vm.createContext(scope);vm.runInContext(fs.readFileSync(path.join(__dirname,'../../extension/background.js'),'utf8'),scope);
(async()=>{
 for(const [name,api,expected] of [
  ['desktop closes',async()=>{throw Error('connection refused')},true],
  ['run cancelled',async()=>({cancelled:true}),true],
  ['active lease',async()=>({cancelled:false}),false]
 ]){scope.api=api;const c=new AbortController();await scope.checkLease({id:'test-run'},c);assert.equal(c.signal.aborted,expected,name);}
 console.log('PASS: desktop shutdown, cancellation and active command lease checks');
})().catch(e=>{console.error(e);process.exitCode=1;});
