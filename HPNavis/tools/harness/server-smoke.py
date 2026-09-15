"""Stdio smoke of the published HPNavis.Mcp.Server.exe against a running bridge (Roamer.exe with the plugin,
listener up, execution opt-in ticked — run-server-smoke.ps1 does that part). One session: initialize,
tools/list, get_navis_context, a read-only execute, a dry-run edit, inspect_type, search_tools. Prints one
line per check and a JSON summary; exit 1 on any failure.

Usage: python server-smoke.py <exe> --registry <isolated root dir>
"""
import argparse, json, os, sys, time

for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

HERE = os.path.dirname(os.path.abspath(__file__))
import importlib.util

spec = importlib.util.spec_from_file_location("mcp_session", os.path.join(HERE, "..", "..", "..", "HPAutoCad", "tools", "harness", "mcp-session.py"))
mcp_session = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mcp_session)

results = []


def check(name, cond, detail=""):
    results.append({"name": name, "pass": bool(cond), "detail": detail})
    print(f"{'PASS' if cond else 'FAIL'} {name} {detail}"[:400])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("exe")
    ap.add_argument("--registry", required=True)
    a = ap.parse_args()

    env = dict(os.environ)
    env["HPNAVIS_MCP_Registry__LibraryPath"] = os.path.join(a.registry, "tools-library")
    env["HPNAVIS_MCP_Registry__DbPath"] = os.path.join(a.registry, "registry.db")
    os.makedirs(env["HPNAVIS_MCP_Registry__LibraryPath"], exist_ok=True)

    s = mcp_session.Server(a.exe, env=env, name="navis-smoke")
    try:
        init = s.initialize()
        check("initialize: serverInfo.name == HPNavis MCP", init.get("serverInfo", {}).get("name") == "HPNavis MCP", json.dumps(init.get("serverInfo")))

        tools = s.tools()
        expected = {"execute_navis_code", "get_navis_context", "inspect_type", "cancel_execution", "search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"}
        check("tools/list == the 12 core + registry tools", set(tools) == expected, f"{len(tools)}: {sorted(tools)}")

        t0 = time.time()
        ctx = s.tool("get_navis_context", {"includeSelection": True})   # the context snapshot itself
        nav = ctx.get("navis") or {}
        check("get_navis_context: host=navis, navis block, no revitVersion/isFamily, fast",
              not ctx.get("isError") and ctx.get("host") == "navis" and nav.get("documentUnits") and "revitVersion" not in ctx and "isFamily" not in ctx and time.time() - t0 < 5,
              f"title={ctx.get('docTitle')} units={nav.get('documentUnits')} models={nav.get('modelCount')} sets={nav.get('selectionSetCount')} clash={nav.get('hasClashModule')} heavy={nav.get('heavyOperationsEnabled')} in {time.time() - t0:.2f}s")

        r = s.tool("execute_navis_code", {"code": "return new { title = doc.Title, sets = doc.SelectionSets.Value.Count, mm = units.ToMm(1.0) };", "transaction": "none", "label": "smoke read"})   # the ExecuteResult
        check("execute_navis_code none: title + counts, changed 0/0/0", not r.get("isError") and (r.get("value") or {}).get("title", "").startswith("gatehouse") and r.get("changed") == {"added": 0, "modified": 0, "deleted": 0},
              json.dumps(r)[:200])

        def sets():
            return s.tool("execute_navis_code", {"code": "return doc.SelectionSets.Value.Count;", "transaction": "none", "label": "count"}).get("value")

        before = sets()
        r = s.tool("execute_navis_code", {"code": "doc.SelectionSets.AddCopy(new SelectionSet(new ModelItemCollection()) { DisplayName = args.Str(\"name\", \"MCP smoke\") }); return doc.SelectionSets.Value.Count;",
                                          "transaction": "auto", "dryRun": True, "label": "smoke dry", "args": {"name": "MCP smoke set"}})
        after = sets()
        check("execute_navis_code dryRun: added 1, rolledBack true, count unchanged afterwards",
              not r.get("isError") and r.get("rolledBack") is True and (r.get("changed") or {}).get("added") == 1 and before == after,
              f"value={r.get('value')} changed={r.get('changed')} rolledBack={r.get('rolledBack')} sets {before}->{after}")

        r = s.tool("execute_navis_code", {"code": "doc.AppendFile(@\"C:\\x.nwc\"); return 1;", "transaction": "auto", "label": "smoke heavy off"})
        diags = r.get("diagnostics") or []
        check("execute_navis_code heavy OFF: HEAVY diagnostic through the server", r.get("isError") is True and any(d.get("id") == "HEAVY" for d in diags), " | ".join(d.get("message", "")[:80] for d in diags))

        r = s.tool("inspect_type", {"typeName": "SelectionSet"})
        text = json.dumps(r)
        check("inspect_type SelectionSet: members listed", not r.get("isError") and "DisplayName" in text, text[:160])

        r = s.tool("search_tools", {"query": "selection set"})
        check("search_tools answers with an empty registry: 0 hits and the execute hint", not r.get("rpcError") and r.get("count") == 0 and "execute_navis_code" in (r.get("hint") or ""), json.dumps(r)[:120])
    finally:
        s.close()

    passed = sum(1 for x in results if x["pass"])
    print(json.dumps({"passed": passed, "total": len(results), "failed": [x["name"] for x in results if not x["pass"]]}))
    sys.exit(0 if passed == len(results) else 1)


if __name__ == "__main__":
    main()
