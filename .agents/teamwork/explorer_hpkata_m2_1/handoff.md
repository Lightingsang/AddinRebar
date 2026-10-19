# Handoff Report — M2 Core Excel Engine Explorer (`explorer_hpkata_m2_1`)

**Target Scope**: Milestone M2, Feature 5 — Bi-Directional Excel Engine in `HPKata.Core`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_1\`  
**Target Solution**: `HPKata/HPKata.slnx` (`HPKata.Core`, `HPKata.Core.Tests`)  
**Parent Conversation ID**: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`  

---

## 1. Observation

### 1.1 Existing Codebase & Environment State
- **Solution & Project Structure**:
  - `HPKata/HPKata.slnx` includes `HPKata.Core` and `HPKata.Core.Tests`.
  - `HPKata/HPKata.Core/HPKata.Core.csproj`:
    ```xml
    <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
    <PackageReference Include="ClosedXML" Version="0.104.2" />
    <PackageReference Include="QRCoder" Version="1.6.0" />
    <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all" />
    ```
  - `HPKata.Core/Excel/` directory does not yet exist.
  - `HPKata.Core/Export/ClosedXmlExporter.cs` (lines 13–166) exports `RebarScheduleReport` into sheet `"Thong_ke"`.
  - `HPKata.Core.Tests` currently has **561 passing tests** (`0 failed, 0 skipped`) under `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj -c Debug --disable-build-servers -m:1`.

