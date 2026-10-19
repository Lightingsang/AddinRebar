# Handoff Report: Milestone M3 AutoCAD Jigs, Commands & XData Exploration

- **Agent**: `explorer_m3_gen2_1`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_gen2_1\`
- **Parent Conversation ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`
- **Target Deliverable**: `HPDcePro` Milestone M3 (`HPDcePro.AutoCad`, `HPDcePro.Core.XData`)
- **Status**: Hard Handoff (Investigation & Blueprint Complete)

---

## 1. Observation

### 1.1 Existing AutoCAD Codebase State (`HPDcePro.AutoCad`)
Direct inspection of `HPDcePro/src/HPDcePro.AutoCad/` revealed:
- `Commands/BeamCommands.cs:24-32`: `VeDam_DCE()` invokes hardcoded `CalculateSampleBeam("D1", 2, 6000.0, 220.0, 400.0)` and renders to model space without user interaction.
- `Commands/BeamCommands.cs:67-82`: `VeBlockCaoDoMB`, `VeBlockTrucDam`, `VeBlockChieuDaySan` insert blocks at fixed coordinate `Point2D(0, 0)` without prompting for insertion point.
- `Commands/BeamCommands.Editing.cs:14-22, 64-74`: `BlockBeam_Size`, `BlockBeam_Width` render static dummy text without selecting existing AutoCAD beam entities (`Editor.GetEntity` / `Editor.GetSelection`).
- `Commands/ColumnCommands.cs:52-74`: `ColumnREC1`, `ColumnREC2`, `ColumnCi` invoke `GenerateSampleColumnBuffer(...)` with hardcoded dimensions (400x400, 450x450).
- `Commands/FootingCommands.cs:19-33, 86-99`: `VeBlockCoc` places circle at `(0, 0)`; `VeMongCoc_DCE` runs hardcoded `PileCapInput("DC-1", 1800, 1800, ...)` without user input.
- `Commands/SlabCommands.cs:19-40, 80-120`: `VeThepSanTopX`, `VeThepSanBotX` render hardcoded spans (`l0 = 4000.0`) at `(0, 0)`.
- `Commands/RebarCommands.cs:24-39, 41-56, 115-126`: `TKTSSOLUONGCAUKIEN`, `QuetThongKe_dce` aggregate hardcoded in-memory `sampleItems` instead of scanning drawing entities.
- `HPDcePro.AutoCad/Jigs/`: Directory does **not exist** yet. No `DrawJig` or `EntityJig` classes are currently implemented.

### 1.2 Decompiled Legacy Jigs Binary Inspection (`C:\Dce Pro\DCE Pro.dll`)
Static decompilation using `ilspycmd@11.1.0.9782` on `C:\Dce Pro\DCE Pro.dll` across TypeDefs `0x02000E52`–`0x02000E5C` confirmed all 11 legacy Jigs inherit `Autodesk.AutoCAD.EditorInput.DrawJig`:

