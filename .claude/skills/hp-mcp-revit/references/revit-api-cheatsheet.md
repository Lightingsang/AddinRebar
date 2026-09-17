# Revit API cheatsheet — the idioms the seeds use (Revit 2025/2026, .NET 8)

Everything below compiles against the bridge's default usings (`Autodesk.Revit.DB`, `.UI`, `.DB.Structure`). Unsure about a member? `inspect_type {typeName: "Wall", memberFilter: "Create"}` reflects over the RevitAPI.dll that is actually running.

## Ids, elements, types

```csharp
var e = doc.GetElement(new ElementId(args.Long("elementId")));      // null when it does not exist → ArgumentException
long id = e.Id.Value;                                               // long since Revit 2024 (IntegerValue is gone)
var type = doc.GetElement(e.GetTypeId());                           // ElementType (FamilySymbol, WallType, …)
var byUnique = doc.GetElement(args.Require("uniqueId"));            // string UniqueId also works
ElementId.InvalidElementId                                           // -1: "no element"
```

## Collectors (never materialise halfway)

```csharp
new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().Cast<Wall>()
new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance))
new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Rooms)          // visible in one view
new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralColumns)
new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation)
new FilteredElementCollector(doc).WherePasses(new ElementLevelFilter(level.Id))                          // instances on a level
new FilteredElementCollector(doc).WherePasses(new BoundingBoxIntersectsFilter(new Outline(minFt, maxFt)))
new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)) / typeof(View) / typeof(ViewSchedule)
```

Category by name: `Enum.TryParse<BuiltInCategory>("OST_Walls", out var bic)` or `doc.Settings.Categories.Cast<Category>().First(c => c.Name == "Walls")`. `ai_element_filter` already accepts both.

## Units (API = feet, radians)

```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
UnitUtils.ConvertFromInternalUnits(area, UnitTypeId.SquareMeters);  UnitTypeId.CubicMeters;  UnitTypeId.Degrees
double rad = args.Double("rotationDeg") * Math.PI / 180;
```

`DisplayUnitType` is gone (2022+) — always `UnitTypeId` / `SpecTypeId`. Display unit of the project: `doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId()`.

## Parameters

```csharp
var p = e.LookupParameter("Comments");                               // instance, by display name (locale-dependent)
var q = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);            // built-in, locale-independent — prefer
var tp = doc.GetElement(e.GetTypeId())?.LookupParameter("Width");    // type parameter
var sp = e.get_Parameter(new Guid("…"));                             // shared parameter by GUID
p.StorageType   // String | Double (feet!) | Integer (int, bool as 0/1, enum) | ElementId
p.AsString(); p.AsDouble(); p.AsInteger(); p.AsElementId(); p.AsValueString()   // AsValueString = what the UI shows, with unit
if (!p.IsReadOnly) p.Set("text"); p.Set(Ft(150)); p.Set(1); p.Set(otherId);       // Set(double) takes feet
```

Common built-ins: `ALL_MODEL_MARK`, `ALL_MODEL_INSTANCE_COMMENTS`, `ALL_MODEL_TYPE_COMMENTS`, `ELEM_FAMILY_AND_TYPE_PARAM`, `CURVE_ELEM_LENGTH`, `WALL_USER_HEIGHT_PARAM`, `WALL_BASE_CONSTRAINT`, `WALL_BASE_OFFSET`, `HOST_AREA_COMPUTED`, `HOST_VOLUME_COMPUTED`, `LEVEL_PARAM`, `ROOM_NAME`, `ROOM_NUMBER`, `ROOM_AREA`, `SHEET_NUMBER`, `SHEET_NAME`, `VIEW_NAME`.

## Location, geometry, bounds

```csharp
var bb = e.get_BoundingBox(null);              // model bbox (feet); e.get_BoundingBox(view) for the view
var curve = (e.Location as LocationCurve)?.Curve;      // walls, beams, MEP curves — GetEndPoint(0/1), Length
var point = (e.Location as LocationPoint)?.Point;      // columns, doors, furniture; .Rotation in radians
var geo = e.get_Geometry(new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine });
foreach (var obj in geo) { if (obj is Solid s && s.Volume > 0) { /* s.Faces, s.Edges */ } else if (obj is GeometryInstance gi) { /* gi.GetInstanceGeometry() */ } }
```

## Creating (every call below is inside the bridge's transaction under `auto`)

