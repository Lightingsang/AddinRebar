"""Live check of the 12 Navisworks seed tools through the published server against a running bridge
(run-seeds-live.ps1 starts Roamer, ticks the opt-ins and calls this twice on the same isolated registry root):

  --phase normal   seeds installed (tools/list == 24), search_tools finds them, every read-only seed runs on the
                   open model, the review seeds pass test_tool (dry run) and one real run, the heavy seed is
                   refused by test_tool and by run_tool while heavy operations are off
  --phase heavy    heavy operations allowed: run_tool create_and_run_clash_test, then get_clash_results sees it

Usage: python seeds-live.py <exe> --registry <dir> --phase normal|heavy
"""
import argparse, json, os, sys, time, importlib.util

for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mcp_session", os.path.join(HERE, "..", "..", "..", "McpShared", "tools", "mcp-session.py"))
mcp_session = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mcp_session)

SEEDS = ["get_model_info", "get_selected_item_properties", "find_items_by_property", "list_selection_sets", "list_viewpoints", "get_clash_results",
         "get_timeliner_tasks", "summarize_by_category", "create_selection_set_from_search", "create_viewpoint", "override_color_by_search", "create_and_run_clash_test"]
results = []


def check(name, cond, detail=""):
    results.append({"name": name, "pass": bool(cond), "detail": detail})
    print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:420])


def ok(r):
    return not r.get("isError") and not r.get("rpcError")


