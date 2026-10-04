---
name: project-revit-view-scale-and-template-facts
description: Revit API facts for reviewing view-scale / view-template / annotation-size code in HPRebar (BeamRebar views, tables)
metadata:
  type: project
---

- `BuiltInParameter.VIEW_SCALE` IS a template parameter id (dev guide "View Templates": it travels together with `VIEW_SCALE_PULLDOWN_METRIC/IMPERIAL` in the non-controlled set), so `!template.GetNonControlledTemplateParameterIds().Contains(new ElementId(VIEW_SCALE))` is a valid "template controls the scale" test. `GetElement(InvalidElementId)` → null = no template.
- `View.IsValidViewTemplate(id)` exists (2026 XML) — the `ViewTemplateId` setter throws `ArgumentException` for an incompatible template. `BeamAnnotationSettings.Load` picks the first "Structural" template of ANY view type (via localized `AsValueString`, R7) — guard missing as of 2026-10-03.
- `TextNoteType` `TEXT_SIZE` is paper size: model height = size × the view's own `Scale`. `RebarTableTagCreator.CalculateRowHeight` uses `× 100/templateScale` (inverted, and reads the template not the view) — pre-existing as of 62d5111.
- BeamRebar `DimensionCreator.PlannedCount` hard-codes 2 sections/span and ignores view flags; `SectionViewCreator.ComputeCutStations` gives 1 cut for cantilevers — progress plans built from `Spans × SectionsPerSpan` overcount.

**Why:** found while reviewing 62d5111 (Beam Views tab wiring); no Revit available, facts from RevitAPI.xml + dev guide.
**How to apply:** any review touching BeamRebar views/tables/progress or view-template handling elsewhere.
