using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Mep;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The read half of the MEP tools: classify the MEP discipline (with no layer/type filter the scan is two passes — the rule
///     set's run and node layers, and its fitting block names — so the candidate cap is spent on MEP entities), turn open runs
///     into <see cref="MepRun"/>s with a system from the caller's layer map (else the layer), node entities into
///     <see cref="MepNode"/>s, and hand them to the network builder.
/// </summary>
public static class MepService
{
    public sealed record Outcome(MepOutcome Network, ClassificationService.Outcome Classification, int Examined, int ClosedRunsIgnored, int TinyRunsIgnored, bool Narrowed, bool Truncated);

    /// <summary>A caller's system map: name → layer wildcards; a run whose layer matches none is its own layer's system.</summary>
    public static Dictionary<string, IReadOnlyList<string>> ParseSystems(ScriptArgs systems)
    {
        var map = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in systems.Keys)
        {
            var patterns = systems.Strings(key);
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("detection.systems: a system needs a name.");
            if (patterns.Count == 0) throw new ArgumentException($"detection.systems.{key}: give the layer wildcards of the system (e.g. [\"M-CHW*\", \"*-CHWS\"]).");
            if (!map.TryAdd(key.Trim(), patterns)) throw new ArgumentException($"detection.systems: '{key}' is given twice.");
        }

        return map;
    }

    /// <summary>The first system (in declaration order) whose wildcards match the layer, else the layer itself.</summary>
    public static string SystemOf(string layer, IReadOnlyDictionary<string, IReadOnlyList<string>> systems)
    {
        foreach (var (name, patterns) in systems)
            if (EntityFilter.Matches(patterns, layer)) return name;
        return layer;
    }

    public static Outcome Read(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, EntityFilter filter, ClassificationRuleSet rules, GeometryTolerance tol,
        IReadOnlyDictionary<string, IReadOnlyList<string>> systems, double nearMissMm, int maxCandidates)
    {
        if (filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("networks are found per space: pass filter.space model or a layout name (handles pick every space).");
        var passes = Narrow(filter, rules);
        var mep = new HashSet<string> { Discipline.Mep };
        var noTexts = (AecClassifier.TextIndex.Empty, false); // no MEP rule reads nearby text: skip the index the rule set as a whole would ask for
        var first = ClassificationService.Classify(db, ed, tr, units, ct, passes?[0] ?? filter, rules, tol, mep, 0, includeUnknown: false, maxCandidates, noTexts);
        var objects = first.Objects.ToList();
        var examined = first.Query.Records.Count;
        var truncated = first.Query.Truncated;
        if (passes is { Length: 2 })
        {
            var second = ClassificationService.Classify(db, ed, tr, units, ct, passes[1], rules, tol, mep, 0, includeUnknown: false, maxCandidates, noTexts);
            var known = new HashSet<string>(objects.Select(o => o.Handle), StringComparer.Ordinal);
            objects.AddRange(second.Objects.Where(o => known.Add(o.Handle)));
            examined += second.Query.Records.Count;
            truncated |= second.Query.Truncated;
        }

        var runs = new List<MepRun>();
        var nodes = new List<MepNode>();
        var closedRuns = 0;
        var tinyRuns = 0;
        foreach (var o in objects)
        {
            ct.ThrowIfCancellationRequested();
            if (o.Shape is null || o.Shape.IsPoint) continue;
            if (MepRunKind.FromAecType(o.AecType) is { } runKind)
            {
                if (o.Shape.Closed) { closedRuns++; continue; }
                if (o.Shape.LengthMm <= tol.TinySegment) { tinyRuns++; continue; }
                runs.Add(new MepRun(o.Handle, runKind, o.Layer, SystemOf(o.Layer, systems), o.Shape, o.Shape.LengthMm));
            }
            else if (MepNodeKind.FromAecType(o.AecType) is { } nodeKind)
                nodes.Add(new MepNode(o.Handle, nodeKind, o.Layer, o.Shape));
        }

        var network = MepNetworkBuilder.Build(runs, nodes, tol, nearMissMm, ct);
        return new Outcome(network, first, examined, closedRuns, tinyRuns, passes is not null, truncated);
    }

    /// <summary>With no layer/type/block filter given: one pass over the rule set's MEP layers and types, one over its fitting block names.</summary>
    private static EntityFilter[]? Narrow(EntityFilter filter, ClassificationRuleSet rules)
    {
        if (filter.Layers.Count > 0 || filter.Types.Count > 0 || filter.BlockNames.Count > 0 || filter.Handles.Count > 0) return null;
        var mep = rules.Rules.Where(r => AecType.DisciplineOf(r.AecType) == Discipline.Mep).ToList();
        var layered = mep.Where(r => r.Layers.Count > 0).ToList();
        var named = mep.Where(r => r.Layers.Count == 0 && r.BlockNames.Count > 0).ToList();
        if (layered.Count == 0) return null;
        EntityFilter Copy(IReadOnlyList<string> layers, IReadOnlyList<string> types, IReadOnlyList<string> blockNames) => new()
        {
            Layers = layers, Types = types, BlockNames = blockNames, Colors = filter.Colors, Linetypes = filter.Linetypes, TextContains = filter.TextContains, VisibleOnly = filter.VisibleOnly, Space = filter.Space,
        };
        var byLayer = Copy(layered.SelectMany(r => r.Layers).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), layered.SelectMany(r => r.Types).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), []);
        return named.Count == 0 ? [byLayer] : [byLayer, Copy([], ["INSERT"], named.SelectMany(r => r.BlockNames).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())];
    }

    /// <summary>Connections listed per endpoint (a header where eight runs start at one point is ordinary); the count is exact.</summary>
    public const int MaxConnectedListed = 4;

    /// <summary>What a network looks like in a tool answer: runs and nodes capped with exact counts, open ends with locations.</summary>
    public static Dictionary<string, object?> Describe(MepNetwork n, int maxHandles, int maxOpenEnds) => new()
    {
        ["id"] = n.Id,
        ["system"] = n.System,
        ["systems"] = n.Systems,
        ["runs"] = n.Runs.Count,
        ["runHandles"] = n.Runs.Take(maxHandles).Select(r => r.Handle).ToArray(),
        ["byKind"] = n.Runs.GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count()),
        ["lengthMm"] = Math.Round(n.LengthMm, 1),
        ["duplicateOverlapMm"] = n.DuplicateOverlapMm,
        ["nodes"] = n.Nodes.Count,
        ["nodeHandles"] = n.Nodes.Take(maxHandles).Select(x => x.Handle).ToArray(),
        ["byNodeKind"] = n.Nodes.GroupBy(x => x.Kind).ToDictionary(g => g.Key, g => g.Count()),
        ["openEnds"] = n.OpenEnds.Count,
        ["openEndsMm"] = n.OpenEnds.Take(maxOpenEnds).Select(e => new { run = e.RunHandle, at = e.PointMm, nearest = e.NearestHandle, gapMm = e.NearestGapMm }).ToArray(),
        ["boundsMm"] = n.BoundsMm.Rounded(),
    };

    public static Dictionary<string, object?> Describe(MepEndpoint e, IReadOnlyDictionary<string, MepRun> runs) => new()
    {
        ["run"] = e.RunHandle,
        ["kind"] = runs.TryGetValue(e.RunHandle, out var r) ? r.Kind : null,
        ["layer"] = r?.Layer,
        ["system"] = e.System,
        ["end"] = e.End == 0 ? "start" : "end",
        ["pointMm"] = e.PointMm,
        ["state"] = e.State,
        ["connectedTo"] = e.ConnectedTo.Take(MaxConnectedListed).ToArray(),
        ["connectedCount"] = e.ConnectedTo.Count,
        ["nearestHandle"] = e.NearestHandle,
        ["nearestGapMm"] = e.NearestGapMm,
    };
}
