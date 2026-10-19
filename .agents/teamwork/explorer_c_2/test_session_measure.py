import os
import sys
import time
import json

HERE = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
MCP_SHARED_TOOLS = os.path.join(REPO_ROOT, "McpShared", "tools")
sys.path.insert(0, MCP_SHARED_TOOLS)

from harness_common import Server, utf8_console

utf8_console()

server_exe = os.path.join(REPO_ROOT, "HPDynamo", "HPDynamo.Mcp.Server", "bin", "Debug", "net10.0", "HPDynamo.Mcp.Server.exe")

s = Server(server_exe)
s.initialize()

# Testing search_tools (in-process SQLite registry lookup)
tool_name = "search_tools"
tool_args = {"query": "node"}

# Cold call
t0 = time.perf_counter()
cold_resp = s.call(tool_name, tool_args)
t1 = time.perf_counter()
cold_ms = round((t1 - t0) * 1000.0, 2)
req_bytes = len(json.dumps({"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": tool_name, "arguments": tool_args}}, ensure_ascii=False).encode('utf-8')) + 1
resp_bytes = len(json.dumps(cold_resp, ensure_ascii=False).encode('utf-8')) + 1

print(f"search_tools Cold call: {cold_ms} ms, reqBytes={req_bytes}, respBytes={resp_bytes}")

warm_samples = []
for i in range(10):
    t0 = time.perf_counter()
    resp = s.call(tool_name, tool_args)
    t1 = time.perf_counter()
    warm_samples.append(round((t1 - t0) * 1000.0, 2))

s.close()

sorted_samples = sorted(warm_samples)
p50 = sorted_samples[len(sorted_samples) // 2]
p95 = sorted_samples[int(len(sorted_samples) * 0.95)]
print(f"Warm samples: {warm_samples}")
print(f"Warm stats: min={min(warm_samples)} ms, max={max(warm_samples)} ms, p50={p50} ms, p95={p95} ms, mean={round(sum(warm_samples)/len(warm_samples), 2)} ms")
