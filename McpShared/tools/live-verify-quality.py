"""Live check of the script-quality gate (`propose_tool`) against one running HP MCP bridge.

    python McpShared/tools/live-verify-quality.py --host navis [--out <folder>] [--exe <server.exe>]
    python McpShared/tools/live-verify-quality.py --list

Runs the host's Debug server exe (`<Host>/<Host>.Mcp.Server/bin/Debug/<tfm>/`) on an isolated registry under
%TEMP%\\hp-live-quality\\<host> (deleted afterwards — never the user's %AppData% registry) and checks, against the
bridge that holds the host's pipe:
  1. the context tool answers, code execution is enabled, the active document is new or a scratch file;
  2. a script with commented-out code and an empty catch is refused with quality Q-B1 and Q-B2;
  3. a script with one vague name is accepted with a quality Q-W3 warning;
  4. test_tool runs both examples (dryRun) and passes;
  5. publish_tool under the manual policy returns pending_approval and the review file carries the quality record.
The scripts never call the host API and declare transaction "none", so no snapshot, undo entry or model change
is produced; hosts that snapshot before writes are checked for a new snapshot file.
"""
import argparse
import glob
import os
import shutil
import sys
import tempfile
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
from harness_common import Checklist, Server, short, utf8_console  # noqa: E402

# Values from each host's *HostProfile.cs: env prefix, version key/value, context tool; every profile has "Generic".
# Last column: the bridge's snapshot folder under %LOCALAPPDATA% (its BridgeEntry.cs), for hosts that snapshot before writes.
HOSTS = {
    "revit": ("HPRebar", "HPREBAR_MCP_", "RevitVersion", 2026, "get_revit_context", None),
    "autocad": ("HPAutoCad", "HPAUTOCAD_MCP_", "HostVersion", 2026, "get_autocad_context", None),
    "civil3d": ("HPCivil3d", "HPCIVIL3D_MCP_", "HostVersion", 2026, "get_civil3d_context", None),
    "navis": ("HPNavis", "HPNAVIS_MCP_", "HostVersion", 2026, "get_navis_context", None),
    "etabs": ("HPEtabs", "HPETABS_MCP_", "HostVersion", 22, "get_etabs_context", "HPEtabs/McpBridge/snapshots"),
    "sap2000": ("HPSap2000", "HPSAP2000_MCP_", "HostVersion", 27, "get_sap2000_context", "HPSap2000/McpBridge/snapshots"),
    "robot": ("HPRobot", "HPROBOT_MCP_", "HostVersion", 2026, "get_robot_context", "HPRobot/McpBridge/snapshots"),
    "excel": ("HPExcel", "HPEXCEL_MCP_", "HostVersion", 2026, "get_excel_context", "HPExcel/Snapshots"),
    "powerbi": ("HPPowerBi", "HPPOWERBI_MCP_", "HostVersion", 2026, "get_powerbi_context", "HPPowerBi/Snapshots"),
    "tekla": ("HPTekla", "HPTEKLA_MCP_", "HostVersion", 2025, "get_tekla_context", "HPTekla/McpBridge/snapshots"),
}

SCHEMA = {"type": "object", "properties": {"count": {"type": "integer", "default": 2}}}
EXAMPLES = [{"title": "default", "args": {}}, {"title": "five", "args": {"count": 5}}]
BAD_CODE = "// var old = args.Int(\"count\", 1);\nint count = args.Int(\"count\", 2);\ntry { count++; } catch { }\nreturn count;"
GOOD_CODE = "var data = args.Int(\"count\", 2);\nreturn data * 2;"
TEMPLATE_EXTENSIONS = (".rte", ".rft", ".dwt")
UNTITLED = ("", "(untitled)")


def server_exe(folder):
    hits = sorted(glob.glob(os.path.join(ROOT, folder, f"{folder}.Mcp.Server", "bin", "Debug", "*", f"{folder}.Mcp.Server.exe")))
    return hits[0] if hits else None


def is_scratch(doc_path):
    """New/untitled document, an unsaved document still named after its template, or a file under a repo output folder."""
    path = (doc_path or "").strip()
    if path.lower() in UNTITLED or path.lower().endswith(TEMPLATE_EXTENSIONS):
        return True
    if "\\" not in path and "/" not in path:
        return True  # a bare name (Excel's "Book1") is a workbook that was never saved

    full = os.path.abspath(path).lower()
    return full.startswith(ROOT.lower()) and os.sep + "output" + os.sep in full


def snapshot_files(product):
    if not product:
        return None
    root = os.path.join(os.environ.get("LOCALAPPDATA", ""), *product.split("/"))
    if not os.path.isdir(root):
        return None
    return {os.path.join(d, f) for d, _, files in os.walk(root) for f in files}


