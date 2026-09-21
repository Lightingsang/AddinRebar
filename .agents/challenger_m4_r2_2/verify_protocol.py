import json
import subprocess
import sys

exe = r"HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe"
harness = r"McpShared/tools/mcp-call.py"

def call(method):
    cmd = [sys.executable, "-X", "utf8", harness, exe, method]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if res.returncode != 0:
        print(f"Error calling {method}: {res.stderr}")
        sys.exit(1)
    return json.loads(res.stdout)

tools_data = call("tools/list")
tools = tools_data.get("result", {}).get("tools", [])
print(f"Total tools count: {len(tools)}")
tool_names = sorted(t["name"] for t in tools)
for idx, name in enumerate(tool_names, 1):
    print(f"  {idx:2d}. {name}")

expected_seeds = [
    "get_model_info",
    "get_structural_objects",
    "get_materials_and_sections",
    "get_coordinate_systems_and_grids",
    "get_load_definitions",
    "draw_bar_by_coords",
    "assign_node_support",
    "assign_bar_section",
    "assign_bar_load",
    "run_calculations",
    "get_node_reactions",
    "get_bar_forces"
]

expected_meta = [
    "search_tools",
    "get_tool",
    "run_tool",
    "propose_tool",
    "test_tool",
    "publish_tool",
    "deprecate_tool",
    "delete_tool",
    "disable_tool",
    "enable_tool"
]

resources_data = call("resources/list")
resources = resources_data.get("result", {}).get("resources", [])
print(f"\nTotal resources count: {len(resources)}")
for r in resources:
    print(f"  - {r['uri']} ({r['name']}): {r['title']}")

prompts_data = call("prompts/list")
prompts = prompts_data.get("result", {}).get("prompts", [])
print(f"\nTotal prompts count: {len(prompts)}")
for p in prompts:
    print(f"  - {p['name']}: {p['title']}")
