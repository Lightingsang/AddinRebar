# Investigation Report: Strict All-or-Nothing Batch Transaction & Script Runner Heuristic Fallbacks (R1.3 & R1.4)

- **Agent**: `explorer_m1_rem_3`
- **Date**: 2026-10-08
- **Milestone**: M1 Remediation (R1.3 & R1.4)
- **Status**: Complete & Ready for Worker Implementation

---

## Executive Summary

This investigation analyzed the root causes of the 08/10/2026 review blockers regarding **Strict All-or-Nothing Batch Transaction Rollback** (R1.3) and **Elimination of Heuristic Fallbacks in Roslyn Runner Scripts** (R1.4).

### Key Deficiencies Identified
1. **Silent Batch Partial Commits (R1.3 Defect)**: In `BimPlanRunnerScriptTemplate.cs` and `BimPlanRunnerScriptTemplate.Receipt.cs`, element failures only roll back individual `SubTransaction`s. The overall script does not throw an exception upon failure, meaning the parent Revit transaction commits successfully via `ScriptRunner.cs`. As a result, successful elements in a flawed batch remain in Revit, and an execution receipt is still written to Extensible Storage.
2. **Missing Column Constraints (R1.4 Defect)**: In `BimPlanRunnerScriptTemplate.Structure.cs`, column placement ignores `rotationDeg` completely and does not constrain `BuiltInParameter.FAMILY_TOP_LEVEL_PARAM` to `TargetTopLevelName`.
3. **Implicit `FirstOrDefault()` Heuristic Fallback (R1.4 Defect)**: In `BimPlanRunnerScriptTemplate.cs`, `Stage7MepGenerator.cs`, and `CompositeBimGenerator.Stages7To8.cs`, `PipingSystemType` and `PipeType` are selected via `.FirstOrDefault()`, binding arbitrary system types (e.g., Hydronic) instead of explicitly resolving the required type or throwing `ERR_SYMBOL_NOT_FOUND`.

---

## 1. Deep-Dive Inspection Findings

### 1.1 Strict Batch Transaction Rollback & Extensible Storage (R1.3)

#### Current Architecture
In `HPRebar.McpBridge/Service/ScriptRunner.cs` (lines 75–125):
- The Revit bridge wraps script execution in:
  - `TransactionGroup group = new TransactionGroup(doc, "MCP: " + label);`
  - `Transaction transaction = new Transaction(doc, "MCP script");`
- The script executes via `script.RunAsync(globals, ...)`.
- If the script finishes normally, `transaction.Commit()` and `group.Assimilate()` commit all changes to the Revit document.
- If and only if the script throws an unhandled `Exception`, the bridge executes `RollBack(transaction, group)`, which rolls back the entire `transaction` and `group`, returning `ExecuteResult` with `IsError = true` and `RolledBack = true`.

#### The Defect in `BimPlanRunnerScriptTemplate`
In `BimPlanRunnerScriptTemplate.cs` (lines 167–189):
- Each item is executed inside a `SubTransaction`:
  ```csharp
  using (var subTx = new SubTransaction(doc))
  {
      subTx.Start();
      try { ... subTx.Commit(); }
      catch (Exception ex)
      {
          subTx.RollBack();
          records.Add(new Dictionary<string, object> { ["status"] = "Failed", ... });
      }
  }
  ```
- When an item fails (e.g., missing level, missing symbol, degenerate geometry), `subTx.RollBack()` only rolls back that single item.
- Then `GetReceiptScript()` in `BimPlanRunnerScriptTemplate.Receipt.cs` executes:
  ```csharp
  if (!dryRun && !string.IsNullOrEmpty(planHash))
  {
      // Writes HPBimCoordinationReceipt DataStorage entity unconditionally!
  }
  ```
- Then `GetEpilogueScript()` returns `{ total, created, failed, records }`.
- No exception is thrown out of the script!
- Therefore, the bridge commits the `Transaction`! Partial elements remain in RVT, the receipt is recorded, and `execRes.RolledBack == false`.

