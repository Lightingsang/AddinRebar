using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EmpiricalSeedChallenger;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("============================================================");
        Console.WriteLine("  EMPIRICAL CHALLENGER: HPDynamo Seeds, Roslyn & Guards     ");
        Console.WriteLine("============================================================");

        var assembly = typeof(DynamoHostProfile).Assembly;
        var seeds = SeedInstaller.LoadSeeds(assembly);
        Console.WriteLine($"[1] Loaded {seeds.Count} embedded seed tools from HPDynamo.Mcp.Server.");
        if (seeds.Count != 12)
        {
            Console.WriteLine($"FAIL: Expected 12 seeds, got {seeds.Count}");
            return 1;
        }

        var hostRefs = ResolveHostReferences();
        if (hostRefs == null || hostRefs.Count == 0)
        {
            Console.WriteLine("FAIL: Host reference assemblies for Revit/Dynamo could not be resolved!");
            return 1;
        }
        Console.WriteLine($"[2] Resolved {hostRefs.Count} host references (Revit/Dynamo assemblies).");

        // PART 1: Roslyn compilation of all 12 seeds
        Console.WriteLine("\n--- PART 1: Roslyn CSharpCompilation for All 12 Seeds ---");
        int compilePassed = 0;
        foreach (var seed in seeds)
        {
            var errors = CompileSeed(seed.Code, hostRefs);
            if (errors.Length == 0)
            {
                Console.WriteLine($"  [PASS] {seed.Category}/{seed.Name}: 0 compilation errors.");
                compilePassed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {seed.Category}/{seed.Name} failed to compile:");
                foreach (var err in errors) Console.WriteLine($"         {err}");
            }
        }
        if (compilePassed != 12)
        {
            Console.WriteLine($"FAIL: Only {compilePassed}/12 seeds compiled cleanly.");
            return 1;
        }
        Console.WriteLine("=> ALL 12 SEEDS COMPILED WITH 0 ERRORS.");

        // PART 2: Adversarial Roslyn Compilation Challenge
        Console.WriteLine("\n--- PART 2: Adversarial Seed Compilation Mutation Challenge ---");
        var badSyntaxErrors = CompileSeed("int x = 10", hostRefs);
        if (badSyntaxErrors.Any(e => e.Contains("CS1002")))
            Console.WriteLine("  [PASS] Missing semicolon correctly rejected with CS1002.");
        else
        {
            Console.WriteLine("  [FAIL] Missing semicolon did not trigger CS1002.");
            return 1;
        }

        var badTypeErrors = CompileSeed("int x = \"hello\"; return x;", hostRefs);
        if (badTypeErrors.Any(e => e.Contains("CS0029")))
            Console.WriteLine("  [PASS] Type mismatch correctly rejected with CS0029.");
        else
        {
            Console.WriteLine("  [FAIL] Type mismatch did not trigger CS0029.");
            return 1;
        }

        var badApiErrors = CompileSeed("return workspace.NonExistentDynamoMethod(123);", hostRefs);
        if (badApiErrors.Any(e => e.Contains("CS1061")))
            Console.WriteLine("  [PASS] Invalid Dynamo API call correctly rejected with CS1061.");
        else
        {
            Console.WriteLine("  [FAIL] Invalid Dynamo API call did not trigger CS1061.");
            return 1;
        }

        // PART 3: ScriptGuard on all 12 Seeds
        Console.WriteLine("\n--- PART 3: ScriptGuard Compliance for All 12 Seeds ---");
        int guardPassed = 0;
        foreach (var seed in seeds)
        {
            var violations = ScriptGuard.Check(seed.Code, GuardProfile.Dynamo);
            if (violations.Count == 0)
            {
                Console.WriteLine($"  [PASS] {seed.Category}/{seed.Name}: 0 guard violations.");
                guardPassed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {seed.Category}/{seed.Name} has {violations.Count} guard violations:");
                foreach (var v in violations) Console.WriteLine($"         {v.Message}");
            }
        }
        if (guardPassed != 12)
        {
            Console.WriteLine($"FAIL: Only {guardPassed}/12 seeds passed ScriptGuard.");
            return 1;
        }
        Console.WriteLine("=> ALL 12 SEEDS PASSED SCRIPTGUARD WITH 0 VIOLATIONS.");

        // PART 4: Adversarial ScriptGuard Challenge
        Console.WriteLine("\n--- PART 4: Adversarial ScriptGuard Mutation Challenge ---");
        string[] maliciousSnippets =
        [
            "System.IO.File.Delete(\"test\"); return 1;",
            "System.Diagnostics.Process.Start(\"cmd.exe\"); return 1;",
            "System.Windows.Forms.MessageBox.Show(\"hacked\"); return 1;",
            "TaskDialog.Show(\"Title\", \"Msg\"); return 1;",
            "global::System.IO.File.WriteAllText(\"a\", \"b\"); return 1;",
            "#r \"some.dll\"\nreturn 1;",
            "#load \"other.csx\"\nreturn 1;"
        ];

        foreach (var snippet in maliciousSnippets)
        {
            var v = ScriptGuard.Check(snippet, GuardProfile.Dynamo);
            if (v.Count > 0)
            {
                Console.WriteLine($"  [PASS] Attack snippet rejected: '{snippet.Split('\n')[0]}' -> {v[0].Message}");
            }
            else
            {
                Console.WriteLine($"  [FAIL] Malicious snippet was NOT rejected: '{snippet}'");
                return 1;
            }
        }

        // PART 5: ScriptQuality on all 12 Seeds
        Console.WriteLine("\n--- PART 5: ScriptQuality Validation for All 12 Seeds ---");
        int qualityPassed = 0;
        foreach (var seed in seeds)
        {
            var findings = ScriptQuality.Find(seed.Code);
            var errs = findings.Where(f => f.Severity == ScriptQuality.Error).ToList();
            var warns = findings.Where(f => f.Severity == ScriptQuality.Warning).ToList();

            if (errs.Count == 0 && warns.Count == 0)
            {
                Console.WriteLine($"  [PASS] {seed.Category}/{seed.Name}: 0 errors, 0 warnings.");
                qualityPassed++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {seed.Category}/{seed.Name}: {errs.Count} errs, {warns.Count} warns:");
                foreach (var f in findings) Console.WriteLine($"         [{f.RuleId}] {f.Message} (Line {f.Line})");
            }
        }
        if (qualityPassed != 12)
        {
            Console.WriteLine($"FAIL: Only {qualityPassed}/12 seeds passed ScriptQuality.");
            return 1;
        }
        Console.WriteLine("=> ALL 12 SEEDS PASSED SCRIPTQUALITY (0 ERRORS, 0 WARNINGS).");

        // PART 6: Adversarial ScriptQuality Challenge
        Console.WriteLine("\n--- PART 6: Adversarial ScriptQuality Mutation Challenge ---");
        var commentedCode = "int a = 1;\n// int b = 2;\nreturn a;";
        var qb1Findings = ScriptQuality.Find(commentedCode).Where(f => f.RuleId == "Q-B1").ToList();
        if (qb1Findings.Count > 0)
            Console.WriteLine("  [PASS] Commented-out code flagged with Q-B1.");
        else
        {
            Console.WriteLine("  [FAIL] Commented-out code not caught by Q-B1.");
            return 1;
        }

        var emptyCatch = "try { int x = 0; } catch (Exception) { } return 1;";
        var qb2Findings = ScriptQuality.Find(emptyCatch).Where(f => f.RuleId == "Q-B2").ToList();
        if (qb2Findings.Count > 0)
            Console.WriteLine("  [PASS] Empty catch without reason flagged with Q-B2.");
        else
        {
            Console.WriteLine("  [FAIL] Empty catch not caught by Q-B2.");
            return 1;
        }

        Console.WriteLine("\n============================================================");
        Console.WriteLine("  ALL 6 EMPIRICAL CHALLENGES COMPLETED: 100% SUCCESS        ");
        Console.WriteLine("============================================================");
        return 0;
    }

    private static string[] CompileSeed(string code, List<MetadataReference> hostRefs)
    {
        var source = $$"""
            using System;
            using System.IO;
            using System.Linq;
            using System.Collections.Generic;
            using System.Text.Json;
            using Autodesk.DesignScript.Geometry;
            using Dynamo.Graph.Nodes;
            using Dynamo.Graph.Workspaces;
            using Dynamo.Models;
            using Dynamo.ViewModels;
            using Dynamo.Applications;
            using Revit.Elements;
            using HPRebar.McpBridge.Core.Scripting;
            using Autodesk.Revit.DB;
            using Autodesk.Revit.UI;

            public sealed class SeedHost
            {
                public object dynamo;
                public WorkspaceModel workspace;
                public Document doc;
                public UIDocument uidoc;
                public UIApplication uiapp;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int?, string?> progress;
                public ScriptArgs args;

                public object Run()
                {
            #line 1 "code.cs"
            {{code}}
                }
            }
            """;

        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is var f && (f.StartsWith("System.", StringComparison.Ordinal) || f is "netstandard.dll" or "mscorlib.dll"))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

        var sharedRefs = new[]
        {
            MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonElement).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PipeNaming).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DynamoHostProfile).Assembly.Location)
        };

        var allReferences = trusted.Concat(sharedRefs).Concat(hostRefs);

        var compilation = CSharpCompilation.Create(
            "dynamo_seed_test_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            allReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Location.GetLineSpan().StartLinePosition.Line + 1}:{d.Location.GetLineSpan().StartLinePosition.Character + 1} {d.Id} {d.GetMessage()}")
            .ToArray();
    }

    private static List<MetadataReference>? ResolveHostReferences()
    {
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var revitBase = Path.Combine(progFiles, "Autodesk", "Revit 2026");
        var dynamoBase = Path.Combine(revitBase, "AddIns", "DynamoForRevit");
        var dirs = new[] { revitBase, dynamoBase, Path.Combine(dynamoBase, "Revit") }
            .Where(Directory.Exists).ToArray();

        string[] dlls =
        [
            "RevitAPI.dll", "RevitAPIUI.dll", "DynamoCore.dll",
            "DynamoCoreWpf.dll", "DynamoApplications.dll", "RevitNodes.dll",
            "ProtoCore.dll", "ProtoGeometry.dll", "DynamoServices.dll", "DynamoRevitDS.dll"
        ];

        var refs = new List<MetadataReference>();
        foreach (var dll in dlls)
        {
            var found = dirs.Select(d => Path.Combine(d, dll)).FirstOrDefault(File.Exists);
            if (found != null) refs.Add(MetadataReference.CreateFromFile(found));
        }

        return refs;
    }
}
