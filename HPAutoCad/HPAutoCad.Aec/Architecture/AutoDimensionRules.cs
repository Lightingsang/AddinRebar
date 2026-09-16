using HPAutoCad.Aec.Geometry;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Architecture;

/// <summary>Something a dimension rule measures: a closed outline with the id the caller knows it by (a room id or an entity handle).</summary>
public sealed record DimensionSubject(string Id, PlanShape Outline, IReadOnlyList<string> Handles);

/// <summary>One dimension to draw: an aligned dimension between two points with its line offset to one side, and what it measures.</summary>
public sealed record DimensionPlan(string Rule, string Subject, string Side, Pt P1Mm, Pt P2Mm, Pt DimLineMm, double MeasurementMm);

/// <summary>A dimensioning rule: from a subject and its options to the dimensions it wants.</summary>
public interface IDimensionRule
{
    string Name { get; }
    IEnumerable<DimensionPlan> Plan(DimensionSubject subject, ScriptArgs options, GeometryTolerance tol);
}

/// <summary>
///     The rule framework of <c>arch_auto_dimension_plan</c>: rules arrive as data (<c>{rule, ...options}</c>), each known rule turns
///     every subject into dimension plans, the adapter draws them. One rule ships — <c>overall</c>, the extents of a closed outline —
///     and new ones register here with no change to the tool. Pure.
/// </summary>
public static class AutoDimensionRules
{
    public const double DefaultOffsetMm = 600;

    private static readonly IReadOnlyDictionary<string, IDimensionRule> Registry = new Dictionary<string, IDimensionRule>(StringComparer.OrdinalIgnoreCase)
    {
        [OverallRule.RuleName] = new OverallRule(),
    };

    public static IReadOnlyList<string> Known => Registry.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

    /// <summary>Every plan of every rule over every subject, in rule → subject → side order; a rule the framework does not know is the caller's error.</summary>
    public static IReadOnlyList<DimensionPlan> Plan(IReadOnlyList<ScriptArgs> rules, IReadOnlyList<DimensionSubject> subjects, GeometryTolerance tol, CancellationToken ct)
    {
        if (rules.Count == 0) throw new ArgumentException($"rules [] is required: at least one of {string.Join(", ", Known)} (e.g. {{\"rule\": \"overall\", \"offsetMm\": 600}}).");
        var plans = new List<DimensionPlan>();
        foreach (var options in rules)
        {
            var name = options.Str("rule") ?? throw new ArgumentException("rules[]: each rule needs \"rule\".");
            if (!Registry.TryGetValue(name.Trim(), out var rule)) throw new ArgumentException($"rules[]: '{name}' is not a known rule (known: {string.Join(", ", Known)}).");
            foreach (var subject in subjects)
            {
                ct.ThrowIfCancellationRequested();
                plans.AddRange(rule.Plan(subject, options, tol));
            }
        }

        return plans;
    }

    /// <summary>The overall width and height of a closed outline, dimension lines <c>offsetMm</c> outside its bounds on the requested sides.</summary>
    private sealed class OverallRule : IDimensionRule
    {
        public const string RuleName = "overall";
        public static readonly IReadOnlyList<string> Sides = ["bottom", "top", "left", "right"];

        public string Name => RuleName;

        public IEnumerable<DimensionPlan> Plan(DimensionSubject subject, ScriptArgs options, GeometryTolerance tol)
        {
            var offset = options.Double("offsetMm", DefaultOffsetMm);
            if (offset <= 0) throw new ArgumentException("rules[].offsetMm must be > 0.");
            var sides = options.Strings("sides") is { Count: > 0 } s ? s.Select(x => x.Trim().ToLowerInvariant()).ToArray() : ["bottom", "right"];
            var unknown = sides.FirstOrDefault(x => !Sides.Contains(x));
            if (unknown is not null) throw new ArgumentException($"rules[].sides: '{unknown}' is not one of {string.Join(", ", Sides)}.");
            var b = subject.Outline.Bounds;
            if (b.Width <= tol.TinySegment || b.Height <= tol.TinySegment) yield break;
            foreach (var side in sides.Distinct())
                yield return side switch
                {
                    "bottom" => new DimensionPlan(RuleName, subject.Id, side, new Pt(b.Min.X, b.Min.Y), new Pt(b.Max.X, b.Min.Y), new Pt(b.Center.X, b.Min.Y - offset), b.Width),
                    "top" => new DimensionPlan(RuleName, subject.Id, side, new Pt(b.Min.X, b.Max.Y), new Pt(b.Max.X, b.Max.Y), new Pt(b.Center.X, b.Max.Y + offset), b.Width),
                    "left" => new DimensionPlan(RuleName, subject.Id, side, new Pt(b.Min.X, b.Min.Y), new Pt(b.Min.X, b.Max.Y), new Pt(b.Min.X - offset, b.Center.Y), b.Height),
                    _ => new DimensionPlan(RuleName, subject.Id, side, new Pt(b.Max.X, b.Min.Y), new Pt(b.Max.X, b.Max.Y), new Pt(b.Max.X + offset, b.Center.Y), b.Height),
                };
        }
    }
}
