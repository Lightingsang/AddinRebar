import subprocess
import json
import sys

proc = subprocess.Popen(
    [r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe'],
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

init_res = send_request(1, 'initialize', {
    'protocolVersion': '2024-11-05',
    'capabilities': {},
    'clientInfo': {'name': 'audit-client', 'version': '1.0'}
})

proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'method': 'notifications/initialized'}) + '\n')
proc.stdin.flush()

tools_res = send_request(2, 'tools/list', {})
resources_res = send_request(3, 'resources/list', {})
prompts_res = send_request(4, 'prompts/list', {})

proc.terminate()

tools = tools_res.get('result', {}).get('tools', [])
resources = resources_res.get('result', {}).get('resources', [])
prompts = prompts_res.get('result', {}).get('prompts', [])

print(f"Total Tools: {len(tools)}")
for t in sorted(tools, key=lambda x: x['name']):
    print(f"  - {t['name']}: {t.get('title', '')}")

print(f"\nTotal Resources: {len(resources)}")
for r in resources:
    print(f"  - {r['uri']}: {r.get('name', '')} ({r.get('mimeType', '')})")

print(f"\nTotal Prompts: {len(prompts)}")
for p in prompts:
    print(f"  - {p['name']}: {p.get('description', '')}")
