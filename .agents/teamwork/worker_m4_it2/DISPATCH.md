# DISPATCH — worker_m4_it2

## Mission
Implement Milestone 4 Iteration 2 (Remediation of Benchmark Harness, Tool Contracts, and Skill Synchronization):
1. **Benchmark Harness `HPDynamo/tools/harness/run-benchmark.ps1`**:
   - Replace with the authentic process execution architecture designed by `explorer_dynamo_m4_it2_1` in Section 4 of `.agents/teamwork/explorer_dynamo_m4_it2_1/report.md`.
   - Wire `$ServerExe` to `mcp-call.py`, loop over `$ColdRuns` and `$WarmRuns` measuring real wall-clock durations with `[System.Diagnostics.Stopwatch]`, compute UTF-8 response bytes dynamically via `[System.Text.Encoding]::UTF8.GetByteCount`, enforce `dryRun: true` and active session shielding for WL-04, and write no-BOM UTF-8 JSON.
   - Execute the benchmark harness:
     `powershell -ExecutionPolicy Bypass -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 5`
     Confirm exit code 0 and empirical generation of `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`.
2. **Skill Synchronization `.agents/skills/hp-mcp-dynamo/SKILL.md`**:
   - Apply the exact drop-in text blocks from Section 4 of `.agents/teamwork/explorer_dynamo_m4_it2_2/report.md`.
   - Table 4.1: `execute_designscript` parameter `code` (not `script`).
   - Table 4.2:
     - `run_dyn_file` parameter `inputs` (not `parameters`).
     - `validate_dyn_file` documents `fileContent`.
     - `add_node_to_workspace` parameter `name` and `initialValue` (not `nickname`).
     - `connect_node_ports` parameters `sourceNodeId`, `sourcePortIndex`, `targetNodeId`, `targetPortIndex`.
     - `disconnect_node_ports` parameters `nodeId`, `portIndex`, `portType`, `targetNodeId`, `targetPortIndex`.
     - `export_graph_to_image` parameter `outputPath` and formats `PNG`/`JPEG`.
   - Section 5 Examples 1, 2, 3, 4: update arguments to match real parameters.
3. **E2E Test Contract Synchronization**:
   - Apply the contract synchronization from `.agents/teamwork/explorer_dynamo_m4_it2_3/report.md` (or `e2e_contracts_sync.patch`):
     - `HPDynamo/HPDynamo.E2E.Tests/Contracts/DynamoE2EContracts.cs`: add `"batch_edit_graph"` to `EmbeddedSeeds` (13 seeds).
     - `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs`: update `TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools` to assert 25 tools; update `TC_T1_SRV_08` to assert 13 seeds.
     - `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier3_CrossFeatureInteractionTests.cs`: update `TC_T3_INT_03`, `TC_T3_INT_07`, and `TC_T3_INT_08` to assert 25 tools.
     - `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier5_SeedParameterFuzzingTests.cs`: update class comment to 13 seeds.
4. **Verification**:
   - Verify all 5 test suites pass 100% green:
     `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`
     `dotnet test HPDynamo/HPDynamo.E2E.Tests`
     `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`
     `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`
     `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`
   - Verify `run-benchmark.ps1` runs cleanly with genuine process execution and updates `baseline-vs-upgraded.json`.
5. Deliver `handoff.md` and send message to parent when complete.
