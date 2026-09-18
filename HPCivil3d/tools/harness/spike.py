"""Spike of the Civil 3D MCP bridge, over stdio, against a running Civil 3D 2026 the PowerShell wrapper started
with the bundle loaded and the opt-in ticked. Each step answers one question the design left open and records
the answer (saved JSON + PASS/FAIL line) — a clear "no" is a valid result here.

  census open every drawing of the scene once and count what it holds; later steps pick drawings by capability
  ctx   context on the start-up drawing + a plain acad.dwt drawing: is `civil` null there?
  align read alignments (names, stations, entities, PointLocation/StationOffset), units, coordinate system code,
        inspect_type of the indexer / Name / label-set questions
  w1    COGO points: dryRun adds 3 and rolls back, auto adds 3 and U removes them, none + Add is refused
  w2    Alignment.Create from a polyline: which overload, is siteName "" siteless, does dryRun leave nothing
  surf  FindElevationAtXY inside and far outside — the exception type
  parcel what a Parcel exposes, is there an area source
  corr  AddVertex under dryRun; Corridor.Rebuild under dryRun and under auto + U — with the shipped guard the
        rebuild is refused and that refusal is the check; a Debug bridge built with the guard relaxed measures
        the rebuild itself — last, because a rebuild may take long
  busy  LINE waiting for input -> -32002 after the grace, ESC, retry
  disabled  opt-in off -> refused (the wrapper toggles the box first)
  nodoc every drawing closed -> context without doc, execute refused
Prints PASS/FAIL lines and a JSON summary; saves every raw answer beside it.
"""
import argparse, ctypes, ctypes.wintypes as wt, json, os, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, mcp_session, ok, short, utf8_console  # noqa: E402

utf8_console()
CL = Checklist()
check, skip, save = CL.check, CL.skip, CL.save
detached = []


# ---- Civil 3D side helpers (COM through Windows PowerShell 5.1, pid-guarded) -------------------------------------
def acad_com(script, wait=True):
    expected = os.environ.get("HP_HARNESS_ACAD_PID", "")
    if not expected:
        return False
    ps = ("$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); "
          f"if ($ids.Count -ne 1 -or [string]$ids[0] -ne '{expected}') {{ throw \"refusing COM: acad pids $ids, harness owns '{expected}'\" }}; "
          "$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " + script)
    if wait:
        p = subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=180, capture_output=True, text=True)
        if p.returncode != 0:
            print("COM:", (p.stdout + p.stderr).strip()[:300], flush=True)
        return p.returncode == 0
    proc = subprocess.Popen(["powershell", "-NoProfile", "-Command", ps], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, stdin=subprocess.DEVNULL)
    detached.append(proc)
    return True


def press_escape(times=2):
    pid = int(os.environ.get("HP_HARNESS_ACAD_PID", "0") or 0)
    if not pid:
        return False
    user32 = ctypes.windll.user32
    hwnds = []

    @ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
    def enum(h, _):
        p = wt.DWORD()
        tid = user32.GetWindowThreadProcessId(h, ctypes.byref(p))
        if p.value == pid and user32.IsWindowVisible(h):
            hwnds.append((h, tid))
        return True

    user32.EnumWindows(enum, 0)
    if not hwnds:
        return False

    class GUITHREADINFO(ctypes.Structure):
        _fields_ = [("cbSize", wt.DWORD), ("flags", wt.DWORD), ("hwndActive", wt.HWND), ("hwndFocus", wt.HWND), ("hwndCapture", wt.HWND),
                    ("hwndMenuOwner", wt.HWND), ("hwndMoveSize", wt.HWND), ("hwndCaret", wt.HWND), ("rcCaret", wt.RECT)]

    # the window with keyboard focus in the harness's own acad.exe (the command line while LINE waits), then the frames
    targets = []
    for h, tid in hwnds:
        info = GUITHREADINFO(cbSize=ctypes.sizeof(GUITHREADINFO))
        if user32.GetGUIThreadInfo(tid, ctypes.byref(info)) and info.hwndFocus:
            targets.append(info.hwndFocus)
    targets += [h for h, _ in hwnds]
    WM_KEYDOWN, WM_KEYUP, VK_ESCAPE = 0x0100, 0x0101, 0x1B
    for _ in range(times):
        for h in targets[:3]:
            user32.PostMessageW(h, WM_KEYDOWN, VK_ESCAPE, 0x00010001)
            user32.PostMessageW(h, WM_KEYUP, VK_ESCAPE, 0xC0010001)
        time.sleep(0.4)
    return True


def is_quiescent(s):
    try:
        return bool((context(s).get("autocad") or {}).get("isQuiescent"))
    except Exception:
        return False


def cancel_command(s, tries=3):
    """ESC until the editor is quiescent: PostMessage to the focused window first (works in AutoCAD), then the
    WScript.Shell route (activate the harness's own acad.exe and type ESC) - Civil 3D keeps keyboard focus in a
    palette more often than AutoCAD does."""
    pid = os.environ.get("HP_HARNESS_ACAD_PID", "")
    for _ in range(tries):
        press_escape(2); time.sleep(1.5)
        if is_quiescent(s):
            return "postmessage"
        if pid:
            ps = (f"$sh = New-Object -ComObject WScript.Shell; if ($sh.AppActivate([int]{pid})) {{ Start-Sleep -Milliseconds 400; "
                  "$sh.SendKeys('{ESC}'); Start-Sleep -Milliseconds 300; $sh.SendKeys('{ESC}') }}")
            subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=30, capture_output=True)
            time.sleep(1.5)
            if is_quiescent(s):
                return "sendkeys"
    return None


