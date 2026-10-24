@echo off
setlocal
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe
if not exist "%UNITY_EXE%" (
  echo Unity 6000.6.5f1 was not found at:
  echo %UNITY_EXE%
  echo Use Ascendance ^> Build ^> Build Windows Standalone from the Unity Editor instead.
  pause
  exit /b 1
)
"%UNITY_EXE%" -batchmode -quit -projectPath "%~dp0" -executeMethod Ascendance.EditorTools.AscendanceStandaloneBuilder.BuildWindowsStandalone -logFile "%~dp0Builds\Windows\build.log"
if errorlevel 1 (
  echo Build failed. Check Builds\Windows\build.log
  pause
  exit /b 1
)
echo Build complete: Builds\Windows\Ascendance.exe
pause
