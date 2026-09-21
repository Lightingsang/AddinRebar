using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPTekla.McpBridge;

public enum TeklaTier
{
    Read = 0,
    Write = 1,
    Destructive = 2
}

public sealed record TeklaTierAnalysisResult(
    TeklaTier HighestTier,
    IReadOnlyList<string> DestructiveMembers,
    IReadOnlyList<string> WriteMembers,
    IReadOnlyList<string> ReadMembers);

/// <summary>
///     Roslyn AST syntactic/semantic analyzer classifying Tekla Structures C# scripts
///     into 3 safety tiers:
///     - Tier R (Read): Queries model info, parts, rebars, properties, drawings (no mutation).
///     - Tier W (Write): Inserts or modifies parts, rebars, cuts, welds, UDAs. Requires snapshot backup.
///     - Tier D (Destructive/Heavy): Deletes objects, IFC export, bulk operations. Gated behind AllowHeavyOperations.
/// </summary>
public static class TeklaTierAnalyzer
{
    private static readonly HashSet<string> DestructiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Delete", "DeleteObjects", "DeleteDrawing", "DeleteDrawings",
        "CreateIFC4ExportFromAll", "CreateIFC", "CreateNC", "CreateNCFiles",
        "Numbering", "Archive", "Purge"
    };

    private static readonly HashSet<string> WriteKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Insert", "Modify", "SetUserProperty", "SetCustomProperty", "SetDynamicAttribute",
        "CommitChanges", "SetTestSavePoint", "RollbackToTestSavePoint",
        "SetCoordinateSystem", "SetCurrentWorkPlane", "SelectModelObjects",
        "AddCustomProperty", "RemoveCustomProperty"
    };

    private static readonly HashSet<string> MutatingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Beam", "Column", "ContourPlate", "PolyBeam", "PadFooting", "StripFooting",
        "RebarGroup", "SingleRebar", "CurvedRebarGroup", "CircularRebarGroup",
        "BoltArray", "BoltCircle", "Weld", "CutPlane", "Fitting", "Detail", "Component",
        "ControlLine", "ControlPoint", "ControlPlane", "Grid", "LoadGroup", "PointLoad"
    };

    public static TeklaTierAnalysisResult Analyze(string scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            return new TeklaTierAnalysisResult(
                TeklaTier.Read,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        var tree = CSharpSyntaxTree.ParseText(scriptCode);
        return Analyze(tree);
    }

    public static TeklaTierAnalysisResult Analyze(SyntaxTree tree)
    {
        var root = tree.GetRoot();
        var destructiveHits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var writeHits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var readHits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var highestTier = TeklaTier.Read;

        foreach (var node in root.DescendantNodes())
        {
            // 1. Check Object Creations (e.g. new Beam(), new RebarGroup())
            if (node is ObjectCreationExpressionSyntax creation)
            {
                var typeName = creation.Type.ToString();
                var simpleName = typeName.Split('.').Last().Trim();
                if (MutatingTypes.Contains(simpleName))
                {
                    writeHits.Add($"new {simpleName}()");
                    if (highestTier < TeklaTier.Write) highestTier = TeklaTier.Write;
                }
            }
            // 2. Check Assignments (e.g. beam.Profile.ProfileString = "HEA300")
            else if (node is AssignmentExpressionSyntax assign)
            {
                if (assign.Left is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax)
                {
                    var exprText = assign.Left.ToString();
                    writeHits.Add(exprText);
                    if (highestTier < TeklaTier.Write) highestTier = TeklaTier.Write;
                }
            }
            // 3. Check Invocations (e.g. beam.Insert(), model.CommitChanges(), part.Delete())
            else if (node is InvocationExpressionSyntax invocation)
            {
                var methodName = GetMethodName(invocation.Expression);
                if (!string.IsNullOrEmpty(methodName))
                {
                    if (DestructiveKeywords.Contains(methodName))
                    {
                        destructiveHits.Add(invocation.Expression.ToString());
                        highestTier = TeklaTier.Destructive;
                    }
                    else if (WriteKeywords.Contains(methodName))
                    {
                        writeHits.Add(invocation.Expression.ToString());
                        if (highestTier < TeklaTier.Write) highestTier = TeklaTier.Write;
                    }
                    else
                    {
                        readHits.Add(invocation.Expression.ToString());
                    }
                }
            }
        }

        return new TeklaTierAnalysisResult(
            highestTier,
            destructiveHits.ToArray(),
            writeHits.ToArray(),
            readHits.ToArray());
    }

    private static string GetMethodName(ExpressionSyntax expression)
    {
        return expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => string.Empty
        };
    }
}
