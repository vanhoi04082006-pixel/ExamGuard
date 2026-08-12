# Hướng dẫn giáo viên sử dụng ExamGuard

Tài liệu này dành cho giáo viên quản lý phòng thực hành lập trình. Mọi thao tác
đều thực hiện trên máy Windows (10/11 x64), không cần kiến thức lập trình.

## 1. Các khái niệm

- **Máy phát triển**: máy dùng để build ra file `ExamGuard.exe` (chỉ làm **một lần**).
  Không cần đặt mật khẩu ở đây.
- **Máy trạm**: máy học viên trong phòng thực hành. Copy **chỉ file** `ExamGuard.exe`
  vào, rồi **đặt mật khẩu ngay trên máy đó** (`--init`); từ đó mọi copy/paste văn bản
  (code) đều bị chặn.

## 2. Chuẩn bị file cài đặt (máy phát triển, làm 1 lần)

1. Mở **PowerShell**, đến thư mục dự án, chạy:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
   ```
2. Lấy file kết quả: `artifacts\ExamGuard-win-x64\ExamGuard.exe`.

## 3. Triển khai lên từng máy trạm

1. Copy **chỉ file** `ExamGuard.exe` vào thư mục ổn định trên máy trạm,
   VD: `C:\Program Files\ExamGuard\` (thư mục phải có quyền ghi vì app lưu
   cấu hình ngay cạnh exe).
2. **Đặt mật khẩu ngay trên máy này** (làm 1 lần):
   ```powershell
   ExamGuard.exe --init
   ```
   Nhập mật khẩu (VD: `GvLab@2026`). App tự sinh `examguard.json` tại chỗ.
   > Ai chạy `--init` là người duy nhất biết mật khẩu. Đừng copy config tạo sẵn
   > từ máy khác — người tạo sẽ biết mật khẩu.
   > Nếu quên mật khẩu: chạy `--init` lại để đặt mật khẩu mới (không cần mật khẩu cũ).
3. Chạy `ExamGuard.exe --service` **một lần**. Lệnh này:
   - Đăng ký tự khởi động cùng phiên đăng nhập (registry `Run`).
   - Đăng ký Task Scheduler `ExamGuardWatchdog` (chạy mỗi phút, cứu khi cả
     service lẫn watchdog bị tắt).
   - Kích hoạt chống kill (DACL): mọi user thường **không thể** kết thúc tiến trình.
4. Đăng xuất rồi đăng nhập lại (hoặc khởi động lại máy) → ExamGuard chạy ẩn
   và tự chặn từ khi vào hệ thống.

## 4. Kiểm tra nhanh trước giờ thi

| Kiểm tra | Kết quả mong đợi |
|---|---|
| Mở máy, không thấy cửa sổ/tray mới | OK (ẩn) |
| Notepad gõ vài chữ → `Ctrl+C`, `Ctrl+V` | Không copy/dán được text |
| File Explorer → copy/paste file | Hoạt động bình thường |
| Mở Task Manager → chuột phải `ExamGuard` → End task | Bị từ chối ("không truy cập được") |
| Nhấn `Ctrl+Alt+Shift+G` | Hộp thoại mật khẩu hiện |

## 5. Thao tác trong giờ thi

Mọi thao tác đều bắt đầu bằng tổ hợp phím bí mật **`Ctrl+Alt+Shift+G`**
(nếu bận, chương trình tự dùng `Ctrl+Alt+G` hoặc `Ctrl+Shift+G`). Hộp thoại có
4 nút: **Mở khóa** (kèm ô chọn thời gian), **Thoát hẳn**, **Đổi mật khẩu** và
**Xóa toàn bộ**.

| Nhu cầu | Cách làm |
|---|---|
| Cho sinh viên copy/paste tạm thời | Hotkey → nhập mật khẩu → chọn thời gian (5/10/15/30/60/120 phút) → **Mở khóa** (hết giờ tự khóa lại) |
| Hết giờ, tắt hẳn | Hotkey → nhập mật khẩu → **Thoát hẳn** (xóa autostart + watchdog; reboot không tự chạy lại) |
| Đổi mật khẩu | Hotkey → nhập mật khẩu hiện tại → **Đổi mật khẩu** |
| Khi máy đang mở khóa | Lặp lại hotkey + mật khẩu → chọn thời gian → **Mở khóa** để gia hạn |
| Xóa toàn bộ ExamGuard | Hotkey → nhập mật khẩu → **Xóa toàn bộ** → xác nhận (xóa mọi file, cấu hình, autostart, watchdog trên máy này) |

## 6. Bảo trì

- **Nâng cấp bản mới**: copy file exe mới đè lên, **giữ nguyên** `examguard.json`.
- **Tạm tắt chống kill để bảo trì**: sửa `"Unkillable": false` trong
  `examguard.json` rồi chạy lại `--service`. (Khi đang bật, user thường không
  thể `taskkill`/End task service — phải tắt trước khi muốn tự kết thúc.)
- **Gỡ cài đặt hẳn**: xóa thư mục cài đặt + xóa giá trị `ExamGuard` trong
  registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` + xóa task:
  ```
  schtasks /Delete /F /TN ExamGuardWatchdog
  ```

## 7. Những điều quan trọng cần biết

- **Test bằng user thường**: chống kill (End task) chỉ chặn được user **không
  phải admin**. Nếu giáo viên đang đăng nhập bằng quyền admin thì kill được —
  đó là giới hạn của Windows, không phải lỗi. Khi kiểm tra phải dùng tài khoản
  học viên (không có quyền admin).
- **Giáo viên nên đăng nhập quyền thường khi phát đề**: app chạy quyền thường
  không chặn được thao tác bên trong phần mềm đang chạy quyền admin.
- ExamGuard không chặn được: gửi code qua mạng/USB, in ấn, chụp màn hình rồi OCR.
