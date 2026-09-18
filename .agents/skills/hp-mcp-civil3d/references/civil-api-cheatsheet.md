# Civil 3D 2026 .NET API — members verified on the dev machine

Every member below was executed live in Civil 3D 2026 (R25.1, `AeccDbMgd` 13.8) by the spike, the seeds or the live-verify harness (`plans/260917-1633-civil3d-mcp-2026/reports/phase-01-spike.md`, `phase-04-live-verify.md`). Anything else: confirm with `inspect_type` first (`typeName` = `Alignment`, `Autodesk.Civil.DatabaseServices.TinSurface`, `CogoPoint`, `Corridor`, `Pipe`, `Structure`, `Parcel`, `Profile`, `PointGroup`; `memberFilter` narrows). Units: API lengths/stations/elevations are in the **drawing unit**; convert with `units.ToMm` / `units.ToDrawing`.

## `CivilDocument` (`civil`)

| Member | Returns | Notes |
|---|---|---|
| `GetAlignmentIds()` | `ObjectIdCollection` | every alignment (sited + siteless) |
| `GetSitelessAlignmentIds()` | `ObjectIdCollection` | |
| `GetSurfaceIds()` | `ObjectIdCollection` | TIN + grid + volume surfaces — cast to `Autodesk.Civil.DatabaseServices.Surface`, check `is TinSurface` |
| `GetSiteIds()` | `ObjectIdCollection` | `Site.GetParcelIds()`, `Site.Name` |
| `GetPipeNetworkIds()` | `ObjectIdCollection` | gravity networks (`Network`) |
| `Autodesk.Civil.ApplicationServices.CivilDocumentPressurePipesExtension.GetPressurePipeNetworkIds(civil)` | `ObjectIdCollection` | pressure networks — static extension, write it in full (`PressurePipeNetwork.Name`, `GetPipeIds()`, `GetFittingIds()`, `GetAppurtenanceIds()`) |
| `CorridorCollection` | `CorridorCollection` | `foreach (ObjectId id in civil.CorridorCollection)`; `.Count`; **`RebuildAll()` is guard-denied** |
| `CogoPoints` | `CogoPointCollection` | `foreach (ObjectId id in civil.CogoPoints)`, `.Count`, `Add(Point3d, string description, bool useNextPointNumber)`, `SetPointName(id, name)` |
| `GetAllPointIds()` | `ObjectIdCollection` | all COGO points (the collection is also enumerable) |
| `PointGroups` | `PointGroupCollection` | `foreach (ObjectId id …)` → `PointGroup.Name`; a COGO point's `PrimaryPointGroupId` |
| `Settings.DrawingSettings.UnitZoneSettings` | | `.DrawingUnits` (`DrawingUnitType.Meters` \| `Feet` — nothing else), `.CoordinateSystemCode` (`"."` = no zone) |
| `Styles.LabelSetStyles.AlignmentLabelSetStyles` | `StyleCollectionBase` | enumerate for names; `Alignment.Create` needs a **non-empty** label set name |
| `Styles.AlignmentStyles` | | first entry = the template's default |

## `Alignment`

| Member | Notes |
|---|---|
| `Name`, `Description`, `StyleName`, `AlignmentType`, `IsSiteless`, `SiteName` | |
| `Length`, `StartingStation`, `EndingStation` | drawing units |
| `GetStationStringWithEquations(double station)` | formatted label `0+000.00` / `12+34.56` honouring equations |
| `StationOffset(double easting, double northing, ref double station, ref double offset)` | plan → station/offset; round trip = 0 |
| `PointLocation(double station, double offset, ref double easting, ref double northing)` | station → plan |
| `Entities` (`AlignmentEntityCollection`) | `.Count`, `[int i]` indexer or `foreach`; `AlignmentEntity.EntityType` (`AlignmentEntityType.Line/Arc/Spiral/…`), `.EntityId`, `.SubEntityCount`, `[int]` → `AlignmentSubEntity` |
| `AlignmentLine`/`AlignmentArc` (cast the entity) | `StartPoint`, `EndPoint` (`Point2d`), `StartStation`, `EndStation`, `Length`; arc adds `Radius`, `CenterPoint`, `Direction`, `Delta`, `PIStation` |
| `GetProfileIds()` | `ObjectIdCollection` → `Profile.Name`, `.StyleName`, `.ProfileType`, `.Length`, `.StartingStation`, `.EndingStation`, `.ElevationMin/Max`, `.PVIs` (`.Count`, `Station`, `Elevation`) |
| `static Alignment.Create(CivilDocument civil, PolylineOptions options, string name, string site, string layer, string styleName, string labelSetName)` | `PolylineOptions { PlineId, AddCurvesBetweenTangents, EraseExistingEntities }`; `site` `""` = siteless; `labelSetName` **must** exist; duplicate name throws |

