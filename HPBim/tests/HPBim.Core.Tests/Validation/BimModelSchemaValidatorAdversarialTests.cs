namespace HPBim.Core.Tests.Validation;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Serialization;
using HPBim.Core.Validation;
using Xunit;

public sealed class BimModelSchemaValidatorAdversarialTests
{
    // =========================================================================
    // SECTION 1: Baseline Rejection Integrity (Positive Rejections)
    // =========================================================================

    [Fact]
    public void Baseline_EmptyJsonAndNull_SuccessfullyRejected()
    {
        Assert.False(BimModelSchemaValidator.Validate("{}").IsValid);
        Assert.False(BimModelSchemaValidator.Validate("").IsValid);
        Assert.False(BimModelSchemaValidator.Validate("   ").IsValid);
        Assert.False(BimModelSchemaValidator.Validate(null!).IsValid);
    }

    [Fact]
    public void Baseline_MalformedSyntaxAndNonObjectRoot_SuccessfullyRejected()
    {
        Assert.False(BimModelSchemaValidator.Validate("{ bad syntax").IsValid);
        Assert.False(BimModelSchemaValidator.Validate("[ 1, 2, 3 ]").IsValid);
        Assert.False(BimModelSchemaValidator.Validate("\"plain string\"").IsValid);
        Assert.False(BimModelSchemaValidator.Validate("12345").IsValid);
    }

