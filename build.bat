@echo off
chcp 65001 >nul
title Building Fortnite Chest Tracker (FCT)
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo The .NET 10 SDK is not installed.
  echo Download it here: https://dotnet.microsoft.com/download/dotnet/10.0  ^(".NET SDK", Windows x64^)
  echo Then run build.bat again.
  pause
  exit /b 1
)

rem FCT running from dist\FCT.exe (the window, or its standby process for "Open with Fortnite") locks the file and
rem the build cannot replace it: close it first (politely, then by force). Data is saved atomically, so nothing is lost.
rem The new FCT launched at the end starts the standby process again.
set "FCT_EXE=%~dp0dist\FCT.exe"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$exe=[IO.Path]::GetFullPath($env:FCT_EXE); $p=@(Get-Process -Name FCT -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }); if ($p.Count) { Write-Host 'Closing the running FCT so dist\FCT.exe can be replaced...'; $p | ForEach-Object { taskkill /PID $_.Id 2>&1 | Out-Null }; Start-Sleep -Seconds 3; $p | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }; Start-Sleep -Seconds 1 }"

echo Building (1 to 2 minutes the first time)...
dotnet publish src\FCT.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o dist
if errorlevel 1 (
  echo.
  echo The build failed. See the message above.
  pause
  exit /b 1
)

echo.
echo Done! Launching %~dp0dist\FCT.exe
start "" "%~dp0dist\FCT.exe"
