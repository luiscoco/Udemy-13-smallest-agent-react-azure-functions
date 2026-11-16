$ErrorActionPreference='Stop'
Push-Location -LiteralPath $PSScriptRoot
try { npm run dev; exit $LASTEXITCODE } finally { Pop-Location }
