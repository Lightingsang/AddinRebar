# Reflection addendum — chữ ký Civil 3D 2026 .NET API đã kiểm (2026-09-17)

Probe riêng (`System.Reflection.MetadataLoadContext`, metadata-only, không nạp acad.exe) chạy từ scratchpad `reflect/Probe/` trên `C:\Program Files\Autodesk\AutoCAD 2026\{,C3D,ACA}\*.dll` + shared framework 8.0. Bổ sung và **sửa** [researcher-01](researcher-01-civil3d-api-reflection.md) (88 KB, đúng ở phần assembly facts/namespace; sai ở vài dòng tóm tắt). Mọi dòng dưới = `verified by reflection <assembly>` trừ khi ghi `[chưa xác minh]`. Đây là **danh sách member duy nhất seed/bridge được dùng**; member ngoài danh sách → phải probe thêm hoặc gắn `[chưa xác minh]`.

## 0. Sửa researcher-01 / researcher-02
| Khẳng định | Thực tế (reflection) |
|---|---|
| r01: `CivilDocument` NOT FOUND | Có — **`Autodesk.Civil.ApplicationServices.CivilDocument`** (r01 tra nhầm `DatabaseServices`), 36 member, base `Autodesk.AutoCAD.Runtime.DisposableWrapper` |
| r01/r02: `Parcel.Area`, `Parcel.Perimeter`, `ParcelLoops`, `ParcelSegments` | **ĐÍNH CHÍNH 2026-09-18 (spike S-09, `inspect_type` runtime):** `Parcel` **có `Double Area { get; }`** (kế thừa — probe metadata của addendum chỉ dump member *declared* nên bỏ sót; `Perimeter`, `ParcelLoops`, `ParcelSegments` vẫn không thấy). Declared: `Number`, `TaxId`, `Address`, `Centroid`, `AreaLocation`, `AreaSelectionLabel*`, UDP get/set; kế thừa `Name/Description/StyleName/StyleId`. Seed `list_parcels` đọc `p.Area` (drawing unit²) |
| r02: "không có API bật/tắt Rebuild – Automatic" | Có — `Corridor.RebuildAutomatic { get; set; }`, `Surface.AutoRebuild { get; set; }` |
| r02: `CogoPointCollection.Renumber()` | `Renumber` nằm trên **`CogoPoint`**: `UInt32 Renumber(UInt32 newPointNumber[, PointNumberResolveType])`; collection có `SetPointNumber(ObjectId, UInt32)` |
| r02: `Alignment.GetStationAndOffsetAtPoint` | Không có. Đúng: `StationOffset(Double easting, Double northing, ref Double station, ref Double offset)` (+ overload `tolerance`; `StationOffsetAcceptOutOfRange(..., ref Boolean outofrange)`) |
| r02: `UnitZoneSettings.Zone` | Không có. Đúng: `SettingsUnitZone.CoordinateSystemCode { get; set; }` + `static SettingsCoordinateSystem GetCoordinateSystemByCode(String code)` → `{Category, Code, Datum, Description, Projection, Unit}` |
| r02: profile `/p "<<C3D Metric>>"` (dấu cách) | Sai — registry `HKCU\…\ACAD-9100:409\Profiles` = `<<C3D_Imperial>>`, `<<C3D_Metric>>`, `AutoCAD`; shortcut Autodesk dùng `<<C3D_Metric>>` ([evidence E13](evidence-on-machine-2026-09-17.md)) |
| r02: "một pipe cho cả AutoCAD và Civil" | Loại — `PipeListener` `maxInstances=1`, hai process không chia một pipe; user đã chốt `hpcivil3d-mcp-2026` (ADR-02) |

