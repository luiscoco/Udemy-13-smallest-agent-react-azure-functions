$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
if (!(Test-Path -LiteralPath (Join-Path $projectRoot 'backend/local.settings.json'))) { throw 'Copy backend/local.settings.example.json to backend/local.settings.json first. See README.md.' }
$env:DEBUG=''
$env:FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT='1'
$env:PATH=(Join-Path $projectRoot 'node_modules/.bin')+[IO.Path]::PathSeparator+$env:PATH
$env:SMALLEST_AGENT_LOCAL_ROOT=Join-Path $projectRoot 'backend'
Push-Location -LiteralPath (Join-Path $projectRoot 'backend')
try { dotnet run --project SmallestAgent.Functions.csproj -- --port 7073; exit $LASTEXITCODE } finally { Pop-Location }
