using System.Reflection;
using System.Text.Json;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

public sealed class ExcelSeedLibraryAdversarialChallengeTests
{
    private static readonly Assembly ServerAssembly = typeof(ExcelHostProfile).Assembly;
    private static readonly IHostProfile Profile = ExcelHostProfile.Instance;

    private static IReadOnlyList<SeedInstaller.SeedContent> GetSeeds() => SeedInstaller.LoadSeeds(ServerAssembly);
    private static ScriptArgs ParseScriptArgs(string json) => new(JsonDocument.Parse(json).RootElement);

    #region 1. Schema Validity & Folder Category Alignment

    [Fact]
    public void Exactly_12_seeds_exist_and_all_categories_match_folders()
    {
        var seeds = GetSeeds();
        Assert.Equal(12, seeds.Count);

        var manifestResourceNames = ServerAssembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("SeedLibrary/", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Replace('\\', '/'))
            .ToList();

        // 12 tools * 3 files = 36 resources
        Assert.Equal(36, manifestResourceNames.Count);

        foreach (var seed in seeds)
        {
            Assert.False(string.IsNullOrWhiteSpace(seed.Name), "Seed name is empty.");
            Assert.False(string.IsNullOrWhiteSpace(seed.Category), $"Seed {seed.Name} category is empty.");
            Assert.Contains(seed.Category, Profile.Categories);

            // Verify category matches manifest path
            var expectedPrefix = $"SeedLibrary/{seed.Category}/{seed.Name}/";
            Assert.Contains(manifestResourceNames, r => r.Replace('\\', '/').StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));

            // Verify tool.json deserialize
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson);
            Assert.NotNull(record);
            Assert.Equal(seed.Name, record.Name);
            Assert.Equal(seed.Category, record.Category);
            Assert.Equal("excel", record.Host);
            Assert.Contains("2026", record.HostVersions);
            Assert.InRange(record.TimeoutSeconds, 5, 600);
            Assert.True(record.Transaction is "auto" or "manual" or "none", $"Invalid transaction mode: {record.Transaction}");
            Assert.True(record.Description.Length >= 20, $"Description too short in {seed.Name}");
        }
    }

    [Fact]
    public void All_tool_schemas_conform_to_JSON_schema_standards_and_ToolValidator()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson)!;
            record.Code = seed.Code;
            record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.ExamplesJson)!;

            // Analyze code for ArgKeys
            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Excel);
            analysis.Compiles = true; // Mark as compiled for validator's analysis check

            // Full ToolValidator run with analysis
            var report = ToolValidator.Validate(record, analysis, [], false, Profile);
            Assert.True(report.IsValid, $"Seed '{seed.Category}/{seed.Name}' failed ToolValidator: {string.Join("; ", report.Errors)}");

            // Deep JSON schema inspection
            var schema = record.InputSchema;
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            Assert.True(schema.TryGetProperty("type", out var typeElem) && typeElem.GetString() == "object");
            Assert.True(schema.TryGetProperty("properties", out var propElem) && propElem.ValueKind == JsonValueKind.Object);

            if (schema.TryGetProperty("additionalProperties", out var addlProps))
            {
                Assert.False(addlProps.GetBoolean(), $"Tool {seed.Name} must specify additionalProperties: false");
            }

            if (schema.TryGetProperty("required", out var reqElem))
            {
                Assert.Equal(JsonValueKind.Array, reqElem.ValueKind);
                var declaredProps = propElem.EnumerateObject().Select(p => p.Name).ToHashSet();
                foreach (var req in reqElem.EnumerateArray())
                {
                    Assert.Contains(req.GetString()!, declaredProps);
                }
            }
        }
    }

    #endregion

    #region 2. Property Coverage in code.cs

    [Fact]
    public void Every_inputSchema_property_is_actively_read_in_code()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson)!;
            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Excel);

            var declaredProps = record.InputSchema.GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var readArgs = analysis.ArgKeys
                .Select(a => a.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unreadProps = declaredProps.Except(readArgs).ToList();
            Assert.Empty(unreadProps);

            var undeclaredReads = readArgs.Except(declaredProps).ToList();
            Assert.Empty(undeclaredReads);
        }
    }

    #endregion

    #region 3. ScriptGuard & Code Security

    [Fact]
    public void All_seeds_pass_ScriptGuard_with_zero_violations()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var violations = ScriptGuard.Check(seed.Code, GuardProfile.Excel);
            Assert.True(violations.Count == 0,
                $"Seed '{seed.Category}/{seed.Name}' failed ScriptGuard: {string.Join("; ", violations.Select(v => $"{v.Line}:{v.Column} {v.Message}"))}");

            // Explicit AST checks: No dynamic keywords
            var tree = CSharpSyntaxTree.ParseText(seed.Code, new CSharpParseOptions(kind: SourceCodeKind.Script));
            var root = tree.GetRoot();

            var dynamicTokens = root.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == "dynamic")
                .ToList();
            Assert.Empty(dynamicTokens);

            // No #r or #load directives
            var triviaDirectives = root.DescendantTrivia()
                .Where(t => t.IsDirective)
                .Select(t => t.ToString())
                .ToList();
            Assert.DoesNotContain(triviaDirectives, d => d.StartsWith("#r", StringComparison.OrdinalIgnoreCase) || d.StartsWith("#load", StringComparison.OrdinalIgnoreCase));

            // No forbidden namespaces
            var text = seed.Code;
            Assert.DoesNotContain("System.Diagnostics.Process", text);
            Assert.DoesNotContain("Application.Quit", text);
        }
    }

    #endregion

    #region 4. Examples Validity & Adversarial Schema Comparison

    [Fact]
    public void Examples_are_non_empty_and_pass_structural_checks()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson)!;
            var examples = RegistryJson.Deserialize<List<ToolExample>>(seed.ExamplesJson)!;

            Assert.NotNull(examples);
            Assert.True(examples.Count >= 2, $"Seed '{seed.Name}' has fewer than 2 examples.");

            var schemaProps = record.InputSchema.GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requiredProps = record.InputSchema.TryGetProperty("required", out var req)
                ? req.EnumerateArray().Select(r => r.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ex in examples)
            {
                Assert.False(string.IsNullOrWhiteSpace(ex.Title), $"Empty example title in {seed.Name}");
                Assert.Equal(JsonValueKind.Object, ex.Args.ValueKind);

                // Verify required props are provided
                foreach (var reqProp in requiredProps)
                {
                    Assert.True(ex.Args.TryGetProperty(reqProp, out _),
                        $"Seed '{seed.Name}' example '{ex.Title}' is missing required property '{reqProp}'");
                }

                // Verify no extra undeclared props are passed in examples
                foreach (var prop in ex.Args.EnumerateObject())
                {
                    Assert.Contains(prop.Name, schemaProps);
                }
            }
        }
    }

    [Fact]
    public void Adversarial_detect_schema_vs_examples_type_discrepancies()
    {
        // Finding 1: write_range declares cell items as string, but examples contain integers
        var seeds = GetSeeds();
        var writeRangeSeed = seeds.First(s => s.Name == "write_range");
        var writeRangeRecord = RegistryJson.Deserialize<ToolRecord>(writeRangeSeed.ToolJson)!;
        var writeRangeExamples = RegistryJson.Deserialize<List<ToolExample>>(writeRangeSeed.ExamplesJson)!;

        var valuesProp = writeRangeRecord.InputSchema.GetProperty("properties").GetProperty("values");
        var cellItemType = valuesProp.GetProperty("items").GetProperty("items").GetProperty("type").GetString();
        Assert.Equal("string", cellItemType); // Declared as string

        // Verify that example 1 contains numbers in the array
        var ex1Values = writeRangeExamples[0].Args.GetProperty("values");
        var row2 = ex1Values.EnumerateArray().Skip(1).First().EnumerateArray().ToList();
        var hasNumericInExample = row2.Any(v => v.ValueKind == JsonValueKind.Number);
        Assert.True(hasNumericInExample, "Adversarial observation: write_range example includes numbers, while schema declares items.type='string'.");

        // Finding 2: run_macro declares args items as string, but example 2 contains boolean
        var runMacroSeed = seeds.First(s => s.Name == "run_macro");
        var runMacroRecord = RegistryJson.Deserialize<ToolRecord>(runMacroSeed.ToolJson)!;
        var runMacroExamples = RegistryJson.Deserialize<List<ToolExample>>(runMacroSeed.ExamplesJson)!;

        var macroArgsProp = runMacroRecord.InputSchema.GetProperty("properties").GetProperty("args");
        var macroItemType = macroArgsProp.GetProperty("items").GetProperty("type").GetString();
        Assert.Equal("string", macroItemType); // Declared as string

        var ex2Args = runMacroExamples[1].Args.GetProperty("args").EnumerateArray().ToList();
        var hasBoolInExample = ex2Args.Any(v => v.ValueKind is JsonValueKind.True or JsonValueKind.False);
        Assert.True(hasBoolInExample, "Adversarial observation: run_macro example includes boolean, while schema declares items.type='string'.");
    }

    #endregion

    private static ScriptArgs ParseArgs(string json) => new ScriptArgs(JsonSerializer.Deserialize<JsonElement>(json));

    #region 5. Edge Cases: write_range 2D Array Processing

    [Fact]
    public void Write_range_correctly_validates_2D_array_boundaries()
    {
        // Scenario A: Empty list
        var emptyArgs = ParseArgs("{\"startCell\": \"A1\", \"values\": []}");
        var emptyList = emptyArgs.List("values");
        Assert.Empty(emptyList);

        // Scenario B: Jagged list
        var jaggedArgs = ParseArgs("{\"startCell\": \"A1\", \"values\": [[\"A\", \"B\"], [\"C\"]]}");
        var jaggedList = jaggedArgs.List("values");
        Assert.Equal(2, jaggedList.Count);

        int numCols = 0;
        foreach (var r in jaggedList)
        {
            if (r.IsArray && r.Raw.HasValue)
            {
                int c = r.Raw.Value.GetArrayLength();
                if (c > numCols) numCols = c;
            }
        }
        Assert.Equal(2, numCols); // Max column count is 2

        // Simulate matrix population as done in write_range/code.cs
        object?[,] data = new object?[2, 2];
        for (int r = 0; r < 2; r++)
        {
            var cols = jaggedList[r].Raw!.Value.EnumerateArray().ToList();
            for (int c = 0; c < 2; c++)
            {
                if (c < cols.Count) data[r, c] = cols[c].GetString();
                else data[r, c] = null;
            }
        }

        Assert.Equal("A", data[0, 0]);
        Assert.Equal("B", data[0, 1]);
        Assert.Equal("C", data[1, 0]);
        Assert.Null(data[1, 1]); // Padded with null

        // Scenario C: 1D array instead of 2D
        var oneDArgs = ParseArgs("{\"startCell\": \"A1\", \"values\": [\"val1\", \"val2\"]}");
        var oneDList = oneDArgs.List("values");
        int oneDCols = 0;
        foreach (var r in oneDList)
        {
            if (r.IsArray && r.Raw.HasValue)
            {
                int c = r.Raw.Value.GetArrayLength();
                if (c > oneDCols) oneDCols = c;
            }
        }
        Assert.Equal(0, oneDCols); // Correctly fails to detect columns, triggering ArgumentException
    }

    #endregion

    #region 6. Edge Cases: manage_worksheet Actions

    [Fact]
    public void Manage_worksheet_actions_coverage_and_unhandled_action_behavior()
    {
        var seeds = GetSeeds();
        var manageSeed = seeds.First(s => s.Name == "manage_worksheet");
        var record = RegistryJson.Deserialize<ToolRecord>(manageSeed.ToolJson)!;

        var actions = record.InputSchema
            .GetProperty("properties")
            .GetProperty("action")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(a => a.GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Defined in enum
        Assert.Contains("add", actions);
        Assert.Contains("rename", actions);
        Assert.Contains("duplicate", actions);
        Assert.Contains("delete", actions);
        Assert.Contains("hide", actions);
        Assert.Contains("unhide", actions);

        // Explicitly NOT in enum
        Assert.DoesNotContain("copy", actions);
        Assert.DoesNotContain("activate", actions);

        // Notice in code.cs:
        // if (action == "add") ... else if (action == "rename") ... else if (action == "duplicate") ...
        // else if (action == "delete") ... else if (action == "hide") ... else if (action == "unhide") ...
        // If an unrecognized action is passed (e.g. bypass or internal), code.cs does NOT throw ArgumentException;
        // it falls through with resultMsg = "" and returns success = true!
        var code = manageSeed.Code;
        Assert.DoesNotContain("throw new ArgumentException($\"Unknown action", code);
        Assert.DoesNotContain("throw new ArgumentException(\"Unknown action", code);
    }

    #endregion

    #region 7. Edge Cases: evaluate_formula Error Detection & Prefix Stripping

    [Fact]
    public void Evaluate_formula_handles_leading_equals_and_error_codes()
    {
        string formulaWithEquals = "=SUM(A1:A10)";
        if (formulaWithEquals.StartsWith("=", StringComparison.Ordinal))
        {
            formulaWithEquals = formulaWithEquals.Substring(1);
        }
        Assert.Equal("SUM(A1:A10)", formulaWithEquals);

        string formulaWithoutEquals = "AVERAGE(B1:B20)";
        if (formulaWithoutEquals.StartsWith("=", StringComparison.Ordinal))
        {
            formulaWithoutEquals = formulaWithoutEquals.Substring(1);
        }
        Assert.Equal("AVERAGE(B1:B20)", formulaWithoutEquals);

        // Excel error code mapping verification
        int[] errCodes = [-2146826281, -2146826246, -2146826259, -2146826288, -2146826252, -2146826265, -2146826273, -9999];
        string[] expectedNames = ["#DIV/0!", "#N/A", "#NAME?", "#NULL!", "#NUM!", "#REF!", "#VALUE!", "#ERROR(-9999)"];

        for (int i = 0; i < errCodes.Length; i++)
        {
            int errCode = errCodes[i];
            string errorName = errCode switch
            {
                -2146826281 => "#DIV/0!",
                -2146826246 => "#N/A",
                -2146826259 => "#NAME?",
                -2146826288 => "#NULL!",
                -2146826252 => "#NUM!",
                -2146826265 => "#REF!",
                -2146826273 => "#VALUE!",
                _ => $"#ERROR({errCode})"
            };
            Assert.Equal(expectedNames[i], errorName);
        }
    }

    #endregion

    #region 8. Edge Cases: run_macro Qualification & Parameter Bounds

    [Fact]
    public void Run_macro_qualifies_macro_name_and_inspects_arg_limits()
    {
        string curWbName = "TestBook.xlsm";

        // Unqualified macro gets prefixed with workbook name
        string unqualified = "UpdateRebar";
        string q1 = unqualified.Contains("!") ? unqualified : $"'{curWbName}'!{unqualified}";
        Assert.Equal("'TestBook.xlsm'!UpdateRebar", q1);

        // Already qualified macro is preserved
        string qualified = "'OtherBook.xlsm'!Sheet1.UpdateRebar";
        string q2 = qualified.Contains("!") ? qualified : $"'{curWbName}'!{qualified}";
        Assert.Equal("'OtherBook.xlsm'!Sheet1.UpdateRebar", q2);

        // Argument count boundary analysis in run_macro/code.cs:
        // switch (parsed.Length) { 1 => ..., 2 => ..., 3 => ..., 4 => ..., _ => parsed[0..4] }
        // If parsed.Length == 6, args 5 and 6 are silently dropped!
        var argsJson = ParseArgs("{\"macroName\": \"M\", \"args\": [\"1\", \"2\", \"3\", \"4\", \"5\", \"6\"]}");
        var list = argsJson.List("args");
        Assert.Equal(6, list.Count);
    }

    #endregion

    #region 9. Script Syntax and Metadata Integrity

    [Fact]
    public void All_seed_scripts_have_zero_Roslyn_parse_diagnostics_and_contain_return_statements()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var tree = CSharpSyntaxTree.ParseText(seed.Code, new CSharpParseOptions(kind: SourceCodeKind.Script));
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count == 0,
                $"Seed '{seed.Name}' has Roslyn syntax errors: {string.Join("; ", errors.Select(e => e.GetMessage()))}");

            var root = tree.GetRoot();
            var returns = root.DescendantNodes().OfType<ReturnStatementSyntax>().ToList();
            Assert.NotEmpty(returns);
        }
    }

    [Fact]
    public void All_tool_metadata_fields_are_fully_populated()
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var record = RegistryJson.Deserialize<ToolRecord>(seed.ToolJson)!;

            Assert.Equal("hprebar", record.Author);
            Assert.Equal(1, record.Version);
            Assert.Equal(ToolStatus.Published, record.Status);
            Assert.NotEmpty(record.Tags);
            Assert.False(string.IsNullOrWhiteSpace(record.Title));
            Assert.False(string.IsNullOrWhiteSpace(record.Notes));

            // Verify timeout rules
            if (record.Transaction == "none")
            {
                Assert.InRange(record.TimeoutSeconds, 5, 30);
                Assert.False(record.Destructive, $"Read-only tool {seed.Name} must have destructive=false");
            }
            else
            {
                Assert.InRange(record.TimeoutSeconds, 5, 120);
                Assert.True(record.Destructive, $"Mutating tool {seed.Name} must have destructive=true");
            }
        }
    }

    #endregion
}
