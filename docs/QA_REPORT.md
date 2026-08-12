# QA REPORT — ExamGuard

Run on: 2026-08-12, Windows 11 (local dev machine), real OS-level execution.
Test plan: `docs/QA_PLAN.md`. Build under test: `fbe6944` (dialog features + SelfDelete),
deployed as `C:\ExamGuard\ExamGuard.exe` (x64, self-contained single-file).

## 1. Summary

- **Total planned:** 27 test cases (TC01–TC27)
- **PASS:** 24 · **PARTIAL:** 2 · **BLOCKED:** 1 · **FAIL:** 0 · **NOT TESTED:** 0
- **Defects found:** 0 × P0/P1, 0 × P2, 2 × P3 (low severity notes, no workaround needed)
- **Verdict: 🟢 GO** — core functionality verified end-to-end on the real deployment.

## 2. Test Matrix — Results

| ID   | Group       | Scenario                                    | Result    | Evidence (excerpt)                                                                 |
|------|-------------|---------------------------------------------|-----------|------------------------------------------------------------------------------------|
| TC01 | Build       | Release build                               | ✅ PASS   | `dotnet build ExamGuard.sln -c Release` → 0 errors                                 |
| TC02 | Build       | Unit tests                                  | ✅ PASS   | `dotnet test ExamGuard.sln` → 14 passed, 0 failed                                   |
| TC03 | Packaging   | Publish single-file exe                     | ✅ PASS   | `artifacts\ExamGuard-win-x64\ExamGuard.exe` (49.2 MB), deploys + runs              |
| TC04 | Lifecycle   | Start `--service`                           | ✅ PASS   | Hidden; log: `unkillable=True` then `service startup complete`; 2 processes        |
| TC05 | Lifecycle   | Duplicate `--service`                       | ✅ PASS   | Second instance exits: `duplicate service instance, exiting`                       |
| TC06 | Lifecycle   | Kill service                                | ✅ PASS   | Watchdog respawns; measured ~2.0 s (see TC26)                                      |
| TC07 | Lifecycle   | Kill watchdog                               | ✅ PASS   | Watchdog respawned automatically                                                  |
| TC08 | Lifecycle   | Stop flag `examguard.stopped`               | ✅ PASS   | Watchdog stands down, no respawn                                                   |
| TC09 | Anti-kill   | Non-admin OpenProcess(TERMINATE)            | ✅ PASS   | Basic-User token → `OpenProcess` returns 0, `GetLastError` = 5 (access denied)     |
| TC10 | Anti-kill   | Admin kill (boundary)                       | ✅ PASS   | Elevated process can terminate — documented Windows limit                          |
| TC11 | Anti-kill   | `Unkillable=false`                          | ✅ PASS   | Non-admin terminate succeeds; config restored to `true` afterwards                 |
| TC12 | Autostart   | Registry `Run` after `--service`            | ✅ PASS   | `HKCU\...\CurrentVersion\Run\ExamGuard` present                                    |
| TC13 | Config      | Corrupt `examguard.json`                    | ⚠️ PARTIAL| Fallback verified by unit test `CorruptFile_FallsBack_ToDefault`; GUI setup form not exercised |
| TC14 | Config      | `UnlockMinutes=0`                           | ✅ PASS   | Logic-only: `Math.Max(1, minutes)` clamps to 1; combo presets are 5–120            |
| TC15 | Clipboard   | Text clipboard externally                   | ✅ PASS   | Text set → cleared by guard within guard interval                                  |
| TC16 | Clipboard   | Unicode text                                | ✅ PASS   | Unicode incl. surrogate emoji cleared                                              |
| TC17 | Clipboard   | File-drop (CF_HDROP)                        | ✅ PASS   | `SetFileDropList` survives 3 s while locked (Explorer copy simulation)             |
| TC18 | KeyboardHook| Ctrl+C/X/V blocked in non-explorer window   | ⛔ BLOCKED | Swallow indistinguishable from clipboard-clear (both yield empty clipboard); side evidence: hook is correctly DISABLED while unlocked (Ctrl+C in Notepad succeeds) |
| TC19 | KeyboardHook| Ctrl+C in File Explorer (file op)           | ✅ PASS   | CF_HDROP (file-drop) preserved while locked → Explorer file ops unaffected         |
| TC20 | Password UI | Wrong pw ×3 → lockout 30 s                  | ✅ PASS   | After 3 wrong attempts the CORRECT password is rejected (dialog stays open) → lockout enforced; cooldown 30 s |
| TC21 | Password UI | Unlock with chosen duration                 | ⚠️ PARTIAL| Unlock E2E PASS: hotkey → pw → dialog closes → guard off (clipboard text persists). Auto-relock at 60 min NOT waited → timing BLOCKED (cannot wait 1 h); mechanism inspected (timer, duration from combo) |
| TC22 | Password UI | Thoát hẳn (permanent exit)                  | ✅ PASS   | After confirm: 0 processes, `Run\ExamGuard` removed, task `ExamGuardWatchdog` deleted (`schtasks /Query` → "cannot find the file"), `examguard.stopped` written |
| TC23 | Password UI | Xóa toàn bộ (delete-all)                    | ✅ PASS   | After confirm: install folder deleted, autostart removed, 0 processes, helper self-deletes |
| TC24 | Security    | Plaintext password in json/log              | ✅ PASS   | `Teacher@QA2026` not present in `examguard.json` (only `SaltBase64` + `PasswordHash`) or `examguard.log` |
| TC25 | Security    | Delete-helper path quoting                  | ✅ PASS   | Paths wrapped in `"..."`; Windows filenames cannot contain `"` → no injection. P3 note (see §4) |
| TC26 | Performance | Startup, memory, watchdog latency           | ✅ PASS   | Startup immediate; service WS ≈ 109 MB, watchdog WS ≈ 94 MB; watchdog respawn ≈ 2.0 s |
| TC27 | Regression  | Re-run TC04–TC11 after UI changes           | ✅ PASS   | All lifecycle/anti-kill tests executed on the current `fbe6944` build             |

