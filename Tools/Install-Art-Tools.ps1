# Install the pinned official Material Maker portable release alongside the project's Blender runtime.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolRoot = Join-Path $projectRoot 'Artifacts/Tools'
New-Item -ItemType Directory -Force $toolRoot | Out-Null
$archive = Join-Path $toolRoot 'material_maker_1_7_windows.zip'
Invoke-WebRequest 'https://github.com/RodZill4/material-maker/releases/download/1.7/material_maker_1_7_windows.zip' -OutFile $archive
$expected = 'DEB4416BC939861D48097A866A8B2BF0363C29FF64874F2E04478658FF900808'
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) { throw 'Material Maker archive checksum mismatch.' }
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $toolRoot 'MaterialMaker-1.7') -Force
Write-Output 'Material Maker 1.7 installed. Use ART-TOOLS.cmd to launch the art tools.'
