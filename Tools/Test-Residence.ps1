param([string]$Label='ResidenceQA',[string]$Stage='', [switch]$Video, [switch]$NoCaptures)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskOutput=Join-Path $taskProject ('Artifacts/Residence/'+$Label)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskArgs=@('-force-d3d11','-force-d3d11-bitblt-model','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-residence-smoke','-quality-output',('"'+$taskOutput+'"'),'-logFile',('"'+(Join-Path $taskOutput 'player.log')+'"'))
if($Stage){$taskArgs+=@('-residence-only',$Stage)}
if($NoCaptures){$taskArgs+='-quality-no-captures'}
if($Video){$taskEncoder=Join-Path $PSScriptRoot '.python-deps/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe';if(!(Test-Path -LiteralPath $taskEncoder)){throw 'Install imageio-ffmpeg or supply the existing ffmpeg executable path.'};$taskArgs+=@('-quality-ffmpeg',('"'+$taskEncoder+'"'))}
$taskProcess=Start-Process (Join-Path $taskProject 'Builds/Windows/AFTERSIGNAL.exe') -ArgumentList $taskArgs -WorkingDirectory $taskProject -WindowStyle Normal -PassThru -Wait
Write-Output ('Native QA exit: '+$taskProcess.ExitCode+' / '+$taskOutput)
if($taskProcess.ExitCode -ne 0){throw 'Native QA failed. Inspect residence.json and player.log.'}
