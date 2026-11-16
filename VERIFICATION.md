# Verification · 2026-11-09

Startup follow-up: npm run dev now starts Azurite first and waits for its three TCP listeners, starts Functions and waits for successful /api/health, then launches React. This replaces concurrent startup, which let browser requests race Functions initialization and could let Functions storage checks race the emulator. The launcher checks occupied ports before starting and shuts down only its own child process trees. No provider POST is needed for readiness.

Verified locally on Windows using Node.js 24.22.2, .NET SDK 10.0.300, Azure Functions Core Tools 4.16.0 and Functions runtime 4.1054.250.26428. No cloud deployment or live paid inference was performed.

| Check | Actual result |
| --- | --- |
| React TypeScript checking | Passed |
| React/Vitest | 19 tests passed |
| C# endpoint + official SDK checks | 25 checks passed |
| .NET 10 isolated Functions build | Passed, no compiler warnings/errors in the release build |
| React/Vite production build | Passed |
| Functions publish | Passed; deployable .NET artifacts generated |
| Bicep compilation | Passed without diagnostics after correcting Flex trigger concurrency structure |
| Real Functions HTTP checks | 15 passed with placeholder-only credentials |
| Chrome desktop/mobile checks | Passed |
| Documented npm run dev | Starts React, actual Functions host and Azurite successfully |
| Shipped browser dependency audit | npm audit --omit=dev reports zero vulnerabilities |

React tests cover five functional tabs and retained lab state, real contract parsing, UTF-8/CRLF SSE boundaries, incomplete streams, incremental text before final completion, Cancel/AbortSignal, duplicate-submit locking, model error after transport completion, C# syntax coloring and safe Markdown/unknown-language rendering. Source-linked cards are checked for all sixteen steps.

C# checks exercise actual bounded request reads, malformed JSON/media/prompt, forbidden Origin, allowed OPTIONS, missing-key responses before runner invocation, safe provider failures, final-plus-done and configuration bounds. The official Anthropic SDK also parses an in-memory HTTP/SSE fixture for usage, stop reasons, cancellation and key redaction across provider delta boundaries. Fixture credentials and responses are synthetic; these tests never contact Claude.

Real Core Tools was started both from the published package with isolated placeholder settings and through the documented npm run dev launcher. HTTP checks assert health api_key_configured=false before any POST, then exercise docs/OpenAPI, invalid input, 400/403/413/415/422, query 503, stream error without done and unknown-route 404. The source launcher uses the new Functions SDK's recommended dotnet run flow, with local Core Tools on its process PATH.

Installed Chrome checked five navigation tabs, real missing-key stream behavior through Vite's proxy, all sixteen highlighted architecture cards, local SVG, theme persistence and a 390px viewport without page overflow. Representative desktop architecture and mobile lab screenshots were inspected. This verifies local browser layout and missing-key integration; successful model output is covered by deterministic React/SDK fixtures rather than a live provider call.

The UML generator reads actual React/C# source markers, includes source line numbers and emits matching SVG/PlantUML interactions. Loop and alternative compartments have explicit transparent fills and separate failure/input sections, preventing the earlier black-frame issue. Generation does not call a remote diagram service.

The initial source-start check exposed the new Functions SDK's recommendation to use dotnet run instead of func start against source. The final helper uses dotnet run and was verified. Release build/publish and the Bicep compiler are separate from the locally running host.

Full npm audit reports ten upstream **development-tool** advisories (seven moderate, three high), under Functions Core Tools/Azurite dependencies including extract-zip, brace-expansion, glob/minimatch and uuid. Compatible npm audit fix did not resolve them; proposed force fixes downgrade the tools to incompatible old majors and were not applied. These Node tools are not deployed with the C# backend or emitted into the React bundle. Shipped browser dependencies audit cleanly.

The Bicep template was compiled, not deployed or subscription-validated. Azure regional .NET 10 availability, identity/RBAC propagation, Key Vault resolution, Entra consent and token validation, cloud SSE buffering/timeouts and real Claude model access still need cloud verification. Static website upload, cloud resource creation, authenticated cloud testing, costs and production operations were not performed. No Azure credentials, existing real environment files or previous application's API key were read or copied.

See DEPLOYMENT.md for explicit hosting commands and README.md for local startup. Outputs and local QA storage/screenshots are ignored. Temporary placeholder-only settings used for QA are removed from the final publish artifact.
