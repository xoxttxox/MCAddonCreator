@echo off
setlocal
cd /d "%~dp0.."

set "PROJECT=src\MCAddonErsteller\MCAddonErsteller.csproj"
set "PUBLISH_DIR=src\MCAddonErsteller\bin\Release\net10.0-windows\win-x64\publish"
set "RELEASE_DIR=release"
set "FINAL_EXE=%RELEASE_DIR%\MC Addon Ersteller.exe"

echo Publishing MC Addon Ersteller as single EXE for win-x64...

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:DebugType=None /p:DebugSymbols=false

if %ERRORLEVEL% neq 0 (
  echo.
  echo Publish fehlgeschlagen.
  pause
  exit /b %ERRORLEVEL%
)

if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"
copy /Y "%PUBLISH_DIR%\MCAddonErsteller.exe" "%FINAL_EXE%" >nul

if %ERRORLEVEL% neq 0 (
  echo.
  echo Konnte EXE nicht nach release kopieren.
  pause
  exit /b %ERRORLEVEL%
)

echo.
echo Fertig. EXE liegt hier:
echo %CD%\%FINAL_EXE%
pause
