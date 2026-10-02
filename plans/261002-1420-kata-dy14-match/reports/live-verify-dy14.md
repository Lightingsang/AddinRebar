# Live verify — T2-DY14 vs Kata drawing (Revit 2026.4, 2026-10-02)

Env: user's Revit (pid 25816) untouched — it had loaded the 12:27 DLL, so that file was renamed `*.locked-<stamp>` and the new build copied; second Revit on a fresh copy `thcphcs2-dy14-test2.rvt`. User's workbook `kata-rebar-live.xlsm` had been switched back to T2-DY7 (unsaved) — that was the "[Chặn] Hàng 11 có 7 cột … Revit đo được 9" the user saw: the block was right. Test ran on a copy of the saved file (`kata-rebar-dy14-check.xlsm`, B3 T2-DY14, 9 columns), closed without saving afterwards. Path = Kata Export: Đọc thép Excel (206 bars, 386.7 kg) → Tạo thép Revit.

x = mm from the outer face of column C, z from the beam top.

| Bar | Kata (DWG − 6732) | Revit | Δ |
|---|---|---|---|
| Top main 2Ø18 | 30 → 16615 | 42 → 16608 | ✅ ≤ 12 |
| Bottom span 1 (cut at E) | 35 → 6640 | 133 (inset, accepted) → 6640 | ✅ end |
| Bottom span 2 | 6135 → 13815 | 6142 → 13808 | ✅ 7 |
| Bottom spans 3-4 | 13310 → 16590 | 13310 → 16565 | ✅ 25 |
| C13 / C14 | → 2400 / 1900 | 2400 / 1900 | ✅ 0 |
| E13 / E14 | 4225–8700 / 4725–8200 | 4150–8700 / 4650–8200 | ⚠️ left −75 (Kata quirk at a cut support) |
| G13 ("-" I, "-" K) | 11150 → 16615 anchored in K | 11150 → 16608, leg in K | ✅ (fixed: dash chain) |
| G14 | 11650–14400 | 11650–14400 | ✅ 0 |
| D17 / D18 | 1475–5150 / 975–5650 | 1400–5150 / 900–5650 | ⚠️ left −75 |
| F17 / F18 | 7600–12250 / 7100–12750 | same | ✅ 0 |
| Side bars span 2 (F20 2f12) | 2 layers z −383/−217, 6405 → 13520 | 2 layers z −386/−214, 6330 → 13520 | ✅ z ≤ 3, left −75 |
| C ties | 14/16 Ø8a500 in every 2-layer section | 3.1.2C, 3.2.2C×2, 3.3.2C×2, 4.1.2C, 4.2.2C, 5.2.1C, 5.2.2C | ✅ |

First run showed G13 stopping at 16400 (one "-" hop only) → `KataTopBarContinuation` now follows consecutive "-" to the last one; re-run 16608 ✅.
Variant E11 = 500 (one bar cranked 6100 → 6700, all ends to ±25 mm) checked by unit tests only (`KataDy14DrawingTests`); not drawn in Revit (the model's E is 350).