```csharp
// 1. VeMBKC_JIG (TypeDef: 0x02000E55, Base: DrawJig)
public class VeMBKC_JIG : DrawJig {
    public Point3d Pt1, Pt2, Diem1, Diem2, Diem3, Diem4, DiemText;
    public double CoChuMD; public int MauChu;
    public bool StartJig(Point3d Diem1, string NoiDung, double CanhB, string TopMidBot, double TyLe, bool raiBlockX, double TDY, bool RaiBlocky, double TDX, bool khongclick = false, Point3d diemclick2 = default(Point3d));
}

// 2. JigSlabRebar (TypeDef: 0x02000E54, Base: DrawJig)
public class JigSlabRebar : DrawJig {
    public Point3d Pt1, Pt2, PointL1, PointL2, PointL3, PointL4, PointL5, Diemdauthep, Diemcuoithep;
    public Point2dCollection Point2dCollection; public DoubleCollection SetBulgeAt; public DBObjectCollection TongHopDim;
    public int DuongKinh;
    public bool StartJig(Point3d Diem1, int DKT, string LoaiThepTrai, double OffsetThepT, double LMocTrai, double OfleaderT, string LoaiThepPhai, double OffsetThepP, double LMocPhai, double OfleaderP, string LoaiTheHien, Point3d Point4, Point3d Point5, bool DimDinhVi);
}

// 3. TagLeader (TypeDef: 0x02000E52, Base: DrawJig)
public class TagLeader : DrawJig {
    public Point3d Pt1, Pt2, Pt3; public bool mirror;
    public bool StartJig(object Polyline, string NoiDung, string NoiDung2, int Tyle, double DoanLuiTag, string Loaitag, Point3d Diem1, Point3d Diem2, string LoaiBlockTag);
}

// 4. TagChiaThep (TypeDef: 0x02000E53, Base: DrawJig)
public class TagChiaThep : DrawJig {
    public Point3d Pt1, Pt2, Pt3; public int KhoangCachChuan; public bool mirror;
    public bool StartJig(Polyline Polyline, int Tyle, Point3d Diem1, int LamTron, string NoiDungThongBaoThem, int kccodinh = 0);
}

// 5. ijgRaithepchamtron (TypeDef: 0x02000E56, Base: DrawJig)
public class ijgRaithepchamtron : DrawJig {
    public Point3dCollection TapHopDiem;
    public bool StartJig(Point3d Diem1, double ostrai, double osphai, double osvg, double KhoangRai, int dkt, bool BoDiemDau, bool BoDiemCuoi);
}

// 6. ijgVeDaiVong (TypeDef: 0x02000E57, Base: DrawJig)
public class ijgVeDaiVong : DrawJig {
    public Point3dCollection Point3dCollection; public Point2dCollection Point2dCollection; public DoubleCollection SetBulgeAt;
    public bool StartJig(Point3d Diem1, double osdv, double ostd, double MocDai, double dktc, double krtc, bool bddt, bool bdct, bool bddtrai, bool bddPhai, bool CheckBoVeD, bool CheckBoVeTC);
}

// 7. JigVeDaiC (TypeDef: 0x02000E58, Base: DrawJig)
public class JigVeDaiC : DrawJig {
    public Point3d Pt1, Pt2, PointL1, PointL2, PointL3; public Point2dCollection Point2dCollection; public DoubleCollection SetBulgeAt;
    public bool StartJig(Point3d Diem1, int DKT, string LoaiThepTrai, double OffsetThepT, double LMocTrai, string LoaiThepPhai, double OffsetThepP, double LMocPhai);
}

// 8. VePolylineBatKy (TypeDef: 0x02000E59, Base: DrawJig)
public class VePolylineBatKy : DrawJig {
    public Point3d Pt1, Pt2; public Polyline Poly2;
    public bool StartJig(Point3d Diem1, Polyline PolyGoc, ref Point3d diempickdau, ref Point3d diempickhai, ref bool dapickdiemdau, ref bool dapickdiemhai);
}

// 9. ijgVeThepVachCT (TypeDef: 0x02000E5A, Base: DrawJig)
public class ijgVeThepVachCT : DrawJig {
    public Point3dCollection Point3dCollection, Point3dCollectionThepGiua; public Polyline polylineBao;
    public Point3d DiemBien1, DiemBien2, DiemBien3, DiemBien4;
    public bool StartJig(Point3d Diem1, double osdv, double ostd, double MocDaiN, double MocDaiT, double dktchuBien, double dktchuGiua, double krtchuBien, double krtchuGiua, bool bddt, bool bdct, bool bddtrai, bool bddPhai, bool CheckBoVeD, bool CheckBoVeTC, double heSoCanhDai, double HeSoCanhNgan, int QuyDinhVungBien);
}

// 10. JigVeVachHam (TypeDef: 0x02000E5B, Base: DrawJig)
public class JigVeVachHam : DrawJig {
    public DBObjectCollection PolyThep, PoLyBao, PolyHacth, polyBangCanNuoc;
    public Point3d bcnd1, bcnd2, bcnd3; public Point3dCollection Point3dCollection, TongHopDim, TongHopDimTren, TongHopDimDuoi;
    public bool StartJig(Point3d Diem1, int DktDoc, int DktNgang, int KRThepNgang, string KTDamDuoi, string KTDamTren, int NeoThep, bool CoThepCho, int BTBVTrai, int BTBVPhai, int BangCanNuoc, int Tyle);
}

// 11. DimNhanhMBKC (TypeDef: 0x02000E5C, Base: DrawJig)
public class DimNhanhMBKC : DrawJig {
    public DBObjectCollection DBObjectCollection;
    public bool StartJig(int TyLe, Point3d Diem1, Point3d Diem2, bool Dim2Hang, DBObjectCollection TongHopDoiTuong);
}
```

