using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

public sealed class ScriptQualityTests
{
    private static IReadOnlyList<string> Rules(string code) => ScriptQuality.Find(code).Select(f => f.RuleId).ToList();

    // ---- Q-B1 commented-out code ----

    [Theory]
    [InlineData("// var x = 1;\nreturn 0;")]
    [InlineData("// DoIt(args.Int(\"count\"));\nreturn 0;")]
    [InlineData("/* var total = items.Sum(i => i.Length); */\nreturn 0;")]
    [InlineData("// if (count > 0)\n// {\n//     count = 0;\n// }\nreturn 0;")]
    [InlineData("// Old approach:\n// var y = Compute(2);\nreturn 0;")]
    public void Find_CommentedOutCode_IsAnError(string code)
    {
        var finding = Assert.Single(ScriptQuality.Find(code), f => f.RuleId == "Q-B1");

        Assert.Equal(ScriptQuality.Error, finding.Severity);
    }

    [Theory]
    [InlineData("// one property condition: equals (display string), contains, wildcard, gt / lt (numeric); bad input is the caller's error")]
    [InlineData("// After any save ETABS names its working copy `.$et` beside the `.EDB`; the model the user (and the snapshots) know is the `.EDB`.")]
    [InlineData("// A load case that exists but was never run has no results: say so instead of reporting an empty table (status 4 = finished).")]
    [InlineData("// v2")]
    [InlineData("// mm")]
    [InlineData("// see https://example.com/doc?x=1;")]
    [InlineData("// TODO(sang): replace with Compute(x);")]
    [InlineData("// EXTMIN/EXTMAX hold ±1e20 sentinels until the drawing has extents; they refresh on regen/save.")]
    [InlineData("// Remove later;")]
    [InlineData("// Example: args.Double(\"spacing\", 150);")]
    [InlineData("// e.g. args.Str(\"name\");")]
    [InlineData("/* Usage: DoIt(args.Int(\"count\")); */")]
    [InlineData("// Default: Compute(spacing);")]
    public void Find_ProseComment_IsNotCode(string comment)
    {
        Assert.DoesNotContain("Q-B1", Rules(comment + "\nreturn 0;"));
    }

    [Fact]
    public void Find_TrailingProseComment_IsNotCode()
    {
        Assert.DoesNotContain("Q-B1", Rules("var dxf = id.Name; // custom ARX classes may carry no DXF name\nreturn dxf;"));
    }

    // ---- Q-B2 empty catch ----

    [Fact]
    public void Find_EmptyCatch_IsAnError()
    {
        var finding = Assert.Single(ScriptQuality.Find("try { return 1; }\ncatch { }\nreturn 0;"));

        Assert.Equal(("Q-B2", ScriptQuality.Error, 2), (finding.RuleId, finding.Severity, finding.Line));
    }

    [Theory]
    [InlineData("try { return 1; } catch (System.InvalidOperationException) { /* no finite extents */ }\nreturn 0;")]
    [InlineData("try { return 1; }\ncatch (System.InvalidOperationException)\n{\n    // some entities have no finite extents\n}\nreturn 0;")]
    public void Find_EmptyCatchWithReason_IsClean(string code)
    {
        Assert.Empty(ScriptQuality.Find(code));
    }

    // ---- Q-B3 length ----

    [Fact]
    public void Find_ScriptOver300Lines_IsAnError()
    {
        var code = string.Concat(Enumerable.Repeat("var a = 1;\n", 300)) + "return 0;";

        Assert.Contains("Q-B3", Rules(code));
    }

    [Fact]
    public void Find_Script300LinesWithBlankEdges_IsClean()
    {
        var code = "\n\n" + string.Concat(Enumerable.Repeat("var a = 1;\n", 299)) + "return 0;\n\n\n";

        Assert.DoesNotContain("Q-B3", Rules(code));
    }

    // ---- Q-W1 long block ----

    [Fact]
    public void Find_LocalFunctionOver50Lines_Warns()
    {
        var body = string.Concat(Enumerable.Repeat("    var a = 1;\n", 50));
        var finding = Assert.Single(ScriptQuality.Find("int Count()\n{\n" + body + "    return 1;\n}\nreturn Count();"), f => f.RuleId == "Q-W1");

        Assert.Equal(ScriptQuality.Warning, finding.Severity);
    }

    [Fact]
    public void Find_LocalFunctionInsideABlockOver50Lines_Warns()
    {
        var body = string.Concat(Enumerable.Repeat("        var a = 1;\n", 50));
        var code = "var total = 0;\nif (total == 0)\n{\n    int Count()\n    {\n" + body + "        return 1;\n    }\n    total = Count();\n}\nreturn total;";

        Assert.Single(ScriptQuality.Find(code), f => f.RuleId == "Q-W1");
    }

    [Fact]
    public void Find_LongTopLevelBody_DoesNotWarn()
    {
        var code = string.Concat(Enumerable.Repeat("var a = 1;\n", 120)) + "return a;";

        Assert.DoesNotContain("Q-W1", Rules(code));
    }

    // ---- Q-W2 nesting ----

