import json
import os
import sys

def main():
    tools_path = "tests/grasshopper_tools_list.json"
    if not os.path.exists(tools_path):
        print(f"Error: {tools_path} does not exist.")
        sys.exit(1)

    with open(tools_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    tools = data.get("result", {}).get("tools", [])
    print(f"Total tools returned by HPGrasshopper.Mcp.Server: {len(tools)}")

    expected_core = {
        "read_grasshopper_canvas",
        "run_grasshopper_player",
        "batch_edit_grasshopper_canvas",
        "execute_grasshopper_code",
        "export_grasshopper_canvas_image",
        "get_grasshopper_context",
        "cancel_execution"
    }

    expected_registry = {
        "search_tools",
        "get_tool",
        "run_tool",
        "get_run",
        "propose_tool",
        "test_tool",
        "publish_tool",
        "manage_tool"
    }

    expected_seeds = {
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
        "export_model_to_rhino"
    }

    tool_names = {t["name"] for t in tools}

    # Verify sets
    missing_core = expected_core - tool_names
    missing_registry = expected_registry - tool_names
    missing_seeds = expected_seeds - tool_names

    unexpected_tools = tool_names - (expected_core | expected_registry | expected_seeds)

    print(f"Missing Core tools: {missing_core}")
    print(f"Missing Registry tools: {missing_registry}")
    print(f"Missing Seed tools: {missing_seeds}")
    print(f"Unexpected tools: {unexpected_tools}")

    # Inspect type check
    if "inspect_type" in tool_names:
        print("FAIL: inspect_type is present in tools/list but should be suppressed!")
    else:
        print("PASS: inspect_type is properly suppressed.")

    # Schema validation
    schema_failures = []
    for t in tools:
        name = t.get("name")
        desc = t.get("description", "")
        schema = t.get("inputSchema", {})

        if not name:
            schema_failures.append(f"Tool missing name: {t}")
            continue

        if not desc or len(desc.strip()) < 10:
            schema_failures.append(f"Tool {name} has missing or too short description: '{desc}'")

        if not isinstance(schema, dict):
            schema_failures.append(f"Tool {name} inputSchema is not an object")
            continue

        if schema.get("type") != "object":
            schema_failures.append(f"Tool {name} inputSchema.type is not 'object': {schema.get('type')}")

        properties = schema.get("properties")
        if properties is not None and not isinstance(properties, dict):
            schema_failures.append(f"Tool {name} inputSchema.properties is not a dictionary")

        required = schema.get("required")
        if required is not None:
            if not isinstance(required, list):
                schema_failures.append(f"Tool {name} inputSchema.required is not a list")
            else:
                for req_key in required:
                    if properties is None or req_key not in properties:
                        schema_failures.append(f"Tool {name} required property '{req_key}' not found in properties")

        # Validate property definitions
        if isinstance(properties, dict):
            for prop_name, prop_def in properties.items():
                if not isinstance(prop_def, dict):
                    schema_failures.append(f"Tool {name} prop '{prop_name}' definition is not an object: {prop_def}")
                else:
                    prop_type = prop_def.get("type")
                    if not prop_type and "type" not in prop_def and "$ref" not in prop_def and "anyOf" not in prop_def and "oneOf" not in prop_def:
                        schema_failures.append(f"Tool {name} prop '{prop_name}' has no type specified")

    if schema_failures:
        print(f"FAIL: {len(schema_failures)} Schema Validation Failures:")
        for sf in schema_failures:
            print("  - " + sf)
    else:
        print(f"PASS: 100% of {len(tools)} tools passed strict JSON Schema validation.")

    if not missing_core and not missing_registry and not missing_seeds and not unexpected_tools and not schema_failures and "inspect_type" not in tool_names:
        print("OVERALL SURFACE TEST: PASS")
        return 0
    else:
        print("OVERALL SURFACE TEST: FAIL")
        return 1

if __name__ == "__main__":
    sys.exit(main())
