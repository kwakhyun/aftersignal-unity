param([string]$Editor='C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe',[switch]$Release)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Check-PrivateAssets.ps1')
if(-not(Test-Path -LiteralPath $Editor)){throw "Unity 6000.4.0f1 editor not found: $Editor"}
& (Join-Path $PSScriptRoot 'Restore-UnityPackages.ps1') -Editor $Editor -ProjectRoot $projectRoot
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Artifacts') | Out-Null
$buildMethod=if($Release){'AfterSignal.Editor.ProjectBuilder.BuildRelease'}else{'AfterSignal.Editor.ProjectBuilder.BuildWindows'}
$arguments=@('-batchmode','-quit','-projectPath',('"'+$projectRoot+'"'),'-executeMethod',$buildMethod,'-logFile',('"'+(Join-Path $projectRoot 'Artifacts/build.log')+'"'))
$process=Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Unity build failed. See Artifacts/build.log.'}
Write-Output (Join-Path $projectRoot 'Builds/Windows/AFTERSIGNAL.exe')
