"""Pipe harness: talks NDJSON JSON-RPC to the Civil 3D bridge over its named pipe and runs the execute
scenarios — the AutoCAD set (the bridge is a copy) plus the Civil ones. No MCP server involved. Prints one line per scenario
and a JSON summary at the end; exit code 1 when any scenario fails.

Usage: python pipe-scenarios.py [--pipe hpcivil3d-mcp-2026] [--only a,b,c]
Set HP_HARNESS_KEYS=1 when the PowerShell wrapper can type into Civil 3D (busy/undo scenarios).
"""
import json, sys, time, argparse, os, subprocess, ctypes, ctypes.wintypes as wt

PIPE = r"\\.\pipe\hpcivil3d-mcp-2026"
_id = 0
results = []
detached = []


class Pipe:
    def __init__(self, path):
        self.f = open(path, "r+b", buffering=0)

    def call(self, method, params=None, timeout=60.0):
        global _id
        _id += 1
        rid = _id
        msg = {"jsonrpc": "2.0", "id": rid, "method": method}
        if params is not None:
            msg["params"] = params
        self.f.write((json.dumps(msg) + "\n").encode("utf-8"))
        deadline = time.time() + timeout
        notes = []
        while time.time() < deadline:
            line = self.f.readline()
            if not line:
                raise RuntimeError("pipe closed")
            obj = json.loads(line.decode("utf-8"))
            if obj.get("id") == rid:
                obj["_notifications"] = notes
                return obj
            notes.append(obj)
        raise TimeoutError(method)

    def notify(self, method, params=None):
        msg = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            msg["params"] = params
        self.f.write((json.dumps(msg) + "\n").encode("utf-8"))

    def close(self):
        self.f.close()


def execute(p, code, transaction="auto", dry_run=False, timeout_s=30, label="harness", args=None, wait=90.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return p.call("civil3d.execute", params, timeout=wait)


COUNT_LINES = """
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
var n = 0; foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") n++;
return n;"""

ADD_LINE_BODY = """
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
var line = new Line(Point3d.Origin, new Point3d(units.ToDrawing(args.Double("lengthMm", 1000)), 0, 0));
ms.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true);
log("added " + line.Handle);"""
ADD_LINE = ADD_LINE_BODY + """
return new { handle = line.Handle.ToString(), layer = line.Layer, length = line.Length };"""


def count_lines(p):
    r = execute(p, COUNT_LINES, transaction="none", label="count")
    assert "result" in r, r
    return r["result"]["value"]


def check(name, cond, detail=""):
    results.append({"name": name, "pass": bool(cond), "detail": detail})
    print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:400])


def acad_com(script, wait=True):
    """Drives the AutoCAD the wrapper started through COM automation (Windows PowerShell 5.1 still has
    GetActiveObject). Refuses to run unless exactly one acad.exe exists and it is the harness's own
    (HP_HARNESS_ACAD_PID): the calls close drawings without saving. A command that waits for input
    (LINE) never returns from SendCommand, so those run detached."""
    if os.environ.get("HP_HARNESS_KEYS") != "1":
        return False
    expected = os.environ.get("HP_HARNESS_ACAD_PID", "")
    ps = ("$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); "
          f"if ($ids.Count -ne 1 -or [string]$ids[0] -ne '{expected}') {{ throw \"refusing COM: acad pids $ids, harness owns '{expected}'\" }}; "
          "$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); " + script)
    if wait:
        subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=60)
        return True
    # detached: no inherited handles, or the wrapper's pipeline waits for this child forever
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


def is_quiescent(p):
    try:
        return bool((p.call("civil3d.context").get("result", {}).get("autocad") or {}).get("isQuiescent"))
    except Exception:
        return False


def cancel_command(p, tries=3):
    """ESC until the editor is quiescent: PostMessage to the focused window first, then the WScript.Shell route
    (activate the harness's own acad.exe and type ESC) - Civil 3D keeps keyboard focus in a palette more often."""
    pid = os.environ.get("HP_HARNESS_ACAD_PID", "")
    for _ in range(tries):
        press_escape(2); time.sleep(1.5)
        if is_quiescent(p):
            return "postmessage"
        if pid:
            ps = (f"$sh = New-Object -ComObject WScript.Shell; if ($sh.AppActivate([int]{pid})) {{ Start-Sleep -Milliseconds 400; "
                  "$sh.SendKeys('{ESC}'); Start-Sleep -Milliseconds 300; $sh.SendKeys('{ESC}') }}")
            subprocess.run(["powershell", "-NoProfile", "-Command", ps], check=False, timeout=30, capture_output=True)
            time.sleep(1.5)
            if is_quiescent(p):
                return "sendkeys"
    return None


