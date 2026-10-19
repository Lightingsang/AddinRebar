import json
import subprocess
import sys
import os

SERVER_EXE = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

def read_response_for_id(proc, req_id):
    while True:
        line = proc.stdout.readline()
        if not line:
            raise EOFError("Server process closed stdout unexpectedly.")
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            print("Non-JSON stdout line:", line)
            continue
        
        if msg.get("id") == req_id:
            return msg
        else:
            print("Received notification or async message:", msg.get("method") or msg.get("id"))

def main():
    print("=== LIVE SERVER SURFACE CHALLENGE ===")
    if not os.path.exists(SERVER_EXE):
        print(f"Error: {SERVER_EXE} not found.")
        sys.exit(1)

    proc = subprocess.Popen(
        [SERVER_EXE],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        bufsize=1
    )

    try:
        # Step 1: Initialize
        init_req = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "empirical-challenger", "version": "1.0"}
            }
        }
        proc.stdin.write(json.dumps(init_req) + "\n")
        proc.stdin.flush()

        init_resp = read_response_for_id(proc, 1)
        print("Initialize response received:", init_resp.get("result", {}).get("serverInfo", {}))

        # Initialized notification
        init_notif = {"jsonrpc": "2.0", "method": "notifications/initialized"}
        proc.stdin.write(json.dumps(init_notif) + "\n")
        proc.stdin.flush()

        # Step 2: tools/list
        tools_req = {
            "jsonrpc": "2.0",
            "id": 2,
            "method": "tools/list",
            "params": {}
        }
        proc.stdin.write(json.dumps(tools_req) + "\n")
        proc.stdin.flush()

        tools_resp = read_response_for_id(proc, 2)

        tools = tools_resp.get("result", {}).get("tools", [])
        tool_names = [t["name"] for t in tools]
        print(f"Total tools count on live server: {len(tool_names)}")
        print("Tool names:", sorted(tool_names))

        expected_core = [
            "execute_dynamo_code",
            "execute_designscript",
            "get_dynamo_context",
            "cancel_execution"
        ]
        expected_meta = [
            "search_tools",
            "get_tool",
            "run_tool",
            "get_run",
            "propose_tool",
            "test_tool",
            "publish_tool",
            "manage_tool"
        ]
        expected_seeds = [
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
            "create_code_block_node"
        ]

        # Verify 24 invariant
        assert len(tools) == 24, f"Expected exactly 24 tools, got {len(tools)}: {tool_names}"
        print("PASS: Exactly 24 tools exposed on live server tools/list.")

        # Verify Core tools
        for c in expected_core:
            assert c in tool_names, f"Missing core tool: {c}"
        print(f"PASS: All 4 core tools present: {expected_core}")

        # Verify Registry Meta tools
        for m in expected_meta:
            assert m in tool_names, f"Missing registry meta tool: {m}"
        print(f"PASS: All 8 registry meta tools present: {expected_meta}")

        # Verify Seeds
        for s in expected_seeds:
            assert s in tool_names, f"Missing seed tool: {s}"
        print(f"PASS: All 12 seed tools present: {expected_seeds}")

        # Verify inspect_type suppression
        assert "inspect_type" not in tool_names, "CRITICAL DEFECT: inspect_type was NOT suppressed from tools/list!"
        print("PASS: inspect_type is strictly suppressed from tools/list.")

        # Step 3: Attempt to call inspect_type
        inspect_call = {
            "jsonrpc": "2.0",
            "id": 3,
            "method": "tools/call",
            "params": {
                "name": "inspect_type",
                "arguments": {"typeName": "WorkspaceModel"}
            }
        }
        proc.stdin.write(json.dumps(inspect_call) + "\n")
        proc.stdin.flush()

        call_resp = read_response_for_id(proc, 3)
        print("inspect_type call response:", call_resp)

        # It should return error (-32602 or -32601) or isError=True
        is_rejected = False
        if "error" in call_resp:
            err = call_resp["error"]
            print(f"PASS: inspect_type returned protocol error: code={err.get('code')}, message='{err.get('message')}'")
            is_rejected = True
        elif "result" in call_resp:
            res = call_resp["result"]
            if res.get("isError"):
                print(f"PASS: inspect_type returned tool error content: {res.get('content')}")
                is_rejected = True

        assert is_rejected, f"Calling suppressed inspect_type did not fail! Response: {call_resp}"
        print("PASS: Calling inspect_type correctly triggers rejection / unknown tool.")
        print("\n=== ALL LIVE SERVER SURFACE CHALLENGES EMPIRICALLY CONFIRMED! ===")

    finally:
        proc.terminate()
        proc.wait(timeout=5)

if __name__ == "__main__":
    main()
