using System.Text.Json.Serialization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Classification;

/// <summary>A runner-up classification: another rule that also matched, with its confidence.</summary>
public sealed record AecAlternative(string AecType, double Confidence, string RuleId);

/// <summary>
///     An entity read as an AEC object: what it is, how sure the rules are and why, its plan dimensions, and
///     the record it came from (for follow-up spatial work). Serialised camelCase; the shape stays in memory.
/// </summary>
public sealed class AecObject
{
    public required string Handle { get; init; }

    /// <summary>DXF name of the underlying entity.</summary>
    public required string Type { get; init; }

    public required string Layer { get; init; }

    public required string AecType { get; init; }

    public string Discipline => Classification.AecType.DisciplineOf(AecType);

    /// <summary>0..1; 0 for <see cref="Classification.AecType.Unknown"/>.</summary>
    public double Confidence { get; init; }

    public string? RuleId { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = [];

    /// <summary>widthMm / depthMm (footprints), lengthMm (runs), areaMm2, orientationDeg, centroidMm, diameterMm, text, blockName.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

    public IReadOnlyList<AecAlternative> Alternatives { get; init; } = [];

    public Box? BoundsMm { get; init; }

    // Not `required`: System.Text.Json refuses to serialise a type whose required member is [JsonIgnore]d (InvalidOperationException).
    [JsonIgnore]
    public AecEntityRecord Record { get; init; } = null!;

    [JsonIgnore]
    public PlanShape? Shape => Record.Shape;
}
