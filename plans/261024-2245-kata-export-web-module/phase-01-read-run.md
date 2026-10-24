# Phase 1 — Đọc run dầm → bảng xem trước

**Status:** Planned · **Type:** AFK

## Hợp đồng (Core/Contracts/KataContracts.cs)
- `kata.readRun {source: selection|pick}` → `KataRunSnapshot` (documentKey, run id = UniqueIds dầm, B3..B10 header, nhịp/gối mm, lưới, cảnh báo) — timeout 120 s, `SingleFlightRun`.
- `kata.buildSheet {run, overrides}` → `KataSheetPreview` (cột C.., hàng 11/19/20/21/22/23 theo `reports/kata-cell-contract.md` của plan 260926-2317) — thuần, không cần Revit (chạy Core).
- Event: dùng lại `model.stale` / `selection.changed`.

## Port (nguồn → đích `HPBimToolkit/Services/Kata/`)
`KataRunReader`, `KataSessionReader`, `KataSupportCollector`, `KataSolidReader`, `KataCandidateCollector`, `KataGridReader`, `KataHeaderReader`, `KataAxisFrame`, `RevitUnits`, `KataBeamGeometry`, `KataExportSession` (→ DTO). Pick: `Selection.PickObjects` với filter framing trong `Execute`. Ghi commit nguồn + danh sách file vào `reports/port-ledger.md`.

## Domain / Application
Validate run (cùng trục, thẳng, ≥ 1 dầm) dùng `KataInputValidator` của HPRebar.Core; điều phối trong `Modules/Kata/KataModule.cs`.

## Frontend
`src/modules/kata/`: chọn run, bảng nhịp/gối + header, cảnh báo, nút đọc lại; mock từ snapshot thật (JSON chụp live).

## Cổng
Core.Tests (map snapshot → sheet so với số tool WPF cho cùng dầm), frontend 4 lệnh npm, live: GMX3 / T1-DX12 trên bản sao (dầm đã verify ở plan 260926-2317) — bảng trùng tool WPF.
