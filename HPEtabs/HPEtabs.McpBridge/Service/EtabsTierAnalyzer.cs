using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace HPEtabs.McpBridge.Service;

/// <summary>One OAPI member the analyzer bound, with where it sits, for the PREVIEW diagnostic.</summary>
public sealed record TierHit(int Line, int Column, string Member, EtabsTier Tier);

/// <summary>
///     What the analyzer decided about a compiled script: its tier (the highest among the members it calls), every
///     bound member, the refusals that stop it before any tier logic (a path argument the policy cannot read
///     statically), the path literals it names and the `args` keys it reads for paths — the runner screens both
///     against the model folder just before the script runs.
/// </summary>
public sealed record TierVerdict(
    EtabsTier Tier,
    IReadOnlyList<TierHit> Hits,
    IReadOnlyList<ScriptDiagnostic> Refusals,
    IReadOnlyList<string> PathLiterals,
    IReadOnlyList<string> PathArgKeys)
{
    public bool TakesPaths => PathLiterals.Count > 0 || PathArgKeys.Count > 0;
}

/// <summary>
///     Semantic classification of the OAPI members a compiled script calls. ETABS has no transaction, so the only
///     way to keep a "read-only" run read-only is to refuse it before it runs. The script's own compilation binds
///     every member access to its symbol: whatever the receiver is called in the script — the global, an alias,
///     a cast, a lambda parameter — a call on an <c>ETABSv1</c> interface resolves to <c>cInterface.Member</c> and
///     is looked up in the tier table. A member of an ETABSv1 type that the table does not know, or a member the
///     compiler could not bind on an ETABSv1 or unknown receiver, is destructive: the analyzer fails closed.
///     Members of any other type (BCL, the script's own objects) are not the model's business.
/// </summary>
public sealed class EtabsTierAnalyzer
{
    public const string WrapperNamespace = "ETABSv1";
    public const string PathDiagnosticId = "PATH";

    private readonly EtabsTierTable _table;

    public EtabsTierAnalyzer(EtabsTierTable table) => _table = table;

