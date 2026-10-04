"""Throw-away: tools/list of every HP MCP server exe on an isolated registry root, with hashes.

Usage: python snapshot-tools-list-baseline.py published|debug
  published -> <Host>/output/<Host>.Mcp.Server/<Host>.Mcp.Server.exe (fallback: Debug bin)
  debug     -> <Host>/<Host>.Mcp.Server/bin/Debug/<tfm>/<Host>.Mcp.Server.exe
Writes reports/tools-list-baseline[-debug]/<host>.json (mcp-call.py stdout verbatim) + summary.tsv.
"""
import glob, hashlib, json, os, shutil, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
REPORTS = os.path.dirname(os.path.abspath(__file__))
MCP_CALL = os.path.join(ROOT, "McpShared", "tools", "mcp-call.py")
HOSTS = [("HPRebar", "HPREBAR_MCP_"), ("HPAutoCad", "HPAUTOCAD_MCP_"), ("HPCivil3d", "HPCIVIL3D_MCP_"),
         ("HPNavis", "HPNAVIS_MCP_"), ("HPEtabs", "HPETABS_MCP_"), ("HPSap2000", "HPSAP2000_MCP_"),
         ("HPRobot", "HPROBOT_MCP_"), ("HPExcel", "HPEXCEL_MCP_"), ("HPPowerBi", "HPPOWERBI_MCP_"),
         ("HPTekla", "HPTEKLA_MCP_")]


def debug_exe(host):
    hits = glob.glob(os.path.join(ROOT, host, f"{host}.Mcp.Server", "bin", "Debug", "*", f"{host}.Mcp.Server.exe"))
    return sorted(hits)[0] if hits else None


def main():
    mode = sys.argv[1]
    out_dir = os.path.join(REPORTS, "tools-list-baseline" + ("" if mode == "published" else "-debug"))
    os.makedirs(out_dir, exist_ok=True)
    tmp_root = os.path.join(REPORTS, "tmp-registry")
    rows = []
    for host, prefix in HOSTS:
        exe = os.path.join(ROOT, host, "output", f"{host}.Mcp.Server", f"{host}.Mcp.Server.exe") if mode == "published" else None
        source = "published"
        if not exe or not os.path.isfile(exe):
            exe, source = debug_exe(host), "debug-bin"
        if not exe:
            rows.append((host, "-", "no exe", "", "", "")); continue
        iso = os.path.join(tmp_root, host.lower())
        os.makedirs(os.path.join(iso, "tools-library"), exist_ok=True)
        cmd = [sys.executable, MCP_CALL, exe, "tools/list",
               "--env", f"{prefix}Registry__LibraryPath={os.path.join(iso, 'tools-library')}",
               "--env", f"{prefix}Registry__DbPath={os.path.join(iso, 'registry.db')}"]
        p = subprocess.run(cmd, capture_output=True, timeout=300, env=dict(os.environ, PYTHONIOENCODING="utf-8"))
        raw = p.stdout
        name = host.replace("HP", "", 1).lower()
        with open(os.path.join(out_dir, f"{name}.json"), "wb") as f:
            f.write(raw)
        try:
            tools = json.loads(raw.decode("utf-8"))["result"]["tools"]
            canon = json.dumps(tools, sort_keys=False, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
            rows.append((host, os.path.relpath(exe, ROOT), source, str(len(tools)),
                         hashlib.sha256(canon).hexdigest(), hashlib.sha256(raw).hexdigest()))
        except Exception as e:  # record, never hide
            rows.append((host, os.path.relpath(exe, ROOT), source, f"ERROR rc={p.returncode} {e}",
                         "", p.stderr.decode("utf-8", "replace")[-300:].replace("\n", " ")))
    with open(os.path.join(out_dir, "summary.tsv"), "w", encoding="utf-8") as f:
        for r in rows:
            f.write("\t".join(r) + "\n")
    for r in rows:
        print("\t".join(r))
    shutil.rmtree(tmp_root, ignore_errors=True)


if __name__ == "__main__":
    main()