def phase_normal(s):
    tools = s.tools()
    check("tools/list == 24 (12 core/registry + 12 seeds installed on first start)", len(tools) == 24 and all(n in tools for n in SEEDS), f"{len(tools)}: missing {[n for n in SEEDS if n not in tools]}")

    hits = s.tool("search_tools", {"query": "clash"})
    names = [h.get("name") for h in (hits.get("hits") or hits.get("tools") or [])]
    check("search_tools 'clash' → get_clash_results + create_and_run_clash_test", "get_clash_results" in names and "create_and_run_clash_test" in names, json.dumps(names))

    r = s.tool("run_tool", {"name": "get_model_info", "args": {"includeRootItems": True}})
    v = r.get("value") or {}
    check("run_tool get_model_info", ok(r) and v.get("documentUnits") and v.get("modelCount", 0) >= 1 and (v.get("rootItems") or []), f"title={v.get('title')} units={v.get('documentUnits')} models={v.get('modelCount')} roots={v.get('rootItemCount')} runId={r.get('runId')}")

    r = s.tool("run_tool", {"name": "summarize_by_category", "args": {}})
    v = r.get("value") or {}
    classes = v.get("groups") or []
    check("run_tool summarize_by_category (by class)", ok(r) and len(classes) >= 1 and v.get("itemsVisited", 0) > 0, f"visited={v.get('itemsVisited')} groups={v.get('groupCount')} top={classes[:3]}")

    r = s.tool("run_tool", {"name": "summarize_by_category", "args": {"groupBy": "Item.Type", "maxGroups": 20}})
    v = r.get("value") or {}
    types = [g["key"] for g in (v.get("groups") or []) if g.get("key")]
    check("run_tool summarize_by_category (Item.Type)", ok(r) and len(types) >= 1, f"types={types[:6]}")
    type_a = types[0] if types else "Group"
    type_b = types[1] if len(types) > 1 else type_a

    r = s.tool("run_tool", {"name": "find_items_by_property", "args": {"category": "Item", "property": "Type", "op": "equals", "value": type_a, "maxResults": 5}})
    v = r.get("value") or {}
    items = v.get("items") or []
    check("run_tool find_items_by_property (Item.Type equals the most common type)", ok(r) and v.get("total", 0) >= 1 and items and all("name" in i and "path" in i for i in items), f"total={v.get('total')} shown={v.get('shown')} first={json.dumps(items[:1])[:160]}")

    bad = s.tool("run_tool", {"name": "find_items_by_property", "args": {"category": "Item", "property": "Type", "op": "gt", "value": "abc"}})
    check("run_tool find_items_by_property with a non-numeric gt value → ArgumentException (caller's error, not the tool's)", bad.get("isError") and "ArgumentException" in (bad.get("message") or ""), (bad.get("message") or "")[:160])

    sel = s.tool("execute_navis_code", {"code": "doc.CurrentSelection.Clear(); doc.CurrentSelection.Add(doc.Models.RootItems.First().Children.First()); return doc.CurrentSelection.SelectedItems.Count;", "transaction": "auto", "label": "select one"})
    r = s.tool("run_tool", {"name": "get_selected_item_properties", "args": {"maxItems": 3}})
    v = r.get("value") or {}
    first = (v.get("items") or [{}])[0]
    check("run_tool get_selected_item_properties (after selecting one item)", ok(sel) and ok(r) and v.get("count", 0) >= 1 and (first.get("categories") or []), f"count={v.get('count')} categories={len(first.get('categories') or [])} first={first.get('name')}")

    r = s.tool("run_tool", {"name": "list_selection_sets", "args": {"includeCounts": True}})
    rows = r.get("value") if isinstance(r.get("value"), list) else []
    check("run_tool list_selection_sets", ok(r) and isinstance(r.get("value"), list), f"{len(rows)} entries: {json.dumps(rows[:2])[:160]}")

    r = s.tool("run_tool", {"name": "list_viewpoints", "args": {"includeComments": True}})
    rows = r.get("value") if isinstance(r.get("value"), list) else []
    check("run_tool list_viewpoints", ok(r) and isinstance(r.get("value"), list) and all("positionMm" in x and "guid" in x for x in rows), f"{len(rows)} viewpoints: {json.dumps(rows[:1])[:160]}")
    viewpoints_before = len(rows)

    r = s.tool("run_tool", {"name": "get_clash_results", "args": {"maxResults": 3}})
    v = r.get("value") or {}
    tests = v.get("tests") or []
    check("run_tool get_clash_results (whatever tests the sample has)", ok(r) and isinstance(v.get("tests"), list) and isinstance(v.get("results"), list) and v.get("testCount") == len(tests) and all("byStatus" in t for t in tests), f"{len(tests)} tests: {json.dumps(tests[:1])[:200]}")

    task = s.tool("execute_navis_code", {"code": "var t = new TimelinerTask { DisplayName = args.Str(\"name\", \"MCP task\"), PlannedStartDate = DateTime.Today, PlannedEndDate = DateTime.Today.AddDays(5) }; doc.GetTimeliner().TaskAddCopy(t); return (int)doc.GetTimeliner().TaskTotalTasks();",
                                       "transaction": "auto", "label": "add task", "args": {"name": "MCP seed task"}})
    r = s.tool("run_tool", {"name": "get_timeliner_tasks", "args": {"includeSelection": True}})
    v = r.get("value") or {}
    tasks = v.get("tasks") or []
    check("run_tool get_timeliner_tasks (after adding one task)", ok(task) and ok(r) and v.get("total", 0) >= 1 and any(t.get("name") == "MCP seed task" for t in tasks), f"total={v.get('total')} shown={v.get('shown')} first={json.dumps(tasks[:1])[:160]}")

    before = (s.tool("execute_navis_code", {"code": "return doc.SelectionSets.Value.Count;", "transaction": "none", "label": "count"}).get("value"))
    t = s.tool("test_tool", {"name": "create_selection_set_from_search", "cases": [{"title": "live", "args": {"name": "MCP test set", "category": "Item", "property": "Type", "value": type_a}}]})
    after = (s.tool("execute_navis_code", {"code": "return doc.SelectionSets.Value.Count;", "transaction": "none", "label": "count"}).get("value"))
    check("test_tool create_selection_set_from_search (dry run): passes, set count unchanged", ok(t) and t.get("passed", t.get("succeeded", 0)) >= 1 and before == after, f"{json.dumps(t)[:220]} sets {before}->{after}")

    t = s.tool("test_tool", {"name": "create_viewpoint", "cases": [{"title": "live", "args": {"name": "MCP test vp", "comment": "dry run", "author": "harness"}}]})
    check("test_tool create_viewpoint (dry run)", ok(t) and t.get("passed", t.get("succeeded", 0)) >= 1, json.dumps(t)[:220])

    t = s.tool("test_tool", {"name": "override_color_by_search", "cases": [{"title": "live", "args": {"category": "Item", "property": "Type", "value": type_a, "r": 0, "g": 128, "b": 255}}]})
    check("test_tool override_color_by_search (dry run)", ok(t) and t.get("passed", t.get("succeeded", 0)) >= 1, json.dumps(t)[:220])

    r = s.tool("run_tool", {"name": "create_viewpoint", "args": {"name": "MCP seed viewpoint", "comment": "created by the seed harness", "author": "harness"}})
    v = r.get("value") or {}
    rows = s.tool("run_tool", {"name": "list_viewpoints", "args": {}}).get("value") or []
    check("run_tool create_viewpoint (real) → list_viewpoints grows by one", ok(r) and v.get("guid") and len(rows) == viewpoints_before + 1 and any(x.get("name") == "MCP seed viewpoint" for x in rows), f"guid={v.get('guid')} viewpoints {viewpoints_before}->{len(rows)} runId={r.get('runId')}")

    t = s.tool("test_tool", {"name": "create_and_run_clash_test", "cases": [{"title": "live", "args": {"name": "MCP dry clash", "a": {"category": "Item", "property": "Type", "value": type_a}, "b": {"category": "Item", "property": "Type", "value": type_b}}}]})
    text = json.dumps(t)
    check("test_tool create_and_run_clash_test: refused (heavy cannot be dry-run) with a clear message", (t.get("isError") or t.get("passed", 1) == 0 or "dryRun" in text or "HEAVY" in text or "heavy" in text) and ("dryRun" in text or "heavy" in text.lower()), text[:240])

    r = s.tool("run_tool", {"name": "create_and_run_clash_test", "args": {"name": "MCP heavy off", "a": {"category": "Item", "property": "Type", "value": type_a}, "b": {"category": "Item", "property": "Type", "value": type_b}}})
    diags = r.get("diagnostics") or []
    check("run_tool create_and_run_clash_test with heavy OFF → HEAVY refusal", r.get("isError") and any(d.get("id") == "HEAVY" for d in diags), " | ".join(d.get("message", "")[:80] for d in diags) or json.dumps(r)[:200])

    with open(os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "..", "..", "output", "spike", "seeds-types.json"), "w", encoding="utf-8") as f:
        json.dump({"typeA": type_a, "typeB": type_b}, f)