#### Required Fix for R1.3
1. In `BimPlanRunnerScriptTemplate.Receipt.cs`:
   - Check `bool hasBatchErrors = records.Any(r => (string)r["status"] == "Failed");`.
   - Only write receipt when `!hasBatchErrors`:
     ```csharp
     if (!dryRun && !string.IsNullOrEmpty(planHash) && !hasBatchErrors)
     {
         // Write receipt
     }
     ```
   - If `hasBatchErrors == true`:
     Throw an explicit exception:
     ```csharp
     if (hasBatchErrors)
     {
         int failCount = records.Count(r => (string)r["status"] == "Failed");
         var firstFailed = records.First(r => (string)r["status"] == "Failed");
         string errCode = firstFailed.ContainsKey("errorCode") ? (string)firstFailed["errorCode"] : "ERR_BATCH_ITEM_FAILED";
         string errReason = firstFailed.ContainsKey("errorReason") ? (string)firstFailed["errorReason"] : "Unknown error";
         string payloadJson = System.Text.Json.JsonSerializer.Serialize(records);
         throw new InvalidOperationException($"[BATCH_TRANSACTION_FAILED] Execution failed for {failCount} element(s). First failure: [{errCode}] {errReason} ||PAYLOAD:{payloadJson}");
     }
     ```
2. In `HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs`:
   - Update `ParseElementRecords`: when `execRes.IsError == true` and `execRes.Message.Contains("||PAYLOAD:")`, extract the payload JSON and deserialize individual `ElementExecutionRecord`s so each item's true failure code and reason are preserved.
   - Update `FormatErrorMessage`: strip `||PAYLOAD:...` so user-facing logs and console messages remain concise and readable.
3. In `HPBim.Cli/Commands/GenerateCommand.cs`:
   - `bool isOk = dryRun ? (result.Success && result.RolledBack) : result.Success;`
   - Returns exit code `1` when `result.Success == false`.

---

### 1.2 Column `rotationDeg` & `topLevel` Constraints (R1.4)

#### Observations
1. In `BimPlanRunnerScriptTemplate.Structure.cs` (lines 162–170):
   ```csharp
   var loc = geom.Obj("location");
   var pt = ToXyz(loc.Double("x"), loc.Double("y"), 0);
   var colInst = doc.Create.NewFamilyInstance(pt, sym, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);
   ```
   - `rotationDeg` is completely ignored.
   - `FAMILY_TOP_LEVEL_PARAM` is never set.
2. In `Stage3StructureGenerator.cs` (lines 86–95) and `CompositeBimGenerator.Stages1To3.cs` (lines 88–95):
   - `topLevel` is set via `FAMILY_TOP_LEVEL_PARAM`, but `rotationDeg` is ignored.

#### Required Fix for Columns
1. **Rotation Application**:
   Using Revit API standard rotation around Z-axis through column base point:
   ```csharp
   double rotDeg = geom.Double("rotationDegrees", geom.Double("rotationDeg", 0.0));
   if (Math.Abs(rotDeg) > 1e-4)
   {
       var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
       ElementTransformUtils.RotateElement(doc, colInst.Id, axis, rotDeg * (Math.PI / 180.0));
   }
   ```
2. **TopLevel Constraint**:
   ```csharp
   string topLvlName = item.Str("targetTopLevelName", string.Empty);
   if (!string.IsNullOrEmpty(topLvlName))
   {
       if (!existingLevels.TryGetValue(topLvlName, out var topLvl))
       {
           subTx.RollBack();
           records.Add(new Dictionary<string, object>
           {
               ["sourceKey"] = sourceKey,
               ["category"] = category,
               ["mark"] = mark,
               ["status"] = "Failed",
               ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
               ["errorReason"] = $"TopLevel '{topLvlName}' not found in document."
           });
           continue;
       }

       var pTop = colInst.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
       if (pTop != null && !pTop.IsReadOnly) pTop.Set(topLvl.Id);
   }
   ```
