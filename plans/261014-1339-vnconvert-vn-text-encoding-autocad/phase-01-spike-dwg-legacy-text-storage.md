---
phase: 1
title: "Spike: how legacy Vietnamese text is stored in a DWG"
status: pending
priority: P1
effort: "2h"
dependencies: []
type: HITL
---

# Phase 1: Spike — legacy text storage

## Overview
Prove, on a copy of one real user DWG, what string AutoCAD 2026 hands to .NET for text drawn with `.VnTime` (TCVN3) and `VNI-Times`
(VNI-Win) fonts, so the converter recovers the original bytes correctly. Output: `reports/phase-01-spike.md`.

## Requirements
- Read-only on a **copy** of the user's DWG (user supplies path); never save.
- Answer: (a) DWGCODEPAGE of the drawing; (b) for each legacy style, the UTF-16 code units of 5+ sample strings (DBText, MText, attribute);
  (c) does `Encoding.GetEncoding(1252).GetBytes(text)` give the TCVN3/VNI bytes (incl. 0x80–0x9F, which cp1252 maps to € ‚ ƒ …)?
  (d) any `\M+nXXXX` / `\U+XXXX` escapes in `TextString`/`Contents`; (e) MText inline `\f<font>|…;` codes present?;
  (f) SHX fonts used for Vietnamese (`vntime.shx`-like) and big-font settings; (g) `.VnTimeH`-style upper-case fonts present?
- A self-made DWG reproducing (b) exactly becomes the harness fixture.

## Implementation Steps
1. Ask user for the DWG path; copy to `HPAutoCad/output/vnconvert-spike/`.
2. Open the copy in AutoCAD 2026; through `execute_autocad_code` (`transaction: none`) dump styles (name, FileName, BigFontFileName,
   TrueType font via `Font`), DWGCODEPAGE, and code-unit dumps (`U+XXXX`) of the first 20 text-like entities per legacy style.
3. Decode candidates offline (scratchpad script): cp1252 bytes → VNConvert TCVN3/VNI → Unicode; eyeball against the drawing.
4. Write fixture recipe: which API call reproduces the stored form (e.g. `DBText.TextString = cp1252.GetString(tcvn3Bytes)`).
5. Report findings + any contract change (if storage is not byte-recoverable → stop and ask user).

## Success Criteria
- [ ] Byte-recovery rule stated per storage form, each verified on ≥ 5 real strings.
- [ ] Fixture recipe reproduces the real storage code-unit for code-unit.
- [ ] Report lists forms NOT handled (if any).

## Risk Assessment
- Real DWG may mix forms (SHX big font, `\M+`) → handle each form found; unknown form = warning in detect, never converted.
- User DWG confidential → stays under `HPAutoCad/output/` (gitignored), not committed.
