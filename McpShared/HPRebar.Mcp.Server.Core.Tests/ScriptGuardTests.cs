using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>The deny-list must catch the accidental escapes and leave ordinary Revit code alone.</summary>
public sealed class ScriptGuardTests
{
    [Theory]
    [InlineData("return new FilteredElementCollector(doc).OfClass(typeof(Wall)).GetElementCount();")]
    [InlineData("var ids = uidoc.Selection.GetElementIds(); log($\"{ids.Count} selected\"); return ids.Count;")]
    [InlineData("using var t = new Transaction(doc, \"x\"); t.Start(); doc.Delete(new ElementId(1L)); t.Commit(); return 1;")]
    [InlineData("var name = doc.GetType().Name; return name;")]
    [InlineData("var p = System.IO.Path.Combine(\"a\", \"b\"); return p;")]
    [InlineData("for (var i = 0; i < 10; i++) { ct.ThrowIfCancellationRequested(); progress(i, 10, \"step\"); } return 10;")]
    [InlineData("var t = new Transaction(doc, \"n\"); t.Start(); return t.GetStatus().ToString();")]
    public void Allows_ordinary_revit_scripts(string code)
    {
        Assert.Empty(ScriptGuard.Check(code));
    }

    [Theory]
    [InlineData("using System.IO; return File.ReadAllText(\"x\");", "using System.IO")]
    [InlineData("return System.IO.File.ReadAllText(\"x\");", "System.IO.File")]
    [InlineData("return File.ReadAllText(\"x\");", "File is not allowed")]
    [InlineData("System.Diagnostics.Process.Start(\"cmd\"); return 1;", "System.Diagnostics.Process")]
    [InlineData("Process.Start(\"cmd\"); return 1;", "Process is not allowed")]
    [InlineData("var m = doc.GetType().GetMethod(\"Delete\"); return m;", ".GetMethod")]
    [InlineData("var a = typeof(Wall).Assembly; return a;", ".Assembly")]
    [InlineData("var t = Type.GetType(\"System.IO.File\"); return t;", "Type.GetType")]
    [InlineData("await System.Threading.Tasks.Task.Delay(1); return 1;", "await")]
    [InlineData("Task.Run(() => 1); return 1;", "Task is not allowed")]
    [InlineData("Environment.Exit(0); return 1;", ".Exit")]
    [InlineData("dynamic d = doc; return d.Title;", "dynamic")]
    [InlineData("unsafe { int x = 1; int* p = &x; } return 1;", "unsafe")]
    [InlineData("Action a = async () => { }; return 1;", "async lambdas")]
    [InlineData("var s = \"System.Reflection.Assembly\"; return s;", "System.Reflection")]
    [InlineData("new Thread(() => { }).Start(); return 1;", "Thread is not allowed")]
    public void Rejects_escapes_with_line_and_reason(string code, string expectedFragment)
    {
        var diagnostics = ScriptGuard.Check(code);

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, d => Assert.Equal(ScriptGuard.DiagnosticId, d.Id));
        Assert.Contains(diagnostics, d => d.Message.Contains(expectedFragment, StringComparison.Ordinal));
        Assert.All(diagnostics, d => Assert.True(d.Line >= 1 && d.Column >= 1));
    }

    [Fact]
    public void Reports_the_line_of_the_offence()
    {
        var diagnostics = ScriptGuard.Check("var x = 1;\nvar y = 2;\nProcess.Start(\"cmd\");\nreturn x;");

        var hit = Assert.Single(diagnostics);
        Assert.Equal(3, hit.Line);
        Assert.Equal(1, hit.Column);
    }
}
