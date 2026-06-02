@echo off
setlocal
cd /d "%~dp0.."

echo Building MC Addon Ersteller Release...
dotnet build "src\MCAddonErsteller\MCAddonErsteller.csproj" -c Release

if %ERRORLEVEL% neq 0 (
  echo.
  echo Build fehlgeschlagen.
  pause
  exit /b %ERRORLEVEL%
)

echo.
echo Build fertig.
pause
