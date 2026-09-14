# PM status — AutoCAD MCP bridge, phase 1 closed (2026-09-14)

## Plan sync-back (all phase files swept)

| Phase | File | Todos | Status |
|---|---|---|---|
| 0 | phase-00 | 8/8 | done (598da4c) |
| 1 | phase-01 | 7/7 | **done** — built, tested, verified in AutoCAD 2026; review 7/10 → 16/16 resolved (cc1868b + bae2fd2) |
| 2 | phase-02 | 0/9 | planned — ready; updated: executor timeout/unsubscribe rule, spike removal moved to step 7 "as soon as pipe harness works", `ExecuteInApplicationContext` branch removed |
| 3–5 | phase-03..05 | 0 | planned |

`plan.md` progress: 2/6 phases done. YAML `status: in-progress` unchanged.

## Evidence

- Build: `HPAutoCad.slnx` Debug + Release 0 warnings / 0 errors; bundle 24 files / 14 MB.
- Tests: McpShared 70/70, HPRebar MCP 106/106 (no shared file touched by the fix; rerun anyway).
- Live: spike 5/5 ×3 runs; run 3 fully unattended incl. SECURELOAD auto-answer and self-quit (exit 0).
- Reports: `phase-01-spike.md` (runs 1–3), `phase-01-test-report.md` (9/9 gates), `phase-01-code-review.md` (+ resolution table).

## Decisions recorded

- ADR-02 Accepted: Idle one-shot; `ExecuteInApplicationContext` rejected (blocked indefinitely on run 2). Executor rule: complete request before unsubscribe; handler no-op on completed request.
- ADR-05 Accepted: SECURELOAD prompts on every new loader hash — *Always Load*; signing deferred to pack.
- Spike gated by `HPAUTOCAD_MCP_SPIKE=1`; deleted in phase 2 once the pipe harness runs.

## Open (not blocking)

- Modal-dialog-while-idle case untested (phase 5, manual).
- Whether *Always Load* writes `TRUSTEDPATHS` or remembers the hash — docs in phase 5.
- Unattended harness (`run-spike-unattended.ps1`, SECURELOAD auto-click) lives in the session scratchpad only; phase 2 decides whether a repo copy under `HPAutoCad/` is worth it.

## Next

`/bs:cook plans/260913-0000-autocad-mcp-bridge-2026/phase-02-autocad-bridge-runtime-threading-transactions-context.md` — user confirmation required before starting.
