using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     Adversarial and empirical stress tests for McNeel Grasshopper contracts:
///     Pipe name resolution, security guard denials/allowances, and transaction AST detection.
/// </summary>
public sealed class GrasshopperContractAdversarialTests
{
    [Theory]
    [InlineData("grasshopper", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("GRASSHOPPER", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("GrassHopper", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("gRasShOpPeR", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("  grasshopper", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("grasshopper  ", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("  grasshopper  ", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("\tgrasshopper\t", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("\r\ngrasshopper\r\n", 2026, "hpgrasshopper-mcp-2026")]
    [InlineData("grasshopper", 2025, "hpgrasshopper-mcp-2025")]
    [InlineData("grasshopper", 8, "hpgrasshopper-mcp-8")]
    public void PipeNaming_resolves_grasshopper_with_casing_trimming_and_versions(string host, int version, string expected)
    {
        Assert.Equal(expected, PipeNaming.For(host, version));
        Assert.Equal(expected, new BridgeOptions { HostId = host, HostVersion = version }.PipeName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    [InlineData(null)]
    public void PipeNaming_rejects_empty_or_whitespace_host(string? host)
    {
        Assert.Throws<ArgumentException>("host", () => PipeNaming.For(host!, 2026));
    }

    [Theory]
    [InlineData("TaskDialog.Show(\"Revit\", \"msg\");", "TaskDialog")]
    [InlineData("Autodesk.Revit.UI.TaskDialog.Show(\"Revit\", \"msg\");", ".Show")]
    [InlineData("global::Autodesk.Revit.UI.TaskDialog.Show(\"Revit\", \"msg\");", ".Show")]
    [InlineData("Autodesk.Revit.UI.TaskDialog td = null;", "TaskDialog")]
    [InlineData("global::Autodesk.Revit.UI.TaskDialog td = null;", "TaskDialog")]
    [InlineData("TaskDialog td = null;", "TaskDialog")]
    [InlineData("MessageBox.Show(\"msg\");", "MessageBox")]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"msg\");", "MessageBox")]
    [InlineData("global::System.Windows.Forms.MessageBox.Show(\"msg\");", "MessageBox")]
    [InlineData("MessageBox mb = null;", "MessageBox")]
    [InlineData("app.Exit();", "Exit")]
    [InlineData("app?.Exit();", "Exit")]
    [InlineData("app.Quit();", "Quit")]
    [InlineData("app?.Quit();", "Quit")]
    [InlineData("app.CloseApplication();", "CloseApplication")]
    [InlineData("app?.CloseApplication();", "CloseApplication")]
    [InlineData("Application.Exit();", "Exit")]
    [InlineData("RhinoApp.Exit();", "Exit")]
    [InlineData("rhinoDoc.Dispose();", "Dispose")]
    [InlineData("rhinoDoc?.Dispose();", "Dispose")]
    [InlineData("using HPGrasshopper.McpBridge;", "HPGrasshopper.McpBridge")]
    [InlineData("using HPGrasshopper.McpBridge.Tools;", "HPGrasshopper.McpBridge")]
    [InlineData("HPGrasshopper.McpBridge.Entry.Stop();", "HPGrasshopper.McpBridge")]
    [InlineData("global::HPGrasshopper.McpBridge.Entry.Stop();", "HPGrasshopper.McpBridge")]
    [InlineData("using HPRebar.McpBridge.Core.Host;", "HPRebar.McpBridge.Core.Host")]
    [InlineData("HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current.Stop();", "HPRebar.McpBridge.Core.Host")]
    [InlineData("using System.Windows.Forms;", "System.Windows.Forms")]
    [InlineData("global::System.Windows.Forms.Form form = null;", "System.Windows.Forms")]
    [InlineData("global::System.IO.File.WriteAllText(\"a\", \"b\");", "System.IO")]
    [InlineData("global::System.Diagnostics.Process.Start(\"cmd\");", "System.Diagnostics.Process")]
    public void GuardProfile_denies_prohibited_apis_and_global_aliases(string code, string expectedFragment)
    {
        var violations = ScriptGuard.Check(code, GuardProfile.Grasshopper);
        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Message.Contains(expectedFragment, StringComparison.Ordinal));
    }

    [Fact]
    public void GuardProfile_permits_rhino_and_grasshopper_legitimate_code()
    {
        const string script = @"
            var pt = new Rhino.Geometry.Point3d(10, 20, 30);
            var crv = Rhino.Geometry.Curve.CreateControlPointCurve(new[] { pt });
            var docName = doc.DisplayName;
            var comps = doc.Objects;
            var canvasZoom = canvas.Viewport.Zoom;
            var rhinoName = rhinoDoc.Name;
            var revitTitle = revitDoc.Title;
            var param = new Grasshopper.Kernel.Parameters.Param_Number();
            var tree = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number>();
            log(""Processing: "" + doc.FilePath);
            progress(50, 100, ""Halfway"");
            var tol = args.Double(""Tolerance"", 0.001);
            return new { Point = pt, Count = comps.Count, Tolerance = tol };";

        var violations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("var t = new Transaction(revitDoc, \"Name\");", true)]
    [InlineData("var tg = new TransactionGroup(revitDoc, \"Group\");", true)]
    [InlineData("var st = new SubTransaction(revitDoc);", true)]
    [InlineData("var t = new Autodesk.Revit.DB.Transaction(revitDoc, \"Name\");", true)]
    [InlineData("var tg = new Autodesk.Revit.DB.TransactionGroup(revitDoc, \"Group\");", true)]
    [InlineData("var st = new Autodesk.Revit.DB.SubTransaction(revitDoc);", true)]
    [InlineData("Transaction t = new Transaction(revitDoc, \"Typed\");", true)]
    [InlineData("using Transaction t = new Transaction(revitDoc, \"Typed\");", true)]
    [InlineData("using (var tg = new TransactionGroup(revitDoc, \"G\")) { using (var t = new Transaction(revitDoc, \"T\")) { } }", true)]
    [InlineData("for (int i=0; i<10; i++) { if (i > 5) { var t = new Transaction(revitDoc, \"T\"); } }", true)]
    [InlineData("void Run() { var t = new Transaction(revitDoc, \"Local\"); } Run();", true)]
    [InlineData("Action a = () => { var t = new Transaction(revitDoc, \"Lambda\"); };", true)]
    [InlineData("Execute(new Transaction(revitDoc, \"Inline\"));", true)]
    [InlineData("return new Transaction(revitDoc, \"Return\");", true)]
    [InlineData("(new Transaction(revitDoc, \"Paren\")).Start();", true)]
    [InlineData("return doc.Objects.Count;", false)]
    [InlineData("var count = 42; return count;", false)]
    [InlineData("string txName = \"Transaction\"; return txName;", false)]
    [InlineData("string tgName = \"TransactionGroup\"; return tgName;", false)]
    [InlineData("string stName = \"SubTransaction\"; return stName;", false)]
    [InlineData("// var t = new Transaction(revitDoc, \"Commented\"); return 1;", false)]
    [InlineData("/* var tg = new TransactionGroup(revitDoc, \"Multi\"); */ return 2;", false)]
    [InlineData("class TransactionConfig { public string Name = \"T\"; } return new TransactionConfig();", false)]
    public void AnalyzerProfile_accurately_detects_transactions_in_nested_and_inline_syntax(string code, bool expectedUsesTransaction)
    {
        var result = ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper);
        Assert.Equal(expectedUsesTransaction, result.UsesTransaction);
    }

    [Fact]
    public void AnalyzerProfile_target_typed_new_is_ast_syntax_limitation()
    {
        // Target-typed new (`Transaction t = new(...)`) produces ImplicitObjectCreationExpressionSyntax,
        // which lacks explicit TypeSyntax on the creation node and is therefore not detected by the syntax walker.
        var result = ScriptAnalyzer.Analyze("Transaction t = new(revitDoc, \"Name\");", AnalyzerProfile.Grasshopper);
        Assert.False(result.UsesTransaction);
    }
}
