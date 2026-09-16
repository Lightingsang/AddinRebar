using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Phase 1 of <see cref="IssueMarkupService"/>: one issue object → where and how big the marker is, and in which space. A
///     given <c>locationMm</c> is the finding, so the marker stays at <c>radiusMm</c> around it; only an issue that has handles
///     but no location grows to cover their extents. The space is the entities' space (first resolvable handle) unless the caller
///     named one. Plus the marker shapes.
/// </summary>
public static partial class IssueMarkupService
{
    private static Markup? Resolve(EditContext cx, ScriptArgs issue, int index, double radiusMm, string requestedSpace, ObjectId explicitSpaceId, EditResult result, out ToolError? error)
    {
        error = null;
        var id = issue.Str("issueId") ?? issue.Str("id") ?? $"ISSUE-{index + 1}";
        var severity = issue.Str("severity")?.Trim().ToLowerInvariant();
        var label = $"{id}: {issue.Str("description") ?? issue.Str("type") ?? ""}".TrimEnd(':', ' ');
        if (label.Length > MaxLabelChars) label = label[..MaxLabelChars] + "…";

        // Handles: extents + the space the entities live in.
        Box? bounds = null;
        string? entitySpace = null;
        var handles = issue.Strings("handles");
        var missing = new List<string>();
        var reader = new EntityShapeReader(cx.Db, cx.Tr, cx.Units, GeometryTolerance.Default);
        foreach (var handle in handles)
        {
            var entity = HandleResolver.OpenEntity(cx.Db, cx.Tr, handle, out _);
            if (entity is null) { missing.Add(handle); continue; }
            var record = reader.Read(entity, false);
            if (record.BoundsMm is { } b) bounds = bounds is null ? b : bounds.Value.Union(b);
            entitySpace ??= record.Space;
            if (record.Space is not null && !string.Equals(record.Space, entitySpace, StringComparison.OrdinalIgnoreCase)) result.WarnItem(index, $"handles span several spaces; drawn in {entitySpace}.");
        }

        if (handles.Count > 0 && missing.Count == handles.Count)
        {
            error = ToolError.Argument($"issues[{index}] ({id}): none of its handles resolve ({string.Join(", ", missing)}).");
            return null;
        }

        if (missing.Count > 0) result.WarnItem(index, $"handle(s) {string.Join(", ", missing)} could not be resolved; the marker covers the rest.");

        // Location: the finding itself when given; otherwise the centre of the handles' extents.
        Pt? centre = null;
        var fromHandles = false;
        if (issue.Has("locationMm"))
        {
            var p = cx.Point(issue, "locationMm", out var pointError);
            if (p is null)
            {
                error = ToolError.Argument($"issues[{index}] ({id}): {pointError!.Message}");
                return null;
            }

            centre = new Pt(cx.ToMm(p.Value.X), cx.ToMm(p.Value.Y));
        }
        else if (bounds is { } box)
        {
            centre = box.Center;
            fromHandles = true;
        }

        if (centre is null)
        {
            // A table finding (layer naming, unused layer…) has nothing to point at: skipped, said so, never a refusal.
            result.WarnItem(index, $"{id} has neither locationMm nor handles (a table-level finding) — not drawn.");
            return new Markup(index, id, severity, label, Pt.Origin, radiusMm, Box.Empty, false, "", ObjectId.Null, Skip: "no location or handles");
        }

        var radius = fromHandles && bounds is { } hb ? Math.Max(radiusMm, hb.HalfDiagonalXY * (1 + MarginFactor)) : radiusMm;
        var extent = fromHandles && bounds is { } eb ? eb : Box.Of(new Pt(centre.Value.X - radius, centre.Value.Y - radius), new Pt(centre.Value.X + radius, centre.Value.Y + radius));

        // Space: the entities' own unless the caller named one (then say so when they disagree).
        string spaceName;
        ObjectId spaceId;
        if (!explicitSpaceId.IsNull)
        {
            spaceName = requestedSpace;
            spaceId = explicitSpaceId;
            if (entitySpace is not null && !SpaceMatches(cx, entitySpace, explicitSpaceId)) result.WarnItem(index, $"drawn in {requestedSpace} although its entities are in {entitySpace}.");
        }
        else
        {
            spaceName = entitySpace ?? "current";
            spaceId = entitySpace is null ? cx.Db.CurrentSpaceId : cx.SpaceId(entitySpace.Equals("Model", StringComparison.OrdinalIgnoreCase) ? "model" : entitySpace, out _);
            if (spaceId.IsNull) spaceId = cx.Db.CurrentSpaceId;
        }

        return new Markup(index, id, severity, label, centre.Value, radius, extent, fromHandles, spaceName, spaceId);
    }

    private static bool SpaceMatches(EditContext cx, string entitySpace, ObjectId spaceId) =>
        cx.SpaceId(entitySpace.Equals("Model", StringComparison.OrdinalIgnoreCase) ? "model" : entitySpace, out _) == spaceId;

    private static Polyline Rectangle(EditContext cx, Box b)
    {
        var margin = Math.Max(b.Width, b.Height) * MarginFactor;
        var pl = new Polyline(4);
        pl.AddVertexAt(0, new Point2d(cx.ToDrawing(b.Min.X - margin), cx.ToDrawing(b.Min.Y - margin)), 0, 0, 0);
        pl.AddVertexAt(1, new Point2d(cx.ToDrawing(b.Max.X + margin), cx.ToDrawing(b.Min.Y - margin)), 0, 0, 0);
        pl.AddVertexAt(2, new Point2d(cx.ToDrawing(b.Max.X + margin), cx.ToDrawing(b.Max.Y + margin)), 0, 0, 0);
        pl.AddVertexAt(3, new Point2d(cx.ToDrawing(b.Min.X - margin), cx.ToDrawing(b.Max.Y + margin)), 0, 0, 0);
        pl.Closed = true;
        return pl;
    }

    /// <summary>A revision cloud is a closed polyline whose every segment is an arc bowing outwards: the rectangle's sides cut into chords.</summary>
    private static Polyline RevisionCloud(EditContext cx, Box b, double radiusMm)
    {
        var margin = Math.Max(b.Width, b.Height) * MarginFactor;
        var chord = Math.Max(radiusMm * CloudChordFactor, 1);
        Pt[] corners = [new(b.Min.X - margin, b.Min.Y - margin), new(b.Max.X + margin, b.Min.Y - margin), new(b.Max.X + margin, b.Max.Y + margin), new(b.Min.X - margin, b.Max.Y + margin)];
        var pl = new Polyline();
        var n = 0;
        for (var side = 0; side < 4; side++)
        {
            var a = corners[side];
            var c = corners[(side + 1) % 4];
            var steps = Math.Max(1, (int)Math.Ceiling(a.DistanceXY(c) / chord));
            for (var k = 0; k < steps; k++)
            {
                var t = (double)k / steps;
                pl.AddVertexAt(n++, new Point2d(cx.ToDrawing(a.X + (c.X - a.X) * t), cx.ToDrawing(a.Y + (c.Y - a.Y) * t)), CloudBulge, 0, 0);
            }
        }

        pl.Closed = true;
        return pl;
    }
}
