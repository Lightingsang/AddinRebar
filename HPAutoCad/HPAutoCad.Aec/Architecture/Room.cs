using System.Text.RegularExpressions;
using HPAutoCad.Aec.Geometry;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Architecture;

/// <summary>
///     A room the architecture tools reason about: its outline (from walls, or an explicit closed outline on a room layer), area,
///     perimeter, a point that is surely inside, and the labels found inside it — name, number, department — read by the rules the
///     caller passes (no room standard is hard-coded).
/// </summary>
public sealed record Room(
    string Id,
    string Source,
    IReadOnlyList<string> Handles,
    PlanShape Outline,
    double AreaMm2,
    double PerimeterMm,
    Pt CentroidMm,
    Pt LabelPointMm)
{
    public const string FromWalls = "walls";
    public const string FromOutline = "outline";

    public string? Name { get; init; }
    public string? Number { get; init; }
    public string? Department { get; init; }

    /// <summary>Handles of the TEXT/MTEXT inside the room (the labels above came from these).</summary>
    public IReadOnlyList<string> TextHandles { get; init; } = [];

    /// <summary>Wall / text handles listed per room; the counts are exact whatever the cap.</summary>
    public const int MaxHandlesListed = 32;
    public const int MaxTextHandlesListed = 16;

    public double AreaM2 => Math.Round(AreaMm2 / 1_000_000, 2);

    /// <summary>The room as the caller sees it: the outline cut to <paramref name="maxVertices"/> (0 = not asked for), handles and texts capped with their counts.</summary>
    public Dictionary<string, object?> Describe(int maxVertices) => new()
    {
        ["id"] = Id,
        ["source"] = Source,
        ["name"] = Name,
        ["number"] = Number,
        ["department"] = Department,
        ["areaMm2"] = Math.Round(AreaMm2, 1),
        ["areaM2"] = AreaM2,
        ["perimeterMm"] = Math.Round(PerimeterMm, 1),
        ["centroidMm"] = CentroidMm.Rounded(),
        ["labelPointMm"] = LabelPointMm.Rounded(),
        ["boundsMm"] = Outline.Bounds.Rounded(),
        ["vertices"] = Outline.Vertices.Count,
        ["outlineMm"] = maxVertices > 0 ? Outline.Vertices.Take(maxVertices).Select(p => p.Rounded()).ToArray() : null,
        ["outlineTruncated"] = maxVertices > 0 ? Outline.Vertices.Count > maxVertices : null,
        ["handles"] = Handles.Take(MaxHandlesListed).ToArray(),
        ["handleCount"] = Handles.Count,
        ["textHandles"] = TextHandles.Take(MaxTextHandlesListed).ToArray(),
        ["textCount"] = TextHandles.Count,
    };

    /// <summary>A point inside the outline: the centroid when it is, else the inner-most sample on a grid over the bounds (an L-shaped room's centroid can lie outside it).</summary>
    public static Pt InsidePoint(PlanShape outline, GeometryTolerance tol)
    {
        if (outline.ContainsPointXY(outline.Centroid, tol.PointEquality) && outline.DistanceToBoundaryXY(outline.Centroid) > tol.RoomGap) return outline.Centroid;
        var b = outline.Bounds;
        Pt best = outline.Centroid;
        var bestDepth = double.NegativeInfinity;
        const int steps = 12;
        for (var i = 1; i < steps; i++)
            for (var j = 1; j < steps; j++)
            {
                var p = new Pt(b.Min.X + b.Width * i / steps, b.Min.Y + b.Height * j / steps);
                if (!outline.ContainsPointXY(p, tol.PointEquality)) continue;
                var depth = outline.DistanceToBoundaryXY(p);
                if (depth > bestDepth) (best, bestDepth) = (p, depth);
            }

        return best;
    }
}

/// <summary>How labels inside a room are read: every pattern is the caller's; the default number is 2–4 digits, or letters + a separator + 3–4 digits (101, A-101, P.101, 1.01 — never a mark like B01, C1 or KT-12; A-12 needs numberPattern).</summary>
public sealed record RoomLabelRules(Regex Number, Regex? Department, Regex Ignore)
{
    public const string DefaultNumberPattern = @"^(?:[A-Z]{1,3}[-.\s]\s?\d{3,4}|\d{2,4})[A-Z]?$|^\d{1,2}\.\d{2,3}$";

    /// <summary>Area annotations ("24.5 m2", "24,5 m²") and pure numbers with units are never a name.</summary>
    public const string DefaultIgnorePattern = @"^\s*\d+([.,]\d+)?\s*(m2|m²|mm|sqm|sq\.? ?m)\s*$";

    public static RoomLabelRules From(ScriptArgs args)
    {
        try
        {
            var number = new Regex(args.Str("numberPattern") is { Length: > 0 } n ? n : DefaultNumberPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            var department = args.Str("departmentPattern") is { Length: > 0 } d ? new Regex(d, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200)) : null;
            var ignore = new Regex(args.Str("ignorePattern") is { Length: > 0 } i ? i : DefaultIgnorePattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            return new RoomLabelRules(number, department, ignore);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException($"labels: invalid regex — {exception.Message}");
        }
    }

    public static RoomLabelRules Default => From(ScriptArgs.Empty);
}

/// <summary>
///     Assigns the texts inside a room to name / number / department: every line of a text (an MTEXT tag "OFFICE\P101" is two lines)
///     is one label; the number is the first line matching the number pattern, the department the first match of its pattern (its
///     first group when it has one), the name the longest remaining line — so the tool's own tags read back as name + ignored area.
/// </summary>
public static class RoomLabels
{
    public sealed record Label(string Handle, string Text, Pt PositionMm);

    private static readonly string[] LineBreaks = ["\\P", "\r\n", "\n", "\r"];

    public static Room Apply(Room room, IReadOnlyList<Label> inside, RoomLabelRules rules)
    {
        string? number = null, department = null, name = null;
        try
        {
            foreach (var l in inside.OrderBy(l => l.PositionMm.DistanceXY(room.CentroidMm)))
            foreach (var line in l.Text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries))
            {
                var text = line.Trim();
                if (text.Length == 0 || rules.Ignore.IsMatch(text)) continue;
                if (number is null && rules.Number.IsMatch(text)) { number = text; continue; }
                if (department is null && rules.Department is { } dp && dp.Match(text) is { Success: true } m) { department = m.Groups.Count > 1 ? m.Groups[1].Value : m.Value; continue; }
                if (name is null || text.Length > name.Length) name = text;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            throw new ArgumentException("labels: a pattern took too long to match (catastrophic regex); simplify numberPattern / departmentPattern.");
        }

        return room with { Name = name, Number = number, Department = department, TextHandles = inside.Select(l => l.Handle).ToArray() };
    }
}
