"""Throw-away live check of the script-quality gate on Revit 2026 (acceptance 7 of the brief).

Runs the Debug HPRebar.Mcp.Server.exe on an isolated registry (never the user's %AppData% root) against the bridge of
the Revit instance that holds the pipe: propose a script with commented-out code and an empty catch (must be refused),
then a script with one vague name (accepted with a warning), test it, publish it under the manual policy and read the
review file. The active Revit document must live under the golden-run folder (scratch copy).
"""
import json, os, shutil, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "McpShared", "tools"))
import importlib.util
spec = importlib.util.spec_from_file_location("mcp_session", os.path.join(ROOT, "McpShared", "tools", "mcp-session.py"))
mcp_session = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mcp_session)

EXE = os.path.join(ROOT, "HPRebar", "HPRebar.Mcp.Server", "bin", "Debug", "net10.0", "HPRebar.Mcp.Server.exe")
RUN_DIR = os.path.join(ROOT, "HPRebar", "output", "golden", "fixture-build")
REGISTRY = os.path.join(HERE, "live-registry")

SCHEMA = {"type": "object", "properties": {"count": {"type": "integer", "default": 2}}}
EXAMPLES = [{"title": "default", "args": {}}, {"title": "five", "args": {"count": 5}}]
BAD = "// var old = args.Int(\"count\", 1);\nint count = args.Int(\"count\", 2);\ntry { count++; } catch { }\nreturn count;"
GOOD = "var data = args.Int(\"count\", 2);\nreturn data * 2;"

checks = []


def check(name, ok, detail=""):
    checks.append({"check": name, "pass": bool(ok), "detail": detail})


shutil.rmtree(REGISTRY, ignore_errors=True)
env = dict(os.environ)
env["HPREBAR_MCP_Bridge__RevitVersion"] = "2026"
env["HPREBAR_MCP_Registry__LibraryPath"] = os.path.join(REGISTRY, "tools-library")
env["HPREBAR_MCP_Registry__DbPath"] = os.path.join(REGISTRY, "registry.db")
server = mcp_session.Server(EXE, env)
try:
    server.initialize()
    context = server.tool("get_revit_context", {})
    doc_path = context.get("docPath") or ""
    check("context: scratch document under the run folder, execution enabled",
          doc_path.lower().startswith(RUN_DIR.lower()) and context.get("executionEnabled"), doc_path)
    if not checks[-1]["pass"]:
        raise SystemExit("refusing to continue: " + json.dumps(context)[:400])

    bad = server.tool("propose_tool", {"name": "mcp_verify_quality_bad", "description": "Live check: commented-out code and an empty catch",
                                       "category": "Generic", "inputSchema": SCHEMA, "code": BAD, "examples": EXAMPLES, "transaction": "none"})
    errors = " | ".join(bad.get("errors", [])) if isinstance(bad.get("errors"), list) else json.dumps(bad)
    check("bad script refused with Q-B1 and Q-B2", not bad.get("accepted", False) and "quality Q-B1" in errors and "quality Q-B2" in errors, errors[:400])

    good = server.tool("propose_tool", {"name": "mcp_verify_quality_good", "description": "Live check: doubles a count, one vague name",
                                        "category": "Generic", "inputSchema": SCHEMA, "code": GOOD, "examples": EXAMPLES, "transaction": "none"})
    warnings = " | ".join(good.get("warnings", [])) if isinstance(good.get("warnings"), list) else json.dumps(good)
    check("good script accepted with a Q-W3 warning", good.get("accepted") is True and "quality Q-W3" in warnings, warnings[:400])

    tested = server.tool("test_tool", {"name": "mcp_verify_quality_good"})
    check("test_tool passes both examples in Revit", tested.get("passed") == 2 and tested.get("failed") == 0, json.dumps(tested)[:300])

    published = server.tool("publish_tool", {"name": "mcp_verify_quality_good"})
    review_file = published.get("reviewFile") or ""
    review = open(review_file, encoding="utf-8").read() if review_file and os.path.isfile(review_file) else ""
    check("publish under the manual policy writes a review with the quality record",
          published.get("status") == "pending_approval" and "## Code quality" in review and "analysed: 0 error(s), 1 warning(s)" in review,
          review[review.find("## Code quality"):review.find("## Code quality") + 160] if review else json.dumps(published)[:300])
finally:
    server.close()

print(json.dumps({"checks": checks, "passed": sum(c["pass"] for c in checks), "total": len(checks)}, indent=1, ensure_ascii=False))
sys.exit(0 if all(c["pass"] for c in checks) else 1)
