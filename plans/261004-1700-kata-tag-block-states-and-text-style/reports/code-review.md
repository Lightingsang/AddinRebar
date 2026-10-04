# Code review — KHT states + kata_text (2026-10-04)

No Critical/High. Fixed: M1 state formatter moved to Models (`KataTagState.Of`, enum `KataTagLayout` beside it; no Models→Calculators call),
M2 `CentredDrop` dropped — centred texts (T13 tag, circle numbers, grid bubbles) all go through `KataCadText.DrawCentredOn`;
L1 `StirrupTexts` / `StirrupTag` names, L2 `WidthFactor` private, L4/L5 docs, L6 test renamed (+ class `KataTagStateTests`),
L9 `CharWidth = CharWidthRatio * TextHeight`, L11 line wrapped. Kept: L3 (`Centred` documents DWG state T13), L7 (state change
already catches a merged text), L8 (second KataCadText instance, cheap), L10 (matches Kata's own spacing).
