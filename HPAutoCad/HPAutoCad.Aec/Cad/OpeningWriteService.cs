using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Coordination;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     <c>aec_create_opening_requests</c>: one rectangle (the opening, turned along the host) and one multileader carrying the
///     request's label per planned opening, on a request layer created on demand. The route and the host are never touched — a
///     request is a proposal the structural engineer answers. Two-phase: layer, space and every plan checked before the first write.
///     A preview lists the first <see cref="OpeningPlanner.MaxRequests"/> of a longer plan and says so; drawing a longer plan is refused.
/// </summary>
public static class OpeningWriteService
{
    public const string DefaultLayer = "HP-MCP-OPENINGS";
    public const double DefaultTextHeightMm = 150;
    private const short RequestColor = 30; // orange: a request, not a markup (magenta) nor a tag (white)

    /// <summary>The leader landing sits this many half-widths from the centre, up and to the right.</summary>
    public const double LeaderOffsetFactor = 1.5;

    public static EditResult Write(EditContext cx, CancellationToken ct, IReadOnlyList<OpeningRequest> requests, string? layerName, double textHeightMm, string? space, bool dryDecisionsOnly)
    {
        var result = new EditResult();
        if (requests.Count > OpeningPlanner.MaxRequests && !dryDecisionsOnly) throw new ArgumentException($"{requests.Count} opening requests planned; the maximum per call is {OpeningPlanner.MaxRequests} — narrow routes or hosts (apply: false lists the plan).");
        var layer = string.IsNullOrWhiteSpace(layerName) ? DefaultLayer : layerName.Trim();
        var spaceId = cx.SpaceId(space, out var spaceError);
        if (spaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        if (textHeightMm <= 0) throw new ArgumentException("textHeightMm must be > 0.");
        if (cx.CheckAnnotationLayer(layer, "opening request", out var layerWarning) is { } layerError) return EditResult.Refused(layerError);
        if (layerWarning is not null) result.Warn(layerWarning);

        if (dryDecisionsOnly)
        {
            var listed = requests.Take(OpeningPlanner.MaxRequests).ToArray();
            if (listed.Length < requests.Count) result.Warn($"{requests.Count} requests planned; the first {listed.Length} are listed and drawing needs narrower routes or hosts (max {OpeningPlanner.MaxRequests} per call).");
            result.Items = listed.Select((r, i) => new ItemOutcome(i, true, null, "LWPOLYLINE", [r.Label])).ToArray();
            result.Summary = new { requested = requests.Count, listed = listed.Length, truncated = listed.Length < requests.Count, drawn = 0, layer, layerCreated = false, requests = listed.Select(CoordinationService.Describe).ToArray() };
            return result;
        }

        var target = (BlockTableRecord)cx.Tr.GetObject(spaceId, OpenMode.ForWrite);
        var layerId = cx.EnsureLayer(layer, RequestColor, out var layerCreated);
        var colour = Color.FromColorIndex(ColorMethod.ByAci, RequestColor);
        var outcomes = new ItemOutcome[requests.Count];
        var written = new List<object>();
        for (var i = 0; i < requests.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var r = requests[i];
            var rectangle = Rectangle(cx, r);
            rectangle.SetDatabaseDefaults(cx.Db);
            rectangle.LayerId = layerId;
            rectangle.Color = colour;
            target.AppendEntity(rectangle);
            cx.Tr.AddNewlyCreatedDBObject(rectangle, true);
            var rectangleHandle = rectangle.Handle.ToString();
            result.Created(rectangleHandle);

            var half = Math.Max(r.WidthMm, r.HeightMm) / 2;
            var centre = new Point3d(cx.ToDrawing(r.CenterMm.X), cx.ToDrawing(r.CenterMm.Y), 0);
            var arrow = centre + new Vector3d(cx.ToDrawing(half * Math.Cos(Math.PI / 4)), cx.ToDrawing(half * Math.Sin(Math.PI / 4)), 0);
            var landing = centre + new Vector3d(cx.ToDrawing(half * LeaderOffsetFactor * 2), cx.ToDrawing(half * LeaderOffsetFactor * 2), 0);
            var leader = AnnotationService.BuildMLeader(cx, r.Label, arrow, landing, textHeightMm);
            leader.LayerId = layerId;
            leader.Color = colour;
            target.AppendEntity(leader);
            cx.Tr.AddNewlyCreatedDBObject(leader, true);
            var leaderHandle = leader.Handle.ToString();
            result.Created(leaderHandle);

            outcomes[i] = new ItemOutcome(i, true, rectangleHandle, "LWPOLYLINE", [r.Id, "leader:" + leaderHandle, "route:" + r.RouteHandle, "host:" + r.HostHandle]);
            written.Add(new { id = r.Id, rectangleHandle, leaderHandle, route = r.RouteHandle, host = r.HostHandle, centerMm = r.CenterMm, widthMm = r.WidthMm, heightMm = r.HeightMm, angleDeg = r.AngleDeg });
        }

        result.Settle(outcomes);
        result.Summary = new { requested = requests.Count, drawn = requests.Count, layer, layerCreated, written };
        return result;
    }

    /// <summary>The opening as a closed polyline centred on the crossing, its width along the host's direction.</summary>
    private static Polyline Rectangle(EditContext cx, OpeningRequest r)
    {
        var a = r.AngleDeg * Math.PI / 180;
        var (ux, uy) = (Math.Cos(a), Math.Sin(a));
        var (hw, hh) = (r.WidthMm / 2, r.HeightMm / 2);
        var pl = new Polyline(4);
        (double X, double Y)[] corners = [(-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)];
        for (var k = 0; k < 4; k++)
        {
            var (x, y) = corners[k];
            pl.AddVertexAt(k, new Point2d(cx.ToDrawing(r.CenterMm.X + x * ux - y * uy), cx.ToDrawing(r.CenterMm.Y + x * uy + y * ux)), 0, 0, 0);
        }

        pl.Closed = true;
        return pl;
    }
}