## 1. Assembly (r01 §1 đúng, tóm lại)
`AeccDbMgd` 13.8.0.280, `TargetFramework .NETCoreApp,Version=v8.0`, 1 596 public type, ref `Acdbmgd 25.1.0.0`, `AecBaseMgd 8.8.58.0`; `AeccPressurePipesMgd` 89 type; `AeccDataShortcutMgd` 95; `AeccCogoMgd` 156; `AeccUiMgd` 244 (namespace `Autodesk.Civil.AeccUiMgd[.PVUpdateBand|.PressurePipes|.Roadway|.Survey|.TrCmd]`); `AecBaseMgd` 265; `AecPropDataMgd` 180. **Reference set compile:** `acdbmgd, acmgd, accoremgd` (NuGet `AutoCAD.NET 25.1.0` như HPAutoCad) + `C3D\AeccDbMgd.dll` + `ACA\AecBaseMgd.dll` (+ `AeccPressurePipesMgd.dll` nếu dùng extension pressure); `AecPropDataMgd` chỉ khi đụng property set. COM interop `Autodesk.AECC.Interop.{Land,Pipe,Roadway,Survey,UiLand,UiPipe,UiRoadway,UiSurvey}.dll` là assembly riêng trong `C3D\` → không reference, guard deny namespace.

## 2. Root
```
static CivilDocument CivilApplication.ActiveDocument { get; }           // AeccDbMgd, Autodesk.Civil.ApplicationServices
static ProductType   CivilApplication.ActiveProduct  { get; }           // enum ProductType { Civil, Civil3D, Map, Other, Unknown }
static SurveyProjectCollection CivilApplication.SurveyProjects { get; } // → guard deny
static CivilDocument CivilDocument.GetCivilDocument(Database database)
CivilDocument: ObjectIdCollection GetAlignmentIds() · GetSitelessAlignmentIds() · GetSiteIds() · GetSurfaceIds() · GetPipeNetworkIds() · GetAllPointIds() · GetIntersectionIds() · GetViewFrameGroupIds() · GetSitelessFeatureLineIds()
              ObjectId GetSitelessAlignmentId(String name)
              CorridorCollection CorridorCollection · CogoPointCollection CogoPoints · PointGroupCollection PointGroups · AssemblyCollection AssemblyCollection · SubassemblyCollection SubassemblyCollection
              SettingsRoot Settings · StylesRoot Styles · CorridorState CorridorState · PipeNetworkState NetworkState · Boolean IsDriveActive / IsCorridorSectionViewActive
static ObjectIdCollection CivilDocumentPressurePipesExtension.GetPressurePipeNetworkIds(CivilDocument)   // AeccPressurePipesMgd
```
Hành vi `ActiveDocument` khi bản vẽ không phải Civil / `ActiveProduct != Civil3D`: `[chưa xác minh]` → spike S-04.

## 3. Settings / units
```
SettingsRoot.DrawingSettings : SettingsDrawing → UnitZoneSettings : SettingsUnitZone, AmbientSettings : SettingsAmbient
SettingsUnitZone: DrawingUnitType DrawingUnits {get;set;}  // enum Autodesk.Civil.Settings.DrawingUnitType { Feet, Meters } — CHỈ HAI GIÁ TRỊ
                  ImperialToMetricConversionType ImperialToMetricConversion · Double DrawingScale · AngleUnitType AngularUnits · String CoordinateSystemCode
                  static SettingsCoordinateSystem GetCoordinateSystemByCode(String code) → Code/Datum/Description/Projection/Unit/Category
SettingsAmbient: Station, Distance, Elevation, Coordinate, GridCoordinate, Area, Volume, Speed, Angle, Direction  (format/precision — không đổi đơn vị API)
```
→ ADR-03: `units` của Civil dựng từ `DrawingUnits` (Meters → 1 000 mm/unit, Feet → 304.8), so với `db.Insunits` và log khi lệch.

## 4. Alignment
```
Entity (Autodesk.Civil.DatabaseServices.Entity : Autodesk.Aec.DatabaseServices.Entity : Autodesk.AutoCAD.DatabaseServices.Entity): Name {get;set;} · Description · DisplayName · StyleId · StyleName {get;set;}
Alignment: Double Length · StartingStation · EndingStation · EndingStationWithEquations · ReferencePointStation
           AlignmentType AlignmentType · Boolean IsSiteless · ObjectId SiteId · String SiteName · AlignmentEntityCollection Entities
           ObjectIdCollection GetProfileIds() · GetProfileViewIds() · GetAlignmentLabelIds() · GetChildOffsetAlignmentIds(Boolean)
           Void PointLocation(Double station, Double offset, ref Double easting, ref Double northing)  (+ overload tolerance, ref Bearing)
           Void StationOffset(Double easting, Double northing, ref Double station, ref Double offset)  (+ tolerance; + ...AcceptOutOfRange(..., ref Boolean outofrange))
           String GetStationStringWithEquations(Double rawStation) · Void Update()
  static ObjectId Create(CivilDocument, String alignmentName, String siteName, String layerName, String styleName, String labelSetName)
  static ObjectId Create(CivilDocument, PolylineOptions plineOptions, String alignmentName, String siteName, String layerName, String styleName, String labelSetName)
  static ObjectId Create(CivilDocument, PolylineOptions, String, ObjectId siteId, ObjectId layerId, ObjectId styleId, ObjectId labelSetId)
  static ObjectId CreateOffsetAlignment(Database, String alignmentName, String parentAlignmentName, Double offset, String styleName)
