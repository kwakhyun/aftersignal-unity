$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$cacheRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'Library/PackageCache'))
$builtinRoot='C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Data/Resources/PackageManager/BuiltInPackages'
$lock=(Get-Content -LiteralPath (Join-Path $projectRoot 'Packages/packages-lock.json') -Raw | ConvertFrom-Json).dependencies
$log=Get-Content -LiteralPath (Join-Path $projectRoot 'Artifacts/import-retry.log') -Raw
$targets=[regex]::Matches($log,"-> '([^']+PackageCache[^']+)'")
foreach($match in $targets){
  $target=[IO.Path]::GetFullPath($match.Groups[1].Value)
  if(-not $target.StartsWith($cacheRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Cache target outside project'}
  $leaf=Split-Path $target -Leaf
  $packageName=$leaf.Split('@')[0]
  if(Test-Path -LiteralPath (Join-Path $target 'package.json')){continue}
  $bundled=Join-Path $builtinRoot $packageName
  New-Item -ItemType Directory -Force -Path $target | Out-Null
  if(Test-Path -LiteralPath (Join-Path $bundled 'package.json')){
    Get-ChildItem -LiteralPath $bundled -Force | Copy-Item -Destination $target -Recurse -Force
  }else{
    $version=$lock.$packageName.version
    $metadata=Invoke-RestMethod -Uri ('https://packages.unity.com/'+$packageName)
    $tarball=$metadata.versions.$version.dist.tarball
    if(-not $tarball){throw "No published tarball for $packageName $version"}
    $archive=Join-Path $projectRoot ('Artifacts/'+$packageName+'.tgz')
    Invoke-WebRequest -Uri $tarball -OutFile $archive
    tar -xf $archive -C $target --strip-components 1
    if($LASTEXITCODE -ne 0){throw "Extraction failed: $packageName"}
  }
  Write-Output "Prepared local cache: $leaf"
}
