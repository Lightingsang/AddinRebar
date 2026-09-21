import json
import os

with open(".agents/challenger_m4_2/tools.json", encoding="utf-8") as f:
    tools_data = json.load(f)
with open(".agents/challenger_m4_2/resources.json", encoding="utf-8") as f:
    resources_data = json.load(f)
with open(".agents/challenger_m4_2/prompts.json", encoding="utf-8") as f:
    prompts_data = json.load(f)

tools = tools_data.get("result", {}).get("tools", [])
resources = resources_data.get("result", {}).get("resources", [])
prompts = prompts_data.get("result", {}).get("prompts", [])

print(f"=== MCP CAPABILITY AUDIT ===")
print(f"Total Tools: {len(tools)} (Expected: 24)")
print(f"Total Resources: {len(resources)} (Expected: 3)")
print(f"Total Prompts: {len(prompts)} (Expected: 4)")

seeds = [
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

core_and_registry = [
    "get_robot_context",
    "execute_robot_code",
    "search_tools",
    "propose_tool",
    "test_tool",
    "publish_tool",
    "manage_tool",
    "get_tool",
    "run_tool",
    "get_run",
    "cancel_execution",
    "inspect_type"
]

tool_names = set(t["name"] for t in tools)
print("\nVerifying 12 Seed Tools:")
for s in seeds:
    present = s in tool_names
    print(f"  [{'PASS' if present else 'FAIL'}] {s}")
    assert present, f"Missing seed tool: {s}"

print("\nVerifying Core & Registry Meta Tools:")
for cr in core_and_registry:
    present = cr in tool_names
    print(f"  [{'PASS' if present else 'FAIL'}] {cr}")
    assert present, f"Missing core/registry tool: {cr}"

print("\nVerifying 3 Resources:")
expected_resources = {"robot://model/info", "robot://selection", "registry://tools"}
actual_resources = set(r["uri"] for r in resources)
for r in expected_resources:
    present = r in actual_resources
    print(f"  [{'PASS' if present else 'FAIL'}] {r}")
    assert present, f"Missing resource: {r}"

print("\nVerifying 4 Prompts:")
expected_prompts = {"robot_analysis_template", "toolify_run", "robot_query_template", "robot_modify_template"}
actual_prompts = set(p["name"] for p in prompts)
for p in expected_prompts:
    present = p in actual_prompts
    print(f"  [{'PASS' if present else 'FAIL'}] {p}")
    assert present, f"Missing prompt: {p}"

print("\nALL MCP CONTRACT CHECKS PASSED!")
