@echo off
chcp 65001 >nul
title Satınalma Pro — APK Oluştur
cd /d "%~dp0"

echo.
echo Satınalma Pro APK derleniyor...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0apk-olustur.ps1"
if errorlevel 1 (
    echo.
    echo APK derlemesi başarısız oldu.
    pause
    exit /b 1
)

echo.
pause
