"""Live verification of the Navisworks MCP server against a running Navisworks Manage 2026, over stdio:
  E  execute matrix (read, W1 commit, dryRun, same-label dryRun, empty dryRun, exception, none+modify, manual, guard,
     heavy off, compile error, cancel racing, timeout, context while running, audit)
  S  every seed at least once (run_tool / test_tool on the open model)
  R  MISS → memory: ad-hoc run → get_run → toolify_run → propose → test → publish → CLI approve → tools/list_changed →
     call by name; fragile tool → 5 failing runs → quarantined → restore → fix (newVersion) → approve → back;
     argument errors never count; a proposal with a heavy call is refused
  C  context, resources, prompts, inspect_type
The PowerShell wrapper (run-live-verify.ps1) starts Roamer, ticks the opt-ins and calls this once per phase on one
isolated registry root: --phase disabled (before the execution opt-in), main (E+S+R+C), heavy (after "Allow heavy
operations"), modal (a dialog is open) + aftermodal (closed again), nodoc (a Roamer without a model). Prints PASS/FAIL lines and a JSON summary; exit 1 on any failure.
"""
import argparse, glob, importlib.util, json, os, subprocess, sys, time

for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mcp_session", os.path.join(HERE, "..", "..", "..", "McpShared", "tools", "mcp-session.py"))
mcp_session = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mcp_session)
Server = mcp_session.Server

results = []
OUT = None
EXE = None


def check(name, cond, detail=""):
    results.append({"name": name, "pass": bool(cond), "detail": str(detail)[:600]})
    print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:500], flush=True)


def skip(name, reason):
    results.append({"name": name, "pass": True, "skipped": True, "detail": reason})
    print(f"SKIP {name} {reason}", flush=True)


def short(o, n=260):
    t = json.dumps(o, ensure_ascii=False, default=str)
    return t if len(t) <= n else t[:n] + "…"


def save(name, obj):
    if OUT:
        with open(os.path.join(OUT, name + ".json"), "w", encoding="utf-8") as f:
            json.dump(obj, f, indent=2, ensure_ascii=False, default=str)


def ok(r):
    return not r.get("isError") and not r.get("rpcError")


# ---- scripts ---------------------------------------------------------------------------------------------------------
COUNTS = """
return new { sets = doc.SelectionSets.Value.Count, vps = doc.SavedViewpoints.Value.Count, models = doc.Models.Count,
             selected = doc.CurrentSelection.SelectedItems.Count, undo = doc.NextUndo, modified = doc.IsModified };"""

W1_EDITS = """
var set = new SelectionSet(new ModelItemCollection()) { DisplayName = args.Str("name", "MCP verify set") };
doc.SelectionSets.AddCopy(set);
var vp = new SavedViewpoint(doc.CurrentViewpoint.ToViewpoint()) { DisplayName = args.Str("name", "MCP verify vp") };
doc.SavedViewpoints.AddCopy(vp);
var first = doc.Models.RootItems.First();
doc.Models.OverridePermanentColor(new[] { first }, Color.FromByteRGB(255, 0, 0));
log("added set + viewpoint, coloured " + first.DisplayName);
return doc.NextUndo;"""

COUNT_BY_SOURCE_ADHOC = """
var counts = new Dictionary<string, int>();
foreach (var item in doc.Models.RootItems.SelectMany(r => r.DescendantsAndSelf).Take(50000))
{
    var p = item.PropertyCategories.FindPropertyByDisplayName("Item", "Source File");
    var key = p == null ? "(none)" : (p.Value.IsDisplayString ? p.Value.ToDisplayString() : p.Value.ToString());
    counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
}
return counts.OrderByDescending(kv => kv.Value).Select(kv => new { source = kv.Key, count = kv.Value }).ToList();"""

COUNT_BY_SOURCE_TOOL = """
int maxItems = Math.Max(1, args.Int("maxItems", 50000));
var counts = new Dictionary<string, int>();
foreach (var item in doc.Models.RootItems.SelectMany(r => r.DescendantsAndSelf).Take(maxItems))
{
    var p = item.PropertyCategories.FindPropertyByDisplayName("Item", "Source File");
    var key = p == null ? "(none)" : (p.Value.IsDisplayString ? p.Value.ToDisplayString() : p.Value.ToString());
    counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
}
return counts.OrderByDescending(kv => kv.Value).Select(kv => new { source = kv.Key, count = kv.Value }).ToList();"""

