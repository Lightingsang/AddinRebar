"""Live verification of the AutoCAD MCP server against a running AutoCAD 2026, over stdio, in one session:
  A  execute matrix (read, dryRun, commit + undo, exception, none+modify, manual, guard, compile, cancel, timeout, busy)
  B  every seed at least once (insert_block with a real block, get_selected_entities with a pickfirst set)
  C  MISS → memory: ad-hoc run → get_run → toolify_run → propose → test → publish → CLI approve → tools/list_changed → call by name
  D  unguarded tool → 5 failing runs → quarantined → restore → fix (newVersion) → test → publish → approve → back;
     argument errors (ArgumentException) never count against stability
  E  Revit exe side by side: 34 tools, context, a seed, a dryRun; library untouched
The PowerShell wrapper (run-live-verify.ps1) starts AutoCAD, toggles the opt-in and calls this with --only for the
steps that need the box off (disabled) or no drawing (nodoc). Prints PASS/FAIL lines and a JSON summary.
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
OUT = None


# ---- AutoCAD side helpers (COM through Windows PowerShell 5.1, pid-guarded; ESC through the focused window) -------
def acad_com(script, wait=True):
    expected = os.environ.get("HP_HARNESS_ACAD_PID", "")
    if not expected:
        return False
    ps = ("$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); "
          f"if ($ids.Count -ne 1 -or [string]$ids[0] -ne '{expected}') {{ throw \"refusing COM: acad pids $ids, harness owns '{expected}'\" }}; "
          "$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " + script)
    if wait:
        subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=90)
        return True
    proc = subprocess.Popen(["powershell", "-NoProfile", "-Command", ps], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, stdin=subprocess.DEVNULL)
    detached.append(proc)
    return True


def press_escape(times=2):
    """Posts ESC to the window that has keyboard focus in the harness's AutoCAD (its command line while LINE
    waits). PostMessage, not SendKeys: nothing here depends on which window is in the foreground."""
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


# ---- scripts ---------------------------------------------------------------------------------------------------
COUNT_LINES = 'var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead); var n = 0; foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") n++; return n;'
ADD_LINE_BODY = ('var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite); '
                 'var line = new Line(Point3d.Origin, new Point3d(units.ToDrawing(args.Double("lengthMm", 1000)), 0, 0)); '
                 'ms.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true); log("added " + line.Handle); ')
ADD_LINE = ADD_LINE_BODY + 'return new { handle = line.Handle.ToString(), length = line.Length };'

MAKE_BLOCK = r'''
string name = args.Str("name", "MCP-BLOCK");
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
if (bt.Has(name)) return new { name, created = false };
var btr = new BlockTableRecord { Name = name };
var id = bt.Add(btr); tr.AddNewlyCreatedDBObject(btr, true);
var c = new Circle(Point3d.Origin, Vector3d.ZAxis, units.ToDrawing(250)); btr.AppendEntity(c); tr.AddNewlyCreatedDBObject(c, true);
var att = new AttributeDefinition(new Point3d(0, units.ToDrawing(300), 0), "D00", "TAG", "Tag", db.Textstyle) { Height = units.ToDrawing(100) };
btr.AppendEntity(att); tr.AddNewlyCreatedDBObject(att, true);
return new { name, created = true, handle = id.Handle.ToString() };'''

MOVE_TEXT_ADHOC = r'''
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
if (!lt.Has("TXT-B")) throw new ArgumentException("Layer 'TXT-B' does not exist.");
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
var moved = 0;
foreach (ObjectId id in ms)
{
    if (tr.GetObject(id, OpenMode.ForRead) is not Entity e || e is not (DBText or MText)) continue;
    if (!string.Equals(e.Layer, "TXT-A", StringComparison.OrdinalIgnoreCase)) continue;
    e.UpgradeOpen(); e.Layer = "TXT-B"; moved++;
}
return new { moved };'''

MOVE_TEXT_TOOL = r'''
string fromLayer = args.Str("fromLayer");
string toLayer = args.Str("toLayer");
if (string.IsNullOrWhiteSpace(fromLayer) || string.IsNullOrWhiteSpace(toLayer)) throw new ArgumentException("fromLayer and toLayer are required.");
var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
if (!lt.Has(toLayer)) throw new ArgumentException($"Layer '{toLayer}' does not exist; run create_layer first.");
var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
var moved = 0;
foreach (ObjectId id in ms)
{
    ct.ThrowIfCancellationRequested();
    if (tr.GetObject(id, OpenMode.ForRead) is not Entity e || e is not (DBText or MText)) continue;
    if (!string.Equals(e.Layer, fromLayer, StringComparison.OrdinalIgnoreCase)) continue;
    e.UpgradeOpen(); e.Layer = toLayer; moved++;
}
log($"{moved} text entities moved from {fromLayer} to {toLayer}");
return new { moved, fromLayer, toLayer };'''

# Fragile on purpose: no guard, so a missing block surfaces as AutoCAD's eKeyNotFound — a real tool failure.
COUNT_REFS_FRAGILE = r'''
string blockName = args.Str("blockName");
bool directOnly = args.Bool("directOnly", true);
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
return new { blockName, references = btr.GetBlockReferenceIds(directOnly, false).Count };'''

# The fix: validate the argument, so a wrong call is reported as the caller's (ArgumentException) and never counts
# against the tool's stability.
COUNT_REFS_TOOL = r'''
string blockName = args.Str("blockName");
bool directOnly = args.Bool("directOnly", true);
if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName is required.");
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
if (!bt.Has(blockName)) throw new ArgumentException($"Block '{blockName}' is not defined in this drawing.");
var btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
return new { blockName, references = btr.GetBlockReferenceIds(directOnly, false).Count };'''


def schema(props, required):
    return {"type": "object", "properties": props, "required": required, "additionalProperties": False}


# ---- scenario helpers --------------------------------------------------------------------------------------------
def execute(s, code, transaction="auto", dry_run=False, timeout_s=30, label="verify", args=None, wait=120.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return s.tool("execute_autocad_code", params, timeout=wait)


def count_lines(s):
    r = execute(s, COUNT_LINES, transaction="none", label="count")
    assert not r.get("isError"), r
    return r["value"]


def cli(exe, *args):
    p = subprocess.run([exe, "registry", *args], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    return p.returncode, (p.stdout + p.stderr)


def library_root():
    # The wrapper points the server at an isolated root through the same option override the exe honours.
    return os.environ.get("HPAUTOCAD_MCP_Registry__LibraryPath") or os.path.join(os.environ["APPDATA"], "HPAutoCad", "McpServer", "tools-library")


def remove_tool_folder(category, name):
    folder = os.path.join(library_root(), category, name)
    if os.path.isdir(folder):
        shutil.rmtree(folder, ignore_errors=True)
        return True
    return False


def folder_hash(root):
    h = hashlib.sha256()
    for dirpath, _, files in sorted(os.walk(root)):
        for f in sorted(files):
            p = os.path.join(dirpath, f)
            h.update(os.path.relpath(p, root).encode()); h.update(open(p, "rb").read())
    return h.hexdigest()[:16]


def wait_for_tool(s, name, present=True, seconds=10.0):
    t0 = time.time()
    while time.time() - t0 < seconds:
        has = name in s.tools()
        if has == present:
            return time.time() - t0
        time.sleep(0.25)
    return None


# ---- A: execute matrix ---------------------------------------------------------------------------------------------
def scenario_a(s):
    r = execute(s, "return db.Filename;", transaction="none", label="read")
    check("A read (none)", not r.get("isError") and isinstance(r.get("value"), str) and r.get("runId"), short(r))

    base = count_lines(s)
    r = execute(s, ADD_LINE, dry_run=True, label="dry line", args={"lengthMm": 500})
    after = count_lines(s)
    check("A dryRun", not r.get("isError") and r.get("changed", {}).get("added") == 1 and r.get("rolledBack") is True and after == base, f"changed={r.get('changed')} count {base}->{after}")

    acad_com("$a.ActiveDocument.SendCommand('_REGEN ')"); time.sleep(1)
    r = execute(s, ADD_LINE, label="real line", args={"lengthMm": 1234.5})
    mid = count_lines(s)
    check("A commit", not r.get("isError") and r.get("changed", {}).get("added") == 1 and r.get("rolledBack") is False and mid == base + 1 and r.get("runId") and r.get("hint"), f"runId={r.get('runId')} count {base}->{mid} hint={str(r.get('hint'))[:60]}")
    acad_com("$a.ActiveDocument.SendCommand('_U ')"); time.sleep(2)
    after = count_lines(s)
    check("A undo (U after a REGEN boundary reverts the run)", after == base, f"count {base}->{mid}->{after}")
    base = after

    r = execute(s, ADD_LINE_BODY + 'throw new InvalidOperationException("boom after append");', label="throw")
    after = count_lines(s)
    check("A exception rolls back", r.get("isError") and r.get("rolledBack") is True and "boom" in (r.get("message") or "") and after == base, f"message={r.get('message')} count {after}")

    r = execute(s, ADD_LINE, transaction="none", label="none modify")
    after = count_lines(s)
    check("A none + modify refused", r.get("isError") and 'transaction="none"' in (r.get("message") or "") and after == base, f"message={r.get('message')}")

    r = execute(s, "using (var t = db.TransactionManager.StartTransaction()) { t.Commit(); } return 1;", transaction="manual", label="manual nested")
    check("A guard denies StartTransaction (manual)", r.get("isError") and any("StartTransaction" in d.get("message", "") for d in r.get("diagnostics", [])), short(r.get("diagnostics")))
    r = execute(s, ADD_LINE, transaction="manual", label="manual like auto")
    after = count_lines(s)
    check("A manual runs like auto", not r.get("isError") and after == base + 1 and any("manual" in l for l in r.get("logs", [])), f"logs={r.get('logs')} count {base}->{after}")
    base = after

    for label, code, needle in [("ed.GetPoint", 'var pt = ed.GetPoint("pick"); return pt.Value;', "GetPoint"),
                                ("tr.Commit", "tr.Commit(); return 1;", "Commit"),
                                ("SendStringToExecute", 'doc.SendStringToExecute("_LINE ", true, false, false); return 1;', "SendStringToExecute")]:
        r = execute(s, code, label="guard")
        check(f"A guard denies {label}", r.get("isError") and any(needle in d.get("message", "") for d in r.get("diagnostics", [])), short(r.get("diagnostics")))

    r = execute(s, "return nothingHere + 1;", label="compile")
    check("A compile error", r.get("isError") and any(d.get("id", "").startswith("CS") for d in r.get("diagnostics", [])), short(r.get("diagnostics")))

    # cancel: send the execute, then cancel_execution while the script spins on the main thread
    code = ADD_LINE_BODY + 'var end = DateTime.Now.AddSeconds(20); while (DateTime.Now < end) { ct.ThrowIfCancellationRequested(); } return "not cancelled";'
    rid = s.send("tools/call", {"name": "execute_autocad_code", "arguments": {"code": code, "transaction": "auto", "timeoutSeconds": 60, "label": "cancel me"}})
    time.sleep(2.0)
    cid = s.send("tools/call", {"name": "cancel_execution", "arguments": {}})
    cancel = mcp_session.parse_tool_result(s.wait(cid, 60))
    r = mcp_session.parse_tool_result(s.wait(rid, 60))
    after = count_lines(s)
    check("A cancel_execution", cancel.get("cancelled") is True and r.get("isError") and r.get("rolledBack") is True and not r.get("timedOut") and after == base, f"cancel={short(cancel)} message={r.get('message')} durationMs={r.get('durationMs')}")

    code = ADD_LINE_BODY + 'var end = DateTime.Now.AddSeconds(7); while (DateTime.Now < end) { } return "finished late";'
    r = execute(s, code, timeout_s=5, label="timeout", wait=90)
    after = count_lines(s)
    check("A timeout 5 s", r.get("isError") and r.get("timedOut") is True and r.get("rolledBack") is True and after == base, f"message={r.get('message')} durationMs={r.get('durationMs')}")

    r = execute(s, 'for (var i = 1; i <= 3; i++) { progress(i, 3, "step " + i); log("line " + i); } return new { n = args.Int("n", 0), pt = new Point3d(1, 2, 3) };', transaction="none", label="progress", args={"n": 7})
    check("A logs + args + serializer", not r.get("isError") and r.get("logs") == ["line 1", "line 2", "line 3"] and r.get("value", {}).get("n") == 7 and r.get("value", {}).get("pt", {}).get("z") == 3, short(r))

    audit = os.path.join(os.environ["APPDATA"], "HPAutoCad", "McpBridge", "audit")
    lines = sum(len(open(os.path.join(audit, f), encoding="utf-8", errors="replace").read().splitlines()) for f in os.listdir(audit)) if os.path.isdir(audit) else 0
    check("A audit lines exist", lines > 0, f"{lines} audit lines")


def scenario_busy(s):
    acad_com("$a.ActiveDocument.SendCommand('_LINE ')", wait=False)
    time.sleep(2.5)
    t0 = time.time()
    r = execute(s, "return 1;", transaction="none", label="busy", wait=60)
    elapsed = time.time() - t0
    check("A busy -> refused after the grace", r.get("isError") and "Press ESC" in (r.get("message") or "") and 5 <= elapsed <= 30, f"message={r.get('message')} after {elapsed:.1f}s")
    press_escape(3)
    time.sleep(2)
    r = execute(s, "return 2;", transaction="none", label="after esc", wait=60)
    check("A ESC then retry succeeds", not r.get("isError") and r.get("value") == 2, short(r))


def scenario_disabled(s):
    r = execute(s, "return 1;", transaction="none", label="disabled")
    check("A opt-in off -> refused", r.get("isError") and "Allow AI code execution" in (r.get("message") or ""), short(r))


def scenario_nodoc(s):
    ctx = s.tool("get_autocad_context", {"includeSelection": False})
    r = execute(s, "return 1;", transaction="none", label="nodoc")
    check("A no drawing -> context without doc, execute refused", not ctx.get("docTitle") and r.get("isError") and "Open one first" in (r.get("message") or ""), f"openDocs={ctx.get('openDocs')} message={r.get('message')}")


# ---- B: every seed ---------------------------------------------------------------------------------------------------
def scenario_b(s):
    names = s.tools()
    seeds = ["list_layers", "list_block_definitions", "get_entities", "list_layouts", "get_drawing_info", "get_selected_entities",
             "draw_polyline", "draw_circle", "add_text", "create_layer", "insert_block", "add_linear_dimension"]
    check("B tools/list holds the 12 seeds", all(n in names for n in seeds) and len(names) >= 24, f"{len(names)} tools")

    blk = execute(s, MAKE_BLOCK, label="make block", args={"name": "MCP-BLOCK"})
    check("B block definition MCP-BLOCK with an attribute (execute)", not blk.get("isError"), short(blk.get("value")))

    r = s.tool("create_layer", {"name": "MCP-VERIFY", "colorIndex": 3, "lineweight": 30})
    check("B create_layer", not r.get("isError") and r.get("value", {}).get("name") == "MCP-VERIFY", short(r.get("value")))
    r = s.tool("list_layers", {"includeCounts": True})
    check("B list_layers", not r.get("isError") and any(l.get("name") == "MCP-VERIFY" and l.get("lineweight") == 30 for l in r.get("value", [])), short(r.get("value")))
    r = s.tool("list_block_definitions", {})
    check("B list_block_definitions sees MCP-BLOCK", not r.get("isError") and any(b.get("name") == "MCP-BLOCK" and b.get("hasAttributes") for b in r.get("value", [])), short(r.get("value")))
    r = s.tool("insert_block", {"blockName": "MCP-BLOCK", "position": {"x": 5000, "y": 5000}, "scale": 2, "rotationDeg": 45, "layer": "MCP-VERIFY", "attributes": {"TAG": "D01"}})
    check("B insert_block with a real block + attribute", not r.get("isError") and r.get("value", {}).get("attributesSet") == 1 and r.get("changed", {}).get("added") >= 1, short(r))
    r = s.tool("insert_block", {"blockName": "MCP-BLOCK", "position": {"x": 6000, "y": 5000}, "dryRun": True})
    check("B insert_block dryRun rolls back", not r.get("isError") and r.get("rolledBack") is True, short(r.get("changed")))
    r = s.tool("draw_polyline", {"points": [{"x": 0, "y": 0}, {"x": 2000, "y": 0}, {"x": 2000, "y": 1000}], "closed": True, "layer": "MCP-VERIFY"})
    check("B draw_polyline", not r.get("isError") and r.get("value", {}).get("vertexCount") == 3, short(r.get("value")))
    r = s.tool("draw_polyline", {"points": [[0, 0], [1, 1]]})
    check("B draw_polyline rejects [x, y] arrays with a clear error", r.get("isError") and "{x, y}" in (r.get("message") or ""), short(r.get("message")))
    r = s.tool("draw_circle", {"center": {"x": 1000, "y": 500}, "radiusMm": 250})
    check("B draw_circle", not r.get("isError") and abs(r.get("value", {}).get("areaMm2", 0) - 196349.5) < 2, short(r.get("value")))
    r = s.tool("add_text", {"text": "VERIFY", "position": {"x": 0, "y": 3000}, "heightMm": 200, "mtext": False, "layer": "MCP-VERIFY"})
    check("B add_text (DBText)", not r.get("isError") and r.get("value", {}).get("type") == "DBText", short(r.get("value")))
    r = s.tool("add_text", {"text": "Line one\\PLine two", "position": {"x": 0, "y": 3500}, "heightMm": 200, "mtext": True, "widthMm": 3000})
    check("B add_text (MText, \\P break)", not r.get("isError") and r.get("value", {}).get("type") == "MText", short(r.get("value")))
    r = s.tool("add_linear_dimension", {"p1": {"x": 0, "y": 0}, "p2": {"x": 0, "y": 1000}, "dimLinePoint": {"x": -500, "y": 500}, "aligned": True})
    check("B add_linear_dimension aligned 1000 mm", not r.get("isError") and abs(r.get("value", {}).get("measurementMm", 0) - 1000) < 1, short(r.get("value")))
    r = s.tool("get_entities", {"layer": "MCP-VERIFY"})
    check("B get_entities on MCP-VERIFY (INSERT + LWPOLYLINE + TEXT)", not r.get("isError") and {i.get("type") for i in r.get("value", {}).get("items", [])} >= {"INSERT", "LWPOLYLINE", "TEXT"}, short(r.get("value")))
    r = s.tool("get_entities", {"type": "INSERT", "limit": 1})
    check("B get_entities type filter + limit", not r.get("isError") and r.get("value", {}).get("count") >= 1 and len(r.get("value", {}).get("items", [])) == 1, short(r.get("value")))
    r = s.tool("list_layouts", {})
    check("B list_layouts", not r.get("isError") and sum(1 for l in r.get("value", []) if l.get("isModel")) == 1, short(r.get("value")))
    r = s.tool("get_drawing_info", {"countByType": True})
    ext = r.get("value", {}).get("extentsMm")
    check("B get_drawing_info (counts, extents sane)", not r.get("isError") and r.get("value", {}).get("countsByType", {}).get("INSERT") == 1 and (ext is None or ext["min"]["x"] <= ext["max"]["x"]), short(r.get("value")))

    acad_com("$a.ActiveDocument.SendCommand('(sssetfirst nil (ssget ' + [char]34 + '_X' + [char]34 + ')) ')"); time.sleep(2)
    r = s.tool("get_selected_entities", {})
    check("B get_selected_entities after a pickfirst set (sssetfirst via COM)", not r.get("isError") and len(r.get("value", [])) >= 3, f"{len(r.get('value', []))} selected")
    acad_com("$a.ActiveDocument.SendCommand('(sssetfirst nil nil) ')"); time.sleep(1)


# ---- C: MISS -> memory -> HIT ------------------------------------------------------------------------------------------
def scenario_c(s, exe):
    name = "move_text_between_layers"
    s.tool("create_layer", {"name": "TXT-A", "colorIndex": 4}); s.tool("create_layer", {"name": "TXT-B", "colorIndex": 5})
    s.tool("add_text", {"text": "A1", "position": {"x": 100, "y": 100}, "heightMm": 100, "layer": "TXT-A"})
    s.tool("add_text", {"text": "A2", "position": {"x": 100, "y": 400}, "heightMm": 100, "layer": "TXT-A", "mtext": True})

    miss = s.tool("search_tools", {"query": "move text entities to another layer", "limit": 5})
    check("C search misses (no stored tool for the task)", not any(t.get("name") == name for t in miss.get("tools", [])), [t.get("name") for t in miss.get("tools", [])])

    dry = execute(s, MOVE_TEXT_ADHOC, dry_run=True, label="move text dry")
    check("C ad-hoc dryRun", not dry.get("isError") and dry.get("value", {}).get("moved") == 2 and dry.get("rolledBack") is True and dry.get("changed", {}).get("modified") == 2, short(dry))
    real = execute(s, MOVE_TEXT_ADHOC, label="move text")
    check("C ad-hoc real run + hint", not real.get("isError") and real.get("value", {}).get("moved") == 2 and real.get("runId") and "propose_tool" in (real.get("hint") or ""), f"runId={real.get('runId')}")
    run_id = real.get("runId")

    run = s.tool("get_run", {"runId": run_id, "analyze": True})
    lits = [l.get("value") for l in (run.get("analysis") or {}).get("literals", [])]
    check("C get_run shows the literals to parameterise", run.get("codeAvailable") and any("TXT-A" in str(v) for v in lits) and any("TXT-B" in str(v) for v in lits), short(lits))
    save("c-get-run", run)

    prompt = s.prompt("toolify_run", {"runId": str(run_id)})
    ptext = json.dumps(prompt, ensure_ascii=False)
    check("C toolify_run prompt mentions the run and AutoCAD", str(run_id) in ptext and "AutoCAD" in ptext and "propose_tool" in ptext, f"{len(ptext)} chars")

    proposed = s.tool("propose_tool", {
        "name": name, "title": "Move text between layers",
        "description": "Moves every DBText and MText entity in model space from one layer to another; the target layer must exist.",
        "category": "annotation", "tags": ["text", "layer"],
        "inputSchema": schema({"fromLayer": {"type": "string", "description": "Source layer"}, "toLayer": {"type": "string", "description": "Target layer (must exist)"}}, ["fromLayer", "toLayer"]),
        "code": MOVE_TEXT_TOOL,
        "examples": [{"title": "TXT-B back to TXT-A", "args": {"fromLayer": "TXT-B", "toLayer": "TXT-A"}}, {"title": "TXT-A to TXT-B", "args": {"fromLayer": "TXT-A", "toLayer": "TXT-B"}}],
        "transaction": "auto", "timeoutSeconds": 30, "sourceRunId": run_id})
    tool_json = json.load(open(os.path.join(proposed.get("folder") or "", "tool.json"), encoding="utf-8")) if proposed.get("folder") else {}
    check("C propose_tool accepted; tool.json stamped host=autocad, category normalised", proposed.get("accepted") and proposed.get("status") == "draft" and tool_json.get("host") == "autocad" and tool_json.get("category") == "Annotation" and tool_json.get("hostVersions") == ["2026"], short(proposed))
    save("c-propose", proposed)

    tested = s.tool("test_tool", {"name": name})
    check("C test_tool 2/2 dryRun", tested.get("passed") == 2 and tested.get("failed") == 0 and tested.get("status") == "tested", short(tested))

    t_before = s.tools()
    published = s.tool("publish_tool", {"name": name})
    review = published.get("reviewFile")
    review_text = open(review, encoding="utf-8").read() if review and os.path.exists(review) else ""
    check("C publish_tool -> pending_approval + review names this exe and the host", published.get("status") == "pending_approval" and "**Host:** autocad" in review_text and "HPAutoCad.Mcp.Server.exe registry approve" in review_text and "HPRebar.Mcp.Server.exe" not in review_text and name not in t_before, f"review={review}")
    gated = s.tool("run_tool", {"name": name, "args": {"fromLayer": "TXT-B", "toLayer": "TXT-A"}, "dryRun": True})
    check("C run_tool refused while pending", gated.get("isError") and "pending" in (gated.get("message") or "").lower(), short(gated.get("message")))

    t0 = time.time()
    code, out = cli(exe, "approve", name, "--by", "harness (CLI)")
    check("C CLI approve on the AutoCAD exe", code == 0 and "published" in out.lower(), out.strip()[:200])
    latency = wait_for_tool(s, name, present=True, seconds=15)
    check("C running server lists the tool after approve (no restart)", latency is not None and latency <= 5, f"visible after {latency}s; list_changed notifications={len(s.list_changed_since(t0))}")

    hit = s.tool("search_tools", {"query": "move text to another layer", "limit": 5})
    check("C search now hits", any(t.get("name") == name for t in hit.get("tools", [])), [t.get("name") for t in hit.get("tools", [])])
    r = s.tool(name, {"fromLayer": "TXT-B", "toLayer": "TXT-A", "dryRun": True})
    check("C call by name (dryRun)", not r.get("isError") and r.get("value", {}).get("moved") == 2 and r.get("rolledBack") is True, short(r))
    r = s.tool(name, {"fromLayer": "TXT-B", "toLayer": "TXT-A"})
    check("C call by name (real)", not r.get("isError") and r.get("value", {}).get("moved") == 2 and r.get("runId"), f"runId={r.get('runId')}")
    detail = s.tool("get_tool", {"name": name})
    save("c-get-tool", detail)
    check("C get_tool: approved, from run, runs counted", "harness" in json.dumps(detail) and str(run_id) in json.dumps(detail), short(detail))


# ---- D: fragile -> quarantine -> restore ---------------------------------------------------------------------------------
def scenario_d(s, exe):
    name = "mcp_verify_count_block_refs"
    proposed = s.tool("propose_tool", {
        "name": name, "title": "Count block references", "description": "Counts the references of a named block definition in the drawing; fails when the block is not defined.",
        "category": "Block", "tags": ["block", "count"],
        "inputSchema": schema({"blockName": {"type": "string"}, "directOnly": {"type": "boolean", "default": True}}, ["blockName"]),
        "code": COUNT_REFS_FRAGILE,
        "examples": [{"title": "direct", "args": {"blockName": "MCP-BLOCK"}}, {"title": "nested too", "args": {"blockName": "MCP-BLOCK", "directOnly": False}}],
        "transaction": "none", "timeoutSeconds": 30})
    tested = s.tool("test_tool", {"name": name})
    published = s.tool("publish_tool", {"name": name})
    code, out = cli(exe, "approve", name, "--by", "harness (CLI)")
    ok = proposed.get("accepted") and tested.get("failed") == 0 and published.get("status") == "pending_approval" and code == 0
    check("D propose/test/publish/approve a tool that needs a block", ok and wait_for_tool(s, name, True, 15) is not None, f"tested={tested.get('passed')}/{tested.get('failed')} approve={out.strip()[:80]}")

    r = s.tool(name, {"blockName": "MCP-BLOCK"})
    check("D tool works on the block that exists", not r.get("isError") and r.get("value", {}).get("references") == 1, short(r.get("value")))

    fails = 0
    for i in range(5):
        r = s.tool(name, {"blockName": "NONEXISTENT"})
        fails += 1 if r.get("isError") else 0
    gone = wait_for_tool(s, name, present=False, seconds=10)
    detail = s.tool("get_tool", {"name": name})
    save("d-quarantined", detail)
    check("D 5 failing runs (eKeyNotFound) -> quarantined automatically, out of tools/list", fails == 5 and gone is not None and "quarantined" in json.dumps(detail).lower(), f"failures={fails} removed after {gone}s status={detail.get('status') or detail.get('tool', {}).get('status')}")
    refused = s.tool("run_tool", {"name": name, "args": {"blockName": "MCP-BLOCK"}})
    check("D run_tool refuses a quarantined tool", refused.get("isError") and "quarantin" in (refused.get("message") or "").lower(), short(refused.get("message")))

    restored = s.tool("manage_tool", {"name": name, "action": "restore", "reason": "guard the block name"})
    fixed = s.tool("propose_tool", {
        "name": name, "title": "Count block references", "description": "Counts the references of a named block definition in the drawing; refuses an unknown block name.",
        "category": "Block", "tags": ["block", "count"],
        "inputSchema": schema({"blockName": {"type": "string"}, "directOnly": {"type": "boolean", "default": True}}, ["blockName"]),
        "code": COUNT_REFS_TOOL,
        "examples": [{"title": "direct", "args": {"blockName": "MCP-BLOCK"}}, {"title": "nested too", "args": {"blockName": "MCP-BLOCK", "directOnly": False}}],
        "transaction": "none", "timeoutSeconds": 30, "newVersion": True})
    tested = s.tool("test_tool", {"name": name})
    published = s.tool("publish_tool", {"name": name})
    code, out = cli(exe, "approve", name, "--by", "harness (CLI)")
    back = wait_for_tool(s, name, True, 15)
    check("D restore -> fix (newVersion) -> test -> publish -> approve -> back in tools/list", (restored.get("status") or "").lower() == "draft" and fixed.get("accepted") and fixed.get("version") == 2 and tested.get("failed") == 0 and published.get("status") == "pending_approval" and code == 0 and back is not None, f"restore={restored.get('status')} v={fixed.get('version')} tests={tested.get('passed')} back after {back}s")
    r = s.tool(name, {"blockName": "MCP-BLOCK"})
    check("D restored tool runs again", not r.get("isError") and r.get("value", {}).get("references") == 1, short(r.get("value")))

    for i in range(5):
        r = s.tool(name, {"blockName": "NONEXISTENT"})
    still = wait_for_tool(s, name, True, 3)
    detail = s.tool("get_tool", {"name": name})
    check("D 5 argument errors (unknown block) do not quarantine the guarded tool", r.get("isError") and "ArgumentException" in (r.get("message") or "") and still is not None and "published" in json.dumps(detail).lower(), f"status={detail.get('status')}")


# ---- E: Revit exe side by side ----------------------------------------------------------------------------------------------
def scenario_e(revit_exe):
    lib = os.path.join(os.environ["APPDATA"], "HPRebar", "McpServer", "tools-library")
    before = folder_hash(lib) if os.path.isdir(lib) else None
    rv = Server(revit_exe, name="revit")
    try:
        init = rv.initialize()
        tools = rv.tools()
        core = {"execute_revit_code", "get_revit_context", "inspect_type", "cancel_execution", "search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"}
        check("E Revit exe: core + registry + seeds (>= 34), Revit names only", len(tools) >= 34 and core <= set(tools) and not any("autocad" in n for n in tools), f"{init.get('serverInfo')} {len(tools)} tools")
        ctx = rv.tool("get_revit_context", {"includeSelection": False})
        check("E get_revit_context (Revit 2026 running)", not ctx.get("isError") and ctx.get("revitVersion") == "2026" and ctx.get("docTitle"), short(ctx))
        if ctx.get("executionEnabled"):
            stats = rv.tool("analyze_model_statistics", {})
            check("E seed analyze_model_statistics", not stats.get("isError") and stats.get("value"), short(stats.get("value")))
            dry = rv.tool("execute_revit_code", {"code": "return doc.Title;", "transaction": "none", "label": "verify"})
            check("E execute_revit_code none read", not dry.get("isError") and isinstance(dry.get("value"), str), short(dry))
        else:
            # The Revit opt-in is never persisted and this harness must not touch the user's Revit session.
            refused = rv.tool("execute_revit_code", {"code": "return doc.Title;", "transaction": "none", "label": "verify"})
            check("E execute_revit_code refused while the Revit opt-in is off", refused.get("isError") and "Allow AI code execution" in (refused.get("message") or ""), short(refused))
            skip("E seed analyze_model_statistics + execute_revit_code read", "tick 'Allow AI code execution' in Revit's HP MCP Bridge window and rerun --only e")
    finally:
        rv.close()
    after = folder_hash(lib) if os.path.isdir(lib) else None
    check("E Revit tools-library untouched", before == after and before is not None, f"{before} -> {after}")


def main():
    global OUT
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", required=True, help="published HPAutoCad.Mcp.Server.exe")
    ap.add_argument("--revit-exe", default="")
    ap.add_argument("--out", default="")
    ap.add_argument("--only", default="", help="comma list of a,busy,disabled,nodoc,b,c,d,e (default: a,b,c,d + e when --revit-exe)")
    ap.add_argument("--reset-verify-tools", action="store_true", help="delete the harness's own tool folders from the library first (re-runnable)")
    a = ap.parse_args()
    OUT = a.out or None
    CL.out_dir = OUT
    only = [x for x in a.only.split(",") if x] or ["a", "b", "c", "d"] + (["e"] if a.revit_exe else [])

    if a.reset_verify_tools:
        for cat, n in [("Annotation", "move_text_between_layers"), ("Block", "mcp_verify_count_block_refs")]:
            if remove_tool_folder(cat, n):
                print(f"reset: removed {cat}/{n} from the library", flush=True)

    s = Server(a.exe, name="autocad")
    try:
        init = s.initialize()
        print(f"initialize: {init.get('serverInfo')}", flush=True)
        if "disabled" in only: scenario_disabled(s)
        if "nodoc" in only: scenario_nodoc(s)
        if "a" in only: scenario_a(s)
        if "busy" in only: scenario_busy(s)
        if "b" in only: scenario_b(s)
        if "c" in only: scenario_c(s, a.exe)
        if "d" in only: scenario_d(s, a.exe)
    except Exception as exception:
        check("run aborted", False, repr(exception))
    finally:
        s.close()
    if "e" in only and a.revit_exe:
        try:
            scenario_e(a.revit_exe)
        except Exception as exception:
            check("E aborted", False, repr(exception))
    for proc in detached:
        try: proc.kill()
        except Exception: pass
    sys.exit(CL.finish("summary-" + "-".join(only)))


if __name__ == "__main__":
    main()
