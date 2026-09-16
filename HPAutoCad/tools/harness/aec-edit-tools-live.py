"""Live verification of the AEC write tools (phase C) against a running AutoCAD 2026, over stdio, in one session:
  S  scene: layers (incl. one locked, one frozen), a room outline, a column outline, an open outline, a text, a block definition with an attribute
  C  create_entities_batch: atomic batch of 5 types, atomic refusal on a locked layer (nothing created), non-atomic partial, frozen-layer warning,
     block reference with attributes, unknown type, dryRun rolled back, empty items refused
  U  update_entities_batch: shared set over handles, per-item text/height/geometry/move/rotate, locked → LAYER_LOCKED, atomic refusal keeps the
     drawing untouched, erased → ERASED, wrong key → UNSUPPORTED_ENTITY
  B  manage_blocks_attributes: listDefinitions, findReferences, insert, readAttributes, writeAttributes (+ unknown tag warning),
     batchUpdateAttributes by filter, inspectDynamic on a plain block, setDynamic refused, bad op
  A  manage_annotations: text, mtext, aligned/angular/radial/diameter dimensions with measurements, mleader, update, delete, geometry refused
  H  manage_hatches: boundaryHandles ANSI31 with area, seedPoint SOLID, detectBoundary innermost first, NOT_CLOSED, update, delete, non-hatch refused
  X  manage_xrefs: list, attach (a DWG written by Wblock), overlay, unload → bind refused, reload, detach, bind (safe), missing file refused
  K  create_issue_markup: revclouds + leaders from audit issues on a created markup layer (coloured by severity), rectangle from handles,
     dryRun rolled back, atomic refusal for an issue without a location, locked markup layer refused
  M  structural_tag_members (preview, apply → texts on S-ANNO-TEXT, existing mark kept, dryRun) + structural_generate_member_schedule
     (rows; writeTable → one ACAD_TABLE)
  D  undo: U after a REGEN boundary reverts the last batch (COM)
The PowerShell wrapper (run-aec-edit-tools-live.ps1) starts AutoCAD, ticks the opt-in and calls this. Prints PASS/FAIL lines and a JSON summary.
"""
import argparse, json, os, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, short, utf8_console  # noqa: E402

utf8_console()
CL = Checklist()
check, save = CL.check, CL.save

SCENE = r'''
double Du(double mm) => units.ToDrawing(mm);
Point3d P(double x, double y) => new Point3d(Du(x), Du(y), 0);
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
ObjectId Layer(string name, short color, bool locked = false, bool frozen = false)
{
    if (lt.Has(name)) return lt[name];
    var rec = new LayerTableRecord { Name = name, Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, color), IsLocked = locked, IsFrozen = frozen };
    var id = lt.Add(rec); tr.AddNewlyCreatedDBObject(rec, true); return id;
}
var col = Layer("S-COL", 1); var beam = Layer("S-BEAM", 2); var wall = Layer("A-WALL", 3); var txt = Layer("A-TEXT", 7); var door = Layer("A-DOOR", 5);
var locked = Layer("LOCKED", 6, locked: true); var frozen = Layer("FROZEN", 8, frozen: true);
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
var handles = new Dictionary<string, string>();
string Add(string key, Entity e, ObjectId layer)
{
    e.LayerId = layer; ms.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); handles[key] = e.Handle.ToString(); return handles[key];
}
Polyline Rect(double x, double y, double w, double h, bool closed = true)
{
    var pl = new Polyline(4);
    pl.AddVertexAt(0, new Point2d(Du(x), Du(y)), 0, 0, 0); pl.AddVertexAt(1, new Point2d(Du(x + w), Du(y)), 0, 0, 0);
    pl.AddVertexAt(2, new Point2d(Du(x + w), Du(y + h)), 0, 0, 0); pl.AddVertexAt(3, new Point2d(Du(x), Du(y + h)), 0, 0, 0);
    pl.Closed = closed; return pl;
}
Add("room", Rect(0, 0, 8000, 6000), wall);
Add("col", Rect(1000, 1000, 400, 400), col);
Add("open", Rect(10000, 0, 2000, 1000, false), wall);
Add("beam", new Line(P(1400, 1200), P(7000, 1200)), beam);
Add("text", new DBText { Position = P(2000, 4000), Height = Du(200), TextString = "OFFICE 01" }, txt);
Add("lockedLine", new Line(P(0, 9000), P(3000, 9000)), locked);
Add("circle", new Circle(P(5000, 3000), Vector3d.ZAxis, Du(600)), wall);
// a block definition with one attribute, inserted once with MARK = D01
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
var def = new BlockTableRecord { Name = "DOOR-TEST" };
var defId = bt.Add(def); tr.AddNewlyCreatedDBObject(def, true);
var doorLine = new Line(new Point3d(0, 0, 0), new Point3d(Du(900), 0, 0)); def.AppendEntity(doorLine); tr.AddNewlyCreatedDBObject(doorLine, true);
var attDef = new AttributeDefinition(new Point3d(0, Du(100), 0), "D00", "MARK", "Door mark", db.Textstyle) { Height = Du(150) };
def.AppendEntity(attDef); tr.AddNewlyCreatedDBObject(attDef, true);
var br = new BlockReference(P(3000, 0), defId);
Add("door", br, door);
var attRef = new AttributeReference(); attRef.SetAttributeFromBlock(attDef, br.BlockTransform); attRef.TextString = "D01";
br.AttributeCollection.AppendAttribute(attRef); tr.AddNewlyCreatedDBObject(attRef, true);
handles["attdef"] = attDef.Handle.ToString(); handles["blockLine"] = doorLine.Handle.ToString();
// a second block whose attribute definition lives on the locked layer: its references carry attributes that cannot be edited
var def2 = new BlockTableRecord { Name = "DOOR-LOCKATT" };
var def2Id = bt.Add(def2); tr.AddNewlyCreatedDBObject(def2, true);
var line2 = new Line(new Point3d(0, 0, 0), new Point3d(Du(900), 0, 0)); def2.AppendEntity(line2); tr.AddNewlyCreatedDBObject(line2, true);
var attDef2 = new AttributeDefinition(new Point3d(0, Du(100), 0), "R0", "REV", "Revision", db.Textstyle) { Height = Du(150), LayerId = locked };
def2.AppendEntity(attDef2); tr.AddNewlyCreatedDBObject(attDef2, true);
var br2 = new BlockReference(P(9000, 0), def2Id);
Add("doorLocked", br2, door);
var attRef2 = new AttributeReference(); attRef2.SetAttributeFromBlock(attDef2, br2.BlockTransform); attRef2.TextString = "R0"; attRef2.LayerId = locked;
br2.AttributeCollection.AppendAttribute(attRef2); tr.AddNewlyCreatedDBObject(attRef2, true);
return handles;
'''

# Writes a DWG the xref step can attach: WBLOCK of the column outline into a new database, saved beside the run.
WBLOCK = r'''
var id = db.GetObjectId(false, new Handle(Convert.ToInt64(args.Str("handle"), 16)), 0);
var wdb = db.Wblock(new ObjectIdCollection(new[] { id }), Point3d.Origin);
try { wdb.SaveAs(args.Str("path"), DwgVersion.Current); } finally { wdb.Dispose(); }
return args.Str("path");
'''

COUNT = "var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead); var n = 0; foreach (ObjectId id in ms) if (!id.IsErased) n++; return n;"


def value(r):
    return r.get("value") if isinstance(r, dict) else None


