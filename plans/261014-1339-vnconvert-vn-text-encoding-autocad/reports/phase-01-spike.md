# Phase 1 spike — legacy Vietnamese text in a real DWG (2026-10-14)

Drawing: user's `1-GHI CHU CHUNG KET CAU A1 2022.dwg` (opened by the user — the original, read-only runs only, `transaction: none`,
runs 785–788, `changed` 0/0/0 each). Copy kept at `HPAutoCad/output/vnconvert-spike/real-ghi-chu-ket-cau.dwg` for write tests.

## Facts (verified by runs 785–788)
- DWGCODEPAGE = SYSCODEPAGE = `ANSI_1252`.
- Entities: MTEXT 467, DIMENSION 141, INSERT 343 (6 attributes, ASCII only), LEADER 31, no DBText, no MLeader, no Table, 1 ATTDEF.
- Styles: 13. Legacy TrueType faces `VNI-Times` (Standard), `VNI-Avo` (×5 styles), `VNI-Helve-Condense` (×3, files `Vhelvcn.ttf`,
  `Vhelvcb.ttf` = bold). Unicode: `Arial` ×2. SHX: `vn_vni.shx` (VNI) and `uv-abc.shx` (TCVN3/ABC), both with big font `@extfont2.shx`.
- **Storage form:** each legacy byte is stored as the UTF-16 code unit of the same value (latin-1 style): `ĐƠN VỊ` in VNI-Avo is
  `D1 D4 4E 20 56 D2` (`ÑÔN VÒ`). No code unit in 0x80–0x9F seen; cp1252 specials (U+20AC…) handled anyway (map back to 0x80–0x9F).
- Real Unicode mixed in: U+2265 `≥` ×10, always inside a `\fMicrosoft Sans Serif|…;` run → runs under a Unicode font must stay untouched.
- Inline fonts in MText/dimension text: `VNI-Avo` 95, `Symbol` 88 (φ/δ via `f`/`d` — must stay untouched), `Microsoft Sans Serif` 13.
- Escapes: `\U+00B0` ×3 (degree, in `*D` dimension blocks) — a format code, never converted. No `\M+`.
- Legacy text inside block definitions: only anonymous `*D…` dimension blocks (regenerated from the Dimension) → skip `*D`/`*T`
  anonymous blocks, convert the Dimension's own `DimensionText` and recompute.
- Upper-case VNI text is real upper case (VNI has distinct capitals); TCVN3 `.VnTimeH`-style fonts not present here.

## Rules for the converter (phase 3)
1. A run's charset = its inline `\f` face if any, else the entity style's face/file → classifier (VNI*, `.Vn*`, `vn_vni.shx`,
   `uv-abc.shx`…). Symbol / Unicode faces → untouched.
2. Byte recovery: code unit ≤ 0xFF → byte; cp1252 special → its 0x80–0x9F byte; anything else → kept verbatim (split the run around it).
3. Format codes (`\P`, `\H…;`, `\C…;`, `\f…;`, `\U+XXXX`, `{` `}`, `\\` …) copied verbatim; only text between codes is converted.
4. Inline `\f<legacy face>|…;` → face replaced by the mapped Unicode face, flags kept.
5. SHX styles: charset detected by file name; font remap only with an explicit `fontMap` entry (no default target) → `NO_FONT_MAPPING`.
6. Default face map: VNI-Times → Times New Roman; VNI-Helve, VNI-Helve-Condense → Arial Narrow; VNI-Avo → Arial; `.VnTime*` → Times
   New Roman; `.VnArial*` → Arial (bold/italic carried by the style's Font flags).

## Fixture recipe (phase 5)
`MText.Contents = new string(vniBytes.Select(b => (char)b).ToArray())` with style face `VNI-Avo` reproduces the stored form exactly.
