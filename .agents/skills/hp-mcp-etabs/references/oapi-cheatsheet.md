# ETABSv1 OAPI cheatsheet (wrapper 2.10.0.0, ETABS 22)

Signatures read by reflection from the installed `ETABSv1.dll` (2026-09-17); tier from `HPEtabs/HPEtabs.McpBridge/Resources/etabs-oapi-tiers.txt`. Every member returns `int` (0 = ok) unless noted. `ref` = output through a reference parameter; defaults shown as `= x`. When in doubt, `inspect_type` with `typeName` = the interface name gives the live signature.

Navigation from `sapModel` (properties, not tiered): `Analyze`, `AreaObj`, `AreaElm`, `DatabaseTables`, `Diaphragm`, `EditArea`, `EditFrame`, `EditGeneral`, `EditPoint`, `File`, `FrameObj`, `GridSys`, `GroupDef`, `LoadCases`, `LoadPatterns`, `PierLabel`, `PointObj`, `PropArea`, `PropFrame`, `PropMaterial`, `PropRebar`, `RespCombo` (type `cCombo`), `Results` (`cAnalysisResults`, with `Results.Setup` = `cAnalysisResultsSetup`), `SelectObj` (`cSelect`), `SpandrelLabel`, `Story`, `Tower`, `View`, `DesignConcrete`, `DesignSteel`, `DesignShearWall`, `DesignResults`.

## Model (cSapModel)

| Member | Tier | Notes |
|---|---|---|
| `string GetModelFilename(bool IncludePath = true)` | R | `"(Untitled)"` when no model; after a save-as reports the `.$et` working copy → normalise to `.EDB` |
| `string GetModelFilepath()` | R | folder |
| `bool GetModelIsLocked()` | R | locked = results exist, definitions frozen |
| `SetModelIsLocked(bool Lockit)` | **D** | unlocking discards every result |
| `eUnits GetPresentUnits()` / `GetDatabaseUnits()` | R | the run always sees `kN_mm_C` |
| `SetPresentUnits(eUnits)` | W | never call — the bridge owns units |
| `GetVersion(ref string Version, ref double MyVersionNumber)` | R | |

## Stories & grids

| Member | Tier |
|---|---|
| `cStory.GetStories_2(ref double BaseElevation, ref int NumberStories, ref string[] StoryNames, ref double[] StoryElevations, ref double[] StoryHeights, ref bool[] IsMasterStory, ref string[] SimilarToStory, ref bool[] SpliceAbove, ref double[] SpliceHeight, ref int[] color)` | R (10 ref params, BaseElevation first) |
| `cStory.GetNameList(ref int NumberNames, ref string[] MyName)` | R |
| `cGridSys.GetNameList(ref int NumberNames, ref string[] MyName)` | R |

## Objects

| Member | Tier | Notes |
|---|---|---|
| `cPointObj.GetNameList(ref int NumberNames, ref string[] MyName)` | R | unique names |
| `cPointObj.GetCoordCartesian(string Name, ref double X, ref double Y, ref double Z, string CSys = "Global")` | R | mm |
| `cPointObj.GetLabelFromName(string Name, ref string Label, ref string Story)` | R | label + story |
| `cPointObj.GetNameFromLabel(string Label, string Story, ref string Name)` | R | label → unique name |
| `cPointObj.GetRestraint(string Name, ref bool[] Value)` / `SetRestraint(string Name, ref bool[] Value, eItemType = Objects)` | R / W | 6 bools U1 U2 U3 R1 R2 R3 |
| `int cPointObj.Count()` / `cFrameObj.Count(string MyType = "All")` / `cAreaObj.Count()` | R | returns the count directly |
| `cFrameObj.GetNameList(ref int, ref string[])` / `GetNameListOnStory(string StoryName, ref int, ref string[])` | R | |
| `cFrameObj.GetPoints(string Name, ref string Point1, ref string Point2)` | R | |
| `cFrameObj.GetSection(string Name, ref string PropName, ref string SAuto)` | R | |
| `cFrameObj.GetLabelFromName(string Name, ref string Label, ref string Story)` | R | |
| `cFrameObj.GetDesignOrientation(string Name, ref eFrameDesignOrientation)` | R | Column/Beam/Brace/Null/Other |
| `cFrameObj.SetSection(string Name, string PropName, eItemType = Objects, double SVarRelStartLoc = 0, double SVarTotalLength = 0)` | W | `changed` stays 0 (no object added) |
| `cFrameObj.AddByCoord(double XI, YI, ZI, XJ, YJ, ZJ, ref string Name, string PropName = "Default", string UserName = "", string CSys = "Global")` | W | creates frame + 2 points → `changed.added` 3; a duplicate `UserName` is renumbered by ETABS |
| `cFrameObj.AddByPoint(string Point1, string Point2, ref string Name, string PropName = "Default", string UserName = "")` | W | |
| `cFrameObj.Delete(string Name, eItemType = Objects)` | **D** | orphan points go too (−6 for a lone frame) |
| `cFrameObj.SetLoadDistributed(string Name, string LoadPat, int MyType, int Dir, double Dist1, double Dist2, double Val1, double Val2, string CSys = "Global", bool RelDist = true, bool Replace = true, eItemType = Objects)` | W | MyType 1 force / 2 moment; Dir 1–3 need `CSys = "Local"`, 4–6 global XYZ, 7–9 projected, 10 gravity, 11 projected gravity; Val in kN/mm |
| `cFrameObj.SetLoadPoint(string Name, string LoadPat, int MyType, int Dir, double Dist, double Val, string CSys = "Global", bool RelDist = true, bool Replace = true, eItemType = Objects)` | W | Val kN |
| `cFrameObj.GetLoadDistributed(string Name, ref int NumberItems, ref string[] FrameName, ref string[] LoadPat, ref int[] MyType, ref string[] CSys, ref int[] Dir, ref double[] RD1, ref double[] RD2, ref double[] Dist1, ref double[] Dist2, ref double[] Val1, ref double[] Val2, eItemType = Objects)` | R | |
| `cAreaObj.GetNameList` / `GetPoints(string Name, ref int NumberPoints, ref string[] Point)` / `GetLabelFromName` | R | area section: `GetProperty` is guard-blocked — use `DatabaseTables` |
| `cAreaObj.SetLoadUniform(string Name, string LoadPat, double Value, int Dir, bool Replace = true, string CSys = "Global", eItemType = Objects)` | W | kN/mm² |
| `cEditGeneral.Move(...)` | W | selection-based edits are W (not D) |

