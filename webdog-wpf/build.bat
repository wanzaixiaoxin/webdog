@echo off
chcp 65001 >nul
title WebDog Build

echo ========================================
echo  WebDog Build Script
echo ========================================
echo.

:: Kill running instance
taskkill /f /im WebDog.exe 2>nul >nul
if %errorlevel% equ 0 (
    echo [OK] Closed running WebDog instance
) else (
    echo [..] No running WebDog instance found
)

:: Restore NuGet packages
echo [..] Restoring packages...
dotnet restore "%~dp0WebDog\WebDog.csproj" --verbosity quiet
if %errorlevel% neq 0 (
    echo [FAIL] Package restore failed
    pause
    exit /b 1
)
echo [OK] Packages restored

:: Build
echo [..] Building...
dotnet build "%~dp0WebDog\WebDog.csproj" -c Debug --verbosity minimal
if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo  BUILD SUCCESS
    echo ========================================
    echo  Output: %~dp0WebDog\bin\Debug\net9.0-windows\WebDog.exe
    echo.
) else (
    echo.
    echo ========================================
    echo  BUILD FAILED - check errors above
    echo ========================================
    echo.
    pause
    exit /b 1
)
