using System;
using System.IO;
using System.Text.RegularExpressions;
using HPRebar.McpBridge.Core.Scripting;

namespace TestApp;

public class Program
{
    public static void Main()
    {
        string baseDir = @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary";

        // Test 1: validate_dyn_file
        string valCode = File.ReadAllText(Path.Combine(baseDir, "Graph", "validate_dyn_file", "code.cs"));
        string fixedVal = Regex.Replace(valCode, @"nodeElement\.GetProperty\([^)]+\)\.GetString\(\)", "\"test\"");
        var diagsVal = ScriptGuard.Check(fixedVal, GuardProfile.Dynamo);
        Console.WriteLine($"validate_dyn_file without GetProperty: {diagsVal.Count} diagnostic(s)");
        foreach (var d in diagsVal) Console.WriteLine("  " + d.Message);

        // Test 2: inspect_node_type
        string inspCode = File.ReadAllText(Path.Combine(baseDir, "Library", "inspect_node_type", "code.cs"));
        string fixedInsp = Regex.Replace(inspCode, @"targetEntry\.Assembly", "targetEntry.FullName");
        var diagsInsp = ScriptGuard.Check(fixedInsp, GuardProfile.Dynamo);
        Console.WriteLine($"inspect_node_type without .Assembly: {diagsInsp.Count} diagnostic(s)");
        foreach (var d in diagsInsp) Console.WriteLine("  " + d.Message);

        // Test 3: search_nodes_library
        string searchCode = File.ReadAllText(Path.Combine(baseDir, "Library", "search_nodes_library", "code.cs"));
        string fixedSearch = Regex.Replace(searchCode, @"searchEntry\.Assembly", "searchEntry.FullName");
        var diagsSearch = ScriptGuard.Check(fixedSearch, GuardProfile.Dynamo);
        Console.WriteLine($"search_nodes_library without .Assembly: {diagsSearch.Count} diagnostic(s)");
        foreach (var d in diagsSearch) Console.WriteLine("  " + d.Message);
    }
}