3. Update `Stage3StructureGenerator.cs` and `CompositeBimGenerator.Stages1To3.cs` to apply `rotationDeg` equivalently.

---

### 1.3 Total Elimination of `FirstOrDefault()` for Pipe/PipingSystem (R1.4)

#### Observations
1. In `BimPlanRunnerScriptTemplate.cs` (lines 151–152):
   ```csharp
   var pipingSysType = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)).Cast<Autodesk.Revit.DB.Plumbing.PipingSystemType>().FirstOrDefault();
   var pipeType = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)).Cast<Autodesk.Revit.DB.Plumbing.PipeType>().FirstOrDefault();
   ```
2. In `Stage7MepGenerator.cs` (lines 82–90) and `CompositeBimGenerator.Stages7To8.cs` (lines 30–38):
   Identical `.FirstOrDefault()` queries without name matching.
3. In `ExecutionPlanBuilder.ArchitectureAndMep.cs` (lines 176–188):
   `Resource` and `Geometry` for `PipeRun` items are not populated.

#### Required Fix for Pipe/PipingSystem
1. **Dictionary Collection in Preamble**:
   ```csharp
   var pipingSysTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipingSystemType>(StringComparer.OrdinalIgnoreCase);
   foreach (Autodesk.Revit.DB.Plumbing.PipingSystemType pst in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)))
   {
       if (!pipingSysTypes.ContainsKey(pst.Name)) pipingSysTypes[pst.Name] = pst;
   }

   var pipeTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipeType>(StringComparer.OrdinalIgnoreCase);
   foreach (Autodesk.Revit.DB.Plumbing.PipeType pt in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)))
   {
       if (!pipeTypes.ContainsKey(pt.Name)) pipeTypes[pt.Name] = pt;
   }
   ```
2. **Explicit Resolution & `ERR_SYMBOL_NOT_FOUND` Failure**:
   In `BimPlanRunnerScriptTemplate.MepAndRebar.cs`:
   ```csharp
   string reqSysType = res.Str("familyName", item.Obj("parameters").Str("SystemType", "Domestic Cold Water"));
   string reqPipeType = res.Str("typeName", item.Obj("parameters").Str("PipeType", "Standard"));

   pipingSysTypes.TryGetValue(reqSysType, out var pipingSysType);
   pipeTypes.TryGetValue(reqPipeType, out var pipeType);

   if (pipingSysType == null || pipeType == null)
   {
       subTx.RollBack();
       string missing = pipingSysType == null && pipeType == null
           ? $"PipingSystemType '{reqSysType}' and PipeType '{reqPipeType}'"
           : (pipingSysType == null ? $"PipingSystemType '{reqSysType}'" : $"PipeType '{reqPipeType}'");

       records.Add(new Dictionary<string, object>
       {
           ["sourceKey"] = sourceKey,
           ["category"] = category,
           ["mark"] = mark,
           ["status"] = "Failed",
           ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
           ["errorReason"] = $"{missing} not found in document."
       });
       continue;
   }
   ```
3. Update `TypeResolutionRules.cs`:
   Add `GetPipeTypeName(string material)` and `GetPipingSystemTypeName(PipeSystemType systemType)`.
4. Update `ExecutionPlanBuilder.ArchitectureAndMep.cs`:
   Populate `Resource` and `Geometry` for `PipeRun` using `TypeResolutionRules`.
5. Update `Stage7MepGenerator.cs` and `CompositeBimGenerator.Stages7To8.cs`:
   Replace `.FirstOrDefault()` with dictionary lookup; throw `InvalidOperationException("[ERR_SYMBOL_NOT_FOUND] ...")` if missing.

---

## 2. Step-by-Step Fix Strategy for Worker

