@echo off
cd /d "%~dp0"
set "trinh_bien_dich=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%trinh_bien_dich%" (
    echo Khong tim thay csc.exe x64 tai %trinh_bien_dich%
    pause
    exit /b 1
)
"%trinh_bien_dich%" /nologo /platform:x64 /target:winexe /optimize+ ^
  /out:..\ForgeTaxMod.exe ^
  /win32manifest:yeu_cau_admin.manifest ^
  /win32icon:bieu_tuong.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  chuong_trinh.cs ngon_ngu.cs dong_co_bo_nho.cs dieu_khien_tuy_chinh.cs giao_dien_chinh.cs
if %errorlevel% neq 0 (
    echo Bien dich that bai!
    pause
    exit /b %errorlevel%
)
echo Da bien dich thanh cong ..\ForgeTaxMod.exe
exit /b 0
