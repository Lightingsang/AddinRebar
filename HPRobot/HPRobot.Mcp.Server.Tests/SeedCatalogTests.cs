using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

/// <summary>
///     Every seed tool embedded in the Robot server: a well-formed record for this host, accepted by the registry
///     validator under the Robot profile, guard-clean code that reads exactly the parameters its schema declares,
///     the destructive marker on exactly the seed that calls destructive members, and examples an AI can copy.
///     Together with the 4 core tools and 8 registry tools, exactly 24 tools are exposed.
/// </summary>
public sealed class SeedCatalogTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private const int MaxCodeLines = 120;
    private static readonly string[] AllowedModes = ["auto", "manual", "none"];

    private static readonly Regex ForbiddenPatterns = new(
        @"\b(System\.Diagnostics\.Process|Process\.Start|Quit|ApplicationExit|Interactive|MessageBox|Assembly\.Load|Type\.GetType)\b",
        RegexOptions.Compiled);

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> CachedSeeds = new(LoadEmbeddedSeeds);

    public static IReadOnlyList<Seed> Seeds => CachedSeeds.Value;

    public static IEnumerable<object[]> SeedKeys() => Seeds.Select(s => new object[] { s.Category + "/" + s.Name });

    public static Seed GetSeed(string key) => Seeds.Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadEmbeddedSeeds()
    {
        var assembly = typeof(RobotHostProfile).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(SeedInstaller.ResourcePrefix, StringComparison.Ordinal))
            .ToArray();

        string ReadResource(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return resourceNames
            .Select(n => n.Replace('\\', '/'))
            .GroupBy(n => n[..n.LastIndexOf('/')])
            .Select(g =>
            {
                var parts = g.Key.Split('/'); // SeedLibrary/<Category>/<Name>
                string GetFile(string fileName) => resourceNames.First(n => n.Replace('\\', '/') == g.Key + "/" + fileName);

                return new Seed(
                    parts[1],
                    parts[2],
                    JsonSerializer.Deserialize<JsonElement>(ReadResource(GetFile("tool.json"))),
                    ReadResource(GetFile("code.cs")),
                    JsonSerializer.Deserialize<JsonElement>(ReadResource(GetFile("examples.json"))));
            })
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToArray();
    }

    private static bool EndsWithReturn(string code)
    {
        var lines = code.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var last = Array.FindLastIndex(lines, l => l.StartsWith("return ", StringComparison.Ordinal) || l == "return;");
        if (last < 0 || !lines[^1].TrimEnd().EndsWith(';')) return false;
        return lines.Skip(last + 1).All(l => l.Length == 0 || char.IsWhiteSpace(l[0]) || l[0] is '{' or '}' or ')');
    }

    [Fact]
    public void Manifest_DiscoversExactlyTwelveSeeds_AcrossSixCategories()
    {
        Assert.Equal(12, Seeds.Count);

        var categories = Seeds.Select(s => s.Category).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray();
        Assert.Equal(["Analysis", "Geometry", "Load", "Model", "Property", "Results"], categories);

        Assert.Equal(7, Seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
        Assert.Equal(5, Seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "auto"));

        var destructiveSeeds = Seeds.Where(s =>
            s.Tool.TryGetProperty("tags", out var tags) &&
            tags.EnumerateArray().Any(t => t.GetString() == "destructive")).Select(s => s.Name).ToArray();

        Assert.Equal(["run_calculations"], destructiveSeeds);
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_ToolJson_ConformsToSchemaAndConventions(string key)
    {
        var seed = GetSeed(key);
        var tool = seed.Tool;
        var profile = RobotHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidator.IsReserved(seed.Name, profile), $"Seed '{seed.Name}' shadows a core tool name.");
        Assert.Equal("robot", tool.GetProperty("host").GetString());
        Assert.Contains("2026", tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        if (tool.TryGetProperty("notes", out var notes))
        {
            Assert.False(string.IsNullOrWhiteSpace(notes.GetString()));
        }

        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 40, $"Description of '{seed.Name}' is too short.");

        var mode = tool.GetProperty("transaction").GetString();
        Assert.Contains(mode, AllowedModes);

        var timeout = tool.GetProperty("timeoutSeconds").GetInt32();
        Assert.InRange(timeout, 10, profile.MaxTimeoutSeconds);

        var isDestructive = tool.GetProperty("destructive").GetBoolean();
        if (seed.Name == "run_calculations")
        {
            Assert.True(isDestructive);
            Assert.Equal("auto", mode);
            Assert.Equal(profile.MaxTimeoutSeconds, timeout);
        }
        else
        {
            Assert.False(isDestructive);
            Assert.True(timeout <= 30);
        }

        var schema = tool.GetProperty("inputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, schema.GetProperty("properties").ValueKind);

        if (schema.TryGetProperty("additionalProperties", out var addl))
        {
            Assert.False(addl.GetBoolean());
        }

        var properties = schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (schema.TryGetProperty("required", out var requiredArray))
        {
            foreach (var req in requiredArray.EnumerateArray())
            {
                Assert.Contains(req.GetString()!, properties);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_ExamplesJson_HasAtLeastTwoValidExamples_WithDistinctArgs(string key)
    {
        var seed = GetSeed(key);
        var schema = seed.Tool.GetProperty("inputSchema");
        var declaredProperties = schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredProperties = schema.TryGetProperty("required", out var reqArr)
            ? reqArr.EnumerateArray().Select(r => r.GetString()!).ToArray()
            : [];

        Assert.True(seed.Examples.GetArrayLength() >= 2, $"Seed '{seed.Name}' must have >= 2 examples.");

        if (declaredProperties.Count > 0)
        {
            var distinctArgs = seed.Examples.EnumerateArray()
                .Select(e => JsonSerializer.Serialize(e.GetProperty("args")))
                .Distinct()
                .Count();
            Assert.True(distinctArgs >= 2, $"Seed '{seed.Name}' examples must have distinct argument payloads.");
        }
        else
        {
            var distinctTitles = seed.Examples.EnumerateArray()
                .Select(e => e.GetProperty("title").GetString())
                .Distinct()
                .Count();
            Assert.True(distinctTitles >= 2, $"Seed '{seed.Name}' examples must have distinct titles.");
        }

        foreach (var example in seed.Examples.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("title").GetString()));
            var args = example.GetProperty("args");
            Assert.Equal(JsonValueKind.Object, args.ValueKind);

            var argKeys = args.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.All(argKeys, k => Assert.Contains(k, declaredProperties));
            Assert.All(requiredProperties, req => Assert.Contains(req, argKeys, StringComparer.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_Code_PassesSafetyGuard_AndMatchesInputSchema(string key)
    {
        var seed = GetSeed(key);

        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes);
        Assert.True(seed.Code.Split('\n').Length <= MaxCodeLines);
        Assert.True(EndsWithReturn(seed.Code), $"Seed '{seed.Name}' code must end with a top-level return statement.");
        Assert.DoesNotMatch(ForbiddenPatterns, seed.Code);

        var guardViolations = ScriptGuard.Check(seed.Code, GuardProfile.Robot);
        Assert.Empty(guardViolations);

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Robot);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), $"Seed '{seed.Name}' reads undeclared args: {string.Join(", ", read.Except(declared))}");
        Assert.True(declared.IsSubsetOf(read), $"Seed '{seed.Name}' declares unused args: {string.Join(", ", declared.Except(read))}");
        Assert.False(facts.UsesTransaction, $"Seed '{seed.Name}' must not open a transaction.");
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_Record_PassesToolValidator(string key)
    {
        var seed = GetSeed(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, RobotHostProfile.Instance);
        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Fact]
    public void DynamicRegistry_YieldsExactly24ToolsTotal()
    {
        // 1. Check static tools
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();
        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        // 2. Check embedded seeds
        var seeds = SeedInstaller.LoadSeeds(typeof(RobotHostProfile).Assembly)
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        var combinedCatalog = new HashSet<string>(staticTools, StringComparer.Ordinal);
        foreach (var seedName in seeds)
        {
            combinedCatalog.Add(seedName);
        }

        Assert.Equal(24, combinedCatalog.Count);
        Assert.Equal(4, RobotHostProfile.Instance.CoreToolNames.Count);
        Assert.Equal(8, staticTools.Count - RobotHostProfile.Instance.CoreToolNames.Count);
        Assert.Equal(12, seeds.Count);

        // Check specific seed names exist in combined catalog
        Assert.Contains("run_calculations", combinedCatalog);
        Assert.Contains("assign_node_support", combinedCatalog);
        Assert.Contains("draw_bar_by_coords", combinedCatalog);
        Assert.Contains("get_coordinate_systems_and_grids", combinedCatalog);
        Assert.Contains("get_structural_objects", combinedCatalog);
        Assert.Contains("assign_bar_load", combinedCatalog);
        Assert.Contains("get_load_definitions", combinedCatalog);
        Assert.Contains("get_model_info", combinedCatalog);
        Assert.Contains("assign_bar_section", combinedCatalog);
        Assert.Contains("get_materials_and_sections", combinedCatalog);
        Assert.Contains("get_bar_forces", combinedCatalog);
        Assert.Contains("get_node_reactions", combinedCatalog);
    }
}