- **Library Asset Availability**:
  - Central installation root `C:\HPKata\` is populated with:
    - `Kata.xlsm` (474,860 bytes)
    - `Kata ENG.xlsm` (186,618 bytes)
    - `Iso_QHPlus.xls` (562,176 bytes, OLE2 Compound Document format `\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1`)
    - Distribution mirrors exist at `HPKata/Distribution/Assets/Kata.xlsm` and `Iso_QHPlus.xls`.

- **KataInstallEnvironment**:
  - `HPKata.Core/Configuration/KataInstallEnvironment.cs` provides:
    - `public static IKataInstallEnvironment Current => _currentScope.Value ?? _globalEnvironment ?? _defaultEnvironment;`
    - `public static IDisposable UseRootDirectory(string rootDirectory)` for hermetic test isolation using `AsyncLocal<T>`.

### 1.2 Empirical Structure of `Kata.xlsm` Sheets
Direct extraction via `openpyxl` on `C:\HPKata\Kata.xlsm` revealed 15 worksheets:
`['Vach', 'Cot', 'Dam', 'ThepDamAllMBKC', 'save_info', 'Thong_ke', 'Thong_ke_QS', 'To_hop_thep', 'Kho_thep', 'Kata_QS', 'BEAM DESIGN', 'SLAB DESIGN', 'In_tem_thep', 'Data', 'Gan_tag_block']`.

#### Sheet `Dam` (Beam) Layout:
- **Global / Header Metadata**:
  - Row 1: B1 = Drawing Scale (`"1:50"`), G1 = Top curtailment extension length (`500` mm).
  - Row 2: B2 = Section Cut Scale (`"1:25"`), G2 = Tension anchorage factor (`40` * d).
  - Row 3: B3 = Beam Mark (`"B01"`), G3 = Compression anchorage factor (`30` * d), H3 = Top rebar curtailment ratio (`0.2`).
  - Row 4: B4 = Member Count (`1`), G4 = Skin bar diameter (`12` mm).
  - Row 5: B5 = Beam Depth h (`1100` mm), G5 = Number of skin bar layers (`2`), H5 = Bottom rebar curtailment ratio (`0.25`).
  - Row 6: B6 = Beam Width b (`500` mm), G6 = Stirrup diameter (`10` mm).
  - Row 7: B7 = Slab thickness (`150` mm), G7 = Near support stirrup spacing (`"a150"`), J7 = Cover note (`"a500"`).
  - Row 8: B8 = Axis name, G8 = Midspan stirrup spacing (`"a200"`), I8 = Stirrup branch count (`2`).
  - Row 9: B9 = Axis offset (`-100` mm), G9 = Cantilever stirrup spacing (`150` mm), J9 = Main/stirrup cover (`"50/25"`).
  - Row 10: B10 = Top elevation (text `"+3.300"`).
- **Alternating Columns (Starting at Column C = index 3)**:
  - Row 10: Alternating labels `"Cột"` (Support) and `"Nhịp"` (Span).
  - Row 11: Support width (mm, e.g. `400`, `0` for joints/console tips) / Span clear length (mm, e.g. `10400`, `6500`, `2500`).
  - Row 11 Col B: Continuous Top Rebar (e.g. `"6f25"`).
  - Row 12: Continuous Bottom Rebar (e.g. `"6f25"`).
  - Row 13: Top curtailment rebar layer 1.
  - Row 14: Top curtailment rebar layer 2 (e.g. support 1: `"6f25"`, support 2: `"6f20"`, support 5: `"2f20;2f16"`).
  - Row 15: Top curtailment rebar layer 3.
  - Row 16: Top curtailment rebar layer 4.
  - Row 17: Bottom curtailment rebar layer 2 (e.g. span 1: `"6f25"`, span 2: `"2f20"`, span 4: `"2f20"`).
  - Row 18: Bottom curtailment rebar layer 1.
  - Row 19: Top step-down / elevation delta (e.g. `-50`, `-200`, or `"3f20"`).
  - Row 20: Crossing beam size at column (`"400x500"`, `"400x800"`) / Skin/hanger bar spec (`"0f12"`, `"1f12"`).
  - Row 21: Crossing beam eccentricity / Bottom step-down delta.
  - Row 22: Column grid axis name (`"1"`, `"2"`, `"3"`...) / Span-specific stirrup override (`"a100/200"`).
  - Row 23: Column axis eccentricity from column center.
  - Rows 25–27: Stirrup branch configurations (`"Đai bao"`, `"Đai U"`, `"Đai C"`).

#### Sheet `Cot` (Column) Layout:
- **Global / Header Metadata**:
  - C1 = Transverse beam width (`800` mm).
  - C2 = Slab thickness (`200` mm), F2 = Drawing scale (`"1:30"`).
  - C3 = Column mark (`"C1"`), F3 = Section scale (`"1:20"`).
  - C4 = Member count (`1`), F4 = Cover (`"30/100"` mm), H4 = Outer tie spec (`"d10a100/200"`).
  - C5 = Foundation bottom elevation (`-5.5` m), F5 = Dowel bend factor (`15` * d), H5 = Additional tie spec (`"d8a400"`).
- **Story Table (Starting at Row 10 downwards)**:
  - Col A (1): Story name (`"PILECAPS"`, `"BASEMENT"`, `"FLOOR 1"`, `"FLOOR 2"`...).
  - Col B (2): Story height in meters (`1.5`, `3.0`, `3.5`, `3.9`...).
  - Col C (3): Column width b in mm (`1000`).
  - Col D (4): Column depth h in mm (`800`).
  - Col E (5): Bar count along b Layer 1 (`7`).
  - Col F (6): Bar count along b Layer 2 (`7`).
  - Col G (7): Bar count along h Layer 1 (`7`).
  - Col H (8): Bar count along h Layer 2 (`7`).
  - Col I (9): Longitudinal bar diameter in mm (`25` or composite `"25/20"`).
  - Col J (10): Splice position in mm.
  - Col K (11): Beam depth at column head in mm (`600`, `400`).
  - Col L (12): Outer tie specification (`"d10a100/200"`).
  - Col M–O (13–15): Vertical rectangular tie (ratio e.g. `0.5`, spec `"d10a400"`, count).
  - Col P–R (16–18): Horizontal rectangular tie (ratio e.g. `0.5`, spec `"d10a400"`, count).
  - Col S–T (19–20): Vertical cross tie (spec `"d10a400"`, count `2`).
  - Col U–V (21–22): Horizontal cross tie (spec `"d10a400"`, count `2`).

#### Sheet `Vach` (Shear Wall) Layout:
- **Global / Header Metadata**:
  - C1 = Transverse beam width (`800` mm), F1 = Boundary element width (`675` mm).
  - C2 = Slab thickness (`200` mm), F2 = Drawing scale (`"1:30"`).
  - C3 = Wall mark (`"V1"`), F3 = Section scale (`"1:20"`).
  - C4 = Member count (`1`), F4 = Cover (`"30/100"` mm), H4 = Outer tie spec (`"d10a100/200"`).
  - C5 = Foundation bottom elevation (`-5.5` m), F5 = Dowel bend factor (`15` * d), H5 = Additional tie spec (`"d8a400"`).
- **Story Table (Starting at Row 10 downwards)**:
  - Col A (1): Story name (`"PILECAPS"`, `"BASEMENT"`, `"FLOOR 1"`...).
  - Col B (2): Story height in meters (`1.5`, `3.0`, `3.5`...).
  - Col C (3): Wall length along long side Lw in mm (`3000`).
  - Col D (4): Wall thickness along short side tw in mm (`600`).
  - Col E (5): Boundary zone vertical bar count (`7`).
  - Col F (6): Web zone vertical bar count (`11`).
  - Col G (7): Short side bars Layer 1 (`7`).
  - Col H (8): Short side bars Layer 2 (`7`).
  - Col I (9): Vertical bar diameter in mm (`25` or `"25/20"`).
  - Col J (10): Splice position in mm.
  - Col K (11): Coupling beam depth in mm (`600`).
  - Col L (12): Outer boundary tie spec (`"d10a100/200"`).
  - Col M (13): Boundary cage tie spec (`"d10a100/200"`).
  - Col N–O (14–15): Vertical cross tie spec (`"d10a400"`) & count (`"2/3"`).
  - Col P–Q (16–17): Horizontal cross tie spec (`"d10a400"`) & count (`2`).

#### Sheet `Thong_ke` & `Iso_QHPlus.xls` Layout:
- `Kata.xlsm` Sheet `Thong_ke`:
  - Rows 4+: 19 columns: Component mark (A), Bar mark (B), Shape code (C), L1..L6 (D..I), Notes (J), Diameter (K), Unlapped length (L), Cutting length (M), Member count (N), Bars/member (O), Total bars (P), Total length m (Q), Total weight kg (R), Weight/member kg (S).
  - Col U+: Diameter bin summary table (<=10mm, 10-18mm, >18mm, total kg) and Coupler summary table.
- `Iso_QHPlus.xls` Sheet `Output_QHP`:
  - Rows 5+: Component mark (A), Sub-mark (B), Bar mark (C), Shape & dimensions (D–M), Diameter (N), Bars/member (O), Member count (P), Total bars (Q), Cutting length mm (R), Total length m (S), Unit weight kg/m (T), Total weight kg (U).

### 1.3 Repository Precedents for COM & ClosedXML
- `HPRebar/HPRebar/KataExport/Service/ComLateBinding.cs`:
  - Pure late-binding reflection using `Type.InvokeMember(name, flags, null, target, args)` with `BindingFlags.GetProperty`, `SetProperty`, and `InvokeMethod`.
  - Catches `TargetInvocationException` and re-throws the inner `COMException` via `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()`.
- `HPRebar/HPRebar/KataExport/Service/ExcelComAttach.cs`:
  - P/Invoke `ole32.dll!CLSIDFromProgID("Excel.Application", out clsid)` and `oleaut32.dll!GetActiveObject(ref clsid, IntPtr.Zero, out punk)`.
  - Works transparently across .NET 8 (where `Marshal.GetActiveObject` is not part of .NET Core runtime) and .NET Framework.
- `ClosedXML`:
  - Successfully opens, reads, modifies, and saves `.xlsm` workbooks while preserving VBA macros (`vbaProject.bin`).
  - Binary `.xls` (BIFF8) cannot be opened directly by ClosedXML; for `Iso_QHPlus.xls`, fallback generates an identical `.xlsx` file or uses COM when Excel is running.

---

## 2. Logic Chain

1. **Dual-Mode Abstraction Requirement**:
   - The user requires bi-directional sync with desktop Excel when open (allowing instant visual verification of detailing parameters in `Kata.xlsm` while CAD/Revit dialogs are active), yet demands 100% reliable headless operation in batch runs, unit tests, and CI/CD pipelines without Office installed.
   - Therefore, a common abstraction `IKataExcelSession` must encapsulate cell/range reads, writes, and saves. Two implementations implement `IKataExcelSession`: `KataComExcelSession` (for desktop Excel COM via late-binding reflection) and `KataClosedXmlSession` (for file-based ClosedXML).
   - `KataExcelSessionProvider` acts as the intelligent factory: it attempts ROT probe via `oleaut32!GetActiveObject("Excel.Application")`. If desktop Excel is active and has `Kata.xlsm` loaded (or openable), it returns `KataComExcelSession`. Otherwise, it falls back to `KataClosedXmlSession` on `KataInstallEnvironment.Current.KataWorkbookPath`.

2. **Clean Code & Modularity Enforcement (Rules C2, M3)**:
   - Rule C2 requires file length `<= 250` lines (hard project ceiling `<= 300`).
   - Rule M3 requires method length `<= 50` lines.
   - An all-in-one Excel manager handling 4 distinct sheets (`Dam`, `Cot`, `Vach`, `Thong_ke`) plus COM and ClosedXML would exceed 1,500 lines.
   - Therefore, the design decomposes the engine into 19 dedicated, single-responsibility files:
     - 4 Data Transfer Objects (DTOs) in `HPKata.Core/Excel/Data/`.
     - 2 Abstractions in `HPKata.Core/Excel/Abstractions/`.
     - 4 Session & COM helpers in `HPKata.Core/Excel/Session/`.
     - 8 Specialized Readers & Writers in `HPKata.Core/Excel/ReadersWriters/`.
     - 1 Orchestrator facade `KataExcelAdapter` implementing `IKataExcelAdapter`.
   - Each file is projected to be between 40 and 230 lines (100% compliant with C2).
   - Each reader/writer divides logic into discrete parsing methods: `ReadHeader`, `ReadColumnData`, `WriteMetadata`, `WriteStoryRows`, etc. (100% compliant with M3).

3. **Data Fidelity & Bi-Directional Mapping**:
   - For `Dam`:
     - Reads/writes span clear lengths, support widths, continuous top/bottom bars, 4 layers of top curtailment, 2 layers of bottom curtailment, skin bars, hanger bars, elevation step-downs, and stirrups.
     - Maps directly to/from `BeamRebarLayout`, `BeamSpanModel`, `BeamSupportModel`, and `BeamDetailingViewModel`.
   - For `Cot`:
     - Reads/writes story heights, dimensions (b, h), longitudinal rebar counts (corner + sides), bar diameters, splice locations, outer ties, rectangular ties, and cross ties.
     - Maps directly to/from `ColumnRebarLayout`, `ColumnStorySegment`, `ColumnSectionModel`, and `ColumnDetailingViewModel`.
   - For `Vach`:
     - Reads/writes story heights, wall length, wall thickness, boundary vertical bars, web vertical bars, transverse bars, vertical bar diameters, coupling beam depths, outer ties, boundary ties, and cross ties.
     - Maps directly to/from `ShearWallModel`, `WallStorySegment`, and `WallDetailingViewModel`.
   - For `Thong_ke` & `Iso_QHPlus.xls`:
     - Reads/writes rebar schedule entries, computes standard shape parameters (L1..L6), diameters, counts, cutting lengths, total length, total weight, diameter bin breakdowns, and coupler schedules.
     - Maps directly to/from `RebarScheduleReport`, `RebarItem`, and AutoCAD `toexcel`/`toexcelqh` commands.

4. **Hermetic Test Isolation**:
   - `KataInstallEnvironment.UseRootDirectory(tempDir)` uses `AsyncLocal<IKataInstallEnvironment?>`, providing thread-isolated ambient asset paths.
   - Unit tests create a unique temporary directory, copy `Kata.xlsm` from `Distribution/Assets/Kata.xlsm`, activate the scope token, execute read/write roundtrip tests via `KataClosedXmlSession`, and assert exact value preservation.
   - Because `KataClosedXmlSession` operates completely in managed memory via ClosedXML, tests run hermetically with 0 dependencies on Microsoft Excel or Windows ROT.

---

## 3. Caveats

1. **Binary `.xls` (BIFF8) Limitation in ClosedXML**:
   - ClosedXML does not support legacy binary `.xls` files (such as `Iso_QHPlus.xls`).
   - For `Iso_QHPlus.xls`, when Excel COM is active, `KataQhPlusExporter` opens and populates the native `.xls` file. When running in headless fallback without Excel installed, `KataQhPlusExporter` outputs an OpenXML `.xlsx` file (`_ThongKe_QHPlus.xlsx` or `Iso_QHPlus.xlsx`) containing the identical `Output_QHP` sheet structure and column schema.
2. **Excel Busy State (RPC_E_CALL_REJECTED / RPC_E_SERVERCALL_RETRYLATER)**:
   - When a user is actively editing a cell in desktop Excel or has a modal dialog open, COM calls fail with `0x80010001` or `0x800AC472`.
   - `KataComExcelSession` must catch `COMException` with busy HRESULTs and return descriptive failure messages instructing the user to press Enter/Esc in Excel or automatically offer headless fallback.
3. **Kata.xlsm Formula Preservation**:
   - `Kata.xlsm` contains formulas and VBA macros in summary rows and calculation sheets (`BEAM DESIGN`, `SLAB DESIGN`, `To_hop_thep`).
   - The readers and writers must strictly write to the designated data input ranges (e.g. `B1:B10`, `C11:BZ23` on `Dam`; `C1:C5`, `F1:F5`, `A10:V...` on `Cot`; `A10:Q...` on `Vach`), avoiding cell formula overwrites in peripheral calculation areas.

---

## 4. Conclusion & Proposed Architecture

### 4.1 Component Inventory & File Layout (`HPKata.Core/Excel/`)

All files strictly `<= 250` lines per `.cs` file (Rule C2):

| # | File Path | Responsibilities & Scope | Est. Lines |
|---|---|---|---|
| 1 | `Excel/Abstractions/IKataExcelAdapter.cs` | Main public adapter interface matching `PROJECT.md` | ~35 |
| 2 | `Excel/Abstractions/IKataExcelSession.cs` | Low-level cell/range abstraction over COM & ClosedXML | ~60 |
| 3 | `Excel/Data/KataBeamSheetData.cs` | DTO for sheet `Dam`: metadata, spans, supports, curtailment, stirrups, skin bars | ~120 |
| 4 | `Excel/Data/KataColumnSheetData.cs` | DTO for sheet `Cot`: metadata, story tiers, section dims, bar counts, ties | ~100 |
| 5 | `Excel/Data/KataWallSheetData.cs` | DTO for sheet `Vach`: metadata, story tiers, wall length/thickness, boundary, web mesh | ~100 |
| 6 | `Excel/Data/KataRebarScheduleItem.cs` | DTO for schedule items (shapes, segments L1..L6, counts, weight, couplers) | ~80 |
| 7 | `Excel/Session/ComLateBindingHelper.cs` | P/Invoke `ole32!CLSIDFromProgID`, `oleaut32!GetActiveObject`, `Type.InvokeMember` | ~90 |
| 8 | `Excel/Session/KataExcelSessionProvider.cs` | Session factory: probes desktop Excel COM, falls back to ClosedXML | ~140 |
| 9 | `Excel/Session/KataComExcelSession.cs` | Implements `IKataExcelSession` via late-binding COM reflection | ~180 |
| 10 | `Excel/Session/KataClosedXmlSession.cs` | Implements `IKataExcelSession` via `ClosedXML.Excel.XLWorkbook` | ~170 |
| 11 | `Excel/ReadersWriters/KataBeamSheetReader.cs` | Reads sheet `Dam` from `IKataExcelSession` into `KataBeamSheetData` | ~210 |
| 12 | `Excel/ReadersWriters/KataBeamSheetWriter.cs` | Writes `KataBeamSheetData` into sheet `Dam` of `IKataExcelSession` | ~220 |
| 13 | `Excel/ReadersWriters/KataColumnSheetReader.cs` | Reads sheet `Cot` from `IKataExcelSession` into `KataColumnSheetData` | ~175 |
| 14 | `Excel/ReadersWriters/KataColumnSheetWriter.cs` | Writes `KataColumnSheetData` into sheet `Cot` of `IKataExcelSession` | ~195 |
| 15 | `Excel/ReadersWriters/KataWallSheetReader.cs` | Reads sheet `Vach` from `IKataExcelSession` into `KataWallSheetData` | ~175 |
| 16 | `Excel/ReadersWriters/KataWallSheetWriter.cs` | Writes `KataWallSheetData` into sheet `Vach` of `IKataExcelSession` | ~195 |
| 17 | `Excel/ReadersWriters/KataScheduleSheetWriter.cs` | Writes schedule & summary bins into sheet `Thong_ke` | ~160 |
| 18 | `Excel/ReadersWriters/KataQhPlusExporter.cs` | Exports schedule into `Iso_QHPlus.xls` (COM) or `.xlsx` (ClosedXML) | ~180 |
| 19 | `Excel/KataExcelAdapter.cs` | Facade implementing `IKataExcelAdapter` coordinating sessions & readers/writers | ~150 |

---

### 4.2 Detailed Class Contracts & Method Signatures

#### A. Abstractions
```csharp
// HPKata.Core/Excel/Abstractions/IKataExcelAdapter.cs
namespace HPKata.Core.Excel
{
    public interface IKataExcelAdapter
    {
        bool IsDesktopExcelOpen { get; }
        bool ReadBeamSheet(string? workbookPath, out KataBeamSheetData data);
        bool WriteBeamSheet(string? workbookPath, in KataBeamSheetData data);
        bool ReadColumnSheet(string? workbookPath, out KataColumnSheetData data);
        bool WriteColumnSheet(string? workbookPath, in KataColumnSheetData data);
        bool ReadWallSheet(string? workbookPath, out KataWallSheetData data);
        bool WriteWallSheet(string? workbookPath, in KataWallSheetData data);
        bool ExportSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items);
        bool ExportQhPlusSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items);
    }
}

