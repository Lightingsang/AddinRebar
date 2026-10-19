# Technical Exploration & Architectural Specification Report: Milestone M1 (P0 Core: Schema Validation & Build Repair)

**Agent**: `explorer_m1_1` (Teamwork Explorer)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1`  
**Reference Dispatch**: Milestone M1 from `ORIGINAL_REQUEST.md` (## 2026-10-07T13:49:28Z) and `PROJECT.md`  

---

## 1. Executive Summary & Problem Statement

Milestone M1 addresses two foundational defects in the `HPBim` deliverable:
1. **Broken Build Baseline (CS0535)**: `HPBim.Client.Tests` currently fails compilation because `FakeRevitPipeClient.cs(14,43)` does not implement `IRevitPipeClient.CancelAsync(CancellationToken)`. Fixing this single defect restores compilation across the entire solution and enables running all 370 tests (268 Core + 102 Client).
2. **Silent Schema Bypass & False PASS**: In `HPBim.Core.Serialization.BimModelSerializer`, JSON deserialization ignores unrecognized properties and defaults all collections in `BimModel` to empty arrays. As a consequence:
   - Raw CAD JSON (e.g. `{ "cad": { "blockName": "Rec-TT", ... } }`) silently deserializes into an empty `BimModel`.
   - Empty JSON `{}` silently deserializes into an empty `BimModel`.
   - `QcEngine.Validate(model)` runs 0 checks on empty collections, reports 0 errors and 0 clashes, producing `report.IsValid == true`.
   - `ValidateCommand` outputs `[PASS] TRẠNG THÁI: SẠCH 100%. Mô hình sẵn sàng dispatch tới Revit.` with exit code `0`.
   - `GenerateCommand` dispatches an empty model to Revit and outputs `DỰNG HÌNH HOÀN TẤT` with exit code `0`.
   - Models with invalid or dangling level references (e.g. a column referencing non-existent `"Level_Unknown"`) pass validation without catching broken dependencies.

This report provides the complete, read-only architectural specification and exact source code designs for:
- The CS0535 build repair in `FakeRevitPipeClient.cs`.
- `SchemaValidationResult.cs`, `BimModelSchemaValidator.cs`, and `SchemaValidationException.cs` in `HPBim.Core/Validation/`.
- Integration into `BimModelSerializer.cs`, `BimModel.cs`, and `ValidateCommand.cs`.
- The exhaustive xUnit test suite in `HPBim.Core.Tests/Validation/BimModelSchemaValidatorTests.cs`.

---

## 2. Build Baseline & Exact Fix for CS0535

### 2.1 Diagnostic Observation
- **Project**: `HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
- **File**: `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs` line 14
- **Verbatim Compiler Output**:
  ```
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs(14,43): 
  error CS0535: 'FakeRevitPipeClient' does not implement interface member 'IRevitPipeClient.CancelAsync(CancellationToken)'
  ```
- **Interface Contract** (`HPBim/src/HPBim.Client/Connection/IRevitPipeClient.cs:52`):
  ```csharp
  /// <summary>
  ///     Cancels running script execution in the bridge via "revit.cancel".
  /// </summary>
  Task<bool> CancelAsync(CancellationToken cancellationToken = default);
  ```

### 2.2 Proposed Fix in `FakeRevitPipeClient.cs`
In `FakeRevitPipeClient.cs`, add the test-observable properties and the method implementation:

```csharp
// --- Add properties around line 26 ---
public bool CancelResult { get; set; } = true;
public int CancelCount { get; private set; }

// --- Add implementation after PingAsync / GetContextAsync (around line 76) ---
public Task<bool> CancelAsync(CancellationToken cancellationToken = default)
{
    if (!IsConnected)
    {
        throw new RevitPipeException("Client is not connected.");
    }

    CancelCount++;
    return Task.FromResult(CancelResult);
}
```

### 2.3 Rationale & Impact
- Adheres to `IRevitPipeClient` contract.
- Throws `RevitPipeException` when `IsConnected == false`, matching real behavior of `RevitPipeClient.EnsureConnected()`.
- Tracks `CancelCount` and allows controlling `CancelResult` for deterministic adversarial testing.
- Restores 0 compilation errors across the solution.

---

## 3. Design of `BimModelSchemaValidator` & `SchemaValidationResult`

