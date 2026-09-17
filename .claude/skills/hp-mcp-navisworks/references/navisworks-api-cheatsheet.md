# Navisworks API cheatsheet — the idioms the seeds use (Navisworks Manage 2026, API 23.0, .NET Framework 4.8)

Everything below uses the bridge's default usings (`Autodesk.Navisworks.Api`, `.DocumentParts`, `.Clash`, `.Timeliner`). Unmarked lines are what a seed or the live harness executed in Navisworks Manage 2026; lines marked *API docs; not driven live* come from the API reference — confirm with `inspect_type {typeName: "DocumentModels", memberFilter: "SetHidden"}` (it reflects over the `Autodesk.Navisworks.Api.dll` that is actually loaded) before relying on them.

## Document, models, units

```csharp
doc.IsClear                                     // true = no model open (the bridge already answers -32003 then)
doc.Title; doc.FileName; doc.IsModified; doc.Units   // Units enum: Meters, Millimeters, Feet, Inches, …
doc.Models.Count; doc.Models.RootItems         // one root ModelItem per appended model
foreach (var m in doc.Models) { m.FileName; m.SourceFileName; m.Units; m.RootItem; }
units.ToMm(doubleInDocUnits); units.ToDrawing(mm); units.Label       // bridge helper — the API never converts for you
```

## Finding items — `Search`, not `Descendants`

```csharp
var search = new Search();
search.Selection.SelectAll();                            // scope = whole model; or search.Selection.CopyFrom(items)
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true;                           // do not report a match's own descendants again
var c = SearchCondition.HasPropertyByDisplayName("Item", "Name");   // category + property as shown in the Properties window
search.SearchConditions.Add(c.DisplayStringContains("Wall"));      // or:
//   c.EqualValue(VariantData.FromDisplayString("Basic Wall"))
//   c.DisplayStringWildcard("W-*")
//   c.CompareWith(SearchConditionComparison.NumericGreaterThan, VariantData.FromDouble(3.0))   // gt / lt on numbers
//   SearchCondition.HasCategoryByDisplayName("Element") — items that carry a category at all (API docs; not driven live)
var found = search.FindAll(doc, false);                  // ModelItemCollection; FindFirst(doc, false) for one
found.Count; found.Take(50)
```

Common category / property pairs on Revit-born models: `Item` / `Name`, `Item` / `Type`, `Item` / `Source File`, `Item` / `Layer`, `Element` / `Category`, `Element` / `Level`, `Element` / `Id`, `Element ID` / `Value`, `Revit Type` / `…`. Names are localised to the Navisworks UI language — read them from `get_selected_item_properties` first when unsure.

## Reading properties — `VariantData` by kind

```csharp
var prop = item.PropertyCategories.FindPropertyByDisplayName("Element", "Level");     // DataProperty or null
foreach (var cat in item.PropertyCategories) foreach (var p in cat.Properties) { cat.DisplayName; p.DisplayName; p.Value; }
VariantData v = prop.Value;
v.IsNone; v.IsDisplayString → v.ToDisplayString(); v.IsIdentifierString → v.ToIdentifierString();
v.IsDoubleLength → units.ToMm(v.ToDoubleLength()); v.IsDoubleArea; v.IsDoubleVolume; v.IsAnyDouble → v.ToAnyDouble();
v.IsInt32 → v.ToInt32(); v.IsBoolean → v.ToBoolean(); v.IsDateTime → v.ToDateTime(); v.IsNamedConstant → v.ToNamedConstant().DisplayName
```

