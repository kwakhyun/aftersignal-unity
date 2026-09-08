@echo off
cd /d "%~dp0"
echo 1. Blender - 3D modelling
 echo 2. Material Maker - PBR materials
choice /c 12 /n /m "Select 1 or 2: "
if errorlevel 2 (
 start "Material Maker" "Artifacts\Tools\MaterialMaker-1.7\material_maker_1_7_windows\material_maker.exe"
) else (
 start "Blender" "Artifacts\Tools\blender-4.5.3-windows-x64\blender.exe"
)
