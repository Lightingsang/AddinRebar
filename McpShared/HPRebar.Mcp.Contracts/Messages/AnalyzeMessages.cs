using System.Collections.Generic;

namespace HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Parameters of `revit.analyze`: look at a script without running it. <see cref="Transaction"/> is the
///     mode a proposed tool declares (`none` / `auto` / `manual`); a host whose reads and writes are told apart
///     statically (ETABS) refuses a `none` declaration on code that writes. Null when unknown; bridges that do
///     not care ignore it.
/// </summary>
public sealed record AnalyzeRequest(string Code, string? Transaction = null);

/// <summary>
///     Static facts about a script, computed on the bridge's pipe thread (guard, compile, syntax walk) —
///     never on Revit's thread. The registry uses it to validate a proposed tool and to show the model
///     which literals it should turn into `args` before packaging a successful run as a tool.
/// </summary>
public sealed class AnalyzeResult
{
    /// <summary>True when the guard passed and the compiler produced no errors.</summary>
    public bool Compiles { get; set; }

    public IReadOnlyList<ScriptDiagnostic> GuardViolations { get; set; } = System.Array.Empty<ScriptDiagnostic>();

    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; set; } = System.Array.Empty<ScriptDiagnostic>();

    /// <summary>Numeric and string literals that are candidates for parameters.</summary>
    public IReadOnlyList<CodeLiteral> Literals { get; set; } = System.Array.Empty<CodeLiteral>();

    /// <summary>Every `args.X("key")` the script reads.</summary>
    public IReadOnlyList<ArgUsage> ArgKeys { get; set; } = System.Array.Empty<ArgUsage>();

    public int LineCount { get; set; }

    public bool HasLoops { get; set; }

    /// <summary>The script opens its own Transaction / TransactionGroup — it needs `transaction: "manual"`.</summary>
    public bool UsesTransaction { get; set; }

    /// <summary>The compiled script was already in the bridge cache.</summary>
    public bool CacheHit { get; set; }
}

/// <summary>
///     One literal in the source. <see cref="Kind"/> is `number` or `string`; <see cref="Context"/> is the
///     enclosing statement trimmed for display; <see cref="BoundTo"/> is the variable or constant name when
///     the literal initialises one (`double spacing = 150;` → "spacing").
/// </summary>
public sealed record CodeLiteral(int Line, int Column, string Kind, string Value, string Context, string? BoundTo);

/// <summary>An `args` read: key, accessor used (`Double`, `Str`, …) and the line it appears on.</summary>
public sealed record ArgUsage(string Key, string Accessor, int Line);
