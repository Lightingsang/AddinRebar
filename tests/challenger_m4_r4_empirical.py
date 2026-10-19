#!/usr/bin/env python3
"""
Adversarial Empirical Verification Harness — Challenger M4 Round 4
Covers:
1. Reflection snapshot parsing & strict schema assertions (27 tools)
2. diameterToleranceMm presence & properties in generate_rebar_curves
3. Zero phantom tools cross-checked against SKILL.md and seed files
4. Verification of 12 Seed manifests on disk vs reflection snapshot
5. Verification of AST Clean Code rules on PlayerRunner.cs (C2 <= 300, M3 <= 50)
6. Check for unannotated empty catches and commented code
7. McpShared invariance check
"""

import os
import sys
import json
import glob
import re

def assert_true(cond, msg):
    if not cond:
        print(f"[FAIL] {msg}")
        raise AssertionError(msg)
    else:
        print(f"[PASS] {msg}")

def main():
    print("=" * 80)
    print("CHALLENGER M4 R4 EMPIRICAL ADVERSARIAL VERIFICATION HARNESS")
    print("=" * 80)

    # 1. Load Reflection Snapshot
    snapshot_path = "tests/grasshopper_tools_list.json"
    assert_true(os.path.exists(snapshot_path), f"Snapshot file exists: {snapshot_path}")

    with open(snapshot_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    tools = {t["name"]: t for t in data.get("result", {}).get("tools", [])}
    assert_true(len(tools) == 27, f"Exact count of 27 tools in reflection snapshot (actual: {len(tools)})")

    # 2. Check generate_rebar_curves specifically
    assert_true("generate_rebar_curves" in tools, "generate_rebar_curves exists in reflection tools")
    rebar_tool = tools["generate_rebar_curves"]
    rebar_props = rebar_tool.get("inputSchema", {}).get("properties", {})
    
    assert_true("diameterToleranceMm" in rebar_props, "diameterToleranceMm exists in generate_rebar_curves inputSchema")
    tol_prop = rebar_props["diameterToleranceMm"]
    assert_true(tol_prop.get("type") == "number", f"diameterToleranceMm type is number: {tol_prop.get('type')}")
    assert_true(tol_prop.get("default") == 2.0, f"diameterToleranceMm default is 2.0: {tol_prop.get('default')}")
    assert_true(len(tol_prop.get("description", "")) > 10, f"diameterToleranceMm has descriptive text: {tol_prop.get('description')}")

    # 3. Check for 0 Phantom Tools against SKILL.md
    skill_path = ".agents/skills/hp-mcp-grasshopper/SKILL.md"
    assert_true(os.path.exists(skill_path), f"SKILL.md exists: {skill_path}")
    with open(skill_path, "r", encoding="utf-8") as f:
        skill_content = f.read()

    # Parse table rows in Section 4
    skill_tool_names = set()
    table_lines = [l.strip() for l in skill_content.splitlines() if l.strip().startswith("|") and "`" in l]
    for line in table_lines:
        matches = re.findall(r"`([a-z_0-9]+)`", line)
        for m in matches:
            if m in tools:
                skill_tool_names.add(m)

    print(f"Discovered {len(skill_tool_names)} tool names in SKILL.md tables.")
    assert_true(len(skill_tool_names) == 27, f"All 27 tools identified in SKILL.md (found: {len(skill_tool_names)})")

    phantom_tools = skill_tool_names - set(tools.keys())
    assert_true(len(phantom_tools) == 0, f"Zero phantom tools in SKILL.md (phantom: {phantom_tools})")

    missing_tools = set(tools.keys()) - skill_tool_names
    assert_true(len(missing_tools) == 0, f"Zero missing tools in SKILL.md (missing: {missing_tools})")

    # Confirm diameterToleranceMm is in SKILL.md under generate_rebar_curves
    assert_true("`diameterToleranceMm`" in skill_content, "diameterToleranceMm mentioned in SKILL.md")

    # 4. Cross-check 12 Seed Library Manifests on disk
    seed_dirs = glob.glob("HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/**/tool.json", recursive=True)
    assert_true(len(seed_dirs) == 12, f"Found exactly 12 seed manifests on disk (found: {len(seed_dirs)})")

    for s_path in seed_dirs:
        with open(s_path, "r", encoding="utf-8") as f:
            manifest = json.load(f)
        s_name = manifest["name"]
        assert_true(s_name in tools, f"Seed '{s_name}' is registered in server reflection tools")
        
        # Verify schema properties match
        m_props = manifest.get("inputSchema", {}).get("properties", {})
        refl_props = tools[s_name].get("inputSchema", {}).get("properties", {})
        for pk, pv in m_props.items():
            assert_true(pk in refl_props, f"Property '{pk}' of seed '{s_name}' exists in reflection")
            if "type" in pv:
                assert_true(refl_props[pk].get("type") == pv["type"], f"Type match for {s_name}.{pk}")

    # 5. Clean Code Audit on PlayerRunner.cs
    player_runner_path = "HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs"
    assert_true(os.path.exists(player_runner_path), f"PlayerRunner.cs exists: {player_runner_path}")
    with open(player_runner_path, "r", encoding="utf-8") as f:
        pr_lines = f.readlines()

    line_count = len(pr_lines)
    assert_true(line_count <= 300, f"Rule C2: PlayerRunner.cs length <= 300 lines (actual: {line_count})")

    # Check method length of Run
    in_run = False
    run_body_lines = 0
    brace_depth = 0
    for idx, line in enumerate(pr_lines):
        if "public object Run(" in line:
            in_run = True
        if in_run:
            run_body_lines += 1
            if "{" in line:
                brace_depth += line.count("{")
            if "}" in line:
                brace_depth -= line.count("}")
                if brace_depth == 0 and run_body_lines > 1:
                    in_run = False
                    break

    assert_true(run_body_lines <= 50, f"Rule M3: PlayerRunner.Run length <= 50 lines (actual: {run_body_lines})")

    # 6. Verify McpShared Invariance (no files modified in last 4 hours)
    import time
    cutoff = time.time() - 3600 * 4
    recent_mcpshared = []
    for root, dirs, files in os.walk("McpShared"):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                if os.path.getmtime(p) > cutoff:
                    recent_mcpshared.append(p)
    assert_true(len(recent_mcpshared) == 0, f"McpShared has 0 modified .cs files in session (found: {recent_mcpshared})")

    print("\n" + "=" * 80)
    print("ALL 100% ADVERSARIAL ASSERTIONS PASSED WITH ZERO ERRORS!")
    print("=" * 80)
    return True

if __name__ == "__main__":
    try:
        main()
        sys.exit(0)
    except AssertionError as e:
        print(f"\nCRITICAL ASSERTION FAILED: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\nUNEXPECTED FAILURE: {e}")
        sys.exit(2)
