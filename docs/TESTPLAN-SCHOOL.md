# CHECKLIST KIỂM THỬ TRƯỜNG HỌC — ExamGuard

Dùng khi triển khai trên phòng máy thật. Ghi kết quả: ✅ PASS / ❌ FAIL / ⚠️ LƯU Ý / ⬜ CHƯA TEST.

Chuẩn bị: bản `ExamGuard.exe` mới nhất (single-file, build `Release`), USB chứa exe, mật khẩu giáo viên chung cho tất cả máy.

---

## G1 — Máy dùng user thường (KHÔNG phải admin)

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Đăng nhập tài khoản học sinh (user thường) | Đăng nhập bình thường, ExamGuard tự chạy ẩn | |
| 2 | Copy text trong Notepad → Ctrl+C | Không copy được, clipboard trống | |
| 3 | Copy file trong Explorer → dán | Copy file vẫn hoạt động | |
| 4 | Task Manager → End task `ExamGuard.exe` | Bị từ chối "không truy cập được" | |
| 5 | Mở CMD (user thường) → `taskkill /F /IM ExamGuard.exe` | Access denied, process sống | |
| 6 | Ctrl+Alt+Shift+G → nhập mật khẩu | Dialog mở được, unlock hoạt động | |
| 7 | Nhập sai mật khẩu 3 lần | Khóa 30s | |

## G2 — Chống kill & phục hồi

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Kill 1 process `--service` (admin) | Watchdog nội bộ restart ≤2s | |
| 2 | Kill 1 process `--watchdog` (admin) | Service spawn watchdog mới ~5s | |
| 3 | Kill CẢ 2 process cùng lúc (admin) | Task Scheduler `ExamGuardWatchdog` cứu trong ~1 phút | |
| 4 | Kill cả 2 → tắt máy/khởi động lại | Tự chạy lại sau đăng nhập (autostart + task) | |
| 5 | Mất điện giữa chừng → bật lại | Bảo vệ tự hoạt động, mật khẩu không mất | |

## G3 — Deploy nhiều máy

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Copy exe vào `C:\ExamGuard\` trên từng máy | Cài đặt giống nhau trên mọi máy | |
| 2 | Chạy `ExamGuard.exe --init` với quyền admin trên từng máy | Đặt cùng 1 mật khẩu GV | |
| 3 | Học sinh đăng nhập user thường → chạy | Mọi máy chặn copy text đồng bộ | |
| 4 | Học sinh đổi mật khẩu bằng `--init` (không admin) | Bị từ chối (cần admin) | |

## G4 — Nhiều học sinh / nhiều phiên đăng nhập

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Học sinh A đăng nhập → học sinh B (đăng xuất/đăng nhập) | ExamGuard chạy cho từng phiên | |
| 2 | Task Scheduler chạy khi user khác đăng nhập | Watchdog khôi phục bất kể phiên nào | |
| 3 | Hai máy khác nhau cùng 1 mật khẩu | Unlock được trên cả hai | |

## G5 — Quản lý giờ thi

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | Giáo viên unlock 60 phút | Copy text mở trong 60 phút, tự khóa lại | |
| 2 | Giáo viên đổi mật khẩu giữa buổi | Đổi được (đòi pass cũ), áp dụng ngay | |
| 3 | Giáo viên quên mật khẩu | `--init` với quyền admin → đặt lại được | |
| 4 | Thoát hẳn sau khi hết giờ | 0 process, autostart + task bị xóa, không sống lại | |
| 5 | Xóa toàn bộ khi hết đợt thi | Folder `C:\ExamGuard` + autostart + task bị xóa sạch | |

## G6 — Môi trường mạng / firewall (chuẩn bị remote control LAN tương lai)

| # | Thao tác | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | `ipconfig /all` trên máy trường | Ghi lại IP/dải mạng phòng máy | |
| 2 | Kiểm tra quyền admin trên máy trường | `net localgroup administrators` chứa user GV? | |
| 3 | Kiểm tra firewall có mở được port | Thử mở port / rule tạm | |
| 4 | Ping giữa các máy phòng | Các máy ping thấy nhau? | |

---

## Hướng dẫn nhanh dùng khi triển khai

```text
Cài đặt (1 lần, quyền admin):
  mkdir C:\ExamGuard
  copy ExamGuard.exe C:\ExamGuard\
  C:\ExamGuard\ExamGuard.exe --init      # đặt mật khẩu GV

Chạy (mỗi lần đăng nhập - tự động qua autostart):
  C:\ExamGuard\ExamGuard.exe             # ẩn, bảo vệ máy

Giáo viên:
  Ctrl+Alt+Shift+G  → mở hộp thoại quản lý
  Nhập mật khẩu     → Mở khóa / Đổi mật khẩu / Thoát hẳn / Xóa toàn bộ
```

Ghi chú: các bước đánh dấu `(admin)` phải chạy với quyền quản trị viên.