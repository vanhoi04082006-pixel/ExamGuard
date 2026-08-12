# ExamGuard - Kế hoạch triển khai (PLAN)

## Mục tiêu
Tiện ích Windows chạy nền phục vụ phòng thực hành lập trình:
- **Chặn** Copy/Paste văn bản (code) bằng bàn phím, menu chuột phải, mọi trình soạn thảo.
- **Cho phép** Copy/Paste file/thư mục (File Explorer, kéo thả).
- Chạy **ẩn** (không cửa sổ, không tray), quản lý bằng **mật khẩu giáo viên** qua hotkey bí mật.
- Đóng gói **1 file .exe** duy nhất.

## Quyết định kỹ thuật
| Hạng mục | Quyết định |
|---|---|
| Ngôn ngữ | C# / .NET 10 (WinForms), x64 |
| Chặn phím | Low-level hook `WH_KEYBOARD_LL` |
| Chặn clipboard | `AddClipboardFormatListener` + xóa định dạng text |
| Nhận dạng File Explorer | Whitelist window class: `CabinetWClass`, `Progman`, `WorkerW` |
| Ẩn & quản lý | Không window/tray; hotkey `Ctrl+Alt+Shift+G` |
| Mật khẩu | SHA-256 + salt, lưu `examguard.json`, 3 lần sai → khóa 30s |
| Chống tắt | 4 lớp: DACL "unkillable" (mọi user bị từ chối kill) + watchdog 2 tiến trình + kiểm tra chéo 5s + Task Scheduler mỗi phút |
| Autostart | Registry `HKCU\...\Run` |
| Đóng gói | `dotnet publish` self-contained single-file (~49 MB) |

## Cơ chế chặn (2 lớp, chồng nhau)
1. **KeyboardHook**: nuốt `Ctrl+C/X/V`, `Ctrl+Insert`, `Shift+Insert` **trừ khi** cửa sổ trước mặt là File Explorer (khi đó là thao tác file → cho qua). Chặn cả terminal.
2. **ClipboardGuard**: mỗi lần clipboard thay đổi, nếu có định dạng text (`CF_UNICODETEXT`/`CF_TEXT`) và KHÔNG phải file drop (`CF_HDROP`) → xóa clipboard. Bắt mọi đường copy (menu chuột phải, copy lập trình). File drop luôn giữ nguyên.

## Cấu trúc solution
```
ExamGuard.sln
├─ src/ExamGuard.Core/      NativeMethods, KeyboardHook, ClipboardGuard,
│                           ForegroundWindow, AppConfig, ConfigStore,
│                           PasswordHasher, AutoStart, ProcessGuard,
│                           Security/ProcessProtector (DACL unkillable)
├─ src/ExamGuard.App/       Program, GuardForm (ẩn), PasswordDialog,
│                           Watchdog, Initializer, LockoutGuard
├─ tests/ExamGuard.Core.Tests/  xUnit
├─ scripts/publish.ps1      đóng gói 1 file exe
└─ docs/                    README, GIAOVIEN, DEPLOYMENT, TESTPLAN
```

## Phân công 3 thành viên / 4 ngày
- **Ngày 1**: Scaffold solution; `NativeMethods`, `KeyboardHook`, `ForegroundWindow`; skeleton WinForms ẩn.
- **Ngày 2**: `ClipboardGuard`; `AppConfig`+`PasswordHasher`; `PasswordDialog`+hotkey; trạng thái khóa/mở.
- **Ngày 3**: `AutoStart`; `Watchdog`; edge case (thanh địa chỉ Explorer, dialog, terminal); `publish.ps1`; build đầu tiên.
- **Ngày 4**: Chạy test toàn diện trên máy sạch; sửa lỗi; viết docs; build bản cuối + demo.

## Trạng thái (đã hoàn thành)
- [x] Solution + Core + App + Tests (build 0 lỗi)
- [x] 14 unit test pass
- [x] Publish 1 file .exe (49.2 MB)
- [x] Smoke test: chạy ẩn, watchdog tự khởi động lại, thoát sạch
- [x] Lớp "unkillable" (DACL): deny Everyone terminate/suspend/inject, ACE deny đặt trước ACE allow
- [x] Kiểm chứng unkillable: user thường (Task Manager "End task", `taskkill`, `Stop-Process`) bị từ chối (access denied); watchdog vẫn restart service khi bị giết
- [x] Tài liệu README / DEPLOYMENT / TESTPLAN

## Lưu ý vận hành
- Cờ `"Unkillable"` trong `examguard.json` (mặc định `true`): bật/tắt lớp DACL khi cần gỡ rối hay bảo trì, vì khi bật, tiến trình không thể bị kết thúc từ bên ngoài (kể cả bằng `taskkill /F`).
- Tắt lớp DACL không làm mất các lớp watchdog — service vẫn tự phục hồi khi bị giết.

## Giới hạn đã biết (documented)
- Menu chuột phải vẫn hiện nhưng copy/paste text không có tác dụng (không dùng DLL injection).
- Clipboard chứa cả text lẫn ảnh → bị xóa toàn bộ.
- Hook của tiến trình không nâng quyền không chặn được thao tác trong app chạy quyền admin.
- DACL "unkillable" chỉ chặn user không có quyền admin; admin/`SeDebugPrivilege` vẫn kill được (đặt lại DACL/ownership) — xem README.
- Chưa chống: gửi code qua mạng/USB, in ấn, chụp màn hình → OCR.
