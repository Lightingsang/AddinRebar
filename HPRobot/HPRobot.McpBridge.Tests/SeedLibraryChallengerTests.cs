using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPRobot.McpBridge.Host;
using HPRobot.McpBridge.Safety;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class SeedLibraryChallengerTests
{
    private static readonly string SeedLibraryDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPRobot.Mcp.Server", "Registry", "SeedLibrary"));

    public sealed record SeedEntry(string Category, string Name, string ToolJsonPath, string CodePath, string ExamplesJsonPath);

    public static IEnumerable<object[]> GetAllSeeds()
    {
        var dirs = Directory.GetDirectories(SeedLibraryDir, "*", SearchOption.AllDirectories)
            .Where(d => File.Exists(Path.Combine(d, "tool.json")))
            .OrderBy(d => d);

        foreach (var dir in dirs)
        {
            var cat = Path.GetFileName(Path.GetDirectoryName(dir))!;
            var name = Path.GetFileName(dir)!;
            yield return new object[] { cat, name };
        }
    }

    private static (JsonDocument ToolDoc, string Code, JsonDocument ExamplesDoc) LoadSeed(string category, string name)
    {
        var toolFile = Path.Combine(SeedLibraryDir, category, name, "tool.json");
        var codeFile = Path.Combine(SeedLibraryDir, category, name, "code.cs");
        var exFile = Path.Combine(SeedLibraryDir, category, name, "examples.json");

        Assert.True(File.Exists(toolFile), $"tool.json missing: {toolFile}");
        Assert.True(File.Exists(codeFile), $"code.cs missing: {codeFile}");
        Assert.True(File.Exists(exFile), $"examples.json missing: {exFile}");

        var toolDoc = JsonDocument.Parse(File.ReadAllText(toolFile));
        var code = File.ReadAllText(codeFile);
        var exDoc = JsonDocument.Parse(File.ReadAllText(exFile));

        return (toolDoc, code, exDoc);
    }

    [Theory]
    [MemberData(nameof(GetAllSeeds))]
    public void Seed_Code_CompilesCleanly_AgainstRobotOM(string category, string name)
    {
        var (_, code, _) = LoadSeed(category, name);
        var compiler = RobotBridgeExecutor.CreateDefaultCompiler();

        var compiled = compiler.GetOrCompile(code);

        Assert.True(compiled.Succeeded,
            $"Tool '{category}/{name}' failed Roslyn compilation:{Environment.NewLine}" +
            string.Join(Environment.NewLine, compiled.Diagnostics.Select(d => $"  Line {d.Line}, Col {d.Column}: {d.Message}")));
    }

    [Theory]
    [MemberData(nameof(GetAllSeeds))]
    public void Seed_Passes_SafetyGuard(string category, string name)
    {
        var (_, code, _) = LoadSeed(category, name);
        var violations = ScriptGuard.Check(code, GuardProfile.Robot);

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(GetAllSeeds))]
    public void Seed_ToolJson_SchemaValidity(string category, string name)
    {
        var (toolDoc, _, _) = LoadSeed(category, name);
        var root = toolDoc.RootElement;

        Assert.Equal(name, root.GetProperty("name").GetString());
        Assert.Equal(category, root.GetProperty("category").GetString());
        Assert.Equal("robot", root.GetProperty("host").GetString());
        Assert.True(root.GetProperty("version").GetInt32() >= 1);
        Assert.Equal("published", root.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("description").GetString()));

        var schema = root.GetProperty("inputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out var props));
        Assert.Equal(JsonValueKind.Object, props.ValueKind);
    }

    [Theory]
    [MemberData(nameof(GetAllSeeds))]
    public void Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples(string category, string name)
    {
        var (toolDoc, _, exDoc) = LoadSeed(category, name);
        var root = exDoc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);

        // Standard requirement: At least 2 examples
        Assert.True(root.GetArrayLength() >= 2,
            $"Seed '{category}/{name}' has {root.GetArrayLength()} example(s). Expected at least 2 distinct examples.");

        var props = toolDoc.RootElement.GetProperty("inputSchema").GetProperty("properties")
            .EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var required = toolDoc.RootElement.GetProperty("inputSchema").TryGetProperty("required", out var r)
            ? r.EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < root.GetArrayLength(); i++)
        {
            var ex = root[i];
            Assert.True(ex.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()),
                $"Seed '{category}/{name}' example[{i}] missing or empty title.");

            // Must use "args", NOT "input"
            Assert.False(ex.TryGetProperty("input", out _),
                $"Seed '{category}/{name}' example[{i}] uses non-standard key 'input' instead of 'args'.");

            Assert.True(ex.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object,
                $"Seed '{category}/{name}' example[{i}] missing 'args' object.");

            var argKeys = args.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // All required schema properties must be present in example args
            foreach (var req in required)
            {
                Assert.True(argKeys.Contains(req),
                    $"Seed '{category}/{name}' example[{i}] is missing required parameter '{req}'.");
            }

            // No undeclared properties in example args
            foreach (var key in argKeys)
            {
                Assert.True(props.Contains(key),
                    $"Seed '{category}/{name}' example[{i}] contains undeclared property '{key}'.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetAllSeeds))]
    public void Seed_ArgsRead_Match_DeclaredProperties(string category, string name)
    {
        var (toolDoc, code, _) = LoadSeed(category, name);
        var compiler = RobotBridgeExecutor.CreateDefaultCompiler();

        var facts = ScriptAnalyzer.Run(compiler, code, GuardProfile.Robot, AnalyzerProfile.Robot);
        var declared = toolDoc.RootElement.GetProperty("inputSchema").GetProperty("properties")
            .EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var undeclared = read.Except(declared).ToArray();
        Assert.True(undeclared.Length == 0,
            $"Seed '{category}/{name}' reads undeclared args: {string.Join(", ", undeclared)}");

        var unread = declared.Except(read).ToArray();
        Assert.True(unread.Length == 0,
            $"Seed '{category}/{name}' declares schema properties that are never read: {string.Join(", ", unread)}");
    }
}