### 3.1 Architecture & Governance
- **Location**: `HPBim/src/HPBim.Core/Validation/`
- **Target Framework**: `netstandard2.0` (zero Revit API references, pure C#).
- **Clean Code Rules** (`HP_CLEAN_CODE_CORE.md`):
  - Every method $\le 50$ lines.
  - Every class $\le 300$ lines.
  - Single Responsibility Principle: Validator inspects structure, schema metadata, required elements, and referential integrity; domain QC (TCVN covers, clashes, clearances) remains in `QcEngine`.

### 3.2 Error Code Registry
All validation errors are formatted with standardized machine-readable prefixes:

| Error Code | Description | Trigger Condition |
| :--- | :--- | :--- |
| `ERR_EMPTY_PAYLOAD` | Empty or whitespace payload | Input JSON is null, empty string, or whitespace. |
| `ERR_INVALID_JSON` | Malformed JSON syntax | Input string cannot be parsed by `JsonDocument`. |
| `ERR_INVALID_ROOT` | Non-object root | Root token is an Array, String, Number, or Boolean. |
| `ERR_RAW_CAD_PAYLOAD` | Raw CAD data detected | Root object contains CAD signatures (`cad`, `blockName`, `entities`, `layers`, `dwg`, `handles`, `modelSpace`). |
| `ERR_EMPTY_MODEL` | Empty object bypass | Root object has no BIM properties, or model contains 0 levels and 0 elements (`{}`). |
| `ERR_MISSING_ARTIFACT_TYPE` | Missing artifact discriminant | Property `artifactType` is missing when strict schema discriminant is required. |
| `ERR_INVALID_ARTIFACT_TYPE` | Unrecognized artifact type | Value of `artifactType` is not in allowed list (`HPBim.Model.v1`, `BimModel`, `HPBim.Model`). |
| `ERR_UNSUPPORTED_VERSION` | Unsupported schema version | Value of `schemaVersion` is not compatible with supported versions (`1.0`, `v1`). |
| `ERR_MISSING_LEVELS` | No levels defined | `Levels` collection is null or empty (`Levels.Count == 0`). |
| `ERR_DUPLICATE_LEVEL` | Duplicate level name | Two or more levels have identical names (case-insensitive). |
| `ERR_NO_ELEMENTS` | No physical elements | Model defines levels but 0 grids, 0 columns, 0 walls, 0 beams, 0 slabs, 0 rooms, 0 fixtures. |
| `ERR_BROKEN_REFERENCE` | Dangling referential dependency | Element references a level, wall, or host that does not exist in the model. |
| `ERR_INVALID_GEOMETRY` | Degenerate geometry | Element has zero/negative length, zero thickness, or non-finite coordinates. |

---

### 3.3 Complete Source Specification: `SchemaValidationResult.cs`

File path: `HPBim/src/HPBim.Core/Validation/SchemaValidationResult.cs`

```csharp
namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the result of an artifact and schema validation check.
/// </summary>
public sealed record SchemaValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string? DetectedArtifactType { get; init; }
    public string? DetectedSchemaVersion { get; init; }

    public static SchemaValidationResult Success(
        string? artifactType = null,
        string? schemaVersion = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            Errors = Array.Empty<string>(),
            Warnings = warnings ?? Array.Empty<string>(),
            DetectedArtifactType = artifactType,
            DetectedSchemaVersion = schemaVersion
        };

    public static SchemaValidationResult Failure(
        IEnumerable<string> errors,
        string? artifactType = null,
        string? schemaVersion = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            Errors = new List<string>(errors),
            Warnings = warnings ?? Array.Empty<string>(),
            DetectedArtifactType = artifactType,
            DetectedSchemaVersion = schemaVersion
        };

    public static SchemaValidationResult Failure(string singleError) =>
        Failure(new[] { singleError });
}
```

---

### 3.4 Complete Source Specification: `SchemaValidationException.cs`

File path: `HPBim/src/HPBim.Core/Validation/SchemaValidationException.cs`

```csharp
namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;

/// <summary>
/// Exception thrown when strict schema validation fails during deserialization.
/// </summary>
public sealed class SchemaValidationException : Exception
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public SchemaValidationException(IReadOnlyList<string> errors)
        : base($"BIM Model schema validation failed with {errors.Count} error(s): {string.Join("; ", errors)}")
    {
        ValidationErrors = errors ?? Array.Empty<string>();
    }

    public SchemaValidationException(string singleError)
        : this(new[] { singleError })
    {
    }
}
```

---

### 3.5 Complete Source Specification: `BimModelSchemaValidator.cs`

File path: `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs`

```csharp
namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Models;

/// <summary>
/// Pure C# schema and artifact validator for HPBim models.
/// Enforces schema versioning, artifact discrimination, non-empty collections,
/// and cross-entity referential integrity prior to deserialization or Revit dispatch.
/// </summary>
public static class BimModelSchemaValidator
{
    private static readonly HashSet<string> AllowedArtifactTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "HPBim.Model.v1",
        "HPBim.Model",
        "BimModel"
    };

    private static readonly HashSet<string> AllowedSchemaVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        "1.0",
        "1.0.0",
        "v1",
        "1.1"
    };

    private static readonly string[] CadRootSignatures =
    {
        "cad", "blockName", "entities", "layers", "dwg", "handles", "modelSpace", "polylines"
    };

    /// <summary>
    /// Validates raw JSON string: checks syntax, root type, CAD signatures,
    /// artifact discriminants, and then deserializes to check domain referential integrity.
    /// </summary>
    public static SchemaValidationResult Validate(string json, bool requireStrictDiscriminant = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return SchemaValidationResult.Failure("[ERR_EMPTY_PAYLOAD] JSON payload cannot be null, empty, or whitespace.");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return SchemaValidationResult.Failure($"[ERR_INVALID_JSON] Malformed JSON syntax: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return SchemaValidationResult.Failure($"[ERR_INVALID_ROOT] Root element must be a JSON Object, found {root.ValueKind}.");
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // 1. Check CAD signatures
            CheckRawCadSignatures(root, errors);
            if (errors.Count > 0)
            {
                return SchemaValidationResult.Failure(errors);
            }

            // 2. Check empty object bypass
            if (IsEmptyJsonObject(root))
            {
                return SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Input is an empty JSON object with no BIM model properties.");
            }

            // 3. Extract and validate schema metadata
            string? artifactType = GetStringProperty(root, "artifactType") ?? GetStringProperty(root, "format");
            string? schemaVersion = GetStringProperty(root, "schemaVersion") ?? GetStringProperty(root, "version");

            ValidateArtifactMetadata(artifactType, schemaVersion, requireStrictDiscriminant, errors, warnings);
            if (errors.Count > 0)
            {
                return SchemaValidationResult.Failure(errors, artifactType, schemaVersion, warnings);
            }

            // 4. Validate top-level collections in JSON
            ValidateJsonCollections(root, errors);
            if (errors.Count > 0)
            {
                return SchemaValidationResult.Failure(errors, artifactType, schemaVersion, warnings);
            }

            // 5. Deserialize to model and perform deep referential integrity checks
            BimModel? model;
            try
            {
                model = JsonSerializer.Deserialize<BimModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return SchemaValidationResult.Failure($"[ERR_INVALID_SCHEMA] Failed to deserialize BimModel structure: {ex.Message}");
            }

            if (model == null)
            {
                return SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Deserialization produced a null model instance.");
            }

            ValidateModel(model, errors, warnings);

            return errors.Count > 0
                ? SchemaValidationResult.Failure(errors, artifactType, schemaVersion, warnings)
                : SchemaValidationResult.Success(artifactType, schemaVersion, warnings);
        }
    }

    /// <summary>
    /// Validates an in-memory BimModel instance for referential integrity and non-empty collections.
    /// </summary>
    public static SchemaValidationResult ValidateModel(BimModel model)
    {
        if (model == null)
        {
            return SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Model instance is null.");
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateModel(model, errors, warnings);

        return errors.Count > 0
            ? SchemaValidationResult.Failure(errors, model.ArtifactType, model.SchemaVersion, warnings)
            : SchemaValidationResult.Success(model.ArtifactType, model.SchemaVersion, warnings);
    }

    private static void CheckRawCadSignatures(JsonElement root, List<string> errors)
    {
        foreach (var sig in CadRootSignatures)
        {
            if (root.TryGetProperty(sig, out _))
            {
                errors.Add($"[ERR_RAW_CAD_PAYLOAD] Detected raw CAD property '{sig}' at root. Raw CAD JSON files cannot be processed directly as BIM models.");
                return;
            }
        }
    }

    private static bool IsEmptyJsonObject(JsonElement root)
    {
        using var enumerator = root.EnumerateObject();
        return !enumerator.MoveNext();
    }

    private static void ValidateArtifactMetadata(
        string? artifactType, string? schemaVersion, bool strict, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(artifactType))
        {
            if (strict)
            {
                errors.Add("[ERR_MISSING_ARTIFACT_TYPE] Missing required 'artifactType' property (expected 'HPBim.Model.v1' or 'BimModel').");
            }
            else
            {
                warnings.Add("Missing 'artifactType' property; defaulting to 'HPBim.Model.v1'.");
            }
        }
        else if (!AllowedArtifactTypes.Contains(artifactType))
        {
            errors.Add($"[ERR_INVALID_ARTIFACT_TYPE] Unsupported artifactType '{artifactType}'. Allowed values: {string.Join(", ", AllowedArtifactTypes)}.");
        }

        if (!string.IsNullOrWhiteSpace(schemaVersion) && !AllowedSchemaVersions.Contains(schemaVersion))
        {
            errors.Add($"[ERR_UNSUPPORTED_VERSION] Unsupported schemaVersion '{schemaVersion}'. Allowed values: {string.Join(", ", AllowedSchemaVersions)}.");
        }
    }

    private static void ValidateJsonCollections(JsonElement root, List<string> errors)
    {
        if (!root.TryGetProperty("levels", out var levelsElem) || levelsElem.ValueKind != JsonValueKind.Array || levelsElem.GetArrayLength() == 0)
        {
            errors.Add("[ERR_MISSING_LEVELS] The model must define at least one level in 'levels'.");
        }
    }

    private static void ValidateModel(BimModel model, List<string> errors, List<string> warnings)
    {
        // 1. Levels validation
        if (model.Levels == null || model.Levels.Count == 0)
        {
            errors.Add("[ERR_MISSING_LEVELS] Model contains 0 levels. At least 1 Level is required.");
            return;
        }

        var levelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lvl in model.Levels)
        {
            if (string.IsNullOrWhiteSpace(lvl.Name))
            {
                errors.Add("[ERR_INVALID_LEVEL] Level contains empty or whitespace Name.");
            }
            else if (!levelNames.Add(lvl.Name))
            {
                errors.Add($"[ERR_DUPLICATE_LEVEL] Duplicate level name detected: '{lvl.Name}'.");
            }

            if (double.IsNaN(lvl.ElevationM) || double.IsInfinity(lvl.ElevationM))
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Level '{lvl.Name}' has non-finite elevation: {lvl.ElevationM}.");
            }
        }

        // 2. Minimum physical content check
        int physicalCount = (model.Grids?.Count ?? 0)
                          + (model.Columns?.Count ?? 0)
                          + (model.Walls?.Count ?? 0)
                          + (model.Beams?.Count ?? 0)
                          + (model.Slabs?.Count ?? 0)
                          + (model.Rooms?.Count ?? 0)
                          + (model.Fixtures?.Count ?? 0);

        if (physicalCount == 0)
        {
            errors.Add("[ERR_NO_ELEMENTS] Model defines levels but contains zero structural, architectural, or MEP elements.");
        }

        // 3. Referential integrity
        ValidateColumns(model.Columns, levelNames, errors);
        ValidateWalls(model.Walls, levelNames, errors);
        ValidateBeams(model.Beams, levelNames, errors);
        ValidateSlabs(model.Slabs, levelNames, errors);
        ValidateRooms(model.Rooms, levelNames, errors);
        ValidateFixtures(model.Fixtures, levelNames, errors);
        ValidatePipes(model.Pipes, levelNames, errors);
        ValidateRebars(model.Rebars, model, errors);
    }

    private static void ValidateColumns(IReadOnlyList<ColumnDef>? columns, HashSet<string> levels, List<string> errors)
    {
        if (columns == null) return;
        foreach (var c in columns)
        {
            if (string.IsNullOrWhiteSpace(c.BaseLevel) || !levels.Contains(c.BaseLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Column '{c.Id}' references non-existent BaseLevel '{c.BaseLevel}'.");
            }
            if (!string.IsNullOrWhiteSpace(c.TopLevel) && !levels.Contains(c.TopLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Column '{c.Id}' references non-existent TopLevel '{c.TopLevel}'.");
            }
            if (c.WidthMm <= 0 || c.DepthMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Column '{c.Id}' has invalid cross-section dimensions: {c.WidthMm}x{c.DepthMm}mm.");
            }
        }
    }

    private static void ValidateWalls(IReadOnlyList<WallDef>? walls, HashSet<string> levels, List<string> errors)
    {
        if (walls == null) return;
        foreach (var w in walls)
        {
            if (string.IsNullOrWhiteSpace(w.BaseLevel) || !levels.Contains(w.BaseLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Wall '{w.Id}' references non-existent BaseLevel '{w.BaseLevel}'.");
            }
            if (!string.IsNullOrWhiteSpace(w.TopLevel) && !levels.Contains(w.TopLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Wall '{w.Id}' references non-existent TopLevel '{w.TopLevel}'.");
            }
            if (w.LengthMm < 1.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Wall '{w.Id}' has degenerate length {w.LengthMm:F1}mm (< 1.0mm).");
            }
            if (w.ThicknessMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Wall '{w.Id}' has invalid thickness {w.ThicknessMm}mm.");
            }

            if (w.Openings != null)
            {
                foreach (var op in w.Openings)
                {
                    if (op.WidthMm <= 0 || op.HeightMm <= 0)
                    {
                        errors.Add($"[ERR_INVALID_GEOMETRY] Opening '{op.Id}' on wall '{w.Id}' has invalid size {op.WidthMm}x{op.HeightMm}mm.");
                    }
                    if (op.DistanceAlongWallMm < 0 || op.DistanceAlongWallMm + op.WidthMm > w.LengthMm + 10.0)
                    {
                        errors.Add($"[ERR_INVALID_GEOMETRY] Opening '{op.Id}' extends outside wall '{w.Id}' boundary (Offset {op.DistanceAlongWallMm:F0}mm + Width {op.WidthMm:F0}mm > Wall Length {w.LengthMm:F0}mm).");
                    }
                }
            }
        }
    }

    private static void ValidateBeams(IReadOnlyList<BeamDef>? beams, HashSet<string> levels, List<string> errors)
    {
        if (beams == null) return;
        foreach (var b in beams)
        {
            if (string.IsNullOrWhiteSpace(b.Level) || !levels.Contains(b.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Beam '{b.Id}' references non-existent Level '{b.Level}'.");
            }
            if (b.LengthMm < 1.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Beam '{b.Id}' has degenerate length {b.LengthMm:F1}mm.");
            }
        }
    }

    private static void ValidateSlabs(IReadOnlyList<SlabDef>? slabs, HashSet<string> levels, List<string> errors)
    {
        if (slabs == null) return;
        foreach (var s in slabs)
        {
            if (string.IsNullOrWhiteSpace(s.Level) || !levels.Contains(s.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Slab '{s.Id}' references non-existent Level '{s.Level}'.");
            }
            if (s.ThicknessMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Slab '{s.Id}' has invalid thickness {s.ThicknessMm}mm.");
            }
        }
    }

    private static void ValidateRooms(IReadOnlyList<RoomDef>? rooms, HashSet<string> levels, List<string> errors)
    {
        if (rooms == null) return;
        foreach (var r in rooms)
        {
            if (string.IsNullOrWhiteSpace(r.Level) || !levels.Contains(r.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Room '{r.Name}' references non-existent Level '{r.Level}'.");
            }
        }
    }

    private static void ValidateFixtures(IReadOnlyList<MepFixtureDef>? fixtures, HashSet<string> levels, List<string> errors)
    {
        if (fixtures == null) return;
        foreach (var f in fixtures)
        {
            if (string.IsNullOrWhiteSpace(f.Level) || !levels.Contains(f.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Fixture '{f.Id}' references non-existent Level '{f.Level}'.");
            }
        }
    }

    private static void ValidatePipes(IReadOnlyList<PipeRunDef>? pipes, HashSet<string> levels, List<string> errors)
    {
        if (pipes == null) return;
        foreach (var p in pipes)
        {
            if (!string.IsNullOrWhiteSpace(p.Level) && !levels.Contains(p.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Pipe '{p.Id}' references non-existent Level '{p.Level}'.");
            }
            if (p.Points == null || p.Points.Count < 2)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Pipe '{p.Id}' has fewer than 2 route points.");
            }
        }
    }

    private static void ValidateRebars(IReadOnlyList<RebarDef>? rebars, BimModel model, List<string> errors)
    {
        if (rebars == null || rebars.Count == 0) return;
        var validHostIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (model.Columns != null) foreach (var c in model.Columns) validHostIds.Add(c.Id);
        if (model.Beams != null) foreach (var b in model.Beams) validHostIds.Add(b.Id);
        if (model.Walls != null) foreach (var w in model.Walls) validHostIds.Add(w.Id);
        if (model.Slabs != null) foreach (var s in model.Slabs) validHostIds.Add(s.Id);

        foreach (var rb in rebars)
        {
            if (!string.IsNullOrWhiteSpace(rb.HostElementId) && !validHostIds.Contains(rb.HostElementId))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Rebar '{rb.Id}' references non-existent HostElementId '{rb.HostElementId}'.");
            }
            if (rb.DiameterMm <= 0 || rb.LengthMm <= 0 || rb.Quantity <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Rebar '{rb.Id}' has invalid geometry parameters (Dia: {rb.DiameterMm}, Length: {rb.LengthMm}, Qty: {rb.Quantity}).");
            }
        }
    }

    private static string? GetStringProperty(JsonElement element, string propName)
    {
        if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }
}
```

---

## 4. Integration Specification: ValidateCommand & BimModelSerializer

### 4.1 Updates to `BimModel.cs`
Add metadata properties with safe defaults so any newly instantiated or serialized `BimModel` automatically includes versioning and artifact discriminant:

```csharp
// In HPBim.Core/Models/BimModel.cs:
public string SchemaVersion { get; init; } = "1.0";
public string ArtifactType { get; init; } = "HPBim.Model.v1";
```

### 4.2 Updates to `BimModelSerializer.cs`
Add strict validated deserialization methods without altering existing methods (preventing regression in existing tests such as `BimModelSerializer_EmptyJsonObject_ProducesValidModel`):

```csharp
// In HPBim.Core/Serialization/BimModelSerializer.cs:

/// <summary>
/// Deserializes JSON after strictly validating against BimModelSchemaValidator.
/// Throws SchemaValidationException if the JSON is raw CAD, empty, or violates schema rules.
/// </summary>
public static BimModel DeserializeValidated(string json, bool requireStrictDiscriminant = false)
{
    var validationResult = BimModelSchemaValidator.Validate(json, requireStrictDiscriminant);
    if (!validationResult.IsValid)
    {
        throw new SchemaValidationException(validationResult.Errors);
    }

    return Deserialize(json);
}

/// <summary>
/// Asynchronously reads file and deserializes after strict schema validation.
/// </summary>
public static async Task<BimModel> DeserializeValidatedFromFileAsync(
    string filePath, bool requireStrictDiscriminant = false, CancellationToken ct = default)
{
    if (!File.Exists(filePath))
    {
        throw new FileNotFoundException($"BIM model JSON file not found at: {filePath}", filePath);
    }

    using var reader = new StreamReader(filePath);
    string json = await reader.ReadToEndAsync().ConfigureAwait(false);
    return DeserializeValidated(json, requireStrictDiscriminant);
}
```

### 4.3 Refactoring `ValidateCommand.cs`
In `HPBim.Cli/Commands/ValidateCommand.cs`, insert the schema validation gate before domain QC:

```csharp
// In HPBim.Cli/Commands/ValidateCommand.cs ExecuteAsync:
_formatter.WriteBanner("Validate BIM Model", $"Kiểm tra hợp chuẩn và va chạm offline: {inputPath}");

string json = await File.ReadAllTextAsync(inputPath, ct).ConfigureAwait(false);

// Step 1: Strict Artifact & Schema Validation Gate
var schemaResult = BimModelSchemaValidator.Validate(json);
if (!schemaResult.IsValid)
{
    _formatter.WriteError("THẤT BẠI: Dữ liệu không đạt chuẩn Schema BIM.");
    var errorRows = schemaResult.Errors.Select((err, idx) => new[] { (idx + 1).ToString(), err }).ToArray();
    _formatter.RenderTable(["STT", "Chi tiết lỗi Schema"], errorRows);
    _formatter.WriteError("TRẠNG THÁI: KHÔNG HỢP LỆ (Schema Rejected). Không thể dispatch sang Revit.");
    return 1;
}

// Step 2: Deserialization into Strongly-Typed Domain Model
var model = BimModelSerializer.Deserialize(json);

// Step 3: Domain & TCVN QC Engine
var qcEngine = new QcEngine();
var report = qcEngine.Validate(model);

RenderValidationSummary(report);
return report.IsValid ? 0 : 1;
```

### 4.4 Hardening `GenerateCommand.cs`
In `HPBim.Cli/Commands/GenerateCommand.cs`, use `DeserializeValidatedFromFileAsync` so that running `hpbim generate --input scratch/piles_cad_vs_revit_inspection.json` or `hpbim generate --input empty.json` fails immediately with an informative message instead of generating 0 elements and claiming success.

---

## 5. Specification of Unit Test Suite: `BimModelSchemaValidatorTests.cs`

File path: `HPBim/tests/HPBim.Core.Tests/Validation/BimModelSchemaValidatorTests.cs`  
Target: `net10.0`, xUnit v3 / Microsoft.Testing.Platform.

### 5.1 Test Cases Matrix

| Method Name | Category | Input Scenario | Expected Outcome |
| :--- | :--- | :--- | :--- |
| `Validate_RawCadInspectionJson_FailsWithCadError` | CAD Rejection | Root `{ "cad": { "blockName": "Rec-TT" } }` | `!IsValid`, contains `[ERR_RAW_CAD_PAYLOAD]` |
| `Validate_RawCadEntitiesJson_FailsWithCadError` | CAD Rejection | Root `{ "entities": [ { "type": "Line" } ] }` | `!IsValid`, contains `[ERR_RAW_CAD_PAYLOAD]` |
| `Validate_RawDwgHeaderJson_FailsWithCadError` | CAD Rejection | Root `{ "dwg": "drawing.dwg", "layers": [] }` | `!IsValid`, contains `[ERR_RAW_CAD_PAYLOAD]` |
| `Validate_EmptyJsonObject_FailsWithEmptyModelError` | Empty Rejection | `{}` | `!IsValid`, contains `[ERR_EMPTY_MODEL]` |
| `Validate_NullOrWhitespaceJson_FailsWithEmptyPayloadError` | Empty Rejection | `""`, `"   "`, `null!` | `!IsValid`, contains `[ERR_EMPTY_PAYLOAD]` |
| `Validate_MalformedJsonSyntax_FailsWithInvalidJsonError` | Syntax Check | `"{ not a json:"` | `!IsValid`, contains `[ERR_INVALID_JSON]` |
| `Validate_NonObjectJsonArray_FailsWithInvalidRootError` | Root Check | `"[1, 2, 3]"` | `!IsValid`, contains `[ERR_INVALID_ROOT]` |
| `Validate_MissingArtifactType_FailsUnderStrictMode` | Discriminant | Missing `artifactType` when strict = true | `!IsValid`, contains `[ERR_MISSING_ARTIFACT_TYPE]` |
| `Validate_UnsupportedArtifactType_Fails` | Discriminant | `artifactType: "AutoCAD.Drawing"` | `!IsValid`, contains `[ERR_INVALID_ARTIFACT_TYPE]` |
| `Validate_UnsupportedSchemaVersion_Fails` | Versioning | `schemaVersion: "99.0"` | `!IsValid`, contains `[ERR_UNSUPPORTED_VERSION]` |
| `Validate_ModelWithZeroLevels_FailsWithMissingLevelsError` | Hierarchy | Model has columns but `levels: []` | `!IsValid`, contains `[ERR_MISSING_LEVELS]` |
| `Validate_DuplicateLevelNames_FailsWithDuplicateError` | Level Integrity | Two levels named `"TRET"` | `!IsValid`, contains `[ERR_DUPLICATE_LEVEL]` |
| `Validate_OnlyLevelsNoElements_FailsWithNoElementsError` | Empty Elements | 1 level, 0 grids, 0 elements | `!IsValid`, contains `[ERR_NO_ELEMENTS]` |
| `Validate_ColumnReferencingNonExistentBaseLevel_Fails` | Ref Integrity | Column references `BaseLevel: "Ghost_Level"` | `!IsValid`, contains `[ERR_BROKEN_REFERENCE]` |
| `Validate_WallReferencingNonExistentBaseLevel_Fails` | Ref Integrity | Wall references `BaseLevel: "Missing_Level"` | `!IsValid`, contains `[ERR_BROKEN_REFERENCE]` |
| `Validate_BeamReferencingNonExistentLevel_Fails` | Ref Integrity | Beam references `Level: "Unknown_Level"` | `!IsValid`, contains `[ERR_BROKEN_REFERENCE]` |
| `Validate_OpeningExceedingWallLength_Fails` | Geometry | Opening at 12m on a 6m wall | `!IsValid`, contains `[ERR_INVALID_GEOMETRY]` |
| `Validate_RebarReferencingNonExistentHost_Fails` | Ref Integrity | Rebar with `HostElementId: "Invalid_Host"` | `!IsValid`, contains `[ERR_BROKEN_REFERENCE]` |
| `Validate_NhaPho5x18mBenchmark_PassesCleanly` | Benchmark | Full `NhaPho5x18mFixture.CreateModel()` | `IsValid == true`, `Errors` is empty |
| `DeserializeValidated_ThrowsSchemaValidationException_OnCadJson` | Serializer API | Passing CAD JSON to `DeserializeValidated` | Throws `SchemaValidationException` |

---

### 5.2 Complete Source Specification: `BimModelSchemaValidatorTests.cs`

```csharp
namespace HPBim.Core.Tests.Validation;

using System;
using System.IO;
using System.Threading.Tasks;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPBim.Core.Validation;
using Xunit;

public sealed class BimModelSchemaValidatorTests
{
    [Fact]
    public void Validate_RawCadInspectionJson_FailsWithCadError()
    {
        string rawCadJson = """
        {
            "cad": {
                "found": true,
                "blockName": "Rec-TT",
                "entityCount": 4
            }
        }
        """;

        var result = BimModelSchemaValidator.Validate(rawCadJson);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RAW_CAD_PAYLOAD"));
    }

    [Fact]
    public void Validate_RawCadEntitiesJson_FailsWithCadError()
    {
        string rawCadJson = """
        {
            "blockName": "Rec-TT",
            "entities": [
                { "type": "Polyline", "layer": "Rec-TT" }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(rawCadJson);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RAW_CAD_PAYLOAD"));
    }

    [Fact]
    public void Validate_EmptyJsonObject_FailsWithEmptyModelError()
    {
        var result = BimModelSchemaValidator.Validate("{}");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EMPTY_MODEL"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_NullOrWhitespace_FailsWithEmptyPayloadError(string? payload)
    {
        var result = BimModelSchemaValidator.Validate(payload!);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EMPTY_PAYLOAD"));
    }

    [Fact]
    public void Validate_MalformedJsonSyntax_FailsWithInvalidJsonError()
    {
        var result = BimModelSchemaValidator.Validate("{ not a json: 123");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_JSON"));
    }

    [Fact]
    public void Validate_NonObjectRoot_FailsWithInvalidRootError()
    {
        var result = BimModelSchemaValidator.Validate("[ 1, 2, 3 ]");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_ROOT"));
    }

    [Fact]
    public void Validate_MissingArtifactType_FailsWhenStrict()
    {
        string json = """
        {
            "projectName": "Test",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ],
            "grids": [ { "name": "X1", "coordinateMm": 0 } ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(json, requireStrictDiscriminant: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MISSING_ARTIFACT_TYPE"));
    }

    [Fact]
    public void Validate_UnsupportedArtifactType_Fails()
    {
        string json = """
        {
            "artifactType": "AutoCAD.BlockTable",
            "schemaVersion": "1.0",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_ARTIFACT_TYPE"));
    }

    [Fact]
    public void Validate_UnsupportedSchemaVersion_Fails()
    {
        string json = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "99.0",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_UNSUPPORTED_VERSION"));
    }

    [Fact]
    public void Validate_ModelWithZeroLevels_Fails()
    {
        string json = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [],
            "grids": [ { "name": "X1", "coordinateMm": 0 } ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MISSING_LEVELS"));
    }

    [Fact]
    public void Validate_DuplicateLevelNames_Fails()
    {
        var model = new BimModel
        {
            ProjectName = "Dup Level Test",
            Levels = new[]
            {
                new LevelDef("TRET", 0.0, 3600, 1),
                new LevelDef("tret", 3.6, 3600, 2)
            },
            Grids = new[]
            {
                new AxisGrid("1", 0, GridDirection.X, new Point2D(0, 0), new Point2D(0, 1000))
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_DUPLICATE_LEVEL"));
    }

    [Fact]
    public void Validate_OnlyLevelsNoElements_Fails()
    {
        var model = new BimModel
        {
            ProjectName = "Empty Elements",
            Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_NO_ELEMENTS"));
    }

    [Fact]
    public void Validate_ColumnReferencingNonExistentBaseLevel_Fails()
    {
        var model = new BimModel
        {
            ProjectName = "Broken Ref Test",
            Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
            Columns = new[]
            {
                new ColumnDef
                {
                    Id = "C1",
                    BaseLevel = "NON_EXISTENT_LEVEL",
                    WidthMm = 220,
                    DepthMm = 220
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_BROKEN_REFERENCE") && e.Contains("NON_EXISTENT_LEVEL"));
    }

    [Fact]
    public void Validate_WallReferencingNonExistentBaseLevel_Fails()
    {
        var model = new BimModel
        {
            ProjectName = "Broken Wall Ref",
            Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
            Walls = new[]
            {
                new WallDef
                {
                    Id = "W1",
                    BaseLevel = "MISSING_LEVEL",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 220
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_BROKEN_REFERENCE") && e.Contains("MISSING_LEVEL"));
    }

    [Fact]
    public void Validate_OpeningExceedingWallLength_Fails()
    {
        var model = new BimModel
        {
            ProjectName = "Opening Out of Bounds",
            Levels = new[] { new LevelDef("TRET", 0.0, 3600, 1) },
            Walls = new[]
            {
                new WallDef
                {
                    Id = "W1",
                    BaseLevel = "TRET",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(3000, 0), // Length = 3000mm
                    ThicknessMm = 220,
                    Openings = new[]
                    {
                        new OpeningDef
                        {
                            Id = "D1",
                            DistanceAlongWallMm = 2500,
                            WidthMm = 900, // 2500 + 900 = 3400 > 3000
                            HeightMm = 2200
                        }
                    }
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY") && e.Contains("D1"));
    }

    [Fact]
    public void Validate_NhaPho5x18mBenchmark_PassesCleanly()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        string json = BimModelSerializer.Serialize(model);

        var result = BimModelSchemaValidator.Validate(json);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DeserializeValidated_ThrowsSchemaValidationException_OnCadJson()
    {
        string cadJson = """{ "cad": { "blockName": "Rec-TT" } }""";

        var ex = Assert.Throws<SchemaValidationException>(() =>
            BimModelSerializer.DeserializeValidated(cadJson));

        Assert.Contains(ex.ValidationErrors, e => e.Contains("ERR_RAW_CAD_PAYLOAD"));
    }

    [Fact]
    public void DeserializeValidated_ThrowsSchemaValidationException_OnEmptyJson()
    {
        var ex = Assert.Throws<SchemaValidationException>(() =>
            BimModelSerializer.DeserializeValidated("{}"));

        Assert.Contains(ex.ValidationErrors, e => e.Contains("ERR_EMPTY_MODEL"));
    }

    [Fact]
    public void DeserializeValidated_SucceedsOnValidModel()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        string json = BimModelSerializer.Serialize(model);

        var restored = BimModelSerializer.DeserializeValidated(json);

        Assert.NotNull(restored);
        Assert.Equal(model.ProjectName, restored.ProjectName);
        Assert.Equal(model.Levels.Count, restored.Levels.Count);
    }
}
```

---

## 6. Implementation Checklist & Verification Plan for Implementer

| Task # | Action | Target File | Verification Method |
| :--- | :--- | :--- | :--- |
| **T1** | Implement `CancelAsync` and properties in fake | `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs` | `dotnet build HPBim/tests/HPBim.Client.Tests/` succeeds with 0 errors. |
| **T2** | Add `SchemaVersion` and `ArtifactType` properties | `HPBim/src/HPBim.Core/Models/BimModel.cs` | `dotnet test HPBim/tests/HPBim.Core.Tests/` continues 268/268 PASS. |
| **T3** | Create `SchemaValidationResult.cs` | `HPBim/src/HPBim.Core/Validation/SchemaValidationResult.cs` | Compiles under `netstandard2.0`. |
| **T4** | Create `SchemaValidationException.cs` | `HPBim/src/HPBim.Core/Validation/SchemaValidationException.cs` | Compiles under `netstandard2.0`. |
| **T5** | Create `BimModelSchemaValidator.cs` | `HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs` | Compiles under `netstandard2.0` with 0 Revit references. |
| **T6** | Add `DeserializeValidated` methods | `HPBim/src/HPBim.Core/Serialization/BimModelSerializer.cs` | All existing serializer tests continue to PASS. |
| **T7** | Integrate schema gate into `ValidateCommand` | `HPBim/src/HPBim.Cli/Commands/ValidateCommand.cs` | `ValidateCommand` rejects `{}` and raw CAD with code 1. |
| **T8** | Create unit test suite | `HPBim/tests/HPBim.Core.Tests/Validation/BimModelSchemaValidatorTests.cs` | `dotnet test` executes all new tests green (~20 new tests, 288+ total Core tests). |
| **T9** | Run full solution test suite | Root `HPBim/` | `dotnet test HPBim.slnx` runs all Core and Client tests green (370+ tests). |
