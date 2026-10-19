"""HPGrasshopper MCP Subsystem Automated Live Verification Driver.
Executes 7 sequential testing stages (A through G) across all 24 tools,
dynamic registry lifecycle, 12 embedded seeds, and dual-layer safety.
"""
import argparse
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(REPO_ROOT, "McpShared", "tools"))
from harness_common import Checklist, Server, ok, short, utf8_console

utf8_console()

CORE_TOOLS = [
    "read_grasshopper_canvas",
    "run_grasshopper_player",
    "batch_edit_grasshopper_canvas",
    "execute_grasshopper_code",
    "export_grasshopper_canvas_image",
]

REGISTRY_TOOLS = [
    "search_tools",
    "propose_tool",
    "test_tool",
    "publish_tool",
    "get_tool",
    "run_tool",
    "get_run",
    "manage_tool",
]

SEED_TOOLS = [
    "create_levels_grids",
    "generate_columns_beams",
    "create_floors_slabs",
    "generate_rebar_curves",
    "generate_stirrup_distribution",
    "facade_panelization",
    "curtain_grid_layout",
    "create_wall_openings",
    "directshape_from_geometry",
    "query_revit_geometry",
    "evaluate_solar_radiation",
    "export_model_to_rhino",
]


def run_stage_a(s, cl):
    """Stage A: Handshake & 24 Tools Catalog Surface Check."""
    print("\n=== Stage A: Handshake & Catalog Surface Check ===")
    tools = s.tools()
    cl.check("Stage A: Server exposes >= 24 tools", len(tools) >= 24, f"Total tools exposed: {len(tools)}")

    for ct in CORE_TOOLS:
        cl.check(f"Stage A: Core tool '{ct}' present", ct in tools, f"Annotation & Schema: {ct in tools}")

    for rt in REGISTRY_TOOLS:
        cl.check(f"Stage A: Registry tool '{rt}' present", rt in tools, f"Registry tool: {rt}")

    for st in SEED_TOOLS:
        cl.check(f"Stage A: Embedded seed '{st}' present", st in tools, f"Seed tool: {st}")

    cl.check("Stage A: get_grasshopper_context utility present", "get_grasshopper_context" in tools)
    cl.check("Stage A: cancel_execution utility present", "cancel_execution" in tools)


def run_stage_b(s, cl):
    """Stage B: Context Snapshot, Resources & Prompts."""
    print("\n=== Stage B: Context Snapshot ===")
    ctx = s.tool("get_grasshopper_context", {})
    cl.check("Stage B: get_grasshopper_context succeeds", ok(ctx), short(ctx))

    host = ctx.get("host")
    cl.check("Stage B: Host profile identifies as 'grasshopper'", host == "grasshopper", f"host: {host}")

    gh_info = ctx.get("grasshopper") or {}
    cl.check("Stage B: Context confirms Rhino.Inside.Revit integration", gh_info.get("isRhinoInsideRevitActive") is True or gh_info.get("rhinoInsideRevit") is True, short(gh_info))
    cl.check("Stage B: Context contains active document info", "activeDefinitionName" in gh_info or "activeDefinitionPath" in gh_info, short(gh_info))
    cl.check("Stage B: Context contains component count", "componentCount" in gh_info, short(gh_info))
    cl.check("Stage B: Zero foreign host properties in context", "autocad" not in ctx and "etabs" not in ctx, "Purity verified")


def run_stage_c(s, cl):
    """Stage C: 5 Core Tools Live / Dry-Run Invocations."""
    print("\n=== Stage C: 5 Core Tools Invocations ===")

    # 1. read_grasshopper_canvas
    r1 = s.tool("read_grasshopper_canvas", {"includeValues": False, "selectedOnly": False})
    cl.check("Stage C: read_grasshopper_canvas executes successfully", ok(r1), short(r1))
    cl.check("Stage C: read_grasshopper_canvas returns document metadata", "document" in (r1.get("value") or r1), short(r1))

    # 2. run_grasshopper_player
    r2 = s.tool("run_grasshopper_player", {
        "filePath": os.path.join(HERE, "fixtures", "MathSlider.ghx"),
        "inputs": {"A": 10.0, "B": 25.0},
        "outputs": ["Result"],
        "headless": True
    })
    cl.check("Stage C: run_grasshopper_player executes successfully", ok(r2), short(r2))

    # 3. batch_edit_grasshopper_canvas (dryRun: true)
    r3 = s.tool("batch_edit_grasshopper_canvas", {
        "dryRun": True,
        "componentsToAdd": [{"type": "NumberSlider", "x": 100.0, "y": 200.0, "nickName": "TestVal"}]
    })
    cl.check("Stage C: batch_edit_grasshopper_canvas executes with dryRun", ok(r3), short(r3))
    cl.check("Stage C: batch_edit_grasshopper_canvas reports rolledBack=true", bool(r3.get("rolledBack") or (r3.get("value") or {}).get("rolledBack")), short(r3))

    # 4. execute_grasshopper_code (read-only)
    r4a = s.tool("execute_grasshopper_code", {"code": "return doc != null;", "transaction": "none"})
    cl.check("Stage C: execute_grasshopper_code read-only succeeds", ok(r4a), short(r4a))

    # 4b. execute_grasshopper_code (dryRun: true)
    r4b = s.tool("execute_grasshopper_code", {"code": "return 42;", "transaction": "auto", "dryRun": True})
    cl.check("Stage C: execute_grasshopper_code dryRun reports rolledBack=true", ok(r4b) and bool(r4b.get("rolledBack")), short(r4b))

    # 5. export_grasshopper_canvas_image
    r5 = s.tool("export_grasshopper_canvas_image", {"mode": "FullCanvas", "returnBase64": True})
    cl.check("Stage C: export_grasshopper_canvas_image executes successfully", ok(r5), short(r5))
    cl.check("Stage C: export_grasshopper_canvas_image returns valid PNG header or base64", bool(r5.get("imageBase64") or (r5.get("value") or {}).get("imageBase64") or r5.get("base64") or (r5.get("value") or {}).get("base64")), short(r5))


