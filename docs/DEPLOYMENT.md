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

## Bước 2 - Copy & đặt mật khẩu NGAY TRÊN máy trạm
1. Copy **chỉ file** `ExamGuard.exe` vào thư mục ổn định trên máy trạm,
   VD: `C:\Program Files\ExamGuard\`.
   (Chỉ cần quyền ghi thư mục đó vì app ghi file cấu hình cạnh exe.)
2. Trên **chính máy trạm đó**, chạy lệnh sau (1 lần) để đặt mật khẩu giáo viên:
   ```powershell
   ExamGuard.exe --init
   ```
   Nhập mật khẩu (VD: `GvLab@2026`). App tự sinh `examguard.json` ngay tại chỗ.

> **Vì sao phải tạo mật khẩu ngay trên máy trạm?** Ai chạy `--init` là người duy nhất
> biết mật khẩu. Nếu tạo sẵn config ở máy khác rồi copy sang, người tạo cũng biết mật
> khẩu — không còn bí mật với giáo viên máy trạm. `examguard.json` chỉ chứa hash + salt,
> không đọc ngược ra được mật khẩu.

> Muốn đổi sau này: chạy `--init` lại (không cần mật khẩu cũ) hoặc qua hotkey → "Đổi mật khẩu".

## Bước 3 - Bật tự khởi động
Chạy `ExamGuard.exe --service` một lần để bật autostart
(viết registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) và đăng ký
**Task Scheduler** `ExamGuardWatchdog` (chạy mỗi phút, dùng để cứu hệ thống khi
cả service lẫn watchdog bị giết).
Đăng xuất/đăng nhập lại → ExamGuard tự chạy ẩn cùng phiên làm việc.

> Nếu chưa có `examguard.json` mà chạy `--service` luôn, app cũng tự hiện hộp thoại
> **Cài đặt mật khẩu** — tương đương `--init`.

## Bước 4 - Kiểm tra nhanh tại chỗ
| Kiểm tra | Kết quả mong đợi |
|---|---|
| Không thấy cửa sổ/tray nào mới | OK (ẩn) |
| Notepad: Ctrl+C/Ctrl+V | Không copy/dán được text |
| Explorer: copy/paste file | Hoạt động bình thường |
| Nhấn `Ctrl+Alt+Shift+G` | Hiện hộp thoại mật khẩu |
| Task Manager → chuột phải `ExamGuard` → End task | **"Không truy cập được" (bị từ chối)** — lớp unkillable |

> Kiểm tra nhanh lớp unkillable bằng lệnh (mở bằng user thường, không phải admin):
> `powershell -c "Get-Process ExamGuard -ErrorAction Stop | Stop-Process -Force -ErrorAction Stop"`
> → phải báo lỗi *access denied*; nếu nó bị tắt thì watchdog sẽ khởi động lại sau ~2-8s.

## Giờ thi / khi giao máy
- Máy đã có sẵn autostart → mở máy là chặn.
- Nếu máy đang mở khóa: giáo viên nhấn hotkey, nhập mật khẩu, chọn hành động
  (chương trình khóa lại tự động sau thời gian mở khóa).

## Khi hết giờ / muốn tắt
- Nhấn `Ctrl+Alt+Shift+G` → nhập mật khẩu → **Thoát hẳn**.
- Thao tác này ghi cờ dừng (watchdog không khởi động lại), xóa task watchdog **và**
  xóa autostart trong registry `Run` → máy reboot sẽ **không** tự chặn lại nữa.
- Muốn kích hoạt lại: chạy tay `ExamGuard.exe --service` (tự bật lại autostart + watchdog).

## Bảo trì
- **Đổi mật khẩu**: hotkey → Đổi mật khẩu (cần mật khẩu hiện tại).
- **Nâng cấp bản mới**: copy file exe mới đè lên, giữ `examguard.json`.
- **Tạm tắt lớp unkillable (bảo trì)**: sửa `"Unkillable": false` trong `examguard.json` rồi khởi động lại service. Nếu không làm trước, service sẽ **không thể bị `taskkill`/End task** bởi user thường.
- **Gỡ cài đặt**: xóa thư mục + xóa giá trị `ExamGuard` trong
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` + xóa task
  `ExamGuardWatchdog` (lệnh `schtasks /Delete /F /TN ExamGuardWatchdog`).

## Lưu ý
- Nếu máy trạm chạy antivirus: cấu hình ngoại lệ cho thư mục cài đặt nếu bị chặn
  (low-level hook + exe self-contained đôi khi bị nhận nhầm).
- Nếu chạy với quyền admin (`runas`) trên máy trạm, mọi thao tác text trong app
  admin sẽ không bị chặn — giáo viên nên chạy quyền thường khi phát đề.
- Lớp unkillable (DACL) **không chặn được user có quyền admin**: admin vẫn có thể
  lấy quyền sở hữu / đặt lại DACL (Windows cho phép). Với phòng thực hành (học viên
  không có quyền admin) lớp này là đủ; muốn xoá hoàn toàn hãy tắt `Unkillable`.
