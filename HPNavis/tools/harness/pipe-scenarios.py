"""Bridge harness: talks NDJSON JSON-RPC to the Navisworks bridge over its named pipe and runs the scenario
matrix of the plan (read, W1 edits, dryRun, none, errors, guard, heavy gate, timeout, cancel, busy, big results).
No MCP server involved. Prints one line per scenario and a JSON summary at the end; exit code 1 on any failure.

Usage: python pipe-scenarios.py [--pipe hpnavis-mcp-2026] [--only a,b,c] [--audit <dir>]
Default set: ping context read models search counts w1 dryrun samelabel empty current selected manual nonemod compile
             exception guard heavy bigreturn timeout cancel contextbusy
Exclusive (need runner setup): disabled modal clash heavyappend nodoc
"""
import json, os, sys, time, argparse, glob

# the runner captures stdout through a Windows console codepage; scenario names carry arrows and ellipses
for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

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
    ap.add_argument("--audit", default=os.path.join(os.environ.get("APPDATA", ""), "HPNavis", "McpBridge", "audit"))
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

    if "heavyappend" in only:
        nwc = os.environ.get("HPNAVIS_APPEND_FILE", "")
        assert nwc and os.path.exists(nwc), "HPNAVIS_APPEND_FILE must point at an .nwc"
        code = "doc.AppendFile(args.Str(\"path\", \"\")); return doc.Models.Count;"
        before = counts(p)
        r = execute(p, code, dry_run=True, label="append dry", args={"path": nwc}, timeout_s=300)
        v = r.get("result", {})
        check("heavy ON + dryRun: AppendFile refused before running", v.get("isError") is True and "dryRun cannot undo" in (v.get("message") or "") and counts(p)["models"] == before["models"],
              v.get("message"))
        t0 = time.time()
        r = execute(p, code, label="append mep", args={"path": nwc}, timeout_s=300, wait=400)
        v = r.get("result", {})
        after = counts(p)
        check("heavy ON: AppendFile adds a model, rolledBack=false, changed.added=1",
              not v.get("isError") and after["models"] == before["models"] + 1 and v.get("rolledBack") is False and v.get("changed", {}).get("added") == 1,
              f"models {before['models']}->{after['models']} value={v.get('value')} in {time.time() - t0:.1f}s")
        # the two audit lines of the real append: "started" when it was queued, then the outcome — later `counts`
        # reads add lines of their own, so pick the append's lines by source
        lines = [l for l in audit_tail(a.audit, 12) if "AppendFile" in (l.get("source") or "") and not l.get("dryRun")][-2:]
        check("heavy audit: a 'started' line before and a '[heavy]' message after",
              len(lines) == 2 and lines[0].get("outcome") == "started" and (lines[0].get("message") or "").startswith("[heavy]")
              and lines[1].get("outcome") == "ok" and (lines[1].get("message") or "").startswith("[heavy]"),
              " | ".join(f"{l.get('outcome')}:{(l.get('message') or '')[:40]}" for l in lines))
        r = execute(p, "doc.AppendFile(@\"\\\\srv\\models\\x.nwc\"); return 1;", label="append unc", timeout_s=300)
        v = r.get("result", {})
        check("heavy ON: UNC path still refused (HEAVY path policy)", v.get("isError") is True and any(d.get("id") == "HEAVY" and "UNC" in d.get("message", "") for d in v.get("diagnostics", [])),
              " | ".join(d.get("message", "")[:80] for d in v.get("diagnostics", [])))
        rc = p.call("navis.context")
        nav = rc.get("result", {}).get("navis") or {}
        check("context after append: modelCount +1, per-model units, not busy, heavy flag on",
              nav.get("modelCount") == before["models"] + 1 and len(nav.get("models") or []) == nav.get("modelCount") and all(m.get("units") for m in nav.get("models") or [])
              and nav.get("isBusy") is False and nav.get("heavyOperationsEnabled") is True,
              json.dumps({k: nav.get(k) for k in ("modelCount", "isBusy", "heavyOperationsEnabled", "isModified")}) + " models=" + json.dumps(nav.get("models"))[:200])
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

    if want("models"):
        r = execute(p, "return doc.Models.Select(m => new { m.FileName, units = m.Units.ToString(), roots = m.RootItem.Children.Count() }).ToList();", transaction="none", label="models")
        v = r.get("result", {})
        arr = v.get("value") or []
        check("read: doc.Models projection under none", not v.get("isError") and isinstance(arr, list) and len(arr) >= 1 and arr[0].get("fileName", "").endswith(".nwd") and arr[0].get("units"),
              json.dumps(arr)[:200])

    if want("search"):
        code = """
var search = new Search();
search.Selection.SelectAll();
search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName("Item", "Name").DisplayStringContains(args.Str("text", "a")));
return search.FindAll(doc, false);"""
        t0 = time.time()
        r = execute(p, code, transaction="none", label="search", args={"text": "a"})
        v = r.get("result", {})
        arr = v.get("value")
        marker = [x for x in (arr or []) if isinstance(x, dict) and "truncated" in x]
        items = [x for x in (arr or []) if isinstance(x, dict) and x.get("type") == "ModelItem"]
        check("read: Search → ModelItemCollection serialised as ModelItem summaries (capped at 200 + marker)",
              not v.get("isError") and isinstance(arr, list) and len(items) >= 1 and all("displayName" in x for x in items) and len(items) <= 200 and v.get("changed") == {"added": 0, "modified": 0, "deleted": 0},
              f"{len(items)} items, marker={marker[:1]}, first={json.dumps(items[:1])[:160]} in {time.time() - t0:.1f}s")

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

    if want("selected"):
        r = execute(p, "return doc.CurrentSelection.SelectedItems;", transaction="none", label="selected")
        v = r.get("result", {})
        arr = v.get("value")
        check("read: CurrentSelection.SelectedItems after the harness selected one item (auto run above)",
              not v.get("isError") and isinstance(arr, list) and len(arr) == 1 and arr[0].get("type") == "ModelItem" and v.get("rolledBack") is False,
              json.dumps(arr)[:200])

    if want("manual"):
        before = counts(p)
        r = execute(p, W1_EDITS, transaction="manual", label="spike manual", args={"name": "MCP manual"})
        v = r.get("result", {})
        after = counts(p)
        check("manual ≡ auto: commits, logs the note, undo entry carries the label",
              not v.get("isError") and after["sets"] == before["sets"] + 1 and after["undo"] == "MCP: spike manual" and any("behaves like" in l for l in v.get("logs", [])),
              f"undo={after['undo']!r} logs={v.get('logs')}")

    if want("nonemod"):
        before = counts(p)
        r = execute(p, "doc.SelectionSets.AddCopy(new SelectionSet(new ModelItemCollection()) { DisplayName = \"MCP none\" }); return 1;", transaction="none", label="none mod")
        v = r.get("result", {})
        after = counts(p)
        check("none + edit: isError with the 'declared none' message, rolledBack=true, sets unchanged, user's undo top restored",
              v.get("isError") is True and 'declared transaction="none"' in (v.get("message") or "") and v.get("rolledBack") is True and after["sets"] == before["sets"] and after["undo"] == before["undo"],
              f"msg={v.get('message')!r} sets {before['sets']}->{after['sets']} undo {before['undo']!r}->{after['undo']!r}")

    if want("compile"):
        r = execute(p, "return doc.NoSuchMember;", transaction="none", label="compile")
        v = r.get("result", {})
        diags = v.get("diagnostics", [])
        check("compile error: isError with a CS diagnostic, nothing ran", v.get("isError") is True and any(d.get("id", "").startswith("CS") for d in diags) and v.get("durationMs", 1) >= 0,
              " | ".join(f"{d.get('id')} {d.get('message', '')[:60]}" for d in diags))

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

    if want("bigreturn"):
        t0 = time.time()
        r = execute(p, "return doc.Models.RootItems.First().DescendantsAndSelf.ToList();", transaction="none", label="big", timeout_s=60)
        v = r.get("result", {})
        val = v.get("value")
        cut = v.get("truncated") is True or (isinstance(val, list) and any(isinstance(x, dict) and "truncated" in x for x in val))
        check("big result: the whole tree comes back bounded (truncated flag or collection marker), fast",
              not v.get("isError") and cut and time.time() - t0 < 30, f"truncated={v.get('truncated')} type={v.get('valueType')} len={len(val) if isinstance(val, list) else len(str(val))} in {time.time() - t0:.1f}s")

    if want("contextbusy"):
        # a long script is running: context must answer busy at once instead of waiting for the main thread
        _id += 1
        rid = _id
        p.f.write((json.dumps({"jsonrpc": "2.0", "id": rid, "method": "navis.execute", "params": {"code": "while (!ct.IsCancellationRequested) { } return 1;", "transaction": "none", "dryRun": False, "timeoutSeconds": 30, "label": "spin-context"}}) + "\n").encode("utf-8"))
        time.sleep(1.0)
        t0 = time.time()
        rc = p.call("navis.context")
        dt = time.time() - t0
        err = rc.get("error", {})
        cancel = p.call("navis.cancel")
        got = next((n for n in (rc.get("_notifications", []) + cancel.get("_notifications", [])) if n.get("id") == rid), None)
        deadline = time.time() + 30
        while got is None and time.time() < deadline:
            line = p.f.readline()
            if not line:
                break
            obj = json.loads(line.decode("utf-8"))
            if obj.get("id") == rid:
                got = obj
        check("context while a script runs: -32002 within 1 s (no wait on the main thread)", err.get("code") == -32002 and dt < 1.0 and got is not None,
              f"error={json.dumps(err)[:100]} in {dt:.2f}s; spin result={(got or {}).get('result', {}).get('message')!r}")

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


def audit_tail(folder, n):
    files = sorted(glob.glob(os.path.join(folder, "audit-*.log")))
    if not files:
        return []
    with open(files[-1], "r", encoding="utf-8") as f:
        lines = [l for l in f.read().splitlines() if l.strip()]
    out = []
    for l in lines[-n:]:
        try:
            out.append(json.loads(l))
        except ValueError:
            out.append({})
    return out


def finish():
    passed = sum(1 for r in results if r["pass"])
    print(json.dumps({"passed": passed, "total": len(results), "failed": [r["name"] for r in results if not r["pass"]]}))
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
