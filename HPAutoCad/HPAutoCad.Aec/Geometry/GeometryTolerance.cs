using System.Text.Json.Serialization;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Geometry;

/// <summary>
///     Every tolerance the engine uses, in millimetres and degrees, in one place — services take the
///     record, never a literal. Defaults suit building plans drawn in millimetres; a tool's optional
///     <c>tolerance</c> object overrides any member by its camelCase name.
/// </summary>
public sealed record GeometryTolerance(
    double PointEquality = 0.5,
    double EndpointConnection = 10,
    double Collinearity = 1,
    double ParallelAngle = 0.5,
    double Duplicate = 1,
    double TinySegment = 5,
    double RoomGap = 25)
{
    public static readonly GeometryTolerance Default = new();

    /// <summary>Chord error allowed when an arc is tessellated — half the point-equality tolerance keeps tessellation invisible to the predicates.</summary>
    [JsonIgnore]
    public double ChordError => Math.Max(PointEquality / 2, 0.05);

    /// <summary>Overrides from a tool's <c>tolerance</c> argument; unknown keys are reported so a typo never silently keeps a default.</summary>
    public static GeometryTolerance From(ScriptArgs? args, out IReadOnlyList<string> unknownKeys)
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "pointEquality", "endpointConnection", "collinearity", "parallelAngle", "duplicate", "tinySegment", "roomGap" };
        if (args is null || args.IsEmpty)
        {
            unknownKeys = [];
            return Default;
        }

        // A tolerance that is zero, negative or not a number cannot be used: report it like an unknown key and keep the default.
        unknownKeys = args.Keys.Where(k => !known.Contains(k) || !(args.Double(k, double.NaN) > 0)).ToArray();
        var d = Default;
        var result = new GeometryTolerance(
            Positive(args.Double("pointEquality", d.PointEquality), d.PointEquality),
            Positive(args.Double("endpointConnection", d.EndpointConnection), d.EndpointConnection),
            Positive(args.Double("collinearity", d.Collinearity), d.Collinearity),
            Positive(args.Double("parallelAngle", d.ParallelAngle), d.ParallelAngle),
            Positive(args.Double("duplicate", d.Duplicate), d.Duplicate),
            Positive(args.Double("tinySegment", d.TinySegment), d.TinySegment),
            Positive(args.Double("roomGap", d.RoomGap), d.RoomGap));
        return result;
    }

    private static double Positive(double value, double fallback) => value > 0 && double.IsFinite(value) ? value : fallback;
}
