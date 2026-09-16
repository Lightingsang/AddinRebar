using System.Text.RegularExpressions;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Model;

/// <summary>
///     What a tool's <c>filter</c> object selects. Types, layers and block names take AutoCAD wildcards
///     (<c>*</c>, <c>?</c>, comma lists); handles short-circuit everything else. Parsed once from
///     <see cref="ScriptArgs"/> so every tool reads the same keys the same way.
/// </summary>
public sealed class EntityFilter
{
    public IReadOnlyList<string> Types { get; init; } = [];

    public IReadOnlyList<string> Layers { get; init; } = [];

    public IReadOnlyList<string> Colors { get; init; } = [];

    public IReadOnlyList<string> Linetypes { get; init; } = [];

    public IReadOnlyList<string> BlockNames { get; init; } = [];

    public string? TextContains { get; init; }

    public IReadOnlyList<string> Handles { get; init; } = [];

    public bool VisibleOnly { get; init; }

    /// <summary>model (default), current, all, or a layout name.</summary>
    public string Space { get; init; } = "model";

    public bool IsEmpty => Types.Count == 0 && Layers.Count == 0 && Colors.Count == 0 && Linetypes.Count == 0 && BlockNames.Count == 0
                           && TextContains is null && Handles.Count == 0 && !VisibleOnly;

    public static readonly IReadOnlyList<string> KnownKeys = ["types", "type", "layers", "layer", "colors", "color", "linetypes", "linetype", "blockNames", "blockName", "textContains", "handles", "handle", "visibleOnly", "space"];

    /// <summary>Accepts both the list keys (<c>types</c>) and their singular convenience forms (<c>type</c>).</summary>
    public static EntityFilter From(ScriptArgs? args, out IReadOnlyList<string> unknownKeys)
    {
        if (args is null || args.IsEmpty)
        {
            unknownKeys = [];
            return new EntityFilter();
        }

        unknownKeys = args.Keys.Where(k => !KnownKeys.Contains(k, StringComparer.OrdinalIgnoreCase)).ToArray();
        var handles = ListOrSingle(args, "handles", "handle").Select(h => h.Trim().ToUpperInvariant()).ToArray();
        return new EntityFilter
        {
            Types = ListOrSingle(args, "types", "type").Select(t => t.Trim().ToUpperInvariant()).ToArray(),
            Layers = ListOrSingle(args, "layers", "layer"),
            Colors = ListOrSingle(args, "colors", "color"),
            Linetypes = ListOrSingle(args, "linetypes", "linetype"),
            BlockNames = ListOrSingle(args, "blockNames", "blockName"),
            TextContains = string.IsNullOrWhiteSpace(args.Str("textContains")) ? null : args.Str("textContains"),
            Handles = handles,
            VisibleOnly = args.Bool("visibleOnly"),
            // A handle is unique across spaces: given handles, the space defaults to "all" so a paper-space entity is found unless the caller narrows it.
            Space = (args.Str("space", handles.Length > 0 ? "all" : "model") ?? "model").Trim(),
        };
    }

    private static string[] ListOrSingle(ScriptArgs args, string listKey, string singleKey)
    {
        var values = new List<string>();
        if (args.Has(listKey))
        {
            var raw = args.Strings(listKey);
            if (raw.Count == 0) values.AddRange(SplitList(args.Str(listKey))); // a single string given where a list was expected
            else values.AddRange(raw);
        }

        if (args.Has(singleKey)) values.AddRange(SplitList(args.Str(singleKey)));
        return values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<string> SplitList(string? text) => (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    ///     Wildcard match used on the handle path and for block names: <c>*</c> any run, <c>?</c> one character, case-insensitive;
    ///     a list matches when any pattern does. (The selection-engine path honours AutoCAD's full grammar — <c>~</c>, <c>#</c>,
    ///     <c>@</c>, <c>[…]</c> — so stick to <c>*</c>/<c>?</c> for patterns that must behave the same on both.)
    /// </summary>
    public static bool Matches(IReadOnlyList<string> patterns, string? value)
    {
        if (patterns.Count == 0) return true;
        if (value is null) return false;
        foreach (var pattern in patterns)
            if (WildcardMatch(pattern, value)) return true;
        return false;
    }

    public static bool WildcardMatch(string pattern, string value)
    {
        if (pattern.IndexOfAny(['*', '?']) < 0) return string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