def near(a, b, eps=0.05):
    return a is not None and b is not None and abs(a - b) <= eps


def acad_com(script):
    expected = os.environ.get("HP_HARNESS_ACAD_PID", "")
    if not expected:
        return False
    ps = ("$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); "
          f"if ($ids.Count -ne 1 -or [string]$ids[0] -ne '{expected}') {{ throw \"refusing COM: acad pids $ids, harness owns '{expected}'\" }}; "
          "$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " + script)
    subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=90)
    return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", required=True)
    ap.add_argument("--out", default="")
    a = ap.parse_args()
    CL.out_dir = a.out or None
    out_dir = os.path.abspath(a.out or HERE)

    s = Server(a.exe, name="autocad-aec-edit")
    try:
        init = s.initialize()
        check("initialize", init.get("serverInfo", {}).get("name") == "HPAutoCad MCP", short(init.get("serverInfo")))
        names = s.tools()
        edit_tools = ["create_entities_batch", "update_entities_batch", "manage_blocks_attributes", "manage_annotations", "manage_hatches", "manage_xrefs", "create_issue_markup", "structural_tag_members", "structural_generate_member_schedule"]
        check("tools/list holds the 9 AEC write seeds (+ the others)", all(n in names for n in edit_tools) and len(names) >= 47, f"{len(names)} tools")
        raw_tool = s.tool  # the unparsed run (changed / rolledBack) beside the tool value

        def count():
            return value(s.tool("execute_autocad_code", {"code": COUNT, "transaction": "none", "label": "count"}, timeout=60))

        def query(handle, detail=False):
            q = value(s.tool("query_entities", {"filter": {"handles": [handle]}, "mode": "detail" if detail else "summary"})) or {}
            return (q.get("items") or [{}])[0], q

        # ---- S: scene -------------------------------------------------------------------------------------------------
        scene = s.tool("execute_autocad_code", {"code": SCENE, "transaction": "auto", "label": "aec edit scene", "timeoutSeconds": 60}, timeout=120)
        h = value(scene) or {}
        check("S scene drawn (9 entities + 2 inner handles, locked + frozen layers, DOOR-TEST + DOOR-LOCKATT blocks)", not scene.get("isError") and len(h) == 11, f"handles={len(h)} {short(scene.get('message'))}")
        save("scene-handles", h)
        if not h:
            return CL.finish("aec-edit-tools-live")
        base = count()

        # ---- C: create_entities_batch ---------------------------------------------------------------------------------------
        items = [
            {"type": "polyline", "layer": "S-COL", "closed": True, "points": [{"x": 6000, "y": 1000}, {"x": 6400, "y": 1000}, {"x": 6400, "y": 1400}, {"x": 6000, "y": 1400}]},
            {"type": "line", "layer": "S-BEAM", "start": {"x": 1400, "y": 4800}, "end": {"x": 6000, "y": 4800}, "colorIndex": 1},
            {"type": "circle", "center": {"x": 4000, "y": 3000}, "radiusMm": 300, "color": "#00FF00"},
            {"type": "text", "layer": "A-TEXT", "text": "C2", "position": {"x": 6450, "y": 1100}, "heightMm": 150},
            {"type": "arc", "center": {"x": 9000, "y": 3000}, "radiusMm": 500, "startAngleDeg": 0, "endAngleDeg": 90},
        ]
        c = value(s.tool("create_entities_batch", {"items": items})) or {}
        created = [i.get("handle") for i in c.get("items", [])]
        check("C atomic batch of 5 types -> createdCount 5, handles in input order, +5 entities",
              c.get("success") is True and c.get("createdCount") == 5 and len(created) == 5 and all(created) and [i.get("type") for i in c["items"]] == ["LWPOLYLINE", "LINE", "CIRCLE", "TEXT", "ARC"] and count() == base + 5, short(c, 300))
        line_item, _ = query(created[1], detail=True)
        check("C created line has the layer, colour and geometry asked for", line_item.get("layer") == "S-BEAM" and str(line_item.get("color")) in ("1", "Red") and near((line_item.get("geometry") or {}).get("lengthMm"), 4600, 0.01), short(line_item, 300))
        n0 = count()
        refused = value(s.tool("create_entities_batch", {"items": [{"type": "line", "start": {"x": 0, "y": 0}, "end": {"x": 1, "y": 0}}, {"type": "line", "layer": "LOCKED", "start": {"x": 0, "y": 0}, "end": {"x": 1, "y": 0}}]})) or {}
        check("C atomic + one item on a locked layer -> refused: success false, createdCount 0, LAYER_LOCKED, nothing created",
              refused.get("success") is False and refused.get("createdCount") == 0 and any(e.get("code") == "LAYER_LOCKED" for e in refused.get("errors", [])) and (refused.get("summary") or {}).get("refused") is True and count() == n0, short(refused, 300))
        partial = value(s.tool("create_entities_batch", {"atomic": False, "items": [{"type": "line", "start": {"x": 0, "y": 7000}, "end": {"x": 1000, "y": 7000}}, {"type": "line", "layer": "LOCKED", "start": {"x": 0, "y": 0}, "end": {"x": 1, "y": 0}}, {"type": "blob"}]})) or {}
        check("C atomic false -> 1 created, LAYER_LOCKED + INVALID_ARGUMENT listed per item, items[0].ok",
              partial.get("success") is False and partial.get("createdCount") == 1 and partial["items"][0].get("ok") is True and partial["items"][1].get("error", {}).get("code") == "LAYER_LOCKED" and partial["items"][2].get("error", {}).get("code") == "INVALID_ARGUMENT" and count() == n0 + 1, short(partial.get("items"), 300))
        frozen = value(s.tool("create_entities_batch", {"items": [{"type": "line", "layer": "FROZEN", "start": {"x": 0, "y": 8000}, "end": {"x": 1000, "y": 8000}}]})) or {}
        check("C frozen layer -> created with a warning", frozen.get("createdCount") == 1 and any("frozen" in w for w in frozen.get("warnings", [])), short(frozen.get("warnings")))
        blk = value(s.tool("create_entities_batch", {"items": [{"type": "blockReference", "blockName": "DOOR-TEST", "layer": "A-DOOR", "position": {"x": 5000, "y": 0}, "rotationDeg": 90, "attributes": {"MARK": "D02"}}, {"type": "dimension", "kind": "aligned", "p1": {"x": 1400, "y": 1200}, "p2": {"x": 7000, "y": 1200}, "dimLinePoint": {"x": 4000, "y": 600}}]})) or {}
        door2 = (blk.get("items") or [{}])[0].get("handle")
        ra = value(s.tool("manage_blocks_attributes", {"op": "readAttributes", "handles": [door2 or "0"]})) or {}
        check("C block reference with attributes + aligned dimension -> MARK D02 readable, DIMENSION created",
              blk.get("createdCount") == 2 and ((ra.get("items") or [{}])[0].get("attributes") or {}).get("MARK") == "D02" and blk["items"][1].get("type") == "DIMENSION", short(ra.get("items"), 200))
        n1 = count()
        dry = s.tool("create_entities_batch", {"items": [{"type": "line", "start": {"x": 0, "y": 0}, "end": {"x": 100, "y": 0}}, {"type": "circle", "center": {"x": 0, "y": 0}, "radiusMm": 5}], "dryRun": True})
        dv = value(dry) or {}
        check("C dryRun -> envelope says 2 created, run reports rolledBack + changed.added 2, nothing persists",
              dv.get("createdCount") == 2 and dry.get("rolledBack") is True and (dry.get("changed") or {}).get("added") == 2 and count() == n1, f"changed={short(dry.get('changed'))} rolledBack={dry.get('rolledBack')} count {n1}->{count()}")
        lay = value(s.tool("create_entities_batch", {"space": "Layout1", "items": [{"type": "text", "text": "SHEET NOTE", "position": {"x": 10, "y": 10}, "heightMm": 5}]})) or {}
        layq = value(s.tool("query_entities", {"filter": {"handles": [(lay.get("items") or [{}])[0].get("handle") or "0"]}})) or {}
        check("C space Layout1 -> created in paper space", lay.get("createdCount") == 1 and ((layq.get("items") or [{}])[0].get("space") or "").lower().startswith("layout1"), short(layq.get("items"), 200))
        empty = s.tool("create_entities_batch", {"items": []})
        check("C empty items -> ArgumentException", empty.get("isError") and "items" in (empty.get("message") or ""), short(empty.get("message")))
        save("create", {"batch": c, "refused": refused, "partial": partial, "frozen": frozen, "block": blk, "dry": dry})

        # ---- U: update_entities_batch ---------------------------------------------------------------------------------------
        u = value(s.tool("update_entities_batch", {"handles": [created[0], created[1]], "set": {"layer": "A-WALL", "colorIndex": 3}})) or {}
        moved, _ = query(created[0])
        check("U shared set over 2 handles -> modifiedCount 2, layer changed", u.get("success") is True and u.get("modifiedCount") == 2 and moved.get("layer") == "A-WALL" and all("layer" in (i.get("changed") or []) for i in u.get("items", [])), short(u, 300))
        u2 = value(s.tool("update_entities_batch", {"items": [
            {"handle": h["text"], "set": {"text": "OFFICE 02", "heightMm": 300}},
            {"handle": h["beam"], "set": {"geometry": {"end": {"x": 7400, "y": 1200}}}},
            {"handle": h["col"], "set": {"rotate": {"angleDeg": 45}}},
            {"handle": created[2], "set": {"move": {"dx": 1000, "dy": 0}, "geometry": {"radiusMm": 250}}},
            {"handle": h["room"], "set": {"geometry": {"points": [{"x": 0, "y": 0}, {"x": 8000, "y": 0}, {"x": 8000, "y": 6000}, {"x": 4000, "y": 7000}, {"x": 0, "y": 6000}], "closed": True}}},
        ]})) or {}
        t, _ = query(h["text"])
        b, _ = query(h["beam"], detail=True)
        circ, _ = query(created[2], detail=True)
        room, _ = query(h["room"], detail=True)
        check("U per-item text/height, line end, rotate, move + radius, polyline re-vertexed (5 points)",
              u2.get("modifiedCount") == 5 and t.get("text") == "OFFICE 02" and near((b.get("geometry") or {}).get("lengthMm"), 6000, 0.01) and near((circ.get("boundsMm") or {}).get("min", {}).get("x"), 4750, 0.01)
              and (room.get("geometry") or {}).get("vertexCount") == 5 and near((room.get("geometry") or {}).get("areaMm2"), 48_000_000 + 4_000_000, 1), f"text={t.get('text')} beam={short(b.get('geometry'), 80)} circle={short(circ.get('boundsMm'), 90)} room={short(room.get('geometry'), 120)}")
        lk = value(s.tool("update_entities_batch", {"atomic": False, "items": [{"handle": h["lockedLine"], "set": {"colorIndex": 1}}, {"handle": created[3], "set": {"text": "C3"}}]})) or {}
        check("U locked layer -> LAYER_LOCKED for that item, the other applied (atomic false)", lk.get("modifiedCount") == 1 and lk["items"][0].get("error", {}).get("code") == "LAYER_LOCKED" and lk["items"][1].get("ok") is True, short(lk.get("items"), 300))
        before, _ = query(created[3])
        at = value(s.tool("update_entities_batch", {"items": [{"handle": created[3], "set": {"text": "C4"}}, {"handle": h["lockedLine"], "set": {"colorIndex": 1}}]})) or {}
        after_, _ = query(created[3])
        check("U atomic + locked -> refused, modifiedCount 0, the valid item untouched", at.get("success") is False and at.get("modifiedCount") == 0 and (at.get("summary") or {}).get("refused") is True and after_.get("text") == before.get("text") == "C3", short(at.get("summary")))
        s.tool("execute_autocad_code", {"code": f"var id = db.GetObjectId(false, new Handle(0x{created[4]}), 0); var e = (Entity)tr.GetObject(id, OpenMode.ForWrite); e.Erase(); return 1;", "transaction": "auto", "label": "erase arc"}, timeout=60)
        bad = value(s.tool("update_entities_batch", {"atomic": False, "items": [{"handle": created[4], "set": {"colorIndex": 2}}, {"handle": "ZZZZ", "set": {"colorIndex": 2}}, {"handle": h["beam"], "set": {"heightMm": 100}}]})) or {}
        codes = [i.get("error", {}).get("code") for i in bad.get("items", [])]
        check("U erased -> ERASED, bad handle -> INVALID_HANDLE, heightMm on a line -> UNSUPPORTED_ENTITY", codes == ["ERASED", "INVALID_HANDLE", "UNSUPPORTED_ENTITY"], short(codes))
        before2, _ = query(created[3])
        ur = s.tool("update_entities_batch", {"items": [{"handle": created[3], "set": {"text": "C5"}}, {"handle": h["beam"], "set": {"heightMm": 100, "colour": 1}}]})
        urv = value(ur) or {}
        after2, _ = query(created[3])
        check("U atomic + bad key on item 1 -> refused structurally (UNSUPPORTED_ENTITY + unknown key), item 0 untouched, run changed.modified 0",
              urv.get("success") is False and (urv.get("summary") or {}).get("refused") is True and urv["items"][1].get("error", {}).get("code") in ("UNSUPPORTED_ENTITY", "INVALID_ARGUMENT") and "+1 more" in urv["items"][1]["error"]["message"] and after2.get("text") == before2.get("text") and (ur.get("changed") or {}).get("modified") == 0,
              f"changed={short(ur.get('changed'))} err={short(urv['items'][1].get('error'), 200)}")
        dup = value(s.tool("update_entities_batch", {"handles": [created[3], created[3]], "set": {"colorIndex": 5}})) or {}
        check("U duplicate handle -> applied once with a warning", dup.get("modifiedCount") == 1 and any("more than once" in w for w in dup.get("warnings", [])), short(dup.get("warnings")))
        blk_inner = value(s.tool("update_entities_batch", {"atomic": False, "items": [{"handle": h["blockLine"], "set": {"colorIndex": 1}}]})) or {}
        check("U entity inside a block definition -> UNSUPPORTED_ENTITY naming the definition", blk_inner["items"][0].get("error", {}).get("code") == "UNSUPPORTED_ENTITY" and "DOOR-TEST" in blk_inner["items"][0]["error"]["message"], short(blk_inner.get("items"), 200))
        udry = s.tool("update_entities_batch", {"handles": [created[3]], "set": {"text": "DRY"}, "dryRun": True})
        udry_after, _ = query(created[3])
        check("U dryRun -> envelope modified 1, rolledBack, text unchanged", (value(udry) or {}).get("modifiedCount") == 1 and udry.get("rolledBack") is True and udry_after.get("text") == after2.get("text"), f"rolledBack={udry.get('rolledBack')} text={udry_after.get('text')}")
        nothing = s.tool("update_entities_batch", {"handles": [h["beam"]]})
        check("U handles without set -> ArgumentException naming the keys", nothing.get("isError") and "set" in (nothing.get("message") or ""), short(nothing.get("message")))
        save("update", {"shared": u, "items": u2, "locked": lk, "atomic": at, "bad": bad})

        # ---- B: manage_blocks_attributes ----------------------------------------------------------------------------------
        defs = value(s.tool("manage_blocks_attributes", {"op": "listDefinitions", "namePattern": "DOOR*"})) or {}
        d0 = (defs.get("items") or [{}])[0]
        d0 = next((d for d in defs.get("items", []) if d.get("name") == "DOOR-TEST"), {})
        check("B listDefinitions DOOR* -> 2 definitions, DOOR-TEST with attributeTags [MARK], 2 references, not dynamic", defs.get("count") == 2 and d0.get("attributeTags") == ["MARK"] and d0.get("referenceCount") == 2 and d0.get("isDynamic") is False, short(d0))
        refs = value(s.tool("manage_blocks_attributes", {"op": "findReferences", "filter": {"blockNames": ["DOOR-*"], "layers": ["A-DOOR"]}})) or {}
        rot = {r["handle"]: r for r in refs.get("items", [])}
        check("B findReferences DOOR-* on A-DOOR -> 3 with position, rotation 90 on the second, attributes", refs.get("count") == 3 and near(rot.get(door2, {}).get("rotationDeg"), 90) and (rot.get(h["door"], {}).get("attributes") or {}).get("MARK") == "D01", short(refs.get("items"), 300))
        ins = value(s.tool("manage_blocks_attributes", {"op": "insert", "insert": {"blockName": "DOOR-TEST", "position": {"x": 7000, "y": 0}, "layer": "A-DOOR", "attributes": {"MARK": "D03"}}})) or {}
        door3 = (ins.get("items") or [{}])[0].get("handle")
        check("B insert -> created handle, attributesSet 1", ins.get("createdCount") == 1 and door3 and (ins.get("summary") or {}).get("attributesSet") == 1, short(ins.get("summary")))
        wl = s.tool("manage_blocks_attributes", {"op": "writeAttributes", "handles": [h["doorLocked"]], "attributes": {"REV": "B"}})
        wlv = value(wl) or {}
        check("B writeAttributes on an attribute whose own layer is locked -> LAYER_LOCKED structured, modifiedCount 0, changed.modified 0",
              not wl.get("isError") and wlv.get("success") is False and wlv.get("modifiedCount") == 0 and (wlv.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED" and "REV" in wlv["errors"][0].get("message", "") and (wl.get("changed") or {}).get("modified") == 0, short(wlv.get("errors"), 250))
        ins_locked = s.tool("manage_blocks_attributes", {"op": "insert", "insert": {"blockName": "DOOR-TEST", "position": {"x": 0, "y": 0}, "layer": "LOCKED"}})
        ilv = value(ins_locked) or {}
        check("B insert on a locked layer -> structural LAYER_LOCKED refusal, nothing created", not ins_locked.get("isError") and ilv.get("success") is False and ilv.get("createdCount") == 0 and (ilv.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED", short(ilv, 200))
        ins_tag = value(s.tool("manage_blocks_attributes", {"op": "insert", "insert": {"blockName": "DOOR-TEST", "position": {"x": 11000, "y": 0}, "attributes": {"MARC": "D05"}}})) or {}
        check("B insert with a misspelt tag -> created, attributesSet 0, warning names the tag", ins_tag.get("createdCount") == 1 and (ins_tag.get("summary") or {}).get("attributesSet") == 0 and any("MARC" in w for w in ins_tag.get("warnings", [])), short(ins_tag.get("warnings")))
        wa = value(s.tool("manage_blocks_attributes", {"op": "writeAttributes", "handles": [door3], "attributes": {"MARK": "D09", "NOPE": "x"}})) or {}
        ra2 = value(s.tool("manage_blocks_attributes", {"op": "readAttributes", "handles": [door3, h["col"]]})) or {}
        check("B writeAttributes -> MARK D09, unknown tag warned; readAttributes refuses a polyline with NOT_AN_ENTITY-style error",
              wa.get("modifiedCount") == 1 and any("NOPE" in w for w in wa.get("warnings", [])) and ((ra2.get("items") or [{}])[0].get("attributes") or {}).get("MARK") == "D09" and any(e.get("handle") == h["col"] for e in ra2.get("errors", [])), short(ra2, 300))
        bu = value(s.tool("manage_blocks_attributes", {"op": "batchUpdateAttributes", "filter": {"blockNames": ["DOOR-TEST"]}, "attributes": {"MARK": "DX"}})) or {}
        ra3 = value(s.tool("manage_blocks_attributes", {"op": "readAttributes", "handles": [h["door"], door2, door3]})) or {}
        check("B batchUpdateAttributes by filter -> 4 DOOR-TEST references now MARK = DX", bu.get("modifiedCount") == 4 and all((i.get("attributes") or {}).get("MARK") == "DX" for i in ra3.get("items", [])), short(bu.get("summary")))
        dyn = value(s.tool("manage_blocks_attributes", {"op": "inspectDynamic", "handles": [door3]})) or {}
        setd = value(s.tool("manage_blocks_attributes", {"op": "setDynamic", "handles": [door3], "properties": {"Distance1": 100}})) or {}
        badop = s.tool("manage_blocks_attributes", {"op": "explode"})
        check("B inspectDynamic on a plain block -> isDynamic false; setDynamic refused structurally (UNSUPPORTED_ENTITY); unknown op lists the ops",
              (dyn.get("items") or [{}])[0].get("isDynamic") is False and setd.get("success") is False and (setd.get("errors") or [{}])[0].get("code") == "UNSUPPORTED_ENTITY" and badop.get("isError") and "listDefinitions" in (badop.get("message") or ""), short(setd.get("errors"), 200))
        bdry = s.tool("manage_blocks_attributes", {"op": "writeAttributes", "handles": [door3], "attributes": {"MARK": "DRY"}, "dryRun": True})
        ra4 = value(s.tool("manage_blocks_attributes", {"op": "readAttributes", "handles": [door3]})) or {}
        check("B writeAttributes dryRun -> rolled back, MARK still DX", bdry.get("rolledBack") is True and ((ra4.get("items") or [{}])[0].get("attributes") or {}).get("MARK") == "DX", short(ra4.get("items"), 120))
        save("blocks", {"defs": defs, "refs": refs, "insert": ins, "write": wa, "read": ra2, "batch": bu, "dynamic": dyn, "locked": wlv, "tag": ins_tag})

        # ---- A: manage_annotations -----------------------------------------------------------------------------------------
        ann = {}
        for title, annotation in [
            ("text", {"type": "text", "text": "NOTE 1", "position": {"x": 0, "y": -1000}, "heightMm": 200, "layer": "A-TEXT"}),
            ("mtext", {"type": "mtext", "text": "GENERAL NOTES\\PLine two", "position": {"x": 0, "y": -2000}, "heightMm": 150, "widthMm": 3000}),
            ("aligned", {"type": "dimension", "kind": "aligned", "p1": {"x": 0, "y": 0}, "p2": {"x": 8000, "y": 0}, "dimLinePoint": {"x": 4000, "y": -500}}),
            ("angular", {"type": "dimension", "kind": "angular", "vertex": {"x": 0, "y": 0}, "p1": {"x": 1000, "y": 0}, "p2": {"x": 0, "y": 1000}, "arcPoint": {"x": 500, "y": 500}}),
            ("radial", {"type": "dimension", "kind": "radial", "center": {"x": 5000, "y": 3000}, "chordPoint": {"x": 5600, "y": 3000}, "leaderLengthMm": 300}),
            ("diameter", {"type": "dimension", "kind": "diameter", "chordPoint": {"x": 4400, "y": 3000}, "farChordPoint": {"x": 5600, "y": 3000}, "leaderLengthMm": 300}),
            ("mleader", {"type": "mleader", "text": "C1 400x400", "arrowPoint": {"x": 1200, "y": 1200}, "landingPoint": {"x": 2500, "y": 2500}, "heightMm": 150}),
        ]:
            ann[title] = value(s.tool("manage_annotations", {"op": "create", "annotation": annotation})) or {}
        m = {k: (v.get("summary") or {}).get("measurement") or {} for k, v in ann.items()}
        check("A create text, mtext, aligned 8000, angular 90°, radial 600, diameter 1200, mleader -> all created with measurements",
              all(v.get("createdCount") == 1 for v in ann.values()) and near(m["aligned"].get("lengthMm"), 8000, 0.01) and near(m["angular"].get("angleDeg"), 90, 0.001) and near(m["radial"].get("lengthMm"), 600, 0.01) and near(m["diameter"].get("lengthMm"), 1200, 0.01) and (ann["mleader"].get("summary") or {}).get("type") == "MLeader",
              short(m, 300))
        th = (ann["text"].get("items") or [{}])[0].get("handle")
        up = value(s.tool("manage_annotations", {"op": "update", "handles": [th], "set": {"text": "NOTE 1A", "heightMm": 250, "rotationDeg": 15}})) or {}
        tq, _ = query(th)
        check("A update text -> content, height and rotation applied", up.get("modifiedCount") == 1 and tq.get("text") == "NOTE 1A" and set(up["items"][0].get("changed") or []) >= {"text", "heightMm", "rotationDeg"}, short(up.get("items")))
        al = s.tool("manage_annotations", {"op": "create", "annotation": {"type": "text", "text": "X", "position": {"x": 0, "y": 0}, "heightMm": 100, "layer": "LOCKED"}})
        alv = value(al) or {}
        check("A create on a locked layer -> structural LAYER_LOCKED refusal", not al.get("isError") and alv.get("success") is False and (alv.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED", short(alv, 200))
        bup = value(s.tool("manage_annotations", {"op": "batchUpdate", "items": [{"handle": th, "set": {"colorIndex": 2}}, {"handle": (ann["mtext"].get("items") or [{}])[0].get("handle"), "set": {"colorIndex": 2}}]})) or {}
        check("A batchUpdate -> 2 modified", bup.get("modifiedCount") == 2, short(bup.get("summary")))
        adry = s.tool("manage_annotations", {"op": "update", "handles": [th], "set": {"text": "DRY"}, "dryRun": True})
        tq2, _ = query(th)
        check("A update dryRun -> rolled back, text unchanged", adry.get("rolledBack") is True and tq2.get("text") == "NOTE 1A", f"text={tq2.get('text')}")
        attdef = value(s.tool("manage_annotations", {"op": "delete", "handles": [h["attdef"]]})) or {}
        check("A delete of an attribute definition inside a block -> UNSUPPORTED_ENTITY, nothing deleted", attdef.get("deletedCount") == 0 and (attdef.get("errors") or [{}])[0].get("code") == "UNSUPPORTED_ENTITY", short(attdef.get("errors"), 200))
        dl = value(s.tool("manage_annotations", {"op": "delete", "handles": [th, h["beam"]]})) or {}
        dl2 = value(s.tool("manage_annotations", {"op": "delete", "handles": [th, (ann["mtext"].get("items") or [{}])[0].get("handle")]})) or {}
        gone, _ = query(th)
        check("A delete refuses a LINE (UNSUPPORTED_ENTITY, atomic: nothing deleted), then deletes text + mtext",
              dl.get("success") is False and dl.get("deletedCount") == 0 and any(e.get("code") == "UNSUPPORTED_ENTITY" for e in dl.get("errors", [])) and dl2.get("deletedCount") == 2 and not gone, short(dl2))
        save("annotations", {"create": ann, "update": up, "delete": [dl, dl2]})

        # ---- H: manage_hatches ---------------------------------------------------------------------------------------------
        hb = value(s.tool("manage_hatches", {"op": "create", "hatch": {"boundaryHandles": [created[0]], "pattern": "ANSI31", "scale": 25, "layer": "S-COL"}})) or {}
        hbq, _ = query((hb.get("items") or [{}])[0].get("handle") or "0", detail=True)
        check("H create from a closed polyline, ANSI31 -> HATCH with area 160 000 mm2, on layer S-COL", hb.get("createdCount") == 1 and (hb.get("summary") or {}).get("pattern") == "ANSI31" and near((hb.get("summary") or {}).get("areaMm2"), 160000, 1) and hbq.get("layer") == "S-COL", f"{short(hb.get('summary'), 160)} layer={hbq.get('layer')}")
        hc = value(s.tool("manage_hatches", {"op": "create", "hatch": {"points": [{"x": 13000, "y": 0}, {"x": 15000, "y": 0}, {"x": 14000, "y": 1500}], "pattern": "ANSI32", "scale": 10, "hatchStyle": "outer", "colorIndex": 4, "layer": "A-WALL"}})) or {}
        hcq, _ = query((hc.get("items") or [{}])[0].get("handle") or "0", detail=True)
        check("H create from a polygon with hatchStyle outer, colour 4 -> area 1 500 000, colour and layer kept (SetDatabaseDefaults first)", hc.get("createdCount") == 1 and near((hc.get("summary") or {}).get("areaMm2"), 1_500_000, 1) and str(hcq.get("color")) == "4" and hcq.get("layer") == "A-WALL", f"{short(hc.get('summary'), 120)} color={hcq.get('color')} layer={hcq.get('layer')}")
        hl = s.tool("manage_hatches", {"op": "create", "hatch": {"boundaryHandles": [created[0]], "layer": "LOCKED"}})
        hlv = value(hl) or {}
        check("H create on a locked layer -> structural LAYER_LOCKED refusal, nothing created", not hl.get("isError") and hlv.get("success") is False and hlv.get("createdCount") == 0 and (hlv.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED", short(hlv, 200))
        db_ = value(s.tool("manage_hatches", {"op": "detectBoundary", "seedPoint": {"x": 1100, "y": 1100}})) or {}
        inner = [i.get("handle") for i in db_.get("items", [])]
        check("H detectBoundary inside the column -> column first (innermost), then the room", inner[:2] == [h["col"], h["room"]] and (db_.get("summary") or {}).get("innermost") == h["col"], short(db_.get("items"), 300))
        hs = value(s.tool("manage_hatches", {"op": "create", "hatch": {"seedPoint": {"x": 7000, "y": 5000}, "pattern": "SOLID", "colorIndex": 8}})) or {}
        check("H create from seedPoint -> SOLID over the room (52 000 000 mm2 after the re-vertex), warning names the boundary", hs.get("createdCount") == 1 and near((hs.get("summary") or {}).get("areaMm2"), 52_000_000, 1) and any(h["room"] in w for w in hs.get("warnings", [])), short(hs, 300))
        nc = value(s.tool("manage_hatches", {"op": "create", "hatch": {"boundaryHandles": [h["open"]]}})) or {}
        check("H open polyline boundary -> NOT_CLOSED, nothing created", nc.get("success") is False and nc.get("createdCount") == 0 and any(e.get("code") == "NOT_CLOSED" for e in nc.get("errors", [])), short(nc.get("errors")))
        hh = (hb.get("items") or [{}])[0].get("handle")
        hbad = value(s.tool("manage_hatches", {"op": "update", "handles": [hh], "set": {"colorIndex": 4, "layer": "NOPE"}})) or {}
        hbq2, _ = query(hh, detail=True)
        check("H update with one bad key -> whole update refused, colour unchanged", hbad.get("success") is False and hbad.get("modifiedCount") == 0 and str(hbq2.get("color")) == str(hbq.get("color")), f"{short(hbad.get('errors'), 160)} color {hbq.get('color')}->{hbq2.get('color')}")
        hu = value(s.tool("manage_hatches", {"op": "update", "handles": [hh], "set": {"pattern": "ANSI32", "angleDeg": 45, "scale": 20, "colorIndex": 4}})) or {}
        check("H update pattern/angle/scale/colour", hu.get("modifiedCount") == 1 and (hu.get("summary") or {}).get("pattern") == "ANSI32" and set(hu["items"][0].get("changed") or []) >= {"pattern", "angleDeg", "scale", "color"}, short(hu.get("items")))
        hd = value(s.tool("manage_hatches", {"op": "delete", "handles": [hh, h["col"]]})) or {}
        hd2 = value(s.tool("manage_hatches", {"op": "delete", "handles": [hh]})) or {}
        check("H delete refuses a polyline (atomic), then deletes the hatch", hd.get("deletedCount") == 0 and any(e.get("code") == "UNSUPPORTED_ENTITY" for e in hd.get("errors", [])) and hd2.get("deletedCount") == 1, short(hd2))
        ha = value(s.tool("manage_hatches", {"op": "create", "hatch": {"boundaryHandles": [created[0]], "pattern": "SOLID", "associative": True}})) or {}
        hah = (ha.get("items") or [{}])[0].get("handle")
        s.tool("update_entities_batch", {"handles": [created[0]], "set": {"move": {"dx": 0, "dy": 3000}}})
        acad_com("$a.ActiveDocument.SendCommand('_REGEN ')"); time.sleep(1)
        haq, _ = query(hah or "0")
        check("H associative hatch follows its moved boundary (bounds y 4000..4400 after a 3000 mm move)", ha.get("createdCount") == 1 and (ha.get("summary") or {}).get("associative") is True and near((haq.get("boundsMm") or {}).get("min", {}).get("y"), 4000, 1), short(haq.get("boundsMm"), 120))
        hdry = s.tool("manage_hatches", {"op": "create", "hatch": {"boundaryHandles": [created[0]], "pattern": "SOLID"}, "dryRun": True})
        check("H create dryRun -> rolled back", (value(hdry) or {}).get("createdCount") == 1 and hdry.get("rolledBack") is True, f"rolledBack={hdry.get('rolledBack')}")
        save("hatches", {"boundary": hb, "polygon": hc, "detect": db_, "seed": hs, "notClosed": nc, "update": hu, "bad": hbad, "associative": ha, "delete": [hd, hd2]})

        # ---- X: manage_xrefs -----------------------------------------------------------------------------------------------
        x0 = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        check("X list on a fresh drawing -> 0 xrefs", x0.get("count") == 0 and (x0.get("summary") or {}).get("xrefs") == 0, short(x0.get("summary")))
        xref_path = os.path.join(out_dir, "xref-column.dwg")
        if os.path.exists(xref_path):
            os.remove(xref_path)
        wb = s.tool("execute_autocad_code", {"code": WBLOCK, "transaction": "none", "label": "wblock xref", "args": {"handle": h["col"], "path": xref_path}}, timeout=120)
        check("X a DWG for the xref written by Wblock + SaveAs", not wb.get("isError") and os.path.exists(xref_path), short(wb.get("value") or {k: wb.get(k) for k in ("message", "diagnostics", "errors", "hint") if wb.get(k)}, 400))
        xa = value(s.tool("manage_xrefs", {"op": "attach", "attach": {"path": xref_path, "position": {"x": 20000, "y": 0}, "layer": "A-WALL"}})) or {}
        xb = value(s.tool("manage_xrefs", {"op": "attach", "attach": {"path": xref_path, "name": "XCOL-OVL", "position": {"x": 25000, "y": 0}, "overlay": True}})) or {}
        xl = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        by = {i["name"]: i for i in xl.get("items", [])}
        check("X attach + overlay attach -> 2 xrefs Resolved, found, referenceCount 1, overlay flagged",
              xa.get("createdCount") == 1 and xb.get("createdCount") == 1 and xl.get("count") == 2 and by.get("xref-column", {}).get("status") == "Resolved" and by.get("xref-column", {}).get("found") is True and by.get("XCOL-OVL", {}).get("overlay") is True and by.get("XCOL-OVL", {}).get("referenceCount") == 1, short(xl.get("items"), 400))
        xs = value(s.tool("manage_xrefs", {"op": "resolveStatus"})) or {}
        check("X resolveStatus -> both still Resolved", xs.get("count") == 2 and all(i.get("status") == "Resolved" for i in xs.get("items", [])), short(xs.get("summary")))
        xdr = s.tool("manage_xrefs", {"op": "attach", "attach": {"path": xref_path, "name": "XDRY", "position": {"x": 0, "y": 0}}, "dryRun": True})
        xl_dry = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        check("X attach dryRun -> rolled back, still 2 xrefs", xdr.get("rolledBack") is True and xl_dry.get("count") == 2, f"rolledBack={xdr.get('rolledBack')} count={xl_dry.get('count')}")
        xbadname = s.tool("manage_xrefs", {"op": "attach", "attach": {"path": xref_path, "name": "A/B", "position": {"x": 0, "y": 0}}})
        xrel = s.tool("manage_xrefs", {"op": "attach", "attach": {"path": "C:x.dwg", "position": {"x": 0, "y": 0}}})
        check("X invalid name and drive-relative path -> ArgumentException each", xbadname.get("isError") and "valid block name" in (xbadname.get("message") or "") and xrel.get("isError") and "fully qualified" in (xrel.get("message") or ""), short(xrel.get("message")))
        xdet_bad = value(s.tool("manage_xrefs", {"op": "detach", "names": ["XCOL-OVL", "NOT-AN-XREF"]})) or {}
        xl_bad = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        check("X detach with one unknown name -> refused whole, nothing detached", xdet_bad.get("success") is False and xdet_bad.get("deletedCount") == 0 and xl_bad.get("count") == 2, short(xdet_bad.get("summary")))
        xu = value(s.tool("manage_xrefs", {"op": "unload", "names": ["XCOL-OVL"]})) or {}
        xbind_refused = value(s.tool("manage_xrefs", {"op": "bind", "names": ["XCOL-OVL"]})) or {}
        xr = value(s.tool("manage_xrefs", {"op": "reload", "names": ["XCOL-OVL"]})) or {}
        check("X unload -> Unloaded; bind refused (INVALID_ARGUMENT, nothing bound); reload -> Resolved",
              xu.get("modifiedCount") == 1 and xu.get("affectedHandles") == [] and (xu.get("summary") or {}).get("xrefs", [{}])[0].get("status") == "Unloaded" and xbind_refused.get("success") is False and any(e.get("code") == "INVALID_ARGUMENT" for e in xbind_refused.get("errors", [])) and (xr.get("summary") or {}).get("xrefs", [{}])[0].get("status") == "Resolved", f"unload={short(xu.get('summary'), 100)} bind={short(xbind_refused.get('errors'), 160)} reload={short(xr.get('summary'), 100)}")
        xd = value(s.tool("manage_xrefs", {"op": "detach", "names": ["XCOL-OVL"]})) or {}
        xl2 = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        check("X detach -> 1 reference erased, 1 xref left", xd.get("deletedCount") == 1 and xl2.get("count") == 1, short(xd.get("summary")))
        xbind = value(s.tool("manage_xrefs", {"op": "bind", "names": ["xref-column"], "insertBind": True})) or {}
        xl3 = value(s.tool("manage_xrefs", {"op": "list"})) or {}
        local = value(s.tool("manage_blocks_attributes", {"op": "listDefinitions", "namePattern": "xref-column"})) or {}
        check("X bind (insert style) -> no xrefs left, xref-column is now a local block", xbind.get("success") is True and xl3.get("count") == 0 and local.get("count") == 1 and (local.get("items") or [{}])[0].get("isXref") is False, short(local.get("items"), 200))
        xmiss = s.tool("manage_xrefs", {"op": "attach", "attach": {"path": os.path.join(out_dir, "does-not-exist.dwg"), "position": {"x": 0, "y": 0}}})
        check("X attach of a missing file -> ArgumentException", xmiss.get("isError") and "not found" in (xmiss.get("message") or ""), short(xmiss.get("message")))
        save("xrefs", {"list0": x0, "attach": [xa, xb], "list": xl, "unload": xu, "bindRefused": xbind_refused, "reload": xr, "detach": xd, "bind": xbind, "local": local})

        # ---- K: issue markup -------------------------------------------------------------------------------------------------
        aud = value(s.tool("audit_aec_drawing", {"minSeverity": "warning", "limit": 30})) or {}
        # table-level findings (layer naming, unused layers) have nothing to cloud: pick issues that point somewhere
        picked = [i for i in aud.get("items", []) if i.get("handles") or i.get("locationMm")][:3]
        mk = value(s.tool("create_issue_markup", {"issues": picked, "style": "revcloud"})) or {}
        msum = mk.get("summary") or {}
        mq = value(s.tool("query_entities", {"filter": {"layers": ["HP-MCP-ISSUES"], "space": "all"}, "properties": ["type", "color"]})) or {}
        mtypes = sorted(i.get("type") for i in mq.get("items", []))
        check("K revcloud markup of 3 audit issues -> 3 clouds + 3 leaders on the created layer HP-MCP-ISSUES, coloured by severity",
              len(picked) == 3 and mk.get("createdCount") == 6 and msum.get("layerCreated") is True and len(msum.get("markups", [])) == 3 and all(m.get("leaderHandle") for m in msum["markups"])
              and mtypes == ["LWPOLYLINE"] * 3 + ["MULTILEADER"] * 3 and all(str(i.get("color")) in ("1", "2") for i in mq.get("items", [])), f"{short(msum, 200)} types={mtypes}")
        rect = value(s.tool("create_issue_markup", {"issues": [{"issueId": "RFI-1", "severity": "info", "handles": [h["col"], h["beam"]], "description": "check column/beam"}], "style": "rectangle", "withLeader": False})) or {}
        rq, _ = query((rect.get("items") or [{}])[0].get("handle") or "0", detail=True)
        check("K rectangle from two handles without a leader -> one closed polyline around both, info colour cyan", rect.get("createdCount") == 1 and (rq.get("geometry") or {}).get("closed") is True and (rq.get("boundsMm") or {}).get("min", {}).get("x", 1) < 1000 and (rq.get("boundsMm") or {}).get("max", {}).get("x", 0) > 7400 and str(rq.get("color")) == "4", short(rq.get("boundsMm"), 120))
        n_before = count()
        mdry = s.tool("create_issue_markup", {"issues": [{"issueId": "DRY", "locationMm": {"x": 0, "y": 0}}], "dryRun": True})
        check("K dryRun -> 2 would be created, rolled back", (value(mdry) or {}).get("createdCount") == 2 and mdry.get("rolledBack") is True and count() == n_before, f"rolledBack={mdry.get('rolledBack')}")
        mskip = value(s.tool("create_issue_markup", {"issues": [{"issueId": "OK", "locationMm": {"x": 0, "y": 0}}, {"issueId": "TABLE", "type": "layer_naming", "layer": "walls_old"}]})) or {}
        check("K a table-level finding (no location, no handles) is skipped with a warning, the other issue drawn", mskip.get("success") is True and mskip.get("createdCount") == 2 and (mskip.get("summary") or {}).get("skipped") == 1 and any("TABLE" in w for w in mskip.get("warnings", [])), short(mskip.get("summary"), 200))
        mref = value(s.tool("create_issue_markup", {"issues": [{"issueId": "OK", "locationMm": {"x": 0, "y": 0}}, {"issueId": "LOST", "handles": ["ZZZZ"]}]})) or {}
        check("K atomic + an issue whose handles do not resolve -> refused, nothing drawn, error names it", mref.get("success") is False and mref.get("createdCount") == 0 and "LOST" in (mref.get("errors") or [{}])[0].get("message", "") and count() == n_before + 2, short(mref.get("errors"), 200))
        mlock = value(s.tool("create_issue_markup", {"issues": [{"issueId": "X", "locationMm": {"x": 0, "y": 0}}], "layer": "LOCKED"})) or {}
        check("K locked markup layer -> structural LAYER_LOCKED refusal", mlock.get("success") is False and (mlock.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED", short(mlock.get("errors"), 200))
        gap = next((i for i in aud.get("items", []) if i["type"] == "endpoint_gap"), None) or next((i for i in (value(s.tool("audit_aec_drawing", {"sections": ["geometry"]})) or {}).get("items", []) if i["type"] == "endpoint_gap"), None)
        mg = value(s.tool("create_issue_markup", {"issues": [gap] if gap else [{"issueId": "GAP", "locationMm": {"x": 15007, "y": 6000}, "handles": [h["beam"], h["col"]]}], "withLeader": False})) or {}
        check("K an issue with locationMm and handles keeps radius 500 at the location (not the handles' extents)", near((mg.get("summary") or {}).get("markups", [{}])[0].get("radiusMm"), 500) and (mg.get("summary") or {}).get("markups", [{}])[0].get("sizedByHandles") is False, short(mg.get("summary"), 200))
        sheet = value(s.tool("cad_standards_check", {"filter": {"space": "Layout1"}, "checks": ["layer_zero"]})) or {}
        sheet_issue = next((i for i in sheet.get("items", []) if i["type"] == "layer_zero"), None)
        mp = value(s.tool("create_issue_markup", {"issues": [sheet_issue] if sheet_issue else [], "withLeader": False})) if sheet_issue else {}
        mpq = value(s.tool("query_entities", {"filter": {"layers": ["HP-MCP-ISSUES"], "space": "Layout1"}})) or {}
        check("K a paper-space finding (SHEET NOTE on layer 0 in Layout1; filter.space alone is a filter, wholeDrawing false) is drawn on Layout1 with space auto",
              (sheet.get("summary") or {}).get("wholeDrawing") is False and sheet_issue is not None and (mp or {}).get("createdCount") == 1 and (mp.get("summary") or {}).get("spaces") == ["Layout1"] and mpq.get("count") == 1, f"issue={short(sheet_issue, 120)} spaces={(mp or {}).get('summary', {}).get('spaces')} onLayout={mpq.get('count')}")
        save("markup", {"audit": aud, "revcloud": mk, "rectangle": rect, "skipped": mskip, "refused": mref, "gap": mg, "sheet": [sheet, mp]})

        # ---- M: structural tagging + schedule --------------------------------------------------------------------------------
        preview = value(s.tool("structural_tag_members", {"kinds": ["column", "beam"], "apply": False})) or {}
        psum = preview.get("summary") or {}
        # members left on S-* layers: the column, the bound xref block named xref-column (a column block by name) and the beam, which has the stray "C3" text within 300 mm — a mark with another prefix
        c3 = next((m for m in psum.get("marks", []) if m.get("existing") == "C3"), {})
        check("M tag preview (apply false) -> the beam keeps its foreign C3 text (kept_foreign, markHandle = that text), the two columns are assigned, nothing written",
              preview.get("createdCount") == 0 and psum.get("assigned") == 2 and psum.get("keptForeign") == 1 and c3.get("handle") == h["beam"] and c3.get("markHandle") == created[3] and (psum.get("byKind") or {}).get("column") == 2 and (psum.get("byKind") or {}).get("beam") == 1, short(psum, 300))
        tg = value(s.tool("structural_tag_members", {"kinds": ["column", "beam"], "digits": 2})) or {}
        tsum = tg.get("summary") or {}
        tq = value(s.tool("query_entities", {"filter": {"layers": ["S-ANNO-TEXT"]}, "properties": ["type", "text"]})) or {}
        texts = sorted(i.get("text") for i in tq.get("items", []))
        check("M tag apply -> new TEXT marks C01 + C02 on the created layer S-ANNO-TEXT, the beam's foreign C3 untouched", tg.get("createdCount") == 2 and tsum.get("layerCreated") is True and texts == ["C01", "C02"] and tsum.get("keptForeign") == 1, f"{texts} {short(tsum.get('written'), 200)}")
        ow = value(s.tool("structural_tag_members", {"kinds": ["column", "beam"], "digits": 2, "overwrite": True, "prefixes": {"column": "KC"}})) or {}
        osum = ow.get("summary") or {}
        owc3 = next((w for w in osum.get("written", []) if w.get("textHandle") == created[3]), {})
        c3now = value(s.tool("query_entities", {"filter": {"handles": [created[3]]}, "properties": ["text"]})) or {}
        check("M overwrite true with prefixes column KC -> every mark renumbered in place (3 texts modified, none created): C01/C02 become KC01/KC02, the beam's C3 text becomes B01, same handles",
              ow.get("createdCount") == 0 and ow.get("modifiedCount") == 3 and osum.get("overwritten") == 3 and all(w.get("via") == "text" for w in osum.get("written", [])) and owc3.get("was") == "C3" and (c3now.get("items") or [{}])[0].get("text") == "B01", f"{short(osum.get('written'), 300)} c3now={short(c3now.get('items'), 100)}")
        again = value(s.tool("structural_tag_members", {"kinds": ["column", "beam"], "digits": 2, "prefixes": {"column": "KC"}})) or {}
        asum = again.get("summary") or {}
        check("M tagging again keeps the existing marks (kept_existing 3, nothing created)", again.get("createdCount") == 0 and asum.get("keptExisting") == 3 and asum.get("assigned") == 0, short(asum, 200))
        foreign = value(s.tool("structural_tag_members", {"kinds": ["column", "beam"], "digits": 2, "apply": False})) or {}
        fsum = foreign.get("summary") or {}
        check("M under the default prefixes KC is not a mark prefix: the columns count as unmarked (assigned), the beam keeps B01 (kept_existing) — a door tag never becomes a member mark", fsum.get("assigned") == 2 and fsum.get("keptExisting") == 1 and fsum.get("keptForeign") == 0 and fsum.get("overwritten") == 0 and foreign.get("createdCount") == 0, short(fsum, 200))
        n_tbl = count()
        tblr = value(s.tool("structural_generate_member_schedule", {"kinds": ["column", "beam"], "prefixes": {"column": "KC"}, "writeTable": True, "insertPoint": {"x": 30000, "y": 0}, "layer": "LOCKED"})) or {}
        check("M writeTable on a locked layer -> LAYER_LOCKED refusal, nothing created (space never opened for write)", tblr.get("success") is False and (tblr.get("errors") or [{}])[0].get("code") == "LAYER_LOCKED" and tblr.get("createdCount") == 0 and count() == n_tbl, short(tblr.get("errors"), 200))
        sch = value(s.tool("structural_generate_member_schedule", {"kinds": ["column", "beam"], "prefixes": {"column": "KC"}})) or {}
        rows = {(r["kind"], r["section"]): r for r in sch.get("items", [])}
        check("M schedule rows (prefixes KC): column 400×400 with its KC mark, beam L 6000 with mark B01", ("column", "400×400") in rows and rows[("column", "400×400")]["count"] == 1 and len(rows[("column", "400×400")]["marks"]) == 1 and rows[("column", "400×400")]["marks"][0].startswith("KC") and ("beam", "L 6000") in rows and rows[("beam", "L 6000")]["marks"] == ["B01"] and (sch.get("summary") or {}).get("unmarked") == 0, short(sch.get("items"), 300))
        tbl = value(s.tool("structural_generate_member_schedule", {"kinds": ["column", "beam"], "prefixes": {"column": "KC"}, "writeTable": True, "insertPoint": {"x": 30000, "y": 0}, "layer": "S-ANNO-TEXT"})) or {}
        tblq = value(s.tool("query_entities", {"filter": {"types": ["ACAD_TABLE"]}})) or {}
        check("M writeTable -> one ACAD_TABLE created at (30000, 0) on S-ANNO-TEXT, rows in the summary", tbl.get("createdCount") == 1 and tblq.get("count") == 1 and (tblq.get("items") or [{}])[0].get("layer") == "S-ANNO-TEXT" and (tbl.get("summary") or {}).get("table", {}).get("rows") == len(sch.get("items", [])), short(tbl.get("summary"), 250))
        save("structural-write", {"preview": preview, "tag": tg, "overwrite": ow, "again": again, "foreign": foreign, "refusedTable": tblr, "schedule": sch, "table": tbl})

        # ---- D: undo -------------------------------------------------------------------------------------------------------
        acad_com("$a.ActiveDocument.SendCommand('_REGEN ')"); time.sleep(1)
        before_u = count()
        last = value(s.tool("create_entities_batch", {"items": [{"type": "line", "start": {"x": 0, "y": 12000}, "end": {"x": 1000, "y": 12000}}, {"type": "circle", "center": {"x": 2000, "y": 12000}, "radiusMm": 100}, {"type": "text", "text": "UNDO ME", "position": {"x": 3000, "y": 12000}, "heightMm": 200}]})) or {}
        mid_u = count()
        acad_com("$a.ActiveDocument.SendCommand('_U ')"); time.sleep(2)
        after_u = count()
        check("D U after a REGEN boundary reverts the whole last batch (3 entities)", last.get("createdCount") == 3 and mid_u == before_u + 3 and after_u == before_u, f"count {before_u}->{mid_u}->{after_u}")
    finally:
        s.close()

    return CL.finish("aec-edit-tools-live")


if __name__ == "__main__":
    sys.exit(main())
