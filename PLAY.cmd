@echo off
if not exist "%~dp0Builds\Windows\AFTERSIGNAL.exe" (
  echo Build not found. Open this project in Unity or run Tools\Build-Windows.ps1.
  pause
  exit /b 1
)
start "" "%~dp0Builds\Windows\AFTERSIGNAL.exe"
