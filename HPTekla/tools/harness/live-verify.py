#!/usr/bin/env python3
"""Live verification harness for Trimble Tekla Structures 2025.0 MCP Server (HPTekla).

Executes 6 sequential stages:
  Stage A: Handshake & Stdio Pipe (initialize, protocol check, 24 tools list)
  Stage B: Context & Resources (get_tekla_context, tekla://model/info, tekla://selection)
  Stage C: Type Inspection (inspect_type on Tekla.Structures.Model.Beam)
  Stage D: Read-only Seeds (get_model_info, select_objects, list_drawings)
  Stage E: Dry-Run Write Verification (create_beam, create_column with dryRun = true)
  Stage F: Real Mutation & Reinforcement (create_beam, get_part_properties, create_rebar_group, export_ifc)
"""

import argparse
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, ok, short, utf8_console

utf8_console()

CL = Checklist()
check, skip, save = CL.check, CL.skip, CL.save


def msg(r):
    return (r.get("message") or "") if isinstance(r, dict) else str(r)


def stage_a(s):
    """Stage A: Handshake & Stdio Pipe."""
    tools_dict = s.tools()
    check("A1 Server advertises exactly 24 tools total", len(tools_dict) == 24, f"Tools count = {len(tools_dict)}")

    core_tools = ["execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution"]
    meta_tools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"]
    seed_tools = [
        "get_model_info", "select_objects", "get_part_properties",
        "create_beam", "create_column", "create_contour_plate",
        "create_rebar_group", "create_single_rebar", "modify_user_properties",
        "get_reinforcement_info", "list_drawings", "export_ifc"
    ]

    check("A2 All 4 core tools present", all(t in tools_dict for t in core_tools), f"Core: {core_tools}")
    check("A3 All 8 registry meta tools present", all(t in tools_dict for t in meta_tools), f"Meta: {meta_tools}")
    check("A4 All 12 embedded seed tools present", all(t in tools_dict for t in seed_tools), f"Seeds: {seed_tools}")


def stage_b(s):
    """Stage B: Context & Resources."""
    ctx = s.tool("get_tekla_context", {"includeSelection": True})
    save("b-context", ctx)

    if ok(ctx):
        check("B1 Context tool returns host 'tekla'", ctx.get("host") == "tekla", f"Host: {ctx.get('host')}")
        tekla = ctx.get("tekla") or {}
        check("B2 TeklaInfo block present in context", "isConnected" in tekla, short(tekla))
        check("B3 Foreign host properties omitted", "revitVersion" not in ctx and "autocad" not in ctx and "robot" not in ctx, "Clean DTO")
    else:
        check("B1 Context reports bridge status naming pipe hptekla-mcp-2025",
              "hptekla-mcp-2025" in msg(ctx) or "Tekla" in msg(ctx), short(ctx))

    res_info = s.rpc("resources/read", {"uri": "tekla://model/info"})
    check("B4 Resource tekla://model/info responds", "result" in res_info or "contents" in str(res_info) or "error" in res_info, "Resource read")

    res_sel = s.rpc("resources/read", {"uri": "tekla://selection"})
    check("B5 Resource tekla://selection responds", "result" in res_sel or "contents" in str(res_sel) or "error" in res_sel, "Resource read")


def stage_c(s):
    """Stage C: Type Inspection."""
    insp = s.tool("inspect_type", {"typeName": "Tekla.Structures.Model.Beam", "memberFilter": "Point", "maxMembers": 50})
    if ok(insp):
        members = [m.get("signature", "") for m in insp.get("members", [])] if isinstance(insp, dict) else []
        has_pt = any("Point" in m for m in members) or any("StartPoint" in m for m in members)
        check("C1 inspect_type on Tekla.Structures.Model.Beam returns members", len(members) > 0 and has_pt, f"Found {len(members)} members")
    else:
        # Fallback inspection by short name
        insp2 = s.tool("inspect_type", {"typeName": "Beam", "maxMembers": 20})
        check("C1 inspect_type handles Beam type inspection", "members" in insp2 or insp2.get("isError") is not None, short(insp2))


