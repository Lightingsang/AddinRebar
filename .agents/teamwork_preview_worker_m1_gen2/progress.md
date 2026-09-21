# Progress Log — teamwork_preview_worker_m1_gen2

Last visited: 2026-09-22T00:48:30Z

## Completed
1. Updated `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:
   Added `"PickFace"` and `"CommitChanges"` to `GuardProfile.Tekla.deniedMembers`.
2. Updated `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs`:
   Updated vulnerability test cases to assert that parenthesized receiver `(model).CommitChanges()`, aliased receiver `m.CommitChanges()`, and `picker.PickFace()` are strictly blocked by `ScriptGuard.Check`. Added test aliases for all variants.
3. Updated `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaMilestone1ChallengerTests.cs`:
   Updated evasion test cases to assert that `CommitChanges` on aliased and parenthesized receivers is blocked. Added `PickFace` test cases to theory.
4. Updated `McpShared/HPRebar.Mcp.Server.Core.Tests/TeklaProfileTests.cs`:
   Added test cases for `PickFace` and `CommitChanges` variations.
5. Executed test suite:
   - `dotnet test HPRebar.McpBridge.Core.Net48Tests`: 113 / 113 passed (0 failed, 0 skipped).
   - `dotnet test HPRebar.Mcp.Server.Core.Tests`: 742 / 742 passed (0 failed, 0 skipped).
   - `dotnet test HPRebar.Mcp.Server.Tests`: 109 / 109 passed (0 failed, 0 skipped).
6. BRIEFING.md updated.
