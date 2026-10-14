# Code review — VNConvert (TCVN3 / VNI → Unicode) in HPAutoCad.Aec, 2026-10-14

Scope: `HPAutoCad.Aec/Text/*`, `Rules/vn-legacy-fonts.default.json`, `Cad/VnStyleCatalog|VnTextScanner|VnTextWriteService.cs`,
`AecTools.VnText.cs`, `Cad/ChangeSetReplay.cs` (diff), csproj/slnx diffs, 2 seeds, `HPAutoCad.Aec.Tests/VnConvert/*`,
`SeedLibraryTests.cs` diff, `tools/vnconvert/generate-golden-from-unikey-data.py`. ~1 300 LOC new. Vendored `ThirdParty/VNConvert`
read only for how HP code calls it.

Score: **5/10** — careful design (two-phase, caps, golden oracle, change-set guard), but 2 verified data-loss/hang defects
and 3 High gaps that put garbage on screen. Not ready for live verify until C1, C2, H1–H3 are fixed.

Evidence: findings marked **[probe]** were reproduced off-AutoCAD with a scratch console against the built
`HPAutoCad.Aec.dll` + `VNConvert.dll` (session scratchpad, not committed); **[metadata]** = read from
`AutoCAD.NET.Model 25.1.0/AcDbMgd.dll` with System.Reflection.Metadata.

## Critical

