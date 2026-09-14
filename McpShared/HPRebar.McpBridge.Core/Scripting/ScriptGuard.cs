using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HPRebar.McpBridge.Core.Scripting;

/// <summary>
///     Static deny-list over the script's syntax tree. This is NOT a sandbox: reflection can reach
///     anything the process can, and a determined script can get around a name check. It exists to stop
///     the accidental cases — an AI reaching for File.WriteAllText, Process.Start or `await` — before
///     they run on the host's API thread, and to give the model a line-numbered reason it can act on.
///     The base list is host-neutral; a <see cref="GuardProfile"/> adds what is dangerous in one host.
/// </summary>
public static class ScriptGuard
{
    public const string DiagnosticId = "GUARD";

    /// <summary>Namespaces a script may not import or name. `System.IO.Path` is carved out below.</summary>
    private static readonly string[] DeniedNamespaces =
    [
        "System.IO", "System.Net", "System.Diagnostics.Process", "System.Reflection",
        "System.Runtime.InteropServices", "System.Runtime.Loader", "System.Threading.Tasks", "System.Security",
        "Microsoft.CodeAnalysis", "Microsoft.Win32",
    ];

    private static readonly string[] AllowedQualifiedPrefixes = ["System.IO.Path"];

    /// <summary>Type names reachable through the default `System` import that open the door to the machine.</summary>
    private static readonly HashSet<string> DeniedIdentifiers = new(StringComparer.Ordinal)
    {
        "Process", "ProcessStartInfo", "Thread", "ThreadPool", "Task", "Parallel", "Timer",
        "AppDomain", "Assembly", "AssemblyLoadContext", "Activator", "Marshal", "GCHandle",
        "MethodInfo", "MethodBase", "PropertyInfo", "FieldInfo", "ConstructorInfo", "BindingFlags",
        "DllImportAttribute", "DllImport", "Registry", "RegistryKey", "WebClient", "HttpClient", "Socket",
        "File", "Directory", "FileStream", "StreamWriter", "StreamReader", "FileInfo", "DirectoryInfo",
    };

    /// <summary>Member names that only make sense for reflection or process control.</summary>
    private static readonly HashSet<string> DeniedMembers = new(StringComparer.Ordinal)
    {
        "GetMethod", "GetMethods", "GetProperty", "GetProperties", "GetField", "GetFields", "GetMembers", "GetMember",
        "GetConstructor", "GetConstructors", "GetNestedType", "GetNestedTypes", "GetTypes", "GetExportedTypes",
        "InvokeMember", "DynamicInvoke", "CreateInstance", "LoadFrom", "LoadFile", "Exit", "FailFast",
        "Assembly", "Module", "DeclaringMethod",
    };

    private static readonly string[] DeniedLiteralFragments = ["System.Reflection", "System.IO", "System.Net", "System.Diagnostics.Process"];

    /// <summary>Checks with the Revit profile — the behaviour the Revit bridge shipped with.</summary>
    public static IReadOnlyList<ScriptDiagnostic> Check(string code) => Check(code, GuardProfile.Revit);

