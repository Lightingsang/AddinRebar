"""Regenerate references/tool-catalog.md from the Civil 3D MCP server's own tools/list.

    python .claude/skills/hp-mcp-civil3d/scripts/generate-tool-catalog.py [--exe <HPCivil3d.Mcp.Server.exe>]

Runs the server once on an isolated registry root (the user's %AppData%\\HPCivil3d\\McpServer is never touched), asks
tools/list, and writes one section per category with every argument's type / default / description. Read-only for
Civil 3D: tools/list needs no drawing and no bridge. The exe path must be absolute on Windows (CreateProcess).
"""
import argparse, io, json, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
CORE = ["get_civil3d_context", "execute_civil3d_code", "inspect_type", "cancel_execution"]
REGISTRY = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"]
ORDER = ["Core", "Registry", "Document", "Alignment", "Profile", "Surface", "Corridor", "Pipe", "Parcel", "Point", "Data", "Generic", "Other"]

HEADER = """# HPCivil3d MCP — tool catalog ({total} tools)

Generated from `tools/list` of `HPCivil3d.Mcp.Server.exe` on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-civil3d__<name>` in Claude Code. `REQ` = required. **Units:** plan x/y and lengths cross the tool boundary in **millimetres**; stations, elevations and areas stay in the **Civil drawing unit** (Meters or Feet — every envelope says `drawingUnit`); handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

The engine's registry tools (`inspect_type`, `search_tools`, `propose_tool`, `test_tool`) carry host-neutral descriptions that quote Revit/AutoCAD examples. For Civil 3D read them as: `inspect_type.typeName` = an AutoCAD or Civil type (`Alignment`, `Autodesk.Civil.DatabaseServices.TinSurface`, `CogoPoint`, `Corridor`, `Pipe`, `Parcel`, `Profile`); `category` ∈ Document | Alignment | Profile | Surface | Corridor | Pipe | Parcel | Point | Data | Generic; `transaction` `manual` ≡ `auto`; the CLI is `HPCivil3d.Mcp.Server.exe registry …`.

Result shape of every run (`execute_civil3d_code`, `run_tool`, seeds): `{{isError, value, valueType, message, logs[], diagnostics[{{id, line, column, message}}], changed{{added, modified, deleted}}, rolledBack, timedOut, durationMs, truncated, runId, hint}}` — `changed` is counted from the drawing's HANDSEED and modified objects and is reported even when `dryRun` rolled everything back.
"""


def tools_list(exe):
    registry = tempfile.mkdtemp(prefix="hp-civil3d-catalog-")
    env = dict(os.environ, HPCIVIL3D_MCP_Registry__LibraryPath=os.path.join(registry, "tools-library"), HPCIVIL3D_MCP_Registry__DbPath=os.path.join(registry, "registry.db"), HPCIVIL3D_MCP_Bridge__HostVersion="2026")
    out = os.path.join(registry, "tools.json")
    subprocess.run([sys.executable, os.path.join(ROOT, "McpShared", "tools", "mcp-call.py"), os.path.abspath(exe), "tools/list", "--out", out], check=True, env=env, capture_output=True)
    return json.load(io.open(out, encoding="utf-8"))["result"]["tools"]


def category(tool):
    d = tool.get("description", "")
    i = d.rfind("[Registry tool v")
    if i < 0:
        return None, d
    parts = d[i:].strip("[]").split(",")
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


def render(by, total):
    lines = [HEADER.format(total=total)]
    for cat in ORDER + sorted(k for k in by if k not in ORDER):
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
            required = set(schema.get("required") or [])
            if props:
                lines.append("| arg | type | default | description |")
                lines.append("|---|---|---|---|")
                for name, p in props.items():
                    typ = p.get("type", "")
                    if typ == "array" and isinstance(p.get("items"), dict):
                        items = p["items"]
                        inner = items.get("type", "object")
                        if inner == "object" and items.get("properties"):
                            inner = "{" + ", ".join(f"{k}{'' if k in set(items.get('required') or []) else '?'}" for k in items["properties"]) + "}"
                        typ = f"array<{inner}>"
                    if "enum" in p:
                        typ += " " + "|".join(str(e) for e in p["enum"])
                    default = p.get("default", "")
                    default = json.dumps(default, ensure_ascii=False) if default not in ("", None) else ""
                    rng = ""
                    if "minimum" in p or "maximum" in p:
                        rng = f" ({p.get('minimum', '…')}–{p.get('maximum', '…')})"
                    d = (p.get("description") or "").replace("|", "\\|").replace("\n", " ")
                    lines.append(f"| `{name}`{' REQ' if name in required else ''} | {typ}{rng} | {default} | {d} |")
                lines.append("")
            else:
                lines.append("_No arguments._")
                lines.append("")
    return "\n".join(lines).rstrip("\n") + "\n"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", default=os.path.join(ROOT, "HPCivil3d", "output", "HPCivil3d.Mcp.Server", "HPCivil3d.Mcp.Server.exe"))
    ap.add_argument("--out", default=os.path.join(HERE, "..", "references", "tool-catalog.md"))
    a = ap.parse_args()
    tools = tools_list(a.exe)
    io.open(a.out, "w", encoding="utf-8", newline="\n").write(render(group(tools), len(tools)))
    print(f"{len(tools)} tools -> {os.path.abspath(a.out)}")


if __name__ == "__main__":
    main()