## Properties

| Member | Tier | Notes |
|---|---|---|
| `cPropFrame.GetNameList(ref int, ref string[], eFramePropType PropType = 0)` | R | 0 = all types |
| `cPropFrame.GetTypeOAPI(string Name, ref eFramePropType PropType)` | R | |
| `cPropFrame.GetSectProps(string Name, ref double Area, As2, As3, Torsion, I22, I33, S22, S33, Z22, Z33, R22, R33)` | R | mm², mm⁴, mm³, mm |
| `cPropFrame.GetRectangle(string Name, ref string FileName, ref string MatProp, ref double T3, ref double T2, ref int Color, ref string Notes, ref string GUID)` | R | `ref FileName` is an output, not a path argument |
| `cPropFrame.SetRectangle(string Name, string MatProp, double T3, double T2, int Color = -1, string Notes = "", string GUID = "")` | W | T3 depth, T2 width (mm) |
| `cPropMaterial.GetNameList(ref int, ref string[], eMatType MatType = 0)` | R | |
| `cPropMaterial.GetMaterial(string Name, ref eMatType MatType, ref int Color, ref string Notes, ref string GUID)` | R | |
| `cPropMaterial.GetMPIsotropic(string Name, ref double E, ref double U, ref double A, ref double G, double Temp = 0)` | R | E, G in kN/mm² → × 1000 MPa |

## Loads

| Member | Tier |
|---|---|
| `cLoadPatterns.GetNameList` / `GetLoadType(string Name, ref eLoadPatternType)` / `GetSelfWTMultiplier(string Name, ref double)` | R |
| `cLoadPatterns.Add(string Name, eLoadPatternType MyType, double SelfWTMultiplier = 0, bool AddAnalysisCase = true)` | W |
| `cLoadCases.GetNameList(ref int, ref string[], eLoadCaseType CaseType = 0)` / `GetTypeOAPI(string Name, ref eLoadCaseType CaseType, ref int SubType)` | R |
| `cCombo.GetNameList` / `GetTypeOAPI(string name, ref int ComboType)` / `GetCaseList(string Name, ref int NumberItems, ref eCNameType[] CNameType, ref string[] CName, ref double[] SF)` | R |

## Analysis (cAnalyze)

| Member | Tier | Notes |
|---|---|---|
| `GetCaseStatus(ref int NumberItems, ref string[] CaseName, ref int[] Status)` | R | 1 not run, 2 could not start, 3 not finished, 4 finished |
| `GetRunCaseFlag(ref int, ref string[] CaseName, ref bool[] Run)` | R | |
| `SetRunCaseFlag(string Name, bool Run, bool All = false)` | W | flags persist in the model file |
| `RunAnalysis()` | **D** | synchronous, no cancel, saves the file itself |
| `DeleteResults(string Name, bool All = false)` | **D** | |

## Results (cAnalysisResults — all R; select output first)

