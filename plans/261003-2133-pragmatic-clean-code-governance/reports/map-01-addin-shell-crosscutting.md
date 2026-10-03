# Map 01 — HPRebar add-in shell + cross-cutting code

Scope: HPRebar/HPRebar shell (Application, Commands, Resources, csproj, .addin), install/, cross-feature grep stats.
Mode: read-only scan 2026-10-03, no build run. Paths below relative to HPRebar/HPRebar/ unless prefixed.
Line counts = `wc -l`; method lengths = brace-counting heuristic (±3 lines).
HPRebar/build/ was **not read**: the repo's scout-block hook (.claude/.ckignore pattern `build`) blocks it; build facts in §10 come from CLAUDE.md, GIẢ ĐỊNH CHƯA XÁC MINH.

## 1. Entry flow

1. HPRebar.addin:2-13 — `Type=Application`, `HPRebar\HPRebar.dll`, AddInId `AB6B2397-…`, `HPRebar.Application`, VendorId `Development`, `UseRevitContext=False` + `ContextName=HPRebar` (isolated load context, `EnableDynamicLoading` in csproj).
2. Application.cs:28 `OnStartup` → `CreateLogger()` (:95) → `CreateRibbon()` (:57) inside try; failure = `Log.Fatal` + `CloseAndFlush` + rethrow (:36-45).
3. Logging (:95-119): static Serilog `Log.Logger`, Debug sink + rolling file `%LocalAppData%\HPRebar\logs\hprebar-.log`, 7 files, min Debug; `AppDomain.CurrentDomain.UnhandledException` → `Log.Fatal` (:114). Serilog is ILRepack-merged, so `Log` is private to HPRebar.dll.
4. Ribbon (:57-79): Nice3point `Application.CreatePanel("Rebar","HPRebar")` + 5 `AddPushButton<TCommand>` (Column, Beam, Foundation, KataExport, KataRebar) (:63-68). `#if KATA_ONLY` (csproj `-p:KataOnly=true`, HPRebar.csproj:13-15) → panel "Kata" with KataExport only (:59-61). No tooltip / LongDescription / AvailabilityClassName on any button.
5. Feature registration = hard-coded list in `CreateRibbon`; each `Track(button, icons => icons.X)` (:81) feeds `_buttons` (:23) for icon repaint. No registry/discovery.
6. Icons: `ApplyIcons` (:84-93) builds `new RibbonIcons(RibbonIcons.RevitIsDark())`, one 32×32 frozen `DrawingImage` for both `Image` and `LargeImage`.
7. Theme wiring: R2024+ `ThemeChanged` lambda (:76) = `ApplyIcons()` + `RevitHostTheme.Instance.NotifyChanged()`; unsubscribed in `OnShutdown` (:52). Each window code-behind calls `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).X)` — 6 sites: BeamRebarView.xaml.cs:18, ColumnRebarView.xaml.cs:15, FoundationRebarView.xaml.cs:15, KataExportView.xaml.cs:18, KataSettingsView.xaml.cs:18, KataRebarView.xaml.cs:21. Resources/Themes/Theme.xaml:8-16 merges MaterialBridge → ThemeDark → Typography → Spacing → Controls → ThemeIcons.
8. Commands: Nice3point `ExternalCommand`, `[Transaction(Manual)]`, modeless window (`view.Show()`), owner via `WindowInteropHelper(view).Owner = Application.MainWindowHandle` (5 sites, e.g. ColumnRebarCommand.cs:102). Feature roots all hold exactly the 4 mandated files.
9. Shared linking: HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj:47-52 links RibbonIcons.cs, IHostTheme.cs, RevitHostTheme.cs, MaterialThemeBridge.cs, ThemeInfo.cs; HPRebar.McpBridge/Application.cs:92 repeats the same ThemeChanged lambda shape (own `RevitHostTheme` singleton per assembly).

## 2. Composition roots

- No DI container — verified: 0 hits for `IServiceCollection|IServiceProvider|AddSingleton|AddTransient|Host.CreateDefault`, 0 `ILogger` in HPRebar/HPRebar. `Application` constructs no services.
- Real composition root = each `XxxCommand.Execute`:
  - ColumnRebarCommand.cs:86-100: `RebarShapeResolver.Load`, `AnnotationSettings.Load`, `new ColumnRebarOrchestrator`, `new ColumnRebarExternalEventHandler`, `new ColumnRebarSession`, `new ColumnRebarViewModel(session, new LocalizationService(), handler)`, `new ColumnRebarView`.
  - BeamRebarCommand.cs:86-92 (same shape), FoundationRebarCommand.cs:87-88, KataExportCommand.cs:86-88, KataRebarCommand.cs:53-55.