### Step 1: Clean Code File Boundary Preparation
`BimPlanRunnerScriptTemplate.Structure.cs` is currently **268 lines**. Adding Column `rotationDeg` and `topLevel` logic directly would bring it to ~295 lines, dangerously close to the 300-line limit (Rule C2).
- **Strategy**: Split `BimPlanRunnerScriptTemplate.Structure.cs` into:
  - `BimPlanRunnerScriptTemplate.Structure.cs` (Levels, Grids, Beams; ~160 lines).
  - `BimPlanRunnerScriptTemplate.Columns.cs` (Columns; ~120 lines).
  - Both are `partial class BimPlanRunnerScriptTemplate`.
  - In `BimPlanRunnerScriptTemplate.cs`:
    `GetStructureScript() => GetLevelsAndGridsScript() + GetColumnsScript() + GetBeamsScript();`

### Step 2: Implement Column `rotationDeg` & `topLevel`
- In `BimPlanRunnerScriptTemplate.Columns.cs`:
  - Read `rotationDegrees` (or `rotationDeg`) from `geom`. If `Math.Abs(rotDeg) > 1e-4`, rotate instance around Z-axis via `ElementTransformUtils.RotateElement`.
  - Read `targetTopLevelName` from `item`. If set, validate against `existingLevels` (failing with `ERR_LEVEL_NOT_FOUND` if missing) and set `FAMILY_TOP_LEVEL_PARAM`.
- In `Stage3StructureGenerator.cs` and `CompositeBimGenerator.Stages1To3.cs`:
  - Apply `rotationDeg` around Z-axis.

### Step 3: Implement Pipe Type Resolution & Eliminate `FirstOrDefault()`
- In `TypeResolutionRules.cs`:
  - Add `GetPipeTypeName(string material)` and `GetPipingSystemTypeName(PipeSystemType type)`.
- In `ExecutionPlanBuilder.ArchitectureAndMep.cs`:
  - Set `Resource.FamilyName` and `Resource.TypeName` on `ElementPlanItem` for `PipeRun`.
- In `BimPlanRunnerScriptTemplate.cs` (Preamble):
  - Replace lines 151–152 (`FirstOrDefault()`) with dictionary collectors for `pipingSysTypes` and `pipeTypes`.
- In `BimPlanRunnerScriptTemplate.MepAndRebar.cs`:
  - Lookup requested system and pipe type by name.
  - If missing, fail with `ERR_SYMBOL_NOT_FOUND`.
- In `Stage7MepGenerator.cs` and `CompositeBimGenerator.Stages7To8.cs`:
  - Replace `FirstOrDefault()` with dictionary lookup.

### Step 4: Implement Strict All-or-Nothing Batch Rollback
- In `BimPlanRunnerScriptTemplate.Receipt.cs`:
  - Compute `bool hasBatchErrors = records.Any(r => (string)r["status"] == "Failed");`.
  - Guard receipt writing with `if (!dryRun && !string.IsNullOrEmpty(planHash) && !hasBatchErrors)`.
  - If `hasBatchErrors`: throw `InvalidOperationException($"[BATCH_TRANSACTION_FAILED] ... ||PAYLOAD:{payloadJson}")`.
- In `BimPipelineEngine.PlanExecution.cs`:
  - In `ParseElementRecords`: extract records from `||PAYLOAD:` when `execRes.IsError == true`.
- In `BimPipelineEngine.cs`:
  - In `FormatErrorMessage`: strip `||PAYLOAD:...` for clean error reporting.

### Step 5: Test Verification & Acceptance Testing
- Add `StrictBatchRollbackTests.cs` in `HPBim.Client.Tests`:
  - Test 1: Mixed batch of 10 elements (9 valid, 1 invalid symbol). Verify transaction is rolled back (`RolledBack == true`), `Success == false`, and no receipt written.
  - Test 2: Verify `GenerateCommand` returns exit code 1 when batch fails.
- Add `RunnerHeuristicEliminationTests.cs` in `HPBim.Client.Tests`:
  - Test 3: Generated script contains `ElementTransformUtils.RotateElement` and `FAMILY_TOP_LEVEL_PARAM`.
  - Test 4: Generated script has zero `.FirstOrDefault()` calls for `PipingSystemType` and `PipeType`.
  - Test 5: Missing pipe type triggers `ERR_SYMBOL_NOT_FOUND`.
