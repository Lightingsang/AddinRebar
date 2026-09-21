using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRobot.McpBridge.Safety;

public sealed record RobotTierAnalysisResult(
    RobotTier HighestTier,
    IReadOnlyList<string> DeleteHeavyMembers,
    IReadOnlyList<string> WriteMembers,
    IReadOnlyList<string> ReadMembers);

/// <summary>
///     Roslyn AST semantic/syntactic analyzer that classifies Robot C# scripts
///     into Tier R (Read), Tier W (Write), or Tier D (Delete/Heavy).
/// </summary>
public static class RobotTierAnalyzer
{
    public const string RobotWrapperNamespace = "RobotOM";

    public static RobotTierAnalysisResult Analyze(string scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            return new RobotTierAnalysisResult(
                RobotTier.Read,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        var tree = CSharpSyntaxTree.ParseText(scriptCode);
        return Analyze(tree);
    }

    public static RobotTierAnalysisResult Analyze(SyntaxTree tree, SemanticModel? model = null)
    {
        var root = tree.GetRoot();
        var deleteHeavy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var write = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var highestTier = RobotTier.Read;

        foreach (var node in root.DescendantNodes())
        {
            // 1. Check Assignments: Property or element mutation on Robot objects
            if (node is AssignmentExpressionSyntax assign)
            {
                var left = assign.Left.ToString();
                var tier = RobotTierTable.Classify(left);

                if (assign.Left is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax)
                {
                    // Property/field assignments to object members are at least Write mutations
                    if (tier == RobotTier.Read)
                        tier = RobotTier.Write;
                }

                RecordHit(left, tier, deleteHeavy, write, read, ref highestTier);
            }
            // 2. Check Invocations
            else if (node is InvocationExpressionSyntax invocation)
            {
                var exprStr = invocation.Expression.ToString();
                var tier = RobotTierTable.Classify(exprStr);

                // If semantic model is available, verify symbol
                if (model != null)
                {
                    var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        var containingNs = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                        if (containingNs.StartsWith(RobotWrapperNamespace, StringComparison.OrdinalIgnoreCase))
                        {
                            var memberKey = symbol.ContainingType.Name + "." + symbol.Name;
                            var semanticTier = RobotTierTable.Classify(memberKey);
                            if (semanticTier > tier) tier = semanticTier;
                        }
                    }
                }

                RecordHit(exprStr, tier, deleteHeavy, write, read, ref highestTier);
            }
            // 3. Check Member Accesses
            else if (node is MemberAccessExpressionSyntax memberAccess)
            {
                // Skip if parent is already an invocation expression (handled above)
                if (memberAccess.Parent is InvocationExpressionSyntax)
                    continue;

                var name = memberAccess.Name.Identifier.Text;
                var tier = RobotTierTable.Classify(name);

                RecordHit(memberAccess.ToString(), tier, deleteHeavy, write, read, ref highestTier);
            }
        }

        return new RobotTierAnalysisResult(
            highestTier,
            deleteHeavy.ToList(),
            write.ToList(),
            read.ToList());
    }

    private static void RecordHit(
        string member,
        RobotTier tier,
        HashSet<string> deleteHeavy,
        HashSet<string> write,
        HashSet<string> read,
        ref RobotTier highestTier)
    {
        switch (tier)
        {
            case RobotTier.DeleteHeavy:
                deleteHeavy.Add(member);
                highestTier = RobotTier.DeleteHeavy;
                break;
            case RobotTier.Write:
                write.Add(member);
                if (highestTier < RobotTier.Write)
                    highestTier = RobotTier.Write;
                break;
            default:
                read.Add(member);
                break;
        }
    }
}
