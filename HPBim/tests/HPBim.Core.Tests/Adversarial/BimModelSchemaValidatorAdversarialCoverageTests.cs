namespace HPBim.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Validation;
using Xunit;

public sealed class BimModelSchemaValidatorAdversarialCoverageTests
{
    [Fact]
    public void Validate_UnicodeAndVietnameseDiacriticLevelNames_ResolvesAndPasses()
    {
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[]
            {
                new LevelDef("Tầng Hầm B1 (-3.000m)", -3.0, 3000, -1),
                new LevelDef("Tầng Trệt (+0.000m)", 0.0, 3800, 1)
            },
            Columns = new[]
            {
                new ColumnDef
                {
                    Id = "C1",
                    BaseLevel = "Tầng Hầm B1 (-3.000m)",
                    TopLevel = "Tầng Trệt (+0.000m)",
                    WidthMm = 220,
                    DepthMm = 220
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmojiAndSpecialCharactersInLevelNames_PassesIfMatching()
    {
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("🏢 Level 1 [Ground]", 0.0, 3000, 1) },
            Beams = new[]
            {
                new BeamDef
                {
                    Id = "B1",
                    Level = "🏢 Level 1 [Ground]",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(4000, 0),
                    WidthMm = 200,
                    HeightMm = 350
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ExtremeCoordinates_NaNInGrids_Rejected()
    {
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            SchemaVersion = "1.0",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Grids = new[]
            {
                new AxisGrid("X1", 0, GridDirection.X, new Point2D(double.NaN, 0), new Point2D(0, 5000))
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));
    }

    [Fact]
    public void Validate_GridLengthNear1mmBoundary_StrictBoundaryEnforcement()
    {
        var modelFail = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Grids = new[]
            {
                new AxisGrid("X1", 0, GridDirection.X, new Point2D(0, 0), new Point2D(0, 0.99))
            }
        };
        var resultFail = BimModelSchemaValidator.ValidateModel(modelFail);
        Assert.False(resultFail.IsValid);
        Assert.Contains(resultFail.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));

        var modelPass = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Grids = new[]
            {
                new AxisGrid("X1", 0, GridDirection.X, new Point2D(0, 0), new Point2D(0, 1.05))
            }
        };
        var resultPass = BimModelSchemaValidator.ValidateModel(modelPass);
        Assert.True(resultPass.IsValid);
    }

    [Fact]
    public void Validate_WallOpening_BeyondWallBoundary_Rejected()
    {
        var wallWithOverhangingOpening = new WallDef
        {
            Id = "W1",
            BaseLevel = "Level 1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            ThicknessMm = 200,
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D1",
                    WidthMm = 1000,
                    HeightMm = 2200,
                    DistanceAlongWallMm = 3015 // 3015 + 1000 = 4015mm > 4000 + 10mm
                }
            }
        };

        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Walls = new[] { wallWithOverhangingOpening }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));
    }

    [Fact]
    public void Validate_WallOpening_NegativeOffset_Rejected()
    {
        var wallWithNegativeOpening = new WallDef
        {
            Id = "W1",
            BaseLevel = "Level 1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(4000, 0),
            ThicknessMm = 200,
            Openings = new[]
            {
                new OpeningDef { Id = "D1", WidthMm = 900, HeightMm = 2100, DistanceAlongWallMm = -50 }
            }
        };

        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Walls = new[] { wallWithNegativeOpening }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));
    }

    [Fact]
    public void Validate_CorruptedJsonNodes_NonArrayLevels_Rejected()
    {
        string badLevelsJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": { "name": "Level 1", "elevationM": 0.0 },
            "columns": []
        }
        """;

        var result = BimModelSchemaValidator.Validate(badLevelsJson);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MISSING_LEVELS"));
    }

    [Fact]
    public void Validate_CorruptedJsonNodes_TypeMismatchInElevation_Rejected()
    {
        string badElevationJson = """
        {
            "artifactType": "HPBim.Model.v1",
            "schemaVersion": "1.0",
            "levels": [ { "name": "Level 1", "elevationM": "NOT_A_NUMBER" } ]
        }
        """;

        var result = BimModelSchemaValidator.Validate(badElevationJson);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_SCHEMA"));
    }

    [Fact]
    public void Validate_RebarHostResolution_UnderscorePrefixAndBrokenRef()
    {
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Columns = new[]
            {
                new ColumnDef { Id = "Column_01_SectionA", BaseLevel = "Level 1", WidthMm = 220, DepthMm = 220 }
            },
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "RB1",
                    HostElementId = "Column_01", // Should match via prefix
                    DiameterMm = 18,
                    LengthMm = 3000,
                    Quantity = 4
                },
                new RebarDef
                {
                    Id = "RB2",
                    HostElementId = "UnknownHost_99", // Broken reference
                    DiameterMm = 18,
                    LengthMm = 3000,
                    Quantity = 4
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_BROKEN_REFERENCE") && e.Contains("UnknownHost_99"));
        Assert.DoesNotContain(result.Errors, e => e.Contains("RB1"));
    }

    [Fact]
    public void Validate_PipeRunDef_DegeneratePointsList_Rejected()
    {
        var model = new BimModel
        {
            ArtifactType = "HPBim.Model.v1",
            Levels = new[] { new LevelDef("Level 1", 0.0, 3000, 1) },
            Pipes = new[]
            {
                new PipeRunDef
                {
                    Id = "P1",
                    Level = "Level 1",
                    Points = new[] { new Point3D(0, 0, 0) } // Only 1 point (< 2)
                }
            }
        };

        var result = BimModelSchemaValidator.ValidateModel(model);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_INVALID_GEOMETRY"));
    }
}
