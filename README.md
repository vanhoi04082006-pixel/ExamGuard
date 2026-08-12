# ExamGuard

Công cụ Windows chạy nền cho phòng thực hành lập trình: **chặn Copy/Paste văn bản (code)** nhưng **vẫn cho phép Copy/Paste file**, quản lý bằng mật khẩu giáo viên, đóng gói 1 file `.exe`.

## Tính năng
- Chặn `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+Insert`, `Shift+Insert` trên toàn hệ thống (kể cả terminal) **trừ File Explorer**.
- Chặn copy/paste văn bản qua menu chuột phải (clipboard text bị xóa ngay).
- Copy/cut/paste file & thư mục qua File Explorer hoặc kéo thả **hoạt động bình thường** (`CF_HDROP`).
- Chạy ẩn hoàn toàn: không cửa sổ, không tray icon.
- Hotkey bí mật mặc định: **`Ctrl + Alt + Shift + G`** → mở hộp thoại mật khẩu.
- Mật khẩu: SHA-256 + salt; sai 3 lần → khóa 30 giây.
- Khóa tự động khóa lại sau thời gian mở khóa (mặc định 60 phút).
- Chống tắt: watchdog tự khởi động lại nếu tiến trình chính bị giết.
- Tự động chạy cùng hệ thống (registry Run).
- Đóng gói 1 file `.exe` duy nhất.

## Build
```powershell
# Build debug
dotnet build ExamGuard.sln

# Chạy unit test
dotnet test ExamGuard.sln

# Đóng gói 1 file exe (tự chứa, win-x64)
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
```
Kết quả: `artifacts\ExamGuard-win-x64\ExamGuard.exe`

## Sử dụng
```powershell
# Chạy dịch vụ (chặn hoạt động ngay)
ExamGuard.exe --service

# Lần đầu / cài lại mật khẩu giáo viên
ExamGuard.exe --init
```

### Mật khẩu mặc định & cài đặt lần đầu
- Lần đầu chạy không có file `examguard.json`, chương trình tự hiện hộp thoại **Cài đặt mật khẩu**.
- Có thể chạy trước `ExamGuard.exe --init` để cài mật khẩu rồi mới deploy.
- Mật khẩu được hash SHA-256 + salt, lưu tại `examguard.json` (cùng thư mục exe).

### Thao tác giáo viên
| Thao tác | Cách làm |
|---|---|
| Mở hộp thoại quản lý | Nhấn `Ctrl+Alt+Shift+G` |
| Mở khóa tạm thời | Nhập mật khẩu → **Mở khóa** (tự khóa lại sau 60 phút) |
| Thoát hẳn | Nhập mật khẩu → **Thoát** (kèm watchdog tắt theo) |
| Đổi mật khẩu | Nhập mật khẩu hiện tại → **Đổi mật khẩu** |

## Cấu trúc
```
ExamGuard.sln
├─ src/ExamGuard.Core/      Lõi: hook, clipboard, cấu hình, bảo mật
├─ src/ExamGuard.App/       Giao diện ẩn, watchdog, dialog mật khẩu
├─ tests/ExamGuard.Core.Tests/
├─ scripts/publish.ps1
└─ docs/  PLAN.md · DEPLOYMENT.md · TESTPLAN.md
```

## Giới hạn đã biết
- Không chặn chuyển code qua mạng/USB, in ấn, ảnh chụp màn hình + OCR.
- Tiến trình không chạy quyền admin sẽ không chặn được thao tác trong app chạy **quyền admin**.
- Menu chuột phải vẫn hiển thị; copy/paste text chỉ không còn tác dụng.

Xem chi tiết triển khai ở `docs/DEPLOYMENT.md` và kế hoạch kiểm thử ở `docs/TESTPLAN.md`.