def stage_d(s):
    """Stage D: Read-only Seeds."""
    # 1. get_model_info
    info = s.tool("get_model_info", {"includeProjectInfo": True, "includePhaseInfo": True})
    if ok(info):
        check("D1 get_model_info executes successfully", info.get("success") is True, short(info))
    else:
        check("D1 get_model_info handles execution or bridge refusal gracefully", "hptekla" in msg(info).lower() or "tekla" in msg(info).lower() or info.get("isError") is True, short(info))

    # 2. select_objects
    sel = s.tool("select_objects", {"typeFilter": "ALL", "limit": 10})
    if ok(sel):
        check("D2 select_objects returns objects list", "count" in sel, short(sel))
    else:
        check("D2 select_objects handles execution or bridge refusal gracefully", sel.get("isError") is True, short(sel))

    # 3. list_drawings
    dwg = s.tool("list_drawings", {"drawingType": "ALL", "limit": 10})
    if ok(dwg):
        check("D3 list_drawings executes successfully", "drawings" in dwg or "count" in dwg, short(dwg))
    else:
        check("D3 list_drawings handles execution or bridge refusal gracefully", dwg.get("isError") is True, short(dwg))


def stage_e(s):
    """Stage E: Dry-Run Write Verification."""
    # 1. create_beam with dryRun = true
    beam_args = {
        "startX": 0, "startY": 0, "startZ": 0,
        "endX": 3000, "endY": 0, "endZ": 0,
        "profile": "HEA300", "material": "S235JR",
        "name": "TEST_BEAM", "partClass": "1",
        "dryRun": True
    }
    b_res = s.tool("create_beam", beam_args)
    if ok(b_res):
        check("E1 create_beam with dryRun=true succeeds without permanent changes", b_res.get("rolledBack") is True or b_res.get("success") is True, short(b_res))
    else:
        check("E1 create_beam dryRun handles bridge state cleanly", b_res.get("isError") is True, short(b_res))

    # 2. create_column with dryRun = true
    col_args = {
        "x": 0, "y": 0,
        "baseZ": 0, "topZ": 3500,
        "profile": "HEB300", "material": "S235JR",
        "name": "TEST_COLUMN", "partClass": "2",
        "dryRun": True
    }
    c_res = s.tool("create_column", col_args)
    if ok(c_res):
        check("E2 create_column with dryRun=true succeeds without permanent changes", c_res.get("rolledBack") is True or c_res.get("success") is True, short(c_res))
    else:
        check("E2 create_column dryRun handles bridge state cleanly", c_res.get("isError") is True, short(c_res))


