import re

proposed_BimModelSchemaValidator = """namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPBim.Core.Models;

/// <summary>
/// Pure C# schema and artifact validator for HPBim models.
/// Enforces schema versioning, artifact discrimination, non-empty collections,
/// and cross-entity referential integrity prior to deserialization or Revit dispatch.
/// </summary>
public static partial class BimModelSchemaValidator
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

    private static readonly HashSet<string> CadRootSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "cad", "blockName", "entities", "layers", "dwg", "handles", "modelSpace", "polylines",
        "drawing", "blocks", "blockDefinitionName", "totalPolylines", "allPolylines", "dxf", "paperSpace"
    };

    public static SchemaValidationResult Validate(string json, bool requireStrictDiscriminant = false)
    {
        if (!TryParseJsonDocument(json, out var doc, out var parseError))
        {
            return parseError!;
        }

        using (doc!)
        {
            var root = doc.RootElement;
            if (!CheckCadAndEmpty(root, out var payloadError))
            {
                return payloadError!;
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            string? type = GetStringProperty(root, "artifactType") ?? GetStringProperty(root, "format");
            string? ver = GetStringProperty(root, "schemaVersion") ?? GetStringProperty(root, "version");

            ValidateArtifactMetadata(type, ver, requireStrictDiscriminant, errors, warnings);
            if (errors.Count > 0) return SchemaValidationResult.Failure(errors, type, ver, warnings);

            ValidateJsonCollections(root, errors);
            if (errors.Count > 0) return SchemaValidationResult.Failure(errors, type, ver, warnings);

            return DeserializeAndValidateModel(json, type, ver, errors, warnings);
        }
    }

    private static bool TryParseJsonDocument(string json, out JsonDocument? doc, out SchemaValidationResult? failure)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            doc = null;
            failure = SchemaValidationResult.Failure("[ERR_EMPTY_PAYLOAD] JSON payload cannot be null, empty, or whitespace.");
            return false;
        }

        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            doc = null;
            failure = SchemaValidationResult.Failure($"[ERR_INVALID_JSON] Malformed JSON syntax: {ex.Message}");
            return false;
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            failure = SchemaValidationResult.Failure($"[ERR_INVALID_ROOT] Root element must be a JSON Object, found {doc.RootElement.ValueKind}.");
            doc.Dispose();
            doc = null;
            return false;
        }

        failure = null;
        return true;
    }

    private static bool CheckCadAndEmpty(JsonElement root, out SchemaValidationResult? failure)
    {
        var errors = new List<string>();
        CheckRawCadSignatures(root, errors);
        if (errors.Count > 0)
        {
            failure = SchemaValidationResult.Failure(errors);
            return false;
        }

        if (IsEmptyJsonObject(root))
        {
            failure = SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Input is an empty JSON object with no BIM model properties.");
            return false;
        }

        failure = null;
        return true;
    }

    private static readonly JsonSerializerOptions ModelDeserializerOptions = CreateDeserializerOptions();

    private static JsonSerializerOptions CreateDeserializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    private static SchemaValidationResult DeserializeAndValidateModel(
        string json, string? artifactType, string? schemaVersion, List<string> errors, List<string> warnings)
    {
        BimModel? model;
        try
        {
            model = JsonSerializer.Deserialize<BimModel>(json, ModelDeserializerOptions);
        }
        catch (Exception ex)
        {
            return SchemaValidationResult.Failure($"[ERR_INVALID_SCHEMA] Failed to deserialize BimModel structure: {ex.Message}");
        }

        if (model == null)
        {
            return SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Deserialization produced a null model instance.");
        }

        ValidateModelStructure(model, errors, warnings);

        return errors.Count > 0
            ? SchemaValidationResult.Failure(errors, artifactType, schemaVersion, warnings)
            : SchemaValidationResult.Success(artifactType, schemaVersion, warnings);
    }

    public static SchemaValidationResult ValidateModel(BimModel model)
    {
        if (model == null)
        {
            return SchemaValidationResult.Failure("[ERR_EMPTY_MODEL] Model instance is null.");
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateModelStructure(model, errors, warnings);

        return errors.Count > 0
            ? SchemaValidationResult.Failure(errors, model.ArtifactType, model.SchemaVersion, warnings)
            : SchemaValidationResult.Success(model.ArtifactType, model.SchemaVersion, warnings);
    }

    private static void ValidateModelStructure(BimModel model, List<string> errors, List<string> warnings)
    {
        if (!ValidateLevels(model.Levels, errors, out var levelNames))
        {
            return;
        }

        ValidateElementPresence(model, errors);

        ValidateGrids(model.Grids, errors);
        ValidateColumns(model.Columns, levelNames, errors);
        ValidateWalls(model.Walls, levelNames, errors);
        ValidateBeams(model.Beams, levelNames, errors);
        ValidateSlabs(model.Slabs, levelNames, errors);
        ValidateRooms(model.Rooms, levelNames, errors);
        ValidateFixtures(model.Fixtures, levelNames, errors);
        ValidatePipes(model.Pipes, levelNames, errors);
        ValidateRebars(model.Rebars, model, errors);
    }

    private static bool ValidateLevels(IReadOnlyList<LevelDef>? levels, List<string> errors, out HashSet<string> levelNames)
    {
        levelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (levels == null || levels.Count == 0)
        {
            errors.Add("[ERR_MISSING_LEVELS] Model contains 0 levels. At least 1 Level is required.");
            return false;
        }

        foreach (var lvl in levels)
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

        return true;
    }

    private static void ValidateElementPresence(BimModel model, List<string> errors)
    {
        int physicalCount = (model.Grids?.Count ?? 0)
                          + (model.Columns?.Count ?? 0)
                          + (model.Walls?.Count ?? 0)
                          + (model.Beams?.Count ?? 0)
                          + (model.Slabs?.Count ?? 0)
                          + (model.Rooms?.Count ?? 0)
                          + (model.Fixtures?.Count ?? 0)
                          + (model.Pipes?.Count ?? 0)
                          + (model.Rebars?.Count ?? 0);

        if (physicalCount == 0)
        {
            errors.Add("[ERR_NO_ELEMENTS] Model defines levels but contains zero structural, architectural, or MEP elements.");
        }
    }

    private static void CheckRawCadSignatures(JsonElement root, List<string> errors)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (CadRootSignatures.Contains(prop.Name))
            {
                errors.Add($"[ERR_RAW_CAD_PAYLOAD] Detected raw CAD property '{prop.Name}' at root. Raw CAD JSON files cannot be processed directly as BIM models.");
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
        else if (!AllowedArtifactTypes.Contains(artifactType!))
        {
            errors.Add($"[ERR_INVALID_ARTIFACT_TYPE] Unsupported artifactType '{artifactType}'. Allowed values: {string.Join(", ", AllowedArtifactTypes)}.");
        }

        if (!string.IsNullOrWhiteSpace(schemaVersion) && !AllowedSchemaVersions.Contains(schemaVersion!))
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

    private static string? GetStringProperty(JsonElement element, string propName)
    {
        if (element.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }
}
"""

def analyze_code(title, code):
    print(f"=== {title} ===")
    lines = code.strip().split('\n')
    print(f"Total lines: {len(lines)}")
    method_pattern = re.compile(r'^\s*(public|private|internal|protected)\s+(static\s+)?(async\s+)?([A-Za-z0-9_<>?,\s]+)\s+([A-Za-z0-9_]+)\s*\(')
    i = 0
    while i < len(lines):
        match = method_pattern.match(lines[i])
        if match:
            start_line = i + 1
            method_name = match.group(5)
            brace_count = 0
            found_opening = False
            end_line = start_line
            for j in range(i, len(lines)):
                for ch in lines[j]:
                    if ch == '{':
                        brace_count += 1
                        found_opening = True
                    elif ch == '}':
                        brace_count -= 1
                if found_opening and brace_count == 0:
                    end_line = j + 1
                    break
            count = end_line - start_line + 1
            status = 'PASS (<= 35)' if count <= 35 else 'FAIL (> 35)'
            print(f"  {method_name:<30} Lines: {start_line:3d}..{end_line:3d} ({count:2d} lines) -> {status}")
            i = end_line
        else:
            i += 1

analyze_code("Proposed BimModelSchemaValidator.cs", proposed_BimModelSchemaValidator)
