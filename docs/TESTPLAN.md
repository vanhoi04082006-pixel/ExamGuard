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
| 23 | Giết tiến trình --service | Watchdog khởi động lại trong ~2-5s | |
| 24 | Khởi động lại máy | Tự chạy ẩn sau đăng nhập | |

### B4. Đóng gói
| # | Kiểm tra | Kết quả mong đợi | Pass? |
|---|---|---|---|
| 25 | `artifacts\...\ExamGuard.exe` | 1 file duy nhất, tự chạy trên máy không cài .NET | |
| 26 | Chạy trên Windows 11 sạch (VM) | Toàn bộ B1-B3 OK | |

## C. Ghi chú khi fail
- Ghi lại app đang test, phiên bản Windows, log nếu có.
- Phân biệt "không chặn" do: app chạy quyền admin / cửa sổ nền tảng khác / lỗi hook.
