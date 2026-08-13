# Tính năng ExamGuard

ExamGuard là công cụ chạy nền trên Windows dành cho phòng thực hành lập trình, giúp giáo viên kiểm soát thao tác trên máy học sinh.

## Chặn copy/paste/cắt văn bản
- Chặn `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+Insert`, `Shift+Insert` trên toàn hệ thống.
- Hoạt động ở hầu hết ứng dụng: Notepad, Word, trình duyệt, IDE (VS Code, IntelliJ, Eclipse...), terminal (CMD/PowerShell/Windows Terminal).
- Chặn cả copy/paste qua **menu chuột phải** — văn bản trên clipboard bị xóa ngay.

## Vẫn cho phép thao tác file
- Copy/cắt/paste **file và thư mục** qua File Explorer hoạt động bình thường.
- Kéo-thả file giữa các thư mục, kéo-thả file vào IDE vẫn được (chỉ chặn văn bản, không chặn file).

## Chạy ẩn hoàn toàn
- Không cửa sổ, không tray icon, không lộ diện trên Taskbar.
- Tự động chạy cùng hệ thống khi mở máy (autostart) — bảo vệ ngay từ lúc học sinh vào phiên làm việc.

## Quản lý bằng mật khẩu giáo viên
- Nhấn `Ctrl+Alt+Shift+G` để mở bảng quản lý.
- **Phải nhập đúng mật khẩu giáo viên** mới dùng được các tính năng.
- Mật khẩu được mã hóa SHA-256 + salt, không lưu dạng chữ thường.

## Bảo vệ mật khẩu
- Nhập sai mật khẩu **3 lần** → khóa 30 giây (chống thử đoán).

## Mở khóa tạm thời theo giờ
- Giáo viên có thể mở khóa tạm thời trong 5 / 10 / 15 / 30 / 60 / 120 phút.
- Hết thời gian **tự động khóa lại** — không cần nhớ thao tác.

## Tắt tool khi cần
- **Thoát hẳn**: tắt hoàn toàn, xóa autostart + watchdog; máy khởi động lại sẽ **không** tự bảo vệ nữa.
- **Xóa toàn bộ**: gỡ sạch ExamGuard khỏi máy (file, cấu hình, autostart, watchdog).
- Cả hai thao tác đều yêu cầu mật khẩu giáo viên.

## Đổi mật khẩu / đặt lại khi quên
- Đổi mật khẩu ngay trên bảng quản lý (cần nhập mật khẩu hiện tại).
- Quên mật khẩu: chạy `ExamGuard.exe --init` (cần quyền quản trị viên) để đặt lại.

## Chống bị tắt (4 lớp)
1. **Unkillable (DACL)**: tiến trình tự ghi đè quyền của chính nó → user thường bị **Từ chối** khi End task trong Task Manager, `taskkill /F`, `Stop-Process`.
2. **Watchdog tiến trình anh em**: nếu service bị giết, tự khởi động lại trong ~2 giây.
3. **Watchdog định kỳ**: service kiểm tra watchdog mỗi 5 giây và sinh watchdog mới nếu thiếu.
4. **Task Scheduler**: cứu khi cả service lẫn watchdog bị giết cùng lúc (khôi phục trong ~1 phút).

## Yêu cầu hệ thống
- Windows 10/11 (64-bit).
- Không cần cài .NET — bản build self-contained, 1 file `.exe` duy nhất.

## Hướng dẫn liên quan
- Hướng dẫn sử dụng đơn giản: `docs/HUONGDAN-SU-DUNG.md`.
- Hướng dẫn chi tiết cho giáo viên: `docs/GIAOVIEN.md`.
- Triển khai lên phòng máy: `docs/DEPLOYMENT.md`.
