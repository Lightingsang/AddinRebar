---
name: dotnet-test-zero-tests-from-repo-root
description: `dotnet test <path>` run from the repo root silently runs 0 tests and exits 0 for the xunit.v3 MTP projects (HPRebar/, McpShared/) — always cd into the folder with global.json first
metadata:
  type: project
---

`dotnet test HPRebar/HPRebar.Mcp.Server.Tests` or `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` executed from the **repo root** prints only "Determining projects to restore…" and exits 0 with zero tests run.

**Why:** the repo root has no `global.json`; the `test.runner = Microsoft.Testing.Platform` pin lives in `HPRebar/global.json` and `McpShared/global.json`. Without it `dotnet test` uses the VSTest path, which the xunit.v3 MTP-only exe does not answer. Verified 2026-09-14 during the phase-0 McpShared review (66 + 105 tests appear only when run from inside the folder).

**How to apply:** when verifying a claim "tests pass", check the log shows `Test run summary: Passed!` with a non-zero `total:`; treat a restore-only log as "not run". Flag any doc/CI command that invokes `dotnet test` on these projects from the root (McpShared/README.md had one).
