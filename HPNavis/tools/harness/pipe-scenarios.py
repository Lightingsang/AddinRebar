"""Phase-1 spike harness: talks NDJSON JSON-RPC to the Navisworks bridge over its named pipe and runs the
spike scenarios of the plan (S-03..S-06, S-05b/c, S-11). No MCP server involved (that is phase 3).
Prints one line per scenario and a JSON summary at the end; exit code 1 when any scenario fails.

Usage: python pipe-scenarios.py [--pipe hpnavis-mcp-2026] [--only a,b,c]
Scenario keys: ping context read counts w1 dryrun samelabel empty current exception guard heavy timeout cancel; exclusive: disabled modal clash nodoc
"""
import json, sys, time, argparse

PIPE = r"\\.\pipe\hpnavis-mcp-2026"
_id = 0
results = []


class Pipe:
    def __init__(self, path, retries=20):
        # right after the listener starts the pipe exists but may refuse the first open (EINVAL / pipe busy)
        for attempt in range(retries):
            try:
                self.f = open(path, "r+b", buffering=0)
                return
            except OSError:
                if attempt == retries - 1:
                    raise
                time.sleep(0.5)

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

    def close(self):
        self.f.close()


def execute(p, code, transaction="auto", dry_run=False, timeout_s=30, label="spike", args=None, wait=90.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return p.call("navis.execute", params, timeout=wait)


COUNTS = """
return new { sets = doc.SelectionSets.Value.Count, vps = doc.SavedViewpoints.Value.Count, models = doc.Models.Count,
             selected = doc.CurrentSelection.SelectedItems.Count, undo = doc.NextUndo, modified = doc.IsModified };"""

# W1 edits: a selection set, a saved viewpoint and a permanent colour override on the first root item.
W1_EDITS = """
var set = new SelectionSet(new ModelItemCollection()) { DisplayName = args.Str("name", "MCP spike set") };
doc.SelectionSets.AddCopy(set);
var vp = new SavedViewpoint(doc.CurrentViewpoint.ToViewpoint()) { DisplayName = args.Str("name", "MCP spike vp") };
doc.SavedViewpoints.AddCopy(vp);
var first = doc.Models.RootItems.First();
doc.Models.OverridePermanentColor(new[] { first }, Color.FromByteRGB(255, 0, 0));
log("added set + viewpoint, coloured " + first.DisplayName);
return doc.NextUndo;"""


def counts(p):
    r = execute(p, COUNTS, transaction="none", label="counts")
    assert "result" in r, r
    return r["result"]["value"]


def check(name, cond, detail=""):
    results.append({"name": name, "pass": bool(cond), "detail": detail})
    print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:500])


