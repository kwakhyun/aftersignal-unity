param([string]$Label='Release13')
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskMode in @('Rail','Expansion','Residence')){
    $taskOutput=Join-Path $taskProject ('Artifacts/Residence/'+$Label+'/'+$taskMode)
    New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
    $taskArgs=@('-force-d3d11','-force-d3d11-bitblt-model','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-quality-output',('"'+$taskOutput+'"'),'-quality-no-captures','-logFile',('"'+(Join-Path $taskOutput 'player.log')+'"'))
    if($taskMode -eq 'Rail'){$taskArgs+=@('-aftersignal-smoke','-quality-campaign')}
    elseif($taskMode -eq 'Expansion'){$taskArgs+='-expansion-smoke'}
    else{$taskArgs+='-residence-smoke'}
    $taskProcess=Start-Process (Join-Path $taskProject 'Builds/Windows/AFTERSIGNAL.exe') -ArgumentList $taskArgs -WorkingDirectory $taskProject -WindowStyle Normal -Wait -PassThru
    Write-Output ($taskMode+' native exit: '+$taskProcess.ExitCode)
    if($taskProcess.ExitCode -ne 0){throw ($taskMode+' route failed. Inspect '+$taskOutput)}
}