// HPKata.Core/Excel/Abstractions/IKataExcelSession.cs
namespace HPKata.Core.Excel.Abstractions
{
    public interface IKataExcelSession : IDisposable
    {
        bool IsComSession { get; }
        string WorkbookPath { get; }
        bool SheetExists(string sheetName);
        void EnsureSheet(string sheetName);
        string? GetCellValue(string sheetName, int row, int col);
        double GetCellDouble(string sheetName, int row, int col, double defaultValue = 0.0);
        int GetCellInt(string sheetName, int row, int col, int defaultValue = 0);
        void SetCellValue(string sheetName, int row, int col, object? value);
        void ClearRange(string sheetName, int startRow, int startCol, int endRow, int endCol);
        void Save();
    }
}
```

#### B. Data Contracts (DTOs)
```csharp
// HPKata.Core/Excel/Data/KataBeamSheetData.cs
namespace HPKata.Core.Excel.Data
{
    public sealed class KataBeamSheetData
    {
        public string BeamMark { get; set; } = "B01";
        public int MemberCount { get; set; } = 1;
        public double DefaultWidthMm { get; set; } = 300.0;
        public double DefaultHeightMm { get; set; } = 600.0;
        public double SlabThicknessMm { get; set; } = 150.0;
        public string DrawingScale { get; set; } = "1:50";
        public string SectionScale { get; set; } = "1:25";
        public double ElevationDeltaMm { get; set; }
        public string ElevationString { get; set; } = "+3.300";
        public string AxisName { get; set; } = string.Empty;
        public double AxisOffsetMm { get; set; }