```csharp
var level = Level.Create(doc, Ft(3500)); level.Name = "Level 2";
var grid  = Grid.Create(doc, Line.CreateBound(p0, p1)); grid.Name = "A";
var wall  = Wall.Create(doc, Line.CreateBound(p0, p1), wallType.Id, level.Id, Ft(3000), 0, false, structural: false);
if (!symbol.IsActive) symbol.Activate();                                                        // once per FamilySymbol
var col   = doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.Column);              // point-based
var door  = doc.Create.NewFamilyInstance(ptOnWall, symbol, hostWall, level, StructuralType.NonStructural);
var beam  = doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), symbol, level, StructuralType.Beam);
var floor = Floor.Create(doc, new List<CurveLoop> { loop }, floorType.Id, level.Id, structural: true, null, 0);   // 2022+
var ceil  = Ceiling.Create(doc, new List<CurveLoop> { loop }, ceilingType.Id, level.Id);
var roof  = doc.Create.NewFootPrintRoof(curveArray, level, roofType, out ModelCurveArray _);
var room  = doc.Create.NewRoom(level, new UV(xFt, yFt));   room.Name = "Office"; room.Number = "101";  // room.Area > 0 = enclosed
var rtag  = doc.Create.NewRoomTag(new LinkElementId(room.Id), new UV(xFt, yFt), view.Id);
var tag   = IndependentTag.Create(doc, tagSymbol.Id, view.Id, new Reference(wall), addLeader: false, TagOrientation.Horizontal, midPt);
var dim   = doc.Create.NewDimension(view, Line.CreateBound(a, b), referenceArray);                // refs: faces/datums along the line
var bs    = BeamSystem.Create(doc, profileCurves, level, directionIndex, is3D: false);
var deleted = doc.Delete(ids);                                                                    // returns ids incl. dependents
```

`CurveLoop.Create(new List<Curve> { … })` for floor/ceiling loops (closed, planar, non-self-intersecting). Points for room/tag `UV` are feet too.

## Views and overrides

```csharp
var view = doc.ActiveView;   view.ViewType   // FloorPlan, Section, ThreeD, Schedule, DrawingSheet…
var ogs = new OverrideGraphicSettings(); ogs.SetProjectionLineColor(new Color(255, 0, 0)); ogs.SetSurfaceForegroundPatternColor(…);
var solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().First(p => p.GetFillPattern().IsSolidFill);
ogs.SetSurfaceForegroundPatternId(solid.Id); ogs.SetSurfaceForegroundPatternVisible(true); ogs.SetSurfaceTransparency(50);
view.SetElementOverrides(id, ogs);   view.SetElementOverrides(id, new OverrideGraphicSettings());   // clear one
view.HideElements(ids); view.UnhideElements(ids);                       // permanent (element must be hideable: e.CanBeHidden(view))
view.HideElementsTemporary(ids); view.IsolateElementsTemporary(ids); view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
uidoc.Selection.SetElementIds(ids);   uidoc.Selection.GetElementIds();
uidoc.ShowElements(ids);              // zoom to — UI call, fine from the API thread
```

`operate_element` runs every action — Select included — under `auto`; do the same from a script for anything that touches a view (overrides, hide/unhide, temporary modes): the extra empty transaction costs nothing, a missing one fails the run. Only `uidoc.Selection.*` and `ShowElements` are pure UI calls; selection and temporary modes are not model changes, so do not expect them in `changed` — read the seed's `value` instead.

## Rooms, sheets, structure

```csharp
using Autodesk.Revit.DB.Architecture;   // Room, RoomTag — not in the default usings
var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().Cast<Room>()
    .Where(r => r.Location != null && r.Area > 0);            // placed + enclosed
r.GetBoundarySegments(new SpatialElementBoundaryOptions())    // loops of BoundarySegment (Curve, ElementId of the wall)
var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>();  sheet.SheetNumber; sheet.GetAllPlacedViews()
// Structure (Autodesk.Revit.DB.Structure): StructuralType.Beam|Column|Footing; RebarBarType, RebarShape, RebarHostData.GetRebarHostData(host)
// The rebar features of the HPRebar add-in use Rebar.CreateFromRebarShape — stable on every version; avoid RebarHookOrientation / Curve.Intersect(Curve, out …) (removed in 2027)
```

## Version notes (bridge = R25/R26; scripts compile against the running Revit)

- `ElementId.Value` (long) — `IntegerValue` does not exist here.
- `UnitTypeId`/`SpecTypeId`/`ForgeTypeId` everywhere; no `DisplayUnitType`, no `UnitType`.
- `Floor.Create` / `Ceiling.Create` with `CurveLoop`s (not `doc.Create.NewFloor`).
- Revit 2025+ runs .NET 8: `Math.Clamp`, `is not`, target-typed `new()`, `record` are fine in scripts; top-level `using var` is **not** (Roslyn script) — use a block `using (var t = …) { }`.
- Worksharing: on a central model an element may be owned by another user → `Set` throws; report the id, do not retry.
- A transaction that produced Revit **warnings** still commits (the bridge dismisses them); a Revit **error** rolls the run back with "Revit rejected the change".
