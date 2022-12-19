@ECHO OFF

setlocal

rem start with editor install layout
set CSC=%~dp0..\..\Tools\Roslyn\csc.exe

rem fall back to source tree layout
if not exist "%CSC%" set CSC=%~dp0..\..\..\..\artifacts\buildprogram\Stevedore\roslyn-csc-win64\csc.exe

if not exist "%CSC%" (
	echo Failed to find csc.exe
	exit /b 1
)

set WRAPPER=%~dp0\csc.wrapper.exe
"%WRAPPER%" %*
if errorlevel 228 (
	"%CSC%" /shared %*
)

exit /b %ERRORLEVEL%

endlocal
