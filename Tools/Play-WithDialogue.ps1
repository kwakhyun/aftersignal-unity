param([string]$Python)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$gamePath=Join-Path $taskRoot 'Builds/Windows/AFTERSIGNAL.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw 'Build the Unity Windows player first: Tools/Build-Windows.ps1 -Release' }
$gatewayProcess=$null
$running=$false
try { $health=Invoke-RestMethod -Uri 'http://127.0.0.1:8766/health' -TimeoutSec 2; $running=$health.model -eq 'gpt-5.6-luna' } catch {}
if (-not $running) {
    if (-not $Python) {
        $candidates=@((Join-Path $taskRoot '.venv/Scripts/python.exe'),(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'))
        foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate) { $Python=$candidate; break } }
        if (-not $Python) { $command=Get-Command python.exe -ErrorAction SilentlyContinue; if ($command -and $command.Source -notlike '*WindowsApps*') { $Python=$command.Source } }
    }
    if ($Python) {
        New-Item -ItemType Directory -Force -Path (Join-Path $taskRoot 'Artifacts/CityLife') | Out-Null
        $gatewayProcess=Start-Process -FilePath $Python -ArgumentList @(('"'+(Join-Path $PSScriptRoot 'NpcGateway.py')+'"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskRoot 'Artifacts/CityLife/gateway.log') -RedirectStandardError (Join-Path $taskRoot 'Artifacts/CityLife/gateway-error.log')
    }
}
try { $gameProcess=Start-Process -FilePath $gamePath -WorkingDirectory $taskRoot -PassThru; $gameProcess.WaitForExit() }
finally { if ($gatewayProcess -and -not $gatewayProcess.HasExited) { Stop-Process -Id $gatewayProcess.Id -ErrorAction SilentlyContinue } }
