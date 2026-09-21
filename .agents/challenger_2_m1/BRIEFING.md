# BRIEFING — 2026-09-20T22:35:00Z

## Mission
Adversarial stress-testing and empirical challenge of LayoutRangeParser, FileNameService, and PresetService for Milestone M1 (Smart Plot Pro).

## 🔒 My Identity
- Archetype: Empirical Challenger
- Roles: critic, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1
- Original parent: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Milestone: M1 (Parser, Sanitization & Concurrency Stress)
- Instance: 2 of 2

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Report failures as findings — do not fix them yourself
- .agents/ holds only agent metadata — NEVER place source code, tests, or data files here
- Empirical reproduction required: every challenge must be backed by executed tests

## Current Parent
- Conversation ID: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Updated: not yet

## Review Scope
- **Files to review**:
  - `HPAutoCad/HPAutoCad.Core/SmartPlot/Services/LayoutRangeParser.cs`
  - `HPAutoCad/HPAutoCad.Core/SmartPlot/Services/FileNameService.cs`
  - `HPAutoCad/HPAutoCad.Core/SmartPlot/Services/PresetService.cs`
  - `HPAutoCad/HPAutoCad.Tests/SmartPlot/`
- **Interface contracts**: `ORIGINAL_REQUEST.md` (## 2026-09-20T22:21:59Z)
- **Review criteria**: Robustness against malicious/invalid/corrupted inputs, memory protection, concurrency safety, zero unhandled exceptions.

## Attack Surface
- **Hypotheses tested**:
  1. `LayoutRangeParser`: Integer overflow (int.MaxValue, int.MinValue, 40-digit numbers), huge strings (100k chars), deeply nested delimiters, negative/zero indices, non-ASCII/Unicode, control chars, memory bounds via HardCap (10000). (Result: Robust, zero exceptions, passes).
  2. `FileNameService`: Control chars 0x00-0x1F, DEL, Unicode (Vietnamese, Japanese, symbols), empty tokens, MAX_PATH exceeding paths, Windows reserved device names with and without extensions. (Result: Control chars and unicode handled cleanly; reserved names with extensions like `aux.pdf`, `CON.txt` are NOT sanitized with `_` prefix).
  3. `PresetService`: Corrupted JSON syntax, truncated files, wrong types (arrays, primitives), empty/whitespace files, binary garbage, invalid enums, null items/fields, concurrent reads, concurrent writes, mixed read/write stress. (Result: Deserialization fallback works for syntax/type errors, but returns `null` items on `{"Presets": [null]}` or `{"Name": null}` causing fatal NREs in callers; concurrent writes collide with IOException and UnauthorizedAccessException due to lack of synchronization).
- **Vulnerabilities found**:
  1. [CRITICAL] `PresetService` does not validate or filter null presets/names after deserialization, allowing `[null]` or `[{"Name": null}]` to be returned by `LoadPresets()`, causing `NullReferenceException` in `GetDefaultPreset`, `GetPreset`, `SavePreset`, and `DeletePreset`.
  2. [HIGH] `PresetService` has zero locking/synchronization on file I/O: concurrent writes collide on `${FilePath}.tmp` throwing `IOException`, and mixed read/write causes `UnauthorizedAccessException` in `File.Move`.
  3. [MEDIUM] `FileNameService.SanitizeFileName` only checks exact matches in `ReservedNames`, failing to sanitize DOS reserved names with extensions (`aux.pdf`, `CON.txt`, `NUL.dat`).
- **Untested angles**:
  - Direct CAD runtime interaction (Milestone M2 scope).

## Loaded Skills
- None

## Key Decisions Made
- Added empirical test suite `HPAutoCad/HPAutoCad.Tests/SmartPlot/Challenger2StressTests.cs` covering 77 stress scenarios across all 3 components.
- Empirically reproduced and proved both vulnerabilities in `PresetService` and the edge case in `FileNameService`.
- Recommending verdict: REQUEST_CHANGES pending worker fixes.

## Artifact Index
- `handoff.md` — Final Challenger 2 assessment and verdict
- `progress.md` — Liveness heartbeat
