# The Smallest Agent · React + Azure Functions / .NET 10

Independent edition 13: React 19 and TypeScript in the browser, with real C# Azure Functions HTTP triggers in a .NET 10 isolated worker. The backend uses the official Anthropic C# **Messages SDK** from the documentation supplied by the user. This is one model request with no tools, rather than the CLI-backed Python/TypeScript Claude Agent SDK.

Five functional lesson sections preserve the lab state: Application lab, What you will learn, How to run, Architecture & workflow, and Code walkthrough. Large readable text, pastel blue/mint/lilac/peach cards, dark theme, syntax-colored source examples and a 16-step downloadable SVG/PlantUML workflow carry forward the previous editions.

## Run on Windows

Prerequisites: Node.js 24 and the .NET 10 SDK. Local Azure Functions Core Tools 4.16.0 and Azurite install with npm; no global Functions installation or Azure account is required for local development. The first npm install downloads the Functions host and may take several minutes.

From **Lesson 0**:

```powershell
cd .\13-smallest-agent-react-azure-functions
npm ci
if (!(Test-Path backend\local.settings.json)) {
    Copy-Item backend\local.settings.example.json backend\local.settings.json
}
notepad backend\local.settings.json
```

Replace `sk-ant-replace-with-your-key` in `Values.ANTHROPIC_API_KEY` with your own key. Preserve the other Values entries, then start:

```powershell
npm run dev
```

Open **http://127.0.0.1:5400** once Vite prints its Local URL. The command first waits for all three Azurite storage services, then starts the actual Functions host on **7073** and waits for /api/health, then starts React/Vite. This prevents browser proxy requests and Functions storage checks racing initial startup. Ctrl+C stops this launcher's services together. First Functions startup compiles C# and can take longer. `Start-Application.ps1` locates its own directory.

API health: **http://127.0.0.1:7073/api/health**. Custom API reference: **http://127.0.0.1:7073/api/docs**. OpenAPI JSON: **http://127.0.0.1:7073/api/openapi.json**. The custom page is not Swagger UI.

For separate terminals, run these three commands from the application root:

```powershell
npm run dev:storage
npm run dev:backend
npm run dev:frontend
```

If Azurite reports a port already in use, stop the other storage emulator you started or reuse an already running compatible Azurite instance and launch only backend/frontend. Do not terminate unrelated applications. No Python, virtual environment, Android SDK or previous edition is required.

## Private and public configuration

The canonical local backend configuration is the ignored **backend/local.settings.json**. Example:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ANTHROPIC_API_KEY": "sk-ant-replace-with-your-key",
    "CLAUDE_MODEL": "claude-sonnet-5-5",
    "AGENT_MAX_TOKENS": "1024",
    "ALLOWED_ORIGINS": "http://127.0.0.1:5400,http://localhost:5400"
  }
}
```

Use a model your account can access. AGENT_MAX_TOKENS accepts integers 1–8192. Optional AGENT_SYSTEM_PROMPT supplies server instructions. Restart Functions after editing. Health reports key presence, not valid credentials, model access or credits. Placeholder keys and unresolved Key Vault references count as unconfigured.

An optional **backend/.env** can supply fallback application settings, using backend/.env.example. Configuration/environment values take precedence. To use a key from .env, remove the ANTHROPIC_API_KEY entry from local.settings.json rather than keeping a placeholder override there. Do not remove the storage or worker settings. The loader reads only the Function App content root; .env is not published. No actual .env or local.settings.json is included in the scaffold.

For local React, leave **frontend/.env** absent. Vite proxies /api to port 7073. For Azure, copy frontend/.env.example to frontend/.env and set the public HTTPS Function App origin and public Entra identifiers **before rebuilding**. VITE_ variables are compiled into JavaScript: never use them for the Claude key, client secrets or a shared Function key.

## What the application does

- **Lab:** trimmed prompt counter, examples, Stream/Simple modes, duplicate-submit lock, Ctrl+Enter, elapsed time, Cancel, incremental answer, safe event timeline, final token/duration/stop metrics, clipboard and Markdown download.
- **Learning content:** six explanations of React state, isolated workers, the C# SDK, private settings, streaming and safe rendering.
- **Run guide:** exact Windows commands, local storage configuration, independent publish artifacts and public Azure settings.
- **Architecture:** five lifelines, 16 numbered arrows, streaming loop and completion/failure/input alternatives. Every arrow has a colored excerpt generated from this edition's actual C#/TypeScript source, with file and line number.
- **Walkthrough:** five selected real-source explanations. Code blocks preserve indentation, support copy, register C# syntax and escape unknown languages.

SystemMessage, TextDelta and ResultMessage are **application projection names**, not C# SDK class names. Streaming uses client.Messages.CreateStreaming. The backend aggregates final text and usage; num_turns=1 means one model request. Cost is unavailable and displayed as Not reported. max_tokens and other incomplete stop reasons remain error states even after SSE done. No fabricated response is preloaded.

Markdown is highlighted and sanitized with DOMPurify. Credentials and raw provider exceptions never enter public responses. The backend allowlists text and usage; configured key occurrences are redacted even across text-delta boundaries. The display timeline caps at 500 entries while final-result handling continues.

## Validation and artifacts

```powershell
npm run typecheck
npm test
npm run build
npm run publish:backend
```

`npm test` runs React tests and C# executable checks using the real SDK with an in-memory HTTP fixture. No paid model call is made. Build regenerates lesson content, compiles Functions and builds React. Outputs:

| Artifact | Directory |
| --- | --- |
| React static website | frontend/dist |
| Azure Functions publish package | artifacts/functions |
| Compiled .NET backend | backend/bin/Release/net10.0 |

There is one npm lockfile and independent NuGet package lockfiles. `npm audit --omit=dev` checks shipped browser dependencies. Local Azure tooling currently has upstream development-only advisories; see VERIFICATION.md. Do not force-downgrade Functions Core Tools or Azurite to incompatible old major versions.

With a **placeholder-only** Functions host running, `npm run verify:http` asserts missing-key health before any POST and checks real HTTP behavior. `node scripts/verify-browser.mjs` checks the running UI in installed Chrome; set PLAYWRIGHT_CHANNEL=msedge to use Edge. Browser verification requires the missing-key host and never calls a paid model. QA screenshots are ignored under verification/.

## Host on Azure

See **DEPLOYMENT.md** and **infra/main.bicep** for separate Azure Functions Flex Consumption and Azure Storage static website hosting. The template enables Entra authentication, managed storage access, HTTPS and a Key Vault reference. React obtains a bearer token with MSAL; it calls the Function App directly so there is no Static Web Apps managed API proxy in the streaming path.

Cloud deployment was not performed. Azure provisioning, tenant app registrations, Key Vault secret entry and publication are separate user commands described in the guide. Creating Azure resources incurs charges. Local lessons remain usable without Azure authentication or a Claude key.

Application requests are limited to 180 seconds, below Azure's documented HTTP response limit. Linked request/invocation/deadline cancellation cleans up stream iteration, and heartbeat comments are sent while waiting. These limits do not guarantee that every model call fits a serverless request; longer jobs need a queued/asynchronous design.

See **VERIFICATION.md** for actual check results and limits, **ASSETS.md** for provenance and documentation sources, and **BUILD-PROMPT.txt** for the reusable standalone specification.
