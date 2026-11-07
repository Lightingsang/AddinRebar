# 0015 — HPFoundation: strip footing automation as a standalone tool, Excel stays the calculator

- **Status:** Accepted (2026-11-06, by user — plan `plans/261106-1549-mong-bang-etabs-excel/`)
- **Tags:** [PROJECT]

## Context
Engineers design strip footings in sheets of `5574-2018-mong-bang-T1.xls`: the sheet gives loads and soil springs, the
engineer builds an ETABS model by hand, types the moments and shears back, and the sheet sizes the rebar. The work
touches two hosts, and the MCP folders must not reference each other (Repository Layout in `CLAUDE.md`).

## Decision
1. **New folder `HPFoundation/`** with its own solution: `HPFoundation.Core` (net8.0, no COM), its tests, and
   `HPFoundation.App` (net8.0-windows) holding the Excel and ETABS adapters. It references no MCP folder and nothing
   from `McpShared/`, and follows `docs/clean-code/HP_CLEAN_CODE_CORE.md` with the COM standalone appendix.
2. **Excel stays the calculator.** The tool works on a private copy, so the 387 formulas the engineer trusts are not
   reimplemented. Only `F34` and `D39` are rewritten on the copy, to the ETABS moment convention the user chose.
3. **Own host instances.** The tool starts a hidden Excel and its own ETABS. `File.NewBlank` on a running ETABS would
   discard the engineer's model, so attaching to a running instance is not an option.
4. **Cells are found by their labels**, because sheets of one workbook differ in rows (`MB10- 4COT` has 174, others 176).
   A missing or ambiguous anchor stops the run; it is never guessed.

## Consequences
- Excel and ETABS must be installed; only `HPFoundation.Core` and its tests run without them.
- Tool results differ from the current sheet where the user decided so; the plan's phase 6 sets a new approved baseline.
- A later change to the sheet's labels stops the tool with a named anchor until the profile is updated.

## Rejected
- **MCP seed tools driven by an AI session:** results would depend on a session and on two bridges being attached.
- **Reimplementing the sheet in C#:** two sources of truth for the same formulas.
