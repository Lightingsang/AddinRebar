"""Live verification of the Civil 3D MCP server against a running Civil 3D 2026, over stdio, in one session:
  a  start-up and context: the civil3d block, the document resource, inspect_type on a Civil type, units per drawing
  e  execute matrix (read, dryRun, commit + U, exception, none+modify, manual, guard x7 incl. the Civil denials, compile,
     cancel, timeout, logs/args/serializer, a Civil exception, audit)
  s  the 12 seeds on the tutorial drawings (reads with real numbers; the two writes as test_tool dryRun -> run_tool -> U)
  r  MISS -> memory: ad-hoc run -> get_run -> toolify_run -> propose -> test -> publish -> CLI approve -> tools/list_changed
     -> call by name; a fragile tool -> 5 failing runs -> quarantined -> restore -> fix (newVersion) -> back;
     argument errors never count; a proposal that rebuilds or writes under `none` is refused
  x  the AutoCAD exe beside (coexistence at the server level; the process-level isolation lives in the wrapper)
The PowerShell wrapper (run-live-verify.ps1) starts Civil 3D, copies the scene, toggles the opt-in and calls this with
--only for the steps that need the box off (disabled), no drawing (nodoc) or a command waiting (busy).
"""
import argparse, ctypes, ctypes.wintypes as wt, hashlib, json, os, shutil, subprocess, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
# the bookkeeping + stdio session helper every HP MCP harness shares (MCP folder -> McpShared, never the other way round)
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, mcp_session, ok, short, utf8_console  # noqa: E402

utf8_console()

CL = Checklist()
check, skip, save = CL.check, CL.skip, CL.save
detached = []
MAIN_DRAWING = "Profile-5F.dwg"     # Feet: 8 alignments, 14 profiles, 2 surfaces, a corridor, a pipe network, 2 405 COGO points
PARCEL_DRAWING = "Corridor-1a.dwg"  # Meters: 52 parcels in Site 1, EG with 23 092 points, 2 alignments


# ---- Civil 3D side helpers (COM through Windows PowerShell 5.1, pid-guarded; ESC through the focused window) -----------
def acad_com(script, wait=True):
    """Drives the acad.exe the wrapper started. Refuses unless exactly one acad.exe exists and it is the harness's own
    (HP_HARNESS_ACAD_PID): the calls close drawings without saving and type commands."""
    expected = os.environ.get("HP_HARNESS_ACAD_PID", "")
    if not expected:
        return False
    ps = ("$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); "
          f"if ($ids.Count -ne 1 -or [string]$ids[0] -ne '{expected}') {{ throw \"refusing COM: acad pids $ids, harness owns '{expected}'\" }}; "
          "$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " + script)
    if wait:
        subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=180, capture_output=True)
        return True
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
    """ESC until the editor is quiescent: PostMessage first, then WScript.Shell AppActivate + SendKeys on our own pid."""
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


def context(s):
    return s.tool("get_civil3d_context", {"includeSelection": False}, timeout=30)


def open_drawing(s, scene, name, wait_s=120):
    """Activates the drawing if it is already open (a second Documents.Open would make a read-only copy), else opens it."""
    path = os.path.join(scene, name)
    acad_com(f'$hit = @($a.Documents) | Where-Object {{ $_.FullName -ieq "{path}" }} | Select-Object -First 1; '
             f'if ($hit) {{ $hit.Activate() }} else {{ $a.Documents.Open("{path}") | Out-Null }}')
    t0 = time.time()
    while time.time() - t0 < wait_s:
        try:
            ctx = context(s)
            if os.path.basename(ctx.get("docTitle") or "").lower() == name.lower():
                if ctx.get("isReadOnly"):
                    raise RuntimeError(f"drawing {name} opened read-only (stale lock or second open): the writes would fail")
                return ctx
        except RuntimeError:
            raise
        except Exception:
            pass
        time.sleep(2)
    raise RuntimeError(f"drawing {name} did not become active within {wait_s}s")


def undo(s):
    acad_com("$a.ActiveDocument.SendCommand('_U ')"); time.sleep(2)


def regen():
    acad_com("$a.ActiveDocument.SendCommand('_REGEN ')"); time.sleep(1)


# ---- scripts -----------------------------------------------------------------------------------------------------------
COUNT_LINES = 'var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead); var n = 0; foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") n++; return n;'
ADD_LINE_BODY = ('var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite); '
                 'var line = new Line(Point3d.Origin, new Point3d(units.ToDrawing(args.Double("lengthMm", 1000)), 0, 0)); '
                 'ms.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true); log("added " + line.Handle); ')
ADD_LINE = ADD_LINE_BODY + 'return new { handle = line.Handle.ToString(), length = line.Length };'
COGO_COUNT = 'return (int)civil.CogoPoints.Count;'
ALIGNMENT_COUNT = 'return civil.GetAlignmentIds().Count;'
DRAW_POLYLINE = ('var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite); var pl = new Polyline(); '
                 'double x0 = units.ToDrawing(args.Double("x0")), y0 = units.ToDrawing(args.Double("y0")), d = units.ToDrawing(300000); '
                 'pl.AddVertexAt(0, new Point2d(x0, y0), 0, 0, 0); pl.AddVertexAt(1, new Point2d(x0 + d, y0), 0, 0, 0); pl.AddVertexAt(2, new Point2d(x0 + d, y0 + d), 0, 0, 0); '
                 'space.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true); return pl.Handle.ToString();')
