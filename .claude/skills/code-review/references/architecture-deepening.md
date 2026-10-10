# Architecture deepening — `/bs:code-review codebase deepen [path]`

Adapted from Matt Pocock's `improve-codebase-architecture` skill, bounded by this repository's
Clean Code governance (CLAUDE.md: refactoring follows the approved waves of
`docs/clean-code/REFACTORING_PLAN.md`, never mixed with features or fixes, never started without
user approval; ADR-0007 scope).

**Read-only.** This mode never edits code, never edits `docs/clean-code/CLEAN_CODE_AUDIT.md` or
`REFACTORING_PLAN.md`, and never creates GitHub issues.

## Lens: deep vs shallow modules

A **deep module** has a small interface hiding a large implementation (John Ousterhout,
*A Philosophy of Software Design*). Deep modules are easy to test in isolation and easy for an
agent to navigate. Signals of a **shallow** module or a missing deep one:

| Signal | Example of what to look for |
|---|---|
| One concept scattered over many small files | the same geometry or unit knowledge spread across `Service/` files of several features |
| Interface about as large as the implementation | pass-through classes, wrappers that forward every call, "Helper"/"Manager" with many tiny methods |
| Callers must know the internal order of calls | open → configure → validate → apply sequences repeated at each call site |
| Copy-paste skeletons | handler/queue/TCS boilerplate repeated per feature |
| Tight coupling across a boundary | a feature reaching into another feature; Core needing a host type |
| Hard to test | logic only reachable through a host API (`Document`, `Database`, COM) although it is pure math |

Check each candidate against existing records first: if `CLEAN_CODE_AUDIT.md` already has it
(`AUD-xxx`, `B-xx`, `H-xx`) or an ADR decided it, cite that id instead of re-proposing.

## Step 1 — Scan and report

Scope: the `path` argument, else the folders governed by ADR-0007 (`HPRebar/`, `McpShared/`, MCP host
folders). Skip generated, `bin/`, `obj/`, seed libraries.

Write `plans/reports/architecture-deepening-<YYMMDD>.md`:

```markdown
# Architecture deepening candidates — <date> — scope <path>

| # | Candidate | Signal | Evidence (file:line) | Deep-module idea (one line) | Existing record | Wave fit |
|---|---|---|---|---|---|---|
| 1 | ... | scattered concept | `a.cs:12`, `b.cs:40` | one `X` service owning ... | — / AUD-005 | W2 / new |

## Not candidates (checked, fine as is)
- ...
```

Rules: 3–10 candidates, ranked by payoff (test- and agent-friendliness) over risk; every row has
file:line evidence; no fixes, no diffs. Then stop and ask the user which **one** candidate to explore
(or none).

## Step 2 — On the user's pick: three interface designs

For the single candidate the user picked, spawn **3 sub-agents in parallel**, each told to design a
*radically different* interface for the deepened module (e.g. one minimal facade, one
data-in/data-out pure function set, one object owning the lifecycle). Each returns: interface
signatures, what moves behind it, call-site change, test strategy, risk.

Compare the three in a table, recommend one or a hybrid, and write an RFC to
`plans/<YYMMDD-HHmm>-<slug>-rfc/plan.md` (problem, evidence, options, recommendation, test/golden-run
gate, proposed wave). Do not implement.

## Step 3 — Hand-off

- The user decides whether the candidate becomes an `AUD-xxx` row (by hand, columns
  `Id | Tag · Rules | Sev | Location | Fact | Action · Wave`) and which wave takes it.
- Implementation happens only inside an approved wave batch of `REFACTORING_PLAN.md`, with its
  golden-run / test gates.
- A behaviour defect found while scanning is logged in the report, not fixed silently.