`ToDisplayString()` **throws** on a non-string kind — always test the `Is*` flag first (the seeds' `Describe` helper). Writing custom properties needs the COM API (`SetUserDefined`), which the guard denies: not possible from a script.

## Model items

```csharp
item.DisplayName; item.ClassDisplayName; item.ClassName; item.InstanceGuid; item.HasGeometry; item.IsHidden; item.IsRequired
item.Parent; item.Ancestors; item.Children; item.Descendants; item.DescendantsAndSelf      // lazy — always cap with Take
item.BoundingBox()                                    // BoundingBox3D in document units (serialised in mm by the bridge)
item.Model                                            // the appended Model it belongs to
new ModelItemCollection { item }; collection.CopyFrom(found); collection.AddRange(items)
```

## Selection (current) and sets

```csharp
doc.CurrentSelection.SelectedItems; doc.CurrentSelection.CopyFrom(items); doc.CurrentSelection.Clear()   // Clear() on a selection is fine
// Saved sets — a tree of SavedItem: SelectionSet (HasSearch = search set, else explicit) inside FolderItem/GroupItem
foreach (var si in doc.SelectionSets.Value) { if (si is SelectionSet set) { set.DisplayName; set.Guid; set.HasSearch; set.GetSelectedItems(doc); } else if (si is GroupItem g) { g.Children; } }
var searchSet = new SelectionSet(search) { DisplayName = "MCP walls" };            // live search set
var explicitSet = new SelectionSet(items) { DisplayName = "MCP picked" };          // frozen list
doc.SelectionSets.AddCopy(searchSet);                                              // undoable
doc.SelectionSets.Remove(existingSet);   doc.SelectionSets.AddCopy(new FolderItem { DisplayName = "MCP" });   // API docs; not driven live
```

## Appearance and visibility (undoable, saved with the file)

```csharp
doc.Models.OverridePermanentColor(items, Color.FromByteRGB(255, 0, 0));    // Color.FromByteRGB(byte r, byte g, byte b)
doc.Models.OverridePermanentTransparency(items, 0.5);                     // 0 opaque … 1 invisible
doc.Models.ResetPermanentMaterials(items);                                // remove colour + transparency overrides
doc.Models.SetHidden(items, true); doc.Models.SetRequired(items, true);   // API docs; not driven live — inspect_type DocumentModels to confirm
// Temporary overrides (doc.Models.OverrideTemporaryColor …) are not saved and may not be undoable — avoid under dryRun
```

## Viewpoints and comments

```csharp
var saved = new SavedViewpoint(doc.CurrentViewpoint.ToViewpoint()) { DisplayName = "MCP - L3 clash" };
doc.SavedViewpoints.AddCopy(saved);                                                        // undoable
var added = doc.SavedViewpoints.Value.OfType<SavedViewpoint>().Last(v => v.DisplayName == saved.DisplayName);
doc.SavedViewpoints.AddComment(added, new Comment("check duct vs beam", CommentStatus.New, "MCP"));
foreach (var si in doc.SavedViewpoints.Value) { if (si is SavedViewpoint vp) { vp.Viewpoint.Position; vp.Comments; } else if (si is GroupItem g) g.Children; }
doc.CurrentViewpoint.CopyFrom(added.Viewpoint);   // moves the camera — NOT an undo entry
```

## Clash Detective (Manage only — `app.HasClashModule`)

```csharp
var clash = doc.GetClash().TestsData;                       // DocumentClashTests
foreach (var t in clash.Tests.OfType<ClashTest>()) { t.DisplayName; t.Status; t.TestType; t.Tolerance; t.LastRun; t.Children; }
IEnumerable<ClashResult> Results(SavedItem i) => i is ClashResult r ? new[] { r } : i is GroupItem g ? g.Children.SelectMany(Results) : Enumerable.Empty<ClashResult>();
foreach (var r in Results(test)) { r.DisplayName; r.Status; units.ToMm(r.Distance); r.Center; r.Item1; r.Item2; r.Comments; }
// Defining a test is undoable; RUNNING it is heavy (TestsRunTest / TestsRunAllTests — second opt-in, never undone):
var test = new ClashTest { DisplayName = "MCP pipes vs beams", TestType = ClashTestType.Hard, Tolerance = units.ToDrawing(10) };
test.SelectionA.Selection.CopyFrom(itemsA); test.SelectionB.Selection.CopyFrom(itemsB);
clash.TestsAddCopy(test);
clash.TestsRunTest(clash.Tests.OfType<ClashTest>().Last(t => t.DisplayName == test.DisplayName));   // the wrapper handed in is disposed by the run — re-resolve afterwards
clash.TestsEditResultStatus(result, ClashResultStatus.Reviewed);                                   // undoable; API docs, not driven live
```

## TimeLiner

```csharp
var tl = doc.GetTimeliner();                                                  // DocumentTimeliner
void Walk(SavedItem i, int level) { if (i is TimelinerTask t) { t.DisplayName; t.SimulationTaskTypeName; t.PlannedStartDate; t.PlannedEndDate; t.ActualStartDate; t.ActualEndDate; t.Selection.DisplayString; foreach (var c in t.Children) Walk(c, level + 1); } else if (i is GroupItem g) foreach (var c in g.Children) Walk(c, level + 1); }
foreach (var i in tl.Tasks) Walk(i, 0);
var task = new TimelinerTask { DisplayName = "MCP pour L3", PlannedStartDate = new DateTime(2026, 10, 1), SimulationTaskTypeName = "Construct" };
tl.TaskAddCopy(task);                                                         // undoable
```

## Heavy (second opt-in; local paths only; never undone; up to 600 s)

```csharp
doc.AppendFile(args.Str("path"));            // ran live (MEP.nwc, modelCount 1 -> 2); a literal path is screened, an args path is not — keep it local anyway
doc.SaveFile(@"D:\Project\federated.nwf");   // save-as; ask the user first
// MergeFile, RemoveFile, OpenFile, UpdateFiles, PublishFile, ExportAsDwf, GenerateImage, TestsRunTest/TestsRunAllTests: same gate — see script-contract.md
```

## Gotchas

- `Search` over a big model is fast; `RootItems.SelectMany(r => r.DescendantsAndSelf)` over 200 k items is not — cap with `Take` and check `ct`.
- Item identity across calls: `InstanceGuid` (unique per instance) — `ClassName`/`DisplayName` repeat.
- Boxes, distances and positions from the API are in **document units**; convert with `units.ToMm` before returning numbers (the bridge converts the *types* it knows — `BoundingBox3D`, `Point3D` — but not a raw `double`).
- Folders in sets and viewpoints are `GroupItem`/`FolderItem`; walk `Children` recursively — the seeds print the folder path as `A / B / name`.
- A `SelectionSet` built from a `Search` is re-evaluated by Navisworks every time; built from items it is frozen.
- `doc.Clear()` closes the model — heavy and refused unless allowed; `selection.Clear()` is unrelated.
- `.NET 4.8`: no `Math.Clamp`, no `string.Contains(str, comparison)`, no `Span`, no positional records, no `async`.