        public string TopContinuousRebar { get; set; } = "2f20";
        public string BotContinuousRebar { get; set; } = "2f20";
        public int SkinBarDiameterMm { get; set; } = 12;
        public int SkinBarLayers { get; set; } = 1;

        public int StirrupDiameterMm { get; set; } = 8;
        public string StirrupNearSupport { get; set; } = "a100";
        public string StirrupMidSpan { get; set; } = "a200";
        public string StirrupCantilever { get; set; } = "150";

        public List<KataBeamSupportData> Supports { get; } = new();
        public List<KataBeamSpanData> Spans { get; } = new();
    }

    public sealed class KataBeamSupportData
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public double WidthMm { get; set; } = 400.0;
        public string TopCurtailmentLayer1 { get; set; } = string.Empty;
        public string TopCurtailmentLayer2 { get; set; } = string.Empty;
        public string TopCurtailmentLayer3 { get; set; } = string.Empty;
        public string TransverseBeamSize { get; set; } = string.Empty;
        public double TransverseBeamOffsetMm { get; set; }
        public string ColumnGridAxis { get; set; } = string.Empty;
        public double ColumnGridOffsetMm { get; set; }
    }

    public sealed class KataBeamSpanData
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public double ClearLengthMm { get; set; } = 5000.0;
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public string BotCurtailmentLayer1 { get; set; } = string.Empty;
        public string BotCurtailmentLayer2 { get; set; } = string.Empty;
        public string TopStepDownDelta { get; set; } = string.Empty;
        public string SkinBarSpec { get; set; } = string.Empty;
        public string BottomStepDownDelta { get; set; } = string.Empty;
        public string StirrupOverride { get; set; } = string.Empty;
    }
}

