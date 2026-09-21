import json
import subprocess
import sys

exe = r"HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe"
harness = r"McpShared/tools/mcp-call.py"

def call(method, *args):
    cmd = [sys.executable, "-X", "utf8", harness, exe, method] + list(args)
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if res.returncode != 0:
        return {"error": res.stderr}
    try:
        return json.loads(res.stdout)
    except Exception as e:
        return {"raw": res.stdout, "err": str(e)}

print("=== 1. Adversarial: Call non-existent tool ===")
res_invalid = call("tools/call", "non_existent_tool_xyz", "{}")
print("Response:", json.dumps(res_invalid, indent=2))
assert "error" in res_invalid or (res_invalid.get("result", {}).get("isError") is True), "Expected error on invalid tool"
print("PASS: Non-existent tool cleanly returned error\n")

print("=== 2. Adversarial: Call get_robot_context without bridge ===")
res_ctx = call("tools/call", "get_robot_context", "{}")
print("Response:", json.dumps(res_ctx, indent=2))
assert "error" in res_ctx or (res_ctx.get("result", {}).get("isError") is True), "Expected error when bridge is not running"
print("PASS: get_robot_context handled offline bridge gracefully\n")

print("=== 3. Adversarial: Call seed get_model_info without bridge ===")
res_seed = call("tools/call", "get_model_info", "{}")
print("Response:", json.dumps(res_seed, indent=2))
assert "error" in res_seed or (res_seed.get("result", {}).get("isError") is True), "Expected error when bridge is not running"
print("PASS: seed tool handled offline bridge gracefully\n")

print("=== 4. Test prompts/get robot_analysis_template ===")
res_prompt = call("prompts/get", json.dumps({"name": "robot_analysis_template", "arguments": {"task": "verify truss reactions"}}))
print("Response:", json.dumps(res_prompt, indent=2))
messages = res_prompt.get("result", {}).get("messages", [])
assert len(messages) > 0, "Expected non-empty prompt messages"
print(f"PASS: prompt template returned {len(messages)} message(s)\n")

print("=== 5. Deep schema validation across all 24 tools ===")
tools_res = call("tools/list")
tools = tools_res.get("result", {}).get("tools", [])
assert len(tools) == 24, f"Expected 24 tools, got {len(tools)}"

for t in tools:
    name = t.get("name")
    assert name, "Tool must have name"
    desc = t.get("description", "")
    assert len(desc) >= 10, f"Tool {name} description too short ({len(desc)})"
    schema = t.get("inputSchema", {})
    assert schema.get("type") == "object", f"Tool {name} inputSchema must be type=object"
    print(f"  [OK] Tool {name}: description length {len(desc)}, schema valid")

print("\nALL ADVERSARIAL PROTOCOL TESTS PASSED!")