- Run full regression suite:
  - `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` (417 tests)
  - `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` (322 tests)
  - `dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj` (42 tests)

---

## 3. Concrete Code Snippets for Worker

### Snippet 1: `BimPlanRunnerScriptTemplate.Receipt.cs` (Batch Rollback & Receipt Guard)
```csharp
namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetReceiptScript() =>
        GetReceiptHeaderAndSchemaScript() +
        GetReceiptStorageScript() +
        GetBatchRollbackCheckScript();

    private static string GetReceiptHeaderAndSchemaScript() => """
        // -------------------------------------------------------------------------
        // 3. Extensible Storage Receipt Writing (Atomic Commit)
        // -------------------------------------------------------------------------
        bool dryRun = args.Obj("options").Bool("dryRun", false);
        string planHash = args.Str("planHash", string.Empty);
        string opIdStr = args.Obj("options").Str("operationId", string.Empty);
        Guid operationId = Guid.TryParse(opIdStr, out var parsedOp)
            ? parsedOp
            : (Guid.TryParse(args.Str("planId", string.Empty), out var parsedPId) ? parsedPId : Guid.NewGuid());

        bool hasBatchErrors = records.Any(r => (string)r["status"] == "Failed");

        if (!dryRun && !string.IsNullOrEmpty(planHash) && !hasBatchErrors)
        {
            var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
            var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
            if (schema == null)
            {
                var builder = new Autodesk.Revit.DB.ExtensibleStorage.SchemaBuilder(schemaGuid);
                builder.SetSchemaName("HPBimCoordinationReceipt");
                builder.SetDocumentation("HPBim: Execution receipt tracking atomic commit of BIM execution plan.");
                builder.SetReadAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                builder.SetWriteAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                builder.AddSimpleField("OperationId", typeof(Guid));
                builder.AddSimpleField("PlanHash", typeof(string));
                builder.AddSimpleField("CommittedAtUtc", typeof(string));
                builder.AddSimpleField("ElementMapJson", typeof(string));
                schema = builder.Finish();
            }
        """;

    private static string GetReceiptStorageScript() => """
            var elemMap = new Dictionary<string, string>();
            foreach (var r in records)
            {
                string sk = r.ContainsKey("sourceKey") ? (string)r["sourceKey"] : string.Empty;
                string uid = r.ContainsKey("targetUniqueId") ? (string)r["targetUniqueId"] : string.Empty;
                string st = r.ContainsKey("status") ? (string)r["status"] : string.Empty;
                if (!string.IsNullOrEmpty(sk) && !string.IsNullOrEmpty(uid) &&
                    (st == "Created" || st == "Updated" || st == "NoOp"))
                {
                    elemMap[sk] = uid;
                }
            }

            string elemMapJson = System.Text.Json.JsonSerializer.Serialize(elemMap);

            Autodesk.Revit.DB.ExtensibleStorage.DataStorage receiptDs = null;
            var allDs = new FilteredElementCollector(doc)
                .OfClass(typeof(Autodesk.Revit.DB.ExtensibleStorage.DataStorage))
                .Cast<Autodesk.Revit.DB.ExtensibleStorage.DataStorage>();
            foreach (var ds in allDs)
            {
                var ent = ds.GetEntity(schema);
                if (ent != null && ent.IsValid() && ent.Get<Guid>("OperationId") == operationId)
                {
                    receiptDs = ds;
                    break;
                }
            }

            if (receiptDs == null)
            {
                receiptDs = Autodesk.Revit.DB.ExtensibleStorage.DataStorage.Create(doc);
            }

            var entity = new Autodesk.Revit.DB.ExtensibleStorage.Entity(schema);
            entity.Set<Guid>("OperationId", operationId);
            entity.Set<string>("PlanHash", planHash);
            entity.Set<string>("CommittedAtUtc", DateTime.UtcNow.ToString("o"));
            entity.Set<string>("ElementMapJson", elemMapJson);
            receiptDs.SetEntity(entity);
        }
        """;

    private static string GetBatchRollbackCheckScript() => """
        if (hasBatchErrors)
        {
            int failCount = records.Count(r => (string)r["status"] == "Failed");
            var firstFailed = records.First(r => (string)r["status"] == "Failed");
            string errCode = firstFailed.ContainsKey("errorCode") ? (string)firstFailed["errorCode"] : "ERR_BATCH_ITEM_FAILED";
            string errReason = firstFailed.ContainsKey("errorReason") ? (string)firstFailed["errorReason"] : "Item failed";
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(records);
            throw new InvalidOperationException($"[BATCH_TRANSACTION_FAILED] Execution failed for {failCount} element(s). First failure: [{errCode}] {errReason} ||PAYLOAD:{payloadJson}");
        }
        """;
}
```

