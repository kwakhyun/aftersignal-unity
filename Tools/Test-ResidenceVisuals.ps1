$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskPython='C:/Users/82105/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(!(Test-Path -LiteralPath $taskPython)){$taskPython=(Get-Command python).Source}
foreach($taskPair in @(@('Residence13Low','15'),@('Residence13High','120'))){
    & $taskPython (Join-Path $PSScriptRoot 'Record-Quality.py') $taskPair[0] --fps $taskPair[1] --no-video --no-captures --d3d11 --vsync 0
    if($LASTEXITCODE -ne 0){throw 'Frame-rate action verification failed.'}
}
foreach($taskStage in @('Haven','School','Clinic','Headquarters')){
    $taskLabel=if($taskStage -eq 'Haven'){'VisualTown'}else{'Visual'+$taskStage}
    & (Join-Path $PSScriptRoot 'Test-Residence.ps1') -Label $taskLabel -Stage $taskStage
}
& $taskPython (Join-Path $PSScriptRoot 'Record-Quality.py') 'Residence13On' --d3d11 --vsync 0
if($LASTEXITCODE -ne 0){throw 'Effects-on action verification failed.'}
& $taskPython (Join-Path $PSScriptRoot 'Record-Quality.py') 'Residence13Off' --effects-off --d3d11 --vsync 0
if($LASTEXITCODE -ne 0){throw 'Effects-off action verification failed.'}
$taskOutput=Join-Path $taskProject 'Artifacts/Residence/RailVisual';New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskEncoder=Join-Path $PSScriptRoot '.python-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
$taskArgs=@('-force-d3d11','-force-d3d11-bitblt-model','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-aftersignal-smoke','-quality-campaign','-quality-output',('"'+$taskOutput+'"'),'-quality-record-stages','Roof','-quality-ffmpeg',('"'+$taskEncoder+'"'),'-logFile',('"'+(Join-Path $taskOutput 'player.log')+'"'))
$taskProcess=Start-Process (Join-Path $taskProject 'Builds/Windows/AFTERSIGNAL.exe') -ArgumentList $taskArgs -WorkingDirectory $taskProject -WindowStyle Normal -Wait -PassThru
if($taskProcess.ExitCode -ne 0){throw 'Boss visual verification failed.'}
