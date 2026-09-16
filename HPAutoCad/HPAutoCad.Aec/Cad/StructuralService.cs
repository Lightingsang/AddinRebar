using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The read half of the structural tools: classify the structural discipline once (rules + text index), turn the objects
///     into <see cref="StructuralMember"/>s and <see cref="GridSystem"/>s, and hand them to the pure checks. Grid objects are
///     classified with the same rule set, so a project's grid layer convention lives in the rule file, not here.
/// </summary>
public static class StructuralService
{
    public sealed record Outcome(IReadOnlyList<StructuralMember> Members, GridSystem Grids, ClassificationService.Outcome Classification, AecClassifier.TextIndex Texts);

    /// <summary>A mark text (C1, B12, KC-3) sitting on or right beside the member — how far beside is this.</summary>
    public const double MarkReachMm = 300;

    /// <summary>
    ///     Members of the requested kinds (all when empty) and the grid system, from one classification pass over the filter. A block
    ///     member's MARK attribute is its mark; otherwise a mark-shaped TEXT beside it whose letters are a known prefix (the defaults
    ///     and <paramref name="prefixes"/>) — each text belongs to exactly one member, the one containing it or nearest to it.
    /// </summary>
    public static Outcome Read(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, EntityFilter filter, ClassificationRuleSet rules, GeometryTolerance tol,
        IReadOnlySet<string>? kinds, double minConfidence, int maxCandidates, double gridReachMm = GridDetector.BubbleReachMm, IReadOnlyDictionary<string, string>? prefixes = null)
    {
        var texts = ClassificationService.BuildTextIndex(db, ed, tr, units, ct, tol, filter.Space);
        var classification = ClassificationService.Classify(db, ed, tr, units, ct, filter, rules, tol, new HashSet<string> { Discipline.Structural }, minConfidence, includeUnknown: false, maxCandidates, texts);
        var members = new List<StructuralMember>();
        var gridObjects = new List<AecObject>();
        foreach (var o in classification.Objects)
        {
            ct.ThrowIfCancellationRequested();
            if (o.AecType == AecType.StructuralGrid) { gridObjects.Add(o); continue; }
            var member = StructuralMember.From(o, ShapeMetrics.Of(o.Record));
            if (member is null || (kinds is not null && !kinds.Contains(member.Kind))) continue;
            members.Add(member);
        }

        var marked = ClaimMarks(members, texts.Index, tol, prefixes ?? new Dictionary<string, string>());
        var grids = GridDetector.Detect(gridObjects, texts.Index, tol, gridReachMm);
        return new Outcome(marked, grids, classification, texts.Index);
    }

    /// <summary>Every mark-shaped text within reach is claimed once: by the member containing it, else the member of the prefix's kind, else the nearest; a member keeps the claimed text nearest its centre.</summary>
    private static IReadOnlyList<StructuralMember> ClaimMarks(List<StructuralMember> members, AecClassifier.TextIndex texts, GeometryTolerance tol, IReadOnlyDictionary<string, string> prefixes)
    {
        // The defaults and the caller's prefixes together: a drawing moving from C to KC still shows its C marks (foreign under KC, so never renumbered silently).
        var known = new HashSet<string>(MemberKind.All.Select(MemberKind.DefaultPrefix).Concat(MemberKind.All.Select(k => MemberTagging.Prefix(prefixes, k))), StringComparer.OrdinalIgnoreCase);
        var claims = new List<(int Member, AecEntityRecord Text, string Prefix, bool Contained, double Gap)>();
        for (var i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Mark is not null) continue; // the attribute is the mark
            var reach = m.BoundsMm.Expand(MarkReachMm);
            foreach (var t in texts.Near(m.BoundsMm, MarkReachMm))
            {
                if (MemberTagging.Parse(t.Text) is not { } parts || !known.Contains(parts.Prefix)) continue;
                if ((t.PositionMm ?? t.BoundsMm?.Center) is not { } p || !reach.ContainsXY(p, tol.PointEquality)) continue;
                var contained = m.Shape.ContainsPointXY(p, tol.PointEquality);
                claims.Add((i, t, parts.Prefix, contained, contained ? 0 : m.Shape.DistanceToBoundaryXY(p)));
            }
        }

        var owner = new Dictionary<int, (AecEntityRecord Text, double Gap)>();
        foreach (var byText in claims.GroupBy(c => c.Text.Handle))
        {
            var best = byText.OrderByDescending(c => c.Contained)
                .ThenByDescending(c => c.Prefix.Equals(MemberTagging.Prefix(prefixes, members[c.Member].Kind), StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.Gap).ThenBy(c => members[c.Member].Handle, StringComparer.Ordinal).First();
            var centreGap = (best.Text.PositionMm ?? best.Text.BoundsMm!.Value.Center).DistanceXY(members[best.Member].CenterMm);
            if (!owner.TryGetValue(best.Member, out var current) || centreGap < current.Gap) owner[best.Member] = (best.Text, centreGap);
        }

        return members.Select((m, i) => owner.TryGetValue(i, out var o) ? m with { Mark = o.Text.Text!.Trim(), MarkHandle = o.Text.Handle, MarkSource = StructuralMember.MarkFromText } : m).ToArray();
    }

    /// <summary>The kinds a tool argument names, validated against <see cref="MemberKind.All"/>; null when empty (every kind).</summary>
    public static HashSet<string>? ParseKinds<T>(IReadOnlyList<string> kinds, AnalysisResult<T>? result)
    {
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in kinds)
        {
            var known = MemberKind.All.FirstOrDefault(x => x.Equals(k.Trim(), StringComparison.OrdinalIgnoreCase));
            if (known is null) result?.Warn($"kinds: '{k}' is not known (known: {string.Join(", ", MemberKind.All)}).");
            else wanted.Add(known);
        }

        return wanted.Count == 0 ? null : wanted;
    }

    /// <summary>The per-kind prefixes a tool argument carries, keys normalised to the kind names; a kind named twice is the caller's error.</summary>
    public static Dictionary<string, string> ParsePrefixes(ScriptArgs prefixes)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in prefixes.Keys)
        {
            var kind = MemberKind.All.FirstOrDefault(k => k.Equals(key.Trim(), StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"prefixes.{key}: not a kind (known: {string.Join(", ", MemberKind.All)}).");
            if (!map.TryAdd(kind, prefixes.Str(key)?.Trim() ?? "")) throw new ArgumentException($"prefixes: '{kind}' is given twice.");
        }

        return map;
    }

    /// <summary>What a member looks like in a tool answer: the record minus the shape, with the section spelled out.</summary>
    public static Dictionary<string, object?> Describe(StructuralMember m) => new()
    {
        ["handle"] = m.Handle,
        ["kind"] = m.Kind,
        ["aecType"] = m.AecType,
        ["layer"] = m.Layer,
        ["confidence"] = m.Confidence,
        ["mark"] = m.Mark,
        ["markHandle"] = m.MarkHandle,
        ["markSource"] = m.MarkSource,
        ["section"] = m.Section,
        ["widthMm"] = m.WidthMm,
        ["depthMm"] = m.DepthMm,
        ["lengthMm"] = m.LengthMm,
        ["areaMm2"] = m.AreaMm2,
        ["orientationDeg"] = m.OrientationDeg,
        ["centerMm"] = m.CenterMm.Rounded(),
        ["boundsMm"] = m.BoundsMm.Rounded(),
        ["axisMm"] = m.Axis is { } a ? new { start = a.A.Rounded(), end = a.B.Rounded() } : null,
    };
}