## Surfaces

| Member | Notes |
|---|---|
| `Autodesk.Civil.DatabaseServices.Surface` | `Name`, `Description`, `StyleName`, `IsOutOfDate`, `AutoRebuild`; `GetGeneralProperties()` (`MinimumElevation`, `MaximumElevation`, `NumberOfPoints`…) |
| `TinSurface` | `GetTerrainProperties()` (`SurfaceArea2D`, `SurfaceArea3D`, `MinElevation`, `MaxElevation`); `AddVertex(Point3d)` (rolls back under dryRun) |
| `Surface.FindElevationAtXY(double x, double y)` | drawing units; outside → `Autodesk.Civil.PointNotOnEntityException` "Point Outside Surface." — catch it per point |
| `Rebuild()` / `RebuildSnapshot()` | **guard-denied** |

## Corridors

| Member | Notes |
|---|---|
| `Corridor` | `Name`, `IsOutOfDate`, `RebuildAutomatic` (property read is fine), `CodeSetStyleName` |
| `Baselines` | `foreach (Baseline b …)`: `Name`, `AlignmentId`, `ProfileId`, `BaselineRegions` (`.Count`, region `Name`, `StartStation`, `EndStation`) |
| `CorridorSurfaces` | `foreach (CorridorSurface s …)`: `Name`, `SurfaceId` |
| `Rebuild()` | **guard-denied** — 13–43 ms on the tutorial corridor, unmeasured on a real one; rolls back under `Abort()`/`U` |

## Pipe networks (gravity)

| Member | Notes |
|---|---|
| `Network` | `Name`, `PartsListName`, `ReferenceAlignmentName`, `ReferenceSurfaceName`, `GetPipeIds()`, `GetStructureIds()` |
| `Pipe` | `Name`, `PartFamilyName`, `PartSizeName`, `StartPoint`/`EndPoint` (`Point3d`, z = invert elevation), `Slope`, `Length2D`/`Length3D` (or `Length`), `InnerDiameterOrWidth`, `InnerHeight`, `CrossSectionalShape`, `FlowDirection`, `StartStructureId`/`EndStructureId`, `MinimumCover` |
| `Structure` | `Name`, `PartFamilyName`, `PartSizeName`, `Position` (`Point3d`), `RimElevation`, `SumpElevation`, `SumpDepth`, `Height`, `InnerDiameterOrWidth`, `ConnectedPipesCount` |

## Parcels and sites

| Member | Notes |
|---|---|
| `Site` | `Name`, `GetParcelIds()` |
| `Parcel` | `Name`, `Number`, `TaxId`, `Address`, `Description`, `StyleName`, `Area` (**exists**, inherited getter — drawing units squared), `Centroid` (`Point3d`) |

## COGO points

| Member | Notes |
|---|---|
| `CogoPoint` | `PointNumber` (`uint`), `PointName`, `Easting`, `Northing`, `Elevation`, `RawDescription`, `FullDescription`, `PrimaryPointGroupId` |
| `CogoPointCollection.Add(Point3d, string rawDescription, bool useNextPointNumber)` | returns the `ObjectId`; the next-point-number setting numbers it; rolls back under dryRun/`U` |
| `CogoPointCollection.SetPointName(ObjectId, string)` | duplicate names throw |
| `CogoPointCollection.GetPointByPointNumber(uint)` | direct lookup — `list_cogo_points` uses it for a number range ≤ 5 000 instead of scanning |
| `Renumber` | *[unverified]* documented on `CogoPoint`, not on the collection — `inspect_type CogoPoint memberFilter Renumber` before using |

## Handles and ids

`Convert.ToInt64(handle, 16)` + `db.GetObjectId(false, new Handle(value), 0)` resolves a handle string; `id.Handle.ToString()` the other way. `tr.GetObject(id, OpenMode.ForRead)` for every read; `ForWrite` only under `transaction: auto`.

## Namespaces that bite

- `Autodesk.Civil.DatabaseServices` has its own `Entity`, `DBObject`, `Surface` (and `TreeNodeCollectionBase`) — write those three in full, or alias them; the AutoCAD ones stay unqualified.
- `Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument` is the `civil` global — never null inside Civil 3D.
- Styles: `StyleBase.Name` comes from `DBObject.Name`; the serializer renders any style by name.
- `Autodesk.Civil.DataShortcuts.*`, `Autodesk.Civil.AeccUiMgd.*`, `Autodesk.AECC.Interop.*` are guard-denied.
