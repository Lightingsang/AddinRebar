# API Surface Check — R01_ColumnsRebar vs Revit 2023 / 2026 / 2027

Generated: 2026-09-03 (cook Phase 1, step 6)

**Method:** every PascalCase token in the 78 `.cs` files of the source tool, intersected with the `T:` type list of
`RevitAPI.xml` + `RevitAPIUI.xml` from packages `Nice3point.Revit.Api.RevitAPI` 2023.1.90 / 2026.4.10 / 2027.2.0 and
`Nice3point.Revit.Api.RevitAPIUI` 2023.1.90 / 2026.4.10 / 2027.2.0.

**Result: 106 Revit API types referenced. 0 of them missing in any of the three versions. Zero member-name-level differences among used members.**

> **This result is weaker than it looks.** Phase 4 found a genuine breaking change the sweep could not
> detect — see the `Rebar.CreateFreeForm` correction below. Treat the table as "nothing was renamed or
> deleted", not as "everything still compiles".

## Targeted checks (the risks the plan called out)

| Symbol | R23 | R26 | R27 | Verdict |
|---|---|---|---|---|
| `Structure.RebarHookOrientation` (type) | present | present | **REMOVED** | Plan claim confirmed. Source **does not use it** (grep: 0 hit) — no action needed |
| `Rebar.CreateFromCurves` | 4 overloads | 6 | **2** | Overload set churned. Decision D3 (do not use) confirmed correct |
| `Rebar.CreateFreeForm` | 3 overloads | 5 | 3 | Present by name in all three — **but the overload set is not compatible, see below** |
| `Rebar.CreateFromRebarShape` | 1 | 1 | 1 | Present in all three |
| `RebarShapeDrivenAccessor.ScaleToBox(XYZ,XYZ,XYZ)` | present | present | present | Identical signature |
| `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing(int,double,bool,bool,bool)` | present | present | present | Identical signature |
| `RebarFreeFormAccessor.SetLayoutAsNumberWithSpacing(int,double)` | present | present | present | Identical signature |

> Correction to the plan's Validation Log: `ScaleToBox` and `SetLayoutAsNumberWithSpacing` are **not** members of `Rebar`.
> They live on `RebarShapeDrivenAccessor` / `RebarFreeFormAccessor`, reached via `rebar.GetShapeDrivenAccessor()` /
> `rebar.GetFreeFormAccessor()`. The methods exist and are stable — only the owning type was misattributed.

## Correction — `Rebar.CreateFreeForm` (added during Phase 4)

The overload-count column above was not enough. Comparing the full signatures shows the overload set is
**not** compatible across the range, which the name-level sweep could not see:

| Overload | R23 | R24 | R25 | R26 | R27 |
|---|---|---|---|---|---|
| `(Document, RebarBarType, Element, IList<CurveLoop>, out RebarFreeFormValidationResult)` | yes | yes | yes | deprecated | **removed** |
| `(Document, RebarBarType, Element, IList<CurveLoop>, RebarStyle)` | no | no | no | **added** | yes |

The two also differ in return type: the older one returns `Rebar`, the newer returns
`RebarFreeFormCreationResult`, whose `Rebar` and `Error` properties carry what the `out` parameter used to.
`RebarFreeFormValidationResult` itself still exists in R27 as the type of `Error`.

There is therefore no single call that compiles across R23 to R27, and `MainBarCreator` guards it with
`#if REVIT2026_OR_GREATER`. `RebarStyle.Standard` and `RebarStyle.StirrupTie` exist in every version, so the
enum argument itself needs no guard.

`Rebar.CreateFromRebarShape(Document, RebarShape, RebarBarType, Element, XYZ, XYZ, XYZ)` was re-checked at
full signature level and **is** identical in R23, R26 and R27.

## Caveats

- Type-level and member-**name**-level only. An overload signature change on a member that still exists by name is not
  detected by this sweep — that is why `CreateFromCurves` needed the targeted check above.
- Only members documented in the XML are visible. An undocumented public member reads as absent.
- `Reference.ParseFromStableRepresentation` (the `SURFACE->LINEAR` dimension hack, plan risk 4) exists in all three
  versions, but its *behaviour* is undocumented and cannot be verified statically — Phase 5 still wraps it in try/catch.