### 1.3 Existing XData Infrastructure State (`HPDcePro.Core.XData`)
- `XDataRegistry.cs` records all 13 application strings:
  1. `RebarXdataDCE` (implemented: `RebarXDataCodec.cs`)
  2. `DceRebarXDataTKT` (implemented: `ScheduleXDataCodec.cs`)
  3. `Links_XDataObject` (implemented: `LinksXDataCodec.cs`)
  4. `Dce_ThepKep` (missing typed DTO & codec)
  5. `XDATA_APP_SODOCAT` (missing typed DTO & codec)
  6. `XDATA_APP_KHOTHEPDU` (missing typed DTO & codec)
  7. `XDATA_MAT_BANG_MONG` (missing typed DTO & codec)
  8. `XDATA_MONG_COC` (missing typed DTO & codec)
  9. `SYS_XData_ThangMoi` (missing typed DTO & codec)
  10. `SYS_XData_App` (missing typed DTO & codec)
  11. `NHAN_XDATA_COC` (missing typed DTO & codec)
  12. `NHAN_XDATA_DAM_CONG` (missing typed DTO & codec)
  13. `NHAN_XDATA_MB` (missing typed DTO & codec)
- `CadDraftingBufferRenderer.cs:100-132` demonstrates attaching `XDataPacket` to AutoCAD entities via `ResultBuffer` and registering `RegAppTableRecord`.

---

## 2. Logic Chain

1. **Interface Contract 1 Compliance**: `PROJECT.md` mandates `DceDrawJigBase : Autodesk.AutoCAD.EditorInput.DrawJig` with abstract `PromptStatus StartJig(Editor editor)`. All 11 Jigs must inherit from `DceDrawJigBase`.
2. **Headless Testability & Zero GUI Deadlocks**: Direct calls to `ed.Drag(jig)` in automated tests freeze execution waiting for mouse movement. Therefore, every Jig must decouple its pure geometry engine (calculating points, vectors, offsets, and vertices) into static/pure methods (`Calculate*`). The `DrawJig` overrides (`Sampler` and `WorldDraw`) then consume these calculation methods. This enables 100% test coverage in headless runners (`HPDcePro.Adapters.Tests.exe`).
3. **Decoupled XData Architecture**:
   - `HPDcePro.Core.XData` houses the 13 typed DTO models and `IXDataCodec<T>` implementations operating on `XDataPacket`. These have zero AutoCAD dependencies and are tested via unit tests in `HPDcePro.Core.Tests`.
   - `HPDcePro.AutoCad.XData` contains `AutoCadXDataService` converting between `ResultBuffer` and `XDataPacket` for live AutoCAD `DBObject`s.
4. **Interactive Command Realization**:
   - A centralized `PromptHelper` in `HPDcePro.AutoCad.Prompts` wraps `Editor.GetPoint`, `Editor.GetSelection`, `Editor.GetEntity`, `Editor.GetKeywords`, and `Editor.GetDouble` with graceful null checks (`doc == null || doc.Editor == null`).
   - Commands in `BeamCommands.cs`, `ColumnCommands.cs`, `FootingCommands.cs`, `SlabCommands.cs`, and `RebarCommands.cs` replace `CalculateSample*` and dummy rendering by prompting for real input and invoking the corresponding Jigs. When running headless, prompts detect non-interactive mode and exit gracefully.

