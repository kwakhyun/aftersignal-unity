@echo off
set "TASK_PS=powershell"
where pwsh >nul 2>nul
if not errorlevel 1 set "TASK_PS=pwsh"
%TASK_PS% -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Restore-UnityPackages.ps1"
if errorlevel 1 (
  echo Setup failed. See the message above. Existing packages were preserved.
  pause
  exit /b 1
)
echo Ready. Open this folder in Unity 6000.4.0f1 or run OPEN_UNITY.cmd.
