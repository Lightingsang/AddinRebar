using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Coordination;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The read half of the coordination tools: one set is a filter plus the AEC types wanted (any discipline), classified once,
///     kept as the shaped subjects the pure detector and planner work on.
/// </summary>
public static class CoordinationService
{
    /// <param name="WithoutShape">Matching entities set aside: no plan shape, a point, or an open chain shorter than <c>tolerance.tinySegment</c>.</param>
    /// <param name="Spaces">Where the subjects live ("Model" or a layout name), distinct — one entry means every subject shares a space.</param>
    public sealed record SetOutcome(IReadOnlyList<ClashSubject> Subjects, ClassificationService.Outcome Classification, IReadOnlyList<string> AecTypes, int WithoutShape, IReadOnlyList<string> Spaces);

    /// <summary>The AEC types a set argument names, validated against the known types (case-insensitive); empty = every classified type.</summary>
    public static IReadOnlyList<string> ParseTypes(IReadOnlyList<string> names, string setName)
    {
        var wanted = new List<string>();
        foreach (var n in names)
        {
            var known = AecType.All.FirstOrDefault(t => t.Equals(n.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"{setName}.aecTypes: '{n}' is not an AEC type (known: {string.Join(", ", AecType.All)}).");
            if (!wanted.Contains(known)) wanted.Add(known);
        }

        return wanted;
    }

    public static SetOutcome Read(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, EntityFilter filter, IReadOnlyList<string> aecTypes, ClassificationRuleSet rules, GeometryTolerance tol, int maxCandidates)
    {
        var classification = ClassificationService.Classify(db, ed, tr, units, ct, filter, rules, tol, null, 0, includeUnknown: false, maxCandidates);
        var subjects = new List<ClashSubject>();
        var spaces = new List<string>();
        var withoutShape = 0;
        foreach (var o in classification.Objects)
        {
            ct.ThrowIfCancellationRequested();
            if (aecTypes.Count > 0 && !aecTypes.Contains(o.AecType, StringComparer.OrdinalIgnoreCase)) continue;
            if (o.Shape is null || o.Shape.IsPoint || !o.Shape.Closed && o.Shape.LengthMm <= tol.TinySegment) { withoutShape++; continue; }
            var space = o.Record.Space ?? "";
            subjects.Add(new ClashSubject(o.Handle, o.AecType, o.Layer, o.Shape, space));
            if (!spaces.Contains(space, StringComparer.OrdinalIgnoreCase)) spaces.Add(space);
        }

        return new SetOutcome(subjects.OrderBy(s => s.Handle.Length).ThenBy(s => s.Handle, StringComparer.Ordinal).ToArray(), classification, aecTypes, withoutShape, spaces);
    }

    public static OpeningSubject ToOpeningSubject(ClashSubject s) => new(s.Handle, s.AecType, s.Layer, s.Shape, s.Space);

    public static Dictionary<string, object?> Describe(OpeningRequest r) => new()
    {
        ["id"] = r.Id,
        ["route"] = r.RouteHandle,
        ["routeType"] = r.RouteType,
        ["host"] = r.HostHandle,
        ["hostType"] = r.HostType,
        ["centerMm"] = r.CenterMm,
        ["widthMm"] = r.WidthMm,
        ["heightMm"] = r.HeightMm,
        ["angleDeg"] = r.AngleDeg,
        ["label"] = r.Label,
    };
}
