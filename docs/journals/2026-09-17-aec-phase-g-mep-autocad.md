# 2026-09-17 — AEC engine phase G: MEP networks, and what equipment does to a graph

## What landed

- Phase G of `plans/260916-1140-aec-automation-mcp-autocad/`: 3 `mep_*` seeds over `HPAutoCad.Aec/Mep/` (network builder, checks) + `Cad/MepService`.
  55 tools on the AutoCAD server. Report: `reports/phase-G-mep-live.md`.
- Review round 5.5/10 → fixed the same day; `MepTests` 6. Tests 197 + 241, live 83/83 + 89/89.

## Decisions worth remembering

- **A node is attached to every run that passes through it.** The first cut tested nodes only against run *ends*, so every inline valve, pump
  and VAV on an unbroken pipe was an "orphan" and a branch ending inside a VAV on the main made two networks.
- **A node never merges systems.** Union-find runs over runs; a node joins only runs of the same system and belongs to every network it
  touches. Otherwise supply + return + ducts through one AHU were one "mixed" network on every plan, and the real "supply drawn onto a
  return" case drowned.
- **A copy a few mm off-axis is a duplicate, not two tees.** Duplicates are found by projection (parallel, offset ≤ endpointConnection,
  shared run > tolerance.duplicate), and each network reports how much of its length is drawn twice.
- **Facing open ends are one near miss** at the midpoint (phase F's gap lesson again); a ring main whose ends stop 60 mm apart finds its own
  far end — a run's own body counts except for the segment the end sits on.
- **Every list inside a paged item needs a cap** (`connectedTo` at a manifold header, `mixed_system` handles/systems): the size test must use
  the unfriendly case.
- **Single-line drafting is the contract**; double-line ducts are detected heuristically and warned, not paired into centrelines.

## Gotchas

- The seed folder name must equal the category (`MEP`, not `Mep`), and a rename leaves stale embedded resource names in `obj/` — clean it.
- A no-runs drawing must not turn every fixture into an orphan warning: warn once, report nothing.
