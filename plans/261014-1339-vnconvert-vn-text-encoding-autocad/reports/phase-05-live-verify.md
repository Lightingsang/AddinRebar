# Phase 5 — live verify, AutoCAD 2026 (2026-10-14 / 2026-10-16)

Drawing: copy of the user's `1-GHI CHU CHUNG KET CAU A1 2022.dwg` (`HPAutoCad/output/vnconvert-spike/real-ghi-chu-ket-cau.dwg`,
gitignored, never saved). Bridge = Debug bundle deployed 2026-10-16 07:55 (after the review fixes + font-face fix). Runs 798–821.

## Review round (5/10, `code-review-vnconvert.md`) — all Critical/High fixed before live
C1 trailing `\` hang · C2 double conversion (XData `HP_VNCONVERT`) · H1 tables never scanned (Table before BlockReference) ·
H2 styles remapped over unconverted text (per-style census, kept styles + held texts) · H3 forced charset (filter required, SHX style
needs fontMap) · M1 `.VnTimeH` ASCII upper-cased · M2 field texts skipped · M4 TrueType judged by face · M6 locked cells `CELL_LOCKED`.
M3 (2000-snapshot cap) observed fine at 175. M5 (VNConvert.dll inside the MSI) = user decision, pending.

## Live finding fixed here
Style remap wrote `FileName` after `Font` → face cleared (`Standard = '' / times.ttf`). Now only `Font` is set; AutoCAD resolves the
installed file (`times_1.ttf`, `ARIALNB_1.TTF` on this machine — Windows keeps `_1` copies). Hard-coded face→file map removed.

## Scenarios (all PASS)
| # | Scenario | Result |
|---|---|---|
| 1 | detect whole drawing | 474 examined, 175 to convert, 11 legacy styles, SHX styles → `NO_FONT_MAPPING` until fontMap |
| 1 | convert dryRun | 186 modified, `rolledBack`, drawing still 175 |
| 1 | convert commit + read back | texts correct (`ĐƠN VỊ THI CÔNG…`, `{\fArial…;TÊN CẤU KIỆN}`), Symbol φ + Microsoft Sans Serif `≥` runs untouched, 13/13 styles with a face, bold kept (Heo700 → Arial Narrow Bold); screenshot `output/vnconvert-spike/shots/after-convert.png` |
| 1 | rerun | 0 to convert, 175 "already converted" |
| 1 | `U` (COM) | 175 texts, styles, regapp restored |
| 2 | filter `A4` (shared style) | style Avo2 kept (12 left), text held, warning; with `remapStyles:false` only A4 converted + flagged |
| 3 | locked layer | atomic → refused, 0 written, LAYER_LOCKED; non-atomic → 164 failed, their styles kept, rest converted |
| 4 | fixtures (dryRun) | DBText `.VnTime` → `Trường THCS`, `.VnTimeH` → `GHI CHU CHUNG`, MLeader `BÊ TÔNG`, Table `CỘT`/`DẦM`, block text `KHUNG TÊN`, attribute `CẤY` |
| 5 | change set | `remapStyles:true` refused with guidance; `false` recorded → commit (175 snapshots, complete) → rollback restored 175, flags gone |

## Crash 2026-10-14 14:40 (AutoCAD exited after a `U`)
Not reproduced in 3 bisect attempts (texts only, texts + styles, + dryRun style probes) on 2026-10-16. No acad.exe Application Error /
CER dump for that minute; three acad.exe started at 14:42 (not by Claude). Logged as **not reproduced**.

## Gotchas
- Every bridge run, read-only too, is one `HPMCP` undo group: a check run between `U`s is what the next `U` undoes. Undo several steps
  with no run in between.
- `new ScriptArgs(JsonElement)` in a script → MissingMethodException (System.Text.Json of another load context): pass via `args`.
- Two `*D` blocks keep legacy text: orphan dimension blocks no Dimension references (invisible, PURGE removes them).
- After `U`, a VNI style's `FileName` can come back as `VAVON.TTF` instead of `vavon__0.TTF` (AutoCAD re-resolves from the face).

## Publish / regression
- `HPAutoCad.Mcp.Server` republished 2026-10-16 (running exe renamed `*.old-20261016.exe`): `tools/list` = 64 (62 + 2).
- `run-aec-tools-live.ps1` (published exe): 90/90 PASS.
- `run-aec-edit-tools-live.ps1` (published exe): 109/109 PASS (first attempt aborted before any check: UIA could not tick the opt-in; rerun clean).
- Offline: `HPAutoCad.Aec.Tests` 256/256, `HPAutoCad.Mcp.Server.Tests` 294/294, `HPCivil3d.McpBridge.Tests` 60/60.
- Docs: CLAUDE.md + AGENTS.md (same paragraph inserted by hand), skill `hp-mcp-autocad` (64 tools, workflow note, catalog regenerated).
