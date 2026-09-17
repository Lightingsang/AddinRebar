"""Live verification of the ETABS MCP server over stdio against the HPEtabs.McpBridge desktop app (and, for the
attached phases, a running ETABS 22 with a throw-away model). One file, one --phase per session, on an isolated
registry root; the PowerShell wrapper (run-live-verify.ps1) starts the bridge, ticks the opt-ins, clicks Attach and
calls this once per phase:
  disabled  - before the execution opt-in: execute refused naming the separate app, context answers with isAttached=false
  detached  - opt-in on, no ETABS attached: guard refusals (E20), static preview for a writing script, destructive refused
              with -32001, not-attached -> fast -32003, inspect_type over ETABSv1
  spike     - attached to ETABS: E17 compile+run GetNameList, E18 attach facts, E13 forced units, E13b GetNameList per receiver
  nomodel   - (user closed the model, ETABS still up) context / execute report the empty state (E12)
  modal     - (user opened a dialog in ETABS) execute answers busy within the grace (E15)
  closed    - (user closed ETABS) execute -> -32003 at once, context isAttached=false (E19 first half)
  bridge    - attached to a SAVED throw-away model, destructive OFF: read, previews, two writes with snapshot/presave/audit,
              exception after a write persists, D refused -32001, path policy, compile error, timeout (B1-B13)
  bridgedestructive - the wrapper ticked 'Allow destructive operations': the frames phase bridge added are deleted under D
              (cleanup) with snapshot + [destructive] audit, run-time path refusals, dryRun preview (D0-D5)
Prints PASS/FAIL lines and a JSON summary; exit 1 on any failure.
"""
import argparse, json, os, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
# the bookkeeping + stdio session helper every HP MCP harness shares (MCP folder -> McpShared, never the other way round)
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, ok, short, utf8_console  # noqa: E402

utf8_console()

CL = Checklist()
check, skip, save = CL.check, CL.skip, CL.save

# ---- scripts ---------------------------------------------------------------------------------------------------------
NAME_LIST = """
int n = 0; string[] names = null;
int ret = sapModel.PointObj.GetNameList(ref n, ref names);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from PointObj.GetNameList");
return new { points = n, first = names == null || names.Length == 0 ? null : names[0] };"""

UNITS_IN_RUN = """
var present = sapModel.GetPresentUnits().ToString();
var database = sapModel.GetDatabaseUnits().ToString();
int n = 0; string[] names = null;
int ret = sapModel.PointObj.GetNameList(ref n, ref names);
double x = 0, y = 0, z = 0;
int retC = n > 0 ? sapModel.PointObj.GetCoordCartesian(names[0], ref x, ref y, ref z) : -1;
return new { present, database, unitsLabel = units.Label, mmPerUnit = units.MmPerUnit, firstPoint = n > 0 ? names[0] : null, retC, x, y, z };"""

RECEIVERS = ["PointObj", "FrameObj", "AreaObj", "LoadPatterns", "LoadCases", "RespCombo", "PropFrame", "PropMaterial"]

NAME_LIST_PER_RECEIVER = """
var report = new Dictionary<string, string>();
{calls}
double baseElev = 0; int ns = 0; string[] storyNames = null; double[] elev = null, heights = null; bool[] master = null; string[] similar = null; bool[] splice = null; double[] spliceH = null; int[] color = null;
try {{ int r = sapModel.Story.GetStories_2(ref baseElev, ref ns, ref storyNames, ref elev, ref heights, ref master, ref similar, ref splice, ref spliceH, ref color); report["Story.GetStories_2"] = "ret=" + r + " count=" + ns; }}
catch (Exception e) {{ report["Story.GetStories_2"] = e.GetType().Name; }}
return report;"""

RECEIVER_CALL = """
try {{ int n{i} = 0; string[] names{i} = null; int r{i} = sapModel.{recv}.GetNameList(ref n{i}, ref names{i}); report["{recv}"] = "ret=" + r{i} + " count=" + n{i}; }}
catch (Exception e) {{ report["{recv}"] = e.GetType().Name + ": " + e.Message; }}"""

