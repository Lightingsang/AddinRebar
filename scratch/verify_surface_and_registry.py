import json
import sqlite3
import os
import subprocess
import sys

def verify():
    print("=== CHALLENGER B 2: EMPIRICAL VERIFICATION HARNESS ===")
    
    # 1. Inspect live tools/list dump
    dump_path = "scratch/challenger_dynamo_tools_live.json"
    with open(dump_path, "r", encoding="utf-8") as f:
        data = json.load(f)
        
    assert data.get("jsonrpc") == "2.0", f"Invalid jsonrpc version: {data.get('jsonrpc')}"
    assert "result" in data, "No result in response"
    tools = data["result"].get("tools", [])
    
    print(f"[+] Total tools count: {len(tools)}")
    assert len(tools) == 25, f"Expected 25 tools, got {len(tools)}"
    
    expected_core = {
        "execute_dynamo_code",
        "execute_designscript",
        "get_dynamo_context",
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
        "run_dyn_file",
        "get_active_graph_info",
        "search_nodes_library",
        "inspect_node_type",
        "add_node_to_workspace",
        "connect_node_ports",
        "disconnect_node_ports",
        "set_node_properties",
        "get_node_output_values",
        "export_graph_to_image",
        "validate_dyn_file",
        "create_code_block_node",
        "batch_edit_graph"
    }
    
    expected_all = expected_core | expected_registry | expected_seeds
    assert len(expected_all) == 25, f"Expected set size mismatch: {len(expected_all)}"
    
    actual_names = {t["name"] for t in tools}
    print(f"[+] Discovered tool names: {sorted(list(actual_names))}")
    
    missing = expected_all - actual_names
    unexpected = actual_names - expected_all
    assert not missing, f"Missing tools: {missing}"
    assert not unexpected, f"Unexpected tools: {unexpected}"
    print("[+] All 25 expected tool names match exactly (4 Core + 8 Registry + 13 Seeds).")
    
    # 2. Rigorous Schema Inspection
    print("\n--- Validating Schemas of all 25 Tools ---")
    for t in tools:
        name = t["name"]
        desc = t.get("description", "")
        schema = t.get("inputSchema", {})
        
        assert desc and len(desc.strip()) > 0, f"Tool '{name}' has empty description"
        assert schema.get("type") == "object", f"Tool '{name}' schema type is not object: {schema.get('type')}"
        assert "properties" in schema, f"Tool '{name}' schema missing properties"
        props = schema["properties"]
        assert isinstance(props, dict), f"Tool '{name}' properties is not dict"
        
        req = schema.get("required", [])
        assert isinstance(req, list), f"Tool '{name}' required is not list"
        for r in req:
            assert r in props, f"Tool '{name}' required property '{r}' not in properties"
            
        print(f"  - Tool: {name:<25} | Props: {len(props):<2} | Required: {len(req):<2} | Desc: {desc[:40]}...")
        
    print("[+] All 25 tool schemas are valid and conform to MCP standard JSON Schema.")
    
    # 3. SQLite Registry Check
    print("\n--- SQLite Registry DB Inspection ---")
    appdata_db = os.path.expandvars(r"%APPDATA%\HPDynamo\McpServer\registry.db")
    assert os.path.exists(appdata_db), f"registry.db does not exist at {appdata_db}"
    print(f"[+] Found registry.db at: {appdata_db}")
    
    conn = sqlite3.connect(appdata_db)
    cur = conn.cursor()
    
    # Check runs count
    cur.execute("SELECT COUNT(*) FROM runs")
    runs_count = cur.fetchone()[0]
    print(f"[+] Total runs recorded in registry.db: {runs_count}")
    assert runs_count >= 116, f"Runs count diminished! Expected >= 116, got {runs_count}"
    
    # Check tools status
    cur.execute("SELECT name, status, version FROM tools ORDER BY name")
    db_tools = cur.fetchall()
    print(f"[+] Total registered tools in DB: {len(db_tools)}")
    
    published_count = 0
    quarantined_tools = []
    for name, status, version in db_tools:
        if status == "published":
            published_count += 1
        elif status == "quarantined":
            quarantined_tools.append(name)
        print(f"  - DB Tool: {name:<25} | Status: {status:<12} | Version: {version}")
        
    assert len(quarantined_tools) == 0, f"Tools still quarantined: {quarantined_tools}"
    assert published_count == 13, f"Expected 13 published seeds in DB, got {published_count}"
    print(f"[+] Exactly 13/13 seeds in registry.db are 'published', 0 quarantined!")
    
    # Check registry_events
    cur.execute("SELECT tool_name, event, actor, detail, ts FROM registry_events ORDER BY id DESC LIMIT 10")
    events = cur.fetchall()
    print("\n--- Recent Registry Events ---")
    for ev in events:
        print(f"  - Event: Tool={ev[0]} Event={ev[1]} Actor={ev[2]} Detail={ev[3]} Ts={ev[4]}")
        
    conn.close()
    
    # 4. Live MCP Tools/Call Challenge
    print("\n--- Live MCP Tool Invocation Challenge via mcp-call.py ---")
    server_exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
    
    test_queries = [
        ("search_tools", '{"query": "graph"}'),
        ("get_tool", '{"name": "batch_edit_graph"}'),
        ("get_tool", '{"name": "search_nodes_library"}'),
        ("get_tool", '{"name": "get_active_graph_info"}'),
    ]
    
    for tool_name, tool_arg in test_queries:
        cmd = [
            "python", "McpShared/tools/mcp-call.py",
            server_exe,
            "tools/call",
            tool_name,
            tool_arg
        ]
        res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, check=True)
        resp_json = json.loads(res.stdout)
        assert resp_json.get("jsonrpc") == "2.0"
        content = resp_json.get("result", {}).get("content", [])
        assert len(content) > 0, f"Tool call {tool_name} returned empty content"
        text_payload = content[0].get("text", "")
        print(f"[+] Live Call to '{tool_name}' with args {tool_arg}:")
        parsed_payload = json.loads(text_payload)
        if tool_name == "get_tool":
            status = parsed_payload.get("status")
            print(f"    -> Tool: {parsed_payload.get('name')} | Status: {status} | Version: {parsed_payload.get('version')}")
            assert status == "published", f"Tool {parsed_payload.get('name')} status is {status}, expected 'published'!"
        elif tool_name == "search_tools":
            matches = parsed_payload.get("tools", [])
            print(f"    -> Found {len(matches)} matching tools: {[m.get('name') for m in matches]}")
            assert len(matches) > 0, "search_tools returned 0 matches for 'graph'"
            
    print("\n=== ALL EMPIRICAL AND LIVE ADVERSARIAL CHECKS PASSED 100% ===")

if __name__ == "__main__":
    verify()
