param(
    [string]$Editor='C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe',
    [string]$ProjectRoot=(Split-Path $PSScriptRoot -Parent)
)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($ProjectRoot)
$taskSources=Get-Content -LiteralPath (Join-Path $taskRoot 'Packages/sources.json') -Raw | ConvertFrom-Json
if(-not(Test-Path -LiteralPath $Editor)){throw ('Install Unity '+$taskSources.unity+' through Unity Hub first, or pass -Editor.')}
$taskEditorRoot=Split-Path $Editor -Parent
$taskBuiltin=Join-Path $taskEditorRoot 'Data/Resources/PackageManager/BuiltInPackages'
$taskCache=Join-Path $taskRoot 'Tools/.package-downloads'
New-Item -ItemType Directory -Force -Path $taskCache | Out-Null
foreach($taskPackage in $taskSources.packages){
    $taskTarget=Join-Path $taskRoot ('Packages/'+$taskPackage.name)
    $taskJson=Join-Path $taskTarget 'package.json'
    if(Test-Path -LiteralPath $taskTarget){
        if(-not(Test-Path -LiteralPath $taskJson)){throw ('Incomplete existing package: '+$taskTarget+'. Preserve it and inspect before retrying.')}
        $taskExisting=Get-Content -LiteralPath $taskJson -Raw | ConvertFrom-Json
        if($taskExisting.version -ne $taskPackage.version){throw ('Package version differs: '+$taskPackage.name+'. No files were overwritten.')}
        Write-Output ('Present: '+$taskPackage.name+' '+$taskPackage.version)
        continue
    }
    if($taskPackage.source -eq 'editor'){
        $taskSource=Join-Path $taskBuiltin $taskPackage.directory
        $taskBundled=Get-Content -LiteralPath (Join-Path $taskSource 'package.json') -Raw | ConvertFrom-Json
        if($taskBundled.version -ne $taskPackage.version){throw ('Use Unity '+$taskSources.unity+'; bundled '+$taskPackage.name+' has a different version.')}
        Copy-Item -LiteralPath $taskSource -Destination $taskTarget -Recurse
    }else{
        $taskUri=[Uri]$taskPackage.url
        if($taskUri.Scheme -ne 'https' -or $taskUri.Host -ne 'download.packages.unity.com'){throw 'Unexpected package origin'}
        $taskArchive=Join-Path $taskCache ($taskPackage.name+'-'+$taskPackage.version+'.tgz')
        if(-not(Test-Path -LiteralPath $taskArchive)){Invoke-WebRequest -UseBasicParsing -Uri $taskPackage.url -OutFile $taskArchive}
        if((Get-FileHash -LiteralPath $taskArchive -Algorithm SHA1).Hash.ToLowerInvariant() -ne $taskPackage.sha1){throw ('Package checksum mismatch: '+$taskArchive)}
        $taskEntries=& tar -tzf $taskArchive
        if($LASTEXITCODE -ne 0){throw 'Could not inspect package archive'}
        foreach($taskEntry in $taskEntries){if($taskEntry -notmatch '^package(/|$)' -or $taskEntry -match '(^|[/\\])\.\.([/\\]|$)' -or $taskEntry.Contains(':')){throw 'Unexpected archive path'}}
        $taskExtract=Join-Path $taskCache ([Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $taskExtract | Out-Null
        & tar -xzf $taskArchive -C $taskExtract
        if($LASTEXITCODE -ne 0){throw 'Package extraction failed'}
        $taskSource=Join-Path $taskExtract 'package'
        $taskDownloaded=Get-Content -LiteralPath (Join-Path $taskSource 'package.json') -Raw | ConvertFrom-Json
        if($taskDownloaded.name -ne $taskPackage.name -or $taskDownloaded.version -ne $taskPackage.version){throw 'Downloaded package identity differs'}
        Copy-Item -LiteralPath $taskSource -Destination $taskTarget -Recurse
    }
    Write-Output ('Restored: '+$taskPackage.name+' '+$taskPackage.version)
}
$taskCompatibility=Join-Path $taskRoot 'Packages/compatibility.json'
if(Test-Path -LiteralPath $taskCompatibility){
    $taskAdjustments=Get-Content -LiteralPath $taskCompatibility -Raw | ConvertFrom-Json
    foreach($taskAdjustment in $taskAdjustments.files){
        $taskPath=[IO.Path]::GetFullPath((Join-Path $taskRoot $taskAdjustment.path))
        $taskPackageRoot=[IO.Path]::GetFullPath((Join-Path $taskRoot 'Packages'))+[IO.Path]::DirectorySeparatorChar
        if(-not $taskPath.StartsWith($taskPackageRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Adjustment outside Packages'}
        $taskHash=if(Test-Path -LiteralPath $taskPath){(Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash.ToLowerInvariant()}else{$null}
        if($taskHash -eq $taskAdjustment.afterSha256){continue}
        if($taskHash -ne $taskAdjustment.beforeSha256){throw ('Existing package change preserved; inspect '+$taskAdjustment.path)}
        [IO.Directory]::CreateDirectory((Split-Path $taskPath -Parent)) | Out-Null
        [IO.File]::WriteAllText($taskPath,$taskAdjustment.content,[Text.UTF8Encoding]::new($false))
        if((Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $taskAdjustment.afterSha256){throw 'Compatibility checksum mismatch'}
    }
}
Write-Output 'Pinned packages and recorded compatibility adjustments are ready. Unrecognized local edits were preserved.'
