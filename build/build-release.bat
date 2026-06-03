@echo off
setlocal
cd /d "%~dp0.."

echo Restoring...
dotnet restore "src\MCAddonErsteller\MCAddonErsteller.csproj"

if %ERRORLEVEL% neq 0 (
  echo.
  echo Restore fehlgeschlagen.
  pause
  exit /b %ERRORLEVEL%
)

echo Building MC Addon Ersteller Release...
dotnet build "src\MCAddonErsteller\MCAddonErsteller.csproj" -c Release --no-restore

if %ERRORLEVEL% neq 0 (
  echo.
  echo Build fehlgeschlagen.
  pause
  exit /b %ERRORLEVEL%
)

echo.
echo Build fertig.
pause