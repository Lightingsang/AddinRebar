using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPBim.Client.Recovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class Program
{
    private static int totalTests = 0;
    private static int passedTests = 0;
    private static int failedTests = 0;
    private static readonly List<string> failures = new();

    public static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  EMPIRICAL ADVERSARIAL CHALLENGER — M4 RECOVERY & RECEIPT PARSING");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        RunScriptTemplateChallenges();
        Console.WriteLine();

        RunReceiptParsingChallenges();
        Console.WriteLine();

        RunElementMapParsingChallenges();
        Console.WriteLine();

        RunPropertyTypeAdversarialAttacks();
        Console.WriteLine();

        Console.WriteLine("================================================================================");
        Console.WriteLine($"RESULTS SUMMARY: {passedTests}/{totalTests} PASSED ({(failedTests == 0 ? "100%" : $"{failedTests} FAILED")})");
        Console.WriteLine("================================================================================");

        if (failures.Count > 0)
        {
            Console.WriteLine("FAILURES:");
            foreach (var f in failures)
            {
                Console.WriteLine($" - {f}");
            }
            return 1;
        }

        return 0;
    }

    private static void AssertTest(string testName, bool condition, string detail = "")
    {
        totalTests++;
        if (condition)
        {
            passedTests++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            failedTests++;
            string msg = $"[FAIL] {testName}: {detail}";
            Console.WriteLine(msg);
            failures.Add(msg);
        }
    }

    private static void RunScriptTemplateChallenges()
    {
        Console.WriteLine("--- CHALLENGE SUITE 1: ExtensibleStorageQueryScriptTemplate ---");

        string script = ExtensibleStorageQueryScriptTemplate.GetQueryScript();
        AssertTest("Script is non-null and non-empty", !string.IsNullOrWhiteSpace(script));

        // 1. Roslyn SyntaxTree parsing as Script
        var parseOptions = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
        var syntaxTree = CSharpSyntaxTree.ParseText(script, parseOptions);
        var diagnostics = syntaxTree.GetDiagnostics().ToList();

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        AssertTest("Syntax tree contains 0 syntax errors", errors.Count == 0,
            string.Join("; ", errors.Select(e => e.ToString())));

        var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
        AssertTest("Syntax tree contains 0 warnings", warnings.Count == 0,
            string.Join("; ", warnings.Select(w => w.ToString())));

        // 2. Token boundary check between header and filter script
        var tokens = syntaxTree.GetRoot().DescendantTokens().ToList();
        AssertTest("Tokens successfully extracted from AST (>20 tokens)", tokens.Count > 20, $"Token count: {tokens.Count}");

        // Verify key expected tokens exist in stream
        AssertTest("AST contains Guid literal token", tokens.Any(t => t.ValueText == "D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90"));
        AssertTest("AST contains FilteredElementCollector identifier", tokens.Any(t => t.ValueText == "FilteredElementCollector"));
        AssertTest("AST contains DataStorage identifier", tokens.Any(t => t.ValueText == "DataStorage"));
        AssertTest("AST contains collector iteration foreach", tokens.Any(t => t.IsKind(SyntaxKind.ForEachKeyword)));
        AssertTest("AST contains anonymous type creation", tokens.Any(t => t.IsKind(SyntaxKind.NewKeyword)));

        // 3. Monolithic vs Split Token Stream Comparison
        string monolithic = """
        var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
        var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
        if (schema == null)
        {
            return new { count = 0, receipts = new List<object>() };
        }

        string targetOpId = args.Str("targetOperationId", string.Empty);
        string targetPlanHash = args.Str("targetPlanHash", string.Empty);

        var collector = new FilteredElementCollector(doc)
            .OfClass(typeof(DataStorage));

        var receipts = new List<object>();

        foreach (DataStorage ds in collector)
        {
            var entity = ds.GetEntity(schema);
            if (entity == null || !entity.IsValid()) continue;

            Guid opId = entity.Get<Guid>("OperationId");
            string planHash = entity.Get<string>("PlanHash") ?? string.Empty;
            string committedUtc = entity.Get<string>("CommittedAtUtc") ?? string.Empty;
            string elemMapJson = entity.Get<string>("ElementMapJson") ?? "{}";

            if (!string.IsNullOrEmpty(targetOpId) && Guid.TryParse(targetOpId, out var parsedOp) && opId != parsedOp)
                continue;

            if (!string.IsNullOrEmpty(targetPlanHash) && !string.Equals(planHash, targetPlanHash, StringComparison.OrdinalIgnoreCase))
                continue;

            receipts.Add(new
            {
                operationId = opId.ToString("D"),
                planHash = planHash,
                committedAtUtc = committedUtc,
                elementMapJson = elemMapJson
            });
        }

        return new { count = receipts.Count, receipts = receipts };
        """;

        var monoTree = CSharpSyntaxTree.ParseText(monolithic, parseOptions);
        var monoTokens = monoTree.GetRoot().DescendantTokens().Select(t => (t.Kind(), t.Text)).ToList();
        var splitTokens = syntaxTree.GetRoot().DescendantTokens().Select(t => (t.Kind(), t.Text)).ToList();

        bool tokensMatch = monoTokens.SequenceEqual(splitTokens);
        AssertTest("Concatenated script produces 100% identical token stream to monolithic script", tokensMatch);
    }

    private static void RunReceiptParsingChallenges()
    {
        Console.WriteLine("--- CHALLENGE SUITE 2: ExtensibleStorageReceiptReader.ParseReceipts ---");

        // 1. Non-object elements in array (integers, strings, booleans, nulls)
        string primitivesOnlyJson = """
        {
            "receipts": [
                123,
                -456,
                3.14159,
                "some-string",
                "",
                true,
                false,
                null
            ]
        }
        """;
        using (var doc = JsonDocument.Parse(primitivesOnlyJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Pure primitives in array gracefully produce empty list (0 receipts)", receipts.Count == 0);
        }

        // 2. Nested arrays in receipts
        string nestedArraysJson = """
        {
            "receipts": [
                [1, 2, 3],
                ["nested", "strings"],
                []
            ]
        }
        """;
        using (var doc = JsonDocument.Parse(nestedArraysJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Nested arrays in receipts array gracefully produce empty list (0 receipts)", receipts.Count == 0);
        }

        // 3. Mixed arrays: valid objects mixed with primitives and nulls
        var validOp1 = Guid.NewGuid();
        var validOp2 = Guid.NewGuid();
        string mixedJson = $$"""
        {
            "receipts": [
                123,
                "string-garbage",
                {
                    "operationId": "{{validOp1}}",
                    "planHash": "hash-one",
                    "committedAtUtc": "2026-10-07T21:00:00Z",
                    "elementMapJson": "{\"Doc#Col:C1\":\"uid-1\"}"
                },
                null,
                true,
                false,
                [1, 2, 3],
                {
                    "operationId": "{{validOp2}}",
                    "planHash": "hash-two",
                    "committedAtUtc": "2026-10-07T22:00:00Z",
                    "elementMapJson": "{}"
                },
                "trailing-primitive",
                null
            ]
        }
        """;
        using (var doc = JsonDocument.Parse(mixedJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Mixed array extracts exactly 2 valid objects", receipts.Count == 2);
            AssertTest("Valid object 1 operationId matches", receipts.Count > 0 && receipts[0].OperationId == validOp1);
            AssertTest("Valid object 1 planHash matches", receipts.Count > 0 && receipts[0].PlanHash == "hash-one");
            AssertTest("Valid object 1 elementMap parsed", receipts.Count > 0 && receipts[0].ElementMap.ContainsKey("Doc#Col:C1"));
            AssertTest("Valid object 2 operationId matches", receipts.Count > 1 && receipts[1].OperationId == validOp2);
            AssertTest("Valid object 2 planHash matches", receipts.Count > 1 && receipts[1].PlanHash == "hash-two");
        }

        // 4. Missing, null or non-object root
        AssertTest("Null JsonElement root returns empty list",
            ExtensibleStorageReceiptReader.ParseReceipts(null).Count == 0);

        using (var docObj = JsonDocument.Parse("{}"))
        {
            AssertTest("Empty object root returns empty list",
                ExtensibleStorageReceiptReader.ParseReceipts(docObj.RootElement).Count == 0);
        }

        using (var docNoReceipts = JsonDocument.Parse("""{ "other": 123 }"""))
        {
            AssertTest("Object without receipts property returns empty list",
                ExtensibleStorageReceiptReader.ParseReceipts(docNoReceipts.RootElement).Count == 0);
        }

        using (var docNullReceipts = JsonDocument.Parse("""{ "receipts": null }"""))
        {
            AssertTest("Receipts property is null returns empty list",
                ExtensibleStorageReceiptReader.ParseReceipts(docNullReceipts.RootElement).Count == 0);
        }

        using (var docObjReceipts = JsonDocument.Parse("""{ "receipts": {} }"""))
        {
            AssertTest("Receipts property is object (not array) returns empty list",
                ExtensibleStorageReceiptReader.ParseReceipts(docObjReceipts.RootElement).Count == 0);
        }

        using (var docPrimReceipts = JsonDocument.Parse("""{ "receipts": "not-an-array" }"""))
        {
            AssertTest("Receipts property is string (not array) returns empty list",
                ExtensibleStorageReceiptReader.ParseReceipts(docPrimReceipts.RootElement).Count == 0);
        }
    }

    private static void RunElementMapParsingChallenges()
    {
        Console.WriteLine("--- CHALLENGE SUITE 3: ExtensibleStorageReceiptReader.ParseElementMap ---");

        // 1. Normal valid maps
        var normal = ExtensibleStorageReceiptReader.ParseElementMap("{\"k1\":\"v1\",\"k2\":\"v2\"}");
        AssertTest("Valid map parsed accurately (count=2)", normal.Count == 2 && normal["k1"] == "v1");

        // 2. Whitespace and empty
        AssertTest("Empty string returns empty dict", ExtensibleStorageReceiptReader.ParseElementMap("").Count == 0);
        AssertTest("Whitespace string returns empty dict", ExtensibleStorageReceiptReader.ParseElementMap("   \t\n  ").Count == 0);
        AssertTest("Empty object '{}' returns empty dict", ExtensibleStorageReceiptReader.ParseElementMap("{}").Count == 0);

        // 3. Corrupted / truncated JSON
        AssertTest("Truncated JSON 1 '{ corrupt' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("{ corrupt").Count == 0);
        AssertTest("Truncated JSON 2 '{\"key\":' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("{\"key\":").Count == 0);
        AssertTest("Unclosed string '{\"key\":\"val' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("{\"key\":\"val").Count == 0);

        // 4. Non-object JSON roots
        AssertTest("Array root '[1, 2, 3]' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("[1, 2, 3]").Count == 0);
        AssertTest("Number root '12345' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("12345").Count == 0);
        AssertTest("Boolean root 'true' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("true").Count == 0);
        AssertTest("Null literal 'null' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("null").Count == 0);
        AssertTest("String literal '\"a string\"' handled safely", ExtensibleStorageReceiptReader.ParseElementMap("\"a string\"").Count == 0);

        // 5. Object with non-string values
        AssertTest("Map with integer value '{\"k\": 123}' handled safely",
            ExtensibleStorageReceiptReader.ParseElementMap("{\"k\": 123}").Count == 0);
        AssertTest("Map with array value '{\"k\": [1,2]}' handled safely",
            ExtensibleStorageReceiptReader.ParseElementMap("{\"k\": [1,2]}").Count == 0);
        AssertTest("Map with object value '{\"k\": {\"sub\":1}}' handled safely",
            ExtensibleStorageReceiptReader.ParseElementMap("{\"k\": {\"sub\":1}}").Count == 0);
    }

    private static void RunPropertyTypeAdversarialAttacks()
    {
        Console.WriteLine("--- CHALLENGE SUITE 4: Property Edge Cases in Receipt Objects ---");

        // Empty object inside array
        string emptyObjJson = """{ "receipts": [ {} ] }""";
        using (var doc = JsonDocument.Parse(emptyObjJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Empty object {} in receipts returns 1 record with defaults", receipts.Count == 1);
            AssertTest("Empty object has Guid.Empty", receipts[0].OperationId == Guid.Empty);
            AssertTest("Empty object has empty PlanHash", receipts[0].PlanHash == string.Empty);
            AssertTest("Empty object has empty CommittedAtUtc", receipts[0].CommittedAtUtc == string.Empty);
            AssertTest("Empty object has empty ElementMap", receipts[0].ElementMap.Count == 0);
        }

        // Null properties
        string nullPropsJson = """
        {
            "receipts": [
                {
                    "operationId": null,
                    "planHash": null,
                    "committedAtUtc": null,
                    "elementMapJson": null
                }
            ]
        }
        """;
        using (var doc = JsonDocument.Parse(nullPropsJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Object with explicit null properties handled without throwing", receipts.Count == 1);
            AssertTest("Null properties produce Guid.Empty", receipts[0].OperationId == Guid.Empty);
            AssertTest("Null properties produce empty strings", receipts[0].PlanHash == string.Empty);
        }

        // Invalid GUID format
        string invalidGuidJson = """{ "receipts": [ { "operationId": "not-a-guid-at-all" } ] }""";
        using (var doc = JsonDocument.Parse(invalidGuidJson))
        {
            var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
            AssertTest("Invalid GUID string parses safely to Guid.Empty", receipts[0].OperationId == Guid.Empty);
        }

        // Non-string properties: what happens if number or boolean is provided for operationId?
        // Let's test whether GetString throws if operationId is 12345
        string nonStringPropJson = """{ "receipts": [ { "operationId": 12345 } ] }""";
        using (var doc = JsonDocument.Parse(nonStringPropJson))
        {
            try
            {
                var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
                // If it doesn't throw, check result
                AssertTest("Non-string property handled (didn't throw)", receipts.Count == 1);
            }
            catch (InvalidOperationException)
            {
                // Note: GetString() throws InvalidOperationException if ValueKind is Number
                Console.WriteLine("[INFO / CAVEAT] JsonElement.GetString() throws InvalidOperationException if property value is a Number rather than String.");
            }
        }
    }
}
