using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Text;

/// <summary>The pre-Unicode Vietnamese charset a font draws, as far as HP code cares.</summary>
public enum LegacyCharset
{
    /// <summary>Not a Vietnamese legacy font (Symbol, Wingdings) — or an unknown one: see <see cref="VnFontKind"/>.</summary>
    None,
    Tcvn3,
    VniWin,
}

public enum VnFontKind
{
    /// <summary>A font the rules map to TCVN3 or VNI-Win.</summary>
    Legacy,

    /// <summary>A font the rules name as never holding Vietnamese bytes (Symbol: <c>f</c> draws φ) — its text is never converted.</summary>
    NotLegacy,

    /// <summary>Any other font (Arial, an unlisted SHX): untouched unless the caller forces a <c>sourceCharset</c>.</summary>
    Unknown,
}

/// <summary>What the rules say about one font: its charset, whether it draws only capitals (.VnTimeH), and the Unicode face it becomes.</summary>
public sealed record VnFontInfo(VnFontKind Kind, LegacyCharset Charset, bool UpperCaseOnly, string? TargetFace, string? Pattern)
{
    public static readonly VnFontInfo Unknown = new(VnFontKind.Unknown, LegacyCharset.None, false, null, null);

    public bool IsLegacy => Kind == VnFontKind.Legacy;
}

/// <summary>
///     The font rules (embedded <c>Rules/vn-legacy-fonts.default.json</c>) plus the caller's <c>fontMap</c>: a font is matched by its
///     TrueType face first, then by its file name; the first rule wins. <c>fontMap</c> only chooses the Unicode face a legacy font becomes —
///     it never makes a font legacy.
/// </summary>
public sealed class VnFontRules
{
    private const string ResourceName = "HPAutoCad.Aec.Rules.vn-legacy-fonts.default.json";

    private static readonly Lazy<VnFontRules> Embedded = new(LoadEmbedded);

    private readonly IReadOnlyList<FontRule> _rules;
    private readonly IReadOnlyList<KeyValuePair<string, string>> _overrides;

    private VnFontRules(IReadOnlyList<FontRule> rules, IReadOnlyList<KeyValuePair<string, string>> overrides)
    {
        _rules = rules;
        _overrides = overrides;
    }

    public static VnFontRules Default => Embedded.Value;

    /// <summary>The same rules with the caller's face choices (<c>{"VNI-Avo*": "Tahoma", "vn_vni.shx": "Arial"}</c>) taking precedence.</summary>
    public VnFontRules WithFontMap(IEnumerable<KeyValuePair<string, string>> fontMap)
    {
        var overrides = fontMap.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)).ToList();
        foreach (var (pattern, face) in overrides)
            if (string.IsNullOrWhiteSpace(face)) throw new ArgumentException($"fontMap['{pattern}'] must name a Unicode TrueType face (for example \"Arial\").");
        return new VnFontRules(_rules, overrides);
    }

    /// <summary>
    ///     A TrueType style is judged by its face alone — its file name can be a stale leftover (face Arial, file VTIMESN0.TTF draws
    ///     Arial); an SHX style, which has no face, by its file.
    /// </summary>
    public VnFontInfo Classify(string? face, string? fileName)
    {
        string?[] candidates = string.IsNullOrWhiteSpace(face) ? [fileName, Path.GetFileName(fileName ?? "")] : [face];
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var rule = _rules.FirstOrDefault(r => EntityFilter.WildcardMatch(r.Pattern, candidate.Trim()));
            if (rule is null) continue;
            if (rule.ParsedCharset == LegacyCharset.None) return new VnFontInfo(VnFontKind.NotLegacy, LegacyCharset.None, false, null, rule.Pattern);
            return new VnFontInfo(VnFontKind.Legacy, rule.ParsedCharset, rule.UpperCaseOnly, Override(face, fileName) ?? rule.TargetFace, rule.Pattern);
        }

        return VnFontInfo.Unknown;
    }

    /// <summary>The caller's face for a font no rule knows (forced <c>sourceCharset</c>), or null.</summary>
    public string? Override(string? face, string? fileName)
    {
        foreach (var (pattern, target) in _overrides)
            foreach (var candidate in new[] { face, fileName, Path.GetFileName(fileName ?? "") })
                if (!string.IsNullOrWhiteSpace(candidate) && EntityFilter.WildcardMatch(pattern, candidate.Trim()))
                    return target.Trim();
        return null;
    }

    private static VnFontRules LoadEmbedded()
    {
        using var stream = typeof(VnFontRules).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"embedded resource {ResourceName} is missing");
        var file = JsonSerializer.Deserialize<RuleFile>(stream, JsonOptions) ?? throw new InvalidOperationException($"{ResourceName} is empty");
        foreach (var rule in file.Fonts) _ = rule.ParsedCharset; // fail at load on a misspelt charset
        return new VnFontRules(file.Fonts, []);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private sealed record RuleFile(int Version, string? Description, IReadOnlyList<FontRule> Fonts);

    private sealed record FontRule(string Pattern, string Charset, bool UpperCaseOnly = false, string? TargetFace = null)
    {
        [JsonIgnore]
        public LegacyCharset ParsedCharset => ParseCharset(Charset) ?? throw new InvalidOperationException($"font rule '{Pattern}': charset '{Charset}' is not none, tcvn3 or vni");
    }

    /// <summary><c>tcvn3</c> / <c>abc</c>, <c>vni</c> / <c>vni-win</c>, <c>none</c>; null for anything else.</summary>
    public static LegacyCharset? ParseCharset(string? text) => (text ?? "").Trim().ToLowerInvariant() switch
    {
        "none" => LegacyCharset.None,
        "tcvn3" or "abc" => LegacyCharset.Tcvn3,
        "vni" or "vni-win" or "vniwin" => LegacyCharset.VniWin,
        _ => null,
    };
}
