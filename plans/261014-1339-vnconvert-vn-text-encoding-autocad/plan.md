---
title: "VNConvert vendored into HPAutoCad — TCVN3/VNI → Unicode text conversion as two MCP seeds"
description: "Copy the managed VNConvert library (AJS/lisp.vn, commit 205e569) into HPAutoCad/ThirdParty, wrap it in HPAutoCad.Aec, expose detect_vn_legacy_text (none) and convert_vn_text_encoding (auto, dryRun, change set). Internal use only."
status: completed
priority: P2
effort: 25h
branch: RebarVersion1
tags: [autocad, mcp, aec, seeds, vietnamese, tcvn3, vni, encoding, third-party]
created: 2026-10-14
blockedBy: []
blocks: []
---

# VNConvert → HPAutoCad (TCVN3/VNI → Unicode)

Requirement contract agreed with the user 2026-10-14 (`/grill-me`): approach A, copy now, internal use only, MCP seeds only,
convert text **and** remap style fonts, include non-xref block definitions + tables, live on a generated DWG + 1 real user DWG copy.

## Source audit (2026-10-14, clone in session scratchpad, never executed)
- `https://github.com/AutoLISP-Just-Simple/VNConvert` @ `205e569`, 3 commits, 670 KB, no binaries.
- Clean: no network / process / registry / reflection / Base64 / P/Invoke in managed code; no AI-targeting files (CLAUDE.md, AGENTS.md, prompts).
- Issues: **no LICENSE**; port of UniKey `vnconv` (Phạm Kim Long, GPL); README "code lại bằng Google Gemini" = unreviewed AI code;
  `NativeExports.VnConvErrMsg` returns a pointer taken inside `fixed` (dangling); `.tlog` files leak author's OneDrive paths;
  legacy C++ `strcpy(outDir[256])` + `system("mv …")` (non-Windows). NativeExports, CLI, C++ are NOT copied.
- Licence risk accepted by the user: internal use only, never distributed outside the company → recorded in `NOTICE.md`.

## Phases

| # | Phase | Type | Depends | Effort | Status |
|---|---|---|---|---|---|
| 1 | [Spike: how legacy Vietnamese text is stored in a DWG](phase-01-spike-dwg-legacy-text-storage.md) | HITL | — | 2h | completed ([report](reports/phase-01-spike.md)) |
| 2 | [Vendor VNConvert + golden tables](phase-02-vendor-vnconvert-golden-tables.md) | AFK | — | 3h | completed (golden 7/7, port = UniKey tables) |
| 3 | [detect_vn_legacy_text (read-only seed)](phase-03-detect-vn-legacy-text-seed.md) | AFK | 1, 2 | 6h | completed, verified live |
| 4 | [convert_vn_text_encoding (write seed + change set)](phase-04-convert-vn-text-encoding-seed.md) | AFK | 3 | 10h | completed, verified live |
| 5 | [Live verify AutoCAD 2026 + publish + docs](phase-05-live-verify-publish-docs.md) | HITL | 4 | 4h | completed ([report](reports/phase-05-live-verify.md)) |

Phases 1 and 2 run in parallel. Phase 1 needs the user's real DWG path.

## Key decisions
| Decision | Choice | Why |
|---|---|---|
| Packaging | New project `HPAutoCad/ThirdParty/VNConvert` (net8.0), referenced by `HPAutoCad.Aec` | third-party code isolated, auditable, removable |
| Copy scope | `src/VNConvert` minus `NativeExports.cs` | unsafe C-ABI unused + dangling-pointer bug |
| Vendored edits | none except to build; each logged in `NOTICE.md` | keep diff to upstream reviewable |
| Oracle | golden JSON generated from the **original human-written** UniKey `vnconv/data.cpp` tables | comparing the Gemini port against itself proves nothing |
| Charset detection | by style font (`.Vn*` TCVN3, `VNI-*` VNI-Win), `sourceCharset` arg overrides | DWG stores no charset tag |
| Font remap | default map overridable by `fontMap`; SHX / unknown fonts warned, never guessed | wrong guess silently corrupts text |
| Bridge | `HPAutoCad.McpBridge.csproj` untouched | HPCivil3d mirror test pins it |

## Deviations from the agreed contract (2026-10-14, during implementation)
- Change sets hold only `remapStyles: false`: `SnapshotBag` clones entities only, text styles cannot be rolled back. `U` still undoes everything.
- Detect page cap 40 (not 200): worst-case page in 3-byte UTF-8 must stay < 60 KB (`VnTextEnvelopeTests`).
- SHX legacy fonts are classified (vn_vni.shx = VNI, uv-abc.shx = TCVN3) but have no default face: `fontMap` required.
- Already-Unicode text (contains ă đ ơ ư ĩ ũ / U+1EA0–1EF9) is never re-converted; all-Latin-1 Unicode words under a legacy style still would be.

## Open
- M5: `VNConvert.dll` ends up in the MSI (`install/` copies the whole bundle) — user decision: internal-only MSI, or exclude it.
- Crash after `U` on 2026-10-14 not reproduced (3 bisect runs).

## Out of scope
CLI, NativeExports, ribbon/command, Civil 3D, Revit/Kata, distribution outside the company.

## Related plans
`260919-1608-express-tools-port-autocad-mcp` (planned) and `260916-1140-aec-automation-mcp-autocad` (complete) add AEC seeds too —
no shared files beyond seed counts in `SeedLibraryTests`; no blocking relation.
