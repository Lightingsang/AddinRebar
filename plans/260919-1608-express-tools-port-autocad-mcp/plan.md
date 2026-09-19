---
title: "Express-Tools ideas ported into HPAutoCad MCP — dimension chains, title-block numbering, double-line walls, piles, rebar blocks"
description: "Re-implement (never copy) the drafting workflows of a VinaCAD add-in (Formax01/VinaCAD_Express-Tools, develop) as seeds over the HPAutoCad.Aec engine: dimension-chain audit + running dims, title-block numbering + drawing list, double-line wall create/repair/thickness, pile detect/name/table/create, and an optional mapping-driven rebar schedule from attributed blocks. Plan only — nothing implemented."
status: planned
priority: P2
effort: 66h (54h without the optional phase 5)
branch: RebarVersion1
tags: [autocad, mcp, aec, dimensions, titleblock, walls, piles, rebar, seeds, express-tools]
created: 2026-09-19
revised: 2026-09-19
blockedBy: []
blocks: []
---

# Express-Tools ideas → HPAutoCad MCP

Source of the ideas: `https://github.com/Formax01/VinaCAD_Express-Tools` (branch `develop`), analysed 2026-09-19 —
[research/vinacad-express-tools-analysis.md](research/vinacad-express-tools-analysis.md). It is a **VinaCAD** (ODA Teigha) add-in,
**no license**, company code (Prima Solutions). Nothing from it can be installed into or copied into `HPAutoCad/`; what carries over is
the *workflow knowledge* of five command groups, re-designed as MCP seeds over `HPAutoCad.Aec` (AEC plan
[ADR-01](../260916-1140-aec-automation-mcp-autocad/adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) /
[ADR-02](../260916-1140-aec-automation-mcp-autocad/adr/adr-02-units-tolerance-handles-envelopes.md) stay the law).

> Đây là bước lập kế hoạch. Chưa thay đổi source code.

## Phases (ordered by value ÷ cost)

| Phase | File | Tools (category, transaction) | Effort | Status |
|---|---|---|---|---|
| 1 — Dimension chains | [phase-01](phase-01-dimension-chain-audit-and-running-dims.md) | `audit_dimension_chains` (Audit, none) · `create_running_dimensions` (Annotation, auto) · section `dimensions` in `audit_aec_drawing` | 12h | planned |
| 2 — Title blocks | [phase-02](phase-02-title-block-numbering-and-drawing-list.md) | `number_title_blocks` (Block, auto) · `generate_drawing_list` (Data, none) · `create_drawing_list_table` (Data, auto) | 8h | planned |
| 3 — Piles | [phase-03](phase-03-pile-tools.md) | `detect_piles` (Structural, none) · `name_piles` · `pile_coordinate_table` · `create_piles_from_table` (Structural, auto) | 10h | planned |
| 4 — Double-line walls | [phase-04](phase-04-double-line-walls.md) | `create_double_line_walls` · `repair_double_line_walls` · `change_wall_thickness` (Drawing, auto) | 24h | planned |
| 5 — Rebar schedule from blocks (optional) | [phase-05](phase-05-rebar-schedule-from-attributed-blocks.md) | `rebar_schedule_from_blocks` (Structural, none / auto with `write`) | 12h | **blocked on user answer** — only if the office uses attributed rebar-schedule blocks |

Server today: 62 tools. After phases 1–4: **74** (+12); with phase 5: 75.

## Decisions (apply to every phase)

| # | Decision | Why |
|---|---|---|
| D1 | **Re-implement from the description in the research note; never copy a file, a DWG, an image or a name list** from the source repo | No license → all rights reserved; README points at a private company GitLab |
| D2 | Tools are **seeds over `AecTools.*`**, logic in `HPAutoCad.Aec`; **no interactive prompt, no MessageBox, no new ribbon add-in** | The bridge guard denies `ed.Get*`/`SendStringToExecute`; every source command is a prompt loop, so a straight port could not run at all |
| D3 | **No template DWG.** Dim styles, text styles, blocks named by `args` must already exist in the drawing → `ArgumentException` listing the available names | The source depends on its own `TemplateBlocks.dwg`; shipping one would be both a licence problem and a hidden input |
| D4 | mm at the boundary, `EntityFilter` + handles, camelCase envelopes, page caps sized for the 64 KB output cap, caller mistakes = `ArgumentException` | ADR-02 |
| D5 | Every write seed: two-phase (validate read-only, then upgrade + apply), `dryRun`, `changeSetId` through `ChangeSetRecorder.TryRecord` + a reader in `WriteToolTable`, `atomic` where a batch exists | Phase C/I contracts; `ChangeSetTests` pins that every write seed is in `WriteToolTable` |
| D6 | Marks/labels are **edited in place** when they exist (`kept_existing` / `overwrite`), reading order by band clustering (`MemberTagging`) | Phase E rule; the source's "overwritten adds a second text" bug is on the review list already |
| D7 | Phase order 1 → 2 → 3 → 4; phase 5 only after the user confirms the block library question | Phase 1 fills a real gap (no dimension audit exists), 2–3 are cheap over existing services, 4 is geometry-heavy |

## Gate per phase (same as the AEC plan)

Build 0 warnings · `HPAutoCad.Aec.Tests` (pure) + `HPAutoCad.Mcp.Server.Tests` (`SeedLibraryTests`: shim rule, schema ⇔ `args`,
`default` ⇔ fallback, `maximum` ⇔ engine cap, every write seed in `WriteToolTable`) · a new step in `tools/harness/run-aec-tools-live.ps1`
(read tools) / `run-aec-edit-tools-live.ps1` (write tools: preview, apply, `U`, refusals) against AutoCAD 2026 · code-review round ·
`CLAUDE.md` + `AGENTS.md` regenerated · skill `hp-mcp-autocad` tool catalog. A tool is done only after it ran live.

## Not ported (and why)

`FDT` / `RenameText` / `RenameLayer` / `BB` / `BBE` / `IPT` — `query_entities` + `update_entities_batch` + `manage_blocks_attributes`
already cover them. `BT` / `NEO` / `NOI` / `BTCNN` / `DTT` — lookup tables shown as PNG, not a tool. `ABOUTEXPRESS` / `Sample` — nothing to port.

## User Review Required

> [!IMPORTANT]
> 1. Phase 5 — does the office draw rebar schedules with attributed blocks (one block per bar mark carrying diameter / count / segment
>    lengths)? **No → drop phase 5.** Yes → the tool takes the attribute mapping as an argument (D3), never a fixed block library.
> 2. Confirm the phase order (D7). **Recommendation:** start with phase 1 alone as its own `/bs:cook` run.
