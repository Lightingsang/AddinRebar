"""Regenerate the references/tool-catalog-*.md files from the Revit MCP server's own tools/list.

    python .claude/skills/hp-mcp-revit/scripts/generate-tool-catalog.py [--exe <HPRebar.Mcp.Server.exe>]

Runs the server once on an isolated registry root (the user's %AppData%/HPRebar/McpServer registry is never touched),
asks tools/list, and writes one section per category with every argument's type / default / description. Read-only for
Revit: tools/list needs no document and no bridge — the pipe is never opened.
"""
import argparse, io, json, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
CORE = ["get_revit_context", "execute_revit_code", "inspect_type", "cancel_execution"]
REGISTRY = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"]
# One reference file per group so each stays under the 300-line budget a skill reference gets.
GROUPS = {
    "tool-catalog-core-registry.md": (["Core", "Registry"], "core + registry (execute / context / inspect / cancel, search / get / run / get_run / propose / test / publish / manage)"),
    "tool-catalog-seeds-data-generic-view.md": (["Data", "Generic", "View"], "Data / Generic / View seeds (element filter, statistics, material quantities, family types, selection, line-based creation, delete, view info / elements, colour, hide / isolate)"),
    "tool-catalog-seeds-architecture-structure-annotation.md": (["Architecture", "Structure", "MEP", "Annotation"], "Architecture / Structure / Annotation seeds (levels, grids, doors / windows / columns, floors / roofs, rooms, room export, beam systems, tags, dimensions)"),
}


def tools_list(exe):
    registry = tempfile.mkdtemp(prefix="hp-revit-catalog-")
    env = dict(os.environ,
               HPREBAR_MCP_Registry__LibraryPath=os.path.join(registry, "tools-library"),
               HPREBAR_MCP_Registry__DbPath=os.path.join(registry, "registry.db"),
               HPREBAR_MCP_Bridge__RevitVersion="2026")
    out = os.path.join(registry, "tools.json")
    subprocess.run([sys.executable, os.path.join(ROOT, "McpShared", "tools", "mcp-call.py"), exe, "tools/list", "--out", out], check=True, env=env, capture_output=True)
    with io.open(out, encoding="utf-8") as f:
        return json.load(f)["result"]["tools"]


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


def render_props(lines, props, required, indent=""):
    lines.append(f"{indent}| arg | type | default | description |")
    lines.append(f"{indent}|---|---|---|---|")
    nested = []
    for name, p in props.items():
        typ = p.get("type", "")
        items = p.get("items") if isinstance(p.get("items"), dict) else None
        if typ == "array" and items:
            typ = f"array<{items.get('type', 'object')}>"
            if items.get("properties"):
                nested.append((f"{name}[]", items))
        if "enum" in p:
            typ += " " + " \\| ".join(str(e) for e in p["enum"])  # escaped: the type sits in a markdown table cell
        if typ == "object" and p.get("properties"):
            nested.append((name, p))
        default = p.get("default", "")
        default = json.dumps(default, ensure_ascii=False) if default not in ("", None) else ""
        d = (p.get("description") or "").replace("|", "\\|").replace("\n", " ")
        lines.append(f"{indent}| `{name}`{' REQ' if name in required else ''} | {typ} | {default} | {d} |")
    lines.append("")
    # Seeds that take a `data` / `dimensions` array carry the real contract in the item schema — print it too.
    for name, schema in nested:
        lines.append(f"{indent}`{name}` items:")
        lines.append("")
        render_props(lines, schema.get("properties") or {}, set(schema.get("required") or []), indent)


def render(by, cats, what, total):
    lines = [f"# HPRebar Revit MCP — tool catalog: {what}", "",
             f"Generated from `tools/list` of `HPRebar.Mcp.Server.exe` ({total} tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools (e.g. `set_mark_from_comments`). Names are `mcp__hprebar-revit__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (points as `{{x, y, z}}` objects in mm) even though the Revit API works in feet; ids are Revit `ElementId` numbers. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.", ""]
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
            lines.append((f"*{flags}.* " if flags else "") + desc)
            lines.append("")
            schema = t.get("inputSchema") or {}
            props = schema.get("properties") or {}
            if props:
                render_props(lines, props, set(schema.get("required") or []))
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", default=os.path.join(ROOT, "HPRebar", "output", "HPRebar.Mcp.Server", "HPRebar.Mcp.Server.exe"))
    ap.add_argument("--out", default=os.path.join(HERE, "..", "references", "tool-catalog.md"), help="any path inside the references folder; the group files are written beside it")
    a = ap.parse_args()
    tools = tools_list(os.path.abspath(a.exe))
    by = group(tools)
    out_dir = os.path.dirname(os.path.abspath(a.out))
    covered = set()
    for file, (cats, what) in GROUPS.items():
        with io.open(os.path.join(out_dir, file), "w", encoding="utf-8", newline="\n") as f:
            f.write(render(by, cats, what, len(tools)))
        covered.update(cats)
    missing = sorted(k for k in by if k not in covered)
    if missing:
        raise SystemExit(f"categories without a file: {missing} — add them to GROUPS")
    print(f"{len(tools)} tools -> {len(GROUPS)} files in {out_dir}")


if __name__ == "__main__":
    main()
