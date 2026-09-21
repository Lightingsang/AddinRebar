using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPExcel.McpBridge.Safety;

public sealed record ExcelTierAnalysisResult(
    ExcelTier HighestTier,
    IReadOnlyList<string> DestructiveMembers,
    IReadOnlyList<string> WriteMembers,
    IReadOnlyList<string> ReadMembers);

/// <summary>
///     Analyzes C# script syntax to classify operations into Tier R (Read), Tier W (Write), or Tier D (Destructive).
/// </summary>
public static class ExcelTierAnalyzer
{
    public static ExcelTierAnalysisResult Analyze(string scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            return new ExcelTierAnalysisResult(
                ExcelTier.ReadOnly,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        var tree = CSharpSyntaxTree.ParseText(scriptCode);
        var root = tree.GetRoot();

        var destructive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var write = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var highestTier = ExcelTier.ReadOnly;

        foreach (var node in root.DescendantNodes())
        {
            // 1. Check assignments: property/element assignment (e.g. range.Value = ..., cells[1, 1] = ...) is at least Write.
            // Local variable reassignments (e.g. total = total + 10;) are NOT property mutations.
            if (node is AssignmentExpressionSyntax assign)
            {
                var left = assign.Left.ToString();
                var tier = ExcelTierTable.Classify(left);

                if (assign.Left is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax)
                {
                    if (tier == ExcelTier.ReadOnly) tier = ExcelTier.Write; // Member/element assignments mutate objects
                }

                if (tier == ExcelTier.Destructive)
                {
                    destructive.Add(left);
                    highestTier = ExcelTier.Destructive;
                }
                else if (tier == ExcelTier.Write)
                {
                    write.Add(left);
                    if (highestTier < ExcelTier.Write) highestTier = ExcelTier.Write;
                }
                else
                {
                    read.Add(left);
                }
            }
            // 2. Check invocations
            else if (node is InvocationExpressionSyntax invocation)
            {
                var exprStr = invocation.Expression.ToString();
                var tier = ExcelTierTable.Classify(exprStr);

                if (tier == ExcelTier.Destructive)
                {
                    destructive.Add(exprStr);
                    highestTier = ExcelTier.Destructive;
                }
                else if (tier == ExcelTier.Write)
                {
                    write.Add(exprStr);
                    if (highestTier < ExcelTier.Write) highestTier = ExcelTier.Write;
                }
                else
                {
                    read.Add(exprStr);
                }
            }
            // 3. Check member accesses
            else if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var name = memberAccess.Name.Identifier.Text;
                var tier = ExcelTierTable.Classify(name);

                if (tier == ExcelTier.Destructive)
                {
                    destructive.Add(memberAccess.ToString());
                    highestTier = ExcelTier.Destructive;
                }
                else if (tier == ExcelTier.Write)
                {
                    write.Add(memberAccess.ToString());
                    if (highestTier < ExcelTier.Write) highestTier = ExcelTier.Write;
                }
                else
                {
                    read.Add(memberAccess.ToString());
                }
            }
        }

        return new ExcelTierAnalysisResult(
            highestTier,
            destructive.ToList(),
            write.ToList(),
            read.ToList());
    }
}
