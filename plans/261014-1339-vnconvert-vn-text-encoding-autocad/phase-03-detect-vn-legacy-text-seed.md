---
phase: 3
title: "detect_vn_legacy_text (read-only seed)"
status: completed
priority: P1
effort: "6h"
dependencies: [1, 2]
type: AFK
---

# Phase 3: detect seed

## Overview
Vertical slice: pure text logic → Cad reader → `AecTools.VnText.Detect` → seed `detect_vn_legacy_text` (Annotation, `none`) → tests.
The AI scans a drawing and sees which styles/texts are legacy-encoded and their Unicode preview before writing anything.

## Requirements
- Pure (`HPAutoCad.Aec/Text/`, no AutoCAD types, unit-tested):
  - `VnLegacyFontClassifier`: font file/face → `Tcvn3 | VniWin | Unicode | Unknown`, flags `UpperCaseOnly` (`.VnTimeH`, `.VnArialH`…);
    patterns data-driven (embedded `Rules/vn-legacy-fonts.default.json`), `fontMap` defaults in the same file.
  - `VnTextDecoder`: storage form from phase 1 → bytes → VNConvert → Unicode; upper-cases for `UpperCaseOnly`; untouched ASCII.
  - `MTextRunParser`: splits `Contents` into runs by `\f…;` / `\F…;` codes, keeps every other format code verbatim, charset per run
    (run font, else style font); rebuilds contents with converted runs + remapped font names.
- Cad (`Cad/VnTextReader`): walks layouts + non-xref, non-layout block definitions (AttributeDefinition included), DBText, MText,
  AttributeReference, Dimension `DimensionText` override, MLeader MText, Table cells; resolves style per entity/run; skips xref/dependent.
- Facade `AecTools.VnText.DetectLegacyText(args…)`: envelope `{success, summary{styles, texts by charset, blocks, tables}, items[{handle,
  type, owner (layout|block name), style, charset, text (≤120 chars), preview (≤120)}], count, offset, truncated, warnings, errors}`;
  `limit` ≤ 200 + `offset`; SHX/unknown fonts → warning per style; empty drawing → success, count 0.
- Seed `SeedLibrary/Annotation/detect_vn_legacy_text/{tool.json, code.cs, examples.json}`: thin shim, schema ⇔ `args`, defaults = code
  fallbacks.

## Related Code Files
- Create: `HPAutoCad.Aec/Text/{VnLegacyFontClassifier,VnTextDecoder,MTextRunParser}.cs`, `HPAutoCad.Aec/Rules/vn-legacy-fonts.default.json`,
  `HPAutoCad.Aec/Cad/VnTextReader.cs`, `HPAutoCad.Aec/AecTools.VnText.cs`, seed folder, `HPAutoCad.Aec.Tests/VnConvert/*Tests.cs`
- Modify: `HPAutoCad.Aec/HPAutoCad.Aec.csproj` (embedded rule file), `HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs` (counts, caps)

## Implementation Steps
1. TDD the three pure classes (tracer bullet: one TCVN3 DBText string end to end, then MText runs, then upper-case fonts).
2. Reader + facade; envelope size test at `limit` max with 120-char texts (< 64 KB).
3. Seed + `SeedLibraryTests` (compile, shim rule, schema maximum = engine cap).
4. Build gate + `dotnet test` both test projects.

## Success Criteria
- [ ] Unit tests cover: each charset, mixed-run MText, `\P`/`\L`/`{}` codes preserved, upper-case fonts, unknown font warning, ASCII-only text untouched.
- [ ] Seed compiles in `SeedLibraryTests`; envelope < 64 KB at cap.
- [ ] Server `tools/list` = previous + 1.

## Risk Assessment
- MText code grammar edge cases (`\\`, `\{`, stacked `\S…;`) → parser keeps unknown codes verbatim, never converts inside a code.
- Text whose style is Unicode but content was typed under a legacy font → listed only when `sourceCharset` is given (no guessing).
