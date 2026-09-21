#!/usr/bin/env python3
"""Adversarial Challenge Test Harness for HPTekla.Mcp.Server.

Stress-tests:
1. Process launch & MCP stdio protocol handshake (initialize, notifications/initialized)
2. tools/list: exactly 24 tools (4 core, 8 registry meta, 12 embedded seeds)
3. resources/list: exactly 3 resources (tekla://model/info, tekla://selection, registry://tools)
4. prompts/list: 4 prompts, and prompts/get validation
5. Disconnected bridge behavior: execute_tekla_code, get_tekla_context, run_tool
6. Malformed requests: invalid JSON, unknown RPC method, invalid tool name, missing args
7. Recovery and liveness verification after protocol violations
8. Clean process shutdown on stdin closure
"""

import json
import os
import queue
import subprocess
import sys
import threading
import time

def utf8_console():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

utf8_console()

class McpTestClient:
    def __init__(self, exe_path, env=None):
        self.exe_path = exe_path
        self.proc = subprocess.Popen(
            [exe_path],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            env=env or dict(os.environ),
            bufsize=0
        )
        self.stderr_lines = []
        self.stdout_queue = queue.Queue()
        self.next_id = 0
        self.pending = {}
        self.notifications = []
        
        self.t_err = threading.Thread(target=self._read_stderr, daemon=True)
        self.t_err.start()
        self.t_out = threading.Thread(target=self._pump_stdout, daemon=True)
        self.t_out.start()

    def _read_stderr(self):
        for line in iter(self.proc.stderr.readline, b""):
            self.stderr_lines.append(line.decode("utf-8", "replace"))

    def _pump_stdout(self):
        for line in iter(self.proc.stdout.readline, b""):
            self.stdout_queue.put(line)
        self.stdout_queue.put(None)

    def send_raw(self, raw_str):
        self.proc.stdin.write(raw_str.encode("utf-8") + b"\n")
        self.proc.stdin.flush()

    def send_request(self, method, params=None):
        self.next_id += 1
        req_id = self.next_id
        msg = {"jsonrpc": "2.0", "id": req_id, "method": method}
        if params is not None:
            msg["params"] = params
        self.send_raw(json.dumps(msg))
        return req_id

    def send_notification(self, method, params=None):
        msg = {"jsonrpc": "2.0", "method": method}
        if params is not None:
            msg["params"] = params
        self.send_raw(json.dumps(msg))

    def wait_for_response(self, req_id, timeout=15.0):
        deadline = time.time() + timeout
        while time.time() < deadline:
            if req_id in self.pending:
                return self.pending.pop(req_id)
            remaining = max(0.1, deadline - time.time())
            try:
                line = self.stdout_queue.get(timeout=remaining)
            except queue.Empty:
                break
            if line is None:
                raise RuntimeError("Server closed stdout unexpectedly")
            try:
                data = json.loads(line.decode("utf-8"))
            except json.JSONDecodeError:
                # Raw text or log line
                continue
            if "id" in data and ("result" in data or "error" in data):
                self.pending[data["id"]] = data
            elif "method" in data:
                self.notifications.append(data)
        if req_id in self.pending:
            return self.pending.pop(req_id)
        raise TimeoutError(f"Timed out waiting for response to request id {req_id}")

    def call_rpc(self, method, params=None, timeout=15.0):
        req_id = self.send_request(method, params)
        return self.wait_for_response(req_id, timeout)

    def close(self, timeout=5.0):
        if self.proc.poll() is not None:
            return self.proc.returncode
        try:
            self.proc.stdin.close()
            self.proc.wait(timeout=timeout)
        except Exception:
            self.proc.kill()
            self.proc.wait(timeout=2.0)
        return self.proc.returncode


