import {spawn} from 'node:child_process';
import {createConnection} from 'node:net';
import {fileURLToPath} from 'node:url';

const root=fileURLToPath(new URL('../',import.meta.url));
const children=[];
let stopping=false, failure='';
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function listening(port) {
  return new Promise(resolve=>{
    const socket=createConnection({host:'127.0.0.1',port});
    const finish=value=>{socket.destroy();resolve(value);};
    socket.once('connect',()=>finish(true));
    socket.once('error',()=>finish(false));
    socket.setTimeout(1000,()=>finish(false));
  });
}
async function stop(code) {
  if(stopping)return;
  stopping=true;
  await Promise.all(children.filter(child=>child.exitCode===null).map(child=>new Promise(resolve=>{
    if(process.platform==='win32') {
      const killer=spawn('taskkill',['/PID',String(child.pid),'/T','/F'],{stdio:'ignore',windowsHide:true});
      killer.once('exit',resolve);killer.once('error',resolve);
    } else { child.kill('SIGTERM');resolve(); }
  })));
  process.exit(code);
}
function start(script) {
  const child=spawn(process.execPath,[process.env.npm_execpath,'run',script],{cwd:root,stdio:'inherit',windowsHide:true});
  children.push(child);
  child.once('error',()=>{failure=`Could not launch ${script}.`;void stop(1);});
  child.once('exit',code=>{
    if(!stopping){console.error(`[STARTUP] ${script} stopped (exit ${code}). Stopping this application's other services.`);void stop(code||1);}
  });
}
async function waitFor(name,check,seconds) {
  console.log(`[STARTUP] Waiting for ${name}...`);
  const deadline=Date.now()+seconds*1000;
  while(!stopping&&Date.now()<deadline) {
    if(failure)throw Error(failure);
    if(await check()) {console.log(`[STARTUP] ${name} is ready.`);return;}
    await delay(500);
  }
  if(!stopping)throw Error(`${name} did not become ready within ${seconds} seconds. Check its output above. The frontend has not been started.`);
}
process.once('SIGINT',()=>void stop(0));
process.once('SIGTERM',()=>void stop(0));
try {
  if(!process.env.npm_execpath)throw Error('Start this launcher with npm run dev.');
  for(const port of [10000,10001,10002,7073,5400]) {
    if(await listening(port))throw Error(`Port ${port} is already occupied. Stop the previous dev instance you started, or run the separate dev:backend/dev:frontend commands to reuse your storage emulator.`);
  }
  start('dev:storage');
  await waitFor('Azurite Blob, Queue and Table services',async()=> (await Promise.all([10000,10001,10002].map(listening))).every(Boolean),180);
  if(!stopping)start('dev:backend');
  await waitFor('Azure Functions API on port 7073',async()=>{
    try {
      const response=await fetch('http://127.0.0.1:7073/api/health',{signal:AbortSignal.timeout(30000)});
      return response.ok&&(await response.json()).status==='ok';
    }catch{return false;}
  },240);
  if(!stopping){start('dev:frontend');console.log('[STARTUP] Backend ready. React will open at http://127.0.0.1:5400/. Keep this terminal running; Ctrl+C stops all three services.');}
}catch(error){console.error(`[STARTUP] ${error.message}`);await stop(1);}