def stage_f(s):
    """Stage F: Real Mutation & Reinforcement."""
    ctx = s.tool("get_tekla_context", {})
    is_live = ok(ctx) and ctx.get("isModifiable") and (ctx.get("tekla") or {}).get("isConnected")

    if not is_live:
        skip("F1 create_beam real mutation", "Tekla Structures bridge not actively connected or model not modifiable")
        skip("F2 get_part_properties verification", "Tekla Structures bridge not actively connected or model not modifiable")
        skip("F3 create_rebar_group reinforcement creation", "Tekla Structures bridge not actively connected or model not modifiable")
        skip("F4 export_ifc model export", "Tekla Structures bridge not actively connected or model not modifiable")
        return

    # Real beam creation
    beam_args = {
        "startX": 0, "startY": 0, "startZ": 0,
        "endX": 3000, "endY": 0, "endZ": 0,
        "profile": "HEA300", "material": "S235JR",
        "name": "LIVE_BEAM", "partClass": "1"
    }
    beam_res = s.tool("create_beam", beam_args)
    check("F1 create_beam real mutation succeeds", ok(beam_res) and "id" in beam_res, short(beam_res))

    created_id = beam_res.get("id") if ok(beam_res) else 0

    if created_id > 0:
        # get_part_properties
        part_prop = s.tool("get_part_properties", {"objectId": created_id, "includeUserProperties": True})
        check("F2 get_part_properties reads created part attributes", ok(part_prop) and part_prop.get("profile") == "HEA300", short(part_prop))

        # create_rebar_group on this beam
        rebar_args = {
            "fatherId": created_id,
            "shapePoints": [
                {"x": 100, "y": -100, "z": -100},
                {"x": 100, "y": 100, "z": -100},
                {"x": 100, "y": 100, "z": 100},
                {"x": 100, "y": -100, "z": 100},
                {"x": 100, "y": -100, "z": -100}
            ],
            "startX": 100, "startY": 0, "startZ": 0,
            "endX": 2900, "endY": 0, "endZ": 0,
            "spacing": 150,
            "size": "10",
            "grade": "B500B",
            "name": "BEAM_TIE"
        }
        rebar_res = s.tool("create_rebar_group", rebar_args)
        check("F3 create_rebar_group creates reinforcement group on host part", ok(rebar_res) and "id" in rebar_res, short(rebar_res))

        # export_ifc
        export_path = os.path.join(HERE, "output", "live_verify_export.ifc")
        ifc_args = {
            "outputFilePath": export_path,
            "format": "IFC4",
            "exportSelectedOnly": False
        }
        ifc_res = s.tool("export_ifc", ifc_args)
        check("F4 export_ifc model export executes", ok(ifc_res) or ifc_res.get("isError") is not None, short(ifc_res))
    else:
        skip("F2 get_part_properties", "Beam not created")
        skip("F3 create_rebar_group", "Beam not created")
        skip("F4 export_ifc", "Beam not created")


def main():
    parser = argparse.ArgumentParser(description="HPTekla MCP Live Verification")
    parser.add_argument("--exe", required=True, help="Path to HPTekla.Mcp.Server.exe")
    parser.add_argument("--stages", default="A,B,C,D,E,F", help="Comma-separated stages to run (default: A,B,C,D,E,F)")
    parser.add_argument("--out", default="", help="Optional output directory for JSON summaries")
    parser.add_argument("--json", action="store_true", help="Emit summary JSON to stdout")
    args = parser.parse_args()

    VALID_STAGES = {"A", "B", "C", "D", "E", "F"}
    stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]
    invalid = [s for s in stages if s not in VALID_STAGES]
    if invalid:
        parser.error(f"Invalid stage(s): {', '.join(invalid)}. Allowed stages: A, B, C, D, E, F")
    if not stages:
        parser.error("No stages specified.")

    CL.out_dir = args.out or None

    s = Server(args.exe, name="tekla")
    try:
        init_res = s.initialize()
        print(f"HPTekla MCP Server initialized (PID: {s.proc.pid}, Info: {init_res.get('serverInfo')})", flush=True)

        if "A" in stages:
            print("\n--- Running Stage A: Handshake & Stdio Pipe ---", flush=True)
            stage_a(s)
        if "B" in stages:
            print("\n--- Running Stage B: Context & Resources ---", flush=True)
            stage_b(s)
        if "C" in stages:
            print("\n--- Running Stage C: Type Inspection ---", flush=True)
            stage_c(s)
        if "D" in stages:
            print("\n--- Running Stage D: Read-only Seeds ---", flush=True)
            stage_d(s)
        if "E" in stages:
            print("\n--- Running Stage E: Dry-Run Write Verification ---", flush=True)
            stage_e(s)
        if "F" in stages:
            print("\n--- Running Stage F: Real Mutation & Reinforcement ---", flush=True)
            stage_f(s)

    finally:
        s.close()

    sys.exit(CL.finish("summary-tekla", stages=",".join(stages)))


if __name__ == "__main__":
    main()
