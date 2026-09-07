param([string]$Label='CityUpgrade',[switch]$Video)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskOutput=Join-Path $taskProject ('Artifacts/Urban15/'+$Label)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskArgs=@('-force-d3d11','-force-d3d11-bitblt-model','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-city-upgrade-smoke','-quality-output',('"'+$taskOutput+'"'),'-logFile',('"'+$taskOutput+'/player.log"'))
if($Video){$taskArgs+=@('-quality-record-stages','UrbanCity','-quality-ffmpeg',('"'+(Join-Path $PSScriptRoot '.python-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')+'"'))}
$taskProcess=Start-Process (Join-Path $taskProject 'Builds/Windows/AFTERSIGNAL.exe') -ArgumentList $taskArgs -WorkingDirectory $taskProject -WindowStyle Normal -PassThru -Wait
Write-Output ('Native city upgrade QA exit: '+$taskProcess.ExitCode)
if($taskProcess.ExitCode -ne 0){throw 'City QA failed. Inspect city-upgrade.json and player.log.'}
