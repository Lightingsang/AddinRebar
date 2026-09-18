# ADR-03 — Globals `civil` thêm vào AutoCAD; đơn vị: mm cho hình học phẳng, đơn vị bản vẽ cho station/elevation; `Civil3dInfo` trong context

**Ngày:** 2026-09-17 · **Status:** **Accepted (revised)** 2026-09-18 — S-04a/b, S-06 PASS; U1 và U11 sửa như dưới ([reports/phase-01-spike.md](../reports/phase-01-spike.md) run 2–4) · **Owner:** HPCivil3d
**Kế thừa:** [AutoCAD ADR-03 §Globals (`tr`, `units`)](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-03-autocad-transaction-undo-dryrun-policy.md) · AEC ADR-02 "mm at the tool boundary whatever INSUNITS is" (CLAUDE.md § AEC engine) · Navis ADR-04 §4 (`units` từ `Document.Units`)
**Bằng chứng:** [addendum §2–3, §12 U1/U10/U11](../research/reflection-addendum-verified-signatures.md) · [researcher-02 §5](../research/researcher-02-civil3d-autoloader-launch-rebuild-facts.md) · `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptUnits.cs:11–41` (`ScriptUnits(label, mmPerUnit, note)`, `ToDrawing`, `ToMm`) · `HPAutoCad/HPAutoCad.McpBridge/Model/AutocadScriptGlobals.cs:17–55` · `HPAutoCad/HPAutoCad.McpBridge/Service/AutocadScriptRunner.cs:59` (`AutocadInsunits.For((int)db.Insunits)`) · `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs:24–30, 66–73` (`AutocadInfo` 7 field, slot per host) · `McpShared/HPRebar.Mcp.Server.Core/Services/ContextService.cs:52–65` (`Shape` bỏ `revitVersion/isFamily` cho non-Revit; null bị bỏ).