OUTSIDE_SURFACE = ('var ids = civil.GetSurfaceIds(); var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(ids[0], OpenMode.ForRead); '
                   'return s.FindElevationAtXY(1e9, 1e9);')

# R: ad-hoc answer to "how many COGO points carry this description?" and its tool form
COUNT_COGO_ADHOC = r'''
var n = 0;
foreach (ObjectId id in civil.GetAllPointIds()) { var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead); if (string.Equals(p.RawDescription, "GRND", StringComparison.OrdinalIgnoreCase)) n++; }
return new { description = "GRND", count = n };'''
COUNT_COGO_TOOL = r'''
string description = args.Str("description");
if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("description is required.");
var n = 0;
foreach (ObjectId id in civil.GetAllPointIds())
{
    ct.ThrowIfCancellationRequested();
    var p = (CogoPoint)tr.GetObject(id, OpenMode.ForRead);
    if (string.Equals(p.RawDescription, description, StringComparison.OrdinalIgnoreCase)) n++;
}
log($"{n} COGO points described {description}");
return new { description, count = n, total = (int)civil.CogoPoints.Count, drawingUnit = units.Label };'''

# fragile on purpose: an unknown alignment name is a real failure (InvalidOperationException), not the caller's
ALIGNMENT_LENGTH_FRAGILE = r'''
string name = args.Str("alignment");
foreach (ObjectId id in civil.GetAlignmentIds()) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); if (a.Name == name) return new { name, lengthMm = units.ToMm(a.Length) }; }
throw new InvalidOperationException("alignment lookup failed for " + name);'''
ALIGNMENT_LENGTH_TOOL = r'''
string name = args.Str("alignment");
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("alignment is required.");
foreach (ObjectId id in civil.GetAlignmentIds()) { var a = (Alignment)tr.GetObject(id, OpenMode.ForRead); if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return new { name = a.Name, lengthMm = Math.Round(units.ToMm(a.Length), 1), drawingUnit = units.Label }; }
throw new ArgumentException($"No alignment named '{name}' in this drawing.");'''


def schema(props, required):
    return {"type": "object", "properties": props, "required": required, "additionalProperties": False}