// HPKata.Core/Excel/Data/KataColumnSheetData.cs
namespace HPKata.Core.Excel.Data
{
    public sealed class KataColumnSheetData
    {
        public string ColumnMark { get; set; } = "C1";
        public int MemberCount { get; set; } = 1;
        public double TransverseBeamWidthMm { get; set; } = 800.0;
        public double SlabThicknessMm { get; set; } = 200.0;
        public string DrawingScale { get; set; } = "1:30";
        public string SectionScale { get; set; } = "1:20";
        public string CoverSpec { get; set; } = "30/100";
        public string DefaultOuterTie { get; set; } = "d10a100/200";
        public double FoundationBottomElevationM { get; set; } = -5.5;
        public int DowelHookFactor { get; set; } = 15;
        public string DefaultExtraTie { get; set; } = "d8a400";

        public List<KataColumnStoryData> Stories { get; } = new();
    }

    public sealed class KataColumnStoryData
    {
        public string StoryName { get; set; } = string.Empty;
        public double StoryHeightM { get; set; } = 3.3;
        public double WidthBMm { get; set; } = 400.0;
        public double DepthHMm { get; set; } = 400.0;
        public int BarsSideB1 { get; set; } = 2;
        public int BarsSideB2 { get; set; }
        public int BarsSideH1 { get; set; } = 2;
        public int BarsSideH2 { get; set; }
        public string RebarDiameterSpec { get; set; } = "20";
        public double SplicePositionMm { get; set; }
        public double BeamDepthMm { get; set; } = 600.0;
        public string OuterTieSpec { get; set; } = "d10a100/200";
        public double RectTieVerRatio { get; set; }
        public string RectTieVerSpec { get; set; } = string.Empty;
        public int RectTieVerCount { get; set; }
        public double RectTieHorRatio { get; set; }
        public string RectTieHorSpec { get; set; } = string.Empty;
        public int RectTieHorCount { get; set; }
        public string CrossTieVerSpec { get; set; } = string.Empty;
        public int CrossTieVerCount { get; set; }
        public string CrossTieHorSpec { get; set; } = string.Empty;
        public int CrossTieHorCount { get; set; }
    }
}

