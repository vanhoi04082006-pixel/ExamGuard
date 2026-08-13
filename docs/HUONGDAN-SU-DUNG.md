# Hướng dẫn sử dụng ExamGuard

ExamGuard là công cụ chạy nền cho phòng thực hành lập trình: **chặn Copy/Paste văn bản (code)** nhưng **vẫn cho phép Copy/Paste file**, quản lý bằng mật khẩu giáo viên.

## 1. Cài đặt

1. Copy **thư mục chứa `ExamGuard.exe`** vào ổ `C:\` (ví dụ `C:\ExamGuard\`).
2. **Double-click** vào `ExamGuard.exe`.
3. Bảng **Tạo mật khẩu giáo viên** hiện ra → nhập mật khẩu (nhập lại để xác nhận) → **Lưu**.
   > ⚠️ **Ghi nhớ mật khẩu này** — nó dùng để quản lý tool.
4. Sau khi lưu, màn hình hiện **thông báo tổ hợp phím**: nhấn `Ctrl + Alt + Shift + G` để mở bảng quản lý.
5. Góc phải dưới màn hình xuất hiện **thông báo "ExamGuard đang chạy ẩn"** — tool đã chạy và bắt đầu bảo vệ máy.

> Lưu ý: chỉ **lần đầu** (khi chưa có file cấu hình) mới hiện bảng tạo mật khẩu. Từ lần sau double-click, tool chạy ẩn ngay và chỉ hiện thông báo nhỏ ở góc màn hình.

## 2. Quản lý tool (giáo viên)

Nhấn tổ hợp phím **`Ctrl + Alt + Shift + G`** để mở giao diện quản lý. **Phải nhập mật khẩu giáo viên trước** mới sử dụng được các tính năng.

| Nút | Chức năng |
|---|---|
| **Mở khóa** | Tạm cho phép copy/paste text trong thời gian chọn (5 / 10 / 15 / 30 / 60 / 120 phút). Hết giờ tự khóa lại. |
| **Thoát hẳn** | Tắt hẳn ExamGuard: xóa autostart + watchdog, máy khởi động lại sẽ không tự bảo vệ nữa. |
| **Đổi mật khẩu** | Đổi mật khẩu giáo viên (cần nhập mật khẩu hiện tại trước). |
| **Xóa toàn bộ** | Gỡ hoàn toàn ExamGuard khỏi máy: xóa mọi file, cấu hình, autostart, watchdog. |

## 3. Quên mật khẩu

Nếu quên mật khẩu giáo viên:

1. Mở thư mục chứa `ExamGuard.exe`.
2. **Click phải** vào `ExamGuard.exe` → **Chạy với tư cách quản trị viên** → hoặc mở PowerShell/CMD với quyền admin.
3. Chạy lệnh:
   ```powershell
   ExamGuard.exe --init
   ```
4. Bảng **Tạo mật khẩu giáo viên** hiện ra → nhập mật khẩu mới → **Lưu**.

> Khi máy đã có mật khẩu, lệnh `--init` **bắt buộc chạy với quyền quản trị viên** để tránh học sinh tự đổi mật khẩu.

## 4. Kiểm tra nhanh trước giờ thi

| Kiểm tra | Kết quả mong đợi |
|---|---|
| Mở máy, không thấy cửa sổ/tray mới | OK (chạy ẩn) |
| Notepad gõ vài chữ → `Ctrl+C` → `Ctrl+V` | **Không copy/dán được** text |
| File Explorer → copy/paste file | Hoạt động bình thường |
| Task Manager → chuột phải `ExamGuard` → End task | Bị từ chối ("không truy cập được") |
| Nhấn `Ctrl+Alt+Shift+G` | Hộp thoại mật khẩu hiện, luôn ở trên |

## 5. Lưu ý khi hoạt động thực tế

- **Chống tắt chỉ chặn được user không phải admin.** Nếu đang đăng nhập bằng tài khoản **admin** thì vẫn kill được (giới hạn của Windows). Khi kiểm tra chống tắt phải dùng tài khoản **học sinh (user thường)**.
- ExamGuard không chặn được: gửi code qua mạng/USB, in ấn, chụp màn hình rồi OCR.
- Nếu máy có antivirus báo lạ: cấu hình ngoại lệ cho thư mục cài đặt (do hook bàn phím + exe self-contained đôi khi bị nhận nhầm).