---

## 3. Caveats

1. **No Live AutoCAD Event Loop in Test Runner**: xUnit tests in `HPDcePro.Adapters.Tests` run out-of-process without an active `acad.exe` graphics device context. `WorldDraw` and `Sampler` cannot be triggered by real Windows mouse events during automated tests. Testing must target: (a) class reflection & command registrations, (b) pure geometry methods of each Jig, and (c) mock `WorldDraw` geometry emission.
2. **AutoCAD ResultBuffer Memory Management**: `ResultBuffer` implements `IDisposable`. Any `ResultBuffer` returned by `ent.XData` or passed into `ent.XData = rb` must be properly disposed to prevent unmanaged memory leaks in AutoCAD.
3. **Clean Code 300-Line Limit**: Detailed Jigs with complex geometry (e.g. `JigVeThepVachCt`, `JigVeVachHam`) must not exceed 300 lines. Helper methods for hatch generation or dimension offsets must be placed in internal geometry helper classes.

---

## 4. Conclusion & Implementation Blueprint for Worker M3

### 4.1 Subsystem 7: 11 Interactive CAD Jigs (`src/HPDcePro.AutoCad/Jigs/`)

#### Base Class: `DceDrawJigBase.cs`
```csharp
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;

namespace HPDcePro.AutoCad.Jigs
{
    public abstract class DceDrawJigBase : DrawJig
    {
        public abstract PromptStatus StartJig(Editor editor);
    }
}
```

#### Blueprint for the 11 Jigs:

| Jig Class | Legacy Token | Target File | Core Geometry / Formulas | Key Methods & Skeletons |
|---|:---:|---|---|---|
| **`VeMBKCJig`** | `0x02000E55` | `src/HPDcePro.AutoCad/Jigs/VeMBKCJig.cs` | Vector $\vec{v} = P_2 - P_1$; Normal $\vec{n} = \frac{(-v_y, v_x)}{\|\vec{v}\|}$. Corners: $D_1 = P_1 + \vec{n}\frac{b}{2}$, $D_2 = P_2 + \vec{n}\frac{b}{2}$, $D_3 = P_2 - \vec{n}\frac{b}{2}$, $D_4 = P_1 - \vec{n}\frac{b}{2}$. Alignment shift for Top/Mid/Bot. | `StartJig(Editor ed)`, `StartJig(Editor ed, Point3d p1, string text, double width, string align, double scale)`, `CalculateCorners(...)` |
| **`JigSlabRebar`** | `0x02000E54` | `src/HPDcePro.AutoCad/Jigs/JigSlabRebar.cs` | Clear span $L_0 = \|P_2 - P_1\|$. Curtailment cutoff $L_0/4$ or $L_0/3$. Anchorage hook 90° ($12d$) or 180° hook with bulge calculation for curved fillets. | `StartJig(Editor ed)`, `CalculateRebarVertices(Point3d p1, Point3d p2, double offsetL, double hookL, double offsetR, double hookR, int diameter)`, `XacDinhPolyline(...)` |
| **`TagLeaderJig`** | `0x02000E52` | `src/HPDcePro.AutoCad/Jigs/TagLeaderJig.cs` | 3-point leader line: $P_1$ (touch point on bar), $P_2$ (elbow shoulder), $P_3$ (text landing shelf). Dynamic horizontal shelf line based on text length: $P_3 = P_2 + (\pm \text{shelfLen}, 0)$. | `StartJig(Editor ed)`, `CalculateLeaderPoints(Point3d p1, Point3d cursor, double shelfLength, bool mirror)`, `WorldDraw(...)` |
| **`TagChiaThepJig`** | `0x02000E53` | `src/HPDcePro.AutoCad/Jigs/TagChiaThepJig.cs` | Spacing distribution: $N = \lfloor L / a \rfloor + 1$. Rubber-band dimension tick marks perpendicular to distribution line with callout text `"{N}Φ{d} a{s}"`. | `StartJig(Editor ed)`, `CalculateDistributionTicks(Point3d p1, Point3d p2, double spacing, double tickLen)`, `WorldDraw(...)` |
| **`JigRaiThepChamTron`** | `0x02000E56` | `src/HPDcePro.AutoCad/Jigs/JigRaiThepChamTron.cs` | Offset vector by perpendicular $osvg$. $N = \lfloor (L - ostrai - osphai) / KhoangRai \rfloor$. Dot circle radius $r = dkt / 2$. Skip flags `BoDiemDau`, `BoDiemCuoi`. | `StartJig(Editor ed)`, `CalculateDotPositions(Point3d p1, Point3d p2, double ostrai, double osphai, double osvg, double spacing, bool skipFirst, bool skipLast)`, `TapHopDiem` |
| **`JigVeDaiVong`** | `0x02000E57` | `src/HPDcePro.AutoCad/Jigs/JigVeDaiVong.cs` | Circular/oval closed hoop stirrup. Radius $R = \|P_2 - P_1\|$. Overlapping seismic 135° hooks of length $10d$ at splice vertex. Bulge $= \tan(\theta/4)$. | `StartJig(Editor ed)`, `CalculateHoopGeometry(Point3d center, double radius, double hookLen, double diameter)`, `WorldDraw(...)` |
| **`JigVeDaiC`** | `0x02000E58` | `src/HPDcePro.AutoCad/Jigs/JigVeDaiC.cs` | Open C-shaped tie spanning between two rebar points. Polyline vertices: Hook left 90°/135° $\rightarrow$ straight tie length $L \rightarrow$ Hook right 90°/135°. | `StartJig(Editor ed)`, `CalculateCTiePoints(Point3d p1, Point3d p2, double hookLeft, double hookRight, int diameter)`, `WorldDraw(...)` |
| **`VePolylineBatKyJig`** | `0x02000E59` | `src/HPDcePro.AutoCad/Jigs/VePolylineBatKyJig.cs` | Dynamic multi-vertex rebar drafting. Previews segment from previous vertex to cursor. Automatically calculates bend fillet radius $R = 2.5d$ ($5d$ bend diameter) at sharp corners. | `StartJig(Editor ed)`, `AppendVertex(Point3d pt)`, `CalculateFilletedRebar(List<Point3d> pts, double bendRadius)`, `WorldDraw(...)` |
| **`JigVeThepVachCt`** | `0x02000E5A` | `src/HPDcePro.AutoCad/Jigs/JigVeThepVachCt.cs` | Shear wall boundary element detailing ($l_c \times t_w$). Generates: (1) Boundary cage closed stirrup, (2) Boundary longitudinal bars ($dktchuBien$), (3) Web distributed bars ($dktchuGiua$), (4) Cross-ties. | `StartJig(Editor ed)`, `CalculateBoundaryCage(Point3d origin, double lc, double tw, int barCountBoundary, int barCountWeb, double cover)`, `WorldDraw(...)` |
| **`JigVeVachHam`** | `0x02000E5B` | `src/HPDcePro.AutoCad/Jigs/JigVeVachHam.cs` | Basement retaining wall cross-section. Outlines stem thickness $t_w$, footing key, top slab connection, waterstop location ($bcnd$), inner/outer vertical flexural bars, horizontal distribution ties, and concrete hatch. | `StartJig(Editor ed)`, `CalculateRetainingWallSection(Point3d basePt, double height, double tw, double coverLeft, double coverRight, bool waterstop)`, `WorldDraw(...)` |
| **`DimNhanhMbkcJig`** | `0x02000E5C` | `src/HPDcePro.AutoCad/Jigs/DimNhanhMbkcJig.cs` | Framing multi-tier dimensioning. Projects framing beam/column nodes onto dimension line. If `Dim2Hang = true`, generates Tier 1 (bay-by-bay dimensions) + Tier 2 (overall total dimension). | `StartJig(Editor ed)`, `CalculateDimensionChains(Point3d p1, Point3d p2, List<Point3d> framingNodes, double offsetTier1, double offsetTier2, bool twoTiers)`, `WorldDraw(...)` |