- Secondary roots inside ViewModels: KataRebarViewModel.cs:58 `new KataRebarTypeResolver(document)`; KataExportViewModel.cs:70 optional ctor param `KataRebarTypeResolver? typeResolver = null`.
- Services are mostly static classes: static-class count per feature Column 23, Beam 21, Foundation 7, KataExport 15, KataRebar 17 (+ MaterialThemeBridge). Commands/VMs call them directly (e.g. ColumnStackValidator.Validate, ColumnRebarCommand.cs:65).

## 3. Static / global state inventory

| file:line | member | mutable? | purpose | test impact |
|---|---|---|---|---|
| ColumnRebar/ColumnRebarCommand.cs:27 | `static ColumnRebarView? _window` | yes | single modeless window; 2nd click `Activate()` | process-wide; reset only by `Closed` handler |
| BeamRebar/BeamRebarCommand.cs:28 | `_window` | yes | same | same |
| FoundationRebar/FoundationRebarCommand.cs:25 | `_window` | yes | same | same |
| KataExport/KataExportCommand.cs:27 | `_window` | yes | same | same |
| KataRebar/KataRebarCommand.cs:24 | `_window` | yes | same | same |
| ColumnRebar/View/Controls/DrawPrimitives.cs:110 | `static double PixelsPerDip {get;set;}` | yes | DPI for FormattedText; written in OnRender by ColumnElevationCanvas.cs:74, ColumnSectionCanvas.cs:85, DistributionDiagram.cs:52 | shared by every open canvas/window; last renderer wins |
| BeamRebar/View/Controls/BeamDrawPrimitives.cs:26 | `PixelsPerDip` | yes | same; written BeamElevationCanvas.cs:85, BeamSectionCanvas.cs:96 | same (KataDrawPrimitives.cs:43 already uses an instance field) |
| KataRebar/Service/KataSettingsStore.cs:19 | `static KataSettings? _cached` (+ path :16) | yes | per-user `%AppData%\HPRebar\KataSettings.json` cache | no reset API, real file I/O; read from VMs (KataRebarViewModel.cs:176, KataExportViewModel.Rebar.cs:80/183/202, KataSettingsViewModel.cs:93) |
| Resources/Themes/RevitHostTheme.cs:9 | `Instance` singleton + `event Changed` (:29) | yes (subscribers) | window theme source | Revit-only (`UIThemeManager`); one copy per linking assembly |
| Application.cs:104 | Serilog `Log.Logger` | set once | logging sink for 99 `Log.` call sites (features 94: Column 24, Beam 26, Foundation 9, KataExport 12, KataRebar 23; shell 5) | static sink, not injectable |
| Application.cs:114 | AppDomain `UnhandledException` handler | added, never removed | log crashes | fires for every exception in Revit's domain |
| ColumnRebar/Model/UiStringsCatalog.cs:6-8, BeamRebar/Model/UiStringsCatalog.cs:8-10 | `English`/`Vietnamese` | no (init-only) | UI strings | none |
| */Model/ValidationResult.cs, ValidationMessages.cs, RebarShapeResolver name arrays, RibbonIcons brushes, MaterialThemeBridge URIs, DependencyProperties | static readonly | no | constants | none |

## 4. Revit API boundary

- HPRebar.Core: **0** `Autodesk` references (only a doc comment, HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs:10); csproj netstandard2.0, only Polyfill. Clean.
- HPRebar/HPRebar: SDK global usings include `Autodesk.Revit.DB` for **every** file (obj/Debug.R26/HPRebar.GlobalUsings.g.cs), so `using Autodesk` greps undercount.
- Revit types in ViewModel layer: KataRebarViewModel.cs:55 ctor takes `Document`; IKataExportRunner.cs / IKataRebarRunner.cs use Autodesk types; ColumnRebar/ViewModel/Tabs/GeometryTabViewModel.cs:21 reads `Faces[0].Element.Name` (Revit `Element` via session). 17 Model/ files `using Autodesk`.
- Projects referencing Revit: HPRebar, HPRebar.McpBridge, HPRebar.Tests (TUnit). Not: Core, Core.Tests, Mcp.Server(.Tests via ref assemblies only for seed compile).

## 5. Multi-version conditional compilation

16 `#if` sites in HPRebar/HPRebar (13 `REVIT*`, 2 `NETCOREAPP`, 1 `KATA_ONLY`); 17 `// Multi-version` tags.