### C1 — `ConvertMText` never returns when contents end with a single `\` [probe]
[VnTextConverter.cs:38](HPAutoCad/HPAutoCad.Aec/Text/VnTextConverter.cs#L38): a trailing `\` fails `i + 1 < contents.Length`, falls to
`walk.Text`, whose loop ([:81](HPAutoCad/HPAutoCad.Aec/Text/VnTextConverter.cs#L81)) stops at `\` at once and returns `i` → outer
`while` spins forever. Probe: `ConvertMText("abc\\", VNI-Avo)` did not return in 3 s. Runs on the AutoCAD main thread; `ct` is checked
only per entity ([VnTextScanner.cs:55](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L55)), so the bridge timeout cannot stop it →
acad.exe hangs, unsaved work lost. Reachable from both seeds (detect too) via any MText / dimension override / table cell whose text
ends in a raw backslash (LISP/third-party writers, `C:\` typed into a dimension override).
Fix: treat a lone trailing `\` as text (`Text` must always advance ≥ 1 char), e.g. in `ConvertMText` take the `Code` branch only when
`i + 1 < Length`, else append `\` and `i++`. Add a test with trailing `\`, `\U+` truncated, `\f` without `;`.

### C2 — Already-Unicode text whose letters are all Latin-1 is converted again (acceptance (b) fails) [probe]
`HasLegacyBytes` ([LegacyTextDecoder.cs:31-44](HPAutoCad/HPAutoCad.Aec/Text/LegacyTextDecoder.cs#L31)) only recognises Unicode text by a
letter outside Latin-1 (ă đ ơ ư ĩ ũ, U+1EA0–U+1EF9). Many Vietnamese words use only Latin-1 letters (â ê ô à á è é ì í ò ó õ ù ú ý ã).
Probe: VNI `BE\xC2 TO\xC2NG` → once `BÊ TÔNG` → twice `B TƠNG`; Unicode `Cà phê` under a VNI style → `C ph`. Silent, irreversible
after save. The existing test `Converting_twice_leaves_converted_text_alone` passes only because every run happens to hold Ấ/Ệ/Ộ.
Triggers in the shipped workflow:
1. Change-set path: only `remapStyles: false` can be recorded ([ChangeSetReplay.cs:62-63](HPAutoCad/HPAutoCad.Aec/Cad/ChangeSetReplay.cs#L62));
   the style keeps its VNI face, so the follow-up direct call the refusal text recommends ("remap the styles in a direct call") re-scans
   and re-converts every Latin-1-only run. There is no styles-only mode.
2. Any re-run after a `remapStyles: false` run, or after a partial run (filtered / atomic false).
3. `sourceCharset` forced after a remap: the style is now Arial = `Unknown`, which forcing converts (see H3).
4. Unicode typed by a user under a still-legacy style.
Fix (pick one, test each trigger): (a) mark converted entities (XData regapp e.g. `HP_VNCONVERT` or an extension-dictionary flag) and
skip marked entities; plus (b) a `remapStylesOnly` / `texts: false` mode so the change-set flow never re-scans texts. A stricter
byte-pattern heuristic alone cannot separate `BÊ TÔNG` (Unicode) from a VNI byte string — do not rely on it.

## High

### H1 — Tables are never scanned: `Table : BlockReference` [metadata]
[VnTextScanner.cs:57](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L57) sends every `BlockReference` to `ScanAttributes`; `Table` derives
from `BlockReference` (AcDbMgd 25.1: `Table : BlockReference`, `MInsertBlock : BlockReference`), so the `case Table` in `ScanEntity`
([:115](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L115)) is dead, `includeTables` is a no-op, tables are not even counted in
`examined`. Worse with H2: a whole-drawing run still remaps the styles their cells use. Unit tests cannot see it (pure layer only).
Fix: `if (entity is Table) ScanEntity(...) else if (entity is BlockReference reference) ScanAttributes(...)`.

### H2 — Styles are remapped although texts that use them were not converted → garbage on screen
`PlanStyles` ([VnTextWriteService.cs:31-36](HPAutoCad/HPAutoCad.Aec/Cad/VnTextWriteService.cs#L31)) remaps every legacy style on an
unfiltered run, or every style a selected entity uses on a filtered run, and `Write` remaps after `atomic: false` skips
([:61-70](HPAutoCad/HPAutoCad.Aec/Cad/VnTextWriteService.cs#L61)). Texts left in legacy bytes but drawn in Arial afterwards:
- `includeBlocks: false` (title blocks), `includeTables` (+H1);
- `atomic: false` with locked/frozen layers (real drawings lock title-block layers; `atomic: true` refuses on *frozen* text, so users
  are pushed to `atomic: false`);
- skipped multiline attributes / attribute definitions ([VnTextScanner.cs:91](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L91), [:135](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L135));
- a filtered run: the selected entities' style is shared with every unselected text on other layers;
- fields (M2) once skipped.
This is the exact failure `NO_FONT_MAPPING` exists to prevent; the seed text even says "after every text succeeded"
(tool.json `remapStyles`), which is untrue. Fix: decide per style with a drawing-wide read: remap a style only when no remaining text
under it holds legacy bytes after this run (a cheap second pass over the same scan, unfiltered, counting leftovers per style id); else
keep it and report `stylesKept[{style, reason, leftoverCount}]` (or refuse under `atomic`).

### H3 — Forced `sourceCharset` converts text but never remaps / refuses its style; forcing also hits Unicode faces
`Effective` converts `Unknown` fonts when forced ([VnTextConverter.cs:47-52](HPAutoCad/HPAutoCad.Aec/Text/VnTextConverter.cs#L47)), but
`PlanStyles` keeps only `IsLegacy` styles and `MappingErrors` checks only those → an unlisted SHX (`vntime.shx`) style gets Unicode text
it cannot draw, no remap, no `NO_FONT_MAPPING`. `VnFontRules.Override` ("for a font no rule knows (forced sourceCharset)",
[VnFontRules.cs:84-91](HPAutoCad/HPAutoCad.Aec/Text/VnFontRules.cs#L84)) is never called outside `Classify` — the intended path is
unimplemented (K4 dead member). detect's own warning ([AecTools.VnText.cs:49-50](HPAutoCad/HPAutoCad.Aec/AecTools.VnText.cs#L49))
tells the AI to take this path. Also, Arial / Times New Roman / Microsoft Sans Serif are `Unknown`, so forcing rewrites their real
Latin-1 Unicode (°, ±, Ø, é, `Cà phê`) — and after a remap every converted style is Arial (C2 trigger 3).
Fix: under forcing, a style whose text changed becomes a remap candidate with `Override(face, file)` as target, else
`NO_FONT_MAPPING`; add a `"charset": "unicode"` kind to the rules for common Unicode TrueType faces so forcing never touches them.

## Medium

- **M1 .VnTimeH pure-ASCII runs stay lower case [probe].** `HasLegacyBytes` gates `Decode`, so `"ghi chu"` in `.VnTimeH` (drawn as
  capitals) stays lower case and shows lower case after the remap; a mixed run is fully upper-cased (`abc`+0xAD+`xyz` → `ABCƯXYZ`).
  Fix: for `UpperCaseOnly` fonts upper-case pure-ASCII runs too (`%%c` → `%%C` is still valid).
- **M2 Fields.** MText/DBText/attributes/cells with `DBObject.HasFields`: writing `Contents`/`TextString` drops the field link or is
  overwritten on the next field update/REGEN/plot back to the legacy value. Skip with reason `field` (counted in `skipped`, feeds H2).
- **M3 Change set snapshot cap.** `SnapshotBag.MaxSnapshots` 2000 ([ChangeSetSnapshots.cs:18](HPAutoCad/HPAutoCad.Aec/Cad/ChangeSetSnapshots.cs#L18)).
  The spike drawing alone: 467 MText + 141 dimensions, each `RecomputeDimensionBlock(true)` opening/erasing its `*D` entities for write
  (~5–8 each) → likely `Overflow`, rollback incomplete. Refuse recording an unfiltered call, or document + measure live (phase 5).
- **M4 Classify face-then-file.** [VnFontRules.cs:69-81](HPAutoCad/HPAutoCad.Aec/Text/VnFontRules.cs#L69) falls back to the file name
  when the face matches no rule, so a style with face `Arial` and a stale `FileName` `VNI-TIMES.TTF` is legacy and its Unicode text is
  converted. Classify by face when a face exists; by file only for SHX / empty face.
- **M5 Licence leak through the installer.** `VNConvert.dll` lands in `HPAutoCad.McpBridge/bin` → `Contents\Bridge\` (all files
  copied) → `install/Installer.Generator.cs:16-19` harvests the whole bundle into the MSI. NOTICE.md forbids distribution outside the
  company; nothing enforces it. 👤 user decision: guard (build error / internal-only MSI name) or accept.
- **M6 Table write not validated in phase 1** (after H1): a content-locked cell throws on `TextString =`, and any throw in the write
  loop aborts the whole run even under `atomic: false` instead of listing the item. Check `Cell.State`/content lock in phase 1.

## Low

- Unknown handles are silently ignored ([AecTools.VnText.cs:106](HPAutoCad/HPAutoCad.Aec/AecTools.VnText.cs#L106) only normalises) —
  other AEC write tools report `INVALID_HANDLE`; a typo returns success + 0 targets.
- Not covered, document in tool.json: DimStyle `Dimpost`/`Dimapost`, MLeader block-content attributes, `\S` stacked text,
  `\F` SHX inline names without `.shx` (`vn_vni*.shx` needs the extension), attribute layer filter ≠ INSERT layer.
- Envelope: `warnings` (one per legacy style / inline font / unknown SHX) and `legacyStyles` are uncapped; C0/C1 control chars in
  `Original` escape to 6 bytes (`\u0081`), which the 3-byte `ữ` worst case in `VnTextEnvelopeTests` does not cover. Cap warnings at
  ~20 + count; test with `\u0081` strings. Lone surrogates from `Cut` serialise as `\uFFFD` (checked) — harmless.
- Live-verify items (phase 5), GIẢ ĐỊNH CHƯA XÁC MINH: `FontDescriptor(face, bold, italic, 0, 0)` — charset 0 / pitch 0 render of
  Vietnamese in Arial; bold carried only by the descriptor flag while `Vhelvcb.ttf` is bold by file; setting `Font` then `FileName`
  keeps the face; `MLeader.MText` set-back keeps location/attachment; centred `AttributeReference` realigns (auto `AdjustAlignment`
  holds only while `db` is the working database); `RecomputeDimensionBlock` + rollback leaves no duplicate `*D` graphics.
- Clean Code: `VnTextWriteService.Write(..., bool remapStyles, bool atomic, ...)` M5; facade 14–16 parameters M4 (accepted AEC facade
  convention — the analyzer needs flat `args.X("literal")`); `AecTools.ItemFor` unused (K4); `VnFontRules.Override` dead (K4, see H3).
  All files < 300 lines (C2 ✓), file-scoped namespaces ✓, no plan codes in code (CM6 ✓ — "phase 1/2" is the two-phase write term).
- Golden generator: `ord(match.group(1)[-1])` mis-reads C escapes such as `'\n'`/`'\x..'`; harmless for the current tables (bytes are
  hex literals), assert no backslash literal instead.

## Acceptance criteria

| # | Criterion | Result |
|---|---|---|
| a | Format codes verbatim, runs per font in force, `{}` scope, Symbol/unknown untouched | ⚠️ walk correct, but C1 hang; H3 forcing touches Unicode faces |
| b | Never converts Unicode twice | ❌ C2 (verified) |
| c | Two-phase, atomic refusal, NO_FONT_MAPPING, styles after texts | 🟡 order ✓, nothing ForWrite before validation ✓; H2/H3 break the "no garbage" intent |
| d | Blocks: non-xref, non-dependent, anonymous only `*U`; `*D`/`*T` skipped | ✅ [VnTextScanner.cs:67-72](HPAutoCad/HPAutoCad.Aec/Cad/VnTextScanner.cs#L67) |
| e | Envelopes < 64 KB | 🟡 tested worst case ✓; uncapped warnings + 6-byte escapes not covered (Low) |
| f | Change set only with `remapStyles: false` | ✅ guard ✓; ⚠️ the flow it forces triggers C2; M3 cap |
| g | No regression to other seeds / WriteToolTable / EditContext | ✅ additive reader + validate branch; EditContext unchanged; seed counts updated |
| h | Clean Code rules, files < 300 lines | ✅ size; Low items above |

## Positive

Golden oracle from the human-written UniKey `data.cpp` with pinned sha256 and compute-then-write; vendored hashes pinned by test;
explicit `VnConvOptions` (struct — no shared static mutated; `GlobalOptions` never touched ✓); engines stateless; two-phase write
reuses `EditContext.LayerAllowsEdit`; `TrimAffected` keeps style handles; `*D`/`*T` reasoning backed by the spike; change-set guard
explains itself.

## Recommended order

1. C1 (one-line loop fix + tests). 2. C2 marker + styles-only mode. 3. H1 branch order. 4. H2 per-style leftover gate.
5. H3 forced-path remap/refusal + `unicode` font kind. 6. M1, M2, M4, M6; M3 decision. 7. Live verify incl. the Low API items.

## Unresolved (user)

- M5: block VNConvert.dll from MSI builds, or accept the risk for internal MSIs?

**Status:** DONE_WITH_CONCERNS
**Summary:** 2 Critical (verified hang on trailing `\`, verified double conversion of Latin-1-only Unicode), 3 High (tables never
scanned, styles remapped over unconverted text, forced charset path unfinished).
