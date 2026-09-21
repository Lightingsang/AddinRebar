# E2E Test Infra: HPAutoCad & HPGeoLink

## Test Philosophy
- Opaque-box, requirement-driven. Derived strictly from ORIGINAL_REQUEST.md and user-facing specifications.
- Autonomous, unattended verification in AutoCAD 2026 via MCP bridge and harness scripts.
- Methodology: Category-Partition + Boundary Value Analysis (BVA) + Pairwise Combinatorial Testing + Real-World Workload Testing.

## Feature Inventory
| # | Feature | Source (Requirement) | Tier 1 (Coverage) | Tier 2 (Boundary) | Tier 3 (Pairwise) |
|---|---------|----------------------|:-----------------:|:-----------------:|:-----------------:|
| 1 | HPGEODIALOG | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 2 | -HPGEOKMZ | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 3 | HPGEOIMPORT | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 4 | -HPGEOIMPORT | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 5 | -HPGEOIMAGE | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 6 | HPGEOINFO | ORIGINAL_REQUEST §R1, R3 | 5 | 5 | ✓ |
| 7 | HPMCPBRIDGE / Named Pipe IPC | ORIGINAL_REQUEST §R2, R3 | 5 | 5 | ✓ |
| 8 | Shared Ribbon Tab HPAutoCad | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ |
| 9 | Single Bundle & ALC Isolation | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ |
| 10 | AEC Seeds & Standard Tools | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ |

## Test Architecture
- **Test Runners**:
  - `HPAutoCad/tools/harness/run-live-verify.ps1`: Full unattended MCP stdio session driving live AutoCAD 2026.
  - `HPAutoCad/tools/harness/run-bridge-unattended.ps1`: Direct Named Pipe JSON-RPC automated scenario suite.
  - `HPAutoCad/tools/harness/run-ribbon-check.ps1`: UI Automation ribbon tab, panel, and theme flip check.
  - `HPAutoCad/tools/harness/run-geolink-verify.ps1`: Dedicated HPGeoLink live verification suite.
- **Pass / Fail Semantics**:
  - All test scripts exit with code `0` on success, non-zero on failure.
  - Machine-readable JSON summary emitted (`summary.json`) reporting exact counts of passes, failures, and skips.
- **Directory Layout**:
  - Test scripts: `HPAutoCad/tools/harness/`
  - Test fixtures: `HPAutoCad/HPAutoCad.Tests/HPGeoLink/Fixtures/`
  - Test outputs & logs: `HPAutoCad/output/live-verify/`

## Real-World Application Scenarios (Tier 4)
| # | Scenario | Features Exercised | Complexity |
|---|----------|--------------------|------------|
| 1 | Full Cadastral Export & Satellite Overlay | Draw survey points + boundary -> Export KMZ via -HPGEOKMZ -> Fetch & insert satellite raster via -HPGEOIMAGE -> Inspect via HPGEOINFO | High |
| 2 | Round-Trip KMZ Export & Re-Import | Model space geometry -> -HPGEOKMZ -> -HPGEOIMPORT into new DWG -> Assert coordinate delta < 0.001 m | High |
| 3 | Modal Dialog Unattended Off-Screen Validation | HPGEO interactive dialog launched via background job -> off-screen PrintWindow capture -> verify dark/light theme render -> close cleanly via WM_CLOSE | Medium |
| 4 | Import Modal Dialog with Pasted Cadastral Coordinates | HPGEOIMPORT with pasted VN-2000 X/Y cadastral table -> auto pair order detection -> draw polylines on HPGEO-IMPORT layer | Medium |
| 5 | Multi-Workspace & Theme Dynamic Transition | Switch workspace (WSCURRENT) -> flip theme (COLORTHEME 0 <-> 1) -> verify single HPAutoCad tab with MCP and HPGeoLink panels and correct vector icon ink | Medium |

## Coverage Thresholds
- Tier 1: ≥5 test cases per feature (Total ≥ 50 test cases)
- Tier 2: ≥5 boundary/corner test cases per feature (Total ≥ 50 test cases)
- Tier 3: Pairwise combinations of major feature interactions (Total ≥ 10 test cases)
- Tier 4: ≥5 realistic end-to-end cadastral scenarios
- Total Target: ≥ 115 test cases across live and integration tiers