## 3. Notable E2E flows verified (hotkey + dialog automation)

- Unlock: `Ctrl+Alt+Shift+G` → dialog "ExamGuard" → password → Enter → dialog closes, guard off.
- Thoát hẳn: dialog → password → Tab×3 → Enter → process/autostart/task/stop-flag all correct.
- Xóa toàn bộ: dialog → password → Tab×5 → Enter → confirm (Enter) → folder + autostart gone.
- Lockout: 3 wrong attempts → even the correct password is refused during the 30 s cooldown.

### Dialog keyboard-navigation note
Tab order after typing the password: **TextBox → duration ComboBox → Mở khóa → Thoát hẳn → Đổi mật khẩu → Xóa toàn bộ**.
Enter inside the password box = Unlock. This was confirmed during automation (not a defect).

## 4. Findings (P3 only)

1. **SelfDelete CWD edge case (P3, Low):** `rd /s /q "<install folder>"` in the uninstall helper
   cannot delete a directory that is the helper's own working directory. In our run the
   working directory differed, so delete-all succeeded (verified: folder gone). If ExamGuard
   is ever launched with "Start in" set to its install folder, the folder could survive the
   delete-all (exe and config would still be deleted). `SelfDelete.cs:34`.
2. **Watchdog respawn window (P3, Low):** service restart took ~2.0 s (watchdog poll interval
   ~1 s + spawn). A very fast malicious re-launch could win a ~1–2 s window. Acceptable for the
   threat model (anti-kill DACL already blocks non-admin terminates).

## 5. Coverage

- **In scope (plan §2):** all categories executed — build/packaging, lifecycle, anti-kill DACL,
  autostart, config handling, clipboard guard, keyboard-hook best-effort, password dialog E2E,
  security subset, performance.
- **Out of scope / N/A:** web/API/DB/CI (excluded in plan); real Task Manager "End task" UI
  (substituted by OpenProcess access checks, TC09–TC11).
- **Residual risk:** TC18 locked-state hook swallow and TC21 auto-relock timing remain
  BLOCKED by environment/timing; both backed by code inspection and complementary E2E evidence.

## 6. Environment after QA

Machine left in a **protected** state: ExamGuard redeployed to `C:\ExamGuard\`,
`--service` + `--watchdog` running (locked, `unkillable=True`, known QA password `Teacher@QA2026`,
`UnlockMinutes=60`), autostart + watchdog task registered.

---

### Evidence appendix (commands)

```text
dotnet build ExamGuard.sln -c Release            -> 0 errors
dotnet test ExamGuard.sln                        -> 14 passed
scripts\publish.ps1                              -> artifacts\ExamGuard-win-x64\ExamGuard.exe (49.2 MB)
Start-Process C:\ExamGuard\ExamGuard.exe --service -> log: "unkillable=True" / "service startup complete"
Get-Process ExamGuard                             -> 2 (service + watchdog)
Stop-Process (service)                            -> respawn to 2 processes after 1993 ms
runas /trustlevel:0x20000 (non-admin) OpenProcess(PROCESS_TERMINATE) -> err=5
schtasks /Delete /F /TN ExamGuardWatchdog         -> "cannot find the file" (already removed by Thoát hẳn)
Test-Path C:\ExamGuard                            -> False after Xóa toàn bộ; True after redeploy
Get-Content examguard.json                        -> SaltBase64 + PasswordHash only (no plaintext)
watchdog respawn latency                          -> 1993 ms
memory                                           -> service WS 109,428,736; watchdog WS 93,736,960
```
