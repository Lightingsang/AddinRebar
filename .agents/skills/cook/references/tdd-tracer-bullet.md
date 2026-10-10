# TDD — tracer-bullet loop (`/bs:cook --tdd`, new behavior)

Adapted from Matt Pocock's `tdd` skill for this repository.

## Scope: pure code only

| Code | Verified by |
|---|---|
| `HPRebar.Core` (netstandard2.0, never references `Autodesk.*`), `McpShared` engine, `HPAutoCad.Aec`, MCP server-side code, `scripts/skill_sync` | **TDD loop below** (xUnit v3 / unittest) |
| Code that calls a host API — Revit `Document`/`Transaction`, AutoCAD `Database`, ETABS/SAP COM, Navisworks API, WPF views | Golden run, TUnit in-process, seed compile tests, live harness — as named in the phase and in `docs/clean-code/TOOL_DEVELOPMENT_WORKFLOW.md`. Do not fake the host with mocks to get a RED/GREEN cycle (`Document` is sealed). |

If the behavior lives in host code, first ask whether its logic can move into the pure layer
(the reason `HPRebar.Core` exists); TDD that part, keep the host part a thin adapter.

## Loop

1. **Plan with the user** — which public interface changes (names, inputs, outputs, units) and
   which behaviors must be tested, happy path and the edge cases that matter. Get approval before
   the first test. Prefer a deep interface: few entry points, logic hidden behind them.
2. **Tracer bullet** — write ONE test for the simplest behavior. Run it, see it **RED** for the
   right reason (assertion, not compile error or missing fixture). Write the minimal code to make it
   **GREEN**. Run the project's whole test set.
3. **Increment** — repeat RED → GREEN for each remaining behavior, one test at a time. Never write
   several failing tests at once; never write production code without a failing test asking for it.
4. **Refactor** — only when all tests are green: remove duplication, improve names, deepen the
   module. Tests stay green after every step. Agents avoid refactoring code still in their context —
   do this step deliberately, it is not optional.
5. **Report** — test counts before/after in `Kết quả Triển khai`; a test that was never seen RED is
   not evidence.

## Rules

- A failing test is fixed, never skipped, disabled or loosened (CLAUDE.md, REFACTORING_PLAN stop rule).
- Test names describe the scenario, never plan/phase codes.
- Units at boundaries are part of the interface — say them in names and test them (`lengthMm`, `fzKN`).
