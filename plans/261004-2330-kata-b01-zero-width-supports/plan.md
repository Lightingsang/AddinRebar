# Kata B01 rules N1–N3: inherited rows 20/21, supports of no width, console end

Status: done 2026-10-04 (Core 1289/1289, R26 build, review 7/10 → fixed, live B01 row 11 matched — see [reports/live-b01.md](reports/live-b01.md); not committed). Source: [B01 report](../261004-2230-kata-b01-live-test/reports/b01-vs-rules.md) §2; user
"tiếp tục" on the recommendation "N1 + N2 + N3 first, B01 then no longer blocked".

## Rules
| # | Rule | Where |
|---|---|---|
| N1 | Span row 21 empty → previous span's soffit step; row 20 empty (or no readable bar) → previous span's side bars; `0` resets. First span: empty = 0 / G4-G5 | parser loop |
| N2 | Interior support with row 11 = 0 is not a support: spans on both sides merge (length sum, left span's cells); filled cells of the support column and of the merged right span are reported as not drawn | new `Parsers/KataZeroWidthSupports.cs`, called by the parser |
| N3 | Row 11 = 0 at the first / last column = console end: not a sheet cell compared with Revit (Revit has no segment there); scope no longer blocks it, warns that console bars follow HPRebar's provisional rule | `KataSheetGeometryCheck.SheetSequence`, `KataScopeFilter` |

## Out of scope
N4 top drop, N5 split at crossing beam, N6 bottom-extra cut, console rules vs Kata (R-92) — stay ❌ in the report.

## Gate
- Core tests green (DY7 / DY14 goldens unchanged), B01 test updated: plan not blocked, 5 spans, H+J = 6500.
- `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false`.
- Code review. Live: B01 in user's Revit (`Dam kata test.rvt`) → Đọc thép không chặn → Tạo thép → đo.
