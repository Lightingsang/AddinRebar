"""Live verification of the AEC tools (phase A) against a running AutoCAD 2026, over stdio, in one session:
  S  scene: layers + a small structural/architectural/MEP plan with deliberate defects, drawn through execute_autocad_code
  C  get_drawing_context
  Q  query_entities: type/layer filters, paging, detail mode, property selector, handles, text, unknown key warning
  R  query_entities_spatial: crosses, within, nearest, distance_to, touches, tolerance override, bad relation
  M  measure_geometry: length, totalLength, area, distance (points / entities / point-entity), intersections, angle, boundingBox, closestPoint, centroid
  I  detect_geometry_issues: duplicate, near_duplicate, overlapping_segments, endpoint_gap, open_polyline, self_intersection, zero_length, restricted types
  B  classify_aec_entities (default rules on the scene: columns, beams, walls, pipe, door block; unknowns) + get_entity_relationships (connected with gap,
     intersect, self-set parallel with de-duplication, empty source refused)
  E  error paths: invalid handle -> errors[] with INVALID_HANDLE, erased handle, unknown measure -> isError (ArgumentException)
  P  performance: 3 000 extra lines on PERF-* layers; query_entities with a layer filter < 1 s, spatial nearest and issue detection timed
The PowerShell wrapper (run-aec-tools-live.ps1) starts AutoCAD, ticks the opt-in and calls this. Prints PASS/FAIL lines and a JSON summary.
"""
import argparse, json, os, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, ok, short, utf8_console  # noqa: E402

utf8_console()
CL = Checklist()
check, save = CL.check, CL.save

