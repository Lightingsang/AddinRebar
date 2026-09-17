using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPEtabs.McpBridge.Service;

/// <summary>What a script may do to the model, decided before it runs: the highest tier of any member it names.</summary>
public enum EtabsTier
{
    /// <summary>Only reads: Get*/Is*/Has*/Count, analysis results, table display. Runs under `transaction: none`.</summary>
    ReadOnly = 0,

    /// <summary>Changes definitions, assignments or objects. Runs under `auto` after the bridge saved the model and took a snapshot.</summary>
    Write = 1,

    /// <summary>Unlocks, analyses, opens/saves/exports files or deletes results. Needs the second opt-in.</summary>
    Destructive = 2,
}

/// <summary>One member the gate classified, with where it sits, for the PREVIEW diagnostic.</summary>
public sealed record TierHit(int Line, int Column, string Member, EtabsTier Tier);

/// <summary>
///     Static classification of the OAPI members a script names. ETABS has no transaction, so the only way to
///     keep a "read-only" run read-only is to refuse it before it runs — and everything the walker cannot see
///     through counts as destructive rather than harmless. This is a syntax walk, so it is fail-closed: a member
///     is classified when it is reached from the <c>sapModel</c>/<c>etabs</c> globals through a plain member
///     chain; the globals used any other way (aliased into a local, cast, passed as an argument, captured through a
///     lambda parameter, null-conditional) make the script destructive, because the walker can no longer follow
///     what happens to the model. Members on other receivers are BCL or the script's own objects and
///     are only caught by the destructive name list. The semantic pass that binds every receiver to its ETABSv1 type
///     replaces this walker together with the generated tier table.
/// </summary>
public static class EtabsTierGate
{
    private static readonly string[] Roots = ["sapModel", "etabs"];

    /// <summary>Sub-objects whose members read results: everything under `sapModel.Results` (incl. `Results.Setup`) unless on the destructive list.</summary>
    private static readonly string[] ReadOnlyReceivers = ["Results"];

    /// <summary>Members that read without changing anything, even though their prefix would say otherwise.</summary>
    private static readonly HashSet<string> ReadOnlyExact = new(StringComparer.Ordinal)
    {
        "RefreshView", "GetTableForDisplayArray", "GetAvailableTables", "GetAllFieldsInTable", "GetPresentUnits", "GetDatabaseUnits",
        "GetModelIsLocked", "GetModelFilename", "GetModelFilepath", "GetVersion", "GetOAPIVersionNumber", "Visible", "Count",
    };

    /// <summary>Members that unlock, analyse, touch files, launch programs or delete — by name, on any receiver.</summary>
    private static readonly HashSet<string> DestructiveExact = new(StringComparer.Ordinal)
    {
        "SetModelIsLocked", "RunAnalysis", "DeleteResults", "CreateAnalysisModel", "InitializeNewModel", "ApplyEditedTables",
        "OpenFile", "Save", "NewBlank", "NewGridOnly", "NewSteelDeck", "NewConcreteDeck", "NewWallModel",
        "ExportFile", "ImportFile", "ImportProp", "MergeAnalysisResults", "ModifyUndeformedGeometry", "ModifyUndeformedGeometryModeShape",
        "GetTableForDisplayCSVFile", "GetTableForDisplayCSVString", "SetTableForEditingCSVFile", "SetTableForEditingCSVString", "ShowTablesInExcel",
        "StartDesign", "StartSlabDesign", "StartDetailing", "ClearDetailing", "ResetOverwrites", "RenameTower", "Delete", "DeleteSpecialPoint",
    };

    private static readonly HashSet<string> ObjectBasics = new(StringComparer.Ordinal) { "ToString", "Equals", "GetHashCode", "GetType" };

    private static readonly string[] DestructivePrefixes = ["Delete", "Start", "Modify", "Merge", "Reset", "Clear", "Rename", "Show", "Export", "Import", "Replicate", "New", "Open"];

    private static readonly string[] ReadOnlyPrefixes = ["Get", "Is", "Has", "Count"];

    /// <summary>A path-taking parameter name means a file: the member is destructive whatever its name says.</summary>
    public static readonly string[] PathParameterNames = ["FileName", "csvFilePath", "SourceFileName", "FilePath", "Path"];