SET_COUNT_FRAGILE = """
string name = args.Str("setName", "");
var set = doc.SelectionSets.Value.OfType<SelectionSet>().First(s => s.DisplayName == name); // throws InvalidOperationException when missing
return new { name, count = set.GetSelectedItems(doc).Count };"""

SET_COUNT_TOOL = """
string name = args.Require("setName");
var set = doc.SelectionSets.Value.OfType<SelectionSet>().FirstOrDefault(s => s.DisplayName == name);
if (set == null) throw new ArgumentException($"no selection set named '{name}'");
return new { name, count = set.GetSelectedItems(doc).Count };"""

HEAVY_PROPOSAL = """
var dc = doc.GetClash().TestsData;
dc.TestsRunAllTests();
return dc.Tests.Count;"""


def schema(props, required):
    return {"type": "object", "properties": props, "required": required, "additionalProperties": False}


# ---- helpers -----------------------------------------------------------------------------------------------------------
def execute(s, code, transaction="auto", dry_run=False, timeout_s=30, label="verify", args=None, wait=120.0):
    params = {"code": code, "transaction": transaction, "dryRun": dry_run, "timeoutSeconds": timeout_s, "label": label}
    if args is not None:
        params["args"] = args
    return s.tool("execute_navis_code", params, timeout=wait)


def counts(s):
    r = execute(s, COUNTS, transaction="none", label="counts")
    assert ok(r), r
    return r["value"]


