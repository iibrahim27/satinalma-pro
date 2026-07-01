@echo off
chcp 65001 >nul
title Satınalma Pro — APK Oluştur
cd /d "%~dp0"

echo.
echo ========================================
echo   Satınalma Pro — Android APK Derleme
echo ========================================
echo.
echo Bu işlem yaklaşık 3-8 dakika sürebilir.
echo Proje yolu Türkçe karakter içeriyorsa otomatik olarak
echo C:\SatinalmaBuild klasörüne kopyalanır.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SatinalmaPro.Mobile\scripts\apk-olustur.ps1"
if errorlevel 1 (
    echo.
    echo APK derlemesi başarısız oldu.
    pause
    exit /b 1
)

echo.
echo Tamamlandı. APK masaüstünüzde: SatinalmaPro.apk
echo (OneDrive Masaüstü dahil otomatik bulunur)
echo.
pause
