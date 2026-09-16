using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Classification;

/// <summary>
///     Runs a rule set over entity records. Every rule whose hard criteria hold is a candidate; the best
///     confidence wins and the others are listed as alternatives, so a wall drawn on a beam layer is still
///     visible as "maybe a beam". Nearby text (a column mark "C1" beside an outline) is evidence read from
///     a text index the caller builds once per drawing. Pure: no AutoCAD types.
/// </summary>
public static class AecClassifier
{
    public const int MaxAlternatives = 3;
    public const double MaxConfidence = 0.99;

    /// <summary>Text and attribute values carried per object are cut here so one verbose block cannot fill a page.</summary>
    public const int MaxTextChars = 120;
    public const int MaxAttributes = 8;

    /// <summary>TEXT/MTEXT records the classifier can look around for; built once from a query, empty when no rule needs it.</summary>
    public sealed class TextIndex
    {
        private readonly SpatialIndex<AecEntityRecord> _index = new();

        public TextIndex(IEnumerable<AecEntityRecord> texts)
        {
            foreach (var t in texts)
                if (t.Text is not null && t.BoundsMm is { } box) _index.Insert(box, t);
        }

        public static readonly TextIndex Empty = new([]);

        public int Count => _index.Count;

        public IEnumerable<AecEntityRecord> Near(Box area, double radiusMm) => _index.Query(area, radiusMm);
    }

    public static IReadOnlyList<AecObject> Classify(IReadOnlyList<AecEntityRecord> records, ClassificationRuleSet rules, TextIndex texts, GeometryTolerance tol,
        CancellationToken ct, IReadOnlySet<string>? disciplines = null, double minConfidence = 0, bool includeUnknown = false)
    {
        var results = new List<AecObject>(records.Count);
        var active = disciplines is null ? rules.Rules : rules.Rules.Where(r => disciplines.Contains(r.Discipline)).ToArray();

        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();
            var metrics = ShapeMetrics.Of(record);
            var matches = new List<(ClassificationRule Rule, double Confidence, List<string> Evidence)>();

            foreach (var rule in active)
            {
                if (rule.Reject(record, metrics) is not null) continue;
                var evidence = rule.EvidenceFor(record, metrics).ToList();
                var confidence = rule.Confidence;
                if (rule.NearbyTextRegex is { } nearby && record.BoundsMm is { } box)
                {
                    var hit = texts.Near(box, rule.NearbyRadiusMm).FirstOrDefault(t => nearby.IsMatch(t.Text!));
                    if (hit is not null)
                    {
                        confidence += rule.NearbyTextBoost;
                        evidence.Add($"nearby text \"{hit.Text}\" ({hit.Handle}) within {rule.NearbyRadiusMm:0} mm");
                    }
                }

                matches.Add((rule, Math.Min(MaxConfidence, confidence), evidence));
            }

            // Stable: equal confidence keeps rule-file order, so a project's first rule wins a tie deliberately.
            matches = matches.OrderByDescending(m => m.Confidence).ToList();
            var best = matches.FirstOrDefault();
            if (best.Rule is null || best.Confidence < minConfidence)
            {
                if (includeUnknown) results.Add(Unknown(record, metrics, matches));
                continue;
            }

            results.Add(new AecObject
            {
                Handle = record.Handle,
                Type = record.Type,
                Layer = record.Layer,
                AecType = best.Rule.AecType,
                Confidence = Math.Round(best.Confidence, 3),
                RuleId = best.Rule.Id,
                Evidence = best.Evidence,
                Properties = Properties(record, metrics),
                Alternatives = matches.Skip(1).Where(m => m.Rule.AecType != best.Rule.AecType).Take(MaxAlternatives).Select(m => new AecAlternative(m.Rule.AecType, Math.Round(m.Confidence, 3), m.Rule.Id)).ToArray(),
                BoundsMm = record.BoundsMm,
                Record = record,
            });
        }

        return results;
    }

    private static string Cut(string text) => text.Length <= MaxTextChars ? text : text[..MaxTextChars] + "…";

    private static AecObject Unknown(AecEntityRecord record, ShapeMetrics.Metrics metrics, List<(ClassificationRule Rule, double Confidence, List<string> Evidence)> matches) => new()
    {
        Handle = record.Handle,
        Type = record.Type,
        Layer = record.Layer,
        AecType = AecType.Unknown,
        Confidence = 0,
        Evidence = matches.Count == 0 ? ["no rule matched layer/type/size"] : [$"best match {matches[0].Rule.AecType} at {matches[0].Confidence:0.00} is below minConfidence"],
        Properties = Properties(record, metrics),
        Alternatives = matches.Take(MaxAlternatives).Select(m => new AecAlternative(m.Rule.AecType, Math.Round(m.Confidence, 3), m.Rule.Id)).ToArray(),
        BoundsMm = record.BoundsMm,
        Record = record,
    };

    /// <summary>The dimension block the AI reports: only the measures the shape has, rounded to a tenth of a millimetre.</summary>
    public static Dictionary<string, object> Properties(AecEntityRecord record, ShapeMetrics.Metrics m)
    {
        var p = new Dictionary<string, object>(StringComparer.Ordinal);
        if (m.IsCircular && m.DepthMm is { } d) p["diameterMm"] = Math.Round(d, 1);
        else
        {
            if (m.WidthMm is { } w) p["widthMm"] = Math.Round(w, 1);
            if (m.DepthMm is { } dp) p["depthMm"] = Math.Round(dp, 1);
        }

        if (m.LengthMm is { } l) p["lengthMm"] = Math.Round(l, 1);
        if (m.AreaMm2 is { } a) p["areaMm2"] = Math.Round(a, 1);
        if (m.OrientationDeg is { } o) p["orientationDeg"] = Math.Round(o, 2);
        if (m.CentroidMm is { } c) p["centroidMm"] = c.Rounded();
        p["closed"] = m.Closed;
        if (record.Text is not null) p["text"] = Cut(record.Text);
        if (record.BlockName is not null) p["blockName"] = record.BlockName;
        if (record.Attributes is { Count: > 0 } attributes)
        {
            p["attributes"] = attributes.Take(MaxAttributes).ToDictionary(kv => kv.Key, kv => Cut(kv.Value), StringComparer.OrdinalIgnoreCase);
            if (attributes.Count > MaxAttributes) p["attributesTruncated"] = attributes.Count - MaxAttributes;
        }

        return p;
    }
}
