using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     <c>create_issue_markup</c>: one marker (circle, rectangle or revision cloud) and one multileader carrying the issue id per
///     issue, on a dedicated markup layer created on demand, coloured by severity. Issues arrive as the objects the audit tools
///     returned (id, severity, location and/or handles, description) and are drawn in the space their entities live in
///     (<c>space: auto</c>), so a paper-space finding is clouded on its sheet. The original geometry is only read for its
///     extents, never modified. Two-phase: every issue is resolved and every space located before the first entity is appended;
///     the markup layer is created only when something will be drawn.
/// </summary>
public static partial class IssueMarkupService
{
    public const string DefaultLayer = "HP-MCP-ISSUES";
    public const double DefaultRadiusMm = 500;
    public const double DefaultTextHeightMm = 150;
    public const int MaxIssuesPerCall = 100;
    public const int MaxLabelChars = 80;
    public const string AutoSpace = "auto";
    public static readonly IReadOnlyList<string> Styles = ["circle", "rectangle", "revcloud"];

    /// <summary>A marker built from handles grows past their extents by this fraction of the larger side, so the cloud reads as "around", not "on".</summary>
    public const double MarginFactor = 0.25;

    /// <summary>The leader landing sits this many radii from the centre, up and to the right.</summary>
    public const double LeaderOffsetFactor = 1.5;

    /// <summary>Revision-cloud arcs: chord length as a fraction of the radius, bulge −0.5 = a 106° arc bowing outward on a counter-clockwise ring.</summary>
    public const double CloudChordFactor = 0.5;
    public const double CloudBulge = -0.5;

    /// <summary>ACI colours by severity: red, yellow, cyan; magenta when the issue carries none.</summary>
    private static readonly Dictionary<string, short> SeverityColors = new(StringComparer.OrdinalIgnoreCase) { [IssueSeverity.Critical] = 1, [IssueSeverity.Warning] = 2, [IssueSeverity.Info] = 4 };
    private const short DefaultMarkupColor = 6;

    /// <summary>One issue resolved: where it is drawn, how big, in which space; <see cref="Skip"/> names why it is not drawn at all.</summary>
    private sealed record Markup(int Index, string IssueId, string? Severity, string Label, Pt Center, double RadiusMm, Box Bounds, bool FromHandles, string SpaceName, ObjectId SpaceId, string? Skip = null);