PolylineOptions (struct): ObjectId PlineId · Boolean AddCurvesBetweenTangents · Boolean EraseExistingEntities
AlignmentEntityCollection: Int32 Count · AlignmentEntity Item[...] · AlignmentEntity GetEntityByOrder(Int32) · EntityAtStation(Double) · FirstEntity · LastEntity (+ Add*Line/Curve/Spiral… → ngoài MVP)
AlignmentEntity: AlignmentEntityType EntityType · Int32 EntityId · EntityBefore · EntityAfter · Int32 SubEntityCount · AlignmentSubEntity Item[...]
AlignmentCurve : AlignmentEntity — Double StartStation · EndStation · Length · Point2d StartPoint · EndPoint
AlignmentLine : AlignmentCurve — Double Direction · Point2d MidPoint · PassThroughPoint1/2
AlignmentArc  : AlignmentCurve — Double Radius · PIStation · Delta · ChordLength · StartDirection · EndDirection · Boolean Clockwise · GreaterThan180 · Point2d CenterPoint
AlignmentSubEntity: SubEntityType · StartStation · EndStation · Length · Point2d StartPoint · EndPoint
```
`siteName = ""` → siteless? `[chưa xác minh]` (spike W2). Indexer `Item` chữ ký (int/string) `[chưa xác minh]` — dùng `GetEntityByOrder`.

## 5. Profile
```
Profile : Feature — Double ElevationAt(Double station) · StartingStation · EndingStation · Length · ElevationMin · ElevationMax · ObjectId AlignmentId · ProfileType ProfileType · ProfileEntityCollection Entities · ProfilePVICollection PVIs
  static ObjectId CreateByLayout(String profileName, CivilDocument, String alignmentName, String layerName, String styleName, String labelSetName)
  static ObjectId CreateFromSurface(String profileName, CivilDocument, String alignmentName, String surfaceName, String layerName, String styleName, String labelSetName)
```
`Profile` kế thừa `Feature` (không phải `Entity` trực tiếp) — `Name` qua `Feature`? `[chưa xác minh]` → probe phase 1 (`Autodesk.Civil.DatabaseServices.Feature` chưa dump).

## 6. Surface
```
Surface : Entity — Double FindElevationAtXY(Double x, Double y) · GeneralSurfaceProperties GetGeneralProperties() · Boolean IsOutOfDate · Boolean AutoRebuild {get;set;} · Void Rebuild() · RebuildSnapshot() · ExportToDEM(...)
TinSurface : Surface — static ObjectId Create(Database, String surfaceName) · Create(String surfaceName, ObjectId styleId) · CreateFromLandXML(...) · CreateFromTin(Database, String tinFileName) · CreateFromIMX(...)
                       SurfaceOperationAddTinVertex AddVertex(Point3d) · SurfaceOperationAddTinMultipleVertices AddVertices(Point3dCollection) · TinSurfaceTriangleCollection GetTriangles(Boolean) · Point3dCollection SampleElevations(Point3d, Point3d) · TerrainSurfaceProperties GetTerrainProperties()