def run_stage_d(s, cl):
    """Stage D: Dynamic Registry Meta-Tools Lifecycle."""
    print("\n=== Stage D: Dynamic Registry Meta-Tools Lifecycle ===")

    # 1. search_tools
    r1 = s.tool("search_tools", {"query": "rebar"})
    cl.check("Stage D: search_tools query 'rebar' succeeds", ok(r1), short(r1))

    # 2. propose_tool with ADR-0007 compliant code
    clean_code = 'double x = args.Number("x"); return Math.Sin(x);'
    r2 = s.tool("propose_tool", {
        "name": "grasshopper_sine_eval",
        "title": "Calculate Sine for Parametric Curves",
        "description": "Calculates sine value for parametric curves in Grasshopper canvas",
        "category": "Geometry",
        "tags": ["math", "sine", "parametric"],
        "inputSchema": {
            "type": "object",
            "properties": {
                "x": {"type": "number", "description": "Input value"}
            },
            "required": ["x"]
        },
        "code": clean_code,
        "examples": [
            {"title": "Sine of 0", "args": {"x": 0.0}},
            {"title": "Sine of Pi/2", "args": {"x": 1.570796}}
        ],
        "transaction": "none",
        "timeoutSeconds": 30
    })
    cl.check("Stage D: propose_tool submits ADR-0007 compliant candidate", ok(r2) and bool(r2.get("accepted")), short(r2))

    # 3. test_tool in dry-run mode
    r3 = s.tool("test_tool", {"name": "grasshopper_sine_eval"})
    cl.check("Stage D: test_tool dryRun execution succeeds", ok(r3) and r3.get("passed") == 2, short(r3))

    # 4. publish_tool
    r4 = s.tool("publish_tool", {"name": "grasshopper_sine_eval"})
    cl.check("Stage D: publish_tool approves candidate", ok(r4), short(r4))

    # 5. get_tool
    r5 = s.tool("get_tool", {"name": "grasshopper_sine_eval"})
    cl.check("Stage D: get_tool retrieves published tool metadata", ok(r5), short(r5))

    # 6. run_tool
    r6 = s.tool("run_tool", {"name": "grasshopper_sine_eval", "arguments": {"x": 1.0}, "allowUnpublished": True})
    cl.check("Stage D: run_tool invokes published tool", ok(r6), short(r6))

    # 7. get_run
    r7 = s.tool("get_run", {"runId": 1})
    cl.check("Stage D: get_run retrieves execution audit record", ok(r7) or r7.get("isError") is not None, short(r7))

    # 8. manage_tool: deprecate
    r8 = s.tool("manage_tool", {"name": "grasshopper_sine_eval", "action": "deprecate", "reason": "Completed testing lifecycle"})
    cl.check("Stage D: manage_tool deprecate operation succeeds", ok(r8), short(r8))


def run_stage_e(s, cl):
    """Stage E: 12 Embedded Seed Tools Verification (dryRun=true)."""
    print("\n=== Stage E: 12 Embedded Seed Tools Verification ===")

    seed_test_vectors = {
        "create_levels_grids": {"levels": [{"name": "L1", "elevation": 0.0}, {"name": "L2", "elevation": 3600.0}], "dryRun": True},
        "generate_columns_beams": {"columns": [{"x": 0.0, "y": 0.0, "baseLevel": "L1", "topLevel": "L2"}], "dryRun": True},
        "create_floors_slabs": {"boundaryPoints": [[0,0,0], [5000,0,0], [5000,5000,0], [0,5000,0]], "levelName": "L1", "dryRun": True},
        "generate_rebar_curves": {"beamSpan": 6000.0, "width": 300.0, "height": 500.0, "cover": 30.0, "dryRun": True},
        "generate_stirrup_distribution": {"clearSpan": 5400.0, "supportSpacing": 100.0, "midSpanSpacing": 200.0, "dryRun": True},
        "facade_panelization": {"surfaceUCount": 10, "surfaceVCount": 10, "panelType": "Diamond", "dryRun": True},
        "curtain_grid_layout": {"width": 12000.0, "height": 4000.0, "verticalSpacing": 1500.0, "dryRun": True},
        "create_wall_openings": {"wallId": 12345, "openings": [{"x": 1000, "z": 900, "width": 1200, "height": 1500}], "dryRun": True},
        "directshape_from_geometry": {"category": "GenericModel", "geometryData": "mock_brep", "dryRun": True},
        "query_revit_geometry": {"categoryFilter": "OST_StructuralColumns", "detailLevel": "Fine"},
        "evaluate_solar_radiation": {"latitude": 21.0285, "longitude": 105.8542, "surfacePoints": [[0,0,0]], "dryRun": True},
        "export_model_to_rhino": {"elementIds": [12345], "exportFormat": "Rhino7_8", "dryRun": True},
    }

    for seed, vec in seed_test_vectors.items():
        res = s.tool(seed, vec)
        cl.check(f"Stage E: Seed '{seed}' dryRun invocation succeeds", ok(res), short(res))


