# Kata B01 test against docs/specs/kata-beam-rebar-rules.md

Status: done 2026-10-04 (test + report, no algorithm change, not committed). Contract by grill-me (user: "implement").

Inputs: KataB1.xlsm sheet Dam (read-only COM dump), T2-DY7.dwg title "B01 (SL=1; L=33600)" (MCP AutoCAD,
transaction none), user's Revit 2026.4 "Dam kata test.rvt" with B01 selected (Kata Export ▸ Đọc thép only).

Outputs:
- [reports/b01-vs-rules.md](reports/b01-vs-rules.md) — rule by rule, Kata drawing vs HPRebar, mm.
- `HPRebar/HPRebar.Core.Tests/KataRebar/KataB01DrawingTests.cs` — 6 tests locking what already matches.
- Live: plan blocked (13 sheet columns vs 10 measured; supports of no width) → nothing generated.