| file | #if | constant |
|---|---|---|
| Application.cs | 4 | REVIT2024_OR_GREATER ×3, KATA_ONLY |
| Resources/Themes/MaterialThemeBridge.cs | 2 | NETCOREAPP |
| KataRebar/Service/KataRebarCurveFactory.cs | 2 | REVIT2026_OR_GREATER (:21, :47) |
| Resources/Themes/RevitHostTheme.cs, Resources/Icons/RibbonIcons.cs | 1 each | REVIT2024_OR_GREATER |
| FoundationRebar SelectionFilter/Validator/CreationService | 1 each | REVIT2024_OR_GREATER |
| KataRebar/Service/KataStirrupCoverFit.cs, KataRebarCreationService.cs | 1 each | REVIT2025_OR_GREATER |
| ColumnRebar/Service/MainBarCreator.cs:32 | 1 | REVIT2026_OR_GREATER |

R27-removed API still ungated: `RebarHookOrientation` Beam 9 / Foundation 3 (KataRebar's 3 are gated), `Curve.Intersect(…, out)` Beam 1, `#pragma warning disable CS0618` ×5 (Beam 4, Foundation 1). BeamRebar has 0 `#if`.

## 6. Interfaces

| interface | file:line | impls | test seam used? |
|---|---|---|---|
| IColumnRebarRunner | ColumnRebar/ViewModel/ColumnRebarViewModel.cs:19 (inside VM file) | 1 — ColumnRebarExternalEventHandler.cs:19 | no |
| IBeamRebarRunner | BeamRebar/ViewModel/IBeamRebarRunner.cs:10 | 1 — BeamRebarExternalEventHandler.cs:18 | no |
| IFoundationRebarRunner | FoundationRebar/ViewModel/IFoundationRebarRunner.cs:10 | 1 | no |
| IKataExportRunner | KataExport/ViewModel/IKataExportRunner.cs:13 | 1 | no |
| IKataRebarRunner | KataRebar/ViewModel/IKataRebarRunner.cs:10 | 1 | no |
| IHostTheme | Resources/Themes/IHostTheme.cs:7 | 1 (RevitHostTheme) | no |
| IKataDamCellAccessor | HPRebar.Core/KataRebar/Parsers/IKataDamCellAccessor.cs:8 | 1 — KataCellTable (Core) | not named in Core.Tests |

No ViewModel tests exist: VMs live in HPRebar (Revit-referencing, net48/net8-windows); Core.Tests references only Core; HPRebar.Tests covers ColumnRebar services only. Runner seams are unused by tests.

## 7. Helper / Manager / Utils

None in HPRebar/HPRebar by name. Only `KataDamCellAccessorExtensions` (HPRebar.Core/KataRebar/Parsers/IKataDamCellAccessor.cs:23, file 94 LOC). The de-facto helper idiom is the ~83 static service classes (§2) and per-feature `RevitDialogs` / `DrawPrimitives` / `CanvasPalette`.

## 8. Size metrics

Shell LOC: Application.cs 121 · Commands/StartupCommand.cs 16 · Resources/Icons/RibbonIcons.cs 115 · Resources/Themes/MaterialThemeBridge.cs 109 · RevitHostTheme.cs 32 · IHostTheme.cs 13 · ThemeInfo.cs 6. XAML: ThemeIcons 190, MaterialBridge 82, ThemeDark 79, ThemeLight 75, Typography 56, Controls 44, Spacing 38, Theme 19. No shell file > 300.
Feature roots: Commands 74–122, Handlers 92–167 (KataExport 167), Requests 26–41, Filters 15–28.
Feature totals (cs files/LOC + xaml LOC): Column 71/6040 + 852 · Beam 61/5890 + 874 · Foundation 20/1198 + 417 · KataExport 50/5284 + 976 · KataRebar 33/2304 + 335. Project cs total 21 127.

Longest shell/root methods: ColumnRebarCommand.Execute ~93 (:29) · FoundationRebarCommand.Execute ~86 (:27) · BeamRebarCommand.Execute ~84 (:30) · KataExportCommand.Execute ~78 (:29) · KataExportExternalEventHandler.Execute ~73 (:76) · KataRebarCommand.Execute ~48 · RibbonIcons ctor ~44 (:24-67) · MaterialThemeBridge.Apply ~35 (:43) · CreateLogger ~25.
Project-wide: 40 methods > 50 lines; top: KataElevationPainter.PaintMonolithicFrame ~165, BeamSupportFinder.FindSupports ~158, KataElevationAnnotations.PaintTopChain ~133, BeamSectionPainter.Paint ~132, FoundationSolidFaceReader.Read ~117, BeamStackReader.Read ~115. Files > 300: BeamRebarSession 536, BeamSupportFinder 443, BeamElevationPainter 442, KataElevationAnnotations 389, ColumnSpecEditor 320.

## 9. Smells observed (facts only)

1. [dead code] Commands/StartupCommand.cs:11 — empty `Execute`, no button, no reference anywhere.
2. [dead code / comment noise] Resources/Icons/RibbonIcons.cs:47-48,85 — `Execute` glyph "the template command the button still points at"; no `icons.Execute` usage in HPRebar or McpBridge.
3. [dead code] HPRebar.csproj:40 `ClosedXML 0.104.2` ("Excel OpenXML Reader") — no `ClosedXML`/`XLWorkbook` code use (only a doc comment, HPRebar.Core/KataRebar/Parsers/KataCellTable.cs:9); ILRepack merges it into HPRebar.dll.
4. [duplication] RibbonIcons.RevitIsDark() (RibbonIcons.cs:91-99) vs RevitHostTheme.IsDark (RevitHostTheme.cs:15-27): same query, opposite pre-2024 answers (false vs true) and contradicting comments.
5. [naming] `HPRebar.Application` exposes property `Application` (UIControlledApplication, Application.cs:60,77); in commands `Application` = UIApplication — needs an explanatory comment (ColumnRebarCommand.cs:31).
6. [hidden dependency / static state] Application.cs:114-118 — domain-wide UnhandledException subscription, never removed, logs any Revit-process crash as HPRebar Fatal.
7. [SRP / coupling] Application.cs:76 — one ThemeChanged lambda repaints ribbon and drives window theme singleton.
8. [duplication] Application.cs:61 vs :67 — KataExport button registered in both KATA_ONLY and normal branch.
9. [duplication] shell skeleton ×5: static `_window` guard → pick/validate → new handler/VM/View → owner → `Closed{Dispose; _window=null}` (ColumnRebarCommand.cs:27-114, KataRebarCommand.cs:24-66, …). Handlers ×5 near-identical (Column vs Beam differ by 25 lines after name swap): ConcurrentQueue + `ExternalEvent.Create(this)` + dequeue loop + TaskDialog summary (BeamRebarExternalEventHandler.cs:18-95).
10. [duplication] cross-feature same-named classes: RevitDialogs ×4 (Foundation ≡ KataExport except namespace/comment), LocalizationService ×2 (≡ modulo namespace), ValidationResult/ValidationMessages ×2, UiStrings/UiStringsCatalog ×2, RebarShapeResolver ×2 (+ KataRebarShapeResolver), RebarTypeCatalog ×2, DetailViewCreator ×2, DimensionCreator ×2, CanvasPalette ×2 (+ KataCanvasPalette), DrawPrimitives ×3, `RebarFailureHandling.SwallowWarnings` ×3 (+ KataTransactionRunner.FailureCollector).
11. [coupling] feature folders not independent: KataExport → KataRebar (6 files), KataRebar → KataExport (5 files) = cycle; KataRebar → BeamRebar (5 files; KataRebarCommand.cs:6 uses BeamRebar.Service.RevitDialogs). KATA_ONLY hides buttons only — every feature still compiles.
12. [Revit-boundary leak] Revit `Document`/`Element` reach ViewModels (KataRebarViewModel.cs:55-58, GeometryTabViewModel.cs:21) and runner interfaces (IKataExportRunner, IKataRebarRunner).
13. [hidden dependency] ViewModels call static I/O directly: KataExcelWriter.Probe/Write (KataExportViewModel.cs:179,202), KataDamComReader.Read COM (KataExportViewModel.Rebar.cs:50, KataRebarViewModel.cs:111), KataSettingsStore.Load/Save (§3).
14. [static state] PixelsPerDip global setters written from OnRender (§3).
15. [hidden dependency] ColumnRebar/View/Controls/ResourceKeyToControlTemplateConverter.cs:15 resolves via `System.Windows.Application.Current`, while MaterialThemeBridge.cs:17 states an add-in has no Application.Current resources; used at ColumnRebarView.xaml:73. Runtime result GIẢ ĐỊNH CHƯA XÁC MINH.
16. [inconsistency] Column/Beam/Foundation commands read `ActiveUIDocument.Document` with no null check (ColumnRebarCommand.cs:32-33); KataExport/KataRebar check (KataRebarCommand.cs:29). `_window` check comes after reading the document in all five.
17. [naming] IColumnRebarRunner declared inside ColumnRebarViewModel.cs:19; the other four runners have own files.
18. [rule drift] development-rules.md mandates `doc.NewTransaction(...)`; code has 0 `NewTransaction`, 19 `new Transaction(`, 4 `new TransactionGroup(`, 3 `new SubTransaction(`; transactions live in Orchestrators/CreationServices (e.g. BeamRebar/Service/BeamRebarOrchestrator.cs:59, ColumnRebar/Service/ColumnRebarOrchestrator.cs:58), KataRebar via KataTransactionRunner.cs:29. KataExport: no transaction.
19. [naming / i18n] Vietnamese log/exception text in KataRebar (KataTransactionRunner.cs:51, KataRebarCommand.cs:70-71, KataRebarCurveFactory.cs:29) vs English elsewhere.
20. [duplication / multi-version] BeamRebar/FoundationRebar ungated R27-removed APIs (§5) while KataRebar/Service/KataRebarCurveFactory.cs:21-29,47 already has a version-gated `CreateFromCurves` wrapper.
21. [lifecycle] handlers' `Dispose` only disposes the ExternalEvent (BeamRebarExternalEventHandler.cs:76); requests still in `_pending` keep an unfinished TaskCompletionSource. Runtime effect CHƯA TEST.
22. [error handling] 53 `catch (Exception` in features (Column 9, Beam 12, Foundation 6, KataExport 14, KataRebar 12) + 1 shell; 2 bare `catch`: KataRebar/Service/KataTransactionRunner.cs:41 (rollback + rethrow), KataExport/Service/ComLateBinding.cs:61 (swallows `ReleaseComObject` failure). TaskDialog use: Column 12, Beam 9, Foundation 4, KataExport 4, KataRebar 0 (all through per-feature `RevitDialogs`).
23. [stale docs] CLAUDE.md says "Four features exist" — KataRebar is a fifth (Application.cs:68); its test counts (448 Core.Tests) are behind the current 629 test attributes.

## 10. Build / test infrastructure

- HPRebar/HPRebar.slnx: BuildTypes Debug/Release.R23–R27. Projects: HPRebar, HPRebar.Core (Debug/Release), HPRebar.Core.Tests, HPRebar.Tests (`Build=false`, R25/R26), HPRebar.Mcp.Server + Tests, McpShared Contracts / Server.Core / McpBridge.Core, HPRebar.McpBridge (R25/R26 only), build/Build.csproj + install/Installer.csproj (`Build=false`).
- HPRebar.csproj: `Nice3point.Revit.Sdk/6.2.3`; UseWPF, DeployAddin, LaunchRevit, IsRepackable, EnableDynamicLoading; packages Nice3point.Revit.Toolkit/Extensions/Api.RevitAPI/Api.RevitAPIUI `$(RevitVersion).*`, CommunityToolkit.Mvvm 8.4.0, MaterialDesignThemes 5.3.2, Serilog 4.4.0, Serilog.Sinks.Debug 3.0.0, Serilog.Sinks.File 7.0.0, ILRepack 2.0.46, ClosedXML 0.104.2, JetBrains.Annotations 2026.2.0 (private), Polyfill 11.0.1 (private); ProjectReference HPRebar.Core.
- HPRebar.Core: netstandard2.0, Nullable on, ImplicitUsings off. Folders/LOC: BeamRebar 3236, ColumnRebar 1591, FoundationRebar 1229, KataExport 1145, KataRebar 7433.
- HPRebar.Core.Tests: net8.0, xunit.v3 3.1.0 + runner.visualstudio 3.1.5, refs Core only. 629 `[Fact]/[Theory]` attributes (Beam 105, Column 99, Foundation 51, KataExport 85, KataRebar 286, Themes 3 — ThemeTokenCoverageTests is the only test touching shell XAML).
- HPRebar.Tests: Nice3point SDK + TUnit 1.61.38 + Nice3point.TUnit.Revit, refs HPRebar; 21 `[Test]`, ColumnRebar only; every test `Skip.Unless(ColumnStackFixture.Exists)` and `Fixtures/` holds only README.md (no .rvt).
- HPRebar.Mcp.Server.Tests: 41 test attributes (theories expand).
- install/: WixSharp.Core/Msi.Core 2.14.1, net10.0-windows; Installer.cs 61, Installer.Generator.cs 98, Installer.Versioning.cs 52; product GUID `00715D27-…` (Installer.cs:19), WixUI_FeatureTree.
- build/ (from CLAUDE.md only): ModularPipelines; `ResolveConfigurationsModule` filters `Release.R*`, GitVersion versioning, Sourcy `Solutions.HPRebar`, `dotnet run -- pack` = Clean → CreateBundle → PublishServer → CreateInstaller.

## Unresolved

- HPRebar/build/ content unverified (hook-blocked); allow `!build` in .claude/.ckignore if the governance plan needs it mapped.