- Token-intersection can pick up a name collision (a local class sharing a Revit type name). It over-reports, never
  under-reports, so it is safe for a "nothing was removed" conclusion.

## Full type list

| Type | R23 | R26 | R27 |
|---|---|---|---|
| `Autodesk.Revit.ApplicationServices.Application` | OK | OK | OK |
| `Autodesk.Revit.DB.Area` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.AreaReinforcement` | OK | OK | OK |
| `Autodesk.Revit.DB.BoundingBoxIntersectsFilter` | OK | OK | OK |
| `Autodesk.Revit.DB.BoundingBoxXYZ` | OK | OK | OK |
| `Autodesk.Revit.DB.BuiltInCategory` | OK | OK | OK |
| `Autodesk.Revit.DB.BuiltInParameter` | OK | OK | OK |
| `Autodesk.Revit.DB.Category` | OK | OK | OK |
| `Autodesk.Revit.DB.CompoundStructure` | OK | OK | OK |
| `Autodesk.Revit.DB.Curve` | OK | OK | OK |
| `Autodesk.Revit.DB.CurveLoop` | OK | OK | OK |
| `Autodesk.Revit.DB.CylindricalFace` | OK | OK | OK |
| `Autodesk.Revit.DB.CylindricalSurface` | OK | OK | OK |
| `Autodesk.Revit.DB.Definition` | OK | OK | OK |
| `Autodesk.Revit.DB.DetailCurve` | OK | OK | OK |
| `Autodesk.Revit.DB.Dimension` | OK | OK | OK |
| `Autodesk.Revit.DB.DimensionType` | OK | OK | OK |
| `Autodesk.Revit.Creation.Document` | OK | OK | OK |
| `Autodesk.Revit.DB.Element` | OK | OK | OK |
| `Autodesk.Revit.DB.ElementCategoryFilter` | OK | OK | OK |
| `Autodesk.Revit.DB.ElementId` | OK | OK | OK |
| `Autodesk.Revit.DB.ElementSet` | OK | OK | OK |
| `Autodesk.Revit.DB.ElementTransformUtils` | OK | OK | OK |
| `Autodesk.Revit.DB.ElementType` | OK | OK | OK |
| `Autodesk.Revit.DB.Ellipse` | OK | OK | OK |
| `Autodesk.Revit.UI.ExternalCommandData` | OK | OK | OK |
| `Autodesk.Revit.DB.Face` | OK | OK | OK |
| `Autodesk.Revit.DB.FaceArray` | OK | OK | OK |
| `Autodesk.Revit.DB.Family` | OK | OK | OK |
| `Autodesk.Revit.DB.FamilyInstance` | OK | OK | OK |
| `Autodesk.Revit.DB.FamilySymbol` | OK | OK | OK |
| `Autodesk.Revit.DB.FamilyType` | OK | OK | OK |
| `Autodesk.Revit.DB.FilteredElementCollector` | OK | OK | OK |
| `Autodesk.Revit.DB.Floor` | OK | OK | OK |
| `Autodesk.Revit.DB.ForgeTypeId` | OK | OK | OK |
| `Autodesk.Revit.DB.FormatOptions` | OK | OK | OK |
| `Autodesk.Revit.DB.Visual.Generic` | OK | OK | OK |
| `Autodesk.Revit.DB.GeometryElement` | OK | OK | OK |
| `Autodesk.Revit.DB.GeometryInstance` | OK | OK | OK |
| `Autodesk.Revit.DB.GeometryObject` | OK | OK | OK |
| `Autodesk.Revit.UI.IExternalCommand` | OK | OK | OK |
| `Autodesk.Revit.UI.Selection.ISelectionFilter` | OK | OK | OK |
| `Autodesk.Revit.DB.ImageType` | OK | OK | OK |
| `Autodesk.Revit.DB.ImageTypeOptions` | OK | OK | OK |
| `Autodesk.Revit.DB.ImageTypeSource` | OK | OK | OK |
| `Autodesk.Revit.DB.IndependentTag` | OK | OK | OK |
| `Autodesk.Revit.DB.JoinGeometryUtils` | OK | OK | OK |
| `Autodesk.Revit.DB.Level` | OK | OK | OK |
| `Autodesk.Revit.DB.Line` | OK | OK | OK |
| `Autodesk.Revit.DB.Location` | OK | OK | OK |
| `Autodesk.Revit.DB.LocationCurve` | OK | OK | OK |
| `Autodesk.Revit.DB.LocationPoint` | OK | OK | OK |
| `Autodesk.Revit.DB.LogicalAndFilter` | OK | OK | OK |
| `Autodesk.Revit.DB.MultiReferenceAnnotation` | OK | OK | OK |
| `Autodesk.Revit.DB.MultiReferenceAnnotationOptions` | OK | OK | OK |
| `Autodesk.Revit.DB.MultiReferenceAnnotationType` | OK | OK | OK |
| `Autodesk.Revit.UI.Selection.ObjectType` | OK | OK | OK |
| `Autodesk.Revit.DB.Options` | OK | OK | OK |
| `Autodesk.Revit.DB.Outline` | OK | OK | OK |
| `Autodesk.Revit.DB.Parameter` | OK | OK | OK |
| `Autodesk.Revit.DB.PlanarFace` | OK | OK | OK |
| `Autodesk.Revit.DB.Point` | OK | OK | OK |
| `Autodesk.Revit.DB.PointClouds.PointCollection` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.Rebar` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarBarType` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarCoverType` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarFreeFormValidationResult` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarHookType` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarShape` | OK | OK | OK |
| `Autodesk.Revit.DB.Structure.RebarShapeDrivenAccessor` | OK | OK | OK |
| `Autodesk.Revit.DB.Rectangle` | OK | OK | OK |
| `Autodesk.Revit.DB.Reference` | OK | OK | OK |
| `Autodesk.Revit.DB.ReferenceArray` | OK | OK | OK |
| `Autodesk.Revit.UI.Result` | OK | OK | OK |
| `Autodesk.Revit.DB.SchedulableField` | OK | OK | OK |
| `Autodesk.Revit.DB.ScheduleDefinition` | OK | OK | OK |
| `Autodesk.Revit.DB.ScheduleField` | OK | OK | OK |
| `Autodesk.Revit.DB.ScheduleFieldId` | OK | OK | OK |
| `Autodesk.Revit.DB.ScheduleFilter` | OK | OK | OK |
| `Autodesk.Revit.DB.ScheduleFilterType` | OK | OK | OK |
| `Autodesk.Revit.UI.Selection.Selection` | OK | OK | OK |
| `Autodesk.Revit.DB.Solid` | OK | OK | OK |
| `Autodesk.Revit.DB.SpecTypeId` | OK | OK | OK |
| `Autodesk.Revit.DB.StorageType` | OK | OK | OK |
| `Autodesk.Revit.DB.SpecTypeId.String` | OK | OK | OK |
| `Autodesk.Revit.DB.TagMode` | OK | OK | OK |
| `Autodesk.Revit.DB.TagOrientation` | OK | OK | OK |
| `Autodesk.Revit.DB.TextNote` | OK | OK | OK |
| `Autodesk.Revit.DB.TextNoteType` | OK | OK | OK |
| `Autodesk.Revit.DB.Transaction` | OK | OK | OK |
| `Autodesk.Revit.DB.TransactionGroup` | OK | OK | OK |
| `Autodesk.Revit.Attributes.TransactionMode` | OK | OK | OK |
| `Autodesk.Revit.DB.Transform` | OK | OK | OK |
| `Autodesk.Revit.UI.UIApplication` | OK | OK | OK |
| `Autodesk.Revit.UI.UIDocument` | OK | OK | OK |
| `Autodesk.Revit.DB.UnitFormatUtils` | OK | OK | OK |
| `Autodesk.Revit.DB.UnitTypeId` | OK | OK | OK |
| `Autodesk.Revit.DB.View` | OK | OK | OK |
| `Autodesk.Revit.DB.ViewFamily` | OK | OK | OK |
| `Autodesk.Revit.DB.ViewFamilyType` | OK | OK | OK |
| `Autodesk.Revit.DB.ViewSchedule` | OK | OK | OK |
| `Autodesk.Revit.DB.ViewSection` | OK | OK | OK |
| `Autodesk.Revit.DB.Visibility` | OK | OK | OK |
| `Autodesk.Revit.DB.Wall` | OK | OK | OK |
| `Autodesk.Revit.DB.WallType` | OK | OK | OK |
| `XYZUtils.XYZ` | OK | OK | OK |
