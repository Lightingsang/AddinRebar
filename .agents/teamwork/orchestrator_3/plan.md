# Master Execution Plan: Helix 3D Beam Rebar Simulator & TCVN 5574:2018

**Orchestrator**: `orchestrator_3`  
**Parent**: Project Sentinel (`a708f6f4-aef7-4c05-adf5-a1300c76293e`)  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_3\`  

---

## 1. Milestones Overview & Execution Strategy

| Milestone | Scope | Deliverables | Verification Gate |
|---|---|---|---|
| **M1** | HelixToolkit.Wpf & ILRepack Setup | Add `HelixToolkit.Wpf 3.1.2` to `HPRebar.csproj`, ILRepack merge configuration | `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` succeeds (0 errors), DLL merged |
| **M4** | TCVN 5574:2018 Engine & Force Import | Materials, Flexure $A_s$, Shear $A_{sw}$, Spacing validation, Rebar optimizer, ClosedXML & ETABS force readers, Core Unit Tests | `dotnet test HPRebar.Core.Tests` 100% pass, Clean Code <= 300 lines |
| **M2** | Beam3DViewport Component | `Beam3DViewport.xaml`, `Beam3DMaterials`, `Beam3DConcreteMeshBuilder`, `Beam3DRebarMeshBuilder`, `Beam3DSceneBuilder` | 7 frozen models, <3ms CPU build, camera orbit/pan/zoom, 60+ FPS |
| **M3** | Reactive BeamRebar UI Integration | Segmented toggle `[2D]`, `[3D]`, `[Split]`, `GridSplitter`, reactive MVVM binding (<50ms debounce) | Live reactive updates, zero 2D canvas regressions |
| **M5** | Native Revit 2026 Rebar 3D & Excel Report | `TransactionGroup("Beam Rebar")`, `BarTerminationsData`, `ClosedXML` calculation report exporter | Clean rebar generation in Revit 2026, .xlsx report matches TCVN |
| **E2E** | Dual Track E2E Test Suite | Opaque-box test suite for calculation, import, and rebar generation | `TEST_READY.md`, 100% test pass |
| **Final** | Final Hardening & Delivery | Phase 1: 100% E2E tests pass; Phase 2: Adversarial coverage hardening (Tier 5) | Forensic Audit CLEAN, Sentinel Victory Claim |

---

## 2. Iteration Loop Protocol per Milestone

For each milestone:
1. **Worker** (`teamwork_preview_worker`):
   - Given Explorer findings, authoritative request, and Clean Code constraints.
   - Implements code and runs build/tests.
   - Enforces Mandatory Integrity Warning (DO NOT CHEAT, no hardcoded results).
2. **Reviewers** (2 x `teamwork_preview_reviewer` in parallel):
   - Review code quality, architecture adherence, Clean Code rules (<= 300 lines/file, <= 50 lines/method), edge cases.
3. **Challengers** (2 x `teamwork_preview_challenger` in parallel):
   - Empirically test functionality, stress-test inputs, boundary conditions.
4. **Forensic Auditor** (`teamwork_preview_auditor`):
   - Audit code integrity, static analysis, verify no cheating, no facades, no bypasses.
   - Binary Veto: INTEGRITY VIOLATION fails iteration unconditionally.
5. **Gate**:
   - Record in `GATE_STATUS.md`. Pass only if: Build/tests pass, all Reviewers APPROVE, all Challengers APPROVE, Auditor CLEAN.

---

## 3. Immediate Next Steps

1. Launch **Milestone M1**: Dispatch `worker_m1` to add `HelixToolkit.Wpf 3.1.2` to `HPRebar.csproj` and verify `Debug.R26` build.
2. Launch **Milestone M4**: Dispatch `worker_m4` to implement the TCVN 5574:2018 calculation engine, materials, and comprehensive xUnit tests in `HPRebar.Core` and `HPRebar.Core.Tests`.
3. Launch **E2E Testing Track**: Dispatch `test_writer_e2e` to establish `TEST_INFRA.md` and opaque-box acceptance tests.
