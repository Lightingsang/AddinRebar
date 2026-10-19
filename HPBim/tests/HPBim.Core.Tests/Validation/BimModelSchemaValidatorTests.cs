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
