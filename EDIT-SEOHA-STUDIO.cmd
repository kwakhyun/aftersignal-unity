@echo off
setlocal
set "SEOHA_EDITOR=%~dp0Artifacts\Tools\blender-4.5.3-windows-x64\blender.exe"
set "SEOHA_SOURCE=%~dp0Artifacts\SeohaStudio\VRoid\Likeness\Seoha-Likeness-Review.blend"
if not exist "%SEOHA_EDITOR%" (
  echo Blender 4.5.3 is not installed at the local production-tool path.
  pause
  exit /b 1
)
if not exist "%SEOHA_SOURCE%" (
  echo The private Seoha studio source is missing. See Documentation\SeohaStudio\README.md.
  pause
  exit /b 1
)
start "" "%SEOHA_EDITOR%" "%SEOHA_SOURCE%"