---

### 4.2 Subsystem 13 RegApps XData Codecs (`src/HPDcePro.Core/XData/`)

Implement the 10 missing codecs in `HPDcePro.Core.XData` following `IXDataCodec<T>`:

1. **`ThepKepXDataCodec`** (`Dce_ThepKep`):
   ```csharp
   public class ThepKepModel {
       public int BundleCount { get; set; } = 2;
       public double SpacingMm { get; set; } = 30.0;
       public double ClearanceMm { get; set; } = 25.0;
       public string PrimaryBarHandle { get; set; } = "0";
   }
   ```
2. **`SoDoCatXDataCodec`** (`XDATA_APP_SODOCAT`):
   ```csharp
   public class SoDoCatModel {
       public string StockBarId { get; set; } = "1";
       public int PatternIndex { get; set; } = 1;
       public string CutEquation { get; set; } = "5800+5800=11600";
       public double WasteMm { get; set; } = 100.0;
       public double WasteRatio { get; set; } = 0.0085;
   }
   ```
3. **`KhoThepDuXDataCodec`** (`XDATA_APP_KHOTHEPDU`):
   ```csharp
   public class KhoThepDuModel {
       public string OffcutId { get; set; } = "DU-001";
       public int DiameterMm { get; set; } = 20;
       public double LengthMm { get; set; } = 3500.0;
       public string StorageRack { get; set; } = "RACK-A1";
   }
   ```
4. **`MatBangMongXDataCodec`** (`XDATA_MAT_BANG_MONG`):
   ```csharp
   public class MatBangMongModel {
       public string GridMark { get; set; } = "DC-1";
       public double TopElevationM { get; set; } = -1.600;
       public double BottomElevationM { get; set; } = -2.500;
       public string PileType { get; set; } = "D400";
       public int PileCount { get; set; } = 4;
   }
   ```
5. **`MongCocXDataCodec`** (`XDATA_MONG_COC`):
   ```csharp
   public class MongCocModel {
       public string CapMark { get; set; } = "DC-1";
       public double DimX { get; set; } = 1800.0;
       public double DimY { get; set; } = 1800.0;
       public double HeightH { get; set; } = 900.0;
       public string RebarBottomX { get; set; } = "Phi16 a150";
       public string RebarBottomY { get; set; } = "Phi16 a150";
       public double ConcreteVolumeM3 { get; set; } = 2.916;
   }
   ```
6. **`ThangMoiXDataCodec`** (`SYS_XData_ThangMoi`):
   ```csharp
   public class ThangMoiModel {
       public string StairMark { get; set; } = "THANG-1";
       public double FloorHeightMm { get; set; } = 3600.0;
       public int RiserCount { get; set; } = 21;
       public double TreadWidthMm { get; set; } = 280.0;
       public double WaistThicknessMm { get; set; } = 120.0;
       public string MainRebar { get; set; } = "Phi12 a150";
   }
   ```
7. **`SysAppXDataCodec`** (`SYS_XData_App`):
   ```csharp
   public class SysAppModel {
       public string AppVersion { get; set; } = "HPDcePro_2026";
       public string ProjectCode { get; set; } = "PRJ-01";
       public string Timestamp { get; set; } = string.Empty;
   }
   ```
8. **`NhanCocXDataCodec`** (`NHAN_XDATA_COC`):
   ```csharp
   public class NhanCocModel {
       public string PileMark { get; set; } = "C1";
       public double DiameterMm { get; set; } = 400.0;
       public double CapacityTons { get; set; } = 65.0;
       public double CutoffElevationM { get; set; } = -2.400;
       public double CoordX { get; set; } = 0.0;
       public double CoordY { get; set; } = 0.0;
   }
   ```
