param(
    [string]$Editor='C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe',
    [ValidateSet('All','EditMode','PlayMode')][string]$Phase='All',
    [string]$OutputDirectory=''
)
$ErrorActionPreference='Stop'
$audioProjectRoot=Split-Path $PSScriptRoot -Parent
if(-not(Test-Path -LiteralPath $Editor)){throw "Unity editor not found: $Editor"}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $audioProjectRoot ('Artifacts/AudioProduction/Tests/'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))}
$audioOutput=if([System.IO.Path]::IsPathRooted($OutputDirectory)){[System.IO.Path]::GetFullPath($OutputDirectory)}else{[System.IO.Path]::GetFullPath((Join-Path $audioProjectRoot $OutputDirectory))}
New-Item -ItemType Directory -Force -Path $audioOutput | Out-Null
$audioPlatforms=if($Phase -eq 'All'){@('EditMode','PlayMode')}else{@($Phase)}
foreach($audioPlatform in $audioPlatforms){
    $audioResult=Join-Path $audioOutput ($audioPlatform.ToLowerInvariant()+'.xml')
    $audioLog=Join-Path $audioOutput ($audioPlatform.ToLowerInvariant()+'.log')
    if((Test-Path -LiteralPath $audioResult) -or (Test-Path -LiteralPath $audioLog)){throw "Choose a fresh output directory; preserving existing evidence: $audioOutput"}
    $audioFilter=if($audioPlatform -eq 'EditMode'){
        'AfterSignal.Tests.MireloPackTests;AfterSignal.Tests.GeneratedSoundBankTests;AfterSignal.Tests.ProjectContractTests;AfterSignal.Tests.MusicCatalogTests'
    }else{
        'AfterSignal.Tests.MireloPlaybackTests;AfterSignal.Tests.StoryVoicePlaybackTests;AfterSignal.Tests.MusicPlaybackTests'
    }
    $audioArguments=@('-batchmode','-projectPath',('"'+$audioProjectRoot+'"'),'-runTests','-testPlatform',$audioPlatform,'-assemblyNames',('AfterSignal.'+$audioPlatform+'Tests'),'-testFilter',$audioFilter,'-testResults',('"'+$audioResult+'"'),'-logFile',('"'+$audioLog+'"'))
    # PlayMode must initialize graphics and the actual audio device for PCM assertions.
    if($audioPlatform -eq 'EditMode'){$audioArguments+='-nographics'}
    $audioProcess=Start-Process -FilePath $Editor -ArgumentList $audioArguments -WindowStyle Hidden -PassThru
    # Wait only for the editor; its Android ADB service can outlive the test process.
    $audioProcess.WaitForExit()
    if($audioProcess.ExitCode -eq 198){throw "Unity license unavailable. Sign in to Unity Hub and renew the eligible free Personal license, then rerun. Log: $audioLog"}
    if($audioProcess.ExitCode -ne 0){throw "Unity $audioPlatform failed ($($audioProcess.ExitCode)). Results: $audioResult; log: $audioLog"}
    if(-not(Test-Path -LiteralPath $audioResult)){throw "Unity produced no results: $audioLog"}
    [xml]$audioReport=Get-Content -LiteralPath $audioResult -Raw
    $audioRun=$audioReport.'test-run'
    if($audioRun.result -ne 'Passed' -or [int]$audioRun.total -eq 0){throw "Tests did not all pass: $audioResult"}
    Write-Output "$audioPlatform passed: $($audioRun.passed)/$($audioRun.total). Results: $audioResult"
}