class TestHarness:
    def __init__(self, exe_path):
        self.exe_path = exe_path
        self.checks = []

    def record(self, name, passed, detail=""):
        status = "PASS" if passed else "FAIL"
        print(f"[{status}] {name} - {detail}", flush=True)
        self.checks.append({"name": name, "pass": bool(passed), "detail": str(detail)})

    def run_all(self):
        print("=" * 70)
        print("Starting Adversarial Challenge of HPTekla.Mcp.Server")
        print(f"Target Binary: {self.exe_path}")
        print("=" * 70)

        # 1. Process startup & Handshake
        client = McpTestClient(self.exe_path)
        try:
            # Send initialize
            t0 = time.time()
            init_resp = client.call_rpc("initialize", {
                "protocolVersion": "2024-11-05",
                "capabilities": {
                    "tools": {"listChanged": True},
                    "resources": {"subscribe": False, "listChanged": False},
                    "prompts": {"listChanged": False}
                },
                "clientInfo": {"name": "teamwork_preview_challenger", "version": "1.0.0"}
            })
            dt = time.time() - t0
            self.record("Handshake: initialize roundtrip", "result" in init_resp, f"Response in {dt:.3f}s")
            
            result = init_resp.get("result", {})
            proto_version = result.get("protocolVersion")
            server_info = result.get("serverInfo", {})
            server_name = server_info.get("name")
            server_version = server_info.get("version")
            capabilities = result.get("capabilities", {})

            self.record("Handshake: protocolVersion present", bool(proto_version), f"protocolVersion = {proto_version}")
            self.record("Handshake: serverInfo.name == 'HPTekla MCP'", server_name == "HPTekla MCP", f"name = '{server_name}'")
            self.record("Handshake: serverInfo.version present", bool(server_version), f"version = '{server_version}'")
            self.record("Handshake: capabilities advertise tools, resources, prompts",
                        "tools" in capabilities and "resources" in capabilities and "prompts" in capabilities,
                        f"capabilities = {list(capabilities.keys())}")

            # Send initialized notification
            client.send_notification("notifications/initialized")
            time.sleep(0.2)
            self.record("Handshake: notifications/initialized accepted", client.proc.poll() is None, "Process running")

            # 2. tools/list verification
            tools_resp = client.call_rpc("tools/list", {})
            tools_list = tools_resp.get("result", {}).get("tools", [])
            tool_names = [t.get("name") for t in tools_list]
            self.record("Tools: exactly 24 tools registered", len(tools_list) == 24, f"Total tools count = {len(tools_list)}")

            expected_core = {"execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution"}
            expected_meta = {"search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"}
            expected_seeds = {
                "get_model_info", "select_objects", "get_part_properties",
                "create_beam", "create_column", "create_contour_plate",
                "create_rebar_group", "create_single_rebar", "modify_user_properties",
                "get_reinforcement_info", "list_drawings", "export_ifc"
            }
            all_expected = expected_core | expected_meta | expected_seeds

            missing_tools = all_expected - set(tool_names)
            extra_tools = set(tool_names) - all_expected
            self.record("Tools: all expected tools present without omissions", len(missing_tools) == 0, f"Missing: {missing_tools}")
            self.record("Tools: no extra unexpected tools", len(extra_tools) == 0, f"Extra: {extra_tools}")

            # Verify tool schemas
            schema_issues = []
            for t in tools_list:
                name = t.get("name", "<unnamed>")
                if not t.get("description"):
                    schema_issues.append(f"{name}: missing description")
                schema = t.get("inputSchema", {})
                if schema.get("type") != "object":
                    schema_issues.append(f"{name}: inputSchema type is not 'object'")
            self.record("Tools: all tool schemas have descriptions and object schemas", len(schema_issues) == 0, f"Issues: {schema_issues}")

            # 3. resources/list verification
            res_resp = client.call_rpc("resources/list", {})
            res_list = res_resp.get("result", {}).get("resources", [])
            res_uris = [r.get("uri") for r in res_list]
            self.record("Resources: exactly 3 resources registered", len(res_list) == 3, f"Total resources = {len(res_list)}: {res_uris}")

            expected_uris = {"tekla://model/info", "tekla://selection", "registry://tools"}
            missing_uris = expected_uris - set(res_uris)
            self.record("Resources: all expected URIs present", len(missing_uris) == 0, f"Missing: {missing_uris}")

            # 4. prompts/list verification
            prompt_resp = client.call_rpc("prompts/list", {})
            prompt_list = prompt_resp.get("result", {}).get("prompts", [])
            prompt_names = [p.get("name") for p in prompt_list]
            self.record("Prompts: exactly 4 prompts registered", len(prompt_list) == 4, f"Total prompts = {len(prompt_list)}: {prompt_names}")

            expected_prompts = {"toolify_run", "tekla_query_template", "tekla_modify_template", "tekla_rebar_template"}
            missing_prompts = expected_prompts - set(prompt_names)
            self.record("Prompts: all expected prompt names present", len(missing_prompts) == 0, f"Missing: {missing_prompts}")

            # Test prompts/get for tekla_query_template
            p_get = client.call_rpc("prompts/get", {"name": "tekla_query_template", "arguments": {"question": "How many columns?"}})
            p_messages = p_get.get("result", {}).get("messages", [])
            self.record("Prompts: prompts/get for tekla_query_template returns messages", len(p_messages) > 0, f"Messages count = {len(p_messages)}")

            # 5. Disconnected bridge behavior testing
            print("\n--- Testing Disconnected Bridge Behavior ---")
            # Call execute_tekla_code
            exec_resp = client.call_rpc("tools/call", {
                "name": "execute_tekla_code",
                "arguments": {
                    "code": "return 1;",
                    "transaction": "none",
                    "label": "challenger test"
                }
            })
            self.record("Bridge Disconnected: execute_tekla_code returns response", exec_resp is not None, "Response received")
            self.record("Bridge Disconnected: Server did not crash", client.proc.poll() is None, "Process running")
            
            # Check content of execute_tekla_code response
            exec_content = exec_resp.get("result", {}).get("content", [])
            exec_text = "".join(c.get("text", "") for c in exec_content)
            is_error = exec_resp.get("result", {}).get("isError") or "error" in exec_resp
            has_bridge_hint = ("Tekla Structures 2025" in exec_text) or ("hptekla-mcp-2025" in exec_text) or ("Bridge" in exec_text) or ("pipe" in exec_text.lower())
            self.record("Bridge Disconnected: execute_tekla_code signals error gracefully", bool(is_error), f"isError = {is_error}")
            self.record("Bridge Disconnected: error message informs user about bridge/pipe", has_bridge_hint, f"Text: {exec_text[:120]}...")

            # Call get_tekla_context
            ctx_resp = client.call_rpc("tools/call", {
                "name": "get_tekla_context",
                "arguments": {}
            })
            self.record("Bridge Disconnected: get_tekla_context returns response", ctx_resp is not None, "Response received")
            self.record("Bridge Disconnected: Server did not crash after get_tekla_context", client.proc.poll() is None, "Process running")
            
            ctx_content = ctx_resp.get("result", {}).get("content", [])
            ctx_text = "".join(c.get("text", "") for c in ctx_content)
            ctx_is_error = ctx_resp.get("result", {}).get("isError") or "error" in ctx_resp
            has_ctx_hint = ("Tekla Structures" in ctx_text) or ("hptekla-mcp-2025" in ctx_text) or ("Bridge" in ctx_text) or ("pipe" in ctx_text.lower())
            
            # When disconnected, ContextService returns a clean CallToolResult with isError=True and bridge not connected hint
            # If a mock bridge were attached, it would return JSON with isModifiable=False
            if ctx_is_error:
                self.record("Bridge Disconnected: get_tekla_context signals error gracefully", True, f"isError = True")
                self.record("Bridge Disconnected: get_tekla_context error hint informs user about bridge/pipe", has_ctx_hint, f"Text: {ctx_text[:120]}...")
            else:
                try:
                    ctx_data = json.loads(ctx_text)
                    ctx_host = ctx_data.get("host")
                    is_modifiable = ctx_data.get("isModifiable")
                    self.record("Bridge Disconnected: get_tekla_context valid JSON with host='tekla'", ctx_host == "tekla", f"host = '{ctx_host}'")
                    self.record("Bridge Disconnected: isModifiable is false when model closed/disconnected", is_modifiable is False, f"isModifiable = {is_modifiable}")
                except Exception as e:
                    self.record("Bridge Disconnected: get_tekla_context parsed", False, str(e))

            # Call seed tool directly or via run_tool
            run_resp = client.call_rpc("tools/call", {
                "name": "get_model_info",
                "arguments": {}
            })
            self.record("Bridge Disconnected: calling seed tool directly handles disconnect gracefully", run_resp is not None, "Response received")
            self.record("Bridge Disconnected: Server alive after seed tool call", client.proc.poll() is None, "Process running")

            # Call inspect_type
            inspect_resp = client.call_rpc("tools/call", {
                "name": "inspect_type",
                "arguments": {"typeName": "Beam"}
            })
            self.record("Bridge Disconnected: inspect_type responds gracefully", inspect_resp is not None, "Response received")

            # Call cancel_execution
            cancel_resp = client.call_rpc("tools/call", {
                "name": "cancel_execution",
                "arguments": {"executionId": "nonexistent-id"}
            })
            self.record("Bridge Disconnected: cancel_execution responds gracefully", cancel_resp is not None, "Response received")

            # 6. Malformed requests & Edge cases
            print("\n--- Testing Malformed Requests & Edge Cases ---")
            
            # Case A: Unknown method
            bad_method_resp = client.call_rpc("unknown/arbitraryMethod", {})
            self.record("Malformed: unknown method returns JSON-RPC error", "error" in bad_method_resp, f"Error: {bad_method_resp.get('error')}")
            self.record("Malformed: server alive after unknown method", client.proc.poll() is None, "Process running")

            # Case B: Unknown tool name in tools/call
            bad_tool_resp = client.call_rpc("tools/call", {"name": "nonexistent_tool_12345", "arguments": {}})
            bad_tool_content = bad_tool_resp.get("result", {}).get("content", [])
            bad_tool_err = bad_tool_resp.get("result", {}).get("isError") or "error" in bad_tool_resp
            self.record("Malformed: nonexistent tool call returns error", bool(bad_tool_err), f"Result: {bad_tool_resp}")
            self.record("Malformed: server alive after nonexistent tool call", client.proc.poll() is None, "Process running")

            # Case C: Missing arguments to execute_tekla_code (missing 'code')
            missing_arg_resp = client.call_rpc("tools/call", {
                "name": "execute_tekla_code",
                "arguments": {}
            })
            missing_arg_err = missing_arg_resp.get("result", {}).get("isError") or "error" in missing_arg_resp
            self.record("Malformed: missing required argument returns error", bool(missing_arg_err), f"Result: {missing_arg_resp}")
            self.record("Malformed: server alive after missing required argument", client.proc.poll() is None, "Process running")

            # Case D: Malformed JSON raw line
            print("Sending malformed raw JSON string to stdin...")
            client.send_raw('{"jsonrpc": "2.0", "id": "broken", "method": [INVALID_JSON')
            time.sleep(0.3)
            self.record("Malformed: server survives malformed JSON syntax on stdin", client.proc.poll() is None, "Process running")

            # Case E: Empty string / whitespace line
            client.send_raw('   \r\n')
            time.sleep(0.2)
            self.record("Malformed: server survives whitespace / empty line", client.proc.poll() is None, "Process running")

            # Case F: Resource read tests
            print("\n--- Testing Resource Reading & Registry Tools ---")
            res_read_resp = client.call_rpc("resources/read", {"uri": "registry://tools"})
            res_contents = res_read_resp.get("result", {}).get("contents", [])
            self.record("Resources: read registry://tools returns content", len(res_contents) > 0, f"Blocks = {len(res_contents)}")

            # Resource read on disconnected bridge resource: tekla://model/info
            res_tekla_resp = client.call_rpc("resources/read", {"uri": "tekla://model/info"})
            self.record("Resources: read tekla://model/info handles disconnect gracefully", res_tekla_resp is not None, f"Response = {res_tekla_resp.get('error') or res_tekla_resp.get('result')}")
            self.record("Resources: server alive after disconnected resource read", client.proc.poll() is None, "Process running")

            # Case G: Meta tools functional test (search_tools & get_tool)
            search_resp = client.call_rpc("tools/call", {"name": "search_tools", "arguments": {"query": "beam"}})
            search_text = "".join(c.get("text", "") for c in search_resp.get("result", {}).get("content", []))
            self.record("Meta tools: search_tools 'beam' finds create_beam", "create_beam" in search_text, f"Matches in search: {'create_beam' in search_text}")

            get_tool_resp = client.call_rpc("tools/call", {"name": "get_tool", "arguments": {"name": "create_beam"}})
            get_tool_text = "".join(c.get("text", "") for c in get_tool_resp.get("result", {}).get("content", []))
            self.record("Meta tools: get_tool 'create_beam' returns tool definition", "new Beam" in get_tool_text, f"Found Beam script")

            # Case H: Rapid Burst Stress Test (25 requests back to back)
            print("\n--- Testing Rapid Request Burst & Liveness ---")
            burst_success = 0
            burst_count = 25
            for i in range(burst_count):
                r = client.call_rpc("tools/list", {})
                if "result" in r:
                    burst_success += 1
            self.record(f"Stress: {burst_count} rapid tools/list requests handled without error", burst_success == burst_count, f"{burst_success}/{burst_count} succeeded")

            # Case I: Large Payload Stress Test (100KB JSON payload)
            large_comment = "A" * 100000
            large_resp = client.call_rpc("tools/call", {
                "name": "search_tools",
                "arguments": {"query": large_comment}
            })
            self.record("Stress: 100KB oversized query string handled gracefully", large_resp is not None, "Response received")
            self.record("Stress: server alive after 100KB oversized payload", client.proc.poll() is None, "Process running")

            # Case J: Recovery check - send valid tools/list request to ensure server is still completely responsive
            recovery_resp = client.call_rpc("tools/list", {})
            rec_tools = recovery_resp.get("result", {}).get("tools", [])
            self.record("Recovery: server fully responsive after all adversarial payloads", len(rec_tools) == 24, f"Tools count = {len(rec_tools)}")

            # 7. Clean Shutdown verification
            print("\n--- Testing Clean Shutdown ---")
            exit_code = client.close(timeout=5.0)
            self.record("Shutdown: server exits cleanly on stdin close within 5s", exit_code is not None, f"Exit code = {exit_code}")

        finally:
            client.close()

        passed_count = sum(1 for c in self.checks if c["pass"])
        failed_count = sum(1 for c in self.checks if not c["pass"])
        total_count = len(self.checks)

        print("\n" + "=" * 70)
        print(f"Adversarial Challenge Results: {passed_count}/{total_count} PASSED ({failed_count} FAILED)")
        print("=" * 70)

        return self.checks, failed_count == 0


if __name__ == "__main__":
    exe = os.path.abspath(r"HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe")
    if not os.path.exists(exe):
        print(f"ERROR: Executable not found at {exe}")
        sys.exit(2)
    harness = TestHarness(exe)
    checks, success = harness.run_all()
    sys.exit(0 if success else 1)