9. **`NhanDamCongXDataCodec`** (`NHAN_XDATA_DAM_CONG`):
   ```csharp
   public class NhanDamCongModel {
       public string BeamMark { get; set; } = "DC-1";
       public double RadiusMm { get; set; } = 8500.0;
       public double ArcAngleDeg { get; set; } = 45.0;
       public double SectionWidthMm { get; set; } = 220.0;
       public double SectionHeightMm { get; set; } = 400.0;
   }
   ```
10. **`NhanMbXDataCodec`** (`NHAN_XDATA_MB`):
    ```csharp
    public class NhanMbModel {
        public string PlanId { get; set; } = "MBKC_T2";
        public double ElevationM { get; set; } = 3.600;
        public string MemberType { get; set; } = "BEAM";
        public string StoryName { get; set; } = "STORY2";
    }
    ```

#### AutoCAD XData Service (`src/HPDcePro.AutoCad/XData/AutoCadXDataService.cs`):
Provides bidirectional conversion between AutoCAD `DBObject.XData` (`ResultBuffer`) and `XDataPacket`:
```csharp
namespace HPDcePro.AutoCad.XData
{
    public static class AutoCadXDataService
    {
        public static XDataPacket? ReadPacket(DBObject obj, string regAppName);
        public static void WritePacket(DBObject obj, XDataPacket packet, Transaction tr);
        public static ResultBuffer ToResultBuffer(XDataPacket packet);
        public static XDataPacket FromResultBuffer(ResultBuffer rb, string regAppName);
    }
}
```

---

### 4.3 Command Realization Pattern (`src/HPDcePro.AutoCad/Prompts/`)

#### Central Prompt Helper: `PromptHelper.cs`
```csharp
namespace HPDcePro.AutoCad.Prompts
{
    public static class PromptHelper
    {
        public static bool TryGetPoint(Editor ed, string prompt, out Point3d point, Point3d? basePoint = null);
        public static bool TryGetDouble(Editor ed, string prompt, double defaultValue, out double val);
        public static bool TryGetInteger(Editor ed, string prompt, int defaultValue, out int val);
        public static bool TryGetString(Editor ed, string prompt, string defaultValue, out string val);
        public static bool TryGetKeywords(Editor ed, string prompt, string[] keywords, string defaultKeyword, out string selected);
        public static bool TryGetEntity(Editor ed, string prompt, Type[] allowedTypes, out ObjectId entityId);
        public static bool TryGetSelection(Editor ed, string prompt, SelectionFilter? filter, out SelectionSet selection);
    }
}
```

#### Upgraded Commands Realization:
1. `VeDam_DCE`: If interactive (`ed != null`), prompt user for start point $P_1$, invoke `VeMBKCJig.StartJig(ed, p1, ...)`. On completion, calculate reinforcement via `ContinuousBeamCalculator` and render rebar buffer.
2. `BlockBeam_Size`, `BlockBeam_Name`, `BlockBeam_Width`: Use `PromptHelper.TryGetEntity` to select an existing beam polyline/block, read its attached XData (`Links_XDataObject` or `RebarXdataDCE`), modify the target attribute/layer, and update the entity under a Transaction.
3. `ColumnREC1`, `ColumnREC2`, `ColumnCi`: Prompt user for column placement point $P$, section dimensions $b \times h$, calculate P-M-M interaction via `ColumnPmmInteractionCalculator`, and insert the column boundary polyline + rebar dots at $P$.
4. `VeBlockCoc`, `VeMongCoc_DCE`: Prompt user for insertion point $P$, pile diameter, pile count, calculate pile cap via `PileCapCalculator`, render drawing buffer with `FoundationDetailingGenerator` at $P$.
5. `VeThepSanTopX`, `VeThepSanBotX`: Prompt for span start point $P_1$ and end point $P_2$, run `JigSlabRebar`, and render rebar with curtailment cutoffs and `RebarXdataDCE` XData.
6. `QuetThongKe_dce`: Use `PromptHelper.TryGetSelection` with rebar XData filter (`DxfCode.ExtendedDataRegAppName = "RebarXdataDCE"`), iterate selected entities, decode rebar models using `RebarXDataCodec`, aggregate schedules using `RebarScheduleAggregator`, and output summary table.

