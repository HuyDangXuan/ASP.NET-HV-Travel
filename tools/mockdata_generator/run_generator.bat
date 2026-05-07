@echo off
chcp 65001 >nul
echo ===================================================
echo CHAY TRINH TAO MOCK DATA VA FIX ANH UNSPLASH
echo ===================================================
echo.
echo [1] Chay tool sinh data goc (khoi phuc cac buoc sinh mac dinh)...
REM Dòng dưới này là lệnh chạy tool gốc. Bạn có thể sửa thành lệnh hay dùng nếu lệnh này báo lỗi
python -m mockdata_generator

echo.
echo [2] Chay tool Patch (Xoa Can Tho, Fix anh mảng 1 phần tử theo location)...
python patch_tour_images.py

echo.
echo ===================================================
echo HOAN THANH TAT CA! (Neu khong thay bao loi mau do la ok)
echo ===================================================
pause
