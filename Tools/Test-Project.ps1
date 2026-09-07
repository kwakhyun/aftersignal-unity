param([string]$Editor='C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
foreach($platform in @('EditMode','PlayMode')){
  $argsList=@('-batchmode','-nographics','-accept-apiupdate','-projectPath',('"'+$projectRoot+'"'),'-runTests','-testPlatform',$platform,'-assemblyNames',("AfterSignal.$platform"+'Tests'),'-testResults',('"'+(Join-Path $projectRoot "Artifacts/$platform.xml")+'"'),'-logFile',('"'+(Join-Path $projectRoot "Artifacts/$platform.log")+'"'))
  $process=Start-Process -FilePath $Editor -ArgumentList $argsList -WindowStyle Hidden -Wait -PassThru
  if($process.ExitCode -ne 0){throw "Unity $platform tests failed. See Artifacts/$platform.log."}
}
