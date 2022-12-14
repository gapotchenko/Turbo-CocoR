@echo off

setlocal EnableDelayedExpansion

set SCRIPT_DIR=%~dp0.

set DOTNET=dotnet
if not "%DOTNET_ROOT%" == "" set DOTNET=%DOTNET_ROOT%\%DOTNET%

"%DOTNET%" "%SCRIPT_DIR%\net7.0\any\Gapotchenko.Turbo.CocoR.dll" %*

exit /b %ERRORLEVEL%
