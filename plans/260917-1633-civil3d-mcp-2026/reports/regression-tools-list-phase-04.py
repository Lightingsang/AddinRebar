"""Regression gate for the Civil 3D plan: tools/list of the freshly built Revit, AutoCAD, Navisworks and ETABS MCP exes
(Release) against this plan's phase-0 "after" snapshots. Every tool that existed at phase 0 must be byte-identical (name, description, schema,
annotations); tools added since by other plans are listed, never compared. Run from the repo root after building the four
server projects in Release:
    python plans/260917-1633-civil3d-mcp-2026/reports/regression-tools-list-phase-04.py
"""
import json, os, subprocess, sys, tempfile

ROOT = os.getcwd()
REPORTS = os.path.dirname(os.path.abspath(__file__))
PY = os.path.join(ROOT, "McpShared", "tools", "mcp-call.py")
HOSTS = [
    ("revit", os.path.join(ROOT, "HPRebar", "HPRebar.Mcp.Server", "bin", "Release", "net10.0", "HPRebar.Mcp.Server.exe"), "HPREBAR_MCP_"),
    ("autocad", os.path.join(ROOT, "HPAutoCad", "HPAutoCad.Mcp.Server", "bin", "Release", "net10.0", "HPAutoCad.Mcp.Server.exe"), "HPAUTOCAD_MCP_"),
    ("navis", os.path.join(ROOT, "HPNavis", "HPNavis.Mcp.Server", "bin", "Release", "net10.0", "HPNavis.Mcp.Server.exe"), "HPNAVIS_MCP_"),
    ("etabs", os.path.join(ROOT, "HPEtabs", "HPEtabs.Mcp.Server", "bin", "Release", "net10.0", "HPEtabs.Mcp.Server.exe"), "HPETABS_MCP_"),
]

# Phase-0 tools rewritten since by their own plan (not by the engine): none so far for this plan.
KNOWN_CHANGED = {}

ok_all = True
for name, exe, prefix in HOSTS:
    iso = os.path.join(tempfile.gettempdir(), "hp-mcp-snapshot-civil3d-phase4", name)
    lib = os.path.join(iso, "tools-library"); db = os.path.join(iso, "registry.db")
    os.makedirs(lib, exist_ok=True)
    out = os.path.join(REPORTS, f"phase-04-tools-list-{name}.json")
    subprocess.run([sys.executable, PY, exe, "tools/list", "--env", f"{prefix}Registry__LibraryPath={lib}", "--env", f"{prefix}Registry__DbPath={db}", "--out", out],
                   check=True, capture_output=True)
    with open(out, encoding="utf-8") as f:
        raw = json.load(f)
    now = sorted(raw["result"]["tools"], key=lambda t: t["name"])
    with open(out, "w", encoding="utf-8") as f:
        json.dump(now, f, indent=2, ensure_ascii=False)
    with open(os.path.join(REPORTS, f"phase-00-tools-list-after-{name}.json"), encoding="utf-8") as f:
        before = {t["name"]: t for t in json.load(f)}
    now_by = {t["name"]: t for t in now}
    common = sorted(set(before) & set(now_by))
    missing = sorted(set(before) - set(now_by))
    added = sorted(set(now_by) - set(before))
    differ = [n for n in common if json.dumps(before[n], sort_keys=True, ensure_ascii=False) != json.dumps(now_by[n], sort_keys=True, ensure_ascii=False)]
    unexplained = [n for n in differ if n not in KNOWN_CHANGED.get(name, set())]
    good = not missing and not unexplained
    ok_all &= good
    print(f"{name:8} phase0={len(before):3} now={len(now_by):3} common={len(common):3} identical={len(common) - len(differ):3} changed-by-own-plan={sorted(set(differ) - set(unexplained))} unexplained={unexplained} missing={missing} added={len(added)} {'OK' if good else 'FAIL'}")
    if added:
        print(f"         added since phase 0 (other plans): {added}")

print("RESULT:", "byte-identical for every phase-0 tool" if ok_all else "DIFFERENCES FOUND")
sys.exit(0 if ok_all else 1)
