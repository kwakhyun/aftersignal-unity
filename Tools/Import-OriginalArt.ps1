$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$sourceRoot = Join-Path (Split-Path $projectRoot -Parent) 'public/assets'
$artRoot = Join-Path $projectRoot 'Assets/AfterSignal/Resources/Art'
New-Item -ItemType Directory -Force -Path (Join-Path $artRoot 'Hero'),(Join-Path $artRoot 'Enemies'),(Join-Path $artRoot 'NPC') | Out-Null
for ($i = 0; $i -lt 96; $i++) {
  $set = if ($i -lt 32) { 'seo-v2' } elseif ($i -lt 56) { 'seo-v21' } elseif ($i -lt 80) { 'seo-v22' } elseif ($i -lt 88) { 'seo-v23' } else { 'seo-v40' }
  Copy-Item -LiteralPath (Join-Path $sourceRoot "$set/frame-$i.png") -Destination (Join-Path $artRoot ('Hero/seo-{0:D2}.png' -f $i))
}
Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'enemies-v23') -Filter '*.png' | Copy-Item -Destination (Join-Path $artRoot 'Enemies')
Copy-Item -LiteralPath (Join-Path $sourceRoot 'noa/idle.png') -Destination (Join-Path $artRoot 'NPC/noa.png')
Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'citizens-v40') -Filter '*.png' | Copy-Item -Destination (Join-Path $artRoot 'NPC')
Write-Output 'Copied 96 hero frames, 48 enemy frames and 5 NPCs into the independent Unity project.'
