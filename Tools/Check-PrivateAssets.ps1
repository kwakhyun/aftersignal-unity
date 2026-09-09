$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$required=@(
    'Assets/AfterSignal/Scenes/19_SeoResidence.unity',
    'Assets/AfterSignal/Scenes/24_OpenCity.unity',
    'Assets/AfterSignal/Prefabs/Seo.prefab',
    'Assets/AfterSignal/Resources/Art/Portraits/SeoDialogue.png',
    'Assets/AfterSignal/Resources/Art/Title/AfterlightTitle.png',
    'Assets/AfterSignal/Resources/WorldAssets/MobilityDistricts.prefab',
    'Assets/AfterSignal/Resources/Geometry'
)
$missing=@($required | Where-Object {-not(Test-Path -LiteralPath (Join-Path $projectRoot $_))})
if($missing.Count){throw ('Private visual assets are missing. Restore the matching private asset pack including .meta files. See Documentation/PRIVATE-ASSETS.md. Missing: '+($missing -join ', '))}
Write-Output 'Required private visual asset entry points are present. Unity import/build validates their references.'
