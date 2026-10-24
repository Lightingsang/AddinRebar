# Phase 0 — ADR-0009 + HPRebar.Core reference + spike (2026-10-24)

## Done
- ADR-0009 (Accepted), `adr/README.md`, DEPENDENCY_RULES D8.
- `HPBimToolkit.Core.csproj` → `ProjectReference ..\HPRebar.Core`; `HPRebar.Core.dll` lands beside the toolkit (deploy folder `Addins\2026\HPBimToolkit\`).
- Ported `ExcelComAttach`, `ComLateBinding` → `HPBimToolkit/Services/Kata/Excel/` (port ledger).
- Temporary `kata.spike` module, run live, then removed (`ToolkitApplication.cs` back to HEAD).

## Spike result (second Revit 2026.5 on a copy of the user's `Dam kata.rvt`; the user's open Excel `KataDam.xlsm`, read only)
| Hypothesis | Result |
|---|---|
| Kata engine of HPRebar.Core runs in the toolkit | ✅ `KataFormat.Section(300,600)`=`300x600`, `Elevation(3300)`=`+3.300`, `ParseBarList("3d20+2d18")` → 2 bars, `KataColumnLetters.Letter(0)`=`C` |
| No clash with the rebar add-in's copy | ✅ `HPRebar.Core` 1.0.0.0 in context `HPBIMTOOLKIT` (toolkit folder); the rebar add-in's Kata types live inside `HPRebar.dll` in context `HPREBAR` (ILRepack) — two separate assemblies, no `HPRebar.Core` in Default |
| Excel COM from the toolkit | ✅ attached through ROT on the UI thread (STA): 1 workbook `C:\Users\…\Desktop\Kata\KataDam.xlsm`, 15 sheets incl. `Dam` |

## Found live and fixed: UI-thread deadlock at the first message (toolkit-wide, older than Kata)
- First harness Revit froze ("Not Responding", CPU flat, no dialog). Heap dump (dotnet-dump): UI thread in the first message handler's JIT (`DashboardModule.<Register>b__5_0`) → `AssemblyLoadContext.ResolveUsingLoad` → Revit's `AddInLoader.AddInLoadContext.Load` for **System.Text.Json** (context `HPBIMTOOLKIT`), blocked in `RuntimeAssembly.GetPublicKey` while walking loaded assemblies, on **EPPlus.Interfaces** (another add-in's assembly).
- The earlier fix (load references by name at OnStartup + handle messages outside the COM callback) did not cover the JIT's own binding from IL.
- Fix: `DependencyPreloader` also JIT-compiles (`RuntimeHelpers.PrepareMethod`) every non-generic method of the assemblies in the toolkit folder at OnStartup, while Revit loads add-ins one by one: 7 756 methods of 6 assemblies, 222 references, **816 ms** at startup (logged).
- Verification: start with the fix on `Dam kata.rvt` copy → responding, spike answered; `run-live-verify.ps1 -Runs 3` after the spike was removed → **3 × 22/22 PASS**, Revit responding each run (3 starts is a small sample for a race seen 2 times in ~15 starts).

## Not done in this phase
- Generic instantiations and reflection-only paths can still bind lazily (not compiled ahead) — watched by the start runs.

## Review round (code-reviewer, DONE_WITH_CONCERNS → fixed)
- High: a warm-up exception could drop the add-in at startup → `Preload` is best effort: per-method catch-all with a reason comment, whole run wrapped, logs a Warning, never throws; class made `internal`, empty roots guarded.
- Medium: generics and Nice3point stayed lazy; cost would grow with HPRebar.Core → every TypeRef row of every toolkit-folder assembly is resolved (`Module.ResolveType`, binding without JIT, generics included), Nice3point removed from the host list, JIT only for the toolkit's own 2 assemblies. Re-run: **3 × 22/22 PASS**, startup **417–436 ms** (was 816 ms): 224 references, 1 767 type references of 7 assemblies, 606 methods.
- Medium: ADR-0009 now states the isolation condition (HPRebar keeps Core ILRepacked, no loose `HPRebar.Core.dll`); D8 allows `HPRebar.Core` types in the add-in through the transitive reference.
- Low fixed: comments no longer cite plan artifacts. Low kept: `ComLateBinding.Release` catches only `InvalidComObjectException` (documented in the ledger).