---

### 4.4 Comprehensive Worker M3 Execution Checklist

- [ ] **Step 1**: Create `src/HPDcePro.Core/XData/Codecs/` with all 10 remaining codecs (`ThepKepXDataCodec`, `SoDoCatXDataCodec`, `KhoThepDuXDataCodec`, `MatBangMongXDataCodec`, `MongCocXDataCodec`, `ThangMoiXDataCodec`, `SysAppXDataCodec`, `NhanCocXDataCodec`, `NhanDamCongXDataCodec`, `NhanMbXDataCodec`).
- [ ] **Step 2**: Add comprehensive xUnit tests in `tests/HPDcePro.Core.Tests/XDataAllCodecsTests.cs` testing 100% round-trip encode/decode, malformed packet recovery, and invalid app name handling for all 13 codecs.
- [ ] **Step 3**: Create `src/HPDcePro.AutoCad/XData/AutoCadXDataService.cs` implementing `ToResultBuffer`, `FromResultBuffer`, `ReadPacket`, and `WritePacket`.
- [ ] **Step 4**: Create `src/HPDcePro.AutoCad/Prompts/PromptHelper.cs` wrapping interactive Editor prompts with safe fallbacks.
- [ ] **Step 5**: Create `src/HPDcePro.AutoCad/Jigs/DceDrawJigBase.cs` and all 11 sealed Jig classes (`VeMBKCJig`, `JigSlabRebar`, `TagLeaderJig`, `TagChiaThepJig`, `JigRaiThepChamTron`, `JigVeDaiVong`, `JigVeDaiC`, `VePolylineBatKyJig`, `JigVeThepVachCt`, `JigVeVachHam`, `DimNhanhMbkcJig`). Ensure each file is $\le 300$ lines.
- [ ] **Step 6**: Upgrade `BeamCommands.cs`, `BeamCommands.Editing.cs`, `ColumnCommands.cs`, `FootingCommands.cs`, `SlabCommands.cs`, and `RebarCommands.cs` using `PromptHelper` and Jigs, eliminating all dummy `CalculateSample*` calls.
- [ ] **Step 7**: Create unit and adapter tests in `tests/HPDcePro.Adapters.Tests/CadJigsAndCommandsTests.cs` verifying all 11 Jigs' geometric calculation engines, reflection registration of all 11 Jigs, and command execution fallbacks.
- [ ] **Step 8**: Update `docs/rea-dce-pro/TRUTH_MATRIX.md` updating Subsystem 7 (11 CAD Jigs) from `ĐANG LÀ MOCK` to `HOÀN THÀNH`, and marking realigned AutoCAD commands as `HOÀN THÀNH`.
- [ ] **Step 9**: Execute sequential single-node build and test runner verification (MTP direct `.exe` execution).

---

## 5. Verification Method

To independently verify Worker M3's implementation:

1. **Compilation Check (Single-Node, No MSBuild Server)**:
   ```powershell
   dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   ```
   *Expected Result*: Succeeded with `0 Warning(s), 0 Error(s)`.

2. **Core Tests Direct Binary Execution (No `dotnet test`)**:
   ```powershell
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
   ```
   *Expected Result*: All existing 259+ tests plus new 13 XData codec tests pass 100% green.

3. **Adapters Tests Direct Binary Execution**:
   ```powershell
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
   ```
   *Expected Result*: All existing 110+ tests plus new Jig geometric engine tests pass 100% green.

4. **Clean Code Rule Scan**:
   Verify every new `.cs` file in `HPDcePro.Core/XData/` and `HPDcePro.AutoCad/Jigs/` is $\le 300$ lines, every method is $\le 50$ lines, with zero commented-out code and zero unannotated empty catch blocks.
