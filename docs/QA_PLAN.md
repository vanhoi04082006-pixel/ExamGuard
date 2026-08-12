# QA TEST PLAN — ExamGuard

Adapted from the generic web-oriented QA prompt to fit this product: a hidden
Windows desktop app (WinForms, .NET 10, x64, self-contained single-file exe).
All web/full-stack categories (XSS, API, DB, CI, browser, etc.) are **N/A** and
excluded. Real testing here is OS-level on Windows.

## 1. Product / Environment

```text
Application type : Windows desktop app (WinForms), hidden, no window/tray
Technology       : C# / .NET 10, x64, self-contained single-file
Entry point      : src/ExamGuard.App/Program.cs
                   modes: --service (guard) | --watchdog (sibling) | --init (password setup)
Build command    : dotnet build ExamGuard.sln -c Release
Test command     : dotnet test ExamGuard.sln
Lint command     : N/A (no lint config in repo)
Type-check       : N/A (C# compiler does it during build)
Publish          : powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
Runtime          : .NET 10 (self-contained; no install needed on target)
Database         : None (JSON file examguard.json)
External services: None (fully offline, local machine only)
CI               : None (no .github / pipeline config found)
Test env         : Windows 10/11 x64 dev machine; deploy target C:\ExamGuard\
```

## 2. Test Scope

### In scope
- Build & packaging reproducibility.
- Process lifecycle: hidden service, duplicate instance, watchdog restart,
  watchdog respawn, Task Scheduler recovery, stop-flag stand-down.
- Anti-kill DACL: non-admin termination refused; `Unkillable=false` disables it.
- Autostart: `HKCU\...\Run\ExamGuard` written on `--service`, removed on "Thoát hẳn".
- Config handling: missing / corrupt `examguard.json`, `UnlockMinutes` boundary.
- Clipboard guard: text cleared, file-drop preserved, Unicode.
- Keyboard hook (best effort): block Ctrl+C/X/V, allow Explorer, allow while unlocked.
- Password dialog (best effort via UI automation): lockout after 3 fails,
  unlock with preset duration, permanent exit, delete-all.
- Security (testable subset): no plaintext password in json/log, no secrets,
  safe quoting of delete-helper path.
- Performance: startup time, memory usage, watchdog restart latency.

### Out of scope / N/A
- Web/API/DB/browser tests. CI. NetSupport/remote-lab flows (no lab env here).
- Real Task Manager "End task" UI (substituted by equivalent access-check tests).

## 3. Testability rules (4 states)

```text
PASS      tested and matches expected
FAIL      tested and does not match expected
BLOCKED   cannot be tested (missing environment/precondition)
NOT TESTED not yet executed
```

Never infer PASS from code reading. GUI-dependent checks that cannot run in this
environment are BLOCKED (with reason), not PASS.

## 4. Severity

```text
P0 Critical – crash / data loss / security / unusable
P1 High     – core function broken, large user impact
P2 Medium   – bug with a workaround
P3 Low      – cosmetic / minor
```

## 5. Test Matrix (planned)

| ID   | Group        | Scenario                                            | Expected                                                        | Testable |
|------|--------------|-----------------------------------------------------|-----------------------------------------------------------------|----------|
| TC01 | Build        | Release build                                       | 0 errors                                                        | Yes      |
| TC02 | Build        | Unit tests                                          | 14 passed                                                       | Yes      |
| TC03 | Packaging    | Publish single-file exe                             | 1 file, runs                                                    | Yes      |
| TC04 | Lifecycle    | Start --service                                     | Hidden, log `unkillable=True` + `startup complete`              | Yes      |
| TC05 | Lifecycle    | Duplicate --service                                 | Second instance exits quietly                                   | Yes      |
| TC06 | Lifecycle    | Kill service                                        | Watchdog restarts in ~2–8s                                      | Yes      |
| TC07 | Lifecycle    | Kill watchdog                                       | Service respawns watchdog in ~5s                                | Yes      |
| TC08 | Lifecycle    | Stop flag `examguard.stopped`                       | Watchdog stands down                                            | Yes      |
| TC09 | Anti-kill    | Non-admin OpenProcess(TERMINATE)                    | Access denied (err 5)                                           | Yes      |
| TC10 | Anti-kill    | Admin kill (boundary)                               | Allowed (documented Windows limit)                              | Yes      |
| TC11 | Anti-kill    | Unkillable=false                                    | Killable by non-admin                                           | Yes      |
| TC12 | Autostart    | Registry Run after --service                        | Value `ExamGuard` present                                       | Yes      |
| TC13 | Config       | Corrupt examguard.json                              | App must not crash (falls back to setup)                        | Partial (GUI) |
| TC14 | Config       | UnlockMinutes=0                                     | Treated as 1 min                                                | Logic only |
| TC15 | Clipboard    | Set text clipboard externally                       | Cleared by guard                                                | Yes      |
| TC16 | Clipboard    | Unicode text                                        | Cleared by guard                                                | Yes      |
| TC17 | Clipboard    | File-drop (CF_HDROP)                                | Preserved                                                       | Partial |
| TC18 | KeyboardHook | Ctrl+C/X/V in non-explorer window                   | Blocked                                                         | Partial |
| TC19 | KeyboardHook | Ctrl+C in File Explorer                             | Allowed (file op)                                               | Partial |
| TC20 | Password UI  | Wrong pw x3 → lockout 30s                           | Refuses input                                                   | Partial |
| TC21 | Password UI  | Unlock with chosen duration                         | Relock after N minutes                                          | Partial |
| TC22 | Password UI  | Thoát hẳn                                          | Autostart + task removed                                        | Partial |
| TC23 | Password UI  | Xóa toàn bộ (delete-all)                            | Folder removed, helper self-deletes                             | Partial |
| TC24 | Security     | Plaintext password in examguard.json/log            | None present                                                    | Yes      |
| TC25 | Security     | Delete-helper path quoting                          | Path quoted, no injection                                       | Inspect |
| TC26 | Performance  | Startup time, memory, watchdog latency              | Measured numbers reported                                       | Yes      |
| TC27 | Regression   | Re-run TC04–TC11 after UI changes                   | Same results                                                    | Yes      |

"Partial" = main flow testable here, full GUI flow needs interactive desktop.

## 6. Evidence requirements

Command executed, exit code, log excerpt, process list, registry read, ACL dump,
measured timestamps. No evidence → not VERIFIED.