    public static IReadOnlyList<ScriptDiagnostic> Check(string code, GuardProfile profile)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var walker = new DenyListWalker(profile);
        walker.Visit(tree.GetRoot());
        return walker.Diagnostics;
    }

    private static bool IsDeniedNamespace(string dotted, GuardProfile profile)
    {
        if (AllowedQualifiedPrefixes.Any(p => dotted == p || dotted.StartsWith(p + ".", StringComparison.Ordinal))) return false;

        return DeniedNamespaces.Concat(profile.DeniedNamespaces)
            .Any(ns => dotted == ns || dotted.StartsWith(ns + ".", StringComparison.Ordinal));
    }

    private sealed class DenyListWalker(GuardProfile profile) : CSharpSyntaxWalker
    {
        private readonly string _host = profile.HostName;

        public List<ScriptDiagnostic> Diagnostics { get; } = [];

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var name = node.Name?.ToString() ?? string.Empty;
            if (IsDeniedNamespace(name, profile)) Report(node, $"using {name} is not allowed in {_host} scripts.");
            base.VisitUsingDirective(node);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            // Only judge the outermost qualified name; its parts are visited again below and would double-report.
            if (node.Parent is not QualifiedNameSyntax)
            {
                var dotted = node.ToString().Replace(" ", string.Empty);
                if (IsDeniedNamespace(dotted, profile)) Report(node, $"{dotted} is not allowed in {_host} scripts.");
            }

            base.VisitQualifiedName(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Parent is not MemberAccessExpressionSyntax)
            {
                var dotted = node.ToString().Replace(" ", string.Empty);
                if (IsDeniedNamespace(dotted, profile)) Report(node, $"{dotted} is not allowed in {_host} scripts.");
            }

            var member = node.Name.Identifier.ValueText;
            if (DeniedMembers.Contains(member)) Report(node.Name, $".{member} is not allowed: reflection and process control are blocked in {_host} scripts.");
            else if (profile.DeniedMembers.Contains(member)) Report(node.Name, $".{member} is not allowed in {_host} scripts: it prompts the user, leaves or replaces the bridge's transaction, or opens modal UI.");

            // Members denied only on a named global, e.g. tr.Commit() — the bridge owns that transaction.
            if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var receiver }
                && profile.DeniedMembersOnIdentifier.TryGetValue(receiver, out var denied)
                && Array.IndexOf(denied, member) >= 0)
                Report(node.Name, $"{receiver}.{member} is not allowed: the bridge owns `{receiver}` and commits or rolls it back for you.");

            // Type.GetType("...") is reflection by string; instance .GetType().Name stays allowed.
            if (member == "GetType" && node.Expression is IdentifierNameSyntax { Identifier.ValueText: "Type" })
                Report(node, $"Type.GetType(string) is not allowed in {_host} scripts.");

            base.VisitMemberAccessExpression(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var text = node.Identifier.ValueText;

            // A member access like `doc.Assembly` is judged by VisitMemberAccessExpression; here only bare type names.
            var isMemberName = node.Parent is MemberAccessExpressionSyntax access && access.Name == node;
            if (!isMemberName && (DeniedIdentifiers.Contains(text) || profile.DeniedIdentifiers.Contains(text)))
                Report(node, $"{text} is not allowed in {_host} scripts.");

            if (text == "dynamic" && node.Parent is not MemberAccessExpressionSyntax) Report(node, $"dynamic is not allowed in {_host} scripts.");

            base.VisitIdentifierName(node);
        }

        public override void VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            Report(node, $"await is not allowed: scripts run synchronously on {_host}'s API thread.");
            base.VisitAwaitExpression(node);
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => CheckAsync(node.AsyncKeyword, node, () => base.VisitAnonymousMethodExpression(node));

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => CheckAsync(node.AsyncKeyword, node, () => base.VisitParenthesizedLambdaExpression(node));

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => CheckAsync(node.AsyncKeyword, node, () => base.VisitSimpleLambdaExpression(node));

        public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            Report(node, $"unsafe code is not allowed in {_host} scripts.");
            base.VisitUnsafeStatement(node);
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var text = node.Token.ValueText;
                var hit = DeniedLiteralFragments.FirstOrDefault(f => text.Contains(f, StringComparison.Ordinal));
                if (hit is not null) Report(node, $"String mentioning {hit} is not allowed (reflection by name).");
            }

            base.VisitLiteralExpression(node);
        }

        private void CheckAsync(SyntaxToken asyncKeyword, SyntaxNode node, Action visitBase)
        {
            if (asyncKeyword.IsKind(SyntaxKind.AsyncKeyword)) Report(node, $"async lambdas are not allowed: scripts run synchronously on {_host}'s API thread.");
            visitBase();
        }

        private void Report(SyntaxNode node, string message)
        {
            var position = node.GetLocation().GetLineSpan().StartLinePosition;
            Diagnostics.Add(new ScriptDiagnostic(position.Line + 1, position.Character + 1, DiagnosticId, message));
        }
    }
}
