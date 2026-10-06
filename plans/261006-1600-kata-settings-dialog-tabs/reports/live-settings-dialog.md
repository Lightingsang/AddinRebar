# Live — Kata settings dialog (3 tabs), 2026-10-06

Revit 2026 test copy `b01-test.rvt` (pid 45700), Kata Export ▸ Đọc thép Excel (read only) ▸ Cài đặt, driven by UIA
(scratchpad `kata-settings-ui.ps1`). No settings file existed before; the one written by the test was removed after
(kept as scratchpad `KataSettings.live-test.json`).

| Check | Result |
|---|---|
| Dialog opens, 3 tabs, dark theme | ✅ screenshots Detail / Thông số đặc thù / Thép mặc định |
| Detail tab shows drawing values (crank 1/6, side bar 10 d, leg round 25) | ✅ |
| Dầm giao stirrups `5f8a5` → Accept refused, message "đai gia cường '5f8a5' phải dạng …" | ✅ |
| `4f10a100` + hanger `2f14` → Accept → file v3 with `JointBeam.Stirrups`, `JointBeam.Hanger`, `Pending.*`, `Shop.*` | ✅ |
| Reopen → values read back 4f10a100 / 2f14 | ✅ |
| Table headers shortened + long check boxes wrapped (after live run) | CHƯA TEST visually (build + ThemeTokenCoverage only; UIA lost the Kata window on the relaunch) |
| Bars drawn with joint Ø8 in Revit | CHƯA TEST (needs a beam with loads + its workbook; Core tests cover zones, numbers, tags) |

Review: [code-review-settings-dialog.md](code-review-settings-dialog.md) — 0 Critical/High; M1, M2, L1–L6 fixed, L7/L8 noted.
