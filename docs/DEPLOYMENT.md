# Hướng dẫn triển khai (DEPLOYMENT)

## Yêu cầu máy trạm
- Windows 10/11 x64.
- Không cần cài .NET (bản build self-contained).

## Bước 1 - Chuẩn bị bản build
Trên máy phát triển:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
```
Lấy file: `artifacts\ExamGuard-win-x64\ExamGuard.exe` (1 file duy nhất).

## Bước 2 - Cài đặt mật khẩu giáo viên (1 lần, máy phát triển)
Trước khi copy sang máy trạm, tạo mật khẩu trước:
```powershell
ExamGuard.exe --init
```
Đặt mật khẩu (VD: `GvLab@2026`). Sinh ra `examguard.json` cùng thư mục.

> Muốn đổi sau này: chạy `--init` lại hoặc qua hotkey → "Đổi mật khẩu".

## Bước 3 - Copy & bật tự khởi động
1. Copy `ExamGuard.exe` + `examguard.json` vào thư mục ổn định trên máy trạm,
   VD: `C:\Program Files\ExamGuard\`.
   (Chỉ cần quyền ghi thư mục đó vì app ghi file cấu hình cạnh exe.)
2. Chạy `ExamGuard.exe --service` một lần để bật autostart
   (viết registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) và đăng ký
   **Task Scheduler** `ExamGuardWatchdog` (chạy mỗi phút, dùng để cứu hệ thống khi
   cả service lẫn watchdog bị giết).
3. Đăng xuất/đăng nhập lại → ExamGuard tự chạy ẩn cùng phiên làm việc.

## Bước 4 - Kiểm tra nhanh tại chỗ
| Kiểm tra | Kết quả mong đợi |
|---|---|
| Không thấy cửa sổ/tray nào mới | OK (ẩn) |
| Notepad: Ctrl+C/Ctrl+V | Không copy/dán được text |
| Explorer: copy/paste file | Hoạt động bình thường |
| Nhấn `Ctrl+Alt+Shift+G` | Hiện hộp thoại mật khẩu |

## Giờ thi / khi giao máy
- Máy đã có sẵn autostart → mở máy là chặn.
- Nếu máy đang mở khóa: giáo viên nhấn hotkey, nhập mật khẩu, chọn hành động
  (chương trình khóa lại tự động sau thời gian mở khóa).

## Khi hết giờ / muốn tắt
- Nhấn `Ctrl+Alt+Shift+G` → nhập mật khẩu → **Thoát**.
- Thao tác này cũng ghi cờ dừng để watchdog không khởi động lại.

## Bảo trì
- **Đổi mật khẩu**: hotkey → Đổi mật khẩu (cần mật khẩu hiện tại).
- **Nâng cấp bản mới**: copy file exe mới đè lên, giữ `examguard.json`.
- **Gỡ cài đặt**: xóa thư mục + xóa giá trị `ExamGuard` trong
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` + xóa task
  `ExamGuardWatchdog` (lệnh `schtasks /Delete /F /TN ExamGuardWatchdog`).

## Lưu ý
- Nếu máy trạm chạy antivirus: cấu hình ngoại lệ cho thư mục cài đặt nếu bị chặn
  (low-level hook + exe self-contained đôi khi bị nhận nhầm).
- Nếu chạy với quyền admin (`runas`) trên máy trạm, mọi thao tác text trong app
  admin sẽ không bị chặn — giáo viên nên chạy quyền thường khi phát đề.
