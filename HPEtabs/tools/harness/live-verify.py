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
Prints PASS/FAIL lines and a JSON summary; exit 1 on any failure.
"""
import argparse, os, sys, time

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

    r = execute(s, "int ret = sapModel.FrameObj.SetSection(\"F1\", \"C40x40\"); return ret;", transaction="auto", label="write preview")
    check("P1 writing script → static preview: isError, PREVIEW diagnostic names FrameObj.SetSection (W), nothing ran", r.get("isError") and "PREVIEW" in diag_ids(r) and any("FrameObj.SetSection" in d.get("message", "") for d in r["diagnostics"]) and r.get("rolledBack") is True, short(r))

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
    ap.add_argument("--phase", choices=["disabled", "detached", "spike", "nomodel", "modal", "closed"], default="detached")
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
        {"disabled": phase_disabled, "detached": phase_detached, "spike": phase_spike, "nomodel": phase_nomodel, "modal": phase_modal, "closed": phase_closed}[a.phase](s)
    finally:
        s.close()

    sys.exit(CL.finish(f"summary-{a.phase}", phase=a.phase, seconds=round(time.time() - started, 1)))


if __name__ == "__main__":
    main()
