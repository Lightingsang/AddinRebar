namespace HPBim.Client.Tests.ScriptGenerators;

using System.Linq;
using HPBim.Client.ScriptGenerators;
using HPBim.Client.TypeResolver;
using HPBim.Client.Verification;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public sealed class ScriptSyntaxTests
{
    private static void AssertValidCSharpScript(string code, string testName)
    {
        Assert.False(string.IsNullOrWhiteSpace(code), $"{testName} emitted empty code.");

        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            kind: SourceCodeKind.Script);

        var tree = CSharpSyntaxTree.ParseText(code, parseOptions);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Any())
        {
            var firstErr = errors.First();
            Assert.Fail($"{testName} has C# syntax error at line {firstErr.Location.GetLineSpan().StartLinePosition.Line + 1}: {firstErr.GetMessage()}\nCode snippet:\n{code}");
        }
    }

    [Fact]
    public void Stage1LevelGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage1LevelGenerator.GetScriptCode(), "Stage 1 Levels");
    }

    [Fact]
    public void Stage2GridGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage2GridGenerator.GetScriptCode(), "Stage 2 Grids");
    }

    [Fact]
    public void Stage3StructureGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage3StructureGenerator.GetScriptCode(), "Stage 3 Structure");
    }

    [Fact]
    public void Stage4WallGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage4WallGenerator.GetScriptCode(), "Stage 4 Walls");
    }

    [Fact]
    public void Stage5OpeningGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage5OpeningGenerator.GetScriptCode(), "Stage 5 Openings");
    }

    [Fact]
    public void Stage6RoomGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage6RoomGenerator.GetScriptCode(), "Stage 6 Rooms");
    }

    [Fact]
    public void Stage7MepGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage7MepGenerator.GetScriptCode(), "Stage 7 MEP");
    }

    [Fact]
    public void Stage8RebarGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(Stage8RebarGenerator.GetScriptCode(), "Stage 8 Rebars");
    }

    [Fact]
    public void CompositeBimGenerator_ScriptSyntax_HasZeroErrors()
    {
        AssertValidCSharpScript(CompositeBimGenerator.GetScriptCode(), "Composite BIM Pipeline");
    }

    [Fact]
    public void DynamicTypeScriptHelper_AllSnippets_HaveZeroErrors()
    {
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetCommonHelpersScript(), "Common Helpers");
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetEnsureWallTypeScript(), "EnsureWallType");
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetEnsureColumnSymbolScript(), "EnsureColumnSymbol");
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetEnsureBeamSymbolScript(), "EnsureBeamSymbol");
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetEnsureFloorTypeScript(), "EnsureFloorType");
        AssertValidCSharpScript(DynamicTypeScriptHelper.GetEnsureLevelScript(), "EnsureLevel");
    }

    [Fact]
    public void RevitModelVerifier_QueryScript_HasZeroErrors()
    {
        AssertValidCSharpScript(RevitModelVerifier.GetVerificationQueryScript(), "Verification Query");
    }
}