// HPKata.Core/Excel/Data/KataWallSheetData.cs
namespace HPKata.Core.Excel.Data
{
    public sealed class KataWallSheetData
    {
        public string WallMark { get; set; } = "V1";
        public int MemberCount { get; set; } = 1;
        public double TransverseBeamWidthMm { get; set; } = 800.0;
        public double BoundaryZoneWidthMm { get; set; } = 600.0;
        public double SlabThicknessMm { get; set; } = 200.0;
        public string DrawingScale { get; set; } = "1:30";
        public string SectionScale { get; set; } = "1:20";
        public string CoverSpec { get; set; } = "30/100";
        public string DefaultOuterTie { get; set; } = "d10a100/200";
        public double FoundationBottomElevationM { get; set; } = -5.5;
        public int DowelHookFactor { get; set; } = 15;
        public string DefaultExtraTie { get; set; } = "d8a400";

        public List<KataWallStoryData> Stories { get; } = new();
    }

    public sealed class KataWallStoryData
    {
        public string StoryName { get; set; } = string.Empty;
        public double StoryHeightM { get; set; } = 3.3;
        public double LengthLwMm { get; set; } = 3000.0;
        public double ThicknessTwMm { get; set; } = 300.0;
        public int BoundaryBarsCount { get; set; } = 6;
        public int WebBarsCount { get; set; } = 10;
        public int TransverseBarsLayer1 { get; set; } = 2;
        public int TransverseBarsLayer2 { get; set; } = 2;
        public string VerticalBarDiameterSpec { get; set; } = "20";
        public double SplicePositionMm { get; set; }
        public double CouplingBeamDepthMm { get; set; } = 600.0;
        public string OuterTieSpec { get; set; } = "d10a100/200";
        public string BoundaryTieSpec { get; set; } = "d10a100/200";
        public string CrossTieVerSpec { get; set; } = "d10a400";
        public string CrossTieVerCount { get; set; } = "2";
        public string CrossTieHorSpec { get; set; } = "d10a400";
        public int CrossTieHorCount { get; set; } = 2;
    }
}

