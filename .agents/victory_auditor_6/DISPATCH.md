## 2026-09-27T17:10:00Z

You are an independent Victory Auditor (victory_auditor_6) auditing the completion claim for the Kata Rebar feature in the HPRebar ecosystem.

Your assigned workspace directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\victory_auditor_6\

Authoritative user requirements record:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under section ## 2026-09-27T15:57:37Z)

Repo guideline:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\AGENTS.md

Orchestrator Handoff & Plan:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\handoff.md
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\PROJECT.md

Audit Scope:
1. R1. Kata Dam Sheet Data Parser (HPRebar.Core):
   - Strongly-typed DTO models (KataBeamRebarSpec and sub-models) in HPRebar.Core/KataRebar/Models/.
   - Notation string parser (KataBarNotationParser) supporting single, compound, offset, and stirrup spacing syntax.
   - Dual data source capability (IKataDamCellAccessor & KataCellTable): active Excel via COM (oleaut32 batch Value2 2D array) and graceful fallback to ClosedXML reading .xlsm from disk.
   - Strict architectural purity: 100% netstandard2.0 with ZERO references to Autodesk.Revit.*.
2. R2. Rebar Geometry & Distribution Calculator (HPRebar.Core):
   - Explicit 3D rebar curves (Polyline3) and layout packaging in KataRebarCalculator.cs.
   - Continuous main bars with 90° anchorage hooks and cantilever overhang handling.
   - Multi-layer support top additional bars with vertical offsets and L/3, L/4, or sheet cutoff ratios.
   - Mid-span bottom additional bars with L/7 clear span cutoffs.
   - Side bars for deep beams (h >= 700 mm, vertical spacing <= 300 mm).
   - 3-zone stirrup distribution (L/4 dense, L/2 sparse) supporting closed hoops (□), cap stirrups (U), and cross-ties (C).
   - Enforced Polyline3.Simplify(1.0) protection across all generated polylines.
3. R3. Revit 3D Rebar Generation & Idempotent Update (HPRebar):
   - KataBeamMatcher: Matches selected Revit beams with Excel spec by name and span count, strictly enforcing horizontal orientation (|Z| <= 10^-3) and elevation consistency (<= 25 mm).
   - KataRebarTypeResolver: Resolves bar designations to project RebarBarType by numerical diameter (+/-0.5mm, prioritizing Deformed for bars >= 12mm) and standard RebarHookType.
   - KataRebarCleanupService: Idempotently locates and deletes prior rebars tagged Comments = "HPRebar_Kata_{BeamName}" without over-deleting other beam runs.
   - KataRebarCreationService: Generates native 3D Rebar instances using Rebar.CreateFromCurves and Rebar.CreateFromRebarShape, stamping Comments and Partition, enforcing a curve floor of 2.0e-3 ft (~0.6 mm).
   - KataRebarOrchestrator: Atomic TransactionGroup("Kata Rebar - {BeamName}") with group.Assimilate() and rollback on error.
4. R4. User Interface & Ribbon Integration (HPRebar):
   - Ribbon button "Kata Rebar" on "Rebar" panel adjacent to "Kata Export" with dedicated vector glyph in RibbonIcons.cs.
   - Modeless WPF dialog (KataRebarView.xaml) with MaterialDesign 5.3.2 tokens and MaterialThemeBridge dynamic styling matching Revit Dark/Light themes.
   - Detailing preview, span geometry, bar type mapping overrides, and thread-safe async transaction execution via KataRebarExternalEventHandler.cs.
5. Independent Test Execution:
   - Run: dotnet test HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj
   - Run: dotnet test HPRebar/HPRebar.Mcp.Server.Tests/HPRebar.Mcp.Server.Tests.csproj
   - Run: dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
6. Forensic Cheating Detection:
   - Scan for dummy stubs, hardcoded returns, trivial assertions (Assert.True(true)), or facade implementations.

Conduct your 3-phase audit (timeline & traceability, cheating & forensic detection, independent test execution). Deliver your verdict: VICTORY CONFIRMED or VICTORY REJECTED with full evidence chain in report.md and handoff.md, and send your verdict to the Sentinel (caller).