GeneralSurfaceProperties: MinimumElevation · MaximumElevation · MeanElevation · NumberOfPoints · MinimumCoordinateX/Y · MaximumCoordinateX/Y
TerrainSurfaceProperties: SurfaceArea2D · SurfaceArea3D · Min/Mean/MaximumGradeOrSlope
```
Exception khi XY ngoài biên: kiểu `[chưa xác minh]` (ứng viên `Autodesk.Civil.SurfaceException`/`PointNotOnEntityException`) → spike S-07.

## 7. Corridor
```
CorridorCollection : CivilWrapper<AcDbDatabase> — Int32 Count · ObjectId Item[...] · Void RebuildAll() · ObjectId Add(String corridorName[, String baselineName, ObjectId alignmentId, ObjectId profileId[, String regionName, ObjectId assemblyId]])
Corridor : Entity — BaselineCollection Baselines · CorridorSurfaceCollection CorridorSurfaces · Boolean IsOutOfDate · Boolean RebuildAutomatic {get;set;} · Void Rebuild() · CodeSetStyleName · CorridorRegionLockType RegionLockMode
Baseline : BaseBaseline — String Name · ObjectId AlignmentId · ProfileId · BaselineRegionCollection BaselineRegions · Void UpdateStation(Double)
CorridorSurface : CivilWrapper<AeccDbCorridor> — String Name · Description · ObjectId SurfaceId · SurfaceStyleId
```
Hành vi `Rebuild()` dưới `Abort()` `[chưa xác minh]` (KB Autodesk "corridor disappears when rebuilt") → spike S-05; MVP guard deny `Rebuild`/`RebuildAll`/`RebuildSnapshot` (ADR-04).

## 8. Pipe network
```
Network : GeoEntity — static ObjectId Create(CivilDocument, ref String networkName) · ObjectIdCollection GetPipeIds() · GetStructureIds() · ObjectId ReferenceAlignmentId · ReferenceSurfaceId · String ReferenceAlignmentName · ReferenceSurfaceName · PartsListName · Void AddLinePipe(ObjectId pipeFamilyId, ObjectId pipeSizeId, LineSegment3d, ref ObjectId newPipeId, Boolean applyRules) · AddStructure(...)
Part : GeoEntity — String Name · PartFamilyName · PartSizeName · PartDescription · PartSubType · NetworkName · RefAlignmentName · RefSurfaceName · PartType PartType · DomainType Domain · ObjectId NetworkId · Point3d Position
Pipe : Part — Point3d StartPoint · EndPoint · Double Slope · Length2D · Length3D · Length2DCenterToCenter · InnerDiameterOrWidth · OuterDiameterOrWidth · InnerHeight · OuterHeight · Radius · Bearing · MinimumCover · MaximumCover · StartOffset · EndOffset · ObjectId StartStructureId · EndStructureId · FlowDirectionType FlowDirection · SweptShapeType CrossSectionalShape
Structure : Part — Double RimElevation · SumpElevation · SumpDepth · Height · InnerDiameterOrWidth · Int32 ConnectedPipesCount · Boolean IsConnectedPipeFlowingIn/Out(Int32)
PressurePipeNetwork : GeoEntity (AeccPressurePipesMgd) — static Create(Database, String) · GetPipeIds() · GetFittingIds() · GetAppurtenanceIds() · MinimumElevation · MaximumElevation
```

## 9. Site / Parcel / COGO
```
Site : Entity — ObjectIdCollection GetParcelIds() · GetAlignmentIds() · GetFeatureLineIds()
Parcel : Entity — Int32 Number · TaxId · String Address · Point3d Centroid · AreaLocation · (KHÔNG Area/Perimeter — xem §0)
CogoPointCollection (System.Object): UInt32 Count · Boolean Contains(UInt32) · ObjectId GetPointByPointNumber(UInt32)
   ObjectId Add(Point3d location, Boolean useNextPointNumSetting) · Add(Point3d, String desc, Boolean useNextPointNumSetting) · Add(Point3d, String desc, Boolean useDescriptionKey, Boolean matchOnParams, Boolean useNextPointNumSetting)
   ObjectIdCollection Add(Point3dCollection locations, String desc, Boolean useNextPointNumSetting) · SetElevation/SetPointNumber/SetRawDescription/SetPointName(ObjectId|IEnumerable<ObjectId>, …) · Void Remove(ObjectId) · Remove(UInt32)
   (duyệt: KHÔNG có enumerator/indexer → dùng CivilDocument.GetAllPointIds())