### Snippet 2: `BimPlanRunnerScriptTemplate.Columns.cs` (Column Rotation & TopLevel Constraint)
```csharp
namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetColumnsScript() => """
                    else if (category == "Column")
                    {
                        if (!string.IsNullOrEmpty(targetUniqueId))
                        {
                            var existingCol = doc.GetElement(targetUniqueId) as FamilyInstance;
                            if (existingCol != null)
                            {
                                string reqSym = res.Str("typeName", string.Empty);
                                bool modified = false;
                                if (!string.IsNullOrEmpty(reqSym) && columnSymbols.TryGetValue(reqSym, out var newSym) && existingCol.Symbol?.Id != newSym.Id)
                                {
                                    existingCol.Symbol = newSym;
                                    modified = true;
                                }

                                string topLvlUpdate = item.Str("targetTopLevelName", string.Empty);
                                if (!string.IsNullOrEmpty(topLvlUpdate) && existingLevels.TryGetValue(topLvlUpdate, out var topLvlMatch))
                                {
                                    var pTopParam = existingCol.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                                    if (pTopParam != null && !pTopParam.IsReadOnly && pTopParam.AsElementId() != topLvlMatch.Id)
                                    {
                                        pTopParam.Set(topLvlMatch.Id);
                                        modified = true;
                                    }
                                }

                                if (!string.IsNullOrEmpty(mark)) createdElements[mark] = existingCol;
                                subTx.Commit();
                                records.Add(new Dictionary<string, object>
                                {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = modified ? "Updated" : "NoOp",
                                    ["targetUniqueId"] = existingCol.UniqueId,
                                    ["targetElementId"] = (long)existingCol.Id.Value
                                });
                                continue;
                            }
                        }

                        string lvlName = item.Require("targetLevelName");
                        if (!existingLevels.TryGetValue(lvlName, out var baseLvl))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
                                ["errorReason"] = $"BaseLevel '{lvlName}' not found."
                            });
                            continue;
                        }

                        string topLvlName = item.Str("targetTopLevelName", string.Empty);
                        Level topLvl = null;
                        if (!string.IsNullOrEmpty(topLvlName))
                        {
                            if (!existingLevels.TryGetValue(topLvlName, out topLvl))
                            {
                                subTx.RollBack();
                                records.Add(new Dictionary<string, object>
                                {
                                    ["sourceKey"] = sourceKey,
                                    ["category"] = category,
                                    ["mark"] = mark,
                                    ["status"] = "Failed",
                                    ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
                                    ["errorReason"] = $"TopLevel '{topLvlName}' not found."
                                });
                                continue;
                            }
                        }

                        string symName = res.Require("typeName");
                        if (!columnSymbols.TryGetValue(symName, out var sym))
                        {
                            subTx.RollBack();
                            records.Add(new Dictionary<string, object>
                            {
                                ["sourceKey"] = sourceKey,
                                ["category"] = category,
                                ["mark"] = mark,
                                ["status"] = "Failed",
                                ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
                                ["errorReason"] = $"Column symbol '{symName}' not found in document."
                            });
                            continue;
                        }

                        if (!sym.IsActive) sym.Activate();
                        var loc = geom.Obj("location");
                        var pt = ToXyz(loc.Double("x"), loc.Double("y"), 0);

                        var colInst = doc.Create.NewFamilyInstance(pt, sym, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);
                        if (topLvl != null)
                        {
                            var pTop = colInst.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (pTop != null && !pTop.IsReadOnly) pTop.Set(topLvl.Id);
                        }

                        double rotDeg = geom.Double("rotationDegrees", geom.Double("rotationDeg", 0.0));
                        if (Math.Abs(rotDeg) > 1e-4)
                        {
                            var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                            ElementTransformUtils.RotateElement(doc, colInst.Id, axis, rotDeg * (Math.PI / 180.0));
                        }

                        var pComments = colInst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (pComments != null && !pComments.IsReadOnly && !string.IsNullOrEmpty(sourceKey)) pComments.Set(sourceKey);
                        if (!string.IsNullOrEmpty(mark)) createdElements[mark] = colInst;
                        subTx.Commit();

                        records.Add(new Dictionary<string, object>
                        {
                            ["sourceKey"] = sourceKey,
                            ["category"] = category,
                            ["mark"] = mark,
                            ["status"] = "Created",
                            ["targetUniqueId"] = colInst.UniqueId,
                            ["targetElementId"] = (long)colInst.Id.Value
                        });
                    }
        """;
}
```

