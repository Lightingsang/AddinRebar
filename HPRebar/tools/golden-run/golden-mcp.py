"""Golden-run MCP calls against the Revit that holds the bridge pipe, through the Debug HPRebar.Mcp.Server on an isolated
registry (never the user's %AppData% registry). Every script refuses unless the active document lives under --run-dir.

    python golden-mcp.py --run-dir <dir> context
    python golden-mcp.py --run-dir <dir> select --marks C1-LOWER C1-UPPER
    python golden-mcp.py --run-dir <dir> snapshot --scope column --out <file.json>

Prints the tool result as JSON; exit 1 when the tool reports an error.
"""
import argparse
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "McpShared", "tools"))
from harness_common import Server, utf8_console  # noqa: E402

EXE = os.path.join(ROOT, "HPRebar", "HPRebar.Mcp.Server", "bin", "Debug", "net10.0", "HPRebar.Mcp.Server.exe")
SCRIPTS = os.path.join(HERE, "scripts")


def script(name):
    with open(os.path.join(SCRIPTS, name), encoding="utf-8-sig") as f:
        return f.read()


def execute(server, name, args, timeout=120):
    return server.tool("execute_revit_code", {"code": script(name), "args": args, "transaction": "none",
                                              "timeoutSeconds": timeout, "label": "golden " + name.split(".")[0]}, timeout + 30)


def main():
    utf8_console()
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--run-dir", required=True)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("context")
    select = sub.add_parser("select")
    select.add_argument("--marks", nargs="+", required=True)
    snap = sub.add_parser("snapshot")
    snap.add_argument("--scope", choices=["column", "foundation", "beam", "all"], required=True)
    snap.add_argument("--out", required=True)
    args = parser.parse_args()

    run_dir = os.path.abspath(args.run_dir)
    registry = os.path.join(run_dir, "registry")
    env = dict(os.environ)
    env["HPREBAR_MCP_Bridge__RevitVersion"] = "2026"
    env["HPREBAR_MCP_Registry__LibraryPath"] = os.path.join(registry, "tools-library")
    env["HPREBAR_MCP_Registry__DbPath"] = os.path.join(registry, "registry.db")

    server = Server(EXE, env, name="golden")
    try:
        server.initialize()
        if args.command == "context":
            result = server.tool("get_revit_context", {"includeSelection": True})
        elif args.command == "select":
            result = execute(server, "select-by-mark.csx", {"runDir": run_dir, "marks": args.marks})
        else:
            result = execute(server, "snapshot.csx", {"runDir": run_dir, "scope": args.scope})
            if not result.get("isError"):
                value = result.get("value", result)
                if result.get("truncated"):
                    result = {"isError": True, "message": "snapshot truncated by the bridge output cap - narrow the scope"}
                else:
                    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
                        json.dump(value, f, indent=1, ensure_ascii=False, sort_keys=True)
                        f.write("\n")
    finally:
        server.close()

    print(json.dumps(result, ensure_ascii=False)[:4000])
    return 1 if result.get("isError") else 0


if __name__ == "__main__":
    sys.exit(main())