SCENE = r'''
double Du(double mm) => units.ToDrawing(mm);
Point3d P(double x, double y) => new Point3d(Du(x), Du(y), 0);
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
ObjectId Layer(string name, short color)
{
    if (lt.Has(name)) return lt[name];
    var rec = new LayerTableRecord { Name = name, Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, color) };
    var id = lt.Add(rec); tr.AddNewlyCreatedDBObject(rec, true); return id;
}
var col = Layer("S-COL", 1); var beam = Layer("S-BEAM", 2); var wall = Layer("A-WALL", 3); var pipe = Layer("M-PIPE", 4); var txt = Layer("A-TEXT", 7);
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
var handles = new Dictionary<string, string>();
string Add(string key, Entity e, ObjectId layer)
{
    e.LayerId = layer; ms.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); handles[key] = e.Handle.ToString(); return handles[key];
}
Polyline Rect(double x, double y, double w, double h)
{
    var pl = new Polyline(4);
    pl.AddVertexAt(0, new Point2d(Du(x), Du(y)), 0, 0, 0); pl.AddVertexAt(1, new Point2d(Du(x + w), Du(y)), 0, 0, 0);
    pl.AddVertexAt(2, new Point2d(Du(x + w), Du(y + h)), 0, 0, 0); pl.AddVertexAt(3, new Point2d(Du(x), Du(y + h)), 0, 0, 0);
    pl.Closed = true; return pl;
}
// columns 400x400 at (0,0) (6000,0) (0,5000) (6000,5000), centred
Add("c1", Rect(-200, -200, 400, 400), col); Add("c2", Rect(5800, -200, 400, 400), col);
Add("c3", Rect(-200, 4800, 400, 400), col); Add("c4", Rect(5800, 4800, 400, 400), col);
// beams: b1 spans c1-c2 exactly (touches both), b2 spans c3-c4 but stops 7 mm short of c4 (gap), b3 = duplicate of b1
Add("b1", new Line(P(200, 0), P(5800, 0)), beam);
Add("b2", new Line(P(200, 5000), P(5793, 5000)), beam);
Add("b3", new Line(P(5800, 0), P(200, 0)), beam);
// a closed room outline, a second outline that is 12 mm short of closing, two collinear overlapping wall lines,
// and two wall lines whose ends miss each other by 7 mm
Add("room", Rect(1000, 1000, 4000, 3000), wall);
var openRoom = new Polyline(5);
openRoom.AddVertexAt(0, new Point2d(Du(13000), Du(1000)), 0, 0, 0); openRoom.AddVertexAt(1, new Point2d(Du(17000), Du(1000)), 0, 0, 0);
openRoom.AddVertexAt(2, new Point2d(Du(17000), Du(4000)), 0, 0, 0); openRoom.AddVertexAt(3, new Point2d(Du(13000), Du(4000)), 0, 0, 0);
openRoom.AddVertexAt(4, new Point2d(Du(13000), Du(1012)), 0, 0, 0);
Add("openroom", openRoom, wall);
Add("w1", new Line(P(7000, 0), P(10000, 0)), wall); Add("w2", new Line(P(9000, 0), P(12000, 0)), wall);
Add("g1", new Line(P(13000, 6000), P(15000, 6000)), wall); Add("g2", new Line(P(15007, 6000), P(15007, 8000)), wall);
// bow-tie polyline (self-intersection) and a zero-length line
var bow = new Polyline(4);
bow.AddVertexAt(0, new Point2d(Du(7000), Du(2000)), 0, 0, 0); bow.AddVertexAt(1, new Point2d(Du(8000), Du(3000)), 0, 0, 0);
bow.AddVertexAt(2, new Point2d(Du(8000), Du(2000)), 0, 0, 0); bow.AddVertexAt(3, new Point2d(Du(7000), Du(3000)), 0, 0, 0);
bow.Closed = true; Add("bow", bow, wall);
Add("zero", new Line(P(7500, 4000), P(7500, 4000.1)), wall);
// pipe polyline crossing beam b1 and passing through the room; a circle (equipment) inside the room; text
var pl = new Polyline(3);
pl.AddVertexAt(0, new Point2d(Du(3000), Du(-1500)), 0, 0, 0); pl.AddVertexAt(1, new Point2d(Du(3000), Du(2500)), 0, 0, 0); pl.AddVertexAt(2, new Point2d(Du(4500), Du(2500)), 0, 0, 0);
Add("pipe", pl, pipe);
Add("circle", new Circle(P(2000, 2500), Vector3d.ZAxis, Du(300)), pipe);
Add("text", new DBText { Position = P(1500, 3500), Height = Du(200), TextString = "OFFICE 01" }, txt);
Add("arc", new Arc(P(9000, 4000), Du(1000), 0, Math.PI / 2), wall);
// the same quarter arc drawn with a mirrored OCS (normal -Z): in WCS it sits in the upper-LEFT quadrant of its centre
Add("arcneg", new Arc(P(20000, 4000), -Vector3d.ZAxis, Du(1000), 0, Math.PI / 2), wall);
// a two-vertex polyline whose only segment is a semicircle (bulge 1): length pi*1000, top at y = 2000
var bulge = new Polyline(2);
bulge.AddVertexAt(0, new Point2d(Du(24000), Du(1000)), 1, 0, 0); bulge.AddVertexAt(1, new Point2d(Du(26000), Du(1000)), 0, 0, 0);
Add("bulgepl", bulge, wall);
// a closed rectangle exported with Closed = true AND a repeated closing vertex (5 vertices): readers must see 4
var dup = new Polyline(5);
dup.AddVertexAt(0, new Point2d(Du(24000), Du(7000)), 0, 0, 0); dup.AddVertexAt(1, new Point2d(Du(26000), Du(7000)), 0, 0, 0);
dup.AddVertexAt(2, new Point2d(Du(26000), Du(8000)), 0, 0, 0); dup.AddVertexAt(3, new Point2d(Du(24000), Du(8000)), 0, 0, 0);
dup.AddVertexAt(4, new Point2d(Du(24000), Du(7000)), 0, 0, 0); dup.Closed = true;
Add("dupclose", dup, wall);
// a solid hatch over a 2000 x 1000 rectangle
var hatch = new Hatch(); hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
Add("hatch", hatch, wall);
hatch.AppendLoop(HatchLoopTypes.Outermost, new Point2dCollection { new Point2d(Du(24000), Du(4000)), new Point2d(Du(26000), Du(4000)), new Point2d(Du(26000), Du(5000)), new Point2d(Du(24000), Du(5000)) }, new DoubleCollection { 0, 0, 0, 0 });
hatch.EvaluateHatch(true);
// a block definition with one attribute, inserted once with MARK = D01
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
var def = new BlockTableRecord { Name = "DOOR-TEST" };
var defId = bt.Add(def); tr.AddNewlyCreatedDBObject(def, true);
var doorLine = new Line(new Point3d(0, 0, 0), new Point3d(Du(900), 0, 0)); def.AppendEntity(doorLine); tr.AddNewlyCreatedDBObject(doorLine, true);
var attDef = new AttributeDefinition(new Point3d(0, Du(100), 0), "D00", "MARK", "Door mark", db.Textstyle) { Height = Du(150) };
def.AppendEntity(attDef); tr.AddNewlyCreatedDBObject(attDef, true);
var br = new BlockReference(P(28000, 1000), defId);
Add("door", br, txt);
var attRef = new AttributeReference(); attRef.SetAttributeFromBlock(attDef, br.BlockTransform); attRef.TextString = "D01";
br.AttributeCollection.AppendAttribute(attRef); tr.AddNewlyCreatedDBObject(attRef, true);
return handles;
'''

PERF = r'''
double Du(double mm) => units.ToDrawing(mm);
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
ObjectId Layer(string name)
{
    if (lt.Has(name)) return lt[name];
    var rec = new LayerTableRecord { Name = name }; var id = lt.Add(rec); tr.AddNewlyCreatedDBObject(rec, true); return id;
}
var layers = new[] { Layer("PERF-A"), Layer("PERF-B"), Layer("PERF-C") };
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
var n = 0;
for (var i = 0; i < 60; i++)
for (var j = 0; j < 50; j++)
{
    ct.ThrowIfCancellationRequested();
    var x = 30000 + i * 500.0; var y = 30000 + j * 500.0;
    var line = new Line(new Point3d(Du(x), Du(y), 0), new Point3d(Du(x + 400), Du(y + (j % 2 == 0 ? 0 : 300)), 0)) { LayerId = layers[n % 3] };
    ms.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true); n++;
}
return n;
'''