# ---- Civil scripts (drawing units in, drawing units out; the bridge converts nothing) --------------------------------
ALIGNMENTS = """
var list = new List<object>();
foreach (ObjectId id in civil.GetAlignmentIds()) list.Add(tr.GetObject(id, OpenMode.ForRead));
return list;"""

ALIGNMENT_ENTITIES = """
var ids = civil.GetAlignmentIds();
if (ids.Count == 0) throw new ArgumentException("no alignment in this drawing");
var a = (Alignment)tr.GetObject(ids[0], OpenMode.ForRead);
return new { name = a.Name, count = a.Entities.Count, e = a.Entities[0], s = a.Entities[0][0] };"""

COGO_COUNT = "return (int)civil.CogoPoints.Count;"

COGO_ADD = """
var id = civil.CogoPoints.Add(new Point3d(units.ToDrawing(args.Double("xMm", 1000)), units.ToDrawing(args.Double("yMm", 2000)), 12.5), "MCP harness", true);
return tr.GetObject(id, OpenMode.ForRead);"""

FIRST_STYLE = """
foreach (ObjectId id in civil.Styles.AlignmentStyles) return tr.GetObject(id, OpenMode.ForRead);
return null;"""

OUTSIDE_SURFACE = """
var ids = civil.GetSurfaceIds();
if (ids.Count == 0) throw new ArgumentException("no surface in this drawing");
var s = (TinSurface)tr.GetObject(ids[0], OpenMode.ForRead);
return s.FindElevationAtXY(1e9, 1e9);"""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--pipe", default=PIPE)
    ap.add_argument("--only", default="")
    ap.add_argument("--com", default="", help="run one COM statement against the harness's AutoCAD and exit")
    a = ap.parse_args()
    only = set(x for x in a.only.split(",") if x)

    if a.com:
        acad_com(a.com)
        return

    def want(k):
        return not only or k in only

    global _id
    p = Pipe(a.pipe)

    if "disabled" in only:
        r = execute(p, "return 1;", transaction="none", label="disabled")
        err = r.get("error", {})
        check("execute while opt-in off -> -32001", err.get("code") == -32001, json.dumps(err))
        p.close(); finish(); return

    if "nodoc" in only:
        r = p.call("civil3d.context")
        ctx = r.get("result", {})
        r2 = execute(p, "return 1;", transaction="none", label="nodoc")
        err = r2.get("error", {})
        check("no drawing -> context without doc, execute -32003", not ctx.get("docTitle") and err.get("code") == -32003, f"openDocs={ctx.get('openDocs')} error={err}")
        p.close(); finish(); return

    if want("ping"):
        r = p.call("civil3d.ping")
        check("ping", r.get("result", {}).get("pong") is True and r["result"].get("revitVersion") == "2026", json.dumps(r.get("result")))

    if want("context"):
        r = p.call("civil3d.context", {"includeSelection": True})
        res = r.get("result", {})
        check("context", res.get("host") == "civil3d" and res.get("docTitle") and res.get("autocad", {}).get("isQuiescent") is True,
              json.dumps({k: res.get(k) for k in ("host", "hostVersion", "docTitle", "units", "activeView", "autocad", "openDocs", "executionEnabled")}))

    if want("read"):
        r = execute(p, "return db.Filename;", transaction="none", label="read")
        res = r.get("result", {})
        check("read (none)", not res.get("isError") and isinstance(res.get("value"), str) and res.get("value"), json.dumps(res)[:300])

    base = count_lines(p)
    print(f"  model space lines before: {base}")

    if want("dryrun"):
        r = execute(p, ADD_LINE, dry_run=True, label="dry line", args={"lengthMm": 500})
        res = r.get("result", {})
        after = count_lines(p)
        check("dryRun line", not res.get("isError") and res.get("changed", {}).get("added") == 1 and res.get("rolledBack") is True and after == base,
              f"changed={res.get('changed')} rolledBack={res.get('rolledBack')} value={res.get('value')} count {base}->{after} logs={res.get('logs')}")

    if want("commit"):
        r = execute(p, ADD_LINE, label="real line", args={"lengthMm": 1234.5})
        res = r.get("result", {})
        after = count_lines(p)
        check("commit line", not res.get("isError") and res.get("changed", {}).get("added") == 1 and res.get("rolledBack") is False and after == base + 1,
              f"changed={res.get('changed')} value={res.get('value')} durationMs={res.get('durationMs')} count {base}->{after}")
        base = after

    if want("exception"):
        r = execute(p, ADD_LINE_BODY + '\nthrow new InvalidOperationException("boom after append");', label="throw")
        res = r.get("result", {})
        after = count_lines(p)
        check("exception rolls back", res.get("isError") and res.get("rolledBack") is True and "boom" in (res.get("message") or "") and after == base,
              f"message={res.get('message')} changed={res.get('changed')} count {after}")

    if want("none-modify"):
        r = execute(p, ADD_LINE, transaction="none", label="none modify")
        res = r.get("result", {})
        after = count_lines(p)
        check("none + modify refused", res.get("isError") and "transaction=\"none\"" in (res.get("message") or "") and after == base,
              f"message={res.get('message')} rolledBack={res.get('rolledBack')} count {after}")

    if want("manual"):
        # AutoCAD scripts may not start transactions of their own (a leaked wrapper crashes acad.exe at GC time):
        # the guard refuses StartTransaction, and transaction=manual runs like auto with a note in the logs.
        code = "using (var t = db.TransactionManager.StartTransaction()) { t.Commit(); } return 1;"
        r = execute(p, code, transaction="manual", label="manual nested")
        res = r.get("result", {})
        check("guard denies StartTransaction", res.get("isError") and any("StartTransaction" in d.get("message", "") for d in res.get("diagnostics", [])),
              json.dumps(res.get("diagnostics"))[:200])
        r = execute(p, ADD_LINE, transaction="manual", label="manual like auto")
        res = r.get("result", {})
        after = count_lines(p)
        check("manual runs like auto", not res.get("isError") and res.get("changed", {}).get("added") == 1 and after == base + 1 and any("manual" in l for l in res.get("logs", [])),
              f"changed={res.get('changed')} logs={res.get('logs')} count {base}->{after}")
        base = after

    if want("modify-erase"):
        code = """
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
var lines = new List<ObjectId>(); foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") lines.Add(id);
var first = (Line)tr.GetObject(lines[0], OpenMode.ForWrite); first.ColorIndex = 1;
var second = (Line)tr.GetObject(lines[1], OpenMode.ForWrite); second.Erase();
return lines.Count;"""
        r = execute(p, code, label="modify+erase")
        res = r.get("result", {})
        after = count_lines(p)
        check("modify + erase counted", not res.get("isError") and res.get("changed") == {"added": 0, "modified": 1, "deleted": 1} and after == base - 1,
              f"changed={res.get('changed')} message={res.get('message')} count {base}->{after}")
        base = after

    if want("guard"):
        r = execute(p, 'var pt = ed.GetPoint("pick"); return pt.Value;', label="guard")
        res = r.get("result", {})
        check("guard denies ed.GetPoint", res.get("isError") and any("GetPoint" in d.get("message", "") for d in res.get("diagnostics", [])),
              json.dumps(res.get("diagnostics"))[:200])

    if want("guard-tr"):
        r = execute(p, 'tr.Commit(); return 1;', label="guard tr")
        res = r.get("result", {})
        check("guard denies tr.Commit", res.get("isError") and res.get("diagnostics"), json.dumps(res.get("diagnostics"))[:200])

    if want("compile"):
        r = execute(p, "return nothingHere + 1;", label="compile")
        res = r.get("result", {})
        check("compile error", res.get("isError") and any(d.get("id", "").startswith("CS") for d in res.get("diagnostics", [])),
              json.dumps(res.get("diagnostics"))[:200])

    if want("cancel"):
        code = ADD_LINE_BODY + """
var end = DateTime.Now.AddSeconds(20);
while (DateTime.Now < end) { ct.ThrowIfCancellationRequested(); }
return "not cancelled";"""
        # Send the execute, then cancel after a second while the script spins on the main thread.
        _id += 1
        rid = _id
        p.f.write((json.dumps({"jsonrpc": "2.0", "id": rid, "method": "civil3d.execute",
                               "params": {"code": code, "transaction": "auto", "timeoutSeconds": 60, "label": "cancel me"}}) + "\n").encode())
        time.sleep(1.5)
        _id += 1
        cid = _id
        p.f.write((json.dumps({"jsonrpc": "2.0", "id": cid, "method": "civil3d.cancel"}) + "\n").encode())
        got = {}
        deadline = time.time() + 40
        while time.time() < deadline and len(got) < 2:
            line = p.f.readline()
            obj = json.loads(line.decode())
            if obj.get("id") in (rid, cid):
                got[obj["id"]] = obj
        res = got.get(rid, {}).get("result", {})
        after = count_lines(p)
        check("cancel", got.get(cid, {}).get("result", {}).get("cancelled") is True and res.get("isError") and res.get("rolledBack") is True and not res.get("timedOut") and after == base,
              f"cancel={got.get(cid, {}).get('result')} message={res.get('message')} durationMs={res.get('durationMs')} count {after}")

    if want("timeout"):
        code = ADD_LINE_BODY + """
var end = DateTime.Now.AddSeconds(7);
while (DateTime.Now < end) { }
return "finished late";"""
        r = execute(p, code, timeout_s=5, label="timeout", wait=60)
        res = r.get("result", {})
        after = count_lines(p)
        check("timeout", res.get("isError") and res.get("timedOut") is True and res.get("rolledBack") is True and after == base,
              f"message={res.get('message')} durationMs={res.get('durationMs')} count {after}")

    if want("progress"):
        code = "for (var i = 1; i <= 3; i++) { progress(i, 3, \"step \" + i); log(\"line \" + i); } return 3;"
        r = execute(p, code, transaction="none", label="progress")
        notes = [n for n in r.get("_notifications", []) if n.get("method") == "civil3d.progress"]
        check("progress + logs", len(notes) == 3 and r.get("result", {}).get("logs") == ["line 1", "line 2", "line 3"], f"notifications={len(notes)} logs={r.get('result', {}).get('logs')}")

    if want("args-value"):
        code = "return new { handle = db.BlockTableId, pt = new Point3d(1, 2, 3), n = args.Int(\"n\", 0), s = args.Str(\"s\") };"
        r = execute(p, code, transaction="none", label="serialize", args={"n": 7, "s": "hi"})
        res = r.get("result", {})
        v = res.get("value") or {}
        check("serializer + args", not res.get("isError") and v.get("n") == 7 and v.get("s") == "hi" and v.get("pt", {}).get("z") == 3 and "handle" in v.get("handle", {}),
              json.dumps(v)[:200])

    if "busy" in only and os.environ.get("HP_HARNESS_KEYS") == "1":
        acad_com("$a.ActiveDocument.SendCommand('_LINE ')", wait=False)
        time.sleep(2.5)
        t0 = time.time()
        r = execute(p, "return 1;", transaction="none", label="busy", wait=40)
        elapsed = time.time() - t0
        err = r.get("error", {})
        check("busy -> -32002 after grace", err.get("code") == -32002 and 6 <= elapsed <= 30, f"error={err} result={r.get('result')} after {elapsed:.1f}s")
        how = cancel_command(p)
        r = execute(p, "return 2;", transaction="none", label="after-esc")
        check("ESC then retry succeeds", not r.get("result", {}).get("isError") and r.get("result", {}).get("value") == 2, f"cancelled via {how}; {json.dumps(r.get('result'))[:160]}")

    if want("undo") and os.environ.get("HP_HARNESS_KEYS") == "1":
        # Runs happen in application context: AutoCAD merges everything since the last user command into
        # one undo step, so a user command (REGEN) marks the boundary the test needs.
        acad_com("$a.ActiveDocument.SendCommand('_REGEN ')")
        time.sleep(1)
        before = count_lines(p)
        r = execute(p, ADD_LINE, label="undo me")
        mid = count_lines(p)
        acad_com("$a.ActiveDocument.SendCommand('_U ')")
        time.sleep(2)
        after = count_lines(p)
        check("U reverts the MCP runs since the last user command", mid == before + 1 and after == before, f"count {before}->{mid}->{after}")

    if want("civil"):
        r = p.call("civil3d.context")
        res = r.get("result", {})
        civ = res.get("civil3d") or {}
        check("C1 context civil3d block on a Civil drawing", civ.get("isCivilDocument") is True and civ.get("drawingUnit") in ("Meters", "Feet")
              and res.get("units", {}).get("length") == civ.get("drawingUnit") and civ.get("insunitsMismatch") is False and civ.get("alignmentCount", 0) >= 1,
              json.dumps(civ)[:300])

        r = execute(p, ALIGNMENTS, transaction="none", label="alignments")
        res = r.get("result", {})
        items = res.get("value") or []
        first = items[0] if items else {}
        check("C2 alignments serialised as Civil entities {handle,type,layer,dxfName,name}", not res.get("isError") and len(items) >= 1
              and first.get("type") == "Alignment" and first.get("name") and first.get("handle") and first.get("dxfName"),
              json.dumps(first)[:200])

        r = execute(p, ALIGNMENT_ENTITIES, transaction="none", label="alignment entities")
        res = r.get("result", {})
        v = res.get("value") or {}
        e, sub = v.get("e") or {}, v.get("s") or {}
        check("C3 AlignmentEntity / AlignmentSubEntity serialised with stations", not res.get("isError") and e.get("entityType") and "subEntityCount" in e
              and sub.get("subEntityType") and sub.get("length", 0) > 0 and "start" in sub and "end" in sub,
              json.dumps(v)[:300])

        base = execute(p, COGO_COUNT, transaction="none", label="cogo count").get("result", {}).get("value")
        r = execute(p, COGO_ADD, dry_run=True, label="cogo dry", args={"xMm": 1000, "yMm": 2000})
        res = r.get("result", {})
        v = res.get("value") or {}
        after = execute(p, COGO_COUNT, transaction="none", label="cogo count").get("result", {}).get("value")
        check("C4 COGO point dryRun: serialised {number,x,y,elevation}, rolled back", not res.get("isError") and res.get("rolledBack") is True
              and v.get("type") == "CogoPoint" and isinstance(v.get("number"), int) and v.get("number") > 0 and abs(v.get("x", 0) - 1.0) < 1e-6 and v.get("elevation") == 12.5
              and after == base, f"value={json.dumps(v)[:200]} count {base}->{after}")

        if os.environ.get("HP_HARNESS_KEYS") == "1":
            acad_com("$a.ActiveDocument.SendCommand('_REGEN ')"); time.sleep(1)
            r = execute(p, COGO_ADD, label="cogo commit", args={"xMm": 1500, "yMm": 2500})
            res = r.get("result", {})
            mid = execute(p, COGO_COUNT, transaction="none", label="cogo count").get("result", {}).get("value")
            acad_com("$a.ActiveDocument.SendCommand('_U ')"); time.sleep(2)
            after = execute(p, COGO_COUNT, transaction="none", label="cogo count").get("result", {}).get("value")
            check("C5 COGO point commit then U", not res.get("isError") and mid == base + 1 and after == base, f"count {base}->{mid}->{after} changed={res.get('changed')}")

        r = execute(p, "civil.CorridorCollection.RebuildAll(); return 1;", label="guard rebuild")
        res = r.get("result", {})
        check("C6 guard denies RebuildAll", res.get("isError") and any("Rebuild" in d.get("message", "") for d in res.get("diagnostics", [])), json.dumps(res.get("diagnostics"))[:200])
        r = execute(p, 'Autodesk.Civil.DataShortcuts.DataShortcuts.SetWorkingFolder("x"); return 1;', label="guard shortcuts")
        res = r.get("result", {})
        check("C7 guard denies data shortcuts", res.get("isError") and res.get("diagnostics"), json.dumps(res.get("diagnostics"))[:200])

        r = execute(p, OUTSIDE_SURFACE, transaction="none", label="outside surface")
        res = r.get("result", {})
        check("C8 Civil exception reported as {Type}: {Message}", res.get("isError") and (res.get("message") or "").startswith("PointNotOnEntityException:"), str(res.get("message"))[:160])

        r = execute(p, FIRST_STYLE, transaction="none", label="style")
        res = r.get("result", {})
        v = res.get("value") or {}
        check("C9 Civil style serialised with its name", not res.get("isError") and v.get("handle") and v.get("name") and "Style" in (v.get("type") or ""), json.dumps(v)[:200])

    p.close()
    finish()


def finish():
    for proc in detached:
        try: proc.kill()
        except Exception: pass
    passed = sum(1 for r in results if r["pass"])
    print(json.dumps({"passed": passed, "total": len(results), "failed": [r["name"] for r in results if not r["pass"]]}))
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
