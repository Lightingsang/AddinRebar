# 2026-09-17 — AEC engine phase E: structural tools, and two review rounds

## What landed

- Phase E of `plans/260916-1140-aec-automation-mcp-autocad/`: 7 `structural_*` seeds over `HPAutoCad.Aec/Structural/` (members, grids, checks, tagging) +
  `Cad/StructuralService` / `StructuralWriteService`. 47 tools on the AutoCAD server. Report: `reports/phase-E-structural-live.md`.
- The phase-D review round (5.5/10) and the phase-E review round (5/10) were both worked through the same day; every High/Medium is fixed and pinned
  (`CadStandardsReviewTests` 8, `StructuralReviewTests` 9). Tests 179 + 193, live 68/68 + 78/78.

## Decisions worth remembering

- **An existing mark is edited in place.** "Overwritten" used to write a second TEXT at the member's centre and leave the old one; now the mark
  TEXT (or the block's MARK attribute) is the write target, so a member never carries two marks and the drafter's placement survives.
- **A text is a mark only when its letters are a known prefix, and it belongs to one member.** Otherwise a column's C1 became the existing mark of
  every beam framing into it and a door tag D01 qualified as a beam mark. Under `prefixes {column: KC}` the old C marks are still visible
  (defaults ∪ prefixes) but `kept_foreign` — never renumbered silently unless `overwrite`.
- **Numbers are reserved as numbers.** `C01` reserves 1 whatever the padding; `C-12` is prefix C, number 12.
- **A block bubble's label lives in its attribute; a bubble must sit on the line's axis.** The nearest-labelled-bubble-within-reach rule took the
  corner bubble of the crossing grid 2 421 mm away.
- **An INSERT with attributes measures as its symbol.** `GeometricExtents` of a reference includes its attributes, so a column block with its MARK
  600 mm to the right read 400×900 with a spurious axis. The stand-in footprint now comes from the definition's extents (attribute definitions
  excluded, cached per definition); `boundsMm` for `query_entities` is unchanged.
- **Touching is not cutting.** A shaft opening tight against a column shares a face; that is `opening_near_column` at 0 mm, not a critical
  `opening_through_column` — `CutsInto` = proper crossing, a vertex strictly inside, or containment.
- **Every write tool needs a result cap, not only the readers.** `structural_tag_members` echoes each member three times; 300 members = 74 KB while the
  texts stay committed. `MaxTagMembers` 120 refuses up front with the way out (`kinds`, `filter`, `start`).

## Gotchas

- `SpatialRelation.Touches` treats a shared collinear edge as an overlap (not a touch) — do not use it to mean "boundary contact only".
- Git Bash heredocs halve backslashes: `newline="\n"` in a heredoc Python script arrives as `"
"`. Write patch scripts with the Write tool when
  the content carries backslashes or non-ASCII.
- The harness scene grew to 43 entities; every count-based check (Q paging, B classify, B structural paging) moves with it — update them together.