// HPKata.Core/Excel/Data/KataRebarScheduleItem.cs
namespace HPKata.Core.Excel.Data
{
    public sealed class KataRebarScheduleItem
    {
        public string ComponentMark { get; set; } = string.Empty;
        public string BarMark { get; set; } = "1";
        public string ShapeCode { get; set; } = "00";
        public double[] SegmentsMm { get; set; } = Array.Empty<double>();
        public string Notes { get; set; } = string.Empty;
        public int DiameterMm { get; set; } = 20;
        public double UnlappedLengthMm { get; set; }
        public double CuttingLengthMm { get; set; }
        public int MemberCount { get; set; } = 1;
        public int CountPerMember { get; set; } = 1;
        public int TotalCount => MemberCount * CountPerMember;
        public double TotalLengthM => (CuttingLengthMm * TotalCount) / 1000.0;
        public double UnitMassKgPerM { get; set; }
        public double TotalWeightKg => TotalLengthM * UnitMassKgPerM;
        public double WeightPerMemberKg => MemberCount > 0 ? TotalWeightKg / MemberCount : 0.0;
        public bool IsCoupler { get; set; }
    }
}
```

---

### 4.3 Clean Code Metrics Verification Plan

| Metric / Rule | Project Standard | Engine Enforcement Strategy |
|---|---|---|
| File Length (Rule C2) | Max 250 lines (hard ceiling <= 300) | 19 distinct files separated by sheet domain, session, and DTOs. |
| Method Length (Rule M3) | Max 50 lines | Split into dedicated sub-methods (e.g. `WriteMetadata`, `WriteStoryRow`, `ParseSupportColumn`). |
| Compiler Warnings | 0 warnings (`TreatWarningsAsErrors=true`) | Full nullability annotations (`#nullable enable`), no obsolete APIs. |
| Commented-out Code (Q-B1) | Zero occurrences | Enforced at build and review. |
| Empty Catch Blocks (Q-B2) | Forbidden without justification | Every catch block logs reason or rethrows via `ExceptionDispatchInfo`. |
| Dependency Direction | Neutral C# / ClosedXML | Zero references to AutoCAD, Civil 3D, or Revit API in `HPKata.Core`. |

