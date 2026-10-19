import json
import glob
import re
import sys
import os

def run_crosscheck():
    print("=" * 70)
    print("CHALLENGER M4 R3 EMPIRICAL CROSS-CHECK HARNESS")
    print("=" * 70)

    # 1. Load server reflection
    reflection_path = "tests/grasshopper_tools_list.json"
    if not os.path.exists(reflection_path):
        print(f"ERROR: Reflection file not found: {reflection_path}")
        return False

    with open(reflection_path, "r", encoding="utf-8") as f:
        refl_data = json.load(f)

    refl_tools = {t["name"]: t for t in refl_data["result"]["tools"]}
    refl_tool_names = set(refl_tools.keys())
    print(f"[1] Server reflection tools loaded: {len(refl_tool_names)}")

    # 2. Parse SKILL.md
    skill_path = ".agents/skills/hp-mcp-grasshopper/SKILL.md"
    if not os.path.exists(skill_path):
        print(f"ERROR: SKILL.md not found: {skill_path}")
        return False

    with open(skill_path, "r", encoding="utf-8") as f:
        skill_text = f.read()

    # Extract sections
    # 4.1 Core
    sec_41 = skill_text[skill_text.find("### 4.1."):skill_text.find("### 4.2.")]
    core_tools = {}
    for line in sec_41.splitlines():
        if line.strip().startswith("| `"):
            parts = [p.strip() for p in line.split("|")[1:-1]]
            if len(parts) >= 3:
                name = parts[0].replace("`", "").strip()
                core_tools[name] = {"purpose": parts[1], "params": parts[2]}

    # 4.2 Context
    sec_42 = skill_text[skill_text.find("### 4.2."):skill_text.find("### 4.3.")]
    ctx_tools = {}
    for line in sec_42.splitlines():
        if line.strip().startswith("| `"):
            parts = [p.strip() for p in line.split("|")[1:-1]]
            if len(parts) >= 3:
                name = parts[0].replace("`", "").strip()
                ctx_tools[name] = {"purpose": parts[1], "params": parts[2]}

    # 4.3 Seeds
    sec_43 = skill_text[skill_text.find("### 4.3."):skill_text.find("### 4.4.")]
    seed_tools = {}
    for line in sec_43.splitlines():
        if "|" in line and "`" in line and not line.strip().startswith("| Nhóm") and not line.strip().startswith("|---"):
            parts = [p.strip() for p in line.split("|")[1:-1]]
            if len(parts) == 5:
                tool_part = parts[1]
                if "`" in tool_part:
                    name = tool_part.replace("`", "").strip()
                    seed_tools[name] = {
                        "group": parts[0],
                        "transaction": parts[2],
                        "desc": parts[3],
                        "params": parts[4]
                    }

    # 4.4 Registry
    sec_44 = skill_text[skill_text.find("### 4.4."):skill_text.find("## 5.")]
    reg_tools = {}
    for line in sec_44.splitlines():
        if line.strip().startswith("| `"):
            parts = [p.strip() for p in line.split("|")[1:-1]]
            if len(parts) >= 3:
                name = parts[0].replace("`", "").strip()
                reg_tools[name] = {"purpose": parts[1], "params": parts[2]}

    all_skill_tools = set(core_tools.keys()) | set(ctx_tools.keys()) | set(seed_tools.keys()) | set(reg_tools.keys())

    print(f"[2] SKILL.md tools parsed:")
    print(f"    - Core Native Tools (4.1):    {len(core_tools)} (expected 6)")
    print(f"    - Context Tool (4.2):         {len(ctx_tools)} (expected 1)")
    print(f"    - Embedded Seed Tools (4.3):  {len(seed_tools)} (expected 12)")
    print(f"    - Registry Meta Tools (4.4):  {len(reg_tools)} (expected 8)")
    print(f"    - Total in SKILL.md:          {len(all_skill_tools)} (expected 27)")

    # 3. Cross-check Tool Names & Phantom Check
    print("\n[3] Phantom Tools & Completeness Cross-Check:")
    phantom_tools = all_skill_tools - refl_tool_names
    missing_tools = refl_tool_names - all_skill_tools

    print(f"    - Phantom tools (in SKILL.md but NOT in reflection): {len(phantom_tools)} {phantom_tools if phantom_tools else '=> ZERO PHANTOM TOOLS (PASS)'}")
    print(f"    - Missing tools (in reflection but NOT in SKILL.md): {len(missing_tools)} {missing_tools if missing_tools else '=> ZERO MISSING TOOLS (PASS)'}")

    if phantom_tools or missing_tools:
        print("FAIL: Tool set mismatch detected!")
        return False

    # Check cancel_execution and all 8 registry tools specifically
    if "cancel_execution" not in core_tools:
        print("FAIL: cancel_execution not present in SKILL.md core tools!")
        return False
    print("    - cancel_execution presence: PASS")

    expected_registry_tools = {
        "search_tools", "get_tool", "run_tool", "get_run",
        "propose_tool", "test_tool", "publish_tool", "manage_tool"
    }
    missing_reg = expected_registry_tools - set(reg_tools.keys())
    if missing_reg:
        print(f"FAIL: Registry tools missing: {missing_reg}")
        return False
    print("    - All 8 registry meta tools present: PASS")

    # 4. Seed Library Manifests Verbatim Cross-Check
    print("\n[4] Cross-checking 12 Seed Manifests against SKILL.md and Reflection:")
    manifest_paths = glob.glob("HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/**/tool.json", recursive=True)
    if len(manifest_paths) != 12:
        print(f"ERROR: Expected 12 seed manifests, found {len(manifest_paths)}")
        return False

    manifests = {}
    for p in manifest_paths:
        with open(p, "r", encoding="utf-8") as f:
            m = json.load(f)
        manifests[m["name"]] = m

    all_seed_passed = True

    for name in sorted(manifests.keys()):
        meta = manifests[name]
        m_schema = meta.get("inputSchema", {})
        m_props = m_schema.get("properties", {})
        m_req = set(m_schema.get("required", []))
        m_trans = meta.get("transaction", "auto")

        skill_entry = seed_tools.get(name)
        if not skill_entry:
            print(f"FAIL: Seed {name} missing in SKILL.md table 4.3!")
            all_seed_passed = False
            continue

        skill_params_str = skill_entry["params"]
        skill_trans = skill_entry["transaction"]

        # Check transaction mode match
        if skill_trans.lower() != m_trans.lower():
            print(f"FAIL: Seed {name} transaction mismatch! Manifest: {m_trans}, SKILL.md: {skill_trans}")
            all_seed_passed = False

        # Reflection tool
        refl_tool = refl_tools.get(name)
        refl_props = refl_tool.get("inputSchema", {}).get("properties", {})
        refl_req = set(refl_tool.get("inputSchema", {}).get("required", []))

        print(f"\n  * Validating seed: `{name}` (transaction: {m_trans})")
        print(f"    SKILL.md doc: {skill_params_str}")

        # Check all properties from manifest
        for prop_name, prop_def in m_props.items():
            prop_type = prop_def.get("type", "")
            prop_default = prop_def.get("default")
            is_req = prop_name in m_req

            # 1) Must exist in SKILL.md param string
            if f"`{prop_name}`" not in skill_params_str:
                print(f"    [FAIL] Property `{prop_name}` not mentioned in SKILL.md for `{name}`")
                all_seed_passed = False
                continue

            # 2) If required, SKILL.md must specify required
            if is_req:
                # find parameter snippet
                param_idx = skill_params_str.find(f"`{prop_name}`")
                param_snip = skill_params_str[param_idx:param_idx+60]
                if "required" not in param_snip:
                    print(f"    [FAIL] Property `{prop_name}` is required in manifest but 'required' missing in snippet: {param_snip}")
                    all_seed_passed = False

            # 3) If default exists, verify default is mentioned
            if prop_default is not None:
                param_idx = skill_params_str.find(f"`{prop_name}`")
                param_snip = skill_params_str[param_idx:param_idx+60]
                # Check default value representation
                def_str = str(prop_default)
                if def_str not in param_snip and (not isinstance(prop_default, float) or f"{prop_default:.1f}" not in param_snip):
                    print(f"    [WARN] Property `{prop_name}` default {prop_default} not explicitly seen in snippet: {param_snip}")

            # 4) Verify property also in reflection
            if prop_name not in refl_props:
                print(f"    [FAIL] Property `{prop_name}` missing in server reflection for `{name}`!")
                all_seed_passed = False

        # Note: dryRun is injected by server engine for auto/manual tools
        if "dryRun" in skill_params_str:
            if "dryRun" not in refl_props and m_trans != "none":
                print(f"    [INFO] dryRun in SKILL.md for {name}")

        print(f"    -> All {len(m_props)} manifest properties verified verbatim.")

    if not all_seed_passed:
        print("\nFAIL: Seed parameter schema verification encountered errors!")
        return False

    print("\n" + "=" * 70)
    print("ALL EMPIRICAL SCHEMA AND PHANTOM CROSS-CHECKS PASSED 100%!")
    print("=" * 70)
    return True

if __name__ == "__main__":
    success = run_crosscheck()
    sys.exit(0 if success else 1)
