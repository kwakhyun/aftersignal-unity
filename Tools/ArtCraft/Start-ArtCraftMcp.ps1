param([ValidateSet('photocraft','vectorcraft')][string]$Tool = 'photocraft')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$version = if ($Tool -eq 'photocraft') { '0.9.0' } else { '0.11.0' }
$exe = Join-Path $projectRoot "Artifacts/Tools/ArtCraft/$Tool-$version/$Tool-$version-windows-x64-portable/$Tool-cli.exe"
$work = Join-Path $projectRoot 'Artifacts/ArtCraft'
if (!(Test-Path -LiteralPath $exe)) { throw 'Run Install-ArtCraft.ps1 first.' }
New-Item -ItemType Directory -Force -Path $work | Out-Null
# stdout is reserved for MCP JSON-RPC; do not print banners.
& $exe mcp --automation-read-root $work --automation-write-root $work
exit $LASTEXITCODE
