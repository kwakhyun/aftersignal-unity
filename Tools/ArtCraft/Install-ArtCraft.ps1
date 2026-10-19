$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$toolRoot = Join-Path $projectRoot 'Artifacts/Tools/ArtCraft'
New-Item -ItemType Directory -Force -Path $toolRoot | Out-Null
$packages = @(
    @{ Name = 'photocraft'; Version = '0.9.0'; Sha256 = 'e87bbe26f84d3c359765b61d128b9cc59f016a995956fe7909b328a97650fc97' },
    @{ Name = 'vectorcraft'; Version = '0.11.0'; Sha256 = '699a2a257819b7c8a064839b594dc81653a59cc6d10fde5496aae6f1dd294728' }
)
foreach ($package in $packages) {
    $filename = "$($package.Name)-$($package.Version)-windows-x64-portable.zip"
    $archive = Join-Path $toolRoot $filename
    $url = "https://github.com/storytold/$($package.Name)/releases/download/v$($package.Version)/$filename"
    if (!(Test-Path -LiteralPath $archive)) { Invoke-WebRequest -Uri $url -OutFile $archive }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $package.Sha256) {
        throw "Checksum mismatch: $filename. The existing file was not executed."
    }
    $destination = Join-Path $toolRoot "$($package.Name)-$($package.Version)"
    Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force
    $exe = Join-Path $destination "$($package.Name)-$($package.Version)-windows-x64-portable/$($package.Name)-cli.exe"
    & $exe --help | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "CLI failed: $exe" }
    Write-Output "$($package.Name) $($package.Version): ready ($destination)"
}
