"""Regenerate references/tool-catalog.md from the AutoCAD MCP server's own tools/list.

    python .claude/skills/hp-mcp-autocad/scripts/generate-tool-catalog.py [--exe <HPAutoCad.Mcp.Server.exe>]

Runs the server once on an isolated registry root (the user's %AppData% registry is never touched), asks tools/list,
and writes one section per category with every argument's type / default / description. Read-only for AutoCAD:
tools/list needs no drawing and no bridge.
"""
import argparse, io, json, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
CORE = ["get_autocad_context", "execute_autocad_code", "inspect_type", "cancel_execution"]
REGISTRY = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"]
ORDER = ["Drawing", "Data", "Layer", "Block", "Annotation", "Layout", "Generic", "Geometry", "Audit", "Aec", "Structural", "Architecture", "MEP", "Coordination", "ChangeSet"]
# One reference file per group so each stays under the 300-line budget a skill reference gets.
GROUPS = {
    "tool-catalog-core-registry.md": (["Core", "Registry"], "core + registry (execute / context / inspect / cancel, search / get / run / get_run / propose / test / publish / manage)"),
    "tool-catalog-drawing-data.md": (["Drawing", "Data", "Layer", "Layout", "Generic"], "drawing + data (context, entity query, spatial query, batch create / update, hatches, xrefs, layers, layouts, selection)"),
    "tool-catalog-blocks-annotations-audit.md": (["Block", "Annotation", "Geometry", "Audit"], "blocks, annotations, measure, geometry issues, CAD standards, audit, issue markup"),
    "tool-catalog-aec-structural.md": (["Aec", "Structural"], "AEC classification / relationships and the structural tools"),
    "tool-catalog-arch-mep-coordination-changesets.md": (["Architecture", "MEP", "Coordination", "ChangeSet"], "architecture, MEP, coordination and change sets"),
}


def tools_list(exe):
    registry = tempfile.mkdtemp(prefix="hp-autocad-catalog-")
    env = dict(os.environ, HPAUTOCAD_MCP_Registry__LibraryPath=os.path.join(registry, "tools-library"), HPAUTOCAD_MCP_Registry__DbPath=os.path.join(registry, "registry.db"), HPAUTOCAD_MCP_Bridge__HostVersion="2026")
    out = os.path.join(registry, "tools.json")
    subprocess.run([sys.executable, os.path.join(ROOT, "McpShared", "tools", "mcp-call.py"), exe, "tools/list", "--out", out], check=True, env=env, capture_output=True)
    return json.load(io.open(out, encoding="utf-8"))["result"]["tools"]


def category(tool):
    d = tool.get("description", "")
    i = d.rfind("[Registry tool v")
    if i < 0:
        return None, d
    tag = d[i:]
    parts = tag.strip("[]").split(",")
    return parts[1].strip() if len(parts) > 1 else "?", d[:i].rstrip()


def group(tools):
    by = {}
    for t in tools:
        cat, desc = category(t)
        if t["name"] in CORE:
            cat = "Core"
        elif t["name"] in REGISTRY:
            cat = "Registry"
        by.setdefault(cat or "Other", []).append((t, desc))
    return by


def render(by, cats, what, total):
    lines = [f"# HPAutoCad MCP — tool catalog: {what}", "", f"Generated from `tools/list` of `HPAutoCad.Mcp.Server.exe` ({total} tools in all) on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-autocad__<name>` in Claude Code. `REQ` = required; every length is millimetres, points are `{{x, y}}` objects in mm; handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.", ""]
    for cat in cats:
        if cat not in by:
            continue
        lines.append(f"## {cat}")
        lines.append("")
        for t, desc in sorted(by[cat], key=lambda x: x[0]["name"]):
            ann = t.get("annotations") or {}
            flags = ", ".join(k for k in ("readOnlyHint", "destructiveHint", "idempotentHint") if ann.get(k))
            lines.append(f"### `{t['name']}` — {ann.get('title') or t['name']}")
            lines.append("")
            if flags:
                lines.append(f"*{flags}.* " + desc)
            else:
                lines.append(desc)
            lines.append("")
            schema = t.get("inputSchema") or {}
            props = schema.get("properties") or {}
            required = set(schema.get("required") or [])
            if props:
                lines.append("| arg | type | default | description |")
                lines.append("|---|---|---|---|")
                for name, p in props.items():
                    typ = p.get("type", "")
                    if typ == "array" and isinstance(p.get("items"), dict):
                        typ = f"array<{p['items'].get('type', 'object')}>"
                    if "enum" in p:
                        typ += " " + "|".join(str(e) for e in p["enum"])
                    default = p.get("default", "")
                    default = json.dumps(default, ensure_ascii=False) if default not in ("", None) else ""
                    d = (p.get("description") or "").replace("|", "\\|").replace("\n", " ")
                    lines.append(f"| `{name}`{' REQ' if name in required else ''} | {typ} | {default} | {d} |")
                lines.append("")
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", default=os.path.join(ROOT, "HPAutoCad", "output", "HPAutoCad.Mcp.Server", "HPAutoCad.Mcp.Server.exe"))
    ap.add_argument("--out", default=os.path.join(HERE, "..", "references", "tool-catalog.md"), help="any path inside the references folder; the group files are written beside it")
    a = ap.parse_args()
    tools = tools_list(a.exe)
    by = group(tools)
    out_dir = os.path.dirname(os.path.abspath(a.out))
    covered = set()
    for file, (cats, what) in GROUPS.items():
        io.open(os.path.join(out_dir, file), "w", encoding="utf-8", newline="\n").write(render(by, cats, what, len(tools)))
        covered.update(cats)
    missing = sorted(k for k in by if k not in covered)
    if missing:
        raise SystemExit(f"categories without a file: {missing} — add them to GROUPS")
    print(f"{len(tools)} tools -> {len(GROUPS)} files in {out_dir}")


if __name__ == "__main__":
    main()