    public TierVerdict Inspect(Script<object> script)
    {
        var compilation = script.GetCompilation();
        var hits = new List<TierHit>();
        var refusals = new List<ScriptDiagnostic>();
        var literals = new List<string>();
        var argKeys = new List<string>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                switch (node)
                {
                    case MemberAccessExpressionSyntax access:
                        Classify(model, access, access.Name, access.Expression, hits, refusals, literals, argKeys);
                        break;
                    case MemberBindingExpressionSyntax binding: // `x?.Member`
                        Classify(model, binding, binding.Name, null, hits, refusals, literals, argKeys);
                        break;
                }
            }
        }

        var tier = hits.Count == 0 ? EtabsTier.ReadOnly : hits.Max(h => h.Tier);
        return new TierVerdict(tier, hits, refusals, literals, argKeys);
    }

    /// <summary>The PREVIEW diagnostics for a script that would write: one per member at or above <paramref name="atLeast"/>.</summary>
    public static IReadOnlyList<ScriptDiagnostic> Preview(IReadOnlyList<TierHit> hits, EtabsTier atLeast) =>
        hits.Where(h => h.Tier >= atLeast)
            .Select(h => new ScriptDiagnostic(h.Line, h.Column, "PREVIEW", $"{h.Member} ({(h.Tier == EtabsTier.Destructive ? "D" : "W")})"))
            .ToArray();

    private void Classify(SemanticModel model, ExpressionSyntax node, SimpleNameSyntax name, ExpressionSyntax? receiver,
        List<TierHit> hits, List<ScriptDiagnostic> refusals, List<string> literals, List<string> argKeys)
    {
        // `nameof(sapModel.FrameObj.Delete)` is a string, not a call.
        if (node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })) return;

        var info = model.GetSymbolInfo(node);
        var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        var position = name.GetLocation().GetLineSpan().StartLinePosition;
        var line = position.Line + 1;
        var column = position.Character + 1;

        if (symbol is null)
        {
            // Nothing bound: only a receiver the compiler could not type, or an ETABSv1 one, concerns the model.
            var receiverType = receiver is null ? null : model.GetTypeInfo(receiver).Type;
            if (receiverType is null || receiverType is IErrorTypeSymbol || IsWrapperType(receiverType))
                hits.Add(new TierHit(line, column, name.Identifier.ValueText + " (unbound member on an ETABS object)", EtabsTier.Destructive));
            return;
        }

        if (!IsWrapperType(symbol.ContainingType)) return;

        // `eUnits.kN_mm_C`: an enum member is a value, not a call on the model.
        if (symbol is IFieldSymbol) return;

        // `sapModel.FrameObj`: navigation to a sub-object, not a call — its members are classified when they are called.
        if (symbol is IPropertySymbol property && IsWrapperType(property.Type)) return;

        var member = symbol.ContainingType.Name + "." + symbol.Name;
        if (!_table.TryGet(symbol.ContainingType.Name, symbol.Name, out var entry))
        {
            hits.Add(new TierHit(line, column, member + " (not in the tier table)", EtabsTier.Destructive));
            return;
        }

        hits.Add(new TierHit(line, column, member, entry.Tier));

        if (entry.PathParameters.Count > 0 && symbol is IMethodSymbol method)
            ScreenPathArguments(model, node, method, entry, member, line, column, refusals, literals, argKeys);
    }

    /// <summary>
    ///     A path argument must be something the policy can read before the run: a string literal (screened here
    ///     and again against the model folder at run time) or `args.Str("key")` / `args.Require("key")` with a
    ///     literal key (the value is screened at run time). Anything else — a variable, concatenation, interpolation —
    ///     is refused: the bridge will not pass a path it cannot see to a file-writing member.
    /// </summary>
    private static void ScreenPathArguments(SemanticModel model, ExpressionSyntax node, IMethodSymbol method, TierEntry entry, string member,
        int line, int column, List<ScriptDiagnostic> refusals, List<string> literals, List<string> argKeys)
    {
        var invocation = node.Parent as InvocationExpressionSyntax ?? (node.Parent as ConditionalAccessExpressionSyntax)?.WhenNotNull as InvocationExpressionSyntax;
        if (invocation is null)
        {
            refusals.Add(new ScriptDiagnostic(line, column, PathDiagnosticId, $"{member} takes a file path and may only be called directly, not taken as a method group."));
            return;
        }

        foreach (var index in entry.PathParameters)
        {
            var argument = FindArgument(invocation.ArgumentList.Arguments, index, method);
            if (argument is null)
            {
                // `File.Save()` without a name saves in place: no path to screen. A required path that is missing does not compile anyway.
                if (index < method.Parameters.Length && method.Parameters[index].IsOptional) continue;
                refusals.Add(new ScriptDiagnostic(line, column, PathDiagnosticId, $"{member}: the path argument (parameter {index + 1}) is missing."));
                continue;
            }

            var expression = argument.Expression;
            switch (expression)
            {
                // `File.Save("")` is `File.Save()`: ETABS saves in place, there is no path to screen.
                case LiteralExpressionSyntax empty when empty.IsKind(SyntaxKind.StringLiteralExpression) && empty.Token.ValueText.Length == 0
                                                        && index < method.Parameters.Length && method.Parameters[index].IsOptional:
                    break;

                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                    var value = literal.Token.ValueText;
                    literals.Add(value);
                    if (EtabsPathPolicy.StaticRefusal(value) is { } reason)
                        refusals.Add(new ScriptDiagnostic(line, column, PathDiagnosticId, $"{member}: path \"{value}\" refused — {reason}."));
                    break;

                case InvocationExpressionSyntax argsCall when ArgsKey(model, argsCall) is { } key:
                    argKeys.Add(key);
                    break;

                default:
                    refusals.Add(new ScriptDiagnostic(line, column, PathDiagnosticId,
                        $"{member}: path arguments must be a string literal or args.Str(\"key\") / args.Require(\"key\") with a literal key, so the bridge can check the path before the run."));
                    break;
            }
        }
    }

    private static ArgumentSyntax? FindArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, int index, IMethodSymbol method)
    {
        if (index < method.Parameters.Length)
        {
            var parameterName = method.Parameters[index].Name;
            var named = arguments.FirstOrDefault(a => a.NameColon is not null && a.NameColon.Name.Identifier.ValueText == parameterName);
            if (named is not null) return named;
        }

        return index < arguments.Count && arguments[index].NameColon is null ? arguments[index] : null;
    }

    /// <summary>
    ///     `args.Str("out")` / `args.Require("out")` on the bridge's own `args` global → "out"; anything else → null.
    ///     The receiver must be the global (not a `ScriptArgs` the script built or a type it declared under that
    ///     name), the method the engine's, and there must be no fallback argument — a fallback would be a path
    ///     nobody screened.
    /// </summary>
    private static string? ArgsKey(SemanticModel model, InvocationExpressionSyntax call)
    {
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method) return null;
        if (method.ContainingType.ToDisplayString() != "HPRebar.McpBridge.Core.Scripting.ScriptArgs" || method.Name is not ("Str" or "Require")) return null;
        if (call.ArgumentList.Arguments.Count != 1) return null;
        if (call.Expression is not MemberAccessExpressionSyntax access) return null;
        var receiver = model.GetSymbolInfo(access.Expression).Symbol;
        if (receiver is not (IFieldSymbol or IPropertySymbol) || receiver.Name != "args"
            || receiver.ContainingType.ToDisplayString() != "HPEtabs.McpBridge.Model.EtabsScriptGlobals") return null;
        return call.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
    }

    private static bool IsWrapperType(ITypeSymbol? type) =>
        type is not null && type.ContainingNamespace is { } ns && ns.Name == WrapperNamespace && ns.ContainingNamespace is { IsGlobalNamespace: true };
}
