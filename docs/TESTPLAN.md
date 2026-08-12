# Kế hoạch kiểm thử (TESTPLAN)

## A. Kiểm thử tự động (đã chạy)
```powershell
dotnet test ExamGuard.sln
```
- 14 test pass: hashing mật khẩu, load/save config, phân loại cửa sổ File Explorer.

## B. Kiểm thử thủ công trên máy thật (VM phòng máy)

### B1. Chặn văn bản
| # | Thao tác | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 1 | Notepad: bôi đen text → Ctrl+C | Không có gì vào clipboard | |
| 2 | Notepad: Ctrl+V | Không dán được | |
| 3 | VS Code / IntelliJ / Eclipse: Ctrl+C/X/V | Chặn | |
| 4 | Ctrl+Insert | Chặn | |
| 5 | Shift+Insert | Chặn | |
| 6 | Menu chuột phải → Copy trong Notepad | Clipboard text bị xóa ngay | |
| 7 | Menu chuột phải → Paste trong Notepad | Không dán được gì | |
| 8 | Word, browser, PowerPoint | Chặn tương tự | |
| 9 | CMD / PowerShell / Windows Terminal: Ctrl+C | Không copy text (dùng Ctrl+Break để dừng) | |
| 10 | Ctrl+V khi đang mở khóa (giáo viên) | Dán được bình thường | |

### B2. Cho phép thao tác file
| # | Thao tác | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 11 | Explorer: Ctrl+C file → Ctrl+V | File được copy | |
| 12 | Explorer: Ctrl+X → Ctrl+V | File được cắt/dán | |
| 13 | Menu chuột phải Copy/Paste file | Hoạt động | |
| 14 | Kéo-thả file từ Explorer sang thư mục | Hoạt động | |
| 15 | Kéo-thả file vào IDE (import .jar/.zip/đề) | Hoạt động | |

### B3. Ẩn & quản lý
| # | Thao tác | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 16 | Khởi động --service | Không cửa sổ, không tray, không taskbar | |
| 17 | Task Manager | Thấy tiến trình `ExamGuard` nhưng không có cửa sổ | |
| 18 | Nhấn `Ctrl+Alt+Shift+G` | Hộp thoại mật khẩu hiện, luôn ở trên | |
| 19 | Nhập sai mật khẩu 3 lần | Khóa 30 giây | |
| 20 | Mật khẩu đúng → Mở khóa | Chặn tạm dừng, tự khóa lại sau thời gian cấu hình | |
| 21 | Mật khẩu đúng → Thoát | Thoát hẳn, watchdog không khởi động lại | |
| 22 | Alt+F4 / Task Manager "End task" hộp thoại | Không tắt được | |
| 23 | Giết tiến trình --service | Watchdog khởi động lại trong ~2-8s | |
| 24 | Giết CẢ service + watchdog cùng lúc | Task Scheduler khôi phục trong ~1 phút | |
| 25 | Khởi động 2 bản --service cùng lúc | Bản thứ 2 tự thoát, không sinh zombie | |
| 26 | Khởi động lại máy | Tự chạy ẩn sau đăng nhập | |

### B4. Lớp "unkillable" (DACL)
| # | Thao tác (bằng user thường, KHÔNG admin) | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 26a | `taskkill /F /IM ExamGuard.exe` | Bị từ chối (access denied), service còn sống | ✅ |
| 26b | `Stop-Process -Id <service> -Force` | Bị từ chối (access denied) | ✅ |
| 26c | Task Manager → End task tiến trình service | Báo "không truy cập được", không tắt | |
| 26d | Giết tiến trình **watchdog** (không được bảo vệ) | Service sinh watchdog mới trong ~5s | |
| 26e | Admin `runas` kill service | **Có thể kill** (giới hạn Windows, không phải lỗi) | ✅ |
| 26f | Đặt `"Unkillable": false` → khởi động lại service → kill | Kill được bình thường (đã tắt lớp DACL) | |

Ghi chú kiểm chứng thực tế (phiên QA 12/08):
- DACL áp lên process được xác nhận qua đọc lại ACL: ACE[0] = DENY Everyone
  `0x82B` (terminate/suspend/create-thread/vm-op/vm-write), ACE[1] = ALLOW
  `0x21000` (read-control/query).
- User thường (Basic User trust level): `OpenProcess(PROCESS_TERMINATE)` → err 5
  (access denied), tiến trình sống.
- Log service: `unkillable=True`; watchdog `restarting service` hoạt động đúng.

### B5. Đóng gói
| # | Kiểm tra | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 27 | `artifacts\...\ExamGuard.exe` | 1 file duy nhất, tự chạy trên máy không cài .NET | |
| 28 | Chạy trên Windows 11 sạch (VM) | Toàn bộ B1-B4 OK | |

## C. Ghi chú khi fail
- Ghi lại app đang test, phiên bản Windows, log nếu có.
- Phân biệt "không chặn" do: app chạy quyền admin / cửa sổ nền tảng khác / lỗi hook.
- Khi test lớp unkillable: **phải test bằng user thường**. Nếu test từ shell admin, việc kill thành công là bình thường (admin bypass DACL), không phải lỗi.
- Nếu End task vẫn tắt được tiến trình bằng user thường → kiểm tra log có dòng `unkillable=True`; nếu `False`/thiếu → đã tắt cờ `Unkillable` hoặc lỗi P/Invoke.
