@echo off
echo Building KOZAK RP Launcher Inno Setup Installer...
echo.

REM Check if project is built
if not exist "..\bin\Release\net48\KOZAK RP.exe" (
    echo ERROR: Project not built!
    echo.
    echo Please build the project first:
    echo   dotnet build "..\KOZAK RP.csproj" --configuration Release
    echo.
    pause
    exit /b 1
)

REM Check for Inno Setup
set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%INNO_PATH%" (
    set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
)

if not exist "%INNO_PATH%" (
    echo ERROR: Inno Setup not found!
    echo.
    echo Please install Inno Setup:
    echo   1. Download from: https://jrsoftware.org/isdl.php
    echo   2. Install Inno Setup 6
    echo   3. Run this script again
    echo.
    pause
    exit /b 1
)

REM Build installer
echo Building installer with Inno Setup...
"%INNO_PATH%" "installer_script.iss"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo SUCCESS! Installer created in installer_output folder
    echo.
) else (
    echo.
    echo ERROR: Failed to build installer
    echo.
)

pause
