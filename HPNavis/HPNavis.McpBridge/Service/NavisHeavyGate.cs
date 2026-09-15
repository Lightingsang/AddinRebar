using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     The second consent, checked before the shared guard. Appending or merging files, saving, exporting
///     and running clash tests are not undoable, can take minutes and cannot be interrupted, so they are
///     refused with their own diagnostic until the user ticks "Allow heavy operations" in the bridge
///     window. Once allowed, string literals are still screened: no UNC paths (Roamer would authenticate
///     to a remote share) and nothing under the bridge's own folders (the audit trail must not be a write
///     target). Dynamic paths cannot be seen here — the tool description says so. Syntax only, like the
///     guard: a safety net against the obvious, not a sandbox.
/// </summary>
public sealed class NavisHeavyGate
{
    public const string DiagnosticId = "HEAVY";

    /// <summary>Cooperative timeout ceiling when heavy operations are off — the shared server default.</summary>
    public const int NormalMaxTimeoutSeconds = 120;

    /// <summary>Ceiling when heavy operations are on; the Navisworks server profile advertises the same constant.</summary>
    public const int HeavyMaxTimeoutSeconds = HPRebar.Mcp.Contracts.HostScriptContracts.NavisHeavyMaxTimeoutSeconds;

    /// <summary>Document members that load, merge, save, export, or run a clash test.</summary>
    public static readonly HashSet<string> HeavyMembers = new(StringComparer.Ordinal)
    {
        "AppendFile", "AppendFiles", "TryAppendFile", "TryAppendFiles", "MergeFile", "MergeFiles", "TryMergeFile", "TryMergeFiles",
        "RemoveFile", "TryRemoveFile", "OpenFile", "TryOpenFile", "OpenAggregate", "TryOpenAggregate", "UpdateFiles",
        "SaveFile", "TrySaveFile", "ExportToNwd", "TryExportToNwd", "PublishFile", "TryPublishFile", "ExportAsDwf", "GenerateImage",
        "TestsRunTest", "TestsRunAllTests", "TestsCompactAllTests", "TestsCompactTest",
    };

    /// <summary>Path fragments a heavy call may never name, even when heavy operations are allowed.</summary>
    private static readonly string[] RefusedPathFragments =
    [
        @"HPNavis\McpBridge", @"HPNavis\McpServer", @"HPNavis/McpBridge", @"HPNavis/McpServer", @"\Autodesk\Navisworks Manage",
    ];

    private volatile bool _enabled;

    /// <summary>Per-session, never persisted, off on every Navisworks start; only meaningful while code execution itself is enabled.</summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int MaxTimeoutSeconds => Enabled ? HeavyMaxTimeoutSeconds : NormalMaxTimeoutSeconds;

    /// <summary>Screens the source. <paramref name="hasHeavyCalls"/> is true when any heavy member is named, allowed or not.</summary>
    public IReadOnlyList<ScriptDiagnostic> Check(string code, out bool hasHeavyCalls)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var walker = new HeavyWalker(Enabled);
        walker.Visit(tree.GetRoot());
        hasHeavyCalls = walker.HasHeavyCalls;
        // A UNC or install-folder path can only do harm through a heavy call; a read-only script comparing
        // SourceFileName against a share path is legitimate.
        return hasHeavyCalls ? walker.Diagnostics.Concat(walker.PathDiagnostics).ToList() : walker.Diagnostics;
    }

    private sealed class HeavyWalker(bool enabled) : CSharpSyntaxWalker
    {
        public List<ScriptDiagnostic> Diagnostics { get; } = [];

        public List<ScriptDiagnostic> PathDiagnostics { get; } = [];

        public bool HasHeavyCalls { get; private set; }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var member = node.Name.Identifier.ValueText;
            // Document.Clear() closes the model (destructive, not undoable) but "Clear" is also how a selection is
            // emptied, so it counts only on the document global itself.
            var isDocumentClear = member == "Clear" && node.Expression is IdentifierNameSyntax { Identifier.ValueText: "doc" };
            if (HeavyMembers.Contains(member) || isDocumentClear)
            {
                HasHeavyCalls = true;
                if (!enabled)
                    Report(node.Name, $"{member} is a heavy operation (not undoable, may take minutes, cannot be interrupted). " +
                                      "Ask the user to tick 'Allow heavy operations' in the HPNavis bridge window, then retry.");
            }

            base.VisitMemberAccessExpression(node);
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression)) CheckPath(node, node.Token.ValueText);
            base.VisitLiteralExpression(node);
        }

        public override void VisitInterpolatedStringText(InterpolatedStringTextSyntax node)
        {
            CheckPath(node, node.TextToken.ValueText);
            base.VisitInterpolatedStringText(node);
        }

        private void CheckPath(SyntaxNode node, string text)
        {
            if (text.StartsWith(@"\\", StringComparison.Ordinal) || text.StartsWith("//", StringComparison.Ordinal))
                PathDiagnostics.Add(Diagnostic(node, "Network (UNC) paths are refused: Navisworks would authenticate to a remote share on the AI's behalf."));
            else if (RefusedPathFragments.Any(f => text.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                PathDiagnostics.Add(Diagnostic(node, "Paths under the bridge's own folders or the Navisworks install are refused."));
        }

        private void Report(SyntaxNode node, string message) => Diagnostics.Add(Diagnostic(node, message));

        private static ScriptDiagnostic Diagnostic(SyntaxNode node, string message)
        {
            var position = node.GetLocation().GetLineSpan().StartLinePosition;
            return new ScriptDiagnostic(position.Line + 1, position.Character + 1, DiagnosticId, message);
        }
    }
}
