using System.Runtime.CompilerServices;
using HPNavis.McpBridge.Service;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The heavy gate reads only the script text. The BIM-coordination engine is called from seed scripts, so if it named
///     a heavy member (a clash run, save, append, export) that work would happen with the opt-in off. Every identifier of
///     the engine's source — calls, method groups, Try*/plural forms alike — is checked against the gate's own list.
/// </summary>
public sealed class CoordinationEngineHeavyBoundaryTests
{
    [Fact]
    public void Engine_source_never_names_a_heavy_member()
    {
        var hits = EngineSources()
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
                .Where(name => NavisHeavyGate.HeavyMembers.Contains(name.Identifier.ValueText))
                .Select(name => $"{Path.GetFileName(path)}:{name.GetLocation().GetLineSpan().StartLinePosition.Line + 1} {name.Identifier.ValueText}"))
            .ToList();

        Assert.Empty(hits);
    }

    [Fact]
    public void The_check_sees_the_engine_sources()
    {
        Assert.Contains(EngineSources(), path => path.EndsWith("CoordinatorTools.cs", StringComparison.Ordinal));
    }

    private static IEnumerable<string> EngineSources([CallerFilePath] string here = "")
    {
        var engine = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(here))!, "HPNavis.BIMCoordinator");
        var obj = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(engine, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains(obj)).ToList();
    }
}
