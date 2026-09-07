@echo off
set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo Unity 6000.4.0f1 was not found. Add this folder in Unity Hub.
  pause
  exit /b 1
)
set "TASK_PS=powershell"
where pwsh >nul 2>nul
if not errorlevel 1 set "TASK_PS=pwsh"
%TASK_PS% -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Restore-UnityPackages.ps1" -Editor "%UNITY_EDITOR%"
if errorlevel 1 exit /b 1
start "" "%UNITY_EDITOR%" -projectPath "%~dp0."
