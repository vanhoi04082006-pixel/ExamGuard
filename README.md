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
- Chống tắt 4 lớp:
  1. **Unkillable** (DACL): service tự ghi đè quyền của chính nó để mọi user bị **Từ chối** quyền kết thúc/treo/chèn tiến trình → Task Manager "End task", `taskkill /F`, `Stop-Process` của user thường **bị từ chối** ngay lập tức.
  2. Watchdog tiến trình anh em: nếu service vẫn bị giết thì khởi động lại trong ~2-8s.
  3. Service kiểm tra watchdog định kỳ (5s) và sinh watchdog mới nếu thiếu.
  4. Task Scheduler (`ExamGuardWatchdog`, mỗi phút): cứu khi cả service lẫn watchdog bị giết cùng lúc (~1 phút).
- Có thể tắt lớp DACL qua cờ `Unkillable` trong `examguard.json` (mặc định bật).
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
# Cài đặt mới trên 1 máy: double-click ExamGuard.exe (hoặc chạy không có đối số)
# -> hộp thoại Cài đặt mật khẩu -> Lưu -> app tự chạy ẩn + đăng ký autostart

# Đổi lại mật khẩu giáo viên (quên mật khẩu cũ vẫn được; chạy ngay trên máy cần bảo vệ)
ExamGuard.exe --init

# Chạy nền thủ công (thường do autostart / Task Scheduler gọi)
ExamGuard.exe --service
```

### Mật khẩu & cài đặt lần đầu
- Cài đặt mới: **double-click** `ExamGuard.exe` (hoặc chạy không có đối số). Khi chưa có
  `examguard.json`, app tự hiện hộp thoại **Cài đặt mật khẩu**; lưu xong hiện thông báo
  xác nhận rồi tự chạy ẩn, đăng ký autostart + watchdog — không cần gõ lệnh.
- Ai đặt mật khẩu là người duy nhất biết nó — vì vậy hãy đặt trên chính
  máy giáo viên/máy trạm, **không** tạo sẵn config rồi copy sang (người tạo sẽ
  biết mật khẩu).
- Mật khẩu được hash SHA-256 + salt, lưu tại `examguard.json` (cùng thư mục exe).

### Thao tác giáo viên
| Thao tác | Cách làm |
|---|---|
| Mở hộp thoại quản lý | Nhấn `Ctrl+Alt+Shift+G` |
| Mở khóa tạm thời | Nhập mật khẩu → chọn thời gian (5–120 phút) → **Mở khóa** (hết giờ tự khóa lại) |
| Thoát hẳn | Nhập mật khẩu → **Thoát hẳn** (xóa autostart + watchdog, reboot không chạy lại) |
| Đổi mật khẩu | Nhập mật khẩu hiện tại → **Đổi mật khẩu** |
| Xóa toàn bộ | Nhập mật khẩu → **Xóa toàn bộ** → xác nhận (xóa mọi file, cấu hình, autostart, watchdog) |

## Cấu trúc
```
ExamGuard.sln
├─ src/ExamGuard.Core/      Lõi: hook, clipboard, cấu hình, bảo mật
│  └─ Security/ProcessProtector.cs   Ghi đè DACL tiến trình → "unkillable"
├─ src/ExamGuard.App/       Giao diện ẩn, watchdog, dialog mật khẩu
├─ tests/ExamGuard.Core.Tests/
├─ scripts/publish.ps1
└─ docs/  GIAOVIEN.md · PLAN.md · DEPLOYMENT.md · TESTPLAN.md
```

## Giới hạn đã biết
- Không chặn chuyển code qua mạng/USB, in ấn, ảnh chụp màn hình + OCR.
- Tiến trình không chạy quyền admin sẽ không chặn được thao tác trong app chạy **quyền admin**.
- Menu chuột phải vẫn hiển thị; copy/paste text chỉ không còn tác dụng.
- Lớp "unkillable" (DACL) chỉ chặn được user **không có quyền admin**. User có quyền admin (hoặc nắm `SeDebugPrivilege`) vẫn kết thúc được tiến trình vì admin luôn có thể lấy lại quyền sở hữu/đặt lại DACL — đây là giới hạn của Windows, không phải lỗ hổng. Muốn mở khoá triệt để: đặt `"Unkillable": false` trong `examguard.json` rồi khởi động lại service.
- Sau khi bật lớp DACL, service **không còn tự mở lại được chính mình với toàn quyền** (chỉ được query); nếu có nhu cầu đó, tắt `Unkillable`.

- Hướng dẫn sử dụng cho giáo viên: `docs/GIAOVIEN.md`.
- Xem chi tiết triển khai ở `docs/DEPLOYMENT.md` và kế hoạch kiểm thử ở `docs/TESTPLAN.md`.