    /// <summary>Classification of an OAPI member reached from a global; <paramref name="viaReadOnlyReceiver"/> when it hangs off `Results`.</summary>
    public static EtabsTier Classify(string member, bool viaReadOnlyReceiver = false)
    {
        if (DestructiveExact.Contains(member)) return EtabsTier.Destructive;
        if (viaReadOnlyReceiver || ReadOnlyExact.Contains(member)) return EtabsTier.ReadOnly;
        if (DestructivePrefixes.Any(p => member.StartsWith(p, StringComparison.Ordinal))) return EtabsTier.Destructive;
        if (ReadOnlyPrefixes.Any(p => member.StartsWith(p, StringComparison.Ordinal))) return EtabsTier.ReadOnly;
        return EtabsTier.Write;
    }

    /// <summary>Every classified member of the script and the highest tier among them.</summary>
    public static (EtabsTier tier, IReadOnlyList<TierHit> hits) Inspect(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var root = tree.GetRoot();
        var hits = new List<TierHit>();

        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            // Only the member being invoked or read matters; `sapModel.FrameObj` on the way to `.SetSection` is a collection, not a call.
            if (access.Parent is MemberAccessExpressionSyntax parent && parent.Expression == access) continue;
            // `sapModel.GetPresentUnits().ToString()`: a member of a call's result is a value's member, not an OAPI member
            // (OAPI sub-objects are properties, never method results), and the object basics are never a write.
            if (access.Expression is InvocationExpressionSyntax) continue;
            var member = access.Name.Identifier.ValueText;
            if (ObjectBasics.Contains(member)) continue;

            var chain = Chain(access.Expression);
            Record(hits, access.Name, chain, member, invoked: access.Parent is InvocationExpressionSyntax);
        }

        // The globals anywhere but at the root of a plain member chain — aliased, cast, passed on, compared, null-conditional —
        // are where the walker loses the model; fail closed and tell the author the one form that is classified.
        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!Roots.Contains(identifier.Identifier.ValueText, StringComparer.Ordinal)) continue;
            if (identifier.Parent is MemberAccessExpressionSyntax access && access.Expression == identifier) continue;
            var position = identifier.GetLocation().GetLineSpan().StartLinePosition;
            hits.Add(new TierHit(position.Line + 1, position.Character + 1,
                $"{identifier.Identifier.ValueText} used outside a plain member access (alias, cast, argument, lambda or ?.) — write it as {identifier.Identifier.ValueText}.Member(...)",
                EtabsTier.Destructive));
        }

        var highest = hits.Count == 0 ? EtabsTier.ReadOnly : hits.Max(h => h.Tier);
        return (highest, hits);
    }

    /// <summary>The PREVIEW diagnostics for a script that would write: one per member at or above <paramref name="atLeast"/>.</summary>
    public static IReadOnlyList<ScriptDiagnostic> Preview(IReadOnlyList<TierHit> hits, EtabsTier atLeast) =>
        hits.Where(h => h.Tier >= atLeast)
            .Select(h => new ScriptDiagnostic(h.Line, h.Column, "PREVIEW", $"{h.Member} ({(h.Tier == EtabsTier.Destructive ? "D" : "W")})"))
            .ToArray();

    private static void Record(List<TierHit> hits, SimpleNameSyntax name, List<string> chain, string member, bool invoked)
    {
        var rooted = chain.Count > 0 && Roots.Contains(chain[0], StringComparer.Ordinal);
        var position = name.GetLocation().GetLineSpan().StartLinePosition;
        var described = rooted ? string.Join('.', chain.Skip(1).Append(member)) : string.Join('.', chain.Append(member));

        if (rooted && !invoked)
        {
            // `var fo = sapModel.FrameObj;` — an OAPI sub-object taken as a value is an alias the walker cannot follow.
            hits.Add(new TierHit(position.Line + 1, position.Character + 1, described + " taken as a value (alias) — call its members directly from " + chain[0], EtabsTier.Destructive));
            return;
        }

        EtabsTier tier;
        if (rooted) tier = Classify(member, chain.Count > 1 && ReadOnlyReceivers.Contains(chain[1], StringComparer.Ordinal));
        else if (DestructiveExact.Contains(member)) tier = EtabsTier.Destructive; // BCL or script objects: only the unmistakable names count
        else return;

        hits.Add(new TierHit(position.Line + 1, position.Character + 1, described, tier));
    }

    /// <summary>`sapModel.FrameObj.SetSection` → ["sapModel", "FrameObj"]; a cast, call or anything else breaks the chain (empty).</summary>
    private static List<string> Chain(ExpressionSyntax expression)
    {
        var parts = new List<string>();
        while (true)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax inner:
                    parts.Insert(0, inner.Name.Identifier.ValueText);
                    expression = inner.Expression;
                    continue;
                case IdentifierNameSyntax identifier:
                    parts.Insert(0, identifier.Identifier.ValueText);
                    return parts;
                default:
                    return [];
            }
        }
    }

}