    [Fact]
    public void Baseline_CorruptedMetadata_SuccessfullyRejected()
    {
        string badVersion = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "999.0",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ],
            "grids": [ { "name": "X1", "coordinateMm": 0 } ]
        }
        """;
        var resVer = BimModelSchemaValidator.Validate(badVersion);
        Assert.False(resVer.IsValid);
        Assert.Contains(resVer.Errors, e => e.Contains("ERR_UNSUPPORTED_VERSION"));

        string badType = """
        {
            "artifactType": "AutoCAD.DWG.Entity",
            "schemaVersion": "1.0",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ],
            "grids": [ { "name": "X1", "coordinateMm": 0 } ]
        }
        """;
        var resType = BimModelSchemaValidator.Validate(badType);
        Assert.False(resType.IsValid);
        Assert.Contains(resType.Errors, e => e.Contains("ERR_INVALID_ARTIFACT_TYPE"));
    }

    [Fact]
    public void Baseline_MissingLevelsAndBrokenReference_SuccessfullyRejected()
    {
        string noLevels = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [],
            "grids": [ { "name": "X1", "coordinateMm": 0 } ]
        }
        """;
        Assert.False(BimModelSchemaValidator.Validate(noLevels).IsValid);

        string brokenRef = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [ { "name": "Level 1", "elevationM": 0.0 } ],
            "columns": [
                { "id": "C1", "baseLevel": "NON_EXISTENT", "widthMm": 220, "depthMm": 220 }
            ]
        }
        """;
        var resBroken = BimModelSchemaValidator.Validate(brokenRef);
        Assert.False(resBroken.IsValid);
        Assert.Contains(resBroken.Errors, e => e.Contains("ERR_BROKEN_REFERENCE"));
    }

    [Fact]
    public void Baseline_LowercaseCadKeywords_SuccessfullyRejected()
    {
        string rawCadJson = """{ "cad": { "found": true, "blockName": "Rec-TT" } }""";
        var res = BimModelSchemaValidator.Validate(rawCadJson);
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.Contains("ERR_RAW_CAD_PAYLOAD"));
    }

    // =========================================================================
    // SECTION 2: Adversarial Attack Vectors (VULNERABILITIES & FALSE POSITIVES)
    // =========================================================================

    [Fact]
    public void Adversarial_RawCadWithUpperCasing_SlipsPastValidation()
    {
        // Attack scenario: CAD export uses PascalCase/UPPERCASE property names.
        // CheckRawCadSignatures uses case-sensitive TryGetProperty("cad", out _).
        string rawCadWithLevels = """
        {
            "DWG": "Drawing1.dwg",
            "CAD": {
                "BlockName": "REC-TT",
                "Entities": [
                    { "Type": "LWPOLYLINE", "Layer": "0" }
                ]
            },
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "grids": [
                { "name": "Axis 1", "coordinateMm": 0 }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(rawCadWithLevels);

        // VERIFIED RESOLUTION: Raw CAD with uppercase properties is properly rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RAW_CAD"));
    }

    [Fact]
    public void Adversarial_CadDrawingWithBlocks_SlipsPastValidation()
    {
        // Attack scenario: CAD payload contains "drawing" and "blocks" (unregistered signatures).
        string cadDrawingPayload = """
        {
            "drawing": "FoundationPlan.dwg",
            "blocks": [
                { "name": "Footing_DC1", "x": 1000, "y": 2000 }
            ],
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "grids": [
                { "name": "X1", "coordinateMm": 0 }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(cadDrawingPayload);

        // VERIFIED RESOLUTION: Raw CAD drawing with blocks is properly rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RAW_CAD"));
    }

    [Fact]
    public void Adversarial_MissingArtifactMetadata_DefaultsToPassWhenNonStrict()
    {
        // Attack scenario: Payload completely lacks artifactType and schemaVersion.
        string noMetadataPayload = """
        {
            "projectName": "Anonymous Model",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "grids": [
                {
                    "name": "X1",
                    "coordinateMm": 0,
                    "direction": "X",
                    "startPoint": { "x": 0.0, "y": 0.0 },
                    "endPoint": { "x": 0.0, "y": 5000.0 }
                }
            ]
        }
        """;

        // ValidateCommand and GenerateCommand call Validate with requireStrictDiscriminant = false
        var result = BimModelSchemaValidator.Validate(noMetadataPayload, requireStrictDiscriminant: false);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("Missing 'artifactType' property"));
    }

    [Fact]
    public void Adversarial_MissingArtifactMetadata_RejectedWhenStrict()
    {
        string noMetadataPayload = """
        {
            "projectName": "Anonymous Model",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "grids": [
                {
                    "name": "X1",
                    "coordinateMm": 0,
                    "direction": "X",
                    "startPoint": { "x": 0.0, "y": 0.0 },
                    "endPoint": { "x": 0.0, "y": 5000.0 }
                }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(noMetadataPayload, requireStrictDiscriminant: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MISSING_ARTIFACT_TYPE"));
    }

    [Fact]
    public void Adversarial_DegenerateGrid_ZeroLengthAndEmptyName_SlipsPastValidation()
    {
        // Attack scenario: Grid has empty Name and zero length (StartPoint == EndPoint).
        string degenerateGridJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "grids": [
                {
                    "name": "",
                    "coordinateMm": 0.0,
                    "direction": "X",
                    "startPoint": { "x": 0.0, "y": 0.0 },
                    "endPoint": { "x": 0.0, "y": 0.0 }
                }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(degenerateGridJson);

        // VERIFIED RESOLUTION: Degenerate grid is rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GRID") || e.Contains("ERR_INVALID_GEOMETRY"));
    }

    [Fact]
    public void Adversarial_DuplicateGrids_SlipsPastValidation()
    {
        // Attack scenario: Multiple grids with the exact same name.
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Grids = new[]
            {
                new AxisGrid("X1", 0, GridDirection.X, new Point2D(0, 0), new Point2D(0, 5000)),
                new AxisGrid("X1", 5000, GridDirection.X, new Point2D(5000, 0), new Point2D(5000, 5000))
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        // VERIFIED RESOLUTION: Duplicate grid names are rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_DUPLICATE_GRID"));
    }

    [Fact]
    public void Adversarial_BeamWithNegativeWidthAndZeroHeight_SlipsPastValidation()
    {
        // Attack scenario: Beam with negative width (-300mm) and zero height (0mm).
        string invalidBeamJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "beams": [
                {
                    "id": "B1",
                    "level": "Level 1",
                    "startPoint": { "x": 0.0, "y": 0.0 },
                    "endPoint": { "x": 5000.0, "y": 0.0 },
                    "widthMm": -300.0,
                    "heightMm": 0.0
                }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(invalidBeamJson);

        // VERIFIED RESOLUTION: Beam with negative width or zero height is rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));
    }

    [Fact]
    public void Adversarial_SlabWithEmptyBoundaryPolygon_SlipsPastValidation()
    {
        // Attack scenario: Slab has no boundary polygon (0 vertices).
        // ValidateSlabs does NOT check Boundary!
        string emptySlabJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "slabs": [
                {
                    "id": "S1",
                    "level": "Level 1",
                    "thicknessMm": 150.0,
                    "boundary": { "vertices": [] }
                }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(emptySlabJson);

        // VERIFIED RESOLUTION: Slab with empty boundary polygon is rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY") || e.Contains("ERR_INVALID_SLAB"));
    }

    [Fact]
    public void Adversarial_ColumnWithZeroHeight_BaseLevelEqualsTopLevel_SlipsPastValidation()
    {
        // Attack scenario: Column has BaseLevel == TopLevel (zero height).
        string zeroHeightColJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [
                { "name": "Level 1", "elevationM": 0.0 }
            ],
            "columns": [
                {
                    "id": "C1",
                    "baseLevel": "Level 1",
                    "topLevel": "Level 1",
                    "widthMm": 220.0,
                    "depthMm": 220.0
                }
            ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(zeroHeightColJson);

        // VERIFIED RESOLUTION: Column with BaseLevel == TopLevel is rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY") || e.Contains("ERR_INVALID_COLUMN"));
    }

    [Fact]
    public void Adversarial_DuplicateColumnIds_SlipsPastValidation()
    {
        // Attack scenario: Three columns with identical IDs.
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Columns = new[]
            {
                new ColumnDef { Id = "C1", BaseLevel = "Level 1", WidthMm = 220, DepthMm = 220 },
                new ColumnDef { Id = "C1", BaseLevel = "Level 1", WidthMm = 220, DepthMm = 220 },
                new ColumnDef { Id = "C1", BaseLevel = "Level 1", WidthMm = 220, DepthMm = 220 }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);

        // VERIFIED RESOLUTION: Duplicate column IDs are rejected
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_DUPLICATE_COLUMN") || e.Contains("ERR_DUPLICATE_ID"));
    }

    // =========================================================================
    // SECTION 3: False Rejection Vulnerabilities (Valid Models Accepted)
    // =========================================================================

    [Fact]
    public void Adversarial_ValidMepPipingModel_FalselyRejectedByPhysicalCount()
    {
        // Verified scenario: An MEP model defining levels and pipe runs (plumbing).
        var mepModel = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Pipes = new[]
            {
                new PipeRunDef
                {
                    Id = "P1",
                    Level = "Level 1",
                    SystemType = PipeSystemType.SoilDrain,
                    DiameterMm = 100,
                    Points = new[] { new Point3D(0, 0, 0), new Point3D(5000, 0, 0) }
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(mepModel);

        // VERIFIED RESOLUTION: Valid MEP piping model is accepted
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Adversarial_ValidStructuralRebarModel_FalselyRejectedByPhysicalCount()
    {
        // Verified scenario: A structural model defining levels and rebars.
        var rebarModel = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "RB1",
                    HostElementId = "Column_01",
                    BarMark = 1,
                    DiameterMm = 18,
                    LengthMm = 3000,
                    Quantity = 4
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(rebarModel);

        // VERIFIED RESOLUTION: Valid rebar model is accepted
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