def phase_heavy(s):
    path = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "..", "..", "output", "spike", "seeds-types.json")
    with open(path, encoding="utf-8") as f:
        types = json.load(f)
    t0 = time.time()
    r = s.tool("run_tool", {"name": "create_and_run_clash_test", "args": {"name": "MCP seed clash", "toleranceMm": 0, "a": {"category": "Item", "property": "Type", "value": types["typeA"]}, "b": {"category": "Item", "property": "Type", "value": types["typeB"]}}}, timeout=700)
    v = r.get("value") or {}
    logs = r.get("logs") or []
    check("run_tool create_and_run_clash_test with heavy ON: ran, results counted, 600 s not clamped, never rolledBack",
          ok(r) and v.get("status") and "resultCount" in v and r.get("rolledBack") is False and not any("clamped" in l for l in logs),
          f"status={v.get('status')} results={v.get('resultCount')} byStatus={v.get('byStatus')} elapsedMs={v.get('elapsedMs')} A={v.get('itemsA')} B={v.get('itemsB')} wall={time.time() - t0:.1f}s runId={r.get('runId')}")

    g = s.tool("run_tool", {"name": "get_clash_results", "args": {"testName": "MCP seed clash", "maxResults": 5}})
    gv = g.get("value") or {}
    tests = gv.get("tests") or []
    listed = gv.get("results") or []
    check("run_tool get_clash_results for the new test: one summary, its count, ≤ 5 listed results", ok(g) and len(tests) == 1 and tests[0].get("resultCount") == v.get("resultCount") and len(listed) <= 5 and all(x.get("test") == "MCP seed clash" for x in listed), json.dumps(tests[:1])[:240])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("exe")
    ap.add_argument("--registry", required=True)
    ap.add_argument("--phase", choices=["normal", "heavy"], default="normal")
    a = ap.parse_args()

    env = dict(os.environ)
    env["HPNAVIS_MCP_Registry__LibraryPath"] = os.path.join(a.registry, "tools-library")
    env["HPNAVIS_MCP_Registry__DbPath"] = os.path.join(a.registry, "registry.db")
    os.makedirs(env["HPNAVIS_MCP_Registry__LibraryPath"], exist_ok=True)

    s = mcp_session.Server(a.exe, env=env, name="navis-seeds")
    try:
        s.initialize()
        time.sleep(1.0)  # seed install + registry index on first start
        (phase_normal if a.phase == "normal" else phase_heavy)(s)
    finally:
        s.close()

    passed = sum(1 for x in results if x["pass"])
    print(json.dumps({"phase": a.phase, "passed": passed, "total": len(results), "failed": [x["name"] for x in results if not x["pass"]]}))
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