    public static EditResult Create(EditContext cx, CancellationToken ct, IReadOnlyList<ScriptArgs> issues, string? style, string? layerName, double radiusMm, double textHeightMm, bool withLeader, bool colorBySeverity, string? space, bool atomic)
    {
        if (issues.Count == 0) throw new ArgumentException("issues [] is required: pass the issue objects an audit tool returned (issueId, severity, locationMm and/or handles, description).");
        if (issues.Count > MaxIssuesPerCall) throw new ArgumentException($"{issues.Count} issues; the maximum per call is {MaxIssuesPerCall}.");
        var shape = (style ?? "circle").Trim().ToLowerInvariant();
        if (!Styles.Contains(shape)) throw new ArgumentException($"style must be one of {string.Join(", ", Styles)}.");
        if (radiusMm <= 0 || textHeightMm <= 0) throw new ArgumentException("radiusMm and textHeightMm must be > 0.");
        var layer = string.IsNullOrWhiteSpace(layerName) ? DefaultLayer : layerName.Trim();
        if (EditContext.RefuseSymbolName(layer, "layer") is { } badName) throw new ArgumentException(badName);
        var requestedSpace = string.IsNullOrWhiteSpace(space) ? AutoSpace : space.Trim();
        var explicitSpaceId = ObjectId.Null;
        if (!requestedSpace.Equals(AutoSpace, StringComparison.OrdinalIgnoreCase))
        {
            explicitSpaceId = cx.SpaceId(requestedSpace, out var spaceError);
            if (explicitSpaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        }

        // Phase 1: every issue resolves to a centre, an extent and a space; the markup layer is usable. Nothing is opened for write.
        var result = new EditResult();
        var markups = new Markup?[issues.Count];
        var errors = new ToolError?[issues.Count];
        for (var i = 0; i < issues.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            markups[i] = Resolve(cx, issues[i], i, radiusMm, requestedSpace, explicitSpaceId, result, out errors[i]);
        }

        if (cx.FindLayer(layer, out _) is { } existing)
        {
            if (existing.IsLocked) return EditResult.Refused(new ToolError(ToolErrorCode.LayerLocked, $"markup layer '{layer}' is locked; unlock it or pass another layer."));
            if (existing.IsFrozen) return EditResult.Refused(new ToolError(ToolErrorCode.LayerFrozen, $"markup layer '{layer}' is frozen; thaw it or pass another layer."));
            if (existing.IsOff) result.Warn($"markup layer '{layer}' is off: the markers exist but are not displayed until it is turned on.");
        }

        var drawable = markups.Count(m => m is { Skip: null });
        if (atomic && errors.Any(e => e is not null) || drawable == 0)
        {
            result.Settle(issues.Select((_, i) => new ItemOutcome(i, markups[i] is not null && errors[i] is null, null, Error: errors[i])).ToArray());
            result.Success = false;
            result.Summary = new { requested = issues.Count, created = 0, refused = true, reason = errors.Any(e => e is not null) ? $"atomic: {errors.Count(e => e is not null)} issue(s) could not be located, nothing created" : "no issue has a location or resolvable handles, nothing created" };
            return result;
        }

        // Phase 2: layer, then one marker + leader per issue in its own space.
        var layerId = cx.EnsureLayer(layer, DefaultMarkupColor, out var layerCreated);
        var spaces = new Dictionary<ObjectId, BlockTableRecord>();
        var outcomes = new ItemOutcome[issues.Count];
        var placed = new List<object>();
        foreach (var m in markups)
        {
            ct.ThrowIfCancellationRequested();
            if (m is null || m.Skip is not null) continue;
            if (!spaces.TryGetValue(m.SpaceId, out var target)) spaces[m.SpaceId] = target = (BlockTableRecord)cx.Tr.GetObject(m.SpaceId, OpenMode.ForWrite);
            var color = Color.FromColorIndex(ColorMethod.ByAci, colorBySeverity && m.Severity is not null && SeverityColors.TryGetValue(m.Severity, out var aci) ? aci : DefaultMarkupColor);
            var centre = new Point3d(cx.ToDrawing(m.Center.X), cx.ToDrawing(m.Center.Y), 0);
            var r = cx.ToDrawing(m.RadiusMm);
            Entity marker = shape switch
            {
                "rectangle" => Rectangle(cx, m.Bounds),
                "revcloud" => RevisionCloud(cx, m.Bounds, m.RadiusMm),
                _ => new Circle(centre, Vector3d.ZAxis, r),
            };
            marker.SetDatabaseDefaults(cx.Db);
            marker.LayerId = layerId;
            marker.Color = color;
            target.AppendEntity(marker);
            cx.Tr.AddNewlyCreatedDBObject(marker, true);
            var markerHandle = marker.Handle.ToString();
            result.Created(markerHandle);
            string? leaderHandle = null;
            if (withLeader)
            {
                // The arrow sits on the marker: the 45° point of a circle, the top-right corner of a box.
                var margin = Math.Max(m.Bounds.Width, m.Bounds.Height) * MarginFactor;
                var arrow = shape == "circle" ? centre + new Vector3d(r * Math.Cos(Math.PI / 4), r * Math.Sin(Math.PI / 4), 0) : new Point3d(cx.ToDrawing(m.Bounds.Max.X + margin), cx.ToDrawing(m.Bounds.Max.Y + margin), 0);
                var landing = centre + new Vector3d(r * LeaderOffsetFactor, r * LeaderOffsetFactor, 0);
                if (shape != "circle") landing = arrow + new Vector3d(r * (LeaderOffsetFactor - 1), r * (LeaderOffsetFactor - 1), 0);
                var leader = AnnotationService.BuildMLeader(cx, m.Label, arrow, landing, textHeightMm);
                leader.LayerId = layerId;
                leader.Color = color;
                target.AppendEntity(leader);
                cx.Tr.AddNewlyCreatedDBObject(leader, true);
                leaderHandle = leader.Handle.ToString();
                result.Created(leaderHandle);
            }

            outcomes[m.Index] = new ItemOutcome(m.Index, true, markerHandle, marker.ObjectId.ObjectClass.DxfName, leaderHandle is null ? ["space:" + m.SpaceName] : ["leader:" + leaderHandle, "space:" + m.SpaceName]);
            placed.Add(new { issueId = m.IssueId, severity = m.Severity, markerHandle, leaderHandle, centerMm = m.Center.Rounded(), radiusMm = Math.Round(m.RadiusMm, 1), space = m.SpaceName, sizedByHandles = m.FromHandles });
        }

        for (var i = 0; i < issues.Count; i++)
            outcomes[i] ??= markups[i] is { Skip: { } why } ? new ItemOutcome(i, true, null, Changed: ["skipped: " + why]) : new ItemOutcome(i, false, null, Error: errors[i]);
        result.Settle(outcomes);
        result.Summary = new
        {
            requested = issues.Count, marked = placed.Count, skipped = markups.Count(m => m is { Skip: not null }), style = shape, layer, layerCreated, withLeader, colorBySeverity,
            spaces = markups.Where(m => m is { Skip: null }).Select(m => m!.SpaceName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), markups = placed,
        };
        return result;
    }
}
