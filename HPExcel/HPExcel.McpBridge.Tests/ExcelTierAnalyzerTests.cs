using HPExcel.McpBridge.Safety;
using Xunit;

namespace HPExcel.McpBridge.Tests;

public class ExcelTierAnalyzerTests
{
    [Fact]
    public void LocalVariableReassignment_RemainsReadOnly()
    {
        var code = @"
int total = 0;
total = total + 10;
total += 5;
return total;
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.ReadOnly, result.HighestTier);
        Assert.Empty(result.WriteMembers);
        Assert.Empty(result.DestructiveMembers);
    }

    [Fact]
    public void MemberPropertyAssignment_ElevatesToWrite()
    {
        var code = @"
range.Value = 123;
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.Write, result.HighestTier);
        Assert.Contains(result.WriteMembers, m => m.Contains("Value"));
    }

    [Fact]
    public void ElementAccessAssignment_ElevatesToWrite()
    {
        var code = @"
sheet.Cells[1, 1] = 456;
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.Write, result.HighestTier);
    }

    [Fact]
    public void MutatingInvocations_ClassifiedAsWrite()
    {
        var code = @"
range.Sort();
range.Insert();
range.Merge();
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.Write, result.HighestTier);
    }

    [Fact]
    public void DestructiveInvocations_ClassifiedAsDestructive()
    {
        var code = @"
range.ClearContents();
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.Destructive, result.HighestTier);
        Assert.Contains(result.DestructiveMembers, m => m.Contains("ClearContents"));
    }

    [Fact]
    public void ComplexReadCalculations_RemainReadOnly()
    {
        var code = @"
var list = new List<int> { 1, 2, 3, 4, 5 };
var sum = list.Where(x => x > 2).Sum();
double avg = (double)sum / list.Count;
string report = string.Format(""Avg: {0:F2}"", avg);
log(report);
return new { sum, avg, report };
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.ReadOnly, result.HighestTier);
        Assert.Empty(result.WriteMembers);
        Assert.Empty(result.DestructiveMembers);
    }

    [Fact]
    public void FormattingAndStylingProperties_ClassifiedAsWrite()
    {
        var code = @"
range.Font.Bold = true;
range.Interior.Color = 0xFF0000;
range.NumberFormat = ""$#,##0.00"";
range.HorizontalAlignment = -4108;
";
        var result = ExcelTierAnalyzer.Analyze(code);
        Assert.Equal(ExcelTier.Write, result.HighestTier);
        Assert.Contains(result.WriteMembers, m => m.Contains("Font.Bold") || m.Contains("Bold"));
        Assert.Contains(result.WriteMembers, m => m.Contains("Interior.Color") || m.Contains("Color"));
        Assert.Contains(result.WriteMembers, m => m.Contains("NumberFormat"));
    }

    [Fact]
    public void AllDestructiveTableMethods_TriggerDestructiveClassification()
    {
        var clearComments = ExcelTierAnalyzer.Analyze("range.ClearComments();");
        Assert.Equal(ExcelTier.Destructive, clearComments.HighestTier);

        var clearFormats = ExcelTierAnalyzer.Analyze("range.ClearFormats();");
        Assert.Equal(ExcelTier.Destructive, clearFormats.HighestTier);

        var deleteSheet = ExcelTierAnalyzer.Analyze("sheet.Delete();");
        Assert.Equal(ExcelTier.Destructive, deleteSheet.HighestTier);

        var runMacro = ExcelTierAnalyzer.Analyze("excel.Run(\"MyMacro\");");
        Assert.Equal(ExcelTier.Destructive, runMacro.HighestTier);
    }

    [Fact]
    public void ScriptGuard_BlocksForbiddenPatterns_InExcelProfile()
    {
        // 1. System.Diagnostics.Process
        var processCode = "System.Diagnostics.Process.Start(\"calc.exe\"); return 1;";
        var v1 = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(processCode, HPRebar.McpBridge.Core.Scripting.GuardProfile.Excel);
        Assert.NotEmpty(v1);
        Assert.Contains(v1, v => v.Message.Contains("Process") || v.Message.Contains("denied"));

        // 2. Application.Quit
        var quitCode = "excel.Application.Quit(); return 1;";
        var v2 = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(quitCode, HPRebar.McpBridge.Core.Scripting.GuardProfile.Excel);
        Assert.NotEmpty(v2);

        // 3. #r external DLL directive
        var rDirectiveCode = "#r \"C:\\Malicious.dll\"\nreturn 1;";
        var v3 = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(rDirectiveCode, HPRebar.McpBridge.Core.Scripting.GuardProfile.Excel);
        Assert.NotEmpty(v3);
        Assert.Contains(v3, v => v.Message.Contains("#r"));

        // 4. #load external CSX directive
        var loadDirectiveCode = "#load \"C:\\Script.csx\"\nreturn 1;";
        var v4 = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(loadDirectiveCode, HPRebar.McpBridge.Core.Scripting.GuardProfile.Excel);
        Assert.NotEmpty(v4);
        Assert.Contains(v4, v => v.Message.Contains("#load"));
    }
}
