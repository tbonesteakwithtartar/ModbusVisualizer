@echo off
REM Build script for ModbusVisualizer - Creates standalone executables for Windows

setlocal enabledelayedexpansion

echo.
echo =========================================
echo ModbusVisualizer Build Script
echo =========================================

REM Configuration
set "PROJECT_DIR=%~dp0ModbusVisualizer"
set "BUILD_DIR=%~dp0bin\publish"
set "RUNTIME=win-x64"

echo.
echo Project Directory: %PROJECT_DIR%
echo Build Output: %BUILD_DIR%
echo.

REM Clean previous build
echo Cleaning previous build...
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"

REM Restore dependencies
echo Restoring NuGet packages...
cd /d "%PROJECT_DIR%"
dotnet restore
if errorlevel 1 (
    echo Error restoring packages
    exit /b 1
)

REM Build Release
echo Building Release configuration...
dotnet build --configuration Release --no-restore
if errorlevel 1 (
    echo Error building project
    exit /b 1
)

REM Publish as self-contained single-file executable
echo Publishing self-contained executable...
dotnet publish ^
  --configuration Release ^
  --runtime %RUNTIME% ^
  --self-contained ^
  -p:PublishSingleFile=true ^
  -p:PublishTrimmed=true ^
  -p:PublishReadyToRun=true ^
  --output "%BUILD_DIR%"

if errorlevel 1 (
    echo Error publishing executable
    exit /b 1
)

echo.
echo =========================================
echo Build Complete!
echo =========================================
echo.
echo Executable Location:
echo   %BUILD_DIR%\ModbusVisualizer.exe
echo.
if exist "%BUILD_DIR%\ModbusVisualizer.exe" (
    for /F "usebackq" %%A in ('%BUILD_DIR%\ModbusVisualizer.exe') do set "FILESIZE=%%~zA"
    echo File Size: %FILESIZE% bytes
)
echo.
echo To run:
echo   %BUILD_DIR%\ModbusVisualizer.exe
echo.
endlocal
