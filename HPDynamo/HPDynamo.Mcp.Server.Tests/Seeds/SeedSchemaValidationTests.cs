namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class SeedSchemaValidationTests
{
    private static readonly string[] Modes = ["auto", "manual", "none"];

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> Cache = new(LoadSeedsCore);

    public static IEnumerable<object[]> Seeds() => Cache.Value.Select(s => new object[] { s.Category + "/" + s.Name });

    private static Seed Get(string key) => Cache.Value.Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadSeedsCore()
    {
        var assembly = typeof(DynamoHostProfile).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("SeedLibrary/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string Read(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return names
            .Select(n => n.Replace('\\', '/'))
            .GroupBy(n => n[..n.LastIndexOf('/')])
            .Select(g =>
            {
                var parts = g.Key.Split('/');
                string Of(string file) => names.First(n => n.Replace('\\', '/') == g.Key + "/" + file);
                return new Seed(
                    parts[1],
                    parts[2],
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("tool.json"))),
                    Read(Of("code.cs")),
                    JsonSerializer.Deserialize<JsonElement>(Read(Of("examples.json"))));
            })
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void EverySeed_ToolJson_ConformsToToolRecordSchema(string key)
    {
        var seed = Get(key);
        var tool = seed.Tool;
        var profile = DynamoHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Equal("dynamo", tool.GetProperty("host").GetString());
        Assert.Equal(["2026"], tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()!).ToArray());
        Assert.Equal("published", tool.GetProperty("status").GetString());

        var desc = tool.GetProperty("description").GetString()!;
        Assert.True(desc.Length >= 20, "Description too short");
        Assert.Contains(tool.GetProperty("transaction").GetString(), Modes);

        var timeout = tool.GetProperty("timeoutSeconds").GetInt32();
        Assert.InRange(timeout, 5, profile.MaxTimeoutSeconds);
        Assert.Equal(JsonValueKind.Object, tool.GetProperty("inputSchema").ValueKind);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void EverySeed_ExamplesJson_HasValidStructureAndParity(string key)
    {
        var seed = Get(key);
        var examples = seed.Examples;
        var properties = seed.Tool.GetProperty("inputSchema")
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var required = seed.Tool.GetProperty("inputSchema").TryGetProperty("required", out var r)
            ? r.EnumerateArray().Select(e => e.GetString()!).ToArray()
            : Array.Empty<string>();

        Assert.True(examples.GetArrayLength() >= 2, "Seed must have at least 2 examples");
        var distinctArgs = examples.EnumerateArray()
            .Select(e => JsonSerializer.Serialize(e.GetProperty("args")))
            .Distinct()
            .Count();
        Assert.True(distinctArgs >= 2, "Examples must differ in args");

        foreach (var ex in examples.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(ex.GetProperty("title").GetString()));
            var argKeys = ex.GetProperty("args").EnumerateObject().Select(p => p.Name).ToArray();
            Assert.All(argKeys, k => Assert.Contains(k, properties));
            Assert.All(required, k => Assert.Contains(k, argKeys, StringComparer.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void EverySeed_CodeArgs_MatchesInputSchemaProperties(string key)
    {
        var seed = Get(key);
        var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Dynamo);
        var schemaProperties = seed.Tool.GetProperty("inputSchema")
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var argUsage in analysis.ArgKeys)
        {
            Assert.Contains(argUsage.Key, schemaProperties);
        }
    }
}