# ---- scenario helpers -------------------------------------------------------------------------------------------------
def execute(s, code, transaction="auto", dry_run=False, timeout_s=30, label="verify", args=None, wait=120.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return s.tool("execute_civil3d_code", params, timeout=wait)


def count(s, code):
    r = execute(s, code, transaction="none", label="count")
    assert not r.get("isError"), r
    return r["value"]


def cli(exe, *args):
    p = subprocess.run([exe, "registry", *args], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    return p.returncode, (p.stdout + p.stderr)


def library_root():
    return os.environ.get("HPCIVIL3D_MCP_Registry__LibraryPath") or os.path.join(os.environ["APPDATA"], "HPCivil3d", "McpServer", "tools-library")


def remove_tool_folder(category, name):
    folder = os.path.join(library_root(), category, name)
    if os.path.isdir(folder):
        shutil.rmtree(folder, ignore_errors=True)
        return True
    return False


def folder_hash(root):
    """Content hash of a folder; a SQLite `-shm` index is skipped because any reader rewrites its read marks."""
    h = hashlib.sha256()
    for dirpath, _, files in sorted(os.walk(root)):
        for f in sorted(files):
            if f.endswith("-shm"):
                continue
            p = os.path.join(dirpath, f)
            h.update(os.path.relpath(p, root).encode()); h.update(open(p, "rb").read())
    return h.hexdigest()[:16]


def wait_for_tool(s, name, present=True, seconds=10.0):
    t0 = time.time()
    while time.time() - t0 < seconds:
        if (name in s.tools()) == present:
            return time.time() - t0
        time.sleep(0.25)
    return None


# ---- A: start-up + context ------------------------------------------------------------------------------------------------
def scenario_a(s, scene):
    ctx = open_drawing(s, scene, MAIN_DRAWING)
    civ = ctx.get("civil3d") or {}
    save("a-context", ctx)
    check("A get_civil3d_context: civil3d block on the main drawing", ctx.get("host") == "civil3d" and civ.get("isCivilDocument") is True and civ.get("drawingUnit") in ("Meters", "Feet")
          and civ.get("alignmentCount", 0) >= 1 and civ.get("cogoPointCount", 0) >= 1 and "revitVersion" not in ctx, short(civ))
    check("A units.length follows the Civil drawing unit", (ctx.get("units") or {}).get("length") == civ.get("drawingUnit"), short(ctx.get("units")))
    res = s.rpc("resources/read", {"uri": "civil3d://document/info"})
    text = json.loads(((res.get("result") or res).get("contents") or [{}])[0].get("text") or "{}")
    check("A civil3d://document/info resource = the same snapshot", text.get("host") == "civil3d" and (text.get("civil3d") or {}).get("isCivilDocument") is True, short(text.get("civil3d")))
    insp = s.tool("inspect_type", {"typeName": "Autodesk.Civil.DatabaseServices.Alignment", "memberFilter": "Station", "maxMembers": 200})
    members = [m.get("signature", "") for m in insp.get("members", [])] if isinstance(insp, dict) else []
    check("A inspect_type on a Civil type (filter Station)", not insp.get("isError") and any("StartingStation" in m for m in members) and any("GetStationStringWithEquations" in m for m in members), f"{len(members)} members: " + short(members, 200))


# ---- E: execute matrix --------------------------------------------------------------------------------------------------
def audit_lines():
    audit = os.path.join(os.environ["APPDATA"], "HPCivil3d", "McpBridge", "audit")
    return sum(len(open(os.path.join(audit, f), encoding="utf-8", errors="replace").read().splitlines()) for f in os.listdir(audit)) if os.path.isdir(audit) else 0


def scenario_e(s):
    audit_before = audit_lines()
    r = execute(s, "return db.Filename;", transaction="none", label="read")
    check("E read (none)", not r.get("isError") and isinstance(r.get("value"), str) and r.get("runId"), short(r))

    base = count(s, COUNT_LINES)
    r = execute(s, ADD_LINE, dry_run=True, label="dry line", args={"lengthMm": 500})
    after = count(s, COUNT_LINES)
    check("E dryRun", not r.get("isError") and r.get("changed", {}).get("added") == 1 and r.get("rolledBack") is True and after == base, f"changed={r.get('changed')} count {base}->{after}")

    regen()
    r = execute(s, ADD_LINE, label="real line", args={"lengthMm": 1234.5})
    mid = count(s, COUNT_LINES)
    check("E commit", not r.get("isError") and r.get("changed", {}).get("added") == 1 and r.get("rolledBack") is False and mid == base + 1 and r.get("runId") and r.get("hint"), f"runId={r.get('runId')} count {base}->{mid}")
    undo(s)
    after = count(s, COUNT_LINES)
    check("E undo (U after a REGEN boundary reverts the run)", after == base, f"count {base}->{mid}->{after}")
    base = after

    r = execute(s, ADD_LINE_BODY + 'throw new InvalidOperationException("boom after append");', label="throw")
    after = count(s, COUNT_LINES)
    check("E exception rolls back", r.get("isError") and r.get("rolledBack") is True and "boom" in (r.get("message") or "") and after == base, f"message={r.get('message')}")

    r = execute(s, ADD_LINE, transaction="none", label="none modify")
    after = count(s, COUNT_LINES)
    check("E none + modify refused", r.get("isError") and 'transaction="none"' in (r.get("message") or "") and after == base, f"message={r.get('message')}")

    r = execute(s, ADD_LINE, transaction="manual", label="manual like auto")
    after = count(s, COUNT_LINES)
    check("E manual runs like auto", not r.get("isError") and after == base + 1 and any("manual" in l for l in r.get("logs", [])), f"logs={r.get('logs')} count {base}->{after}")
    base = after

    for label, code, needle in [("Corridor.RebuildAll", "civil.CorridorCollection.RebuildAll(); return 1;", "RebuildAll"),
                                ("DataShortcuts", 'Autodesk.Civil.DataShortcuts.DataShortcuts.SetWorkingFolder("x"); return 1;', "SetWorkingFolder"),
                                ("ExportToDEM", 'var s = (TinSurface)tr.GetObject(civil.GetSurfaceIds()[0], OpenMode.ForRead); s.ExportToDEM("x", 1); return 1;', "ExportToDEM"),
                                ("ed.GetPoint", 'var pt = ed.GetPoint("pick"); return pt.Value;', "GetPoint"),
                                ("tr.Commit", "tr.Commit(); return 1;", "Commit"),
                                ("StartTransaction", "using (var t = db.TransactionManager.StartTransaction()) { t.Commit(); } return 1;", "StartTransaction"),
                                ("System.IO.File", 'return System.IO.File.Exists("C:\\\\x");', "System.IO")]:
        r = execute(s, code, label="guard")
        check(f"E guard denies {label}", r.get("isError") and any(needle in d.get("message", "") for d in r.get("diagnostics", [])), short(r.get("diagnostics")))

    r = execute(s, "return nothingHere + 1;", label="compile")
    check("E compile error", r.get("isError") and any(d.get("id", "").startswith("CS") for d in r.get("diagnostics", [])), short(r.get("diagnostics")))

    code = ADD_LINE_BODY + 'var end = DateTime.Now.AddSeconds(20); while (DateTime.Now < end) { ct.ThrowIfCancellationRequested(); } return "not cancelled";'
    rid = s.send("tools/call", {"name": "execute_civil3d_code", "arguments": {"code": code, "transaction": "auto", "timeoutSeconds": 60, "label": "cancel me"}})
    time.sleep(2.0)
    cid = s.send("tools/call", {"name": "cancel_execution", "arguments": {}})
    cancel = mcp_session.parse_tool_result(s.wait(cid, 60))
    r = mcp_session.parse_tool_result(s.wait(rid, 60))
    after = count(s, COUNT_LINES)
    check("E cancel_execution", cancel.get("cancelled") is True and r.get("isError") and r.get("rolledBack") is True and not r.get("timedOut") and after == base, f"cancel={short(cancel)} message={r.get('message')}")

    code = ADD_LINE_BODY + 'var end = DateTime.Now.AddSeconds(7); while (DateTime.Now < end) { } return "finished late";'
    r = execute(s, code, timeout_s=5, label="timeout", wait=90)
    after = count(s, COUNT_LINES)
    check("E timeout 5 s", r.get("isError") and r.get("timedOut") is True and r.get("rolledBack") is True and after == base, f"message={r.get('message')} durationMs={r.get('durationMs')}")

    r = execute(s, 'for (var i = 1; i <= 3; i++) { progress(i, 3, "step " + i); log("line " + i); } return new { n = args.Int("n", 0), pt = new Point3d(1, 2, 3), a = tr.GetObject(civil.GetAlignmentIds()[0], OpenMode.ForRead) };', transaction="none", label="progress", args={"n": 7})
    v = r.get("value") or {}
    check("E logs + args + serializer (Point3d, Civil entity with name)", not r.get("isError") and r.get("logs") == ["line 1", "line 2", "line 3"] and v.get("n") == 7 and v.get("pt", {}).get("z") == 3 and v.get("a", {}).get("type") == "Alignment" and v.get("a", {}).get("name"), short(v))

    r = execute(s, OUTSIDE_SURFACE, transaction="none", label="civil exception")
    check("E a Civil exception is reported as {Type}: {Message}", r.get("isError") and (r.get("message") or "").startswith("PointNotOnEntityException:"), str(r.get("message"))[:120])

    lines = audit_lines()
    check("E audit grew by one line per run of this matrix", lines - audit_before >= 15, f"{audit_before} -> {lines} audit lines")


def scenario_busy(s):
    acad_com("$a.ActiveDocument.SendCommand('_LINE ')", wait=False)
    time.sleep(2.5)
    t0 = time.time()
    r = execute(s, "return 1;", transaction="none", label="busy", wait=60)
    elapsed = time.time() - t0
    check("E busy -> refused after the grace", r.get("isError") and "Press ESC" in (r.get("message") or "") and 5 <= elapsed <= 30, f"message={r.get('message')} after {elapsed:.1f}s")
    how = cancel_command(s)
    r = execute(s, "return 2;", transaction="none", label="after esc", wait=60)
    check("E ESC then retry succeeds", not r.get("isError") and r.get("value") == 2, f"cancelled via {how}; " + short(r))


def scenario_disabled(s):
    r = execute(s, "return 1;", transaction="none", label="disabled")
    check("E opt-in off -> refused", r.get("isError") and "Allow AI code execution" in (r.get("message") or "") and "Civil 3D" in (r.get("message") or ""), short(r))


def scenario_nodoc(s):
    ctx = context(s)
    r = execute(s, "return 1;", transaction="none", label="nodoc")
    check("E no drawing -> context without doc, execute refused", not ctx.get("docTitle") and r.get("isError") and "Open one first" in (r.get("message") or ""), f"openDocs={ctx.get('openDocs')} message={r.get('message')}")


# ---- S: the 12 seeds ------------------------------------------------------------------------------------------------------
def scenario_s(s, scene):
    names = s.tools()
    seeds = ["get_civil_document_info", "list_alignments", "get_alignment_geometry", "list_profiles", "list_surfaces", "get_surface_elevation",
             "list_corridors", "list_pipe_networks", "list_parcels", "list_cogo_points", "create_cogo_points", "create_alignment_from_polyline"]
    check("S tools/list holds the 12 seeds", all(n in names for n in seeds) and len(names) >= 24, f"{len(names)} tools")
    open_drawing(s, scene, MAIN_DRAWING)

    info = s.tool("get_civil_document_info", {})
    v = info.get("value") or {}
    unit = v.get("drawingUnit")
    check("S get_civil_document_info", not info.get("isError") and v.get("success") and v.get("counts", {}).get("alignments", 0) >= 1 and unit in ("Meters", "Feet") and v.get("insunitsMismatch") is False and len(v.get("styles", {}).get("alignment", [])) >= 1, short(v.get("counts")))

    al = s.tool("list_alignments", {"limit": 50})
    first = (al.get("value") or {}).get("items", [{}])[0]
    check("S list_alignments", not al.get("isError") and al["value"]["count"] >= 1 and first.get("lengthMm", 0) > 0 and first.get("endStationLabel") and al["value"]["drawingUnit"] == unit, short(first))
    geo = s.tool("get_alignment_geometry", {"alignment": first.get("name"), "sampleStepMm": 50000, "maxSamples": 50})
    g = geo.get("value") or {}
    check("S get_alignment_geometry (entities + samples, by name)", not geo.get("isError") and g.get("count", 0) >= 1 and g["alignment"]["handle"] == first.get("handle") and len(g.get("samples", [])) >= 2 and g["entities"][0]["start"]["x"] != 0, f"entities={g.get('count')} samples={len(g.get('samples', []))}")
    page = s.tool("get_alignment_geometry", {"alignment": first.get("handle"), "entityLimit": 1, "entityOffset": 1})
    check("S get_alignment_geometry pages entities (entityLimit 1, offset 1)", not page.get("isError") and page["value"]["count"] == 1 and page["value"]["entities"][0]["order"] == 1 and page["value"]["truncated"] == (page["value"]["entityCount"] > 2), short(page.get("value", {}).get("entities")))
    bad = s.tool("get_alignment_geometry", {"alignment": "NO-SUCH-ALIGNMENT"})
    check("S get_alignment_geometry refuses an unknown alignment as ArgumentException", bad.get("isError") and (bad.get("message") or "").startswith("ArgumentException"), short(bad.get("message")))

    pr = s.tool("list_profiles", {})
    check("S list_profiles", not pr.get("isError") and pr["value"]["count"] >= 1 and pr["value"]["items"][0].get("name") and pr["value"]["items"][0].get("alignment"), f"count={pr.get('value', {}).get('count')}")

    su = s.tool("list_surfaces", {})
    surface = next((i for i in (su.get("value") or {}).get("items", []) if i.get("pointCount", 0) > 0), None)
    check("S list_surfaces", not su.get("isError") and surface is not None and surface["boundsMm"]["maxX"] > surface["boundsMm"]["minX"], short(surface))
    cx = (surface["boundsMm"]["minX"] + surface["boundsMm"]["maxX"]) / 2; cy = (surface["boundsMm"]["minY"] + surface["boundsMm"]["maxY"]) / 2
    el = s.tool("get_surface_elevation", {"surface": surface["name"], "points": [{"x": cx, "y": cy}, {"x": 1e12, "y": 1e12}]})
    e = el.get("value") or {}
    check("S get_surface_elevation: centre answers, far point OUTSIDE_SURFACE, per-point", not el.get("isError") and e.get("okCount") == 1 and e.get("outsideCount") == 1 and e.get("success") is True and e["items"][1]["error"] == "OUTSIDE_SURFACE", short(e.get("items")))

    co = s.tool("list_corridors", {})
    check("S list_corridors (no rebuild)", not co.get("isError") and co["value"]["count"] >= 1 and co["value"]["items"][0]["baselines"][0].get("alignment") and "rebuildAutomatic" in co["value"]["items"][0], short(co["value"]["items"][0]))

    pn = s.tool("list_pipe_networks", {"includeParts": True, "partLimit": 2})
    n = (pn.get("value") or {}).get("items", [{}])[0]
    check("S list_pipe_networks: parts budget honoured (partLimit 2 -> partsTruncated)", not pn.get("isError") and n.get("pipeCount", 0) >= 1 and len(n.get("pipes", [])) + len(n.get("structures", [])) == 2 and n.get("partsTruncated") is True and pn["value"]["truncated"] is True, f"pipes={len(n.get('pipes', []))} structures={len(n.get('structures', []))}")
    pn = s.tool("list_pipe_networks", {"includeParts": True})
    save("s-pipe-networks", pn)
    n = ((pn.get("value") or {}).get("items") or [{}])[0]
    pipes, structures = n.get("pipes") or [], n.get("structures") or []
    check("S list_pipe_networks: full parts with slope and diameters in mm", not pn.get("isError") and pipes and pipes[0].get("length2DMm", 0) > 0 and pipes[0].get("innerDiameterMm", 0) > 0 and structures and "rimElevation" in structures[0],
          short(pipes[0]) if pipes else short(pn))

    cg = s.tool("list_cogo_points", {"limit": 5, "offset": 2})
    c = cg.get("value") or {}
    check("S list_cogo_points limit/offset in number order", not cg.get("isError") and c.get("count") == 5 and c.get("total", 0) >= 7 and c["items"][0]["number"] < c["items"][1]["number"] and c.get("offset") == 2, f"total={c.get('total')} first={c['items'][0]['number'] if c.get('items') else None}")
    rng = s.tool("list_cogo_points", {"numberFrom": 10, "numberTo": 12})
    check("S list_cogo_points number range (direct lookup path)", not rng.get("isError") and rng["value"]["count"] == 3 and [i["number"] for i in rng["value"]["items"]] == [10, 11, 12], short(rng.get("value", {}).get("items")))
    badgroup = s.tool("list_cogo_points", {"pointGroup": "NO-SUCH-GROUP"})
    check("S list_cogo_points refuses an unknown point group as ArgumentException", badgroup.get("isError") and (badgroup.get("message") or "").startswith("ArgumentException"), short(badgroup.get("message")))

    # W1: test_tool (dryRun of the examples) -> run_tool real -> U
    tested = s.tool("test_tool", {"name": "create_cogo_points"})
    check("S test_tool create_cogo_points (examples as dryRun)", tested.get("passed") == 2 and tested.get("failed") == 0, short(tested))
    total0 = count(s, COGO_COUNT)
    regen()
    real = s.tool("run_tool", {"name": "create_cogo_points", "args": {"points": [{"x": cx, "y": cy, "elevation": 12.5, "name": "MCP-LV"}], "description": "MCP LIVE"}})
    total1 = count(s, COGO_COUNT)
    undo(s)
    total2 = count(s, COGO_COUNT)
    check("S create_cogo_points real via run_tool then U", not real.get("isError") and real["value"]["createdCount"] == 1 and total1 == total0 + 1 and total2 == total0, f"count {total0}->{total1}->{total2} number={real.get('value', {}).get('items', [{}])[0].get('number')}")
    dup = s.tool("create_cogo_points", {"points": [{"x": 0, "y": 0, "name": "A"}, {"x": 1, "y": 1, "name": "A"}], "dryRun": True})
    check("S create_cogo_points refuses a duplicate name in the batch", dup.get("isError") and (dup.get("message") or "").startswith("ArgumentException"), short(dup.get("message")))

    # W2: polyline by execute -> test_tool (needs a handle: examples use a placeholder handle, so test_tool is expected to fail on ArgumentException) -> run_tool real -> U
    pl = execute(s, DRAW_POLYLINE, label="polyline", args={"x0": cx, "y0": cy})
    check("S polyline drawn for the alignment", not pl.get("isError") and pl.get("value"), f"handle={pl.get('value')}")
    a0 = count(s, ALIGNMENT_COUNT)
    dry = s.tool("create_alignment_from_polyline", {"polyline": pl.get("value"), "name": "MCP Live Road", "dryRun": True})
    a1 = count(s, ALIGNMENT_COUNT)
    check("S create_alignment_from_polyline dryRun rolls back", not dry.get("isError") and dry.get("rolledBack") is True and dry["value"]["alignment"]["entityCount"] >= 2 and a1 == a0, f"count {a0}->{a1} alignment={short(dry.get('value', {}).get('alignment'))}")
    regen()
    real = s.tool("run_tool", {"name": "create_alignment_from_polyline", "args": {"polyline": pl.get("value"), "name": "MCP Live Road"}})
    a2 = count(s, ALIGNMENT_COUNT)
    seen = s.tool("list_alignments", {"namePattern": "MCP Live*"})
    undo(s)
    a3 = count(s, ALIGNMENT_COUNT)
    check("S create_alignment_from_polyline real via run_tool -> listed -> U", not real.get("isError") and a2 == a0 + 1 and seen["value"]["count"] == 1 and a3 == a0, f"count {a0}->{a2}->{a3}")
    badlayer = s.tool("create_alignment_from_polyline", {"polyline": pl.get("value"), "name": "MCP Live Road 2", "layer": "NO-SUCH-LAYER", "dryRun": True})
    check("S create_alignment_from_polyline refuses an unknown layer as ArgumentException", badlayer.get("isError") and "Layer 'NO-SUCH-LAYER'" in (badlayer.get("message") or ""), short(badlayer.get("message")))

    # parcels + Meters units on the second drawing
    ctx2 = open_drawing(s, scene, PARCEL_DRAWING)
    unit2 = (ctx2.get("civil3d") or {}).get("drawingUnit")
    pa = s.tool("list_parcels", {"site": "Site 1", "limit": 10})
    p = pa.get("value") or {}
    check(f"S list_parcels on {PARCEL_DRAWING} (area in {unit2}²)", not pa.get("isError") and p.get("count", 0) >= 1 and p["items"][0]["area"] > 0 and p.get("areaUnit") == f"{unit2}²" and p.get("truncated") is True, f"count={p.get('count')} first={short(p.get('items', [{}])[0])}")
    check("S units differ per drawing and each context says so", unit != unit2 and (ctx2.get("units") or {}).get("length") == unit2, f"{MAIN_DRAWING}={unit} {PARCEL_DRAWING}={unit2}")
    open_drawing(s, scene, MAIN_DRAWING)


# ---- R: MISS -> memory -> HIT; fragile -> quarantine -> restore ------------------------------------------------------------
def scenario_r(s, exe):
    name = "mcp_verify_count_cogo_points"
    miss = s.tool("search_tools", {"query": "count COGO points with a description", "limit": 5})
    check("R search misses (no stored tool for the task)", not any(t.get("name") == name for t in miss.get("tools", [])), [t.get("name") for t in miss.get("tools", [])])

    adhoc = execute(s, COUNT_COGO_ADHOC, transaction="none", label="count GRND")
    check("R ad-hoc run + hint", not adhoc.get("isError") and adhoc["value"]["count"] >= 1 and adhoc.get("runId") and "propose_tool" in (adhoc.get("hint") or ""), f"count={adhoc.get('value', {}).get('count')} runId={adhoc.get('runId')}")
    run_id = adhoc.get("runId")
    expected = adhoc["value"]["count"]

    run = s.tool("get_run", {"runId": run_id, "analyze": True})
    lits = [l.get("value") for l in (run.get("analysis") or {}).get("literals", [])]
    check("R get_run shows the literal to parameterise", run.get("codeAvailable") and any("GRND" in str(v) for v in lits), short(lits))

    prompt = s.prompt("toolify_run", {"runId": str(run_id)})
    ptext = json.dumps(prompt, ensure_ascii=False)
    check("R toolify_run prompt mentions the run and Civil 3D", str(run_id) in ptext and "Civil 3D" in ptext and "propose_tool" in ptext, f"{len(ptext)} chars")

    proposed = s.tool("propose_tool", {
        "name": name, "title": "Count COGO points by description",
        "description": "Counts the COGO points whose raw description equals the given text (case-insensitive); read-only.",
        "category": "point", "tags": ["cogo", "count"],
        "inputSchema": schema({"description": {"type": "string", "description": "Raw description to match"}}, ["description"]),
        "code": COUNT_COGO_TOOL,
        "examples": [{"title": "ground shots", "args": {"description": "GRND"}}, {"title": "trees", "args": {"description": "TREE"}}],
        "transaction": "none", "timeoutSeconds": 60, "sourceRunId": run_id})
    tool_json = json.load(open(os.path.join(proposed.get("folder") or "", "tool.json"), encoding="utf-8")) if proposed.get("folder") else {}
    check("R propose_tool accepted; tool.json stamped host=civil3d, category normalised", proposed.get("accepted") and proposed.get("status") == "draft" and tool_json.get("host") == "civil3d" and tool_json.get("category") == "Point" and tool_json.get("hostVersions") == ["2026"], short(proposed))

    tested = s.tool("test_tool", {"name": name})
    check("R test_tool 2/2", tested.get("passed") == 2 and tested.get("failed") == 0 and tested.get("status") == "tested", short(tested))

    t_before = s.tools()
    published = s.tool("publish_tool", {"name": name})
    review = published.get("reviewFile")
    review_text = open(review, encoding="utf-8").read() if review and os.path.exists(review) else ""
    check("R publish_tool -> pending_approval + review names this exe and the host", published.get("status") == "pending_approval" and "**Host:** civil3d" in review_text and "HPCivil3d.Mcp.Server.exe registry approve" in review_text and "HPAutoCad" not in review_text and name not in t_before, f"review={review}")
    gated = s.tool("run_tool", {"name": name, "args": {"description": "GRND"}})
    check("R run_tool refused while pending", gated.get("isError") and "pending" in (gated.get("message") or "").lower(), short(gated.get("message")))

    t0 = time.time()
    code, out = cli(exe, "approve", name, "--by", "harness (CLI)")
    check("R CLI approve on the Civil exe", code == 0 and "published" in out.lower(), out.strip()[:200])
    latency = wait_for_tool(s, name, present=True, seconds=15)
    changed = len(s.list_changed_since(t0))
    check("R running server lists the tool after approve (no restart, tools/list_changed sent)", latency is not None and latency <= 5 and changed >= 1, f"visible after {latency}s; list_changed={changed}")

    hit = s.tool("search_tools", {"query": "count COGO points by description", "limit": 5})
    check("R search now hits", any(t.get("name") == name for t in hit.get("tools", [])), [t.get("name") for t in hit.get("tools", [])])
    r = s.tool(name, {"description": "GRND"})
    check("R call by name gives the ad-hoc answer", not r.get("isError") and r["value"]["count"] == expected and r.get("runId"), f"count={r.get('value', {}).get('count')} expected={expected}")
    detail = s.tool("get_tool", {"name": name})
    check("R get_tool: approved by the harness, from the run", "harness" in json.dumps(detail) and str(run_id) in json.dumps(detail), short(detail))

    # fragile -> quarantine -> restore (examples name two real alignments so test_tool passes; the unknown name is the real failure)
    fragile = "mcp_verify_alignment_length"
    names = [i["name"] for i in (s.tool("list_alignments", {"limit": 10}).get("value") or {}).get("items", [])]
    examples = [{"title": "first", "args": {"alignment": names[0]}}, {"title": "second", "args": {"alignment": names[1] if len(names) > 1 else names[0]}}]
    proposed = s.tool("propose_tool", {
        "name": fragile, "title": "Alignment length by name", "description": "Length in mm of the alignment with the given name; fails when the name is unknown.",
        "category": "Alignment", "tags": ["alignment", "length"],
        "inputSchema": schema({"alignment": {"type": "string"}}, ["alignment"]),
        "code": ALIGNMENT_LENGTH_FRAGILE,
        "examples": examples,
        "transaction": "none", "timeoutSeconds": 30})
    tested = s.tool("test_tool", {"name": fragile})
    published = s.tool("publish_tool", {"name": fragile})
    code, out = cli(exe, "approve", fragile, "--by", "harness (CLI)")
    check("R fragile tool proposed/tested/published/approved", proposed.get("accepted") and tested.get("failed") == 0 and published.get("status") == "pending_approval" and code == 0 and wait_for_tool(s, fragile, True, 15) is not None, f"tested={tested.get('passed')}/{tested.get('failed')}")
    r = s.tool(fragile, {"alignment": names[0]})
    check("R fragile tool works on a real alignment", not r.get("isError") and r["value"]["lengthMm"] > 0, short(r.get("value")))
    fails = sum(1 for _ in range(5) if s.tool(fragile, {"alignment": "NONEXISTENT"}).get("isError"))
    gone = wait_for_tool(s, fragile, present=False, seconds=10)
    detail = s.tool("get_tool", {"name": fragile})
    check("R 5 failing runs (InvalidOperationException) -> quarantined, out of tools/list", fails == 5 and gone is not None and detail.get("status") == "quarantined", f"failures={fails} removed after {gone}s status={detail.get('status')}")
    refused = s.tool("run_tool", {"name": fragile, "args": {"alignment": names[0]}})
    check("R run_tool refuses a quarantined tool", refused.get("isError") and "quarantin" in (refused.get("message") or "").lower(), short(refused.get("message")))
    restored = s.tool("manage_tool", {"name": fragile, "action": "restore", "reason": "guard the name"})
    fixed = s.tool("propose_tool", {
        "name": fragile, "title": "Alignment length by name", "description": "Length in mm of the alignment with the given name; refuses an unknown name.",
        "category": "Alignment", "tags": ["alignment", "length"],
        "inputSchema": schema({"alignment": {"type": "string"}}, ["alignment"]),
        "code": ALIGNMENT_LENGTH_TOOL,
        "examples": examples,
        "transaction": "none", "timeoutSeconds": 30, "newVersion": True})
    tested = s.tool("test_tool", {"name": fragile})
    published = s.tool("publish_tool", {"name": fragile})
    code, out = cli(exe, "approve", fragile, "--by", "harness (CLI)")
    back = wait_for_tool(s, fragile, True, 15)
    check("R restore -> fix (newVersion) -> test -> publish -> approve -> back", (restored.get("status") or "").lower() == "draft" and fixed.get("accepted") and fixed.get("version") == 2 and tested.get("failed") == 0 and code == 0 and back is not None, f"v={fixed.get('version')} back after {back}s")
    for _ in range(5):
        r = s.tool(fragile, {"alignment": "NONEXISTENT"})
    still = wait_for_tool(s, fragile, True, 3)
    detail = s.tool("get_tool", {"name": fragile})
    check("R 5 argument errors do not quarantine the guarded tool", r.get("isError") and "ArgumentException" in (r.get("message") or "") and still is not None and detail.get("status") == "published", f"status={detail.get('status')}")

    # proposals the analyzer/guard must refuse
    rebuild = s.tool("propose_tool", {"name": "mcp_verify_rebuild", "title": "Rebuild", "description": "Rebuilds every corridor in the drawing before reading it.",
                                      "category": "Corridor", "tags": ["corridor"], "inputSchema": schema({}, []),
                                      "code": "civil.CorridorCollection.RebuildAll(); return civil.CorridorCollection.Count;",
                                      "examples": [{"title": "a", "args": {}}, {"title": "b", "args": {}}], "transaction": "auto", "timeoutSeconds": 30})
    check("R propose_tool with RebuildAll is refused by the guard", not rebuild.get("accepted") and "Rebuild" in json.dumps(rebuild), short(rebuild))
    none_write = s.tool("propose_tool", {"name": "mcp_verify_none_write", "title": "Write under none", "description": "Adds a COGO point although it says it only reads.",
                                         "category": "Point", "tags": ["cogo"], "inputSchema": schema({}, []),
                                         "code": "civil.CogoPoints.Add(new Point3d(0, 0, 0), true); return 1;",
                                         "examples": [{"title": "a", "args": {}}, {"title": "b", "args": {}}], "transaction": "none", "timeoutSeconds": 30})
    tested = s.tool("test_tool", {"name": "mcp_verify_none_write"}) if none_write.get("accepted") else {}
    check("R a `none` tool that writes fails its own test (bridge refuses the modification)", (not none_write.get("accepted")) or (tested.get("failed", 0) >= 1), short(none_write) if not none_write.get("accepted") else short(tested))


# ---- X: the AutoCAD exe beside --------------------------------------------------------------------------------------------
def live_roots():
    """The user's real registry roots of both products: this harness must leave them exactly as they were. The Civil root is
    compared only while the server is pointed at an isolated one (with -UseLiveRegistry writing it is the point)."""
    products = [("HPAutoCad", os.path.join(os.environ["APPDATA"], "HPAutoCad", "McpServer"))]
    if os.environ.get("HPCIVIL3D_MCP_Registry__LibraryPath"):
        products.append(("HPCivil3d", os.path.join(os.environ["APPDATA"], "HPCivil3d", "McpServer")))
    return {p: (folder_hash(r) if os.path.isdir(r) else None) for p, r in products}


def scenario_x(autocad_exe, out):
    # the AutoCAD server gets an isolated root of its own under the run folder, never the user's %AppData%\HPAutoCad\McpServer
    iso = os.path.join(out or os.getcwd(), "registry-autocad")
    os.makedirs(os.path.join(iso, "tools-library"), exist_ok=True)
    env = dict(os.environ, HPAUTOCAD_MCP_Registry__LibraryPath=os.path.join(iso, "tools-library"), HPAUTOCAD_MCP_Registry__DbPath=os.path.join(iso, "registry.db"))
    ac = Server(autocad_exe, env=env, name="autocad")
    try:
        init = ac.initialize()
        tools = ac.tools()
        check("X AutoCAD exe beside: its own tool surface, no civil3d names", len(tools) >= 24 and "execute_autocad_code" in tools and not any("civil3d" in n for n in tools), f"{init.get('serverInfo')} {len(tools)} tools")
        ctx = ac.tool("get_autocad_context", {"includeSelection": False})
        # no AutoCAD is running here (Civil 3D is), so the AutoCAD server must report its bridge as not connected — not Civil's
        check("X AutoCAD server does not reach the Civil bridge (different pipe)", ctx.get("isError") and "AutoCAD" in (ctx.get("message") or "") and "hpautocad-mcp-2026" in (ctx.get("message") or ""), short(ctx))
    finally:
        ac.close()
    check("X AutoCAD server used the isolated root (seeds installed there)", os.path.isdir(os.path.join(iso, "tools-library", "Layer", "list_layers")), iso)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", required=True, help="published HPCivil3d.Mcp.Server.exe")
    ap.add_argument("--scene-dir", required=True, help="folder with the copied tutorial drawings")
    ap.add_argument("--autocad-exe", default="")
    ap.add_argument("--out", default="")
    ap.add_argument("--only", default="", help="comma list of a,e,busy,disabled,nodoc,s,r,x (default: a,e,s,r + x when --autocad-exe)")
    ap.add_argument("--reset-verify-tools", action="store_true", help="delete the harness's own tool folders from the library first (re-runnable)")
    a = ap.parse_args()
    CL.out_dir = a.out or None
    only = [x for x in a.only.split(",") if x] or ["a", "e", "s", "r"] + (["x"] if a.autocad_exe else [])

    if a.reset_verify_tools:
        for cat, n in [("Point", "mcp_verify_count_cogo_points"), ("Alignment", "mcp_verify_alignment_length"), ("Corridor", "mcp_verify_rebuild"), ("Point", "mcp_verify_none_write")]:
            assert n.startswith("mcp_verify_")  # only the harness's own tools are ever removed, whatever root is in use
            if remove_tool_folder(cat, n):
                print(f"reset: removed {cat}/{n} from the library", flush=True)

    roots_before = live_roots()
    s = Server(a.exe, name="civil3d")
    try:
        init = s.initialize()
        print(f"initialize: {init.get('serverInfo')}", flush=True)
        if "disabled" in only: scenario_disabled(s)
        if "nodoc" in only: scenario_nodoc(s)
        if "a" in only: scenario_a(s, a.scene_dir)
        if "e" in only: scenario_e(s)
        if "busy" in only: scenario_busy(s)
        if "s" in only: scenario_s(s, a.scene_dir)
        if "r" in only: scenario_r(s, a.exe)
    except Exception as exception:
        check("run aborted", False, repr(exception))
    finally:
        s.close()
    if "x" in only and a.autocad_exe:
        try:
            scenario_x(a.autocad_exe, a.out)
        except Exception as exception:
            check("X aborted", False, repr(exception))
    elif "x" in only:
        skip("X AutoCAD exe beside", "no --autocad-exe (publish HPAutoCad.Mcp.Server first)")
    after_roots = live_roots()
    check("live registry roots of both products untouched by this session", after_roots == roots_before, f"{roots_before} -> {after_roots}")
    for proc in detached:
        try: proc.kill()
        except Exception: pass
    sys.exit(CL.finish("summary-" + "-".join(only)))


if __name__ == "__main__":
    main()