## Context
- Script Civil vẫn là script AutoCAD: cần `doc/db/ed/app/tr/units/ct/log/progress/args` nguyên (`tr` để `GetObject` alignment/surface — chúng là `Autodesk.AutoCAD.DatabaseServices.Entity` qua `Autodesk.Aec.DatabaseServices.Entity`; addendum §4). Thêm **một** global: `civil` = `CivilDocument` (`CivilApplication.ActiveDocument` — addendum §2) vì mọi truy vấn Civil bắt đầu từ đó (`GetAlignmentIds()`, `GetSurfaceIds()`, `CogoPoints`, `Settings`, `Styles`).
- Đơn vị: Civil API trả **đơn vị bản vẽ** — `SettingsUnitZone.DrawingUnits ∈ {Meters, Feet}` (enum chỉ hai giá trị, addendum §3) — cho XY, station, elevation, chiều dài, đường kính ống. AutoCAD MCP đã chốt mm ở biên tool (mọi seed AutoCAD, AEC ADR-02). Station là giá trị **có ý nghĩa đọc** (label "1+250.00", `GetStationStringWithEquations`); đổi ra mm (1 250 000) vô nghĩa với người và không khớp label. Elevation tương tự (cao độ 12.345 m).
- Context: AI cần biết ngay có phải Civil doc không, đơn vị bản vẽ, zone, và có gì trong bản vẽ (đếm) để chọn seed — như `NavisInfo`/`EtabsInfo` (≤ 10–11 field; ETABS red-team #12 cắt từ 19).

## Decision

### 1. Globals (`HostScriptContracts.Civil3dGlobals`, phase 0)
`{ "doc", "db", "ed", "app", "tr", "units", "civil", "ct", "log", "progress", "args" }` — AutoCAD + `civil`. Kiểu `Civil3dScriptGlobals` (NEW, `HPCivil3d.McpBridge/Model/`) = copy `AutocadScriptGlobals` + `public readonly CivilDocument civil;` (nullable về kiểu nhưng bridge **không chạy script khi null** — §4). `units` là `ScriptUnits` (Core). `civil.Settings`, `civil.Styles` là đường vào settings/styles — không thêm global riêng (YAGNI).

### 2. Imports (`HostScriptContracts.Civil3dImports`, phase 0)
`AutocadImports` **trừ** `"HPAutoCad.Aec"` **cộng** `"Autodesk.Civil"`, `"Autodesk.Civil.ApplicationServices"`, `"Autodesk.Civil.DatabaseServices"`, `"Autodesk.Civil.DatabaseServices.Styles"`, `"Autodesk.Civil.Settings"` (5 namespace — tên xác nhận bằng reflection; `CivilDocumentPressurePipesExtension` nằm trong `Autodesk.Civil.ApplicationServices` nên extension pressure dùng được khi có reference `AeccPressurePipesMgd`). **Không** import `Autodesk.Civil.DataShortcuts`, `Autodesk.Civil.AeccUiMgd`, `Autodesk.AECC.Interop.*` (guard deny — ADR-04). Compile references của bridge (`Civil3dBridgeEntry.CompilerReferences`): AutoCAD 3 DLL + `AeccDbMgd` + `AecBaseMgd` + `AeccPressurePipesMgd` + Core.

### 3. Đơn vị — hai lớp, mỗi field tự khai đơn vị
| Đại lượng | Ở biên tool (schema/envelope) | Trong script | Quy ước tên |
|---|---|---|---|
| XY plan, khoảng cách phẳng, offset, chiều dài, đường kính/chiều rộng ống, bán kính | **mm** (như AutoCAD MCP) | `units.ToMm(x)` / `units.ToDrawing(mm)` | điểm `{x, y}` (mm — như AutoCAD seeds; description tool nói rõ); scalar hậu tố `Mm` (`lengthMm`, `radiusMm`, `innerDiameterMm`, `offsetMm`) |
| Station | **đơn vị bản vẽ** (m hoặc ft) | thô từ API | `startStation`, `endStation`, `station` + `stationLabel` (`GetStationStringWithEquations`) |
| Elevation / Z / rim / sump / invert | **đơn vị bản vẽ** | thô từ API | `elevation`, `rimElevation`, `z` |
| Slope | tỉ số (unitless) + `slopePercent` | `Pipe.Slope` | |
| Diện tích | m² (metric) / ft² (imperial) — `areaDu2` **không**; dùng `area` + envelope unit | `TerrainSurfaceProperties.SurfaceArea2D` | |
| Envelope | mọi seed trả `"drawingUnit": "Meters"|"Feet"` (từ `DrawingUnits`) và `"lengthUnit": "mm"` cho các field `Mm`/`{x,y}` | | một chỗ khai, field không suffix = drawing unit |

- **`units` của Civil dựng từ `civil.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits`** (Meters → `new ScriptUnits("Meters", 1000)`, Feet → `("Feet", 304.8)`), **không** từ `db.Insunits` như AutoCAD: template Civil có thể để INSUNITS Unitless/khác; Civil coi 1 unit = 1 m/ft theo setting của nó. Bridge đọc cả hai; khi `AutocadInsunits.For(db.Insunits)` ≠ Civil → `log("INSUNITS <x> disagrees with Civil drawing units <y>; scripts use Civil units")` + `Civil3dInfo.InsunitsMismatch=true`. S-06 kiểm trên `Align-1.dwg` (Imperial) và template Metric.
- Lý do không "mm cho tất cả": station/elevation là số người đọc trên label; AI cũng đọc lại chúng từ `stationLabel`. Lý do không "drawing unit cho tất cả": mất tương thích với 62 tool AutoCAD (mm) và với `ScriptUnits` chung; mm phẳng cho phép AI ghép dữ liệu AutoCAD (query_entities) với Civil trong một hội thoại.

### 4. `Civil3dInfo` (Contracts, phase 0) — 11 field, `ContextResult.Civil3d` slot sau `:30`
```csharp
public sealed record Civil3dInfo(
    string Product,              // CivilApplication.ActiveProduct.ToString(): Civil3D | Civil | Map | Other | Unknown
    bool IsCivilDocument,        // civil != null (S-04 quyết định thế nào là "không phải Civil doc")
    string? DrawingUnit,         // "Meters" | "Feet" | null
    string? CoordinateSystemCode,// SettingsUnitZone.CoordinateSystemCode ("" → null)
    bool InsunitsMismatch,       // §3
    int AlignmentCount, int SurfaceCount, int CorridorCount, int PipeNetworkCount, int PressureNetworkCount, int CogoPointCount);
```
- `ParcelCount`/`SiteCount`/`ProfileCount` **không** vào context (phải duyệt site/alignment — chi phí; seed `get_civil_document_info` trả đủ). Đếm = `ObjectIdCollection.Count` — không mở object, < 50 ms.
- `Units.Length` của `ContextResult` = `units.Label` Civil ("Meters"/"Feet"); `Autocad` slot **cũng điền** (`AutocadInfo` 7 field — layout/layer/quiescent vẫn đúng và AI dùng chung mental model) → context Civil = AutoCAD + `civil3d`. `Shape` không sửa (null bị bỏ; `revitVersion/isFamily` đã bị bỏ cho non-Revit).
- Khi `civil == null`: `IsCivilDocument=false`, đếm 0; `execute` với `transaction` bất kỳ → `-32003` "No Civil 3D document — open a drawing in Civil 3D 2026 (not plain AutoCAD)" **hoặc** vẫn chạy như AutoCAD với `civil` null? → **Quyết:** chạy, `civil` null, description tool nói "check `civil != null`"; lý do: drawing rỗng trong Civil 3D vẫn là Civil doc theo API (mọi DWG mở trong Civil 3D có `CivilDocument`) — S-04 xác nhận `ActiveDocument` có bao giờ null/throw không; nếu throw → bridge bắt và để null.

### 5. Resources / prompts
`civil3d://document/info` = `Civil3dInfo` + `AutocadInfo`; `civil3d://selection` = như AutoCAD (`ed.SelectImplied` handles + `GetType().Name` — Civil type names `Alignment`/`TinSurface`/`CogoPoint` tự hiện). Prompt `civil3d_query_template` few-shot: `var ids = civil.GetAlignmentIds(); foreach (ObjectId id in ids) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); … units.ToMm(...) … }`; `civil3d_modify_template`: `CogoPoints.Add` dưới `auto` + dryRun trước.

## Alternatives rejected
- **`civil` không phải global, script tự gọi `CivilApplication.ActiveDocument`:** được, nhưng null-check lặp ở 12 seed và bridge không biết "không phải Civil doc" để điền context; global rẻ hơn.
- **Đổi mọi thứ ra mm kể cả station** (đồng nhất tuyệt đối): xem §3 — vô nghĩa với label; loại.
- **Global `stations` helper (format/parse "1+250")**: `GetStationStringWithEquations` đã có; YAGNI.
- **Đưa `units` Civil lên `McpShared`** (`ScriptUnits.FromCivil`): Core không biết Civil enum; bridge dựng `ScriptUnits(label, mmPerUnit)` bằng ctor public — 0 sửa engine.

## Consequences
- Phase 0: `Civil3dImports`, `Civil3dGlobals`, `Civil3dInfo` (+ slot) — additive, null bị bỏ → 4 host byte-identical.
- Phase 2: `Civil3dScriptGlobals`, `Civil3dContextReader` (copy AutoCAD + đếm), units từ Civil settings + mismatch log.
- Phase 3: mọi seed khai `drawingUnit`/`lengthUnit` trong envelope; `SeedLibraryTests` kiểm mỗi field số có hậu tố `Mm` **hoặc** thuộc danh sách "drawing-unit fields" (`station*`, `elevation*`, `rim*`, `sump*`, `z`, `area*`, `slope*`) — test cơ học "every field unit-labelled".

## Open items — đóng bởi spike 2026-09-18
- ~~U1~~ → **revised**: `CivilApplication.ActiveDocument` trả `CivilDocument` cho **mọi** DWG mở trong Civil 3D, kể cả drawing mới từ `acad.dwt` (S-04b: `isCivilDocument true`, `DrawingUnits Feet` mặc định). `civil` thực tế không null trong Civil 3D; nhánh null của `Civil3dScriptRunner` chỉ là phòng thủ. Hệ quả: template không có Civil settings → `DrawingUnits` mặc định **Feet** dù INSUNITS khác → `insunitsMismatch true` là tình huống thật, description tool phải nói rõ.
- ~~U11~~ → **revised**: "không zone" là `CoordinateSystemCode == "."` (không rỗng); `GetCoordinateSystemByCode("")` ném `ArgumentException`; `GetCoordinateSystemByCode(".")` trả "No Datum, No Projection". Bridge chuẩn hoá `"."`/rỗng → `coordinateSystemCode null` (`Civil3dUnits.ReadCoordinateSystemCode`). Có zone: `NH83F` → "NAD83 New Hampshire State Planes, US Foot".
- ~~U10~~ → station/elevation/area đọc ra đúng drawing unit (S-05 alignment 1 399.81 với `DrawingUnits Meters`; S-09 `Parcel.Area` 3 349 m²; S-07 elevation 65.21). Pipe network field chưa đọc live (không seed pipe nào là W; R8 ở phase 3 kiểm bằng `Pipe Networks-1C`).
- Label `Units.Length` = "Meters"/"Feet" (Civil) thay cho tên INSUNITS: giữ; description `execute_civil3d_code` nói rõ mm ở biên, station/elevation theo drawing unit.
