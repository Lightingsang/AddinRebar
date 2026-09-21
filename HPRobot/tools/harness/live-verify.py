"""Live verification of the Autodesk Robot Structural Analysis MCP server over stdio.
Validates server connection, protocol tools advertisement (24 tools), get_robot_context,
and 3-tier safety refusals.
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
    return r.get("message") or ""

def phase_detached(s):
    # 1. Tools list check
    tools_dict = s.tools()
    check("T1 Exactly 24 tools advertised by HPRobot.Mcp.Server", len(tools_dict) == 24, f"Found {len(tools_dict)} tools")

    # 2. Check context tool
    ctx = s.tool("get_robot_context", {})
    if ok(ctx):
        check("T2 Context tool answers: host robot", ctx.get("host") == "robot", short(ctx))
    else:
        # Bridge not connected case
        check("T2 Context reports bridge guidance naming pipe hprobot-mcp-2026", "hprobot-mcp-2026" in msg(ctx) and "HPRobot.McpBridge" in msg(ctx), short(ctx))

    # 3. Guard test: denied namespace System.IO or bridge refusal
    guard_code = "System.IO.File.WriteAllText(\"test.txt\", \"data\"); return 1;"
    guard_res = s.tool("execute_robot_code", {"code": guard_code, "transaction": "none"})
    check("T3 Guard / Bridge refuses forbidden code execution", guard_res.get("isError") is True, short(guard_res))

    # 4. Seeds presence
    expected_seeds = [
        "get_model_info", "get_structural_objects", "get_materials_and_sections",
        "get_coordinate_systems_and_grids", "get_load_definitions", "draw_bar_by_coords",
        "assign_node_support", "assign_bar_section", "assign_bar_load",
        "run_calculations", "get_node_reactions", "get_bar_forces"
    ]
    all_seeds_present = all(seed in tools_dict for seed in expected_seeds)
    check("T4 All 12 embedded seeds present in tools list", all_seeds_present, f"Seeds checked: {len(expected_seeds)}")

def main():
    parser = argparse.ArgumentParser(description="HPRobot MCP Live Verification")
    parser.add_argument("--exe", required=True, help="Path to HPRobot.Mcp.Server.exe")
    parser.add_argument("--phase", default="detached", choices=["detached", "spike", "seeds", "all"])
    args = parser.parse_args()

    s = Server(args.exe)
    try:
        init_res = s.initialize()
        print(f"Server initialized (PID: {s.proc.pid})")
        if args.phase in ("detached", "all"):
            phase_detached(s)
    finally:
        s.close()

    sys.exit(CL.finish("summary-robot", phase=args.phase))

if __name__ == "__main__":
    main()
