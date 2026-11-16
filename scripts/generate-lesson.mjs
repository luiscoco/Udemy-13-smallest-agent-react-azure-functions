import {readFileSync, writeFileSync, mkdirSync} from 'node:fs';
import {resolve, dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const files={app:'frontend/src/AgentApp.tsx',client:'frontend/src/api.ts',code:'frontend/src/code.ts',endpoints:'backend/AgentEndpoints.cs',runner:'backend/ClaudeAgentRunner.cs',functions:'backend/AgentFunctions.cs'};
const definitions=[
 ['Run prompt',0,1,'app','const trimmed = prompt.trim()',6,'Lock and trim input before beginning asynchronous work.','A React ref prevents duplicate submits immediately; the server independently checks untrusted input.'],
 ['POST /api/agent/stream',1,2,'client',"const response = await authorizedFetch(apiUrl('/api/agent/stream')",1,'POST JSON to the public Functions origin or the development proxy.','The browser sends an Entra bearer token only when public identity settings are configured; the Claude key never enters this client.'],
 ['Validate bounded JSON input',2,2,'endpoints','public static async Task<string?> ReadPromptAsync',16,'Validate media type and actual bytes before parsing.','The 32 KiB limit applies while reading, independent of Content-Length. Strict JSON parsing and trimmed 1–4000 characters precede provider calls.'],
 ['Delegate HTTP trigger',2,3,'functions','await endpoints.StreamAsync',3,'A real .NET isolated HTTP trigger delegates transport work.','The endpoint receives HttpContext through ASP.NET Core integration; no standalone Node or Minimal API server replaces Functions.'],
 ['Create streaming model request',3,4,'runner','await foreach (var chunk in client.Messages.CreateStreaming',6,'Call the official Anthropic C# Messages SDK.','The typed parameters above this line specify one user prompt, max tokens, model, optional system instructions and no tools. There is no CLI subprocess.'],
 ['Receive SDK stream chunks',4,3,'runner','var parameters = new MessageCreateParams',7,'The SDK manages the HTTPS exchange with Claude.','This excerpt is the application request boundary. Provider transport is inside the official library; the diagram does not invent a raw HTTP call in application source.'],
 ['Project safe text delta',3,2,'runner','var safeText = StreamText',3,'Allowlist text instead of forwarding raw SDK objects.','Streaming redaction retains possible key prefixes between chunks, preventing the configured key leaking when split across provider deltas.'],
 ['Write UTF-8 SSE event',2,1,'endpoints','async Task SendEvent',4,'Serialize and flush each application event.','The server awaits writes, sends heartbeat comments and bounds execution with linked request, invocation and deadline cancellation.'],
 ['Update React answer and timeline',1,1,'app',"if (message.type === 'TextDelta'",4,'Update incremental answer text separately from final result.','Visible history caps at 500 events while final result processing continues; tab navigation keeps the mounted lab state.'],
 ['Aggregate result and usage',3,2,'runner','var result = new AgentResult',4,'Collect final text, token usage, duration and stop reason.','num_turns=1 denotes one model request. Cost remains null; a max_tokens stop is an error state even when partial text exists.'],
 ['Dispatch final result',1,1,'client',"if (event === 'message')",2,'Validate application projections before dispatch.','ResultMessage is our public contract, not the name of an official C# SDK class. Unknown or malformed messages are rejected.'],
 ['Render safe highlighted Markdown',1,0,'code','export function renderMarkdown',4,'Sanitize all generated answer HTML.','Markdown code fences use Highlight.js; DOMPurify then removes active markup. Unknown languages display escaped plain text.'],
 ['Complete final result plus done',2,1,'endpoints','if (!foundResult)',3,'Require a final result before sending transport completion.','React also requires final plus done. The final is_error flag still determines model success.'],
 ['Emit safe stream error',2,1,'endpoints','if (!context.RequestAborted.IsCancellationRequested',2,'Send an error without done when the provider or deadline fails.','Public failures omit raw exceptions, credentials and prompts. Client or host cancellation aborts owned work and disposes the iterator.'],
 ['Show recoverable browser error',1,0,'app','setError(message);',3,'Display failure in the answer card and notification.','Users can correct server configuration and retry. Cancel aborts fetch and stops the client run; no automatic duplicate paid requests are made.'],
 ['Reject missing key before inference',2,1,'endpoints','if (!settings.Configured) { await SendEvent',2,'Reject missing configuration before entering the SDK.','Simple mode returns HTTP 503; stream mode sends error without done. Lessons and documentation remain available locally without credentials.'],
];
const escape=s=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;');
const steps=definitions.map(([title,from,to,key,marker,count,text,explanation],i)=>{
 const source=files[key],lines=readFileSync(resolve(root,source),'utf8').split(/\r?\n/),start=lines.findIndex(l=>l.includes(marker));
 if(start<0)throw Error(`Missing source marker: ${source} ${marker}`);
 const selected=lines.slice(start,start+count),indent=Math.min(...selected.filter(l=>l.trim()).map(l=>l.search(/\S/)));
 return {number:i+1,title,source,line:start+1,language:source.endsWith('.cs')?'csharp':'typescript',text,explanation,code:selected.map(l=>l.slice(indent)).join('\n')};
});
const xs=[140,420,700,980,1260],names=['Student / Browser','React client','Azure Functions','C# Messages SDK','Claude API'];
let svg=`<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="1660" viewBox="0 0 1440 1660" role="img" aria-labelledby="title desc"><title id="title">React and Azure Functions UML workflow</title><desc id="desc">Sixteen numbered interactions with streaming loop and success, error and missing-key alternatives.</desc><defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="9" refY="5" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 Z" fill="#285b98"/></marker></defs><rect width="1440" height="1660" fill="#f8fbff"/>`;
const text=(x,y,label,size=18,anchor='start')=>`<text x="${x}" y="${y}" text-anchor="${anchor}" fill="#142547" font-family="Arial,sans-serif" font-size="${size}">${escape(label)}</text>`;
svg+=text(35,40,'One prompt through a serverless C# application',28)+text(35,75,'UML sequence diagram · POST SSE · selected application-level interactions');
names.forEach((name,i)=>{svg+=`<rect x="${xs[i]-125}" y="100" width="250" height="80" rx="14" fill="${['#edf5ff','#f3edff','#eaf7f0','#fff2e6','#edf5ff'][i]}" stroke="#c3d4eb"/>`+text(xs[i],132,name,20,'middle')+text(xs[i],162,['Prompt and result','State and rendering','.NET 10 isolated worker','Typed streaming client','Model inference'][i],16,'middle')+`<line x1="${xs[i]}" y1="180" x2="${xs[i]}" y2="1615" stroke="#9baec7" stroke-width="1.5" stroke-dasharray="7 7"/>`;});
svg+=`<rect x="285" y="650" width="1110" height="280" rx="8" fill="none" stroke="#9baec7" stroke-width="1.5"/>`+text(300,675,'loop [streamed text and application updates]',17);
svg+=`<rect x="20" y="945" width="1380" height="645" rx="8" fill="none" stroke="#9baec7" stroke-width="1.5"/>`+text(30,965,'alt [complete]',17);
for(const [y,label] of [[1290,'[failure]'],[1450,'[missing key or invalid input]']]) svg+=`<line x1="20" y1="${y}" x2="1400" y2="${y}" stroke="#9baec7" stroke-width="1.5" stroke-dasharray="5 5"/>`+text(30,y+20,label,17);
definitions.forEach(([label,from,to],i)=>{let y=225+i*82;if(i>=6)y+=12;if(i>=9)y+=22;const x1=xs[from],x2=xs[to],dashed=[5,6,7,9,10,11,12,13,14,15].includes(i);const path=from===to?`M${x1} ${y} h65 v30 h-65`:`M${x1} ${y} H${x2}`;svg+=text(from===to?x1+10:Math.min(x1,x2)+15,y-10,`${i+1}. ${label}`,17)+`<path d="${path}" fill="none" stroke="#285b98" stroke-width="2" ${dashed?'stroke-dasharray="7 5"':''} marker-end="url(#arrow)"/>`;});
svg+='</svg>\n';
const ids=['Browser','React','Functions','Sdk','Claude'];
let puml='@startuml\nskinparam backgroundColor #F8FBFF\nskinparam defaultFontSize 18\n';
names.forEach((name,i)=>puml+=`participant "${name}" as ${ids[i]}\n`);
definitions.forEach(([label,from,to],i)=>{if(i===6)puml+='loop streamed text and application updates\n';if(i===9)puml+='end\nalt complete\n';if(i===13)puml+='else failure\n';if(i===15)puml+='else missing key or invalid input\n';puml+=`${ids[from]} -> ${ids[to]} : ${i+1}. ${label}\n`;});puml+='end\n@enduml\n';
for(const [name,data] of [['frontend/src/lesson/workflow.json',JSON.stringify(steps,null,2)+'\n'],['frontend/public/workflow.svg',svg],['frontend/public/workflow.puml',puml]]){mkdirSync(dirname(resolve(root,name)),{recursive:true});writeFileSync(resolve(root,name),data);}
if((svg.match(/<text[^>]*>\d+\./g)||[]).length!==16)throw Error('Expected 16 diagram labels.');
console.log('Generated 16 source-linked React/C# explanations and matching explicit-attribute UML assets.');
