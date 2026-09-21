@echo off
chcp 65001 >nul
echo ========================================================
echo   Dang dang ky CSI SAP2000 27 OAPI vao Windows...
echo ========================================================
echo.

dotnet "C:\Program Files\Computers and Structures\SAP2000 27\RegisterSAP2000.dll"

echo.
echo ========================================================
echo   Hoan tat dang ky! Nhan phim bat ky de thoat.
echo ========================================================
pause