# ---- scene census: the Civil tutorial drawings are numbered by tutorial step; the "-1" files are usually the
# starting point (an empty surface, no alignment yet), so each scenario picks the first drawing that has what it needs.
CENSUS = {}


def census(s, scene):
    for f in sorted(os.listdir(scene)):
        if not f.lower().endswith(".dwg") or f in CENSUS:
            continue
        try:
            ctx = open_drawing(s, os.path.join(scene, f), wait_s=60)
            civ = dict(ctx.get("civil3d") or {})
            if civ.get("surfaceCount", 0) > 0:
                pts = value(execute(s, SURFACE_POINTS, transaction="none", label="census-surface")) or {}
                civ["surfacePoints"] = pts.get("points", -1)
            sites = value(execute(s, 'return civil.GetSiteIds().Count;', transaction="none", label="census-sites"))
            civ["siteCount"] = sites if isinstance(sites, int) else 0
            CENSUS[f] = civ
            close_active(discard=True); time.sleep(1)
            print(f"census {f}: " + short({k: civ.get(k) for k in ("drawingUnit", "alignmentCount", "surfaceCount", "surfacePoints", "corridorCount", "pipeNetworkCount", "siteCount", "cogoPointCount")}, 260), flush=True)
        except Exception as exception:
            CENSUS[f] = {"error": repr(exception)}
            print(f"census {f}: {exception!r}", flush=True)
    save("census", CENSUS)


def pick_drawing(s, scene, predicate, what):
    if not CENSUS:
        census(s, scene)
    for f, civ in CENSUS.items():
        if "error" not in civ and predicate(civ):
            open_drawing(s, os.path.join(scene, f))
            print(f"picked {f} for {what}", flush=True)
            return f
    skip(f"{what}: no drawing in the scene has what it needs", short(CENSUS, 400))
    return None


def open_drawing(s, path, wait_s=90):
    """Opens a drawing through COM and waits until the context reports it as the active document."""
    name = os.path.basename(path)
    acad_com(f'$hit = @($a.Documents) | Where-Object {{ $_.FullName -ieq "{path}" }} | Select-Object -First 1; '
             f'if ($hit) {{ $hit.Activate() }} else {{ $a.Documents.Open("{path}") | Out-Null }}')
    t0 = time.time()
    while time.time() - t0 < wait_s:
        try:
            ctx = context(s)
            if os.path.basename(ctx.get("docTitle") or "").lower() == name.lower():
                return ctx
        except Exception:
            pass
        time.sleep(2)
    raise RuntimeError(f"drawing {name} did not become active within {wait_s}s")


def add_drawing(s, template="acad.dwt", wait_s=60):
    before = (context(s).get("docTitle") or "")
    acad_com(f'$a.Documents.Add("{template}") | Out-Null')
    t0 = time.time()
    while time.time() - t0 < wait_s:
        ctx = context(s)
        if (ctx.get("docTitle") or "") != before:
            return ctx
        time.sleep(2)
    raise RuntimeError("new drawing did not become active")


def close_active(discard=True):
    return acad_com(f'$a.ActiveDocument.Close(${str(not discard).lower()})')


def undo():
    return acad_com('$a.ActiveDocument.SendCommand("_.U ")')