def propose(server, name, description, code):
    return server.tool("propose_tool", {"name": name, "description": description, "category": "Generic",
                                        "inputSchema": SCHEMA, "code": code, "examples": EXAMPLES, "transaction": "none"})


def joined(result, key):
    items = result.get(key)
    return " | ".join(items) if isinstance(items, list) else short(result, 400)


def run(host, exe, cl):
    folder, prefix, version_key, version, context_tool, snapshot_product = HOSTS[host]
    registry = os.path.join(tempfile.gettempdir(), "hp-live-quality", host)
    shutil.rmtree(registry, ignore_errors=True)
    env = dict(os.environ)
    env[f"{prefix}Bridge__{version_key}"] = str(version)
    env[f"{prefix}Registry__LibraryPath"] = os.path.join(registry, "tools-library")
    env[f"{prefix}Registry__DbPath"] = os.path.join(registry, "registry.db")
    snapshots_before = snapshot_files(snapshot_product)

    server = Server(exe, env, name=f"quality-{host}")
    try:
        server.initialize()
        context = server.tool(context_tool, {})
        # An unsaved document has no path (its title is Drawing1.dwg, Book1, Untitled…): that is a new document.
        doc_path = context.get("docPath") or ""
        ready = context.get("executionEnabled") is True and is_scratch(doc_path) and not context.get("isError")
        cl.check("1 context: execution enabled, new or scratch document", ready,
                 f"{context_tool}: path={doc_path!r} title={context.get('docTitle')!r} {short(context, 200)}")
        if not ready:
            return

        bad = propose(server, "mcp_verify_quality_bad", "Live check: commented-out code and an empty catch", BAD_CODE)
        errors = joined(bad, "errors")
        cl.check("2 commented-out code + empty catch refused (Q-B1, Q-B2)",
                 bad.get("accepted") is False and "quality Q-B1" in errors and "quality Q-B2" in errors, errors)

        good = propose(server, "mcp_verify_quality_good", "Live check: doubles a count, one vague name", GOOD_CODE)
        warnings = joined(good, "warnings")
        cl.check("3 vague name accepted with a Q-W3 warning", good.get("accepted") is True and "quality Q-W3" in warnings, warnings)

        tested = server.tool("test_tool", {"name": "mcp_verify_quality_good"})
        cl.check("4 test_tool 2/2 (dryRun)", tested.get("passed") == 2 and tested.get("failed") == 0, short(tested, 300))

        published = server.tool("publish_tool", {"name": "mcp_verify_quality_good"})
        review_file = published.get("reviewFile") or ""
        review = open(review_file, encoding="utf-8").read() if review_file and os.path.isfile(review_file) else ""
        start = review.find("## Code quality")
        cl.check("5 manual publish: pending_approval + review quality record",
                 published.get("status") == "pending_approval" and start >= 0 and "analysed: 0 error(s), 1 warning(s)" in review,
                 review[start:start + 160] if start >= 0 else short(published, 300))

        if snapshots_before is not None:
            new = sorted(snapshot_files(snapshot_product) - snapshots_before)
            cl.check("no snapshot written by the read-only scripts", not new, ", ".join(os.path.basename(n) for n in new))
        elif snapshot_product:
            cl.skip("no snapshot written by the read-only scripts", f"%LOCALAPPDATA%/{snapshot_product} does not exist yet")
    finally:
        server.close()
        shutil.rmtree(registry, ignore_errors=True)


def main():
    utf8_console()
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--host", choices=sorted(HOSTS))
    parser.add_argument("--exe", help="server exe (default: the host's Debug build)")
    parser.add_argument("--out", help="folder for the JSON result")
    parser.add_argument("--list", action="store_true", help="print the host table and the server exe each host would use")
    args = parser.parse_args()

    if args.list:
        for host, (folder, prefix, version_key, version, context_tool, product) in sorted(HOSTS.items()):
            print(f"{host:8} {prefix}Bridge__{version_key}={version:<5} {context_tool:22} snapshots={product or '-':10} exe={server_exe(folder) or 'NOT BUILT'}")
        return 0
    if not args.host:
        parser.error("--host is required (or --list)")

    exe = args.exe or server_exe(HOSTS[args.host][0])
    if not exe or not os.path.isfile(exe):
        print(f"server exe not found for {args.host}: build {HOSTS[args.host][0]}.Mcp.Server first", file=sys.stderr)
        return 2

    cl = Checklist(out_dir=args.out)
    started = time.strftime("%Y-%m-%d %H:%M:%S")
    run(args.host, exe, cl)
    return cl.finish(f"live-verify-quality-{args.host}", host=args.host, exe=exe, started=started)


if __name__ == "__main__":
    sys.exit(main())
