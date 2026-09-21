import subprocess
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

def test_server(exe_path, label):
    print(f"==================================================")
    print(f"Forensic Test against {label}: {exe_path}")
    print(f"==================================================")
    
    if not os.path.exists(exe_path):
        print(f"FAIL: Exe path does not exist: {exe_path}")
        return False
        
    proc = subprocess.Popen(
        [exe_path],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding='utf-8'
    )
    
    def send_request(req_id, method, params=None):
        payload = {'jsonrpc': '2.0', 'id': req_id, 'method': method}
        if params is not None:
            payload['params'] = params
        wire_str = json.dumps(payload) + '\n'
        proc.stdin.write(wire_str)
        proc.stdin.flush()
        
        while True:
            line = proc.stdout.readline()
            if not line:
                return None
            line = line.strip()
            if not line:
                continue
            try:
                msg = json.loads(line)
                if msg.get('id') == req_id:
                    return msg
            except json.JSONDecodeError:
                pass

    # 1. Initialize
    init_res = send_request(1, 'initialize', {
        'protocolVersion': '2024-11-05',
        'capabilities': {},
        'clientInfo': {'name': 'forensic-auditor', 'version': '1.0'}
    })
    
    if not init_res or 'result' not in init_res:
        print(f"FAIL: Initialize response invalid: {init_res}")
        proc.kill()
        return False
    
    server_info = init_res['result'].get('serverInfo', {})
    print(f"Server Info: {server_info.get('name')} v{server_info.get('version')}")

    # 2. Initialized notification
    proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'method': 'notifications/initialized'}) + '\n')
    proc.stdin.flush()

    # 3. tools/list
    tools_res = send_request(2, 'tools/list', {})
    if not tools_res or 'result' not in tools_res:
        print(f"FAIL: tools/list response invalid: {tools_res}")
        proc.kill()
        return False
        
    tools = tools_res['result'].get('tools', [])
    print(f"Total Tools Discovered: {len(tools)}")
    
    expected_core = {"cancel_execution", "execute_tekla_code", "get_tekla_context", "inspect_type"}
    expected_meta = {"get_run", "get_tool", "manage_tool", "propose_tool", "publish_tool", "run_tool", "search_tools", "test_tool"}
    expected_seeds = {
        "create_beam", "create_column", "create_contour_plate",
        "create_rebar_group", "create_single_rebar", "get_reinforcement_info",
        "get_model_info", "select_objects",
        "get_part_properties", "modify_user_properties",
        "list_drawings", "export_ifc"
    }
    
    discovered_names = {t['name'] for t in tools}
    
    missing_core = expected_core - discovered_names
    missing_meta = expected_meta - discovered_names
    missing_seeds = expected_seeds - discovered_names
    unexpected = discovered_names - (expected_core | expected_meta | expected_seeds)
    
    print(f"Core tools match: {len(expected_core - missing_core)}/4 (Missing: {missing_core})")
    print(f"Meta tools match: {len(expected_meta - missing_meta)}/8 (Missing: {missing_meta})")
    print(f"Seed tools match: {len(expected_seeds - missing_seeds)}/12 (Missing: {missing_seeds})")
    print(f"Unexpected tools: {unexpected}")
    
    if missing_core or missing_meta or missing_seeds or unexpected:
        print("FAIL: Tool set mismatch!")
        proc.kill()
        return False
        
    # Check seed tools detailed input schema
    for t in tools:
        if t['name'] in expected_seeds:
            schema = t.get('inputSchema', {})
            props = schema.get('properties', {})
            req = schema.get('required', [])
            print(f"  [SEED] {t['name']}: {len(props)} properties, {len(req)} required")

    # 4. Call get_tekla_context (verify graceful handling of disconnected bridge)
    call_ctx_res = send_request(3, 'tools/call', {
        'name': 'get_tekla_context',
        'arguments': {'includeSelection': False}
    })
    print(f"get_tekla_context call result: isError={call_ctx_res.get('result', {}).get('isError')}")
    content = call_ctx_res.get('result', {}).get('content', [{}])[0].get('text', '')
    print(f"Response preview: {content[:150]}...")
    if "hptekla-mcp-2025" not in content and "BridgeNotConnected" not in content and "not connected" not in content.lower():
        print(f"WARNING: Expected bridge connection hint in context response: {content}")

    # 5. resources/list
    resources_res = send_request(4, 'resources/list', {})
    resources = resources_res.get('result', {}).get('resources', [])
    print(f"Total Resources Discovered: {len(resources)}")
    for r in resources:
        print(f"  - URI: {r.get('uri')}, Name: {r.get('name')}")
    if len(resources) != 3:
        print(f"FAIL: Expected exactly 3 resources, got {len(resources)}")
        proc.kill()
        return False

    # 6. prompts/list
    prompts_res = send_request(5, 'prompts/list', {})
    prompts = prompts_res.get('result', {}).get('prompts', [])
    print(f"Total Prompts Discovered: {len(prompts)}")
    for p in prompts:
        print(f"  - Name: {p.get('name')}, Desc: {p.get('description', '')[:60]}...")
    if len(prompts) != 4:
        print(f"FAIL: Expected exactly 4 prompts, got {len(prompts)}")
        proc.kill()
        return False

    proc.terminate()
    stdout, stderr = proc.communicate(timeout=5)
    if stderr:
        print(f"Stderr output:\n{stderr}")
    print(f"PASS: All assertions passed for {label}.\n")
    return True

if __name__ == '__main__':
    rel_exe = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe'
    dbg_exe = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe'
    
    ok_rel = test_server(rel_exe, "Release Build")
    ok_dbg = test_server(dbg_exe, "Debug Build")
    
    if ok_rel and ok_dbg:
        print("ALL FORENSIC TESTS PASSED EMPIRICALLY!")
        sys.exit(0)
    else:
        print("FORENSIC VERIFICATION FAILED!")
        sys.exit(1)