| Member | Notes |
|---|---|
| `Results.Setup.DeselectAllCasesAndCombosForOutput()` | call first |
| `Results.Setup.SetCaseSelectedForOutput(string Name, bool Selected = true)` / `SetComboSelectedForOutput(...)` | a name is a case **or** a combo — try both, `ArgumentException` when neither is 0 |
| `JointReact(string Name, eItemTypeElm ItemTypeElm, ref int NumberResults, ref string[] Obj, ref string[] Elm, ref string[] LoadCase, ref string[] StepType, ref double[] StepNum, ref double[] F1, F2, F3, M1, M2, M3)` | `("All", eItemTypeElm.GroupElm, …)` = every joint; M in kN·mm |
| `JointDispl(string Name, eItemTypeElm, ref int, ref string[] Obj, Elm, LoadCase, StepType, ref double[] StepNum, U1, U2, U3, R1, R2, R3)` | mm / rad |
| `FrameForce(string Name, eItemTypeElm, ref int NumberResults, ref string[] Obj, ref double[] ObjSta, ref string[] Elm, ref double[] ElmSta, ref string[] LoadCase, StepType, ref double[] StepNum, P, V2, V3, T, M2, M3)` | stations mm from I end |
| `ModalPeriod(ref int NumberResults, ref string[] LoadCase, StepType, ref double[] StepNum, Period, Frequency, CircFreq, EigenValue)` | |
| `ModalParticipatingMassRatios(ref int, ref string[] LoadCase, StepType, ref double[] StepNum, Period, UX, UY, UZ, SumUX, SumUY, SumUZ, RX, RY, RZ, SumRX, SumRY, SumRZ)` | match rows to `ModalPeriod` by index |
| `BaseReact(ref int, ref string[] LoadCase, StepType, ref double[] StepNum, FX, FY, FZ, MX, ParamMy, MZ, ref double GX, GY, GZ)` | |
| `StoryDrifts(ref int, ref string[] Story, LoadCase, StepType, ref double[] StepNum, ref string[] Direction, ref double[] Drift, ref string[] Label, ref double[] X, Y, Z)` | |
| `PierForce(ref int, ref string[] StoryName, PierName, LoadCase, Location, ref double[] P, V2, V3, T, M2, M3)` | |

## Tables, selection, view, file

| Member | Tier | Notes |
|---|---|---|
| `cDatabaseTables.GetAvailableTables(ref int NumberTables, ref string[] TableKey, ref string[] TableName, ref int[] ImportType)` | R | |
| `cDatabaseTables.GetTableForDisplayArray(string TableKey, ref string[] FieldKeyList, string GroupName, ref int TableVersion, ref string[] FieldsKeysIncluded, ref int NumberRecords, ref string[] TableData)` | R | flat `TableData` = records × fields; the generic way to read anything the seeds do not cover (area sections, assignments) |
| `cDatabaseTables.*CSVFile*` / `ApplyEditedTables` / `ShowTablesInExcel` | **D** | path-taking / editing |
| `cSelect.GetSelected(ref int NumberItems, ref int[] ObjectType, ref string[] ObjectName)` / `ClearSelection()` | R | ObjectType codes per the CSI docs (1 point, 2 frame, 5 area, 7 link — verify with `includeSelection` of `get_etabs_context`, which maps them to category names) |
| `cView.RefreshView(int Window = 0, bool Zoom = true)` | R | |
| `cFile.Save(string FileName = "")` | **D** (`path=0`) | no name = save in place; a name = **save-as** (model re-pointed) |
| `cFile.OpenFile(string FileName)` / `NewBlank()` / `NewGridOnly(...)` | **D** | |

## Enums

- `eUnits`: `kN_mm_C` (forced), `kN_m_C`, `N_mm_C`, `kgf_m_C`, `Ton_m_C`, `kip_in_F`, … 
- `eItemType`: `Objects`, `Group`, `SelectedObjects`; `eItemTypeElm`: `ObjectElm`, `Element`, `GroupElm`, `SelectionElm`.
- `eLoadPatternType`: `Dead`, `SuperDead`, `Live`, `ReduceLive`, `Quake`, `Wind`, `Snow`, `Other`, `Temperature`, `Rooflive`, `Notional`, …
- `eLoadCaseType`: `LinearStatic`, `NonlinearStatic`, `Modal`, `ResponseSpectrum`, `LinearHistory`, `NonlinearHistory`, `Buckling`, `HyperStatic`, …
- `eFramePropType`: `I`, `Channel`, `T`, `Angle`, `Box`, `Pipe`, `Rectangular`, `Circle`, `General`, `Auto`, `SD`, `Variable`, `Concrete_L`, `ConcreteTee`, `FilledTube`, `EncasedRectangle`, …
- `eMatType`: `Steel`, `Concrete`, `NoDesign`, `Aluminum`, `ColdFormed`, `Rebar`, `Tendon`, `Masonry`.