CogoPoint : Autodesk.AutoCAD.DatabaseServices.Entity — UInt32 PointNumber {get;set;} · String PointName · RawDescription · FullDescription · Double Easting · Northing · Elevation · Point3d Location · ObjectId PrimaryPointGroupId · Boolean IsProjectPoint · UInt32 Renumber(UInt32[, PointNumberResolveType])
PointGroupCollection: Int32 Count · ObjectId Add(String name) · Boolean Contains(String|ObjectId)
```

## 10. Styles (seed ghi phải kiểm style tồn tại trước)
```
StylesRoot: AlignmentStyles · ProfileStyles · SurfaceStyles · PointStyles · ParcelStyles · PipeStyles · StructureStyles · CorridorStyles · CodeSetStyles · LabelStylesRoot LabelStyles · LabelSetStylesRoot LabelSetStyles
TreeNodeCollectionBase (cha của mọi *StyleCollection): Int32 Count · IEnumerator<ObjectId> GetEnumerator() · Boolean Contains(String name) · ObjectId Item[...] · ObjectIdCollection ToObjectIds() · ObjectId Add(String name)
StyleBase : DBObject — String Name {set;} (get qua DBObject? `[chưa xác minh]`; dùng Contains(name) để kiểm) · static ExportTo(...)
```
`LabelSetStylesRoot.AlignmentLabelSetStyles` `[chưa xác minh]` tên member → probe phase 1.

## 11. Data shortcuts / survey / UI (→ guard deny, ADR-04)
`Autodesk.Civil.DataShortcuts.DataShortcuts` (static): `GetWorkingFolder` · **`SetWorkingFolder(String)`** · `GetCurrentProjectFolder` · **`SetCurrentProjectFolder(String)`** · **`CreateProjectFolder(...)`** · **`AssociateDSProject(...)`** · **`CreateReference(...)`** · **`CreatePartialReferenceSurface`** · **`RepairBrokenDRef`** · **`CreateDataShortcutManager(ref Boolean)`** · **`SaveDataShortcutManager`** · `Validate()` · `GetDSProjectId`. `Autodesk.Civil.SurveyProject`, `SurveyProjectCollection` (38 type `Survey*`). Exceptions: `Autodesk.Civil.CivilException`, `EntityNotFoundException`, `PointNotOnEntityException`, `SurfaceException`, `SurveyException`, `PointGroupQuery*Exception`.

## 12. Member `[chưa xác minh]` → spike phase 1 — kết quả 2026-09-18 trong [reports/phase-01-spike.md](../reports/phase-01-spike.md)
| # | Member / hành vi | Spike |
|---|---|---|
| U1 | ~~`CivilApplication.ActiveDocument` khi DWG không phải Civil~~ **đóng 2026-09-18 (S-04a/b):** trả `CivilDocument` cho mọi DWG trong Civil 3D, kể cả `acad.dwt` (`DrawingUnits Feet` mặc định); AutoCAD thuần không nạp bundle nên không có case | S-04 |
| U2 | ~~siteless / labelSetName rỗng~~ **đóng (W2):** `siteName ""` = siteless OK; `labelSetName ""` → `ArgumentException: Label set name must be at least one character` → lấy `Styles.LabelSetStyles.AlignmentLabelSetStyles[0]` | W2 |
| U3 | ~~ngoài biên ném kiểu gì~~ **đóng (S-07):** `Autodesk.Civil.PointNotOnEntityException` "Point Outside Surface." | S-07 |
| U4 | ~~`Corridor.Rebuild()` dưới `outer.Abort()`~~ **đóng (S-10b/c):** hoàn lại sạch, corridor + 4 corridor surface còn, `U` cũng hoàn lại; `TinSurface.AddVertex` hoàn lại (S-10a ×2). `Surface.Rebuild()` chưa gọi trực tiếp | S-10 |
| U5 | ~~`Parcel` diện tích~~ **đóng 2026-09-18:** `Parcel.Area` có (runtime reflection); chu vi không thấy | S-09 |
| U6 | `Profile.Name` (qua `Feature`) — `inspect_type Feature` 200 member (có `Name`); chưa đọc live trên Profile → phase 3 | S-08 |
| U7 | ~~indexer int vs string~~ **đóng (S-08):** `AlignmentEntityCollection[int]`, `AlignmentEntity[int]` (sub-entity), `CorridorCollection[int]` (+ `GetEnumerator`); `TreeNodeCollectionBase` 17 member có `Contains(string)` | S-08 |
| U8 | ~~`StyleBase.Name`; label set root~~ **đóng (S-08):** `Name` qua `DBObject` ("Local Road"); `civil.Styles.LabelSetStyles.AlignmentLabelSetStyles` (`LabelSetStylesRoot` 9 member) | S-08 |
| U9 | ~~`Add` với số điểm trùng~~ **đóng (W1):** `Add(pt, desc, useNextPointNumSetting: true)` cấp số kế tiếp (9, 10, 11), không trùng | W1 |
| U10 | ~~drawing unit~~ **đóng (S-05/07/09):** station 1 399.81 (Meters), elevation 65.21, `Parcel.Area` 3 349 m² — đúng drawing unit; Pipe field chưa đọc live (phase 3/4) | S-06 |
| U11 | ~~`GetCoordinateSystemByCode("")`~~ **đóng (S-06 ×2):** không zone = code `"."` (→ "No Datum, No Projection"); `""` ném `ArgumentException`; có zone `NH83F` → "NAD83 New Hampshire State Planes, US Foot" | S-06 |
