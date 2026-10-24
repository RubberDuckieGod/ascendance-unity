@echo off
setlocal
if "%UNITY_PATH%"=="" set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe
"%UNITY_PATH%" -batchmode -quit -projectPath "%~dp0" -executeMethod Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildWindowsDedicatedServer0100 -logFile "%~dp0Builds\WindowsServer\build.log"
endlocal
