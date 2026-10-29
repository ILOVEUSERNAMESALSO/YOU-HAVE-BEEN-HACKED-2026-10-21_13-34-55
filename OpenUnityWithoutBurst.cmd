@echo off
setlocal EnableExtensions DisableDelayedExpansion
rem Open this project without loading Burst-generated native DLLs (Windows error 4551).
for %%I in ("%~dp0.") do set "CHECKPOINT_PROJECT=%%~fI"
set "CHECKPOINT_VERSION="
if not exist "%CHECKPOINT_PROJECT%\ProjectSettings\ProjectVersion.txt" goto missingProject
for /f "usebackq tokens=1,2" %%A in ("%CHECKPOINT_PROJECT%\ProjectSettings\ProjectVersion.txt") do if "%%A"=="m_EditorVersion:" set "CHECKPOINT_VERSION=%%B"
if not defined CHECKPOINT_VERSION goto missingProject

set "CHECKPOINT_EDITOR=%~1"
if not defined CHECKPOINT_EDITOR set "CHECKPOINT_EDITOR=%ProgramFiles%\Unity\Hub\Editor\%CHECKPOINT_VERSION%\Editor\Unity.exe"
if not exist "%CHECKPOINT_EDITOR%" goto missingEditor

echo Opening Unity %CHECKPOINT_VERSION% with Burst disabled for this session.
echo Close any existing Unity Editor for this project before using this launcher.
start "" "%CHECKPOINT_EDITOR%" -projectPath "%CHECKPOINT_PROJECT%" --burst-disable-compilation
exit /b %errorlevel%

:missingProject
echo Could not read ProjectSettings\ProjectVersion.txt beside this launcher.
echo Keep this file in the root of the downloaded project.
pause
exit /b 1

:missingEditor
echo Unity %CHECKPOINT_VERSION% was not found at:
echo "%CHECKPOINT_EDITOR%"
echo Install that editor version through Unity Hub, or pass its Unity.exe path:
echo OpenUnityWithoutBurst.cmd "D:\Unity\%CHECKPOINT_VERSION%\Editor\Unity.exe"
pause
exit /b 1
