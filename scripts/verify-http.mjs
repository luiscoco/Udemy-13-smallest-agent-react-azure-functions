import assert from 'node:assert/strict';

const base = process.env.VERIFY_BASE_URL || 'http://127.0.0.1:7073';
const health = await fetch(`${base}/api/health`);
assert.equal(health.status, 200);
assert.deepEqual(await health.json(), {status: 'ok', api_key_configured: false},
  'Verification requires a server started with the placeholder key; no model calls are allowed.');
let checks = 1;
for (const path of ['/api/docs', '/api/openapi.json']) {
  const response = await fetch(base + path);
  assert.equal(response.status, 200);
  if (path.endsWith('.json')) assert.ok((await response.json()).paths['/api/agent/query']);
  else assert.match(await response.text(), /Agent/);
  checks++;
}
async function post(body, status, headers = {'Content-Type':'application/json'}) {
  const response = await fetch(`${base}/api/agent/query`, {method:'POST', headers, body});
  assert.equal(response.status, status);
  assert.equal(typeof (await response.json()).detail, 'string');
  checks++;
}
await post('{', 400);
await post('{}', 415, {'Content-Type':'text/plain'});
await post('{}', 415, {'Content-Type':'application/json-incorrect'});
await post(JSON.stringify({prompt:'hello'}), 403, {'Content-Type':'application/json', Origin:'https://foreign.invalid'});
await post(JSON.stringify({prompt:' '.repeat(33000)}), 413);
for (const prompt of ['', '  ', 123, 'x'.repeat(4001)]) await post(JSON.stringify({prompt}), 422);
await post(JSON.stringify({prompt:'hello'}), 503);
const stream = await fetch(`${base}/api/agent/stream`, {
  method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({prompt:'hello'})
});
assert.equal(stream.status, 200);
assert.match(stream.headers.get('content-type'), /text\/event-stream/);
const events = await stream.text();
assert.match(events, /event: error/);
assert.doesNotMatch(events, /event: done/);
checks++;
assert.equal((await fetch(`${base}/unknown`)).status, 404);
checks++;
console.log(`${checks} real HTTP checks passed; inference stayed disabled.`);
