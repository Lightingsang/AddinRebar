# Kata Export: checkbox "Bổ sung dầm giao, cột cấy"

Request (2026-10-29): add a checkbox to add the joint rebar at crossing beams and stub columns; the bar parameters come
from Kata Settings (tab "Thép mặc định", already `KataSettings.JointBeam` / `JointColumn`).

Why: T1-DY8 (T2-DY7.dwg) — Kata draws no joint stirrups / hangers where the sheet says nothing; HPRebar drew Ø8a50 +
2Ø16 hangers at the crossing beam it measured in Revit. B01's drawing has them. So it is the user's call per run.

## Design

| Layer | Change |
|---|---|
| Core `KataRebarPlanner.Plan(..., addJointRebar = true)` | false → every span's `Loads` cleared before rules/layout; one `[Bỏ qua]` line counts the loads not reinforced |
| Revit `KataRebarWorkflow.Prepare/Generate`, `KataExportRequest`, `IKataExportRunner`, handler | carry the flag (preview and generation plan alike) |
| VM `AddJointRebar` (default off = Kata sheet only) | toggling re-plans the read sheet |
| Web `KataViewState.addJointRebar`, `KataUpdateRequest.addJointRebar`, footer checkbox before "Đọc thép Excel" | VI/EN text, mock bridge |

Default stays true in Core (KataRebar window, tests unchanged); Kata Export page starts unticked.

## Checks

- Core test: B01 with loads, flag off → no hanger, no joint zone, skipped note; on → as before.
- Build Debug.R26; Core tests; frontend typecheck/lint/test/build.
- Live: T1-DY8 unticked = no hanger / Ø8a50; ticked = back.

## Status

| # | Step | Status |
|---|---|---|
| 1 | Core flag + test (Core 1700/1700) | ✅ |
| 2 | Host + page (vitest 172/172, typecheck, lint) | ✅ |
| 3 | Build Debug.R26 (24.97 MB repacked) | ✅ |
| 4 | Live 2026-10-29, user Revit via AIM, 7 T1-DY8 elements, sheet cells written in memory into KataB03.xlsm opened read-only (closed unsaved): off 241 bars 413.2 kg + note "3 dầm giao…"; on 257 / 429.5 kg (`reports/live-t1-dy8-joint-*.png`) | ✅ |