---

## 5. Verification Method

### 5.1 Independent Reproduction Steps

1. **Verify Baseline Builds & Tests**:
   ```powershell
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj -c Debug --disable-build-servers -m:1
   ```
   *Expected outcome*: 0 errors, 0 warnings, 561 tests pass.

2. **Verify Sheet Reading on Master `Kata.xlsm`**:
   - Execute test reading sheet `Dam` from `Distribution/Assets/Kata.xlsm`.
   - Verify `beamData.BeamMark == "B01"`, `beamData.DefaultWidthMm == 500.0`, `beamData.DefaultHeightMm == 1100.0`, `beamData.Spans.Count >= 6`.
   - Execute test reading sheet `Cot`.
   - Verify `colData.ColumnMark == "C1"`, `colData.Stories.Count >= 6` with `"BASEMENT"`, `"FLOOR 1"` through `"FLOOR 5"`.
   - Execute test reading sheet `Vach`.
   - Verify `wallData.WallMark == "V1"`, `wallData.BoundaryZoneWidthMm == 675.0`, `wallData.Stories.Count >= 6`.

3. **Verify Hermetic Isolated Roundtrip in `HPKata.Core.Tests`**:
   - In a test method, create a temporary directory:
     ```csharp
     string tempDir = Path.Combine(Path.GetTempPath(), $"HPKata_Test_{Guid.NewGuid():N}");
     Directory.CreateDirectory(tempDir);
     File.Copy(@"HPKata\Distribution\Assets\Kata.xlsm", Path.Combine(tempDir, "Kata.xlsm"));
     File.Copy(@"HPKata\Distribution\Assets\Iso_QHPlus.xls", Path.Combine(tempDir, "Iso_QHPlus.xls"));
     using (KataInstallEnvironment.UseRootDirectory(tempDir))
     {
         var adapter = new KataExcelAdapter();
         // 1. Read Dam
         Assert.True(adapter.ReadBeamSheet(null, out var beam));
         Assert.Equal("B01", beam.BeamMark);
         // 2. Modify Dam
         beam.BeamMark = "D_TEST_99";
         beam.DefaultWidthMm = 350.0;
         Assert.True(adapter.WriteBeamSheet(null, beam));
         // 3. Re-read Dam and assert fidelity
         Assert.True(adapter.ReadBeamSheet(null, out var reBeam));
         Assert.Equal("D_TEST_99", reBeam.BeamMark);
         Assert.Equal(350.0, reBeam.DefaultWidthMm);
     }
     Directory.Delete(tempDir, recursive: true);
     ```
   *Expected outcome*: 100% roundtrip test pass in memory via ClosedXML without requiring Excel desktop installation.

4. **Verify File Length Limits Across All New Files**:
   ```powershell
   Get-ChildItem -Path "HPKata/HPKata.Core/Excel" -Filter "*.cs" -Recurse | ForEach-Object {
       $lines = (Get-Content $_.FullName).Count
       Write-Host "$($_.Name): $lines lines"
       if ($lines -gt 250) { Write-Error "$($_.Name) exceeds 250 lines!" }
   }
   ```
   *Expected outcome*: Every file <= 250 lines.

---

### Invalidation Conditions
- Any new `.cs` file in `HPKata.Core/Excel/` exceeds 250 lines.
- Any method in `HPKata.Core/Excel/` exceeds 50 lines.
- Code introduces dependencies on `Autodesk.Revit.*` or `Autodesk.AutoCAD.*` inside `HPKata.Core`.
- Unit tests fail to execute hermetically without Microsoft Excel installed.