def cli(*args):
    p = subprocess.run([EXE, "registry", *args], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    return p.returncode, (p.stdout + p.stderr)


def wait_for_tool(s, name, present=True, seconds=10.0):
    t0 = time.time()
    while time.time() - t0 < seconds:
        if (name in s.tools()) == present:
            return round(time.time() - t0, 2)
        time.sleep(0.25)
    return None


def audit_tail(n=3):
    folder = os.path.join(os.environ.get("APPDATA", ""), "HPNavis", "McpBridge", "audit")
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


# ---- E: execute matrix ------------------------------------------------------------------------------------------------------
def scenario_e(s):
    r = execute(s, "return doc.Title;", transaction="none", label="read")
    check("E read under none", ok(r) and isinstance(r.get("value"), str) and r.get("changed") == {"added": 0, "modified": 0, "deleted": 0} and r.get("rolledBack") is False, short(r))

    before = counts(s)
    r = execute(s, W1_EDITS, label="verify w1", args={"name": "MCP verify"})
    after = counts(s)
    check("E auto commits W1 edits: sets+1, vps+1, undo entry 'MCP: verify w1'", ok(r) and after["sets"] == before["sets"] + 1 and after["vps"] == before["vps"] + 1 and after["undo"] == "MCP: verify w1" and r.get("changed", {}).get("added") == 2, f"before={before} after={after}")

    before = counts(s)
    r = execute(s, W1_EDITS, dry_run=True, label="verify dry", args={"name": "MCP dry"})
    after = counts(s)
    check("E dryRun: changed reported, rolledBack, counts + undo top unchanged", ok(r) and r.get("rolledBack") is True and r.get("changed", {}).get("added") == 2 and after["sets"] == before["sets"] and after["undo"] == before["undo"], f"before={before} after={after}")

    execute(s, W1_EDITS, label="verify same", args={"name": "MCP same 1"})
    before = counts(s)
    r = execute(s, W1_EDITS, dry_run=True, label="verify same", args={"name": "MCP same 2"})
    after = counts(s)
    check("E dryRun after a real run with the same label still undoes its own edits", ok(r) and r.get("rolledBack") is True and after["sets"] == before["sets"] and after["undo"] == before["undo"] == "MCP: verify same", f"undo={after['undo']!r}")

    before = counts(s)
    r = execute(s, "return 42;", dry_run=True, label="verify empty")
    after = counts(s)
    check("E empty auto+dryRun: rolledBack false, user's undo top untouched", ok(r) and r.get("rolledBack") is False and after["undo"] == before["undo"] and any("nothing to roll back" in l for l in r.get("logs", [])), short(r.get("logs")))

    before = counts(s)
    r = execute(s, "doc.SelectionSets.AddCopy(new SelectionSet(new ModelItemCollection()) { DisplayName = \"MCP boom\" }); throw new InvalidOperationException(\"boom\");", label="verify boom")
    after = counts(s)
    check("E exception after an edit: isError, rolledBack, sets unchanged", r.get("isError") and "boom" in (r.get("message") or "") and r.get("rolledBack") is True and after["sets"] == before["sets"], short(r.get("message")))

    before = counts(s)
    r = execute(s, "doc.SelectionSets.AddCopy(new SelectionSet(new ModelItemCollection()) { DisplayName = \"MCP none\" }); return 1;", transaction="none", label="verify none")
    after = counts(s)
    check("E none + modify: error 'declared none', rolledBack, sets unchanged", r.get("isError") and 'declared transaction="none"' in (r.get("message") or "") and r.get("rolledBack") is True and after["sets"] == before["sets"], short(r.get("message")))

    r = execute(s, W1_EDITS, transaction="manual", label="verify manual", args={"name": "MCP manual"})
    check("E manual behaves like auto and says so", ok(r) and any("behaves like" in l for l in r.get("logs", [])) and counts(s)["undo"] == "MCP: verify manual", short(r.get("logs")))

    guard_cases = [("System.Windows.Forms.MessageBox.Show(\"hi\"); return 1;", "MessageBox"), ("doc.Undo(); return 1;", ".Undo"),
                   ("var t = doc.BeginTransaction(\"x\"); return 1;", "BeginTransaction"),
                   ("var e = System.Linq.Expressions.Expression.Constant(1); return 1;", "Expression"),
                   ("var c = new NavisworksCommand(\"select 1\", doc.Database.ToNavisworksConnection()); return 1;", "NavisworksCommand")]
    refused = 0
    for code, frag in guard_cases:
        r = execute(s, code, transaction="none", label="guard")
        if r.get("isError") and any(d.get("id") == "GUARD" for d in r.get("diagnostics") or []):
            refused += 1
    check("E guard refuses MessageBox / Undo / BeginTransaction / Expression / NavisworksCommand", refused == len(guard_cases), f"{refused}/{len(guard_cases)}")

    r = execute(s, "doc.AppendFile(@\"C:\\models\\x.nwc\"); return 1;", label="heavy off")
    check("E heavy off: AppendFile → HEAVY diagnostic naming the checkbox", r.get("isError") and any(d.get("id") == "HEAVY" and "Allow heavy operations" in d.get("message", "") for d in r.get("diagnostics") or []), short(r.get("diagnostics")))

    r = execute(s, "return doc.NoSuchMember;", transaction="none", label="compile")
    check("E compile error → isError + CS diagnostic", r.get("isError") and any(d.get("id", "").startswith("CS") for d in r.get("diagnostics") or []), short(r.get("diagnostics")))

    mid = s.send("tools/call", {"name": "execute_navis_code", "arguments": {"code": "while (!ct.IsCancellationRequested) { } return 1;", "transaction": "none", "timeoutSeconds": 30, "label": "spin cancel"}})
    time.sleep(1.5)
    cancelled = s.tool("cancel_execution", {})
    spin = mcp_session.parse_tool_result(s.wait(mid, 60))
    check("E cancel_execution stops a spinning script", cancelled.get("cancelled") is True and spin.get("isError") and "cancel" in (spin.get("message") or "").lower(), f"cancel={short(cancelled)} run={short(spin.get('message'))}")

    t0 = time.time()
    r = execute(s, "while (!ct.IsCancellationRequested) { } return 1;", transaction="none", timeout_s=5, label="spin timeout", wait=60)
    check("E timeout 5 s cooperative", r.get("isError") and r.get("timedOut") is True and 4 <= time.time() - t0 <= 20, f"{short(r.get('message'))} in {time.time() - t0:.1f}s")

    mid = s.send("tools/call", {"name": "execute_navis_code", "arguments": {"code": "while (!ct.IsCancellationRequested) { } return 1;", "transaction": "none", "timeoutSeconds": 30, "label": "spin context"}})
    time.sleep(1.0)
    t0 = time.time()
    ctx = s.tool("get_navis_context", {})
    dt = time.time() - t0
    s.tool("cancel_execution", {})
    s.wait(mid, 60)
    check("E context while a script runs → busy error within 1 s", ctx.get("isError") and dt < 1.5 and ("busy" in (ctx.get("message") or "").lower() or "running" in (ctx.get("message") or "").lower()), f"{short(ctx.get('message'))} in {dt:.2f}s")

    r = execute(s, "return doc.Models.Count;", transaction="none", label="audit probe")
    last = (audit_tail(1) or [{}])[0]
    check("E audit: last line is this run (source, label-less none, outcome ok)", last.get("source") == "return doc.Models.Count;" and last.get("outcome") == "ok" and last.get("transaction") == "none", short(last))


# ---- S: seeds -----------------------------------------------------------------------------------------------------------------
SEEDS = ["get_model_info", "get_selected_item_properties", "find_items_by_property", "list_selection_sets", "list_viewpoints", "get_clash_results",
         "get_timeliner_tasks", "summarize_by_category", "create_selection_set_from_search", "create_viewpoint", "override_color_by_search", "create_and_run_clash_test"]


def scenario_s(s):
    tools = s.tools()
    check("S tools/list = 24 (12 core/registry + 12 seeds)", len(tools) == 24 and all(n in tools for n in SEEDS), f"{len(tools)} missing={[n for n in SEEDS if n not in tools]}")

    r = s.tool("run_tool", {"name": "get_model_info", "args": {"includeRootItems": True}})
    v = r.get("value") or {}
    check("S get_model_info", ok(r) and v.get("documentUnits") and v.get("modelCount", 0) >= 1, f"units={v.get('documentUnits')} models={v.get('modelCount')}")

    r = s.tool("run_tool", {"name": "summarize_by_category", "args": {"groupBy": "Item.Type", "maxGroups": 20}})
    v = r.get("value") or {}
    types = [g["key"] for g in (v.get("groups") or []) if g.get("key")]
    check("S summarize_by_category Item.Type", ok(r) and len(types) >= 1 and v.get("itemsVisited", 0) > 0, f"types={types[:5]}")
    type_a, type_b = (types[0] if types else "Group"), (types[1] if len(types) > 1 else (types[0] if types else "Group"))

    r = s.tool("run_tool", {"name": "find_items_by_property", "args": {"category": "Item", "property": "Type", "value": type_a, "maxResults": 5}})
    v = r.get("value") or {}
    check("S find_items_by_property", ok(r) and v.get("total", 0) >= 1 and (v.get("items") or []), f"total={v.get('total')} shown={v.get('shown')}")

    execute(s, "doc.CurrentSelection.Clear(); doc.CurrentSelection.Add(doc.Models.RootItems.First().Children.First()); return 1;", label="select one")
    r = s.tool("run_tool", {"name": "get_selected_item_properties", "args": {"maxItems": 2}})
    v = r.get("value") or {}
    check("S get_selected_item_properties", ok(r) and v.get("count", 0) >= 1 and ((v.get("items") or [{}])[0].get("categories") or []), f"count={v.get('count')}")

    r = s.tool("run_tool", {"name": "list_selection_sets", "args": {"includeCounts": True}})
    check("S list_selection_sets", ok(r) and isinstance(r.get("value"), list), f"{len(r.get('value') or [])} entries")

    r = s.tool("run_tool", {"name": "list_viewpoints", "args": {"includeComments": True}})
    rows = r.get("value") if isinstance(r.get("value"), list) else []
    check("S list_viewpoints", ok(r) and isinstance(r.get("value"), list) and all("positionMm" in x for x in rows), f"{len(rows)} viewpoints")
    viewpoints_before = len(rows)

    r = s.tool("run_tool", {"name": "get_clash_results", "args": {"maxResults": 3}})
    v = r.get("value") or {}
    check("S get_clash_results", ok(r) and isinstance(v.get("tests"), list) and isinstance(v.get("results"), list) and v.get("testCount") == len(v.get("tests") or []), f"tests={v.get('testCount')}")

    execute(s, "var t = new TimelinerTask { DisplayName = args.Str(\"name\", \"MCP task\"), PlannedStartDate = DateTime.Today, PlannedEndDate = DateTime.Today.AddDays(5) }; doc.GetTimeliner().TaskAddCopy(t); return (int)doc.GetTimeliner().TaskTotalTasks();", label="add task", args={"name": "MCP verify task"})
    r = s.tool("run_tool", {"name": "get_timeliner_tasks", "args": {"includeSelection": True}})
    v = r.get("value") or {}
    check("S get_timeliner_tasks (after adding one)", ok(r) and v.get("total", 0) >= 1 and any(t.get("name") == "MCP verify task" for t in v.get("tasks") or []), f"total={v.get('total')}")

    t = s.tool("test_tool", {"name": "create_selection_set_from_search", "cases": [{"title": "live", "args": {"name": "MCP verify search set", "category": "Item", "property": "Type", "value": type_a}}]})
    check("S test_tool create_selection_set_from_search (dry run)", ok(t) and t.get("passed") == 1 and t.get("failed") == 0, short(t))
    t = s.tool("test_tool", {"name": "override_color_by_search", "cases": [{"title": "live", "args": {"category": "Item", "property": "Type", "value": type_a, "r": 0, "g": 128, "b": 255}}]})
    check("S test_tool override_color_by_search (dry run)", ok(t) and t.get("passed") == 1 and t.get("failed") == 0, short(t))
    r = s.tool("run_tool", {"name": "create_viewpoint", "args": {"name": "MCP verify viewpoint", "comment": "live verify", "author": "harness"}})
    rows = s.tool("run_tool", {"name": "list_viewpoints", "args": {}}).get("value") or []
    check("S run_tool create_viewpoint (real) → one more viewpoint", ok(r) and (r.get("value") or {}).get("guid") and len(rows) == viewpoints_before + 1, f"{viewpoints_before}->{len(rows)}")

    t = s.tool("test_tool", {"name": "create_and_run_clash_test", "cases": [{"title": "live", "args": {"name": "MCP dry clash", "a": {"category": "Item", "property": "Type", "value": type_a}, "b": {"category": "Item", "property": "Type", "value": type_b}}}]})
    text = json.dumps(t).lower()
    check("S test_tool create_and_run_clash_test refused (heavy cannot be dry-run)", (t.get("isError") or t.get("failed", 0) >= 1 or t.get("passed", 1) == 0) and "heavy" in text, short(t))
    r = s.tool("run_tool", {"name": "create_and_run_clash_test", "args": {"name": "MCP heavy off", "a": {"category": "Item", "property": "Type", "value": type_a}, "b": {"category": "Item", "property": "Type", "value": type_b}}})
    check("S run_tool create_and_run_clash_test with heavy OFF → HEAVY", r.get("isError") and any(d.get("id") == "HEAVY" for d in r.get("diagnostics") or []), short(r.get("diagnostics")))
    save("types", {"typeA": type_a, "typeB": type_b})


# ---- R: registry loop ---------------------------------------------------------------------------------------------------------
def scenario_r(s):
    name = "count_items_by_source_file"
    miss = s.tool("search_tools", {"query": "count items by source file", "limit": 5})
    check("R search misses", not any(t.get("name") == name for t in miss.get("tools", [])), [t.get("name") for t in miss.get("tools", [])])

    real = execute(s, COUNT_BY_SOURCE_ADHOC, transaction="none", label="count by source")
    check("R ad-hoc run + propose hint", ok(real) and isinstance(real.get("value"), list) and real.get("runId") and "propose_tool" in (real.get("hint") or ""), f"runId={real.get('runId')} groups={len(real.get('value') or [])}")
    run_id = real.get("runId")

    run = s.tool("get_run", {"runId": run_id, "analyze": True})
    check("R get_run returns the code and analysis", run.get("codeAvailable") and "analysis" in run, short(run.get("analysis")))
    prompt = s.prompt("toolify_run", {"runId": str(run_id)})
    ptext = json.dumps(prompt, ensure_ascii=False)
    check("R toolify_run prompt mentions the run and Navisworks", str(run_id) in ptext and "Navisworks" in ptext and "propose_tool" in ptext, f"{len(ptext)} chars")

    proposed = s.tool("propose_tool", {
        "name": name, "title": "Count items by source file",
        "description": "Counts model items grouped by their Item.Source File property (walk bounded by maxItems). Read-only.",
        "category": "report", "tags": ["report", "count"],
        "inputSchema": schema({"maxItems": {"type": "integer", "default": 50000, "description": "Items visited before the walk stops"}}, []),
        "code": COUNT_BY_SOURCE_TOOL,
        "examples": [{"title": "default", "args": {}}, {"title": "first 1000", "args": {"maxItems": 1000}}],
        "transaction": "none", "timeoutSeconds": 60, "sourceRunId": run_id})
    tool_json = json.load(open(os.path.join(proposed.get("folder") or "", "tool.json"), encoding="utf-8")) if proposed.get("folder") else {}
    check("R propose_tool accepted; tool.json stamped host=navis, category normalised", proposed.get("accepted") and proposed.get("status") == "draft" and tool_json.get("host") == "navis" and tool_json.get("category") == "Report" and tool_json.get("hostVersions") == ["2026"], short(proposed))
    save("r-propose", proposed)

    tested = s.tool("test_tool", {"name": name})
    check("R test_tool 2/2", tested.get("passed") == 2 and tested.get("failed") == 0 and tested.get("status") == "tested", short(tested))

    before = s.tools()
    published = s.tool("publish_tool", {"name": name})
    review = published.get("reviewFile")
    review_text = open(review, encoding="utf-8").read() if review and os.path.exists(review) else ""
    check("R publish_tool → pending_approval, review names this exe and host", published.get("status") == "pending_approval" and "**Host:** navis" in review_text and "HPNavis.Mcp.Server.exe registry approve" in review_text and "HPRebar.Mcp.Server.exe" not in review_text and name not in before, f"review={review}")
    gated = s.tool("run_tool", {"name": name, "args": {}})
    check("R run_tool refused while pending", gated.get("isError") and "pending" in (gated.get("message") or "").lower(), short(gated.get("message")))

    t0 = time.time()
    code, out = cli("approve", name, "--by", "harness (CLI)")
    check("R CLI approve on the Navis exe", code == 0 and "published" in out.lower(), out.strip()[:200])
    latency = wait_for_tool(s, name, True, 15)
    check("R running server lists the tool after approve (no restart)", latency is not None and latency <= 5, f"visible after {latency}s; list_changed={len(s.list_changed_since(t0))}")

    hit = s.tool("search_tools", {"query": "count items by source file", "limit": 5})
    check("R search now hits", any(t.get("name") == name for t in hit.get("tools", [])), [t.get("name") for t in hit.get("tools", [])])
    r = s.tool(name, {"maxItems": 1000})
    check("R call by name", ok(r) and isinstance(r.get("value"), list) and r.get("runId"), f"runId={r.get('runId')} groups={len(r.get('value') or [])}")

    fragile = "mcp_verify_selection_set_count"
    proposed = s.tool("propose_tool", {
        "name": fragile, "title": "Count items of a selection set", "description": "Resolves a saved selection set by name and counts its items; fails when the set does not exist.",
        "category": "Selection", "tags": ["selection-sets", "count"],
        "inputSchema": schema({"setName": {"type": "string"}}, ["setName"]), "code": SET_COUNT_FRAGILE,
        "examples": [{"title": "existing", "args": {"setName": "MCP verify"}}, {"title": "other", "args": {"setName": "MCP same 1"}}],
        "transaction": "none", "timeoutSeconds": 30})
    tested = s.tool("test_tool", {"name": fragile})
    published = s.tool("publish_tool", {"name": fragile})
    code, out = cli("approve", fragile, "--by", "harness (CLI)")
    check("R propose/test/publish/approve the fragile tool", proposed.get("accepted") and tested.get("failed") == 0 and published.get("status") == "pending_approval" and code == 0 and wait_for_tool(s, fragile, True, 15) is not None, f"tested={tested.get('passed')}/{tested.get('failed')}")

    fails = sum(1 for _ in range(5) if s.tool(fragile, {"setName": "NONEXISTENT"}).get("isError"))
    gone = wait_for_tool(s, fragile, False, 10)
    detail = s.tool("get_tool", {"name": fragile})
    save("r-quarantined", detail)
    check("R 5 failing runs (InvalidOperationException) → quarantined, out of tools/list", fails == 5 and gone is not None and detail.get("status") == "quarantined", f"fails={fails} gone after {gone}s status={detail.get('status')}")
    refused = s.tool("run_tool", {"name": fragile, "args": {"setName": "MCP verify"}})
    check("R run_tool refuses a quarantined tool", refused.get("isError") and "quarantin" in (refused.get("message") or "").lower(), short(refused.get("message")))

    restored = s.tool("manage_tool", {"name": fragile, "action": "restore", "reason": "guard the set name"})
    fixed = s.tool("propose_tool", {
        "name": fragile, "title": "Count items of a selection set", "description": "Resolves a saved selection set by name and counts its items; refuses an unknown set name.",
        "category": "Selection", "tags": ["selection-sets", "count"],
        "inputSchema": schema({"setName": {"type": "string"}}, ["setName"]), "code": SET_COUNT_TOOL,
        "examples": [{"title": "existing", "args": {"setName": "MCP verify"}}, {"title": "other", "args": {"setName": "MCP same 1"}}],
        "transaction": "none", "timeoutSeconds": 30, "newVersion": True})
    tested = s.tool("test_tool", {"name": fragile})
    published = s.tool("publish_tool", {"name": fragile})
    code, out = cli("approve", fragile, "--by", "harness (CLI)")
    back = wait_for_tool(s, fragile, True, 15)
    check("R restore → newVersion → test → publish → approve → back", (restored.get("status") or "").lower() == "draft" and fixed.get("accepted") and fixed.get("version") == 2 and tested.get("failed") == 0 and code == 0 and back is not None, f"v={fixed.get('version')} back after {back}s")
    for _ in range(5):
        r = s.tool(fragile, {"setName": "NONEXISTENT"})
    still = wait_for_tool(s, fragile, True, 3)
    detail = s.tool("get_tool", {"name": fragile})
    check("R 5 argument errors do not quarantine the guarded tool", r.get("isError") and "ArgumentException" in (r.get("message") or "") and still is not None and detail.get("status") == "published", f"status={detail.get('status')}")

    heavy = s.tool("propose_tool", {
        "name": "mcp_verify_run_all_clashes", "title": "Run all clash tests", "description": "Runs every clash test (heavy).",
        "category": "Clash", "tags": ["clash"], "inputSchema": schema({}, []), "code": HEAVY_PROPOSAL,
        "examples": [{"title": "a", "args": {}}, {"title": "b", "args": {}}], "transaction": "auto", "timeoutSeconds": 600})
    text = json.dumps(heavy).lower()
    check("R propose_tool with TestsRunAllTests is refused (heavy tools are seed-only)", not heavy.get("accepted") and ("heavy" in text or "allow heavy operations" in text), short(heavy))


# ---- C: context / resources / prompts -------------------------------------------------------------------------------------------
def scenario_c(s):
    ctx = s.tool("get_navis_context", {"includeSelection": True})
    nav = ctx.get("navis") or {}
    check("C get_navis_context: navis block, not busy, heavy off, no revit fields", ok(ctx) and ctx.get("host") == "navis" and nav.get("isBusy") is False and nav.get("heavyOperationsEnabled") is False and "revitVersion" not in ctx, short(nav))

    res = s.rpc("resources/read", {"uri": "navis://document/info"})
    text = json.dumps(res)
    check("C resource navis://document/info", "documentUnits" in text and "error" not in res, short(res)[:200])
    reg = s.rpc("resources/read", {"uri": "registry://tools"})
    check("C resource registry://tools lists the seeds", "get_model_info" in json.dumps(reg) and "error" not in reg, short(reg)[:160])

    prompt = s.prompt("navis_query_template", {"question": "how many walls?"})
    ptext = json.dumps(prompt, ensure_ascii=False)
    check("C prompt navis_query_template", "execute_navis_code" in ptext and "Search" in ptext and 'transaction=\\"none\\"' in ptext, f"{len(ptext)} chars")

    r = s.tool("inspect_type", {"typeName": "Search"})
    check("C inspect_type Search", ok(r) and "FindAll" in json.dumps(r) and "Autodesk.Navisworks.Api.Search" in json.dumps(r), short(r)[:160])


# ---- heavy / modal / nodoc phases -----------------------------------------------------------------------------------------
def phase_heavy(s, append_file):
    types = json.load(open(os.path.join(OUT, "types.json"), encoding="utf-8")) if OUT and os.path.exists(os.path.join(OUT, "types.json")) else {"typeA": "Group", "typeB": "Group"}
    t0 = time.time()
    r = s.tool("run_tool", {"name": "create_and_run_clash_test", "args": {"name": "MCP verify clash", "toleranceMm": 0, "a": {"category": "Item", "property": "Type", "value": types["typeA"]}, "b": {"category": "Item", "property": "Type", "value": types["typeB"]}}}, timeout=700)
    v = r.get("value") or {}
    check("H run_tool create_and_run_clash_test with heavy ON: Complete, counted, 600 s honoured, never rolledBack", ok(r) and v.get("status") == "Complete" and "resultCount" in v and r.get("rolledBack") is False and not any("clamped" in l for l in r.get("logs") or []), f"results={v.get('resultCount')} elapsedMs={v.get('elapsedMs')} wall={time.time() - t0:.1f}s")
    g = s.tool("run_tool", {"name": "get_clash_results", "args": {"testName": "MCP verify clash", "maxResults": 3}})
    tests = (g.get("value") or {}).get("tests") or []
    check("H get_clash_results sees the new test", ok(g) and len(tests) == 1 and tests[0].get("resultCount") == v.get("resultCount"), short(tests))

    if append_file and os.path.exists(append_file):
        before = counts(s)
        r = execute(s, "doc.AppendFile(args.Str(\"path\", \"\")); return doc.Models.Count;", label="append", args={"path": append_file}, timeout_s=300, wait=400)
        ctx = s.tool("get_navis_context", {})
        nav = ctx.get("navis") or {}
        check("H append with heavy ON, then context: modelCount +1, per-model units, not busy, heavy flag on", ok(r) and r.get("rolledBack") is False and nav.get("modelCount") == before["models"] + 1 and len(nav.get("models") or []) == nav.get("modelCount") and nav.get("isBusy") is False and nav.get("heavyOperationsEnabled") is True, f"models {before['models']}->{nav.get('modelCount')} heavy={nav.get('heavyOperationsEnabled')}")
    else:
        skip("H append + context", "no .nwc sample given")


def phase_disabled(s):
    r = execute(s, "return 1;", transaction="none", label="disabled")
    ctx = s.tool("get_navis_context", {})
    check("D opt-in off: execute refused naming the checkbox, context still answers", r.get("isError") and "Allow AI code execution" in (r.get("message") or "") and ok(ctx) and ctx.get("host") == "navis", short(r.get("message")))


def phase_modal(s):
    t0 = time.time()
    r = execute(s, "return 1;", transaction="none", label="modal", wait=60)
    check("M modal open: execute → busy error within the grace, script never ran", r.get("isError") and ("dialog" in (r.get("message") or "").lower() or "busy" in (r.get("message") or "").lower()) and 5 <= time.time() - t0 <= 40, f"{short(r.get('message'))} in {time.time() - t0:.1f}s")


def phase_after_modal(s):
    t0 = time.time()
    r = execute(s, "return 2;", transaction="none", label="after modal")
    check("M dialog closed: execute runs again at once", ok(r) and r.get("value") == 2 and time.time() - t0 < 5, f"in {time.time() - t0:.1f}s")


def phase_nodoc(s):
    ctx = s.tool("get_navis_context", {})
    r = execute(s, "return 1;", transaction="none", label="nodoc")
    check("N no model: context isClear, execute refused with the model noun", ((ctx.get("navis") or {}).get("isClear") is True or ctx.get("isError")) and r.get("isError") and (".nwd" in (r.get("message") or "") or "model" in (r.get("message") or "").lower()), f"ctx={short(ctx)[:120]} exec={short(r.get('message'))}")


def main():
    global OUT, EXE
    ap = argparse.ArgumentParser()
    ap.add_argument("exe")
    ap.add_argument("--registry", required=True)
    ap.add_argument("--phase", choices=["main", "disabled", "heavy", "modal", "aftermodal", "nodoc"], default="main")
    ap.add_argument("--out", default=None)
    ap.add_argument("--append-file", default="")
    a = ap.parse_args()
    EXE = a.exe
    OUT = a.out
    if OUT:
        os.makedirs(OUT, exist_ok=True)

    env = dict(os.environ)
    env["HPNAVIS_MCP_Registry__LibraryPath"] = os.path.join(a.registry, "tools-library")
    env["HPNAVIS_MCP_Registry__DbPath"] = os.path.join(a.registry, "registry.db")
    os.makedirs(env["HPNAVIS_MCP_Registry__LibraryPath"], exist_ok=True)
    os.environ.update({k: env[k] for k in ("HPNAVIS_MCP_Registry__LibraryPath", "HPNAVIS_MCP_Registry__DbPath")})  # the CLI approve uses the same root

    started = time.time()
    s = Server(a.exe, env=env, name="navis-verify")
    try:
        s.initialize()
        time.sleep(1.0)
        if a.phase == "main":
            scenario_e(s)
            scenario_s(s)
            scenario_r(s)
            scenario_c(s)
        elif a.phase == "disabled":
            phase_disabled(s)
        elif a.phase == "heavy":
            phase_heavy(s, a.append_file)
        elif a.phase == "modal":
            phase_modal(s)
        elif a.phase == "aftermodal":
            phase_after_modal(s)
        else:
            phase_nodoc(s)
    finally:
        s.close()

    passed = sum(1 for x in results if x["pass"])
    skipped = sum(1 for x in results if x.get("skipped"))
    summary = {"phase": a.phase, "passed": passed - skipped, "skipped": skipped, "failed": [x["name"] for x in results if not x["pass"]], "total": len(results), "seconds": round(time.time() - started, 1)}
    save(f"summary-{a.phase}", {"summary": summary, "results": results})
    print(json.dumps(summary))
    sys.exit(0 if not summary["failed"] else 1)


if __name__ == "__main__":
    main()