GUARD_CASES = [
    ("etabs.ApplicationExit(false); return 1;", ".ApplicationExit"),
    ("var h = new Helper(); return 1;", "Helper"),
    ("var o = Marshal.GetActiveObject(\"x\"); return 1;", "Marshal"),
    ("var e = Expression.Call(Expression.Constant(sapModel), \"InitializeNewModel\", null); return 1;", "Expression"),
    ("HPEtabs.McpBridge.BridgeEntry.Dispose(); return 1;", "HPEtabs.McpBridge"),
    ("var c = HPRebar.McpBridge.Core.Host.McpBridgeHost.Current; return 1;", "HPRebar.McpBridge.Core.Host"),
    ("global::System.IO.File.WriteAllText(\"x\", \"y\"); return 1;", "System.IO"),
]


# ---- helpers -----------------------------------------------------------------------------------------------------------
def execute(s, code, transaction="none", dry_run=False, timeout_s=30, label="verify", args=None, wait=120.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return s.tool("execute_etabs_code", params, timeout=wait)


def msg(r):
    return (r.get("message") or "")


def diag_ids(r):
    return [d.get("id") for d in (r.get("diagnostics") or [])]


# ---- phases ----------------------------------------------------------------------------------------------------------
def phase_disabled(s):
    r = execute(s, "return 1;", label="disabled")
    ctx = s.tool("get_etabs_context", {})
    check("D1 opt-in off: execute refused naming the separate app (-32001)", r.get("isError") and "Allow AI code execution" in msg(r) and "not inside ETABS" in msg(r), short(msg(r)))
    check("D2 context answers while the opt-in is off: host etabs, isAttached false", ok(ctx) and ctx.get("host") == "etabs" and (ctx.get("etabs") or {}).get("isAttached") is False, short(ctx))
    save("disabled-context", ctx)


def phase_detached(s):
    ctx = s.tool("get_etabs_context", {})
    check("X1 context before attach: isAttached false, no units, isModifiable false", ok(ctx) and (ctx.get("etabs") or {}).get("isAttached") is False and "presentUnits" not in (ctx.get("etabs") or {}) and ctx.get("isModifiable") is False, short(ctx))

    t0 = time.time()
    r = execute(s, "return sapModel.GetModelFilename();", label="not attached")
    dt = time.time() - t0
    check("E19a not attached: execute refused at once with 'click Attach' (no 8 s wait)", r.get("isError") and "click Attach" in msg(r) and dt < 3.0, f"{short(msg(r))} in {dt:.1f}s")

    for i, (code, expected) in enumerate(GUARD_CASES, 1):
        r = execute(s, code, label=f"guard {i}")
        check(f"E20.{i} guard refuses `{code[:48]}`", r.get("isError") and "GUARD" in diag_ids(r) and any(expected in d.get("message", "") for d in r.get("diagnostics") or []), short(diag_ids(r) + [msg(r)]))

    r = execute(s, "return sapModel.File.Save();", transaction="auto", label="file member")
    check("E20.8 `sapModel.File.Save()` passes the guard (File in member position) and is tiered destructive → -32001", r.get("isError") and "Allow destructive operations" in msg(r) and "GUARD" not in diag_ids(r), short(msg(r)))

    r = execute(s, "int ret = sapModel.FrameObj.SetSection(\"F1\", \"C40x40\"); return ret;", transaction="none", label="write preview")
    check("P1 writing script under none → static preview: isError, PREVIEW diagnostic names cFrameObj.SetSection (W), nothing ran", r.get("isError") and "PREVIEW" in diag_ids(r) and any("cFrameObj.SetSection (W)" == d.get("message", "") for d in r["diagnostics"]) and r.get("rolledBack") is True, short(r))

    r = execute(s, "int ret = sapModel.FrameObj.SetSection(\"F1\", \"C40x40\"); return ret;", transaction="auto", label="write not attached")
    check("P1b writing script under auto while not attached → -32003 'click Attach' before any save", r.get("isError") and "click Attach" in msg(r) and not r.get("snapshot"), short(msg(r)))

    r = execute(s, "return sapModel.Analyze.RunAnalysis();", transaction="auto", label="destructive off")
    check("P2 destructive member with the second opt-in off → -32001 naming 'Allow destructive operations'", r.get("isError") and "Allow destructive operations" in msg(r), short(msg(r)))

    r = execute(s, "var x = 1; return x + 1;", label="pure read")
    check("E19b a script with no OAPI member still needs the attachment (not attached → refused)", r.get("isError") and "click Attach" in msg(r), short(msg(r)))

    ins = s.tool("inspect_type", {"typeName": "ETABSv1.cSapModel", "maxMembers": 20})
    check("I1 inspect_type sees the ETABSv1 wrapper (cSapModel members)", ok(ins) and any("GetPresentUnits" in m.get("signature", "") or "GetModelFilename" in m.get("signature", "") for m in ins.get("members") or []), short(ins)[:200])

    ins = s.tool("inspect_type", {"typeName": "ETABSv1.cHelper", "maxMembers": 30})
    check("I2 inspect_type answers for cHelper too (the guard, not the inspector, keeps it out of scripts)", ok(ins) and any("GetObject" in m.get("signature", "") for m in ins.get("members") or []), short(ins)[:200])


def phase_spike(s):
    ctx = s.tool("get_etabs_context", {})
    e = ctx.get("etabs") or {}
    check("E18 attached: isAttached, attachedPid > 0, oapiVersion 2.10.0.0, docTitle present", ok(ctx) and e.get("isAttached") is True and (e.get("attachedPid") or 0) > 0 and str(e.get("oapiVersion")).startswith("2.10") and bool(ctx.get("docTitle")) and ctx.get("docTitle") != "(Untitled)" and bool(ctx.get("docPath")), short(ctx))
    save("spike-context", ctx)

    t0 = time.time()
    r = execute(s, NAME_LIST, label="E17 name list")
    check("E17 Roslyn compile + run against ETABSv1 through stdio: PointObj.GetNameList returns an int", ok(r) and isinstance((r.get("value") or {}).get("points"), int), f"{short(r)} in {time.time() - t0:.1f}s")

    r = execute(s, UNITS_IN_RUN, label="E13 units")
    v = r.get("value") or {}
    check("E13 units forced inside the run: GetPresentUnits == kN_mm_C, units.Label kN_mm_C, GetCoordCartesian ret 0 with mm coordinates", ok(r) and v.get("present") == "kN_mm_C" and v.get("unitsLabel") == "kN_mm_C" and v.get("retC") in (0, -1), short(v))
    save("spike-units", r)

    ctx2 = s.tool("get_etabs_context", {})
    check("E13 units restored after the run: presentUnits in context is the user's own again", ok(ctx2) and (ctx2.get("etabs") or {}).get("presentUnits") == e.get("presentUnits"), f"before {e.get('presentUnits')} after {(ctx2.get('etabs') or {}).get('presentUnits')}")

    calls = "".join(RECEIVER_CALL.format(i=i, recv=recv) for i, recv in enumerate(RECEIVERS))
    r = execute(s, NAME_LIST_PER_RECEIVER.format(calls=calls), label="E13b receivers", timeout_s=60)
    v = r.get("value") or {}
    check("E13b GetNameList answers on every receiver the fingerprint will read (and Story.GetStories_2)", ok(r) and all(str(v.get(k, "")).startswith("ret=0") for k in RECEIVERS) and str(v.get("Story.GetStories_2", "")).startswith("ret=0"), short(v))
    save("spike-receivers", r)

    r = execute(s, "int i = 0; while (true) { ct.ThrowIfCancellationRequested(); i++; if (i % 1000 == 0) log(\"tick\"); }", timeout_s=5, label="timeout", wait=40)
    check("T1 cooperative timeout 5 s on a loop that checks ct: timedOut, run marked failed", r.get("isError") and r.get("timedOut") is True, short(msg(r)))


def phase_nomodel(s):
    ctx = s.tool("get_etabs_context", {})
    r = execute(s, NAME_LIST, label="E12 no model")
    w = execute(s, "return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", transaction="auto", label="E12b write no model")
    check("E12b no model open: a writing script under auto is refused with -32003 'No model (.EDB)' before any save", w.get("isError") and "No model (.EDB)" in msg(w) and not w.get("snapshot"), short(msg(w)))
    e = ctx.get("etabs") or {}
    check("E12 no model open: still attached, docTitle/docPath absent (not '(Untitled)'), openDocs empty, isModifiable false, a read still runs",
          ok(ctx) and e.get("isAttached") is True and not ctx.get("docTitle") and not ctx.get("docPath") and not ctx.get("openDocs") and ctx.get("isModifiable") is False and ok(r),
          f"ctx={short(ctx)[:200]} exec={short(r)[:200]}")
    save("nomodel-context", ctx)
    save("nomodel-execute", r)


def phase_modal(s):
    t0 = time.time()
    r = execute(s, "return sapModel.GetModelFilename();", label="E15 modal", wait=60)
    dt = time.time() - t0
    busy = r.get("isError") and ("busy" in msg(r).lower() or "-32002" in msg(r))
    check("E15 modal open in ETABS: either busy (-32002) after the 8 s grace, or the read answers (dialogs do not block the OAPI)",
          (busy and dt >= 8.0) or (ok(r) and dt < 8.0), f"{short(r)[:200]} in {dt:.1f}s")
    save("modal-execute", {"result": r, "seconds": round(dt, 1)})


# ---- phase 2: the writing runtime -----------------------------------------------------------------------------------------
ADD_FRAME = """
string name = "";
double dx = args.Double("dx", 0);
int ret = sapModel.FrameObj.AddByCoord(dx, 0, 0, dx, 0, 3000, ref name);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from FrameObj.AddByCoord");
return name;"""

ADD_FRAME_THEN_THROW = ADD_FRAME.replace("return name;", "log(\"added \" + name); throw new InvalidOperationException(\"after the write: \" + name);")

FIRST_SECTION = """
int n = 0; string[] names = null;
int ret = sapModel.PropFrame.GetNameList(ref n, ref names);
return n > 0 ? names[0] : null;"""


def snapshot_dir(model_title):
    """Mirrors EtabsSnapshotManager.ModelDirectory: SanitizeLabel(stem, 60) — ASCII [A-Za-z0-9_-], others '_', trimmed, cut at 60."""
    stem = os.path.splitext(model_title or "")[0]
    stem = "".join(c if (c.isascii() and c.isalnum()) or c in "_-" else "_" for c in stem).strip("_-") or "script"
    return os.path.join(os.environ.get("LOCALAPPDATA", ""), "HPEtabs", "McpBridge", "snapshots", stem[:60])


def audit_lines():
    import glob
    folder = os.path.join(os.environ.get("APPDATA", ""), "HPEtabs", "McpBridge", "audit")
    files = sorted(glob.glob(os.path.join(folder, "audit-*.log")))
    if not files:
        return []
    with open(files[-1], encoding="utf-8") as f:
        return [line for line in f.read().splitlines() if line.strip()]


def phase_bridge(s):
    """Attached, model saved, destructive opt-in OFF: reads run, writes snapshot first, exceptions persist, D refused, paths policed."""
    ctx = s.tool("get_etabs_context", {})
    e = ctx.get("etabs") or {}
    title = ctx.get("docTitle")
    if not (ok(ctx) and e.get("isAttached") and title and ctx.get("docPath")):
        check("B1 attached to a saved model (docTitle/docPath present)", False, short(ctx))
        return
    check("B1 attached to a saved model (docTitle/docPath present)", True, f"{title} frames={e.get('frameCount')} units={e.get('presentUnits')}")
    frames_before = e.get("frameCount") or 0
    units_before = e.get("presentUnits")
    snap = snapshot_dir(title)
    prerun_before = len(os.listdir(os.path.join(snap, "prerun"))) if os.path.isdir(os.path.join(snap, "prerun")) else 0
    presave_before = len(os.listdir(os.path.join(snap, "presave"))) if os.path.isdir(os.path.join(snap, "presave")) else 0
    audit_before = len(audit_lines())

    r = execute(s, "return sapModel.GetModelFilename();", label="B2 read")
    check("B2 read-only script under none runs: value, changed 0/0/0, no snapshot", ok(r) and isinstance(r.get("value"), str) and r.get("changed") == {"added": 0, "modified": 0, "deleted": 0} and not r.get("snapshot"), short(r))

    r = execute(s, "return sapModel.FrameObj.SetSection(\"F1\", \"C40x40\");", label="B3 preview none")
    check("B3 writing script under none → static preview: isError, rolledBack, PREVIEW cFrameObj.SetSection (W)", r.get("isError") and r.get("rolledBack") is True and any(d.get("id") == "PREVIEW" and "cFrameObj.SetSection (W)" == d.get("message") for d in r.get("diagnostics") or []), short(r))

    r = execute(s, ADD_FRAME, transaction="auto", dry_run=True, label="B4 preview dryRun", args={"dx": 1000})
    ctx2 = s.tool("get_etabs_context", {})
    check("B4 writing script with dryRun → static preview, nothing ran (frame count unchanged, no snapshot)", r.get("isError") and "PREVIEW" in diag_ids(r) and (ctx2.get("etabs") or {}).get("frameCount") == frames_before and not r.get("snapshot"), short(r))

    added = []
    t0 = time.time()
    r = execute(s, ADD_FRAME, transaction="auto", label="B5 add frame", args={"dx": 1000}, timeout_s=60, wait=90)
    dt = time.time() - t0
    name = r.get("value") if ok(r) else None
    if name:
        added.append(name)
    snapshot = r.get("snapshot") or ""
    ctx3 = s.tool("get_etabs_context", {})
    check("B5 writing script under auto runs: value = new frame name, changed.added = 1 frame + its 2 points (log 'FrameObj: +1'), snapshot = file name only, frame count +1",
          ok(r) and bool(name) and (r.get("changed") or {}).get("added") == 3 and any("FrameObj: +1 -0" in line for line in r.get("logs") or []) and snapshot.endswith(".EDB") and "\\" not in snapshot and "/" not in snapshot and (ctx3.get("etabs") or {}).get("frameCount") == frames_before + 1,
          f"{short(r)} in {dt:.1f}s")
    presave_now = len(os.listdir(os.path.join(snap, "presave"))) if os.path.isdir(os.path.join(snap, "presave")) else 0
    presave_logged = any("presave snapshot" in line for line in r.get("logs") or [])
    check("B5a the snapshot file exists in the prerun bucket; a presave copy was taken iff the file on disk was not last written by this bridge (the log says which)",
          bool(snapshot) and os.path.isfile(os.path.join(snap, "prerun", snapshot)) and (presave_now == presave_before + 1) == presave_logged,
          f"presave {presave_before}->{presave_now} logged={presave_logged}; prerun={os.listdir(os.path.join(snap, 'prerun')) if os.path.isdir(os.path.join(snap, 'prerun')) else []}")
    check("B5b the run's log names the snapshot and the forced save", any("snapshot" in line and "saved" in line for line in r.get("logs") or []), short(r.get("logs")))
    save("bridge-add-frame", r)

    lines = audit_lines()
    new = lines[audit_before:]
    started = [i for i, line in enumerate(new) if '"started"' in line and "B5 add frame" in line or ('"started"' in line and "forced save" in line)]
    finished = [i for i, line in enumerate(new) if ('"ok"' in line or '"error"' in line) and "[tier:W]" in line]
    check("B5c audit: a 'started … forced save' line precedes the completed [tier:W] line of the write", bool(started) and bool(finished) and min(started) < min(finished), short(new[-3:]))

    r = execute(s, FIRST_SECTION, label="B6 first section")
    section = r.get("value") if ok(r) else None
    r = execute(s, f"return sapModel.FrameObj.SetSection(\"{added[0] if added else 'F1'}\", \"{section}\");", transaction="manual", label="../x", timeout_s=60, wait=90) if section else {}
    check("B6 second write (manual ≡ auto + log, label ../x): no new presave (the bridge wrote the file last), snapshot name sanitized, changed 0 (Set* is not counted)",
          bool(section) and ok(r) and (r.get("snapshot") or "").endswith("-x.EDB") and len(os.listdir(os.path.join(snap, "presave"))) == presave_now and r.get("changed") == {"added": 0, "modified": 0, "deleted": 0} and any("manual" in line for line in r.get("logs") or []),
          short(r))

    r = execute(s, ADD_FRAME_THEN_THROW, transaction="auto", label="B7 throw after write", args={"dx": 2000}, timeout_s=60, wait=90)
    ctx4 = s.tool("get_etabs_context", {})
    thrown_name = None
    for line in r.get("logs") or []:
        if line.startswith("added "):
            thrown_name = line[len("added "):]
    if thrown_name:
        added.append(thrown_name)
    check("B7 exception after a write: isError, rolledBack false, message says the change persisted and names the snapshot, changed.added 3 (frame + 2 points), frame count +2",
          r.get("isError") and r.get("rolledBack") is False and "persisted" in msg(r) and (r.get("snapshot") or "") in msg(r) and (r.get("changed") or {}).get("added") == 3 and (ctx4.get("etabs") or {}).get("frameCount") == frames_before + 2,
          short(r))

    check("B8 units restored after the writes (context presentUnits unchanged)", (ctx4.get("etabs") or {}).get("presentUnits") == units_before, f"{units_before} → {(ctx4.get('etabs') or {}).get('presentUnits')}")

    r = execute(s, f"return sapModel.FrameObj.Delete(\"{added[0] if added else 'F1'}\", eItemType.Objects);", transaction="auto", label="B9 delete off")
    check("B9 destructive member with the second opt-in off → -32001 (no PREVIEW, no run)", r.get("isError") and "Allow destructive operations" in msg(r) and not r.get("diagnostics"), short(msg(r)))

    r = execute(s, "return sapModel.File.Save(\"\\\\\\\\srv\\\\share\\\\x.EDB\");", transaction="auto", label="B10 unc literal")
    check("B10 UNC path literal → PATH refusal before any opt-in question", r.get("isError") and "PATH" in diag_ids(r) and "UNC" in " ".join(d.get("message", "") for d in r.get("diagnostics") or []), short(r))

    r = execute(s, "var p = args.Str(\"k\"); return sapModel.File.Save(p);", transaction="auto", label="B11 path variable", args={"k": "x"})
    check("B11 path through a variable → PATH refusal (must be a literal or args.Str(\"key\"))", r.get("isError") and "PATH" in diag_ids(r), short(r))

    r = execute(s, "return sapModel.NoSuchMember();", label="B12 compile")
    check("B12 compile error is reported as such", r.get("isError") and "does not compile" in msg(r) and r.get("diagnostics"), short(msg(r)))

    r = execute(s, "int i = 0; while (true) { ct.ThrowIfCancellationRequested(); i++; }", timeout_s=5, label="B13 timeout", wait=40)
    check("B13 cooperative timeout 5 s → timedOut", r.get("isError") and r.get("timedOut") is True, short(msg(r)))
    r = execute(s, "return sapModel.GetModelFilename();", label="B13b after timeout")
    check("B13b the next request runs (the worker is free again)", ok(r), short(r))

    save("bridge-frames", {"added": added, "snapshot_dir": snap, "prerun_before": prerun_before})


def phase_bridgedestructive(s):
    """The wrapper ticked 'Allow destructive operations': the frames added in phase bridge are deleted (cleanup) under D with a snapshot and a [destructive] audit line."""
    state = {}
    try:
        with open(os.path.join(CL.out_dir or ".", "bridge-frames.json"), encoding="utf-8") as f:
            state = json.load(f)
    except OSError:
        pass
    added = state.get("added") or []
    if not added:
        skip("D1 delete the frames added under W", "phase bridge added no frames")
        return

    ctx = s.tool("get_etabs_context", {})
    e = ctx.get("etabs") or {}
    check("D0 context reports destructiveOperationsEnabled true", ok(ctx) and e.get("destructiveOperationsEnabled") is True, short(e))
    frames_before = e.get("frameCount") or 0
    audit_before = len(audit_lines())

    code = "int deleted = 0; " + " ".join(f"if (sapModel.FrameObj.Delete(\"{n}\", eItemType.Objects) == 0) deleted++;" for n in added) + " return deleted;"
    r = execute(s, code, transaction="auto", label="D1 delete frames", timeout_s=60, wait=90)
    ctx2 = s.tool("get_etabs_context", {})
    check("D1 destructive script with the opt-in on runs: value = frames deleted, changed.deleted = frames + their orphaned points (log 'FrameObj: +0 -N'), snapshot taken, frame count back",
          ok(r) and r.get("value") == len(added) and (r.get("changed") or {}).get("deleted") >= len(added) and any(f"FrameObj: +0 -{len(added)}" in line for line in r.get("logs") or []) and (r.get("snapshot") or "").endswith(".EDB") and (ctx2.get("etabs") or {}).get("frameCount") == frames_before - len(added),
          short(r))
    new = audit_lines()[audit_before:]
    check("D2 audit: 'started' with [destructive] precedes the completed [destructive] line", any('"started"' in line and "[destructive]" in line for line in new) and any(("\"ok\"" in line) and "[destructive]" in line for line in new), short(new[-2:]))
    audit_mid = len(audit_lines())

    r = execute(s, "return sapModel.File.Save(\"C:\\\\Windows\\\\Temp\\\\x.EDB\");", transaction="auto", label="D3 path elsewhere")
    check("D3 with D on, a literal path outside the model folder is still refused at run time (PATH)", r.get("isError") and "PATH" in diag_ids(r) and "under the model folder" in " ".join(d.get("message", "") for d in r.get("diagnostics") or []), short(r))

    r = execute(s, "return sapModel.File.Save(args.Str(\"out\"));", transaction="auto", label="D4 args path", args={"out": "C:\\Windows\\Temp\\y.EDB"})
    check("D4 with D on, an args path outside the model folder is refused at run time (PATH names args.out)", r.get("isError") and "PATH" in diag_ids(r) and any("args.out" in d.get("message", "") for d in r.get("diagnostics") or []), short(r))
    check("D4a a run-time path refusal writes no 'started' audit line (nothing was saved)", not any('"started"' in line for line in audit_lines()[audit_mid:]), short(audit_lines()[audit_mid:]))

    r = execute(s, "return sapModel.FrameObj.Delete(\"HPETABS-NO-SUCH-FRAME\", eItemType.Objects);", transaction="auto", dry_run=True, label="D5 dryRun D")
    check("D5 dryRun on a destructive script is a static preview even with the opt-in on", r.get("isError") and "PREVIEW" in diag_ids(r) and r.get("rolledBack") is True, short(r))


def phase_closed(s):
    t0 = time.time()
    r = execute(s, "return sapModel.GetModelFilename();", label="E19 closed")
    dt = time.time() - t0
    ctx = s.tool("get_etabs_context", {})
    check("E19c ETABS closed: execute refused at once with 'click Attach' (liveness dropped the attachment), context isAttached false", r.get("isError") and "click Attach" in msg(r) and dt < 3.0 and (ctx.get("etabs") or {}).get("isAttached") is False, f"{short(msg(r))} in {dt:.1f}s; ctx {short(ctx)[:120]}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("exe")
    ap.add_argument("--registry", required=True)
    ap.add_argument("--phase", choices=["disabled", "detached", "spike", "nomodel", "modal", "closed", "bridge", "bridgedestructive"], default="detached")
    ap.add_argument("--out", default=None)
    a = ap.parse_args()
    CL.out_dir = a.out

    env = dict(os.environ)
    env["HPETABS_MCP_Registry__LibraryPath"] = os.path.join(a.registry, "tools-library")
    env["HPETABS_MCP_Registry__DbPath"] = os.path.join(a.registry, "registry.db")
    os.makedirs(env["HPETABS_MCP_Registry__LibraryPath"], exist_ok=True)

    started = time.time()
    s = Server(a.exe, env=env, name="etabs-verify")
    try:
        s.initialize()
        time.sleep(0.5)
        {"disabled": phase_disabled, "detached": phase_detached, "spike": phase_spike, "nomodel": phase_nomodel, "modal": phase_modal, "closed": phase_closed,
         "bridge": phase_bridge, "bridgedestructive": phase_bridgedestructive}[a.phase](s)
    finally:
        s.close()

    sys.exit(CL.finish(f"summary-{a.phase}", phase=a.phase, seconds=round(time.time() - started, 1)))


if __name__ == "__main__":
    main()