### Snippet 3: Pipe/PipingSystem Type Resolution (Eliminating `FirstOrDefault()`)
In `BimPlanRunnerScriptTemplate.cs` (Preamble):
```csharp
var pipingSysTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipingSystemType>(StringComparer.OrdinalIgnoreCase);
foreach (Autodesk.Revit.DB.Plumbing.PipingSystemType pst in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)))
{
    if (!pipingSysTypes.ContainsKey(pst.Name)) pipingSysTypes[pst.Name] = pst;
}

var pipeTypes = new Dictionary<string, Autodesk.Revit.DB.Plumbing.PipeType>(StringComparer.OrdinalIgnoreCase);
foreach (Autodesk.Revit.DB.Plumbing.PipeType pt in new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)))
{
    if (!pipeTypes.ContainsKey(pt.Name)) pipeTypes[pt.Name] = pt;
}
```

In `BimPlanRunnerScriptTemplate.MepAndRebar.cs`:
```csharp
string reqSysType = res.Str("familyName", item.Obj("parameters").Str("SystemType", "Domestic Cold Water"));
string reqPipeType = res.Str("typeName", item.Obj("parameters").Str("PipeType", "Standard"));

Autodesk.Revit.DB.Plumbing.PipingSystemType pipingSysType = null;
if (!string.IsNullOrEmpty(reqSysType)) pipingSysTypes.TryGetValue(reqSysType, out pipingSysType);

Autodesk.Revit.DB.Plumbing.PipeType pipeType = null;
if (!string.IsNullOrEmpty(reqPipeType)) pipeTypes.TryGetValue(reqPipeType, out pipeType);

if (pipingSysType == null || pipeType == null)
{
    subTx.RollBack();
    string missing = pipingSysType == null && pipeType == null
        ? $"PipingSystemType '{reqSysType}' and PipeType '{reqPipeType}'"
        : (pipingSysType == null ? $"PipingSystemType '{reqSysType}'" : $"PipeType '{reqPipeType}'");

    records.Add(new Dictionary<string, object>
    {
        ["sourceKey"] = sourceKey,
        ["category"] = category,
        ["mark"] = mark,
        ["status"] = "Failed",
        ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
        ["errorReason"] = $"{missing} not found in document."
    });
    continue;
}
```

---

## 4. Verification & Defense

1. **Independent Verification Method**:
   - Run `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`
   - Run `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
   - Run `dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj`
2. **Acceptance Invalidation Criteria**:
   - If any `FirstOrDefault()` remains for `PipeType` or `PipingSystemType`.
   - If a batch with 1 failed item results in `RolledBack == false` or creates a receipt in Extensible Storage.
   - If any C# file exceeds 300 lines or any method exceeds 50 lines.
