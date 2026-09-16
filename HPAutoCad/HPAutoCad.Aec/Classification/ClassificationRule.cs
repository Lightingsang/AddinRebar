using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Classification;

/// <summary>
///     One rule of a classification rule set, as written in JSON. Every criterion that is set must hold
///     (layers, types, block names, closed, sizes, aspect, own text); <see cref="NearbyTextPattern"/> is
///     evidence, not a requirement — finding it raises the confidence by <see cref="NearbyTextBoost"/>.
///     Sizes are millimetres; patterns are AutoCAD wildcards for names and .NET regular expressions for text.
/// </summary>
public sealed class ClassificationRule
{
    public string Id { get; set; } = "";

    public string AecType { get; set; } = Classification.AecType.Unknown;

    /// <summary>Base confidence when every criterion holds (0..1).</summary>
    public double Confidence { get; set; } = 0.8;

    private List<string> _layers = [];
    private List<string> _types = [];
    private List<string> _blockNames = [];

    /// <summary>Layer name wildcards (any may match); empty = any layer. A JSON null reads as empty.</summary>
    public List<string> Layers { get => _layers; set => _layers = value ?? []; }

    /// <summary>DXF names (LINE, LWPOLYLINE, CIRCLE, INSERT…); empty = any type.</summary>
    public List<string> Types { get => _types; set => _types = value ?? []; }

    /// <summary>Block definition name wildcards (INSERT only); empty = no block requirement.</summary>
    public List<string> BlockNames { get => _blockNames; set => _blockNames = value ?? []; }

    /// <summary>true = closed footprint required, false = open run required, null = either.</summary>
    public bool? Closed { get; set; }

    /// <summary>Long side of a footprint / length of a run, mm.</summary>
    public double? MinSizeMm { get; set; }

    public double? MaxSizeMm { get; set; }

    /// <summary>Short side of a footprint, mm — setting it requires a footprint (a run has no short side and is rejected).</summary>
    public double? MinShortSideMm { get; set; }

    public double? MaxShortSideMm { get; set; }

    /// <summary>Long side / short side of a footprint — setting it requires a footprint.</summary>
    public double? MinAspectRatio { get; set; }

    public double? MaxAspectRatio { get; set; }

    /// <summary>Regex the entity's own text / block name / attribute values must match.</summary>
    public string? TextPattern { get; set; }

    /// <summary>Regex a TEXT/MTEXT within <see cref="NearbyRadiusMm"/> should match — evidence, adds <see cref="NearbyTextBoost"/>.</summary>
    public string? NearbyTextPattern { get; set; }

    public double NearbyRadiusMm { get; set; } = 1000;

    public double NearbyTextBoost { get; set; } = 0.05;

    /// <summary>Free text shown as evidence when the rule matches, e.g. "closed outline on a column layer".</summary>
    public string? Evidence { get; set; }

    [JsonIgnore]
    public string Discipline => Classification.AecType.DisciplineOf(AecType);

    private Regex? _text;
    private Regex? _nearby;

    /// <summary>A pattern that takes longer than this on one string is broken (catastrophic backtracking), not slow.</summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    [JsonIgnore]
    public Regex? TextRegex => TextPattern is null ? null : _text ??= new Regex(TextPattern, Options, RegexTimeout);

    [JsonIgnore]
    public Regex? NearbyTextRegex => NearbyTextPattern is null ? null : _nearby ??= new Regex(NearbyTextPattern, Options, RegexTimeout);

    /// <summary>What a rule requires that the record does not satisfy — null when the hard criteria all hold.</summary>
    public string? Reject(AecEntityRecord record, ShapeMetrics.Metrics m)
    {
        if (Layers.Count > 0 && !EntityFilter.Matches(Layers, record.Layer)) return "layer";
        if (Types.Count > 0 && !Types.Any(t => string.Equals(t, record.Type, StringComparison.OrdinalIgnoreCase))) return "type";
        if (BlockNames.Count > 0 && !EntityFilter.Matches(BlockNames, record.BlockName)) return "block name";
        if (Closed is { } closed && m.Closed != closed) return closed ? "not a closed footprint" : "not an open run";
        if (MinSizeMm is { } minSize && !(m.SizeMm >= minSize)) return $"size < {minSize}";
        if (MaxSizeMm is { } maxSize && !(m.SizeMm <= maxSize)) return $"size > {maxSize}";
        if (MinShortSideMm is { } minShort && !(m.WidthMm >= minShort)) return $"short side < {minShort}";
        if (MaxShortSideMm is { } maxShort && !(m.WidthMm <= maxShort)) return $"short side > {maxShort}";
        if (MinAspectRatio is { } minAspect && !(m.AspectRatio >= minAspect)) return $"aspect < {minAspect}";
        if (MaxAspectRatio is { } maxAspect && !(m.AspectRatio <= maxAspect)) return $"aspect > {maxAspect}";
        if (TextRegex is { } regex && !regex.IsMatch(OwnText(record))) return "text";
        return null;
    }

    /// <summary>Human-readable evidence lines for a match — what the AI shows the user.</summary>
    public IEnumerable<string> EvidenceFor(AecEntityRecord record, ShapeMetrics.Metrics m)
    {
        if (Evidence is not null) yield return Evidence;
        if (Layers.Count > 0) yield return $"layer {record.Layer} matches {string.Join("|", Layers)}";
        if (BlockNames.Count > 0) yield return $"block {record.BlockName} matches {string.Join("|", BlockNames)}";
        if (Closed == true && m.DepthMm is not null) yield return m.IsCircular ? $"circular footprint Ø{m.DepthMm:0}" : $"closed footprint {m.WidthMm:0}×{m.DepthMm:0} mm";
        if (Closed == false && m.LengthMm is not null) yield return $"open run {m.LengthMm:0} mm";
        if (TextPattern is not null) yield return $"text matches /{TextPattern}/";
    }

    public static string OwnText(AecEntityRecord record) =>
        string.Join(" ", new[] { record.Text, record.BlockName }.Concat(record.Attributes?.Values ?? []).Where(s => !string.IsNullOrEmpty(s)));
}
