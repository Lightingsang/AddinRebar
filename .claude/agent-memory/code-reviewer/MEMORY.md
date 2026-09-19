## How the lead wants reviews
- [Review report shape](feedback_review_report_shape.md) — read-only, acceptance table + ranked file:line findings + score/verdict + Status footer
- [Phase review report conventions](feedback-phase-review-report-conventions.md) — per-phase report path/sections for the MCP bridge plans; read-only while a tester runs
- [Verify static gates with a scratch probe](feedback_verify_static_gates_with_scratch_probe.md) — scratch console against the bridge csproj; print guard/tier verdicts before ranking

## Recurring review checks (HP MCP hosts)
- [Harness review checks](project-hp-mcp-harness-review-checks.md) — pid guards, silent skips, pipe-name presence, "beside" servers on the live registry, vacuous audit/status checks
- [Live harness gotchas](project-mcp-live-harness-review-gotchas.md) — publishedAt tautology, Start-* throwing leaves host running, log tails need time filter, PS 5.1 OEM decode
- [Seed review pitfalls](project-hp-mcp-seed-review-pitfalls.md) — 64 KB cap arithmetic, nested args keys, units.Label == drawingUnit, mirror fence blind spots, Debug is deployed
- [Seed review gotchas](project-mcp-seed-review-gotchas.md) — stability excludes only ArgumentException, run_tool never validates schema, analyzer sees `args.` receiver only
- [AEC MCP recurring checks](project-aec-mcp-review-recurring-checks.md) — 32 defect classes from AEC phases A–I (caps, predicates, write ordering, change sets) + offline probe recipe
- [.mcp.json carries dev paths](project-tracked-mcp-json-carries-dev-path.md) — tracked file with machine paths; check it is not swept into a phase commit
- [dotnet test from repo root runs 0 tests](project-dotnet-test-zero-tests-from-repo-root.md) — cd into the folder with global.json first

## Verified platform facts
- [AutoCAD.NET assembly ownership + casing](project-autocad-net-assembly-ownership-and-casing.md) — AcCoreMgd vs AcMgd namespaces, lowercase `accoremgd` identity
- [AutoCAD script compile check offline](project-autocad-script-compile-check-offline.md) — nuget ref DLL paths + wrapper shape; OpenMode/Entity.Layer facts
- [AdWindows Ribbon API facts](project-adwindows-ribbon-api-facts.md) — RibbonItem/RibbonControl types, Geometry.Parse fill-rule gotcha
- [Navisworks Roamer host facts](project-navisworks-roamer-host-facts.md) — AssemblyResolve requester null, NextUndo name-only, Idle stops under modals
- [Navisworks ribbon + harness facts](project-navisworks-ribbon-api-and-harness-facts.md) — ribbon attribute defaults, PS continue-in-switch trap, UIA tab tautology
- [net48 pipe CurrentUserOnly + Polyfill](project-net48-pipe-currentuseronly-and-polyfill-facts.md) — owner-SID ACL semantics, ReadLineAsync(ct) on net48, untested net48 branches
- [net48 Roslyn script records/Span probe](project-net48-roslyn-script-records-span-probe.md) — what C# the Navis scripts can use on 4.8
- [Roslyn refuses pre-cancelled token](project-roslyn-script-refuses-precancelled-token.md) — RunAsync throws before running; bridges rely on it
- [STJ Utf8JsonWriter never flushes mid-walk](project-stj-utf8jsonwriter-never-flushes-midwalk.md) — only Stream overloads flush; matters for result serializers
- [Options binder pins computed defaults](project-options-binder-pins-computed-defaults.md) — `_x ?? Derive()` getters written back when a section has ≥1 key