    [Fact]
    public void Find_FourNestedLevels_Warns()
    {
        const string code = "for (var i = 0; i < 2; i++) { if (i > 0) { foreach (var c in \"ab\") { while (i < 1) { i++; } } } }\nreturn 0;";

        Assert.Single(ScriptQuality.Find(code), f => f.RuleId == "Q-W2");
    }

    [Theory]
    [InlineData("for (var i = 0; i < 2; i++) { if (i > 0) { foreach (var c in \"ab\") { i++; } } }\nreturn 0;")]
    [InlineData("var n = 1;\nif (n == 1) { n = 2; } else if (n == 2) { n = 3; } else if (n == 3) { n = 4; } else if (n == 4) { n = 5; }\nreturn n;")]
    [InlineData("for (var i = 0; i < 2; i++) { if (i > 0) { try { foreach (var c in \"ab\") { i++; } } catch (System.Exception e) { throw new System.InvalidOperationException(e.Message); } } }\nreturn 0;")]
    public void Find_ThreeLevelsElseIfChainOrTry_DoesNotWarn(string code)
    {
        Assert.DoesNotContain("Q-W2", Rules(code));
    }

    [Fact]
    public void Find_LocalFunctionStartsItsOwnNesting()
    {
        const string code = "for (var i = 0; i < 2; i++) { if (i > 0) { foreach (var c in \"ab\") { int Twice(int n) { if (n > 0) { return n * 2; } return 0; } i += Twice(1); } } }\nreturn 0;";

        Assert.DoesNotContain("Q-W2", Rules(code));
    }

    [Fact]
    public void Find_LambdaBlockCountsAsALevel()
    {
        const string code = "var sum = 0;\nfor (var i = 0; i < 2; i++) { if (i > 0) { foreach (var c in \"ab\") { System.Action add = () => { sum++; }; add(); } } }\nreturn sum;";

        Assert.Single(ScriptQuality.Find(code), f => f.RuleId == "Q-W2");
    }

    // ---- Q-W3 vague names ----

    [Theory]
    [InlineData("var data = 1;\nreturn data;")]
    [InlineData("var tmp = 1;\nreturn tmp;")]
    [InlineData("foreach (var obj in new[] { 1 }) { }\nreturn 0;")]
    [InlineData("int Twice(int val) => val * 2;\nreturn Twice(1);")]
    [InlineData("try { return 1; } catch (System.Exception tmp) { throw new System.InvalidOperationException(tmp.Message); }")]
    public void Find_VagueName_Warns(string code)
    {
        Assert.Contains("Q-W3", Rules(code));
    }

    [Theory]
    [InlineData("double x = 1, y = 2, x1 = 3, x2 = 4;\nreturn x + y + x1 + x2;")]
    [InlineData("var result = 1;\nvar item = 2;\nvar value = 3;\nreturn result + item + value;")]
    public void Find_CoordinateAndCommonNames_AreNotVague(string code)
    {
        Assert.DoesNotContain("Q-W3", Rules(code));
    }

    // ---- Q-W4 bool parameter ----

    [Theory]
    [InlineData("int Pick(int a, bool fast) { return fast ? a : 0; }\nreturn Pick(1, true);")]
    [InlineData("return Run(1, null);\nint Run(int a, bool? quick) => a;")]
    public void Find_BoolParameterOnFunction_Warns(string code)
    {
        Assert.Contains("Q-W4", Rules(code));
    }

    // ---- Q-W5 swallowing catch ----

    [Theory]
    [InlineData("var count = 0;\ntry { count = 1; }\ncatch (System.Exception) { count = -1; }\nreturn count;")]
    [InlineData("var count = 0;\ntry { count = 1; }\ncatch { count = -1; }\nreturn count;")]
    public void Find_CatchExceptionThatSwallows_Warns(string code)
    {
        Assert.Single(ScriptQuality.Find(code), f => f.RuleId == "Q-W5");
    }

    [Theory]
    [InlineData("try { return 1; } catch (System.Exception) { throw; }")]
    [InlineData("try { return 1; } catch (Exception) { return -1; }")]
    [InlineData("var errors = new List<string>();\ntry { return 1; } catch (Exception ex) { errors.Add(ex.Message); }\nreturn errors.Count;")]
    [InlineData("var count = 0;\ntry { count = 1; } catch (System.FormatException) { count = -1; }\nreturn count;")]
    public void Find_CatchThatReportsOrIsNarrow_DoesNotWarn(string code)
    {
        Assert.DoesNotContain("Q-W5", Rules(code));
    }

    // ---- analyzer integration ----

    [Fact]
    public void Analyze_ReportsQualityFindingsAndMarksAnalysed()
    {
        var result = ScriptAnalyzer.Analyze("// var x = 1;\nvar data = 2;\nreturn data;");

        Assert.True(result.QualityAnalysed);
        Assert.Equal(["Q-B1", "Q-W3"], result.QualityFindings.Select(f => f.RuleId).ToArray());
    }

    [Fact]
    public void Analyze_CleanScript_HasNoFindings()
    {
        var result = ScriptAnalyzer.Analyze("double spacing = args.Double(\"spacing\", 150);\nreturn spacing * 2;");

        Assert.True(result.QualityAnalysed);
        Assert.Empty(result.QualityFindings);
    }
}
