@echo off
if not exist "%~dp0Builds\Windows\AFTERSIGNAL.exe" (
  echo Build not found. Open this project in Unity or run Tools\Build-Windows.ps1.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Play-WithDialogue.ps1"
