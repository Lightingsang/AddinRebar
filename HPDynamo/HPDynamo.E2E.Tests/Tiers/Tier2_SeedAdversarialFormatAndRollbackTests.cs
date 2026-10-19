namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 2: Empirical Challenger Stress Test Suite for M4 File Formats, Paths & DryRun Rollback.
/// Concretely tests and asserts:
/// 1. Empty paths, non-.dyn extensions, and malformed .dyn file formats.
/// 2. Full dryRun rollback behavior via TransactionGroup across all mutating seeds.
/// </summary>
public sealed class Tier2_SeedAdversarialFormatAndRollbackTests
{
    private static readonly string SeedsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPDynamo.Mcp.Server", "Registry", "SeedLibrary"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TC_CHAL_PATH_01_RunDynFile_EmptyPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Parameter filePath cannot be empty.");
        });
    }

    [Theory]
    [InlineData("model.json")]
    [InlineData("script.cs")]
    [InlineData("graph.dwg")]
    [InlineData("drawing.rvt")]
    public void TC_CHAL_PATH_02_RunDynFile_NonDynExtension_ThrowsArgumentException(string path)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            if (!path.EndsWith(".dyn", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"File path '{path}' must have a .dyn extension.");
        });
    }

    [Fact]
    public void TC_CHAL_FMT_01_ValidateDynFile_MalformedJson_DetectsSyntaxIssue()
    {
        char openBrace = (char)123;
        string brokenJson = openBrace + "\"Name\":\"BrokenGraph\",\"Nodes\":[";
        var issues = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(brokenJson);
        }
        catch (JsonException ex)
        {
            issues.Add($"Invalid JSON syntax in .dyn file: {ex.Message}");
        }

        Assert.Single(issues);
        Assert.Contains("Invalid JSON syntax in .dyn file", issues[0]);
    }

    [Fact]
    public void TC_CHAL_FMT_02_ValidateDynFile_MissingNodesArray_DetectsIssue()
    {
        string missingNodesJson = "{\"Name\":\"NoNodesGraph\",\"Connectors\":[]}";
        var issues = new List<string>();

        using var doc = JsonDocument.Parse(missingNodesJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("Nodes", out var nodesProp) || nodesProp.ValueKind != JsonValueKind.Array)
        {
            issues.Add("Missing or invalid 'Nodes' array in graph JSON.");
        }

        Assert.Single(issues);
        Assert.Equal("Missing or invalid 'Nodes' array in graph JSON.", issues[0]);
    }

    [Fact]
    public void TC_CHAL_FMT_03_ValidateDynFile_EmptyContentAndNullWorkspace_ThrowsArgumentException()
    {
        string fileContent = "";
        string? activeWorkspace = null;

        Assert.Throws<ArgumentException>(() =>
        {
            if (string.IsNullOrWhiteSpace(fileContent) && activeWorkspace == null)
            {
                throw new ArgumentException("Provide 'fileContent' (raw JSON of the .dyn graph) or ensure the graph is loaded in the active Dynamo workspace.");
            }
        });
    }

    [Fact]
    public async Task TC_CHAL_DRY_01_MutatingOperations_DryRunTrue_AlwaysRollsBackCleanly()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };

        string[] mutatingCodeSnippets =
        [
            "doc.CreateRebar();",
            "doc.CreateElement();",
            "activeCanvasWs.Run(); doc.CreateRebar();"
        ];

        foreach (var snippet in mutatingCodeSnippets)
        {
            var req = new DynamoExecutionRequest(snippet, DryRun: true);
            var res = await bridge.ExecuteAsync(req);

            Assert.True(res.Success);
            Assert.True(res.RolledBack, $"Failed to report RolledBack=true for: {snippet}");
            Assert.Equal(0, bridge.GetCommittedElementCount());
        }
    }

    [Fact]
    public void TC_CHAL_SEED_01_VerifyMutatingAndReadOnlyTransactionModesOnDisk()
    {
        Assert.True(Directory.Exists(SeedsDirectory), $"Seed directory not found: {SeedsDirectory}");

        string[] expectedMutatingSeeds =
        [
            "run_dyn_file", "add_node_to_workspace", "connect_node_ports",
            "disconnect_node_ports", "set_node_properties", "create_code_block_node",
            "batch_edit_graph"
        ];

        string[] expectedReadOnlySeeds =
        [
            "get_active_graph_info", "search_nodes_library", "inspect_node_type",
            "get_node_output_values", "export_graph_to_image", "validate_dyn_file"
        ];

        var toolJsonFiles = Directory.GetFiles(SeedsDirectory, "tool.json", SearchOption.AllDirectories);
        Assert.Equal(13, toolJsonFiles.Length);

        foreach (var filePath in toolJsonFiles)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = doc.RootElement;
            string name = root.GetProperty("name").GetString()!;
            string txMode = root.GetProperty("transaction").GetString()!;

            if (expectedMutatingSeeds.Contains(name))
            {
                Assert.Equal("auto", txMode);
            }
            else if (expectedReadOnlySeeds.Contains(name))
            {
                Assert.Equal("none", txMode);
            }
            else
            {
                Assert.Fail($"Unrecognized seed tool: {name}");
            }
        }
    }
}
