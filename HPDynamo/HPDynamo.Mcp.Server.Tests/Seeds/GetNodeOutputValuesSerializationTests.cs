namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.IO;
using System.Linq;
using System.Text;
using HPDynamo.Mcp.Server.Hosts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

/// <summary>
///     Validates RV-07 & RV-08 requirements for get_node_output_values seed tool:
///     1. ProtoCore MirrorData collection extraction (IsCollection first, traverses GetElements).
///     2. DesignScript.Builtin.Dictionary serialization (strongly typed .Keys and .ValueAtKey).
///     3. Payload budgets (MaxDepth 6, MaxBreadth 100, MaxTotalItemsPerPort 500).
///     4. Cooperative cancellation (ct.ThrowIfCancellationRequested).
///     5. Clean Code governance (file <= 300, methods <= 50, zero reflection).
/// </summary>
public sealed class GetNodeOutputValuesSerializationTests
{
    private static string LoadGetNodeOutputValuesCode()
    {
        var assembly = typeof(DynamoHostProfile).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("get_node_output_values.code.cs", StringComparison.OrdinalIgnoreCase) ||
                                 (n.Contains("get_node_output_values") && n.EndsWith("code.cs", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(resourceName);
        using var stream = assembly.GetManifestResourceStream(resourceName!)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Code_ChecksIsCollectionFirst_AndIteratesGetElements()
    {
        var code = LoadGetNodeOutputValuesCode();
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        Assert.Contains("mirrorData.IsCollection", code);
        Assert.Contains("mirror.GetElements()", code);
        Assert.Contains("SerializeMirrorData", code);

        // Verify that IsCollection check precedes Data == null check
        int isCollectionPos = code.IndexOf("mirrorData.IsCollection", StringComparison.Ordinal);
        int dataNullPos = code.IndexOf("mirrorData.Data == null", StringComparison.Ordinal);
        Assert.True(isCollectionPos >= 0 && isCollectionPos < dataNullPos, "IsCollection must be checked before Data == null");
    }

    [Fact]
    public void Code_ExtractsDesignScriptDictionary_ViaKeysAndValueAtKey()
    {
        var code = LoadGetNodeOutputValuesCode();
        Assert.Contains("DesignScript.Builtin.Dictionary", code);
        Assert.Contains("dsDict.Keys", code);
        Assert.Contains("dsDict.ValueAtKey", code);
        Assert.Contains("SerializeDictionary", code);
    }

    [Fact]
    public void Code_EnforcesBudgets_MaxDepth_MaxBreadth_MaxTotalItems()
    {
        var code = LoadGetNodeOutputValuesCode();
        Assert.Contains("MaxDepth = 6", code);
        Assert.Contains("MaxBreadth = 100", code);
        Assert.Contains("MaxTotalItemsPerPort = 500", code);
        Assert.Contains("portItemCount", code);
    }

    [Fact]
    public void Code_ContainsCooperativeCancellation()
    {
        var code = LoadGetNodeOutputValuesCode();
        Assert.Contains("ct.ThrowIfCancellationRequested()", code);
    }

    [Fact]
    public void Code_CleanCodeGovernance_CompliesWithAllRules()
    {
        var code = LoadGetNodeOutputValuesCode();
        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Assert.True(lines.Length <= 300, $"File has {lines.Length} lines, expected <= 300");

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        // No forbidden reflection
        Assert.DoesNotContain("GetProperty(", code);
        Assert.DoesNotContain("GetMethod(", code);
        Assert.DoesNotContain("dynamic ", code);

        // All local functions <= 50 lines
        var localFunctions = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToArray();
        foreach (var func in localFunctions)
        {
            var span = func.GetLocation().GetLineSpan();
            int funcLines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            Assert.True(funcLines <= 50, $"Function '{func.Identifier.Text}' has {funcLines} lines, expected <= 50");
        }
    }
}
