import subprocess
import json
import sys

exe_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe"

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
    proc.stdin.write(json.dumps(payload) + '\n')
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

print("1. Handshake test...")
init_res = send_request(1, 'initialize', {
    'protocolVersion': '2024-11-05',
    'capabilities': {},
    'clientInfo': {'name': 'stress-test-client', 'version': '1.0'}
})
assert init_res is not None and 'result' in init_res, f"Init failed: {init_res}"
server_info = init_res['result'].get('serverInfo', {})
print(f"   Server info: {server_info}")

proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'method': 'notifications/initialized'}) + '\n')
proc.stdin.flush()

print("\n2. Tools/list validation...")
tools_res = send_request(2, 'tools/list', {})
assert tools_res is not None and 'result' in tools_res, f"tools/list failed: {tools_res}"
tools = tools_res['result'].get('tools', [])
print(f"   Tools count: {len(tools)} (expected: 24)")
assert len(tools) == 24, f"Expected 24 tools, got {len(tools)}"

for t in tools:
    assert 'name' in t and len(t['name']) > 0, f"Tool missing name: {t}"
    assert 'description' in t, f"Tool {t['name']} missing description"
    schema = t.get('inputSchema', {})
    assert schema.get('type') == 'object', f"Tool {t['name']} inputSchema is not an object"

print("\n3. Testing get_tekla_context when bridge is disconnected...")
ctx_res = send_request(3, 'tools/call', {
    'name': 'get_tekla_context',
    'arguments': {'includeSelection': False}
})
print(f"   get_tekla_context response: {ctx_res}")
assert ctx_res is not None, "get_tekla_context gave no response"
# Expect an error or result indicating bridge is not connected
res_content = ctx_res.get('result', {}).get('content', [])
text_content = res_content[0].get('text', '') if res_content else ''
print(f"   Response text preview: {text_content[:150]}...")
assert 'BridgeNotConnected' in text_content or 'Open Tekla Structures 2025' in text_content or ctx_res.get('result', {}).get('isError') == True or 'error' in ctx_res, "Expected BridgeNotConnected or diagnostic hint"

print("\n4. Testing execute_tekla_code when bridge is disconnected...")
exec_res = send_request(4, 'tools/call', {
    'name': 'execute_tekla_code',
    'arguments': {'code': 'return 42;'}
})
print(f"   execute_tekla_code response: {exec_res}")
assert exec_res is not None, "execute_tekla_code gave no response"
exec_content = exec_res.get('result', {}).get('content', [])
exec_text = exec_content[0].get('text', '') if exec_content else ''
print(f"   Response text preview: {exec_text[:150]}...")
assert 'BridgeNotConnected' in exec_text or 'Open Tekla Structures 2025' in exec_text or exec_res.get('result', {}).get('isError') == True or 'error' in exec_res, "Expected BridgeNotConnected or diagnostic hint"

print("\n5. Testing search_tools registry meta tool...")
search_res = send_request(5, 'tools/call', {
    'name': 'search_tools',
    'arguments': {'query': 'rebar'}
})
assert search_res is not None, "search_tools gave no response"
search_text = search_res.get('result', {}).get('content', [])[0].get('text', '')
print(f"   search_tools 'rebar' result preview: {search_text[:200]}...")
assert 'create_rebar_group' in search_text or 'rebar' in search_text.lower(), "Expected rebar tools found"

proc.terminate()
print("\nALL ADVERSARIAL STRESS TESTS PASSED CLEANLY!")
