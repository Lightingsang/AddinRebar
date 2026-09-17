"""Live verification of the AEC tools (phase A) against a running AutoCAD 2026, over stdio, in one session:
  S  scene: layers + a small structural/architectural/MEP plan with deliberate defects, drawn through execute_autocad_code
  C  get_drawing_context
  Q  query_entities: type/layer filters, paging, detail mode, property selector, handles, text, unknown key warning
  R  query_entities_spatial: crosses, within, nearest, distance_to, touches, tolerance override, bad relation
  M  measure_geometry: length, totalLength, area, distance (points / entities / point-entity), intersections, angle, boundingBox, closestPoint, centroid
  I  detect_geometry_issues: duplicate, near_duplicate, overlapping_segments, endpoint_gap, open_polyline, self_intersection, zero_length, restricted types
  B  classify_aec_entities (default rules on the scene: columns, beams, walls, pipe, door block; unknowns) + get_entity_relationships (connected with gap,
     intersect, self-set parallel with de-duplication, empty source refused)
  N  structural_detect_grids (A/B × 1/2 with bubbles + labels, intersections, spacing), structural_detect_members (columns 400×400, beams,
     slab, openings, marks), connectivity (b2 gap 7 mm under a 5 mm tolerance), column alignment (grid B at y = 5010 → 2 columns off by
     10 mm under a 5 mm tolerance), opening conflicts (one through column c1, one outside the slab)
  R  arch_detect_rooms / arch_room_boundary_check / arch_generate_area_schedule on the two-room plan
  V  mep_detect_network / mep_connectivity_check / mep_endpoint_check on the MEP set
  W  aec_clash_check (pipe × beams / room wall hard, slab edge an area overlap; minSeverity; clearance 50 mm on the 7 mm beam gap located
     in the gap; a frame against itself = contacts only; the pipe set: overlap / clearance / tee; paging; argument errors, empty set warned)
  T  cad_standards_check (layer naming, entity layer, layer 0, colour override, unused layer; stable STD ids; checks subset; bad rule set) +
     audit_aec_drawing (geometry + standards in one stable order, minSeverity, paging, sections)
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
// structural grid A/B (horizontal, B 10 mm off the columns) × 1/2 (vertical) with bubbles and labels, a slab, two openings
var grid = Layer("S-GRID", 8); var slab = Layer("S-SLAB", 9); var open = Layer("S-OPEN", 30);
Add("gA", new Line(P(0, 0), P(7500, 0)), grid); Add("gB", new Line(P(-1500, 5010), P(7500, 5010)), grid);
Add("gl1", new Line(P(0, -1500), P(0, 6500)), grid); Add("gl2", new Line(P(6000, -1500), P(6000, 6500)), grid);
foreach (var (key, c, label) in new[] { ("bubA", P(-1900, 0), "A"), ("bubB", P(-1900, 5010), "B"), ("bub1", P(0, 6900), "1") })
{
    Add(key, new Circle(c, Vector3d.ZAxis, Du(400)), grid);
    Add(key + "t", new DBText { Position = new Point3d(c.X - Du(100), c.Y - Du(100), 0), Height = Du(200), TextString = label }, grid);
}
// the stub that carries bubble A (collinear with grid A: one grid line, not two), and bubble 2 as a block with its label in an attribute
Add("gAs", new Line(P(-1500, 0), P(0, 0)), grid);
var bt0 = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
var bubDef = new BlockTableRecord { Name = "GRID-BUB" };
var bubDefId = bt0.Add(bubDef); tr.AddNewlyCreatedDBObject(bubDef, true);
var bubCircle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, Du(400)); bubDef.AppendEntity(bubCircle); tr.AddNewlyCreatedDBObject(bubCircle, true);
var bubAtt = new AttributeDefinition(new Point3d(-Du(100), -Du(100), 0), "X", "GRIDLABEL", "Grid label", db.Textstyle) { Height = Du(200) };
bubDef.AppendEntity(bubAtt); tr.AddNewlyCreatedDBObject(bubAtt, true);
var bub2 = new BlockReference(P(6000, 6900), bubDefId);
Add("bub2", bub2, grid);
var bub2Att = new AttributeReference(); bub2Att.SetAttributeFromBlock(bubAtt, bub2.BlockTransform); bub2Att.TextString = "2";
bub2.AttributeCollection.AppendAttribute(bub2Att); tr.AddNewlyCreatedDBObject(bub2Att, true);
// a column block with its MARK attribute 600 mm to the right of the symbol: the footprint is the symbol, the mark is the attribute
var colDef = new BlockTableRecord { Name = "COL-400" };
var colDefId = bt0.Add(colDef); tr.AddNewlyCreatedDBObject(colDef, true);
var colRect = new Polyline(4);
colRect.AddVertexAt(0, new Point2d(-Du(200), -Du(200)), 0, 0, 0); colRect.AddVertexAt(1, new Point2d(Du(200), -Du(200)), 0, 0, 0);
colRect.AddVertexAt(2, new Point2d(Du(200), Du(200)), 0, 0, 0); colRect.AddVertexAt(3, new Point2d(-Du(200), Du(200)), 0, 0, 0); colRect.Closed = true;
colDef.AppendEntity(colRect); tr.AddNewlyCreatedDBObject(colRect, true);
var colAtt = new AttributeDefinition(new Point3d(Du(600), 0, 0), "C0", "MARK", "Column mark", db.Textstyle) { Height = Du(200) };
colDef.AppendEntity(colAtt); tr.AddNewlyCreatedDBObject(colAtt, true);
var colBlk = new BlockReference(P(30000, 5000), colDefId);
Add("colblk", colBlk, col);
var colBlkAtt = new AttributeReference(); colBlkAtt.SetAttributeFromBlock(colAtt, colBlk.BlockTransform); colBlkAtt.TextString = "C9";
colBlk.AttributeCollection.AppendAttribute(colBlkAtt); tr.AddNewlyCreatedDBObject(colBlkAtt, true);
Add("slab", Rect(-500, -500, 7000, 6000), slab);
Add("open1", Rect(100, 100, 300, 300), open);
Add("open2", Rect(20000, 8000, 500, 500), open);
// a two-room plan drawn as single-line walls: an 8 x 5 m box at (50000, 0) split at x = 54000, a 900 door in the party wall and one in the
// bottom wall of the left room, room texts inside, and a 250 mm gap in the top wall of the right room (a gap, not a doorway)
Add("hS1", new Line(P(50000, 0), P(51500, 0)), wall); Add("hS2", new Line(P(52400, 0), P(58000, 0)), wall);
Add("hE", new Line(P(58000, 0), P(58000, 5000)), wall);
Add("hN1", new Line(P(58000, 5000), P(56125, 5000)), wall); Add("hN2", new Line(P(55875, 5000), P(50000, 5000)), wall);
Add("hW", new Line(P(50000, 5000), P(50000, 0)), wall);
Add("hM1", new Line(P(54000, 0), P(54000, 2000)), wall); Add("hM2", new Line(P(54000, 2900), P(54000, 5000)), wall);
Add("hT1", new DBText { Position = P(51500, 2500), Height = Du(200), TextString = "PHONG KHACH" }, txt);
Add("hT2", new DBText { Position = P(51500, 2000), Height = Du(200), TextString = "101" }, txt);
Add("hT3", new DBText { Position = P(55500, 2500), Height = Du(200), TextString = "BEDROOM" }, txt);
Add("hT4", new DBText { Position = P(55500, 2000), Height = Du(200), TextString = "B01" }, txt);
// an MEP set at (60000, 0): a chilled-water main with a tee branch, a branch stopping 50 mm short (near miss), a lost run, a run drawn over the main
// (duplicate); a duct served by a diffuser block at its end, and a second diffuser nothing reaches
var duct = Layer("M-DUCT", 5); var diff = Layer("M-DIFF", 6);
Add("mpMain", new Line(P(60000, 0), P(70000, 0)), pipe); Add("mpBr", new Line(P(64000, 0), P(64000, 3000)), pipe);
Add("mpShort", new Line(P(67000, 50), P(67000, 3000)), pipe); Add("mpLost", new Line(P(60000, 8000), P(63000, 8000)), pipe);
Add("mpDup", new Line(P(61000, 0), P(63000, 0)), pipe);
Add("mdMain", new Line(P(60000, -5000), P(66000, -5000)), duct);
var difDef = new BlockTableRecord { Name = "DIFFUSER-600" };
var difDefId = bt0.Add(difDef); tr.AddNewlyCreatedDBObject(difDef, true);
var difRect = new Polyline(4);
difRect.AddVertexAt(0, new Point2d(-Du(300), -Du(300)), 0, 0, 0); difRect.AddVertexAt(1, new Point2d(Du(300), -Du(300)), 0, 0, 0);
difRect.AddVertexAt(2, new Point2d(Du(300), Du(300)), 0, 0, 0); difRect.AddVertexAt(3, new Point2d(-Du(300), Du(300)), 0, 0, 0); difRect.Closed = true;
difDef.AppendEntity(difRect); tr.AddNewlyCreatedDBObject(difRect, true);
Add("dif1", new BlockReference(P(66000, -5000), difDefId), diff);
Add("dif2", new BlockReference(P(70000, -5000), difDefId), diff);
// an inline gate valve block (a fitting by block name) sitting on the unbroken main
var valveDef = new BlockTableRecord { Name = "VALVE-GATE" };
var valveDefId = bt0.Add(valveDef); tr.AddNewlyCreatedDBObject(valveDef, true);
var valveBody = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, Du(120)); valveDef.AppendEntity(valveBody); tr.AddNewlyCreatedDBObject(valveBody, true);
Add("mpValve", new BlockReference(P(62000, 0), valveDefId), pipe);
// standards defects: a text on a wall layer, a line on layer 0, a colour override, a badly named layer with nothing on it
Add("stdText", new DBText { Position = P(40000, 0), Height = Du(200), TextString = "NOT ON A TEXT LAYER" }, wall);
Add("stdZero", new Line(P(40000, 4000), P(41000, 4000)), lt["0"]);
Add("stdColor", new Line(P(40000, 2000), P(41000, 2000)) { ColorIndex = 1 }, wall);
Layer("walls_old", 3);
// a solid hatch over a 2000 x 1000 rectangle
var hatch = new Hatch(); hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
Add("hatch", hatch, wall);
hatch.AppendLoop(HatchLoopTypes.Outermost, new Point2dCollection { new Point2d(Du(24000), Du(4000)), new Point2d(Du(26000), Du(4000)), new Point2d(Du(26000), Du(5000)), new Point2d(Du(24000), Du(5000)) }, new DoubleCollection { 0, 0, 0, 0 });
hatch.EvaluateHatch(true);
// a block definition with one attribute, inserted once with MARK = D01
var bt = bt0;
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
        check("tools/list holds the AEC seeds (+ the core and registry tools)", all(n in names for n in aec) and len(names) >= 40, f"{len(names)} tools")

        # ---- S: scene -------------------------------------------------------------------------------------------------
        scene = s.tool("execute_autocad_code", {"code": SCENE, "transaction": "auto", "label": "aec scene", "timeoutSeconds": 60}, timeout=120)
        h = value(scene) or {}
        check("S scene drawn (64 entities, 11 layers)", not scene.get("isError") and len(h) == 64 and (scene.get("changed") or {}).get("added", 0) >= 64, f"handles={len(h)} changed={short(scene.get('changed'))} {short(scene.get('message'))}")
        save("scene-handles", h)
        if not h:
            return CL.finish("aec-tools-live")

        # ---- C: drawing context ---------------------------------------------------------------------------------------
        ctx = s.tool("get_drawing_context", {"includeLayouts": True, "includeLayers": True})
        v = value(ctx) or {}
        counts = v.get("counts") or {}
        check("C context: units, layout, counts, ucs, extents", not ctx.get("isError") and v.get("units", {}).get("mmPerUnit") and v.get("activeLayout") and counts.get("modelSpaceEntities", 0) >= 42 and counts.get("layers", 0) >= 6 and v.get("ucs", {}).get("isWorld") is not None,
              f"units={short(v.get('units'), 80)} layout={v.get('activeLayout')} entities={counts.get('modelSpaceEntities')} layers={counts.get('layers')}")
        check("C context lists layouts and the S-COL layer", any(l.get("name") == "Model" for l in (v.get("layouts") or [])) and any(l.get("name") == "S-COL" and l.get("color") == "1" for l in (v.get("layers") or [])), short(v.get("layouts")))
        save("context", v)

        # ---- Q: query_entities ----------------------------------------------------------------------------------------
        q = value(s.tool("query_entities", {"filter": {"types": ["LWPOLYLINE"], "layers": ["S-COL"]}})) or {}
        check("Q columns by type + layer: 4 records with bounds", q.get("success") and q.get("count") == 4 and len(q.get("items", [])) == 4 and all(i.get("boundsMm") for i in q["items"]), short(q.get("summary")))
        paged = value(s.tool("query_entities", {"filter": {"layers": ["S-COL", "S-BEAM"]}, "limit": 3, "offset": 3})) or {}
        check("Q paging: layers S-COL + S-BEAM limit 3 offset 3 -> count 8 (4 columns + the column block + 3 beams), 3 items, truncated", paged.get("count") == 8 and len(paged.get("items", [])) == 3 and paged.get("truncated") is True and paged.get("offset") == 3, short(paged.get("summary")))
        detail = value(s.tool("query_entities", {"filter": {"handles": [h["c1"]]}, "mode": "detail"})) or {}
        item = (detail.get("items") or [{}])[0]
        geom = item.get("geometry") or {}
        check("Q detail mode: closed 4-vertex geometry, area 160000 mm2, color/linetype present", geom.get("closed") is True and len(geom.get("vertices", [])) == 4 and abs(geom.get("areaMm2", 0) - 160000) < 1 and item.get("color") and item.get("linetype"), short(item, 300))
        props = value(s.tool("query_entities", {"filter": {"types": ["TEXT"], "textContains": "office"}, "properties": ["text", "position", "bogus"]})) or {}
        pi = (props.get("items") or [{}])[0]
        check("Q textContains + property selector + unknown property warning", props.get("count") == 1 and pi.get("text") == "OFFICE 01" and pi.get("positionMm") and "boundsMm" not in pi and any("bogus" in w for w in props.get("warnings", [])), short(props))
        unknown = value(s.tool("query_entities", {"filter": {"layer": "M-PIPE", "colour": "4"}})) or {}
        check("Q singular key + unknown filter key -> warning, 8 M-PIPE entities", unknown.get("count") == 8 and any("colour" in w for w in unknown.get("warnings", [])), short(unknown.get("warnings")))
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
        check("B classify: 5 columns (4 outlines + the COL-400 block), 3 beams, 6 pipes (the polyline + the MEP set), 1 door block, walls; circle/text/hatch unknown",
              cls.get("success") and summary.get("StructuralColumn") == 5 and summary.get("StructuralBeam") == 3 and summary.get("Pipe") == 6 and summary.get("Door") == 1 and summary.get("ArchitecturalWall", 0) >= 8
              and by.get(h["circle"], {}).get("aecType") == "Unknown" and by.get(h["text"], {}).get("aecType") == "Unknown", short(summary))
        c1 = by.get(h["c1"]) or {}
        check("B column object: confidence >= 0.9, evidence names the layer and the 400x400 footprint, properties width/depth/area/centroid",
              c1.get("aecType") == "StructuralColumn" and c1.get("confidence", 0) >= 0.9 and any("S-COL" in e for e in c1.get("evidence", [])) and near((c1.get("properties") or {}).get("widthMm"), 400) and near((c1.get("properties") or {}).get("areaMm2"), 160000) and (c1.get("properties") or {}).get("centroidMm"), short(c1, 400))
        door = ((value(s.tool("classify_aec_entities", {"filter": {"handles": [h["door"]]}})) or {}).get("items") or [{}])[0]
        check("B door block: Door 0.9 via block name, attributes carried", door.get("aecType") == "Door" and near(door.get("confidence"), 0.9) and ((door.get("properties") or {}).get("attributes") or {}).get("MARK") == "D01", short(door, 300))
        structural = value(s.tool("classify_aec_entities", {"filter": {"layers": ["S-COL", "S-BEAM", "A-*", "M-*"]}, "disciplines": ["Structural"], "limit": 5, "offset": 5})) or {}
        check("B disciplines=Structural + paging: count 8, page 2 has 3 items, ruleSet reported", structural.get("count") == 8 and len(structural.get("items", [])) == 3 and (structural.get("summary") or {}).get("ruleSet", {}).get("rules", 0) >= 20, short(structural.get("summary"), 300))
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
        every = value(s.tool("get_entity_relationships", {"filter": {"layers": ["S-COL", "S-BEAM", "A-WALL"]}, "relations": ["intersect", "connected", "near", "touching", "inside", "contains"], "maxDistance": 500})) or {}
        origin = [r for r in every.get("items", []) if near((r.get("locationMm") or {}).get("x"), 0, 1) and near((r.get("locationMm") or {}).get("y"), 0, 1)]
        check("B every proximity relationship carries a real location (none at the origin)", every.get("count", 0) >= 10 and all(r.get("locationMm") for r in every.get("items", [])) and not origin, f"{every.get('count')} relationships, at origin: {len(origin)}")
        badrel = s.tool("get_entity_relationships", {"relations": ["connected"]})
        check("B empty source -> ArgumentException", badrel.get("isError") and "source" in (badrel.get("message") or ""), short(badrel.get("message")))
        save("semantic", {"classify": cls, "structural": structural, "connected": rel, "intersect": inter, "parallel": par, "capped": capped, "big": big, "every": every})

        # ---- N: structural ---------------------------------------------------------------------------------------------------
        gr = value(s.tool("structural_detect_grids", {})) or {}
        gsum = gr.get("summary") or {}
        lines = {l["handle"]: l for l in gr.get("items", [])}
        check("N detect_grids: 4 lines labelled A, B, 1, 2 (A's stub merged into it, 2 read from the block bubble's attribute), 4 intersections, spacing 5010 / 6000",
              gr.get("count") == 4 and gsum.get("labels") == ["1", "2", "A", "B"] and lines.get(h["gA"], {}).get("label") == "A" and lines.get(h["gA"], {}).get("bubbleHandle") == h["bubA"] and lines.get(h["gA"], {}).get("mergedSegments") == 2 and gsum.get("merged") == 1
              and lines.get(h["gl2"], {}).get("direction") == "vertical" and lines.get(h["gl2"], {}).get("label") == "2" and lines.get(h["gl2"], {}).get("bubbleHandle") == h["bub2"]
              and gsum.get("intersectionCount") == 4 and (gsum.get("spacingMm") or {}).get("horizontal") == [5010] and (gsum.get("spacingMm") or {}).get("vertical") == [6000], short(gsum, 300))
        mem = value(s.tool("structural_detect_members", {})) or {}
        msum = mem.get("summary") or {}
        by_handle = {m["handle"]: m for m in mem.get("items", [])}
        check("N detect_members: 5 columns (4 drawn 400×400 + the block 400×400 despite its attribute, mark C9 from the MARK attribute), 3 beams L 5600/5593, 1 slab, 2 openings",
              (msum.get("byKind") or {}).get("column") == 5 and (msum.get("byKind") or {}).get("beam") == 3 and (msum.get("byKind") or {}).get("slab") == 1 and (msum.get("byKind") or {}).get("opening") == 2
              and by_handle.get(h["c1"], {}).get("section") == "400×400" and near((by_handle.get(h["c1"], {}).get("centerMm") or {}).get("x"), 0) and by_handle.get(h["b2"], {}).get("section") == "L 5593" and by_handle.get(h["b2"], {}).get("axisMm")
              and by_handle.get(h["colblk"], {}).get("section") == "400×400" and by_handle.get(h["colblk"], {}).get("mark") == "C9" and by_handle.get(h["colblk"], {}).get("markSource") == "attribute" and by_handle.get(h["colblk"], {}).get("markHandle") == h["colblk"], f"{short(msum, 200)} colblk={short(by_handle.get(h['colblk']), 200)}")
        con = value(s.tool("structural_member_connectivity_check", {})) or {}
        con5 = value(s.tool("structural_member_connectivity_check", {"tolerance": {"endpointConnection": 5}})) or {}
        c5 = [i for i in con5.get("items", []) if i["type"] == "gap_to_support"]
        check("N connectivity: no issue at 10 mm; at 5 mm b2 -> c4 is a 7 mm gap_to_support (STR-CON-001, warning)",
              con.get("count") == 0 and (con.get("summary") or {}).get("scope", {}).get("beams") == 3 and len(c5) == 1 and c5[0]["handles"] == [h["b2"], h["c4"]] and near(c5[0].get("valueMm"), 7) and c5[0]["issueId"] == "STR-CON-001", short(c5, 300))
        aln = value(s.tool("structural_column_alignment_check", {})) or {}
        aln5 = value(s.tool("structural_column_alignment_check", {"alignmentToleranceMm": 5})) or {}
        off = [i for i in aln5.get("items", []) if i["type"] == "column_off_grid"]
        nogrid = [i for i in aln.get("items", []) if i["type"] == "column_no_grid"]
        check("N column alignment: the 4 drawn columns on grid at 25 mm, the block column 24 m away has no grid (column_no_grid); at 5 mm c3 + c4 are 10 mm off grid B (STR-ALN, handles column + 2 grid lines)",
              aln.get("count") == 1 and nogrid and nogrid[0]["handles"] == [h["colblk"]] and (aln.get("summary") or {}).get("scope", {}).get("intersections") == 4 and len(off) == 2 and {o["handles"][0] for o in off} == {h["c3"], h["c4"]} and all(near(o.get("valueMm"), 10) and h["gB"] in o["handles"] for o in off), short(off, 300))
        opn = value(s.tool("structural_opening_conflict_check", {})) or {}
        ot = {i["type"]: i for i in opn.get("items", [])}
        check("N opening conflicts: open1 through column c1 (critical, STR-OPN-001), open2 outside the slab (warning)",
              opn.get("count") == 2 and ot.get("opening_through_column", {}).get("handles") == [h["open1"], h["c1"]] and ot["opening_through_column"]["issueId"] == "STR-OPN-001" and ot.get("opening_outside_host", {}).get("handles") == [h["open2"]], short(opn.get("items"), 300))
        save("structural", {"grids": gr, "members": mem, "connectivity": [con, con5], "alignment": [aln, aln5], "openings": opn})

        # ---- R: architecture (rooms from the A-WALL scene) ----------------------------------------------------------------------
        rm = value(s.tool("arch_detect_rooms", {})) or {}
        rsum = rm.get("summary") or {}
        rooms = {tuple(sorted(r.get("handles", []))): r for r in rm.get("items", [])}
        office = rooms.get((h["room"],), {})
        openr = rooms.get((h["openroom"],), {})
        dupr = rooms.get((h["dupclose"],), {})
        left = next((r for r in rm.get("items", []) if h["hW"] in r.get("handles", [])), {})
        right = next((r for r in rm.get("items", []) if h["hE"] in r.get("handles", [])), {})
        check("R detect_rooms: 4 rooms from walls — the closed outline (12 m², OFFICE 01), the 12 mm-open outline closed by roomGap (12 m², unlabelled), the 2 × 1 m rectangle, and PHONG KHACH 101 (20 m²) of the two-room plan, kept whole by the two 900 mm doorways bridged; the right room is open (250 mm gap); bow-tie halves too small; total 46 m²",
              rm.get("count") == 4 and rsum.get("fromWalls") == 4 and near(office.get("areaM2"), 12) and office.get("name") == "OFFICE 01" and office.get("textHandles") == [h["text"]] and near(openr.get("areaM2"), 12) and openr.get("name") is None
              and near(dupr.get("areaM2"), 2) and near(rsum.get("totalAreaM2"), 46) and rsum.get("closedGaps") == 2 and rsum.get("tinyFaces") == 2 and rsum.get("openings") == 2 and len(office.get("outlineMm", [])) == 4 and office.get("source") == "walls"
              and near(left.get("areaM2"), 20) and (left.get("name"), left.get("number")) == ("PHONG KHACH", "101") and not right and h["hM1"] in left.get("handles", []) and h["hS1"] in left.get("handles", []), f"{short(rsum, 300)} left={short(left, 160)}")
        strict = value(s.tool("arch_detect_rooms", {"tolerance": {"roomGap": 5}})) or {}
        check("R roomGap 5 mm -> the 12 mm-open outline is no longer a room (3 rooms), the 7 mm g1/g2 gap stays open", strict.get("count") == 3 and (strict.get("summary") or {}).get("closedGaps") == 0, short(strict.get("summary"), 200))
        nodoor = value(s.tool("arch_detect_rooms", {"detection": {"maxOpeningMm": 800}})) or {}
        check("R detection.maxOpeningMm 800 -> the 900 mm doorways are no longer bridged: the left room is open too (3 rooms), no openings", nodoor.get("count") == 3 and (nodoor.get("summary") or {}).get("openings") == 0, short(nodoor.get("summary"), 200))
        badd = s.tool("arch_detect_rooms", {"detection": {"maxGapMm": 700}})
        check("R detection.maxGapMm above minOpeningMm -> ArgumentException", badd.get("isError") and "minOpeningMm" in (badd.get("message") or ""), short(badd.get("message")))
        bc = value(s.tool("arch_room_boundary_check", {})) or {}
        bt_ = {i["type"]: [x for x in bc.get("items", []) if x["type"] == i["type"]] for i in bc.get("items", [])}
        closed_g = [i for i in bt_.get("boundary_gap_closed", []) if sorted(i["handles"]) == sorted([h["g1"], h["g2"]])]
        gap400 = [i for i in bt_.get("boundary_gap", []) if sorted(i["handles"]) == sorted([h["hN1"], h["hN2"]])]
        check("R boundary check: open_boundary critical for the 12 free wall ends (w1/w2, g1/g2, both arcs, the semicircle, the coloured line), one boundary_gap (250 mm, hN1+hN2, located between the ends), boundary_gap_closed for openroom (12 mm) + g1→g2 (7 mm), opening_assumed ×2, unlabelled_room ×2; ids ARC-nnn severity-first",
              bc.get("success") and len(bt_.get("open_boundary", [])) == 12 and len(gap400) == 1 and near(gap400[0].get("valueMm"), 250) and near(gap400[0]["locationMm"]["x"], 56000) and len(bt_.get("boundary_gap", [])) == 1
              and len(bt_.get("boundary_gap_closed", [])) == 2 and len(closed_g) == 1 and near(closed_g[0].get("valueMm"), 7, 0.5) and len(bt_.get("opening_assumed", [])) == 2 and len(bt_.get("unlabelled_room", [])) == 2
              and bc["items"][0]["issueId"] == "ARC-001" and bc["items"][0]["severity"] == "critical" and (bc.get("summary") or {}).get("openEnds") == 14, f"{short((bc.get('summary') or {}).get('byType'), 200)} gap={short(gap400, 200)}")
        sched = value(s.tool("arch_generate_area_schedule", {"groupBy": "name"})) or {}
        srows = {r["group"]: r for r in sched.get("items", [])}
        check("R area schedule by name: PHONG KHACH 20 m² (43.5 %), OFFICE 01 12 m², (none) 14 m² (2 rooms); total 46 m²; the 101 room listed as '101 PHONG KHACH'",
              sched.get("count") == 3 and near(srows.get("PHONG KHACH", {}).get("areaM2"), 20) and near(srows.get("PHONG KHACH", {}).get("percent"), 43.5, 0.15) and srows.get("PHONG KHACH", {}).get("rooms") == ["101 PHONG KHACH"] and near(srows.get("OFFICE 01", {}).get("areaM2"), 12)
              and srows.get("(none)", {}).get("count") == 2 and near(srows.get("(none)", {}).get("areaM2"), 14) and near((sched.get("summary") or {}).get("totalAreaM2"), 46), short(sched.get("items"), 300))
        bad = s.tool("arch_generate_area_schedule", {"groupBy": "colour"})
        check("R groupBy colour -> ArgumentException", bad.get("isError") and "groupBy" in (bad.get("message") or ""), short(bad.get("message")))
        save("architecture", {"rooms": rm, "strict": strict, "boundary": bc, "schedule": sched})

        # ---- V: MEP ------------------------------------------------------------------------------------------------------------
        net = value(s.tool("mep_detect_network", {})) or {}
        nsum = net.get("summary") or {}
        by_run = {}
        for n in net.get("items", []):
            for rh in n.get("runHandles", []): by_run[rh] = n
        main = by_run.get(h["mpMain"], {})
        ductn = by_run.get(h["mdMain"], {})
        check("V detect_network: 5 networks — the pipe main + tee branch + overlapping copy (15 m, 2 m drawn twice, N-001, 3 open ends, the inline valve attached), the duct with its diffuser (node), the old pipe polyline, the lost run, the short branch; 1 orphan diffuser, 1 duplicate, 0 crossings",
              net.get("count") == 5 and nsum.get("runs") == 7 and nsum.get("nodes") == 3 and main.get("id") == "N-001" and main.get("runs") == 3 and near(main.get("lengthMm"), 15000) and near(main.get("duplicateOverlapMm"), 2000) and main.get("openEnds") == 3 and h["mpBr"] in main.get("runHandles", []) and h["mpDup"] in main.get("runHandles", [])
              and main.get("nodeHandles") == [h["mpValve"]] and ductn.get("nodes") == 1 and ductn.get("nodeHandles") == [h["dif1"]] and ductn.get("openEnds") == 1 and nsum.get("orphanNodes") == 1 and nsum.get("orphanHandles") == [h["dif2"]] and nsum.get("duplicates") == 1 and nsum.get("crossings") == 0
              and by_run.get(h["mpShort"], {}).get("runs") == 1 and (by_run.get(h["mpShort"], {}).get("openEndsMm") or [{}])[0].get("nearest") == h["mpMain"], f"{short(nsum, 300)} main={short(main, 200)}")
        sysn = value(s.tool("mep_detect_network", {"detection": {"systems": {"CHW": ["M-PIPE*"], "SA": ["M-DUCT*"]}}})) or {}
        check("V systems by layer map -> the pipe networks are CHW, the duct network SA", {n.get("system") for n in sysn.get("items", [])} == {"CHW", "SA"} and set((sysn.get("summary") or {}).get("bySystem", {}).keys()) == {"CHW", "SA"}, short((sysn.get("summary") or {}).get("bySystem"), 200))
        loose = value(s.tool("mep_detect_network", {"tolerance": {"endpointConnection": 60}})) or {}
        check("V endpointConnection 60 mm -> the short branch joins the main (4 networks)", loose.get("count") == 4 and by_run and any(h["mpShort"] in n.get("runHandles", []) and h["mpMain"] in n.get("runHandles", []) for n in loose.get("items", [])), short(loose.get("summary"), 200))
        cc = value(s.tool("mep_connectivity_check", {})) or {}
        ct_ = {i["type"]: [x for x in cc.get("items", []) if x["type"] == i["type"]] for i in cc.get("items", [])}
        miss = ct_.get("near_miss", [])
        check("V connectivity check: near_miss 50 mm (short → main), open_end ×5 (main both ends, branch, short end, duct start), disconnected_run ×2 (lost run, the old polyline), duplicate_run 2000 mm, orphan_node warning for the far diffuser only (the inline valve is attached); ids MEP-nnn",
              cc.get("success") and len(miss) == 1 and miss[0]["handles"] == [h["mpShort"], h["mpMain"]] and near(miss[0].get("valueMm"), 50) and len(ct_.get("open_end", [])) == 5 and len(ct_.get("disconnected_run", [])) == 2
              and {i["handles"][0] for i in ct_.get("disconnected_run", [])} == {h["mpLost"], h["pipe"]} and len(ct_.get("duplicate_run", [])) == 1 and near(ct_["duplicate_run"][0].get("valueMm"), 2000)
              and len(ct_.get("orphan_node", [])) == 1 and ct_["orphan_node"][0]["handles"] == [h["dif2"]] and ct_["orphan_node"][0]["severity"] == "warning" and cc["items"][0]["issueId"] == "MEP-001", short((cc.get("summary") or {}).get("byType"), 200))
        ep = value(s.tool("mep_endpoint_check", {})) or {}
        eps = ep.get("summary") or {}
        first = (ep.get("items") or [{}])[0]
        check("V endpoint check: 10 open ends listed, the near miss first with its gap; 14 endpoints in all, byState joined/tee/node/open", ep.get("count") == 10 and eps.get("endpoints") == 14 and first.get("run") == h["mpShort"] and first.get("nearestHandle") == h["mpMain"] and near(first.get("nearestGapMm"), 50)
              and (eps.get("byState") or {}).get("open") == 10 and (eps.get("byState") or {}).get("tee") == 3 and (eps.get("byState") or {}).get("node") == 1, f"{short(eps, 250)} first={short(first, 150)}")
        epall = value(s.tool("mep_endpoint_check", {"includeConnected": True, "filter": {"layers": ["M-PIPE"]}})) or {}
        check("V includeConnected + pipe layer -> 12 endpoints, the tee ends name the main", epall.get("count") == 12 and any(e.get("state") == "tee" and e.get("connectedTo") == [h["mpMain"]] for e in epall.get("items", [])), short(epall.get("summary"), 200))
        badm = s.tool("mep_detect_network", {"detection": {"nearMissMm": 10}})
        check("V nearMissMm not above endpointConnection -> ArgumentException", badm.get("isError") and "endpointConnection" in (badm.get("message") or ""), short(badm.get("message")))
        nor = value(s.tool("mep_connectivity_check", {"filter": {"layers": ["M-DIFF"]}})) or {}
        check("V connectivity on the diffuser layer alone (no runs) -> warned, no orphan issues", nor.get("count") == 0 and any("No pipe" in w for w in nor.get("warnings", [])), short(nor.get("warnings"), 200))
        save("mep", {"network": net, "systems": sysn, "loose": loose, "connectivity": cc, "endpoints": [ep, epall]})

        # ---- W: coordination (clash check) ---------------------------------------------------------------------------------
        hosts_w = {"filter": {"handles": [h["b1"], h["b2"], h["b3"], h["room"], h["slab"]]}}
        cw = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["pipe"]]}, "aecTypes": ["Pipe"]}, "setB": hosts_w})) or {}
        cws = cw.get("summary") or {}
        cw_by = {i["handles"][1]: i for i in cw.get("items", [])}
        check("W clash check pipe x {b1, b2, b3, room, slab}: 3 hard clashes listed — crosses b1 and b3 at (3000,0), crosses the room wall at (3000,1000); the slab edge crossing is an area_overlap (info) counted, not listed; b2 clear; ids CL-0001..3, all critical, rule Pipe×Type, byPair counts",
              cw.get("success") and cw.get("count") == 3 and set(cw_by) == {h["b1"], h["b3"], h["room"]} and all(i["type"] == "hard_clash" and i["severity"] == "critical" and i["handles"][0] == h["pipe"] and i["category"] == "coordination" for i in cw.get("items", []))
              and [i["issueId"] for i in cw["items"]] == ["CL-0001", "CL-0002", "CL-0003"] and near(cw_by[h["b1"]]["locationMm"]["x"], 3000) and near(cw_by[h["b1"]]["locationMm"]["y"], 0) and "crosses" in cw_by[h["b1"]]["description"]
              and near(cw_by[h["room"]]["locationMm"]["x"], 3000) and near(cw_by[h["room"]]["locationMm"]["y"], 1000) and cw_by[h["room"]].get("rule") == "Pipe×ArchitecturalWall"
              and cws.get("hard") == 3 and cws.get("clearance") == 0 and cws.get("areaOverlaps") == 1 and cws.get("found") == 4 and cws.get("listed") == 3 and cws.get("belowMinSeverity") == 1 and cws.get("minSeverity") == "warning"
              and (cws.get("byPair") or {}).get("Pipe×StructuralBeam") == 2 and (cws.get("setA") or {}).get("subjects") == 1 and (cws.get("setB") or {}).get("subjects") == 5 and cws.get("sameSet") is False,
              f"{short(cws, 300)} items={short([(i['handles'][1], i['description']) for i in cw.get('items', [])], 400)}")
        cwi = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["pipe"]]}}, "setB": hosts_w, "minSeverity": "info"})) or {}
        slab_i = next((i for i in cwi.get("items", []) if i["handles"][1] == h["slab"]), {})
        check("W minSeverity info lists the slab edge crossing too: area_overlap info 'crosses the edge of' at (3000,-500), last", cwi.get("count") == 4 and slab_i.get("type") == "area_overlap" and slab_i.get("severity") == "info" and "crosses the edge of" in slab_i.get("description", "")
              and near(slab_i.get("locationMm", {}).get("y"), -500) and cwi["items"][-1]["issueId"] == "CL-0004", short(slab_i, 300))
        cwp = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["pipe"]]}}, "setB": hosts_w, "limit": 1, "offset": 1})) or {}
        check("W paging offset 1 limit 1 -> CL-0002 alone, count 3, truncated", [i["issueId"] for i in cwp.get("items", [])] == ["CL-0002"] and cwp.get("count") == 3 and cwp.get("truncated") is True and cwp.get("offset") == 1, short(cwp.get("items"), 200))
        cl50 = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b2"]]}}, "setB": {"filter": {"handles": [h["c4"], h["c3"]]}}, "clearanceMm": 50})) or {}
        cl50i = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b2"]]}}, "setB": {"filter": {"handles": [h["c4"], h["c3"]]}}, "clearanceMm": 50, "minSeverity": "info"})) or {}
        cl5 = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b2"]]}}, "setB": {"filter": {"handles": [h["c4"]]}}, "clearanceMm": 5})) or {}
        gap = (cl50.get("items") or [{}])[0]
        check("W clearance 50 mm: b2 stops 7 mm short of c4 -> clearance_clash warning, valueMm 7, located in the gap (x 5793..5800, y 5000); b2 on c3 is a contact counted (info, listed with minSeverity info as 'meets'); clearance 5 mm -> nothing",
              cl50.get("count") == 1 and gap.get("type") == "clearance_clash" and gap.get("severity") == "warning" and gap.get("handles") == [h["b2"], h["c4"]] and near(gap.get("valueMm"), 7, 0.01) and "50 mm required" in gap.get("description", "")
              and 5793 <= gap.get("locationMm", {}).get("x", 0) <= 5800 and near(gap.get("locationMm", {}).get("y"), 5000) and (cl50.get("summary") or {}).get("contacts") == 1 and (cl50.get("summary") or {}).get("clearance") == 1
              and cl50i.get("count") == 2 and any(i["type"] == "contact" and i["handles"] == [h["b2"], h["c3"]] and "meets" in i["description"] for i in cl50i.get("items", [])) and cl5.get("count") == 0 and cl5.get("success") is True,
              f"{short(gap, 300)} info={short(cl50i.get('items'), 300)} cl5={cl5.get('count')}")
        same = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b1"], h["b3"], h["c1"], h["c2"]]}}})) or {}
        samei = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b1"], h["b3"], h["c1"], h["c2"]]}}, "minSeverity": "info"})) or {}
        same_pairs = sorted(tuple(sorted(i["handles"])) for i in samei.get("items", []))
        check("W a frame against itself: no clash — 5 contacts (b1×b3 runs along, each beam meets both columns), each pair once, never an entity with itself; summary sameSet + no setB; listed only with minSeverity info",
              same.get("count") == 0 and (same.get("summary") or {}).get("contacts") == 5 and (same.get("summary") or {}).get("hard") == 0 and (same.get("summary") or {}).get("pairsChecked") == 5 and (same.get("summary") or {}).get("sameSet") is True and (same.get("summary") or {}).get("setB") is None
              and samei.get("count") == 5 and len(same_pairs) == len(set(same_pairs)) and all(a != b for a, b in same_pairs) and all(i["type"] == "contact" and i["severity"] == "info" for i in samei.get("items", []))
              and any(set(i["handles"]) == {h["b1"], h["b3"]} and "runs along" in i["description"] for i in samei.get("items", [])), f"{short(same.get('summary'), 250)} pairs={same_pairs}")
        mep = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["mpMain"], h["mpBr"], h["mpDup"], h["mpShort"]]}}, "clearanceMm": 100})) or {}
        mep_by = {i["type"]: i for i in mep.get("items", [])}
        check("W the pipe set against itself, clearance 100: the copy over the main is a hard overlap, the 50 mm short branch a clearance clash at (67000, 25), the tee a contact counted",
              mep.get("count") == 2 and set(mep_by["hard_clash"]["handles"]) == {h["mpMain"], h["mpDup"]} and "overlaps" in mep_by["hard_clash"]["description"]
              and set(mep_by["clearance_clash"]["handles"]) == {h["mpMain"], h["mpShort"]} and near(mep_by["clearance_clash"].get("valueMm"), 50, 0.01) and near(mep_by["clearance_clash"]["locationMm"]["x"], 67000) and near(mep_by["clearance_clash"]["locationMm"]["y"], 25)
              and (mep.get("summary") or {}).get("contacts") == 1 and (mep.get("summary") or {}).get("hard") == 1, f"{short(mep.get('summary'), 250)} items={short(mep.get('items'), 400)}")
        cwb = s.tool("aec_clash_check", {"setA": {"aecTypes": ["Bogus"]}})
        cwn = value(s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["text"]]}}})) or {}
        cwc = s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b1"]]}}, "clearanceMm": -1})
        cwk = s.tool("aec_clash_check", {"setA": {"layers": ["S-*"]}})
        cwm = s.tool("aec_clash_check", {"setA": {"filter": {"handles": [h["b1"]]}}, "minSeverity": "high"})
        check("W errors: unknown AEC type, negative clearance, a set key that is not filter/aecTypes, an unknown minSeverity -> ArgumentException each; a set that classifies nothing -> empty result with a warning",
              cwb.get("isError") and "not an AEC type" in (cwb.get("message") or "") and cwn.get("success") is True and cwn.get("count") == 0 and any("matched no classified entity" in w for w in cwn.get("warnings", []))
              and cwc.get("isError") and "clearanceMm" in (cwc.get("message") or "") and cwk.get("isError") and "not a set key" in (cwk.get("message") or "") and cwm.get("isError") and "minSeverity" in (cwm.get("message") or ""),
              short([cwb.get("message"), cwn.get("warnings"), cwc.get("message"), cwk.get("message"), cwm.get("message")], 500))
        save("coordination", {"clash": cw, "info": cwi, "page": cwp, "clearance": cl50, "same": samei, "mep": mep})

        # ---- T: standards + audit ---------------------------------------------------------------------------------------
        std = value(s.tool("cad_standards_check", {})) or {}
        st = {i["type"]: [x for x in std.get("items", []) if x["type"] == i["type"]] for i in std.get("items", [])}
        check("T cad_standards_check whole drawing: layer_naming walls_old, entity_layer text, layer_zero line, color_override, unused_layer walls_old; ids STD-nnnn",
              std.get("success") and (std.get("summary") or {}).get("wholeDrawing") is True
              and any(i.get("layer") == "walls_old" for i in st.get("layer_naming", [])) and any(i.get("handles") == [h["stdText"]] for i in st.get("entity_layer", []))
              and any(i.get("handles") == [h["stdZero"]] for i in st.get("layer_zero", [])) and any(i.get("handles") == [h["stdColor"]] for i in st.get("color_override", []))
              and any(i.get("layer") == "walls_old" for i in st.get("unused_layer", [])) and std["items"][0].get("issueId") == "STD-0001", short((std.get("summary") or {}).get("byType"), 200))
        std2 = value(s.tool("cad_standards_check", {})) or {}
        check("T standards ids and order are identical on a second run", [(i["issueId"], i["type"], i.get("handles"), i.get("layer")) for i in std.get("items", [])] == [(i["issueId"], i["type"], i.get("handles"), i.get("layer")) for i in std2.get("items", [])], f"{len(std.get('items', []))} issues")
        sub = value(s.tool("cad_standards_check", {"checks": ["layer_naming", "bogus"], "filter": {"layers": ["S-*"]}})) or {}
        sub2 = value(s.tool("cad_standards_check", {"checks": ["unused_layer"], "filter": {"layers": ["S-*"]}})) or {}
        check("T checks subset + filter: only layer_naming issues, unknown check warned; unused_layer on a subset skipped with a warning",
              all(i["type"] == "layer_naming" for i in sub.get("items", [])) and (sub.get("summary") or {}).get("checkedTypes") == ["layer_naming"] and any("bogus" in w for w in sub.get("warnings", []))
              and sub2.get("count") == 0 and (sub2.get("summary") or {}).get("checkedTypes") == [] and any("unused_layer skipped" in w for w in sub2.get("warnings", [])), short(sub2.get("warnings"), 250))
        badrs = s.tool("cad_standards_check", {"ruleSet": "does-not-exist"})
        check("T unknown rule set -> ArgumentException without the profile path", badrs.get("isError") and "not found" in (badrs.get("message") or "") and "AppData" not in (badrs.get("message") or ""), short(badrs.get("message")))
        aud = value(s.tool("audit_aec_drawing", {})) or {}
        asum = aud.get("summary") or {}
        sev = [i["severity"] for i in aud.get("items", [])]
        rank = {"critical": 0, "warning": 1, "info": 2}
        check("T audit_aec_drawing: geometry + standards sections, GEO and STD ids, severity-ordered, bySeverity totals add up",
              aud.get("success") and asum.get("sections") == ["geometry", "standards"] and any(i["issueId"].startswith("GEO-") for i in aud.get("items", [])) and any(i["issueId"].startswith("STD-") for i in aud.get("items", []))
              and sev == sorted(sev, key=lambda x: rank[x]) and sum((asum.get("bySeverity") or {}).values()) == aud.get("count"), short(asum, 300))
        warn = value(s.tool("audit_aec_drawing", {"minSeverity": "warning"})) or {}
        check("T minSeverity warning drops the info issues and counts them", warn.get("count") == asum.get("bySeverity", {}).get("critical", 0) + asum.get("bySeverity", {}).get("warning", 0) and (warn.get("summary") or {}).get("belowMinSeverity") == asum.get("bySeverity", {}).get("info"), short(warn.get("summary"), 200))
        page = value(s.tool("audit_aec_drawing", {"limit": 3, "offset": 3})) or {}
        check("T paging: offset 3 limit 3 returns items 4-6 of the same order", [i["issueId"] for i in page.get("items", [])] == [i["issueId"] for i in aud.get("items", [])][3:6] and page.get("truncated") is True, short(page.get("items"), 200))
        geo_only = value(s.tool("audit_aec_drawing", {"sections": ["geometry"], "filter": {"layers": ["S-*", "A-WALL"]}})) or {}
        check("T sections geometry only: no STD issues, no ruleSet in the summary", all(i["category"] == "geometry" for i in geo_only.get("items", [])) and (geo_only.get("summary") or {}).get("ruleSet") is None and (geo_only.get("summary") or {}).get("sections") == ["geometry"], short(geo_only.get("summary"), 200))
        save("audit", {"standards": std, "subset": sub, "audit": aud, "warn": warn, "page": page, "geometryOnly": geo_only})

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
