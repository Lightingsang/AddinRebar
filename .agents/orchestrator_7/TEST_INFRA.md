# E2E Test Infra: HPRobot MCP Subsystem

## Test Philosophy
- Opaque-box and requirement-driven testing covering the entire Model Context Protocol lifecycle for Autodesk Robot Structural Analysis Professional 2026.
- Multi-tier verification: Category-Partition, Boundary Value Analysis, Pairwise Combinations, and Real-World Workload Testing.
- Strict isolation: tests verify HPRobot tools against standard MCP stdio JSON-RPC and Named Pipe wire protocols.

## Feature Inventory Mapping
| # | Feature | Requirement Source | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
|---|---------|-------------------|:------:|:------:|:------:|:------:|
| 1 | `execute_robot_code` (Core Tool) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 2 | `get_robot_context` (Core Tool) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 3 | `robot://` Resources (Core Tool) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 4 | Prompts (Core Tool) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 5 | Registry Meta Tools (8 tools) | ORIGINAL_REQUEST §R4 | 8 | 8 | ✓ | ✓ |
| 6 | `get_model_info` (Seed 1) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 7 | `get_structural_objects` (Seed 2) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 8 | `get_materials_and_sections` (Seed 3) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 9 | `get_coordinate_systems_and_grids` (Seed 4) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 10 | `get_load_definitions` (Seed 5) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 11 | `draw_bar_by_coords` (Seed 6) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 12 | `assign_node_support` (Seed 7) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 13 | `assign_bar_section` (Seed 8) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 14 | `assign_bar_load` (Seed 9) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 15 | `run_calculations` (Seed 10) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 16 | `get_node_reactions` (Seed 11) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 17 | `get_bar_forces` (Seed 12) | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 18 | `RobotTierAnalyzer` (3-Tier Safety) | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 19 | `RobotSnapshotManager` (RTD Backup) | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 20 | `RobotUnitsPolicy` (Metric Enforce/Restore) | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |

## Test Architecture
- **Unit & Host-Free Testing**:
  - `HPRobot.Mcp.Server.Tests`: Catalog completeness (24 tools), JSON schema validation, FakeExecutor pipe round-trips, Roslyn compilation of all 12 seeds against `Interop.RobotOM.dll`.
  - `HPRobot.McpBridge.Tests`: Roslyn AST tier classification, snapshot retention policy, unit standardization & restoration, permission gating.
- **Unattended Live Harness**:
  - Directory: `HPRobot/tools/harness/`
  - Scripts: `run-live-verify.ps1`, `live-verify.py` inheriting `McpShared/tools/harness_common.py`.
  - Automated steps: Connect to stdio server -> Initialize MCP session -> `tools/list` check (all 24 tools) -> `tools/call` for seeds -> Verify JSON results and exit code 0.

## Real-World Application Scenarios (Tier 4)
| # | Scenario | Features Exercised | Expected Outcome |
|---|----------|--------------------|------------------|
| 1 | Model Inspection & Diagnostics | `get_model_info`, `get_structural_objects`, `get_materials_and_sections` | Complete structural summary JSON |
| 2 | Parametric 2D Portal Frame Creation | `draw_bar_by_coords`, `assign_bar_section`, `assign_node_support` | New frame elements generated with snapshots |
| 3 | Load Definition & FEA Analysis | `assign_bar_load`, `run_calculations` | Calculation engine runs; status set to calculated |
| 4 | Internal Force & Reaction Verification | `get_node_reactions`, `get_bar_forces` | FX/FY/FZ/MX/MY/MZ extracted and match equilibrium |
| 5 | Destructive Safety Refusal | Mutation/Calculate without toggles | Refused with error code -32001 |