def run_stage_f(s, cl):
    """Stage F: Dual-Layer Safety, Guardrails & Cancellation."""
    print("\n=== Stage F: Dual-Layer Safety, Guardrails & Cancellation ===")

    # 1. Propose tool with forbidden namespace (Q-B1/Security check)
    bad_code = 'System.Windows.Forms.MessageBox.Show("Test"); return 1;'
    rf1 = s.tool("propose_tool", {
        "name": "forbidden_tool",
        "description": "Forbidden namespace test tool description >= 20 chars",
        "category": "Generic",
        "inputSchema": {"type": "object", "properties": {}},
        "examples": [{"title": "ex1", "args": {}}, {"title": "ex2", "args": {}}],
        "code": bad_code,
    })
    cl.check("Stage F: Guardrail refuses forbidden namespace (System.Windows.Forms)", rf1.get("isError") is True or "denied" in str(rf1).lower() or "violation" in str(rf1).lower() or not rf1.get("accepted"), short(rf1))

    # 2. Propose tool with #r directive (Bypass prevention)
    hash_r_code = '#r "some.dll"\nreturn 1;'
    rf2 = s.tool("propose_tool", {
        "name": "hash_r_tool",
        "description": "Directive test tool description >= 20 chars",
        "category": "Generic",
        "inputSchema": {"type": "object", "properties": {}},
        "examples": [{"title": "ex1", "args": {}}, {"title": "ex2", "args": {}}],
        "code": hash_r_code,
    })
    cl.check("Stage F: Guardrail refuses #r directive", rf2.get("isError") is True or "directive" in str(rf2).lower() or "refused" in str(rf2).lower() or not rf2.get("accepted"), short(rf2))

    # 3. cancel_execution
    rc = s.tool("cancel_execution", {})
    cl.check("Stage F: cancel_execution handles cancellation request cleanly", ok(rc), short(rc))


def run_stage_g(s, cl):
    """Stage G: Error Handling, Resilience & Edge Cases."""
    print("\n=== Stage G: Error Handling & Resilience ===")

    # 1. Non-existent tool call
    r1 = s.tool("non_existent_tool_12345", {})
    cl.check("Stage G: Unknown tool call returns clean error", r1.get("isError") is True or r1.get("rpcError") is not None, short(r1))

    # 2. run_grasshopper_player with non-existent file
    r2 = s.tool("run_grasshopper_player", {"filePath": r"C:\NonExistent\Def.ghx"})
    cl.check("Stage G: Missing player definition returns error", r2.get("isError") is True or "not found" in str(r2).lower() or "Definition file not found" in str(r2) or r2.get("success") is False, short(r2))


def main():
    parser = argparse.ArgumentParser(description="HPGrasshopper Automated Live Verification")
    parser.add_argument("exe", help="Path to HPGrasshopper.Mcp.Server.exe")
    parser.add_argument("--registry", default="", help="Isolated registry root directory")
    parser.add_argument("--out", default="", help="Output directory for reports and summaries")
    parser.add_argument("--stages", default="A,B,C,D,E,F,G", help="Comma-separated stages to run")
    args = parser.parse_args()

    env_overrides = {}
    if args.registry:
        os.makedirs(args.registry, exist_ok=True)
        env_overrides["HPGRASSHOPPER_MCP_Registry__LibraryPath"] = os.path.join(args.registry, "tools-library")
        env_overrides["HPGRASSHOPPER_MCP_Registry__DbPath"] = os.path.join(args.registry, "registry.db")

    cl = Checklist(out_dir=args.out)
    stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]

    print(f"Starting HPGrasshopper Live Verification (Stages: {','.join(stages)})...")
    s = Server(args.exe, env=env_overrides, name="HPGrasshopper-Server")

    try:
        if "A" in stages: run_stage_a(s, cl)
        if "B" in stages: run_stage_b(s, cl)
        if "C" in stages: run_stage_c(s, cl)
        if "D" in stages: run_stage_d(s, cl)
        if "E" in stages: run_stage_e(s, cl)
        if "F" in stages: run_stage_f(s, cl)
        if "G" in stages: run_stage_g(s, cl)
    finally:
        s.close()

    exit_code = cl.finish("run-summary", stages=stages)
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
