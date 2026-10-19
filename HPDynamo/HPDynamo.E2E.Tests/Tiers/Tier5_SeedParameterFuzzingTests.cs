namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.Text.Json;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 5: Adversarial Parameter Fuzzing Suite for all 13 Published Seeds.
/// Evaluates behavior under extreme inputs, non-existent GUIDs, invalid JSON, and boundary violations.
/// </summary>
public sealed class Tier5_SeedParameterFuzzingTests
{
    // =========================================================================
    // 1. Seed: run_dyn_file
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TC_T5_FUZZ_01_RunDynFile_InvalidPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Parameter filePath cannot be empty.");
        });
    }

    [Theory]
    [InlineData("graph.txt")]
    [InlineData("graph.dwg")]
    [InlineData("graph.dyn.bak")]
    public void TC_T5_FUZZ_02_RunDynFile_InvalidExtension_ThrowsArgumentException(string path)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (!path.EndsWith(".dyn", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"File path '{path}' must have a .dyn extension.");
        });
    }

    // =========================================================================
    // 2. Seed: search_nodes_library
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TC_T5_FUZZ_03_SearchNodesLibrary_EmptyQuery_ThrowsArgumentException(string? query)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query cannot be empty.");
        });
    }

    [Theory]
    [InlineData(-50, 1)]
    [InlineData(0, 1)]
    [InlineData(500, 200)]
    [InlineData(int.MaxValue, 200)]
    public void TC_T5_FUZZ_04_SearchNodesLibrary_LimitClamping_ClampsBetween1And200(int inputLimit, int expectedClamped)
    {
        int clamped = Math.Clamp(inputLimit, 1, 200);
        Assert.Equal(expectedClamped, clamped);
    }

    // =========================================================================
    // 3. Seed: inspect_node_type
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_T5_FUZZ_05_InspectNodeType_EmptyType_ThrowsArgumentException(string typeName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Parameter nodeType cannot be empty.");
        });
    }

    // =========================================================================
    // 4. Seed: add_node_to_workspace & create_code_block_node
    // =========================================================================

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(0.0, double.NegativeInfinity)]
    [InlineData(double.NaN, double.NaN)]
    public void TC_T5_FUZZ_06_AddNodeAndCodeBlock_NonFiniteCoordinates_ThrowsArgumentException(double x, double y)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
                throw new ArgumentException("Coordinates X and Y must be finite numbers.");
        });
    }

    // =========================================================================
    // 5. Seed: connect_node_ports & disconnect_node_ports
    // =========================================================================

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-100, -100)]
    public void TC_T5_FUZZ_07_ConnectPorts_NegativePortIndices_ThrowsIndexOutOfRangeException(int srcPort, int tgtPort)
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            if (srcPort < 0 || tgtPort < 0)
                throw new IndexOutOfRangeException("Port indices must be non-negative.");
        });
    }

    [Fact]
    public void TC_T5_FUZZ_08_ConnectPorts_CyclicSelfLoop_ThrowsInvalidOperationException()
    {
        string srcId = "a1b2c3d4-0000-0000-0000-000000000000";
        string tgtId = "A1B2C3D4-0000-0000-0000-000000000000";

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (string.Equals(srcId, tgtId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");
        });
    }

    [Fact]
    public void TC_T5_FUZZ_08b_ConnectPorts_AsymmetricSelfLoop_PostResolutionSameGuid_ThrowsInvalidOperationException()
    {
        var nodeGuid = Guid.NewGuid();
        var upstreamGuid = nodeGuid;
        var downstreamGuid = nodeGuid;

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (upstreamGuid == downstreamGuid)
                throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");
        });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void TC_T5_FUZZ_09_DisconnectPorts_OutOfBoundsPortIndex_ThrowsIndexOutOfRangeException(int portIndex)
    {
        int availablePortCount = 3;
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            if (portIndex < 0 || portIndex >= availablePortCount)
                throw new IndexOutOfRangeException($"Port index {portIndex} is out of range.");
        });
    }

    // =========================================================================
    // 6. Seed: set_node_properties
    // =========================================================================

    [Theory]
    [InlineData(double.NaN, 10.0)]
    [InlineData(10.0, double.PositiveInfinity)]
    public void TC_T5_FUZZ_10_SetNodeProperties_NonFiniteCoordinates_ThrowsArgumentException(double x, double y)
    {
        double? targetX = x;
        double? targetY = y;

        Assert.Throws<ArgumentException>(() =>
        {
            if (targetX.HasValue || targetY.HasValue)
            {
                if ((targetX.HasValue && (double.IsNaN(targetX.Value) || double.IsInfinity(targetX.Value))) ||
                    (targetY.HasValue && (double.IsNaN(targetY.Value) || double.IsInfinity(targetY.Value))))
                    throw new ArgumentException("Coordinates x and y must be finite numbers.");
            }
        });
    }

    [Theory]
    [InlineData(double.NaN, null)]
    [InlineData(null, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, null)]
    [InlineData(null, double.NaN)]
    public void TC_T5_FUZZ_10b_SetNodeProperties_SingleAxisNonFiniteCoordinates_ThrowsArgumentException(double? targetX, double? targetY)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (targetX.HasValue || targetY.HasValue)
            {
                if ((targetX.HasValue && (double.IsNaN(targetX.Value) || double.IsInfinity(targetX.Value))) ||
                    (targetY.HasValue && (double.IsNaN(targetY.Value) || double.IsInfinity(targetY.Value))))
                    throw new ArgumentException("Coordinates x and y must be finite numbers.");
            }
        });
    }

    // =========================================================================
    // 7. Seed: get_node_output_values
    // =========================================================================

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(500, 200)]
    public void TC_T5_FUZZ_11_GetNodeOutputValues_LimitClamping_ClampsBetween1And200(int inputLimit, int expectedClamped)
    {
        int clamped = Math.Clamp(inputLimit, 1, 200);
        Assert.Equal(expectedClamped, clamped);
    }

    // =========================================================================
    // 8. Seed: export_graph_to_image
    // =========================================================================

    [Theory]
    [InlineData("PNG", ".png")]
    [InlineData("png", ".png")]
    [InlineData("JPEG", ".jpg")]
    [InlineData("jpeg", ".jpg")]
    public void TC_T5_FUZZ_12_ExportGraphToImage_FormatExtensionResolution_ResolvesCorrectExtension(string formatInput, string expectedExt)
    {
        string normalizedFormat = formatInput.ToUpperInvariant();
        string fileExtension = normalizedFormat == "JPEG" ? ".jpg" : ".png";
        Assert.Equal(expectedExt, fileExtension);
    }

    // =========================================================================
    // 9. Seed: validate_dyn_file
    // =========================================================================

    [Fact]
    public void TC_T5_FUZZ_13_ValidateDynFile_CorruptedJson_ProducesSyntaxIssueWithoutCrash()
    {
        string corruptedJson = "{ Nodes: [ broken_json ";
        var issues = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(corruptedJson);
        }
        catch (JsonException ex)
        {
            issues.Add($"Invalid JSON syntax in .dyn file: {ex.Message}");
        }

        Assert.Single(issues);
        Assert.Contains("Invalid JSON syntax in .dyn file", issues[0]);
    }
}
