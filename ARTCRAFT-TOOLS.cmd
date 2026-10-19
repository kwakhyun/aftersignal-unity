@echo off
setlocal
cd /d "%~dp0"
if /i "%~1"=="photo" goto photo
set "APP=%~dp0Artifacts\Tools\ArtCraft\vectorcraft-0.11.0\vectorcraft-0.11.0-windows-x64-portable\vectorcraft.exe"
if not exist "%APP%" goto missing
start "" "%APP%" "%~dp0Artifacts\ArtCraft\Sources\FacilityGlyphs.vectorcraft"
exit /b
:photo
set "APP=%~dp0Artifacts\Tools\ArtCraft\photocraft-0.9.0\photocraft-0.9.0-windows-x64-portable\photocraft.exe"
if not exist "%APP%" goto missing
start "" "%APP%"
exit /b
:missing
echo Run powershell -File Tools\ArtCraft\Install-ArtCraft.ps1 first.
pause
exit /b 1
