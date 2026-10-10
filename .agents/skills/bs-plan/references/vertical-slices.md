# Vertical slices, AFK / HITL and blocking

Adapted from Matt Pocock's `prd-to-issues` / `to-tickets` skill (tracer-bullet slices), written for
plan phases in `plans/` instead of GitHub issues.

## Vertical, not horizontal

A phase is a thin slice that runs end to end through every layer it touches and leaves something
that can be observed — a test that passes, a tool that answers, a button that does one thing.

| Horizontal (avoid) | Vertical (prefer) |
|---|---|
| Phase 1 all Core models, phase 2 all services, phase 3 all XAML | Phase 1 one beam span: Core calculator + test + creator + one button path; phase 2 multi-span |
| Phase 1 every seed tool.json, phase 2 every code.cs | Phase 1 one seed (tool.json + code.cs + compile test + live call); phase 2 the next seeds |

The first slice is a tracer bullet: the thinnest path that proves the layers connect. Unknown unknowns
(API that throws, ALC that will not load, pipe that times out) surface there, not in the last phase.

## Type: AFK or HITL

| `type` | Means | Typical phases in this repo |
|---|---|---|
| `AFK` | The agent can finish and prove it alone: build, xUnit, seed compile tests, harness parts that need no host | Pure `HPRebar.Core` logic, McpShared engine, `*.Aec` engine, server-side tools, docs, skills |
| `HITL` | A human must act or decide before the phase is done | Live run in Revit/AutoCAD/Civil 3D/Navisworks/ETABS (open the host, answer SECURELOAD / unsigned prompt, pick a model), approval of a refactor wave (CLAUDE.md: never started without user approval), product/UX choice, push / deploy / anything irreversible |

Rules:
- Prefer AFK. Split a phase so its AFK part is not stuck behind its HITL part.
- One HITL step per phase where possible, stated in `## Success Criteria` as what the human checks.
- A HITL phase that is only "verify live" lists the exact harness or script the human runs and the
  expected result (e.g. `run-live-verify.ps1 -Runs 1` → 80/80).

## Blocking

`dependencies: [..]` in the phase frontmatter lists the phases that must be completed first.
- AFK phases with no shared files and no dependency between them can run in parallel (`/bs:cook --parallel`).
- Never let a phase depend on a later phase; re-cut the slices instead.
- `plan.md` phase table shows `Type` and `Blocked by` columns so the order is visible at a glance.

## Checklist before handing the plan over

- [ ] Every phase ends in something observable and verifiable.
- [ ] Every phase has `type` and `dependencies`.
- [ ] Each host-live verification, wave approval, product decision or irreversible step sits in a HITL phase.
- [ ] The first phase is the thinnest end-to-end slice.
