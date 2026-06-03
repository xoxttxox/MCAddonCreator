@echo off
setlocal
cd /d "%~dp0.."

set "PROJECT=src\MCAddonErsteller\MCAddonErsteller.csproj"
set "RELEASE_DIR=release"

echo Publishing MC Addon Ersteller as single EXE for win-x64...

if exist "%RELEASE_DIR%" rmdir /s /q "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%"

dotnet publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -o "%RELEASE_DIR%" ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false

if %ERRORLEVEL% neq 0 (
  echo.
  echo Publish fehlgeschlagen.
  pause
  exit /b %ERRORLEVEL%
)

echo.
echo Fertig. EXE liegt hier:
echo %CD%\%RELEASE_DIR%\MCAddonErsteller.exe
pause