def value(r):
    return r.get("value") if isinstance(r, dict) else None


AEC_RESULTS = []


def near(a, b, eps=0.05):
    return a is not None and b is not None and abs(a - b) <= eps


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", required=True)
    ap.add_argument("--out", default="")
    a = ap.parse_args()
    CL.out_dir = a.out or None

    s = Server(a.exe, name="autocad-aec")
    raw_tool = s.tool

    def tool(name, args=None, timeout=180.0):
        r = raw_tool(name, args, timeout=timeout)
        if name != "execute_autocad_code":
            AEC_RESULTS.append((name, r))
        return r

    s.tool = tool
    try:
        init = s.initialize()
        check("initialize", init.get("serverInfo", {}).get("name") == "HPAutoCad MCP", short(init.get("serverInfo")))
        names = s.tools()
        aec = ["get_drawing_context", "query_entities", "query_entities_spatial", "measure_geometry", "detect_geometry_issues"]
        check("tools/list holds the 5 AEC seeds (+ the 24 others)", all(n in names for n in aec) and len(names) >= 29, f"{len(names)} tools")

        # ---- S: scene -------------------------------------------------------------------------------------------------
        scene = s.tool("execute_autocad_code", {"code": SCENE, "transaction": "auto", "label": "aec scene", "timeoutSeconds": 60}, timeout=120)
        h = value(scene) or {}
        check("S scene drawn (24 entities, 5 layers)", not scene.get("isError") and len(h) == 24 and (scene.get("changed") or {}).get("added", 0) >= 24, f"handles={len(h)} changed={short(scene.get('changed'))} {short(scene.get('message'))}")
        save("scene-handles", h)
        if not h:
            return CL.finish("aec-tools-live")

        # ---- C: drawing context ---------------------------------------------------------------------------------------
        ctx = s.tool("get_drawing_context", {"includeLayouts": True, "includeLayers": True})
        v = value(ctx) or {}
        counts = v.get("counts") or {}
        check("C context: units, layout, counts, ucs, extents", not ctx.get("isError") and v.get("units", {}).get("mmPerUnit") and v.get("activeLayout") and counts.get("modelSpaceEntities", 0) >= 24 and counts.get("layers", 0) >= 6 and v.get("ucs", {}).get("isWorld") is not None,
              f"units={short(v.get('units'), 80)} layout={v.get('activeLayout')} entities={counts.get('modelSpaceEntities')} layers={counts.get('layers')}")
        check("C context lists layouts and the S-COL layer", any(l.get("name") == "Model" for l in (v.get("layouts") or [])) and any(l.get("name") == "S-COL" and l.get("color") == "1" for l in (v.get("layers") or [])), short(v.get("layouts")))
        save("context", v)

        # ---- Q: query_entities ----------------------------------------------------------------------------------------
        q = value(s.tool("query_entities", {"filter": {"types": ["LWPOLYLINE"], "layers": ["S-COL"]}})) or {}
        check("Q columns by type + layer: 4 records with bounds", q.get("success") and q.get("count") == 4 and len(q.get("items", [])) == 4 and all(i.get("boundsMm") for i in q["items"]), short(q.get("summary")))
        paged = value(s.tool("query_entities", {"filter": {"layers": ["S-*"]}, "limit": 3, "offset": 3})) or {}
        check("Q paging: layers S-* limit 3 offset 3 -> count 7, 3 items, truncated", paged.get("count") == 7 and len(paged.get("items", [])) == 3 and paged.get("truncated") is True and paged.get("offset") == 3, short(paged.get("summary")))
        detail = value(s.tool("query_entities", {"filter": {"handles": [h["c1"]]}, "mode": "detail"})) or {}
        item = (detail.get("items") or [{}])[0]
        geom = item.get("geometry") or {}
        check("Q detail mode: closed 4-vertex geometry, area 160000 mm2, color/linetype present", geom.get("closed") is True and len(geom.get("vertices", [])) == 4 and abs(geom.get("areaMm2", 0) - 160000) < 1 and item.get("color") and item.get("linetype"), short(item, 300))
        props = value(s.tool("query_entities", {"filter": {"types": ["TEXT"], "textContains": "office"}, "properties": ["text", "position", "bogus"]})) or {}
        pi = (props.get("items") or [{}])[0]
        check("Q textContains + property selector + unknown property warning", props.get("count") == 1 and pi.get("text") == "OFFICE 01" and pi.get("positionMm") and "boundsMm" not in pi and any("bogus" in w for w in props.get("warnings", [])), short(props))
        unknown = value(s.tool("query_entities", {"filter": {"layer": "M-PIPE", "colour": "4"}})) or {}
        check("Q singular key + unknown filter key -> warning, 2 pipe entities", unknown.get("count") == 2 and any("colour" in w for w in unknown.get("warnings", [])), short(unknown.get("warnings")))
        save("query", {"columns": q, "paged": paged, "detail": detail, "props": props})

        # ---- R: spatial -----------------------------------------------------------------------------------------------
        crosses = value(s.tool("query_entities_spatial", {"source": {"layers": ["M-PIPE"]}, "target": {"layers": ["S-BEAM"]}, "relation": "crosses"})) or {}
        cm = crosses.get("items") or []
        check("R crosses: pipe x beam b1 (and its duplicate b3) at (3000,0)", crosses.get("success") and len(cm) == 2 and all(m["sourceHandle"] == h["pipe"] for m in cm) and {m["targetHandle"] for m in cm} == {h["b1"], h["b3"]} and abs(cm[0]["pointsMm"][0]["x"] - 3000) < 1 and abs(cm[0]["pointsMm"][0]["y"]) < 1, short(cm))
        within = value(s.tool("query_entities_spatial", {"source": {"types": ["CIRCLE", "TEXT"]}, "target": {"handles": [h["room"]]}, "relation": "within"})) or {}
        check("R within: circle + text inside the room outline", within.get("count") == 2 and {m["sourceHandle"] for m in within.get("items", [])} == {h["circle"], h["text"]}, short(within.get("summary")))
        nearest = value(s.tool("query_entities_spatial", {"source": {"layers": ["S-BEAM"]}, "target": {"layers": ["S-COL"]}, "relation": "nearest"})) or {}
        nm = {m["sourceHandle"]: m for m in nearest.get("items", [])}
        check("R nearest: one column per beam, b2 -> a column at distance 0 (touches c3)", nearest.get("count") == 3 and len(nm) == 3 and nm[h["b2"]]["distanceMm"] == 0 and nm[h["b1"]]["targetHandle"] in (h["c1"], h["c2"]), short(nearest.get("items")))
        dist = value(s.tool("query_entities_spatial", {"source": {"handles": [h["b2"]]}, "target": {"layers": ["S-COL"]}, "relation": "distance_to", "maxDistance": 50})) or {}
        dm = {m["targetHandle"]: m["distanceMm"] for m in dist.get("items", [])}
        check("R distance_to 50 mm from b2: c3 at 0 and c4 at 7", dist.get("count") == 2 and dm.get(h["c3"]) == 0 and abs(dm.get(h["c4"], -1) - 7) < 0.01, short(dm))
        touches = value(s.tool("query_entities_spatial", {"source": {"handles": [h["b2"]]}, "target": {"handles": [h["c4"]]}, "relation": "touches"})) or {}
        touches_tight = value(s.tool("query_entities_spatial", {"source": {"handles": [h["b2"]]}, "target": {"handles": [h["c4"]]}, "relation": "touches", "tolerance": {"endpointConnection": 5}})) or {}
        check("R touches: b2 touches c4 with the default 10 mm, not with 5 mm", touches.get("count") == 1 and touches_tight.get("count") == 0, f"default={touches.get('count')} tight={touches_tight.get('count')}")
        bad = s.tool("query_entities_spatial", {"source": {"layers": ["S-BEAM"]}, "target": {"layers": ["S-COL"]}, "relation": "near"})
        check("R unknown relation -> isError naming the relations (ArgumentException)", bad.get("isError") and "relation must be one of" in (bad.get("message") or ""), short(bad.get("message")))
        save("spatial", {"crosses": crosses, "within": within, "nearest": nearest, "distance": dist, "touches": touches})

        # ---- M: measure -----------------------------------------------------------------------------------------------
        length = value(s.tool("measure_geometry", {"measure": "totalLength", "handles": [h["b1"], h["b2"], h["arc"]]})) or {}
        expected = 5600 + 5593 + 1000 * 3.141592653589793 / 2
        check("M totalLength of b1 + b2 + quarter arc (exact arc length)", abs((length.get("summary") or {}).get("totalLengthMm", 0) - expected) < 0.05 and len(length.get("items", [])) == 3, short(length.get("summary")))
        area = value(s.tool("measure_geometry", {"measure": "area", "handles": [h["c1"], h["circle"]]})) or {}
        ai = {i["handle"]: i["areaMm2"] for i in area.get("items", [])}
        check("M area: column 160000 mm2, circle pi*300^2", abs(ai.get(h["c1"], 0) - 160000) < 0.5 and abs(ai.get(h["circle"], 0) - 3.141592653589793 * 90000) < 1, short(area.get("summary")))
        room_area = value(s.tool("measure_geometry", {"measure": "area", "handles": [h["openroom"]]})) or {}
        check("M area of the open room outline -> NOT_CLOSED error, success false", room_area.get("success") is False and any(e.get("code") == "NOT_CLOSED" for e in room_area.get("errors", [])), short(room_area.get("errors")))
        dpp = value(s.tool("measure_geometry", {"measure": "distance", "points": [{"x": 0, "y": 0}, {"x": 3000, "y": 4000}]})) or {}
        dee = value(s.tool("measure_geometry", {"measure": "distance", "handles": [h["c1"], h["c2"]]})) or {}
        dpe = value(s.tool("measure_geometry", {"measure": "distance", "handles": [h["b1"]], "points": [{"x": 3000, "y": 700}]})) or {}
        check("M distance: points 5000, columns 5600, point-to-beam 700 with closest point (3000,0)",
              near((dpp.get("summary") or {}).get("distanceMm"), 5000) and near((dee.get("summary") or {}).get("distanceMm"), 5600) and near((dpe.get("summary") or {}).get("distanceMm"), 700) and near(((dpe.get("summary") or {}).get("closestPointMm") or {}).get("x"), 3000),
              f"{short(dpp.get('summary'), 100)} {short(dee.get('summary'), 100)} {short(dpe.get('summary'), 140)}")
        inter = value(s.tool("measure_geometry", {"measure": "intersections", "handles": [h["pipe"], h["b1"]]})) or {}
        ip = (inter.get("items") or [{}])[0].get("pointMm") or {}
        check("M intersections pipe x b1: exactly one, at (3000,0), exact curve maths", (inter.get("summary") or {}).get("intersections") == 1 and inter["summary"].get("exact") is True and abs(ip.get("x", 0) - 3000) < 0.001 and abs(ip.get("y", 1)) < 0.001, short(inter))
        angle = value(s.tool("measure_geometry", {"measure": "angle", "handles": [h["b1"], h["pipe"]]})) or {}
        check("M angle: b1 direction 0 deg; b1 vs pipe (open chain end-to-end) reported", any(i.get("handle") == h["b1"] and i.get("directionDegrees") == 0 for i in angle.get("items", [])) and angle.get("summary", {}).get("angleBetweenDegrees") is not None, short(angle))
        bbox = value(s.tool("measure_geometry", {"measure": "boundingBox", "handles": [h["c1"], h["c4"]]})) or {}
        bb = (bbox.get("summary") or {}).get("boundsMm") or {}
        check("M boundingBox union of c1 + c4: (-200,-200)-(6200,5200)", near(bb.get("min", {}).get("x"), -200) and near(bb.get("max", {}).get("x"), 6200) and near(bb.get("max", {}).get("y"), 5200), short(bbox.get("summary")))
        closest = value(s.tool("measure_geometry", {"measure": "closestPoint", "handles": [h["circle"]], "points": [{"x": 2000, "y": 5000}]})) or {}
        cp = (closest.get("items") or [{}])[0]
        check("M closestPoint on the circle from above: (2000,2800), distance 2200", abs(cp.get("closestPointMm", {}).get("y", 0) - 2800) < 0.01 and abs(cp.get("distanceMm", 0) - 2200) < 0.01, short(cp))
        centroid = value(s.tool("measure_geometry", {"measure": "centroid", "handles": [h["c2"]]})) or {}
        cc = (centroid.get("items") or [{}])[0].get("centroidMm") or {}
        check("M centroid of c2: (6000,0)", abs(cc.get("x", 0) - 6000) < 0.01 and abs(cc.get("y", 1)) < 0.01, short(centroid))
        save("measure", {"length": length, "area": area, "distance": [dpp, dee, dpe], "intersections": inter, "angle": angle, "bbox": bbox, "closest": closest, "centroid": centroid})

        # ---- G: geometry that only reads right through AutoCAD's own maths -------------------------------------------------
        arcs = value(s.tool("measure_geometry", {"measure": "boundingBox", "handles": [h["arc"], h["arcneg"]]})) or {}
        ab = {i["handle"]: i["boundsMm"] for i in arcs.get("items", [])}
        pos = ab.get(h["arc"], {}); neg = ab.get(h["arcneg"], {})
        check("G mirrored arc (normal -Z) lies in the upper-left quadrant of its centre, the +Z one upper-right",
              near(pos.get("min", {}).get("x"), 9000) and near(pos.get("max", {}).get("x"), 10000) and near(neg.get("min", {}).get("x"), 19000) and near(neg.get("max", {}).get("x"), 20000) and near(neg.get("max", {}).get("y"), 5000), short(ab))
        arcneg_geom = value(s.tool("query_entities", {"filter": {"handles": [h["arcneg"]], "space": "all"}, "mode": "detail"})) or {}
        ag = ((arcneg_geom.get("items") or [{}])[0].get("geometry") or {})
        av = ag.get("vertices") or []
        check("G mirrored arc tessellation follows the true curve (every vertex 1000 mm from the centre, x <= centre)",
              len(av) >= 5 and all(near(((v["x"] - 20000) ** 2 + (v["y"] - 4000) ** 2) ** 0.5, 1000, 0.5) and v["x"] <= 20000 + 0.5 for v in av) and ag.get("approximate") is True, short(av, 200))
        bul = value(s.tool("measure_geometry", {"measure": "length", "handles": [h["bulgepl"]]})) or {}
        bulb = value(s.tool("measure_geometry", {"measure": "boundingBox", "handles": [h["bulgepl"]]})) or {}
        bb2 = (bulb.get("summary") or {}).get("boundsMm") or {}
        # bulge +1 from the left vertex sweeps counter-clockwise, i.e. below the chord: the semicircle bottoms out at y = 0
        check("G bulge polyline: exact length pi*1000, bounding box bottom at y = 0 (arc sampled, not chorded)",
              near((bul.get("summary") or {}).get("totalLengthMm"), 3141.59, 0.05) and near(bb2.get("min", {}).get("y"), 0, 0.5) and near(bb2.get("max", {}).get("y"), 1000, 0.5), f"{short(bul.get('summary'), 80)} {short(bb2, 120)}")
        hat = value(s.tool("measure_geometry", {"measure": "area", "handles": [h["hatch"]]})) or {}
        hatq = value(s.tool("query_entities", {"filter": {"types": ["HATCH"]}, "mode": "detail"})) or {}
        hg = ((hatq.get("items") or [{}])[0].get("geometry") or {})
        check("G hatch: area 2 000 000 mm2 from AutoCAD, footprint = the 4-vertex loop", near((hat.get("summary") or {}).get("totalAreaMm2"), 2_000_000, 1) and hg.get("closed") is True and hg.get("vertexCount") == 4, f"{short(hat.get('summary'), 80)} {short(hg, 160)}")
        door = value(s.tool("query_entities", {"filter": {"blockNames": ["DOOR-*"], "textContains": "d01"}, "properties": ["block", "attributes", "position"]})) or {}
        di = (door.get("items") or [{}])[0]
        check("G block by wildcard name + attribute text: DOOR-TEST with MARK = D01", door.get("count") == 1 and di.get("blockName") == "DOOR-TEST" and (di.get("attributes") or {}).get("MARK") == "D01" and near((di.get("positionMm") or {}).get("x"), 28000), short(door))
        dupq = value(s.tool("query_entities", {"filter": {"handles": [h["dupclose"]]}, "mode": "detail"})) or {}
        dg = ((dupq.get("items") or [{}])[0].get("geometry") or {})
        check("G closed polyline with a repeated closing vertex reads as a 4-vertex ring (area 2 000 000 mm2)", dg.get("closed") is True and dg.get("vertexCount") == 4 and near(dg.get("areaMm2"), 2_000_000, 1), short(dg, 200))
        save("geometry-special", {"arcs": arcs, "arcneg": arcneg_geom, "bulge": [bul, bulb], "hatch": [hat, hatq], "door": door, "dupclose": dupq})

        # ---- I: geometry issues -----------------------------------------------------------------------------------------
        issues = value(s.tool("detect_geometry_issues", {"filter": {"layers": ["S-*", "A-WALL"]}})) or {}
        by_type = {}
        for i in issues.get("items", []):
            by_type.setdefault(i["type"], []).append(i)
        check("I issues found: duplicate b1/b3, endpoint_gap g1/g2 7 mm, overlapping w1/w2, open_polyline 12 mm, self_intersection bow, zero_length",
              issues.get("success") and {h["b1"], h["b3"]} == set(by_type.get("duplicate", [{}])[0].get("handles", []))
              and any(abs(i.get("valueMm", 0) - 7) < 0.01 and {h["g1"], h["g2"]} == set(i["handles"]) for i in by_type.get("endpoint_gap", []))
              and any({h["w1"], h["w2"]} == set(i["handles"]) for i in by_type.get("overlapping_segments", []))
              and any(i["handles"] == [h["openroom"]] and abs(i.get("valueMm", 0) - 12) < 0.01 for i in by_type.get("open_polyline", []))
              and any(i["handles"] == [h["bow"]] for i in by_type.get("self_intersection", []))
              and any(i["handles"] == [h["zero"]] for i in by_type.get("zero_length", [])),
              short({k: len(v) for k, v in by_type.items()}))
        first = (issues.get("items") or [{}])[0]
        check("I every issue has id, severity, location, description, suggested action", first.get("issueId", "").startswith("GEO-") and first.get("severity") in ("critical", "warning", "info") and first.get("locationMm") and first.get("description") and first.get("suggestedAction"), short(first))
        restricted = value(s.tool("detect_geometry_issues", {"handles": [h["b1"], h["b3"], h["w1"], h["w2"]], "issueTypes": ["endpoint_gap"]})) or {}
        check("I restricted to endpoint_gap on connected/duplicate pairs -> no issues, summary lists the checked type", restricted.get("count") == 0 and (restricted.get("summary") or {}).get("checkedTypes") == ["endpoint_gap"], short(restricted.get("summary")))
        tight = value(s.tool("detect_geometry_issues", {"handles": [h["g1"], h["g2"]], "issueTypes": ["endpoint_gap"], "tolerance": {"endpointConnection": 5}})) or {}
        check("I tolerance override 5 mm hides the 7 mm gap", tight.get("count") == 0 and (tight.get("summary") or {}).get("tolerance", {}).get("endpointConnection") == 5, short(tight.get("summary")))
        save("issues", {"all": issues, "restricted": restricted, "tight": tight})

        # ---- B: semantic ----------------------------------------------------------------------------------------------------
        aec = ["get_drawing_context", "query_entities", "query_entities_spatial", "measure_geometry", "detect_geometry_issues", "classify_aec_entities", "get_entity_relationships"]
        check("B tools/list holds the 7 AEC seeds", all(n in s.tools() for n in aec), "")
        cls = value(s.tool("classify_aec_entities", {"filter": {"layers": ["S-*", "A-*", "M-*"]}, "includeUnknown": True, "minConfidence": 0})) or {}
        by = {o["handle"]: o for o in cls.get("items", [])}
        summary = (cls.get("summary") or {}).get("byAecType") or {}
        check("B classify: 4 columns, 3 beams, 1 pipe, 1 door block, walls; circle/text/hatch unknown",
              cls.get("success") and summary.get("StructuralColumn") == 4 and summary.get("StructuralBeam") == 3 and summary.get("Pipe") == 1 and summary.get("Door") == 1 and summary.get("ArchitecturalWall", 0) >= 8
              and by.get(h["circle"], {}).get("aecType") == "Unknown" and by.get(h["text"], {}).get("aecType") == "Unknown", short(summary))
        c1 = by.get(h["c1"]) or {}
        check("B column object: confidence >= 0.9, evidence names the layer and the 400x400 footprint, properties width/depth/area/centroid",
              c1.get("aecType") == "StructuralColumn" and c1.get("confidence", 0) >= 0.9 and any("S-COL" in e for e in c1.get("evidence", [])) and near((c1.get("properties") or {}).get("widthMm"), 400) and near((c1.get("properties") or {}).get("areaMm2"), 160000) and (c1.get("properties") or {}).get("centroidMm"), short(c1, 400))
        door = by.get(h["door"]) or {}
        check("B door block: Door 0.9 via block name, attributes carried", door.get("aecType") == "Door" and near(door.get("confidence"), 0.9) and ((door.get("properties") or {}).get("attributes") or {}).get("MARK") == "D01", short(door, 300))
        structural = value(s.tool("classify_aec_entities", {"filter": {"layers": ["S-*", "A-*", "M-*"]}, "disciplines": ["Structural"], "limit": 5, "offset": 5})) or {}
        check("B disciplines=Structural + paging: count 7, page 2 has 2 items, ruleSet reported", structural.get("count") == 7 and len(structural.get("items", [])) == 2 and (structural.get("summary") or {}).get("ruleSet", {}).get("rules", 0) >= 20, short(structural.get("summary"), 300))
        rel = value(s.tool("get_entity_relationships", {"filter": {"layers": ["S-BEAM"]}, "target": {"layers": ["S-COL"]}, "relations": ["connected"]})) or {}
        pairs = {(r["source"], r["target"]): r for r in rel.get("items", [])}
        b2c4 = pairs.get((h["b2"], h["c4"])) or {}
        check("B connected: 6 beam-column connections, b2->c4 with a 7 mm gap and lower confidence, ends named by AEC type",
              rel.get("count") == 6 and (h["b1"], h["c1"]) in pairs and (h["b1"], h["c2"]) in pairs and near(b2c4.get("valueMm"), 7) and b2c4.get("confidence", 1) < 1 and b2c4.get("sourceAecType") == "StructuralBeam" and b2c4.get("targetAecType") == "StructuralColumn", short(rel.get("items"), 400))
        inter = value(s.tool("get_entity_relationships", {"handles": [h["pipe"]], "target": {"layers": ["S-BEAM"]}, "relations": ["intersect"]})) or {}
        check("B intersect: pipe x b1 and its duplicate, location (3000,0)", inter.get("count") == 2 and all(near((r.get("locationMm") or {}).get("x"), 3000) for r in inter.get("items", [])), short(inter.get("items"), 300))
        par = value(s.tool("get_entity_relationships", {"filter": {"layers": ["S-COL"]}, "relations": ["parallel"]})) or {}
        check("B self-set parallel: 4 columns -> 6 de-duplicated pairs, sameSet, angleDeg 0 and no valueMm", par.get("count") == 6 and (par.get("summary") or {}).get("sameSet") is True and all(r["source"] < r["target"] for r in par.get("items", []))
              and all(near(r.get("angleDeg"), 0) and r.get("valueMm") is None for r in par.get("items", [])), short(par.get("summary"), 200))
        capped = value(s.tool("get_entity_relationships", {"filter": {"layers": ["S-COL"]}, "relations": ["parallel"], "limit": 2})) or {}
        check("B relationships limit 2: count stays 6 (the total), 2 items, truncated", capped.get("count") == 6 and len(capped.get("items", [])) == 2 and capped.get("truncated") is True, short({k: capped.get(k) for k in ("count", "truncated")}))
        big = value(s.tool("classify_aec_entities", {"filter": {"layers": ["S-*", "A-*", "M-*"]}, "limit": 100})) or {}
        check("B classify limit 100 -> warned, capped at 50 per page", len(big.get("items", [])) <= 50 and any("capped at 50" in w for w in big.get("warnings", [])), short(big.get("warnings")))
        every = value(s.tool("get_entity_relationships", {"filter": {"layers": ["S-*", "A-WALL"]}, "relations": ["intersect", "connected", "near", "touching", "inside", "contains"], "maxDistance": 500})) or {}
        origin = [r for r in every.get("items", []) if near((r.get("locationMm") or {}).get("x"), 0, 1) and near((r.get("locationMm") or {}).get("y"), 0, 1)]
        check("B every proximity relationship carries a real location (none at the origin)", every.get("count", 0) >= 10 and all(r.get("locationMm") for r in every.get("items", [])) and not origin, f"{every.get('count')} relationships, at origin: {len(origin)}")
        badrel = s.tool("get_entity_relationships", {"relations": ["connected"]})
        check("B empty source -> ArgumentException", badrel.get("isError") and "source" in (badrel.get("message") or ""), short(badrel.get("message")))
        save("semantic", {"classify": cls, "structural": structural, "connected": rel, "intersect": inter, "parallel": par, "capped": capped, "big": big, "every": every})

        # ---- E: error paths ---------------------------------------------------------------------------------------------
        badh = value(s.tool("query_entities", {"filter": {"handles": ["ZZZZZZ", "nothex", h["c1"]]}})) or {}
        codes = [e.get("code") for e in badh.get("errors", [])]
        check("E invalid handles -> errors[] with INVALID_HANDLE, the valid one still returned", badh.get("count") == 1 and codes.count("INVALID_HANDLE") == 2 and all(e.get("handle") for e in badh["errors"]), short(badh.get("errors")))
        erased = s.tool("execute_autocad_code", {"code": f"var id = db.GetObjectId(false, new Handle(0x{h['zero']}), 0); var e = (Entity)tr.GetObject(id, OpenMode.ForWrite); e.Erase(); return id.Handle.ToString();", "transaction": "auto", "label": "erase zero"}, timeout=60)
        gone = value(s.tool("measure_geometry", {"measure": "length", "handles": [h["zero"]]})) or {}
        check("E erased entity -> ERASED error code", not erased.get("isError") and any(e.get("code") == "ERASED" for e in gone.get("errors", [])), short(gone.get("errors")))
        badm = s.tool("measure_geometry", {"measure": "volume", "handles": [h["c1"]]})
        check("E unknown measure -> isError listing the measures", badm.get("isError") and "measure must be one of" in (badm.get("message") or ""), short(badm.get("message")))
        dirty = [(n, r.get("changed")) for n, r in AEC_RESULTS if any((r.get("changed") or {}).get(k) for k in ("added", "modified", "deleted"))]
        check("E read-only: no AEC call changed anything (every result reports changed 0/0/0)", len(AEC_RESULTS) >= 30 and not dirty, f"{len(AEC_RESULTS)} calls, dirty={dirty[:3]}")

        # ---- P: performance on a 3 000-line grid ---------------------------------------------------------------------------
        perf = s.tool("execute_autocad_code", {"code": PERF, "transaction": "auto", "label": "perf grid", "timeoutSeconds": 120}, timeout=180)
        check("P 3000 lines drawn on PERF-A/B/C", not perf.get("isError") and value(perf) == 3000, short(perf.get("changed") or perf.get("message")))
        t0 = time.perf_counter()
        pq = s.tool("query_entities", {"filter": {"layers": ["PERF-B"]}, "limit": 100})
        q_ms = (time.perf_counter() - t0) * 1000
        pv = value(pq) or {}
        check("P query_entities layer filter over 3 019 entities: 1000 matched, 100 returned, < 1 s end to end", pv.get("count") == 1000 and len(pv.get("items", [])) == 100 and q_ms < 1000, f"{q_ms:.0f} ms (bridge {pq.get('durationMs')} ms)")
        t0 = time.perf_counter()
        pn = s.tool("query_entities_spatial", {"source": {"layers": ["PERF-A"]}, "target": {"layers": ["PERF-B"]}, "relation": "nearest", "limit": 5, "maxCandidates": 5000}, timeout=120)
        n_ms = (time.perf_counter() - t0) * 1000
        nv = value(pn) or {}
        check("P spatial nearest 1000 x 1000 lines through the grid index: 1000 matches, < 5 s", nv.get("count") == 1000 and n_ms < 5000, f"{n_ms:.0f} ms (bridge {pn.get('durationMs')} ms)")
        t0 = time.perf_counter()
        pi = s.tool("detect_geometry_issues", {"filter": {"layers": ["PERF-*"]}, "limit": 50, "maxCandidates": 5000}, timeout=120)
        i_ms = (time.perf_counter() - t0) * 1000
        iv = value(pi) or {}
        check("P detect_geometry_issues over 3 000 lines: no false positives, < 5 s", iv.get("success") and (iv.get("summary") or {}).get("examined") == 3000 and iv.get("count") == 0 and i_ms < 5000, f"{i_ms:.0f} ms issues={iv.get('count')} (bridge {pi.get('durationMs')} ms)")
        save("perf", {"query": {"ms": q_ms, "summary": pv.get("summary")}, "nearest": {"ms": n_ms, "summary": nv.get("summary")}, "issues": {"ms": i_ms, "summary": iv.get("summary")}})
    finally:
        s.close()

    return CL.finish("aec-tools-live")


if __name__ == "__main__":
    sys.exit(main())