def main():
    global _id
    ap = argparse.ArgumentParser()
    ap.add_argument("--pipe", default=PIPE)
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    only = set(x for x in a.only.split(",") if x)

    def want(k):
        return not only or k in only

    p = Pipe(a.pipe)

    if "disabled" in only:
        r = execute(p, "return 1;", transaction="none", label="disabled")
        err = r.get("error", {})
        check("S-04a execute while opt-in off -> -32001", err.get("code") == -32001, json.dumps(err))
        p.close(); finish(); return

    if "modal" in only:
        # the runner opened a modal owned by Roamer's main window before calling us
        t0 = time.time()
        r = p.call("navis.context")
        ctx = r.get("result", {}).get("navis") or {}
        r2 = execute(p, "return 1;", transaction="none", label="modal", wait=60)
        err = r2.get("error", {})
        check("S-07 modal open: execute -> -32002 (busy) within the 8 s grace, script never ran", err.get("code") == -32002 and 5 <= time.time() - t0 <= 40,
              f"context.isBusy={ctx.get('isBusy')} error={json.dumps(err)[:200]} in {time.time() - t0:.1f}s")
        p.close(); finish(); return

    if "clash" in only:
        # S-08 (script-driven variant): the runner ticked "Allow heavy operations". Create a hard clash test between the
        # two halves of the root's children and run it; a second execute sent meanwhile must come back -32002 or run
        # only after the clash finished (the main thread is inside our own script, so Idle cannot tick).
        r0 = execute(p, "return doc.GetClash().TestsData.Tests.Count;", transaction="none", label="clash count")
        before_tests = r0.get("result", {}).get("value")
        code = """
var dc = doc.GetClash().TestsData;
var root = doc.Models.RootItems.First();
var kids = root.Children.ToList();
if (kids.Count < 2) kids = root.DescendantsAndSelf.Where(c => c.HasGeometry).Take(400).ToList();
var half = kids.Count / 2;
var test = new ClashTest { DisplayName = args.Str("name", "MCP spike clash"), TestType = ClashTestType.Hard, Tolerance = 0.0 };
test.SelectionA.Selection.CopyFrom(kids.Take(half));
test.SelectionB.Selection.CopyFrom(kids.Skip(half));
dc.TestsAddCopy(test);
var added = dc.Tests.OfType<ClashTest>().Last(t => t.DisplayName == test.DisplayName);
var t0 = DateTime.UtcNow;
dc.TestsRunTest(added);
log("clash ran in " + (DateTime.UtcNow - t0).TotalMilliseconds.ToString("F0") + " ms");
// the ClashTest wrapper held across TestsRunTest is disposed afterwards (ObjectDisposedException on .Children): resolve it again
var ran = dc.Tests.OfType<ClashTest>().Last(t => t.DisplayName == test.DisplayName);
return new { tests = dc.Tests.Count, results = ran.Children.Count, a = half, b = kids.Count - half, status = ran.Status.ToString() };"""
        _id += 1
        rid = _id
        t0 = time.time()
        p.f.write((json.dumps({"jsonrpc": "2.0", "id": rid, "method": "navis.execute", "params": {"code": code, "transaction": "auto", "dryRun": False, "timeoutSeconds": 300, "label": "spike clash", "args": {"name": "MCP spike clash"}}}) + "\n").encode("utf-8"))
        time.sleep(0.5)
        r2 = execute(p, "return doc.Title;", transaction="none", label="during clash", wait=400)
        t_second = time.time() - t0
        got = next((n for n in r2.get("_notifications", []) if n.get("id") == rid), None)
        deadline = time.time() + 400
        while got is None and time.time() < deadline:
            line = p.f.readline()
            if not line:
                break
            obj = json.loads(line.decode("utf-8"))
            if obj.get("id") == rid:
                got = obj
        t_clash = time.time() - t0
        v = (got or {}).get("result", {})
        err2 = r2.get("error", {})
        second = "-32002 busy" if err2.get("code") == -32002 else ("ran after the clash" if "result" in r2 else json.dumps(err2)[:100])
        check("S-08 heavy ON: clash test created + run through the API, results counted, rolledBack never true",
              "result" in (got or {}) and not v.get("isError") and isinstance(v.get("value"), dict) and v.get("rolledBack") is False,
              f"value={v.get('value')} logs={v.get('logs')} clash request answered after {t_clash:.1f}s; second request answered after {t_second:.1f}s -> {second}; tests before={before_tests}")
        rc = p.call("navis.context")
        nav = rc.get("result", {}).get("navis") or {}
        check("S-08 after clash: context not busy, clashTestCount incremented", nav.get("isBusy") is False and nav.get("clashTestCount") == (before_tests or 0) + 1,
              f"isBusy={nav.get('isBusy')} clashTestCount={nav.get('clashTestCount')} heavy={nav.get('heavyOperationsEnabled')}")
        p.close(); finish(); return

    if "nodoc" in only:
        r = p.call("navis.context")
        ctx = r.get("result", {})
        r2 = execute(p, "return 1;", transaction="none", label="nodoc")
        err = r2.get("error", {})
        check("nodoc: IsClear -> context isClear, execute -32003", ctx.get("navis", {}).get("isClear") is True and err.get("code") == -32003, f"navis={ctx.get('navis')} error={err}")
        p.close(); finish(); return

    if want("ping"):
        t0 = time.time()
        r = p.call("navis.ping")
        dt = time.time() - t0
        check("S-03 ping < 1 s without touching the mouse", r.get("result", {}).get("pong") is True and r["result"].get("revitVersion") == "2026" and dt < 1.0, f"{json.dumps(r.get('result'))} in {dt:.2f}s")

    if want("context"):
        t0 = time.time()
        r = p.call("navis.context", {"includeSelection": True})
        dt = time.time() - t0
        ctx = r.get("result", {})
        nav = ctx.get("navis") or {}
        check("S-03 context < 1 s, host=navis, navis block present, no revitVersion/isFamily",
              ctx.get("host") == "navis" and nav.get("documentUnits") and "revitVersion" in ctx and dt < 1.0,
              f"title={ctx.get('docTitle')} units={nav.get('documentUnits')} models={nav.get('modelCount')} busy={nav.get('isBusy')} clash={nav.get('hasClashModule')} in {dt:.2f}s")
        # the wire keeps revitVersion; the server's Shape() strips it for non-Revit hosts (tested in Server.Core.Tests)

    if want("read"):
        r = execute(p, "return doc.Title;", transaction="none", label="read")
        v = r.get("result", {})
        check("S-04 read doc.Title under none", "result" in r and not v.get("isError") and isinstance(v.get("value"), str) and v.get("changed") == {"added": 0, "modified": 0, "deleted": 0} and v.get("rolledBack") is False,
              json.dumps({k: v.get(k) for k in ("value", "changed", "rolledBack", "durationMs", "logs")}))

    if want("counts"):
        c = counts(p)
        check("counts readable", isinstance(c, dict) and "sets" in c, json.dumps(c))

    if want("w1"):
        before = counts(p)
        r = execute(p, W1_EDITS, label="spike w1", args={"name": "MCP spike"})
        v = r.get("result", {})
        after = counts(p)
        check("S-05 auto commits W1 edits: sets+1, vps+1, undo entry 'MCP: spike w1'",
              not v.get("isError") and after["sets"] == before["sets"] + 1 and after["vps"] == before["vps"] + 1 and after["undo"] == "MCP: spike w1"
              and v.get("changed", {}).get("added") == 2 and v.get("rolledBack") is False,
              f"before={before} after={after} result={json.dumps({k: v.get(k) for k in ('isError','message','changed','rolledBack','logs')})}")

    if want("dryrun"):
        before = counts(p)
        r = execute(p, W1_EDITS, dry_run=True, label="spike dry", args={"name": "MCP dry"})
        v = r.get("result", {})
        after = counts(p)
        check("S-05 dryRun: commit then Rollback() of the bridge's own entry, counts unchanged, undo top unchanged",
              not v.get("isError") and v.get("rolledBack") is True and after["sets"] == before["sets"] and after["vps"] == before["vps"] and after["undo"] == before["undo"],
              f"before={before} after={after} result={json.dumps({k: v.get(k) for k in ('isError','message','changed','rolledBack','logs')})}")

    if want("samelabel"):
        # the registry labels every run of a tool with the tool's name: a dry run right after a real run of the same
        # tool must still undo its own edits (the bridge suffixes the label so its entry differs from the top)
        r1 = execute(p, W1_EDITS, label="spike same", args={"name": "MCP same 1"})
        before = counts(p)
        r2 = execute(p, W1_EDITS, dry_run=True, label="spike same", args={"name": "MCP same 2"})
        v = r2.get("result", {})
        after = counts(p)
        check("H1 dryRun after a real run with the same label: rolledBack=true, counts unchanged, undo top still the real run",
              not r1.get("result", {}).get("isError") and not v.get("isError") and v.get("rolledBack") is True
              and after["sets"] == before["sets"] and after["vps"] == before["vps"] and after["undo"] == before["undo"] == "MCP: spike same",
              f"before={before} after={after} result={json.dumps({k: v.get(k) for k in ('isError','message','rolledBack','logs')})}")

    if want("empty"):
        before = counts(p)
        r = execute(p, "return 42;", dry_run=True, label="spike empty")
        v = r.get("result", {})
        after = counts(p)
        check("S-05b auto+dryRun with no edit: no Rollback(), user's undo top untouched, rolledBack=false",
              not v.get("isError") and v.get("rolledBack") is False and after["undo"] == before["undo"] and after["sets"] == before["sets"],
              f"before.undo={before['undo']!r} after.undo={after['undo']!r} result={json.dumps({k: v.get(k) for k in ('value','rolledBack','logs')})}")

    if want("current"):
        before = counts(p)
        r = execute(p, "doc.CurrentSelection.Clear(); doc.CurrentSelection.Add(doc.Models.RootItems.First()); return doc.NextUndo;", label="spike current")
        v = r.get("result", {})
        after = counts(p)
        check("S-05c CurrentSelection.Add: record whether it creates an undo entry (informational)", "result" in r,
              f"undo before={before['undo']!r} after={after['undo']!r} selected {before['selected']}->{after['selected']} value={v.get('value')!r} changed={v.get('changed')}")

    if want("exception"):
        before = counts(p)
        code = "doc.SelectionSets.AddCopy(new SelectionSet(new ModelItemCollection()) { DisplayName = \"MCP boom\" }); throw new InvalidOperationException(\"boom\");"
        r = execute(p, code, label="spike boom")
        v = r.get("result", {})
        after = counts(p)
        check("S-06 exception after an edit: isError, rolledBack=true, sets unchanged",
              v.get("isError") is True and "boom" in (v.get("message") or "") and v.get("rolledBack") is True and after["sets"] == before["sets"],
              f"before={before['sets']} after={after['sets']} msg={v.get('message')!r} rolledBack={v.get('rolledBack')}")

    if want("guard"):
        cases = [
            ("var t = doc.BeginTransaction(\"x\"); return 1;", "BeginTransaction"),
            ("using (var t = new Transaction(doc, \"x\")) { } return 1;", "Transaction is not allowed"),
            ("doc.Undo(); return 1;", ".Undo"),
            ("var c = new NavisworksCommand(\"select 1\", doc.Database.ToNavisworksConnection()); return 1;", "NavisworksCommand"),
            ("var e = Expression.Call(Expression.Constant(doc), \"SaveFile\", null, Expression.Constant(\"a\")); return 1;", "Expression"),
            ("System.Windows.Forms.MessageBox.Show(\"hi\"); return 1;", "System.Windows.Forms"),
            ("var a = new Autodesk.Navisworks.Api.Automation.NavisworksApplication(); return 1;", "Autodesk.Navisworks.Api.Automation"),
        ]
        for code, frag in cases:
            r = execute(p, code, transaction="none", label="guard")
            v = r.get("result", {})
            msgs = " | ".join(d.get("message", "") for d in v.get("diagnostics", []))
            check(f"S-11 guard refuses {frag}", v.get("isError") is True and any(d.get("id") == "GUARD" for d in v.get("diagnostics", [])) and frag in msgs, msgs[:200])

    if want("heavy"):
        for code, frag in [
            ("doc.AppendFile(@\"C:\\models\\x.nwd\"); return 1;", "AppendFile"),
            ("doc.SaveFile(@\"\\\\server\\share\\x.nwd\"); return 1;", "SaveFile"),
            ("doc.GetClash().TestsData.TestsRunAllTests(); return 1;", "TestsRunAllTests"),
        ]:
            r = execute(p, code, label="heavy")
            v = r.get("result", {})
            diags = v.get("diagnostics", [])
            check(f"S-11 heavy OFF refuses {frag} with HEAVY diagnostic naming the checkbox",
                  v.get("isError") is True and any(d.get("id") == "HEAVY" and "Allow heavy operations" in d.get("message", "") for d in diags),
                  " | ".join(d.get("message", "")[:120] for d in diags))

    if want("timeout"):
        t0 = time.time()
        r = execute(p, "while (!ct.IsCancellationRequested) { } return 1;", transaction="none", timeout_s=5, label="spin", wait=60)
        v = r.get("result", {})
        check("timeout 5 s cooperative: isError, timedOut, rolledBack=false (nothing to undo)", v.get("isError") and v.get("timedOut") is True and 4 <= time.time() - t0 <= 20,
              f"{v.get('message')!r} rolledBack={v.get('rolledBack')} in {time.time() - t0:.1f}s")

    if want("cancel"):
        # start a spin, then cancel from a second pipe request
        _id += 1
        rid = _id
        p.f.write((json.dumps({"jsonrpc": "2.0", "id": rid, "method": "navis.execute", "params": {"code": "while (!ct.IsCancellationRequested) { } return 1;", "transaction": "none", "dryRun": False, "timeoutSeconds": 30, "label": "spin-cancel"}}) + "\n").encode("utf-8"))
        time.sleep(1.5)
        rc = p.call("navis.cancel")
        # the cancelled run's response often lands before the cancel's own response: call() stashed it in _notifications
        got = next((n for n in rc.get("_notifications", []) if n.get("id") == rid), None)
        deadline = time.time() + 30
        while got is None and time.time() < deadline:
            line = p.f.readline()
            if not line:
                break
            obj = json.loads(line.decode("utf-8"))
            if obj.get("id") == rid:
                got = obj
        v = (got or {}).get("result", {})
        check("cancel_execution stops a spinning script", rc.get("result", {}).get("cancelled") is True and v.get("isError") is True and "cancel" in (v.get("message") or "").lower(),
              f"cancel={rc.get('result')} result={v.get('message')!r}")

    p.close()
    finish()


def finish():
    passed = sum(1 for r in results if r["pass"])
    print(json.dumps({"passed": passed, "total": len(results), "failed": [r["name"] for r in results if not r["pass"]]}))
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