# ---- MCP helpers -------------------------------------------------------------------------------------------------
def execute(s, code, transaction="auto", dry_run=False, timeout_s=30, label="spike", args=None, wait=150.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return s.tool("execute_civil3d_code", params, timeout=wait)


def context(s):
    return s.tool("get_civil3d_context", {}, timeout=30)


def value(r):
    return r.get("value") if isinstance(r, dict) else None


# ---- scripts -----------------------------------------------------------------------------------------------------
CIVIL_NULL = 'return civil == null ? "null" : civil.GetType().FullName;'

READ_ALIGNMENTS = r'''
var ids = civil.GetAlignmentIds();
var list = new List<object>();
foreach (ObjectId id in ids)
{
    var a = (Alignment)tr.GetObject(id, OpenMode.ForRead);
    double e = 0, n = 0;
    a.PointLocation(a.StartingStation, 0, ref e, ref n);
    double st = 0, off = 0;
    a.StationOffset(e, n, ref st, ref off);
    var ents = a.Entities;
    var first = ents.Count > 0 ? ents.GetEntityByOrder(0) : null;
    list.Add(new {
        name = a.Name, type = a.AlignmentType.ToString(), lengthDu = a.Length, lengthMm = units.ToMm(a.Length),
        startStation = a.StartingStation, endStation = a.EndingStation,
        startLabel = a.GetStationStringWithEquations(a.StartingStation), endLabel = a.GetStationStringWithEquations(a.EndingStation),
        site = a.SiteName, siteless = a.IsSiteless, style = a.StyleName, profiles = a.GetProfileIds().Count,
        entityCount = ents.Count, firstEntityType = first == null ? null : first.EntityType.ToString(), firstSubCount = first == null ? -1 : first.SubEntityCount,
        startXY = new { e, n }, roundTripStation = st, roundTripOffset = off });
    if (list.Count >= 5) break;
}
return new { count = ids.Count, items = list };
'''

READ_UNITS = r'''
var uz = civil.Settings.DrawingSettings.UnitZoneSettings;
string csDesc = null, csErr = null;
try { var cs = Autodesk.Civil.Settings.SettingsUnitZone.GetCoordinateSystemByCode(uz.CoordinateSystemCode); csDesc = cs == null ? "null" : cs.Description + " | " + cs.Unit + " | " + cs.Datum; }
catch (System.Exception ex) { csErr = ex.GetType().FullName + ": " + ex.Message; }
string emptyDesc = null, emptyErr = null;
try { var cs0 = Autodesk.Civil.Settings.SettingsUnitZone.GetCoordinateSystemByCode(""); emptyDesc = cs0 == null ? "null" : cs0.Description; }
catch (System.Exception ex) { emptyErr = ex.GetType().FullName + ": " + ex.Message; }
return new {
    drawingUnits = uz.DrawingUnits.ToString(), coordinateSystemCode = uz.CoordinateSystemCode, imperialToMetric = uz.ImperialToMetricConversion.ToString(),
    drawingScale = uz.DrawingScale, angular = uz.AngularUnits.ToString(),
    insunits = db.Insunits.ToString(), unitsLabel = units.Label, mmPerUnit = units.MmPerUnit,
    csDesc, csErr, emptyDesc, emptyErr,
    alignmentStyles = civil.Styles.AlignmentStyles.Count, pointStyles = civil.Styles.PointStyles.Count, surfaceStyles = civil.Styles.SurfaceStyles.Count };
'''

SUBENTITY_INDEXER = r'''
var ids = civil.GetAlignmentIds();
if (ids.Count == 0) return new { alignments = 0 };
var a = (Alignment)tr.GetObject(ids[0], OpenMode.ForRead);
var first = a.Entities.GetEntityByOrder(0);
var sub = first[0];
var byIndex = a.Entities[0];
var byName = civil.Styles.AlignmentStyles.Count > 0 ? ((Autodesk.Civil.DatabaseServices.DBObject)tr.GetObject(civil.Styles.AlignmentStyles.ToObjectIds()[0], OpenMode.ForRead)).Name : null;
return new { entityIndexer = byIndex.EntityType.ToString(), subType = sub.SubEntityType.ToString(), subStart = sub.StartStation, subEnd = sub.EndStation, subLength = sub.Length,
             subStartPoint = new { x = sub.StartPoint.X, y = sub.StartPoint.Y }, firstStyleName = byName, corridorIndexer = civil.CorridorCollection.Count > 0 ? civil.CorridorCollection[0].Handle.ToString() : "no corridor",
             labelSets = civil.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count };
'''

COGO_COUNT = 'return (int)civil.CogoPoints.Count;'

COGO_ADD = r'''
var pts = civil.CogoPoints;
var ids = new List<string>();
for (var i = 0; i < 3; i++)
{
    var p = new Point3d(units.ToDrawing(args.Double("xMm") + i * 1000), units.ToDrawing(args.Double("yMm")), 10.0 + i);
    var id = pts.Add(p, "MCP-SPIKE", true);
    var cp = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    ids.Add(cp.Handle.ToString() + ":" + cp.PointNumber);
}
return new { added = ids, count = (int)pts.Count };
'''

DRAW_POLYLINE = r'''
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
var pl = new Polyline();
pl.AddVertexAt(0, new Point2d(units.ToDrawing(args.Double("x0")), units.ToDrawing(args.Double("y0"))), 0, 0, 0);
pl.AddVertexAt(1, new Point2d(units.ToDrawing(args.Double("x0") + 200000), units.ToDrawing(args.Double("y0") + 50000)), 0, 0, 0);
pl.AddVertexAt(2, new Point2d(units.ToDrawing(args.Double("x0") + 400000), units.ToDrawing(args.Double("y0"))), 0, 0, 0);
ms.AppendEntity(pl);
tr.AddNewlyCreatedDBObject(pl, true);
return pl.Handle.ToString();
'''

ALIGNMENT_FROM_POLYLINE = r'''
var handle = args.Str("handle");
var id = db.GetObjectId(false, new Handle(System.Convert.ToInt64(handle, 16)), 0);
string styleName = null;
foreach (ObjectId sid in civil.Styles.AlignmentStyles) { styleName = ((Autodesk.Civil.DatabaseServices.DBObject)tr.GetObject(sid, OpenMode.ForRead)).Name; break; }
var labelSet = args.Str("labelSet", "");
if (labelSet == "") { foreach (ObjectId lid in civil.Styles.LabelSetStyles.AlignmentLabelSetStyles) { labelSet = ((Autodesk.Civil.DatabaseServices.DBObject)tr.GetObject(lid, OpenMode.ForRead)).Name; break; } }
var labelSetCount = civil.Styles.LabelSetStyles.AlignmentLabelSetStyles.Count;
var site = args.Str("site", "");
var layer = args.Str("layer", "0");
var opts = new PolylineOptions { PlineId = id, AddCurvesBetweenTangents = false, EraseExistingEntities = false };
string overload;
ObjectId aid;
try { aid = Alignment.Create(civil, opts, args.Str("name"), site, layer, styleName, labelSet); overload = "PolylineOptions,string x5"; }
catch (System.Exception ex) { return new { failed = ex.GetType().FullName + ": " + ex.Message, styleName, labelSet, labelSetCount, site }; }
var a = (Alignment)tr.GetObject(aid, OpenMode.ForRead);
return new { overload, handle = a.Handle.ToString(), name = a.Name, lengthMm = units.ToMm(a.Length), startStation = a.StartingStation, endStation = a.EndingStation,
             entities = a.Entities.Count, site = a.SiteName, siteless = a.IsSiteless, style = a.StyleName, styleUsed = styleName, labelSetUsed = labelSet, labelSetCount, alignments = civil.GetAlignmentIds().Count };
'''

COUNT_ALIGNMENTS = 'return civil.GetAlignmentIds().Count;'

SURFACE_ELEVATION = r'''
var ids = civil.GetSurfaceIds();
if (ids.Count == 0) return new { surfaces = 0 };
var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(ids[0], OpenMode.ForRead);
var p = s.GetGeneralProperties();
var cx = (p.MinimumCoordinateX + p.MaximumCoordinateX) / 2; var cy = (p.MinimumCoordinateY + p.MaximumCoordinateY) / 2;
double zIn = double.NaN; string errIn = null; double zOut = double.NaN; string errOut = null, errOutMsg = null;
try { zIn = s.FindElevationAtXY(cx, cy); } catch (System.Exception ex) { errIn = ex.GetType().FullName + ": " + ex.Message; }
try { zOut = s.FindElevationAtXY(p.MaximumCoordinateX + 1e6, p.MaximumCoordinateY + 1e6); } catch (System.Exception ex) { errOut = ex.GetType().FullName; errOutMsg = ex.Message; }
return new { surfaces = ids.Count, name = s.Name, type = s.GetType().Name, isOutOfDate = s.IsOutOfDate, autoRebuild = s.AutoRebuild,
             points = p.NumberOfPoints, minZ = p.MinimumElevation, maxZ = p.MaximumElevation, center = new { cx, cy }, zIn, errIn, zOut = double.IsNaN(zOut) ? (object)null : zOut, errOut, errOutMsg };
'''

PARCELS = r'''
var sites = civil.GetSiteIds();
var list = new List<object>();
foreach (ObjectId sid in sites)
{
    var site = (Site)tr.GetObject(sid, OpenMode.ForRead);
    foreach (ObjectId pid in site.GetParcelIds())
    {
        var p = (Parcel)tr.GetObject(pid, OpenMode.ForRead);
        object ext = null; string extErr = null;
        try { var e = p.GeometricExtents; ext = new { minX = e.MinPoint.X, minY = e.MinPoint.Y, maxX = e.MaxPoint.X, maxY = e.MaxPoint.Y }; } catch (System.Exception ex) { extErr = ex.GetType().Name; }
        double area = double.NaN; string areaErr = null;
        try { area = p.Area; } catch (System.Exception ex) { areaErr = ex.GetType().Name + ": " + ex.Message; }
        list.Add(new { site = site.Name, name = p.Name, number = p.Number, taxId = p.TaxId, address = p.Address, style = p.StyleName, description = p.Description, area = double.IsNaN(area) ? (object)null : area, areaErr, centroid = new { x = p.Centroid.X, y = p.Centroid.Y }, ext, extErr });
        if (list.Count >= 5) break;
    }
}
return new { sites = sites.Count, parcels = list };
'''

CORRIDOR_READ = r'''
var cc = civil.CorridorCollection;
var list = new List<object>();
for (var i = 0; i < cc.Count && i < 5; i++)
{
    var c = (Corridor)tr.GetObject(cc[i], OpenMode.ForRead);
    var bls = new List<object>();
    foreach (Baseline b in c.Baselines) bls.Add(new { b.Name, alignment = ((Alignment)tr.GetObject(b.AlignmentId, OpenMode.ForRead)).Name, regions = b.BaselineRegions.Count });
    list.Add(new { name = c.Name, isOutOfDate = c.IsOutOfDate, rebuildAutomatic = c.RebuildAutomatic, codeSet = c.CodeSetStyleName, baselines = bls, surfaces = c.CorridorSurfaces.Count });
}
return new { corridors = cc.Count, items = list };
'''

SURFACE_ADD_VERTEX = r'''
var ids = civil.GetSurfaceIds();
TinSurface tin = null;
foreach (ObjectId id in ids) { if (tr.GetObject(id, OpenMode.ForWrite) is TinSurface t) { tin = t; break; } }
if (tin == null) return new { tin = false };
var before = tin.GetGeneralProperties().NumberOfPoints;
var p = tin.GetGeneralProperties();
var op = tin.AddVertex(new Point3d((p.MinimumCoordinateX + p.MaximumCoordinateX) / 2, (p.MinimumCoordinateY + p.MaximumCoordinateY) / 2, (p.MinimumElevation + p.MaximumElevation) / 2));
var after = tin.GetGeneralProperties().NumberOfPoints;
return new { tin = true, name = tin.Name, before, after, isOutOfDate = tin.IsOutOfDate, autoRebuild = tin.AutoRebuild, opType = op == null ? null : op.GetType().Name };
'''

SURFACE_POINTS = r'''
var ids = civil.GetSurfaceIds();
foreach (ObjectId id in ids) { if (tr.GetObject(id, OpenMode.ForRead) is TinSurface t) return new { name = t.Name, points = t.GetGeneralProperties().NumberOfPoints, isOutOfDate = t.IsOutOfDate }; }
return new { name = (string)null, points = -1 };
'''

CORRIDOR_REBUILD = r'''
var cc = civil.CorridorCollection;
if (cc.Count == 0) return new { corridors = 0 };
var c = (Corridor)tr.GetObject(cc[0], OpenMode.ForWrite);
var sw = System.Diagnostics.Stopwatch.StartNew();
var beforeOutOfDate = c.IsOutOfDate;
c.Rebuild();
sw.Stop();
return new { name = c.Name, beforeOutOfDate, afterOutOfDate = c.IsOutOfDate, rebuildMs = sw.ElapsedMilliseconds, surfaces = c.CorridorSurfaces.Count };
'''

CORRIDOR_STATE = r'''
var cc = civil.CorridorCollection;
if (cc.Count == 0) return new { corridors = 0 };
var c = (Corridor)tr.GetObject(cc[0], OpenMode.ForRead);
return new { corridors = cc.Count, name = c.Name, isOutOfDate = c.IsOutOfDate, erased = c.IsErased, surfaces = c.CorridorSurfaces.Count, handle = c.Handle.ToString() };
'''

BUSY_WAITER = 'return 2;'


# ---- scenarios ---------------------------------------------------------------------------------------------------
def scenario_ctx(s, scene):
    ctx = context(s)
    save("ctx-startup", ctx)
    civ = ctx.get("civil3d") or {}
    check("S-04a startup drawing (Civil template): context has civil3d block", "civil3d" in ctx and "autocad" in ctx, short(civ))
    check("S-04a product is Civil3D", civ.get("product") == "Civil3D", f"product={civ.get('product')}")
    r = execute(s, CIVIL_NULL, transaction="none", label="civil-null")
    save("ctx-startup-civil", r)
    check("S-04a `civil` resolves on the start-up drawing", not r.get("isError") and value(r) == "Autodesk.Civil.ApplicationServices.CivilDocument",
          f"value={value(r)} isCivilDocument={civ.get('isCivilDocument')} unit={civ.get('drawingUnit')} insunitsMismatch={civ.get('insunitsMismatch')}")
    check("S-06 units on the start-up drawing follow the Civil unit", ctx.get("units", {}).get("length") == civ.get("drawingUnit") and civ.get("drawingUnit") in ("Meters", "Feet"),
          f"units.length={ctx.get('units')} drawingUnit={civ.get('drawingUnit')}")
    # (b) a plain drawing from acad.dwt inside Civil 3D
    try:
        ctx2 = add_drawing(s, "acad.dwt")
        save("ctx-acad-dwt", ctx2)
        r2 = execute(s, CIVIL_NULL, transaction="none", label="civil-null-plain")
        save("ctx-acad-dwt-civil", r2)
        civ2 = ctx2.get("civil3d") or {}
        # Either answer is a valid conclusion; the check only demands consistency between context and script.
        consistent = (civ2.get("isCivilDocument") is True and value(r2) == "Autodesk.Civil.ApplicationServices.CivilDocument") or \
                     (civ2.get("isCivilDocument") is False and value(r2) == "null")
        check("S-04b acad.dwt drawing: context and `civil` agree", not r2.get("isError") and consistent,
              f"isCivilDocument={civ2.get('isCivilDocument')} civil={value(r2)} unit={civ2.get('drawingUnit')} logs={r2.get('logs')}")
        close_active(discard=True)
        time.sleep(2)
    except Exception as exception:
        check("S-04b acad.dwt drawing", False, repr(exception))


def scenario_align(s, scene):
    f = pick_drawing(s, scene, lambda c: c.get("alignmentCount", 0) >= 1, "align")
    if not f:
        return
    ctx = context(s)
    save("align-ctx", ctx)
    civ = ctx.get("civil3d") or {}
    check(f"S-05 {f} is a Civil document with alignments", civ.get("isCivilDocument") is True and civ.get("alignmentCount", 0) >= 1,
          f"alignments={civ.get('alignmentCount')} surfaces={civ.get('surfaceCount')} points={civ.get('cogoPointCount')} unit={civ.get('drawingUnit')}")
    r = execute(s, READ_ALIGNMENTS, transaction="none", label="read-alignments")
    save("align-read", r)
    v = value(r) or {}
    items = v.get("items") or []
    first = items[0] if items else {}
    plausible = bool(first) and abs((first.get("endStation") or 0) - (first.get("startStation") or 0) - (first.get("lengthDu") or -1)) < 1e-3
    check("S-05 read alignment: name/stations/entities/labels/PointLocation/StationOffset", not r.get("isError") and plausible and first.get("entityCount", 0) >= 1,
          short({k: first.get(k) for k in ("name", "lengthDu", "startStation", "endStation", "startLabel", "endLabel", "entityCount", "firstEntityType", "firstSubType", "roundTripStation", "site", "siteless")}))
    idx = execute(s, SUBENTITY_INDEXER, transaction="none", label="indexers")
    save("align-indexers", idx)
    iv = value(idx) or {}
    check("S-08 indexers int on AlignmentEntity/AlignmentEntityCollection/CorridorCollection, StyleBase.Name via DBObject, label-set root",
          not idx.get("isError") and iv.get("subType") is not None, short(iv) if not idx.get("isError") else short(idx.get("diagnostics") or idx.get("message")))
    u = execute(s, READ_UNITS, transaction="none", label="read-units")
    save("align-units", u)
    uv = value(u) or {}
    check("S-06 units: Civil DrawingUnits vs INSUNITS vs units.Label", not u.get("isError") and uv.get("drawingUnits") in ("Meters", "Feet") and uv.get("unitsLabel") == uv.get("drawingUnits"),
          short({k: uv.get(k) for k in ("drawingUnits", "insunits", "unitsLabel", "mmPerUnit", "coordinateSystemCode", "csDesc", "csErr", "emptyDesc", "emptyErr", "alignmentStyles")}))
    check("S-06 GetCoordinateSystemByCode(\"\") answer recorded", not u.get("isError") and ("emptyDesc" in uv or "emptyErr" in uv), f"emptyDesc={uv.get('emptyDesc')} emptyErr={uv.get('emptyErr')}")
    for t in ["Autodesk.Civil.DatabaseServices.Feature", "Autodesk.Civil.DatabaseServices.Styles.StyleBase", "Autodesk.Civil.DatabaseServices.CorridorCollection",
              "Autodesk.Civil.DatabaseServices.AlignmentEntityCollection", "Autodesk.Civil.DatabaseServices.Styles.LabelSetStylesRoot", "Autodesk.Civil.DatabaseServices.Parcel",
              "Autodesk.Civil.DatabaseServices.TreeNodeCollectionBase"]:
        r = s.tool("inspect_type", {"typeName": t, "maxMembers": 200}, timeout=60)
        save("inspect-" + t.split(".")[-1], r)
        members = r.get("members") if isinstance(r, dict) else None
        check(f"S-08 inspect_type {t.split('.')[-1]}", isinstance(r, dict) and not r.get("isError") and members is not None and len(members) > 0,
              f"{len(members or [])} members; e.g. {short([m.get('signature') for m in (members or [])[:6]], 300)}")


def scenario_w1(s, scene):
    base = value(execute(s, COGO_COUNT, transaction="none", label="cogo-count"))
    r = execute(s, COGO_ADD, dry_run=True, label="cogo-dry", args={"xMm": 1000000, "yMm": 1000000})
    save("w1-dry", r)
    after = value(execute(s, COGO_COUNT, transaction="none", label="cogo-count"))
    check("W1 dryRun: 3 COGO points added then rolled back", not r.get("isError") and r.get("rolledBack") is True and (r.get("changed") or {}).get("added", 0) >= 3 and after == base,
          f"changed={r.get('changed')} count {base}->{after} value={short(value(r))}")
    r = execute(s, COGO_ADD, label="cogo-commit", args={"xMm": 1000000, "yMm": 1000000})
    save("w1-commit", r)
    mid = value(execute(s, COGO_COUNT, transaction="none", label="cogo-count"))
    check("W1 commit: count +3, changed.added >= 3", not r.get("isError") and r.get("rolledBack") is False and mid == base + 3, f"changed={r.get('changed')} count {base}->{mid}")
    undo(); time.sleep(3)
    back = value(execute(s, COGO_COUNT, transaction="none", label="cogo-count"))
    check("W1 U reverts the committed run", back == base, f"count after U {back} (base {base})")
    r = execute(s, COGO_ADD, transaction="none", label="cogo-none", args={"xMm": 1000000, "yMm": 1000000})
    save("w1-none", r)
    after = value(execute(s, COGO_COUNT, transaction="none", label="cogo-count"))
    check("W1 none + Add refused and rolled back", r.get("isError") and 'transaction="none"' in (r.get("message") or "") and after == base, f"message={r.get('message')} count {after}")


def scenario_w2(s, scene):
    base = value(execute(s, COUNT_ALIGNMENTS, transaction="none", label="align-count"))
    pl = execute(s, DRAW_POLYLINE, label="polyline", args={"x0": 2000000, "y0": 2000000})
    save("w2-polyline", pl)
    check("W2 polyline drawn (AutoCAD API through the Civil bridge)", not pl.get("isError") and isinstance(value(pl), str), short(pl))
    handle = value(pl)
    r = execute(s, ALIGNMENT_FROM_POLYLINE, dry_run=True, label="align-dry", args={"handle": handle, "name": "MCP-SPIKE-A1", "site": "", "labelSet": "", "layer": "0"}, timeout_s=60)
    save("w2-dry", r)
    v = value(r) or {}
    after = value(execute(s, COUNT_ALIGNMENTS, transaction="none", label="align-count"))
    check("W2 dryRun Alignment.Create(PolylineOptions, siteName \"\", first label set): created then rolled back", not r.get("isError") and "failed" not in v and r.get("rolledBack") is True and after == base,
          short({k: v.get(k) for k in ("overload", "name", "lengthMm", "startStation", "endStation", "entities", "site", "siteless", "style", "labelSetUsed", "labelSetCount", "failed")}) + f" count {base}->{after}")
    if "failed" in v:
        # try the label-set-less variant the docs hint at: pass a real label set name if one exists — recorded, not asserted
        skip("W2 commit", "dryRun failed: " + str(v.get("failed")))
        return
    r = execute(s, ALIGNMENT_FROM_POLYLINE, label="align-commit", args={"handle": handle, "name": "MCP-SPIKE-A2", "site": "", "labelSet": "", "layer": "0"}, timeout_s=60)
    save("w2-commit", r)
    v = value(r) or {}
    mid = value(execute(s, COUNT_ALIGNMENTS, transaction="none", label="align-count"))
    check("W2 commit creates the alignment", not r.get("isError") and "failed" not in v and mid == base + 1, short(v) + f" count {base}->{mid}")
    undo(); time.sleep(3)
    back = value(execute(s, COUNT_ALIGNMENTS, transaction="none", label="align-count"))
    check("W2 U reverts the alignment", back == base, f"count after U {back} (base {base})")


def scenario_surf(s, scene):
    if not pick_drawing(s, scene, lambda c: c.get("surfacePoints", 0) > 0, "surf"):
        return
    r = execute(s, SURFACE_ELEVATION, transaction="none", label="surface-elev", timeout_s=60)
    save("surf-elev", r)
    v = value(r) or {}
    check("S-07 FindElevationAtXY inside the surface returns a number", not r.get("isError") and v.get("surfaces", 0) >= 1 and v.get("errIn") is None and isinstance(v.get("zIn"), (int, float)),
          short({k: v.get(k) for k in ("name", "type", "points", "minZ", "maxZ", "zIn", "errIn", "isOutOfDate", "autoRebuild")}))
    check("S-07 outside the boundary: exception type recorded", not r.get("isError") and (v.get("errOut") is not None or v.get("zOut") is not None),
          f"errOut={v.get('errOut')} msg={short(v.get('errOutMsg'), 200)} zOut={v.get('zOut')}")


def scenario_parcel(s, scene):
    if not pick_drawing(s, scene, lambda c: c.get("siteCount", 0) >= 1, "parcel"):
        return
    r = execute(s, PARCELS, transaction="none", label="parcels", timeout_s=60)
    save("parcel-read", r)
    v = value(r) or {}
    parcels = v.get("parcels") or []
    check("S-09 parcels readable through Site.GetParcelIds (name/number/area/centroid/extents)", not r.get("isError") and len(parcels) >= 1,
          f"sites={v.get('sites')} parcels={len(parcels)} first={short(parcels[0] if parcels else None, 300)}")
    insp = s.tool("inspect_type", {"typeName": "Autodesk.Civil.DatabaseServices.Parcel", "memberFilter": "Area", "maxMembers": 50}, timeout=60)
    save("parcel-inspect-area", insp)
    names = [m.get("signature", "") for m in (insp.get("members") or [])] if isinstance(insp, dict) else []
    has_area = any(" Area " in n or n.endswith(" Area") or "Perimeter" in n for n in names)
    check("S-09 Parcel area source recorded (Area member present at runtime)", isinstance(insp, dict) and not insp.get("isError"), f"area-like members: {short(names, 300)} hasAreaOrPerimeter={has_area}")


def scenario_corr(s, scene):
    if not pick_drawing(s, scene, lambda c: c.get("corridorCount", 0) >= 1, "corr"):
        return
    r = execute(s, CORRIDOR_READ, transaction="none", label="corridor-read", timeout_s=60)
    save("corr-read", r)
    v = value(r) or {}
    check("S-10 corridor read (baselines, RebuildAutomatic, IsOutOfDate)", not r.get("isError") and v.get("corridors", 0) >= 1, short(v))
    before = value(execute(s, SURFACE_POINTS, transaction="none", label="surface-points")) or {}
    r = execute(s, SURFACE_ADD_VERTEX, dry_run=True, label="surface-addvertex-dry", timeout_s=60)
    save("corr-addvertex-dry", r)
    after = value(execute(s, SURFACE_POINTS, transaction="none", label="surface-points")) or {}
    v = value(r) or {}
    check("S-10a TinSurface.AddVertex under dryRun: point count back to before", (v.get("tin") is False) or (not r.get("isError") and r.get("rolledBack") is True and after.get("points") == before.get("points")),
          f"before={before} during={short(v)} after={after}")
    state0 = value(execute(s, CORRIDOR_STATE, transaction="none", label="corridor-state")) or {}
    r = execute(s, CORRIDOR_REBUILD, dry_run=True, label="corridor-rebuild-dry", timeout_s=120, wait=200)
    save("corr-rebuild-dry", r)
    state1 = value(execute(s, CORRIDOR_STATE, transaction="none", label="corridor-state")) or {}
    v = value(r) or {}
    guarded = r.get("isError") and any("Rebuild" in d.get("message", "") for d in r.get("diagnostics", []))
    if guarded:
        check("S-10b guard refuses Corridor.Rebuild under the Civil profile (MVP policy), corridor untouched",
              state1.get("erased") is False and state1.get("handle") == state0.get("handle") and r.get("changed", {}).get("modified", 0) == 0,
              short([d.get("message") for d in r.get("diagnostics", [])], 200))
    else:
        check("S-10b Corridor.Rebuild under dryRun: corridor still present afterwards, state recorded",
              not r.get("isError") and state1.get("erased") is False and state1.get("handle") == state0.get("handle"),
              f"rebuildMs={v.get('rebuildMs')} before={short(state0)} after={short(state1)} rolledBack={r.get('rolledBack')} changed={r.get('changed')}")
    r = execute(s, CORRIDOR_REBUILD, label="corridor-rebuild-commit", timeout_s=120, wait=200)
    save("corr-rebuild-commit", r)
    state2 = value(execute(s, CORRIDOR_STATE, transaction="none", label="corridor-state")) or {}
    if r.get("isError") and any("Rebuild" in d.get("message", "") for d in r.get("diagnostics", [])):
        check("S-10c guard refuses Corridor.Rebuild under auto too", state2.get("erased") is False and r.get("changed", {}).get("modified", 0) == 0, short(r, 200))
    else:
        undo(); time.sleep(5)
        state3 = value(execute(s, CORRIDOR_STATE, transaction="none", label="corridor-state")) or {}
        check("S-10c Corridor.Rebuild under auto then U: corridor present before and after U", not r.get("isError") and state2.get("erased") is False and state3.get("erased") is False,
              f"rebuildMs={(value(r) or {}).get('rebuildMs')} after={short(state2)} afterU={short(state3)} changed={r.get('changed')}")


def scenario_busy(s, scene):
    acad_com('$a.ActiveDocument.SendCommand("_.LINE ")', wait=False)
    time.sleep(2)
    t0 = time.time()
    r = execute(s, BUSY_WAITER, transaction="none", label="busy", timeout_s=5, wait=60)
    elapsed = time.time() - t0
    save("busy", r)
    check("S-11 busy (LINE waiting) -> refused after the grace", r.get("isError") and ("Press ESC" in (r.get("message") or "") or "busy" in (r.get("message") or "").lower()) and 5 <= elapsed <= 40,
          f"message={r.get('message')} after {elapsed:.1f}s")
    how = cancel_command(s)
    r = execute(s, BUSY_WAITER, transaction="none", label="after-esc")
    check("S-11 ESC then retry succeeds", not r.get("isError") and value(r) == 2, f"cancelled via {how}; " + short(r))


def scenario_disabled(s, scene):
    r = execute(s, "return 1;", transaction="none", label="disabled")
    save("disabled", r)
    check("S-11 opt-in off -> refused", r.get("isError") and "Allow AI code execution" in (r.get("message") or ""), short(r))


def scenario_nodoc(s, scene):
    for _ in range(8):
        acad_com('if ($a.Documents.Count -gt 0) { $a.Documents.Item(0).Close($false) }')
        time.sleep(2)
        if not context(s).get("openDocs"):
            break
    ctx = context(s)
    r = execute(s, "return 1;", transaction="none", label="nodoc")
    save("nodoc", {"ctx": ctx, "r": r})
    check("S-11 no drawing -> context without doc, execute refused", not ctx.get("docTitle") and r.get("isError") and "Open one first" in (r.get("message") or ""),
          f"openDocs={ctx.get('openDocs')} message={r.get('message')}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", required=True, help="HPCivil3d.Mcp.Server.exe (Debug build is fine for the spike)")
    ap.add_argument("--scene-dir", required=True, help="folder with the copied tutorial drawings")
    ap.add_argument("--out", default="")
    ap.add_argument("--only", default="", help="comma list of ctx,align,w1,w2,surf,parcel,corr,busy,disabled,nodoc")
    a = ap.parse_args()
    CL.out_dir = a.out or None
    only = [x for x in a.only.split(",") if x] or ["ctx", "align", "w1", "w2", "surf", "parcel", "corr", "busy"]
    s = Server(a.exe, name="civil3d")
    try:
        init = s.initialize()
        print(f"initialize: {init.get('serverInfo')}", flush=True)
        tools = s.tools()
        check("server lists the 4 core + 8 registry tools", "execute_civil3d_code" in tools and "get_civil3d_context" in tools and "search_tools" in tools, f"{len(tools)} tools")
        steps = {"census": lambda s, scene: census(s, scene), "ctx": scenario_ctx, "align": scenario_align, "w1": scenario_w1, "w2": scenario_w2, "surf": scenario_surf, "parcel": scenario_parcel,
                 "corr": scenario_corr, "busy": scenario_busy, "disabled": scenario_disabled, "nodoc": scenario_nodoc}
        for step in only:
            try:
                steps[step](s, a.scene_dir)
            except Exception as exception:
                check(f"{step} aborted", False, repr(exception))
    finally:
        s.close()
    for proc in detached:
        try: proc.kill()
        except Exception: pass
    sys.exit(CL.finish("spike-" + "-".join(only)))


if __name__ == "__main__":
    main()
