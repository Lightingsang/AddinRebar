import json
import os
import subprocess
import sys
import tempfile

def test_compile_all_seeds():
    print("=" * 80)
    print("PHASE 1: INDEPENDENT ROSLYN COMPILATION OF ALL 12 SEED TOOLS")
    print("=" * 80)

    csc = r"C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll"
    net48_dir = r"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
    tekla_dir = r"C:\Program Files\Tekla Structures\2025.0\bin"
    mcp_core = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core\bin\Release\net48\HPRebar.McpBridge.Core.dll"
    mcp_contracts = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Contracts\bin\Release\netstandard2.0\HPRebar.Mcp.Contracts.dll"

    ref_files = [
        os.path.join(net48_dir, "mscorlib.dll"),
        os.path.join(net48_dir, "System.dll"),
        os.path.join(net48_dir, "System.Core.dll"),
        os.path.join(net48_dir, "Microsoft.CSharp.dll"),
        os.path.join(tekla_dir, "Tekla.Structures.dll"),
        os.path.join(tekla_dir, "Tekla.Structures.Model.dll"),
        os.path.join(tekla_dir, "Tekla.Structures.Drawing.dll"),
        os.path.join(tekla_dir, "Tekla.Structures.Catalogs.dll"),
        os.path.join(tekla_dir, "Tekla.Structures.Datatype.dll"),
        mcp_core,
        mcp_contracts
    ]

    for r in ref_files:
        if not os.path.exists(r):
            print(f"ERROR: Missing reference assembly: {r}")
            return False

    seeds_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\Registry\SeedLibrary"

    tools = [
        ("Model", "get_model_info"),
        ("Model", "select_objects"),
        ("Property", "get_part_properties"),
        ("Geometry", "create_beam"),
        ("Geometry", "create_column"),
        ("Geometry", "create_contour_plate"),
        ("Rebar", "create_rebar_group"),
        ("Rebar", "create_single_rebar"),
        ("Property", "modify_user_properties"),
        ("Rebar", "get_reinforcement_info"),
        ("Drawing", "list_drawings"),
        ("Export", "export_ifc")
    ]

    wrapper_template = """using System;
using System.Linq;
using System.Collections.Generic;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Catalogs;
using HPRebar.McpBridge.Core.Scripting;

public class SeedHost
{{
    public Tekla.Structures.Model.Model model;
    public Tekla.Structures.Model.UI.ModelObjectSelector selector;
    public System.Threading.CancellationToken ct;
    public Action<string> log;
    public Action<int, int?, string?> progress;
    public ScriptArgs args;

    public object Run()
    {{
#line 1 "code.cs"
{code}
    }}
}}
"""

    all_passed = True
    compilation_results = {}

    with tempfile.TemporaryDirectory() as tmpdir:
        for cat, name in tools:
            code_path = os.path.join(seeds_dir, cat, name, "code.cs")
            if not os.path.exists(code_path):
                print(f"[MISSING] {cat}/{name}: {code_path}")
                all_passed = False
                continue

            with open(code_path, "r", encoding="utf-8") as f:
                code_text = f.read()

            cs_source = wrapper_template.format(code=code_text)
            src_path = os.path.join(tmpdir, f"{name}.cs")
            out_dll = os.path.join(tmpdir, f"{name}.dll")

            with open(src_path, "w", encoding="utf-8") as f:
                f.write(cs_source)

            cmd = [
                "dotnet", csc,
                "-target:library",
                f"-out:{out_dll}",
                "-nologo",
                "-langversion:latest",
                "-nowarn:CS0168,CS0219"
            ]
            for r in ref_files:
                cmd.append(f"-r:{r}")
            cmd.append(src_path)

            res = subprocess.run(cmd, capture_output=True, text=True)
            if res.returncode == 0:
                print(f"[COMPILES OK] {cat}/{name}")
                compilation_results[f"{cat}/{name}"] = "PASS"
            else:
                all_passed = False
                print(f"[COMPILE FAIL] {cat}/{name}")
                print(res.stdout)
                print(res.stderr)
                compilation_results[f"{cat}/{name}"] = "FAIL: " + res.stdout

    print(f"\nPhase 1 Result: {'PASS (12/12 Clean)' if all_passed else 'FAIL'}")
    return all_passed

def test_stdio_server(config="Release"):
    print("\n" + "=" * 80)
    print(f"PHASE 2: EMPIRICAL STDIO PROTOCOL VERIFICATION ({config} Build)")
    print("=" * 80)

    server_exe = rf"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\{config}\net10.0\HPTekla.Mcp.Server.exe"
    if not os.path.exists(server_exe):
        print(f"ERROR: Server binary not found: {server_exe}")
        return False

    proc = subprocess.Popen(
        [server_exe],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        bufsize=0
    )

    req_id = 0
    def send_rpc(method, params=None):
        nonlocal req_id
        req_id += 1
        req = {
            "jsonrpc": "2.0",
            "id": req_id,
            "method": method
        }
        if params is not None:
            req["params"] = params
        line = json.dumps(req)
        proc.stdin.write(line + "\n")
        proc.stdin.flush()

        while True:
            resp_line = proc.stdout.readline()
            if not resp_line:
                raise RuntimeError(f"Server closed connection unexpectedly on method {method}")
            resp_line = resp_line.strip()
            if not resp_line:
                continue
            try:
                msg = json.loads(resp_line)
                if msg.get("id") == req_id:
                    return msg
            except json.JSONDecodeError:
                pass

    def send_notif(method, params=None):
        req = {
            "jsonrpc": "2.0",
            "method": method
        }
        if params is not None:
            req["params"] = params
        line = json.dumps(req)
        proc.stdin.write(line + "\n")
        proc.stdin.flush()

    try:
        # 1. Initialize
        init_resp = send_rpc("initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "ForensicAuditor", "version": "1.0.0"}
        })
        print(f"Server Name: {init_resp.get('result', {}).get('serverInfo', {}).get('name')}")
        print(f"Server Version: {init_resp.get('result', {}).get('serverInfo', {}).get('version')}")
        send_notif("notifications/initialized")

        # 2. tools/list
        tools_resp = send_rpc("tools/list")
        tools = tools_resp.get("result", {}).get("tools", [])
        tool_names = set(t["name"] for t in tools)
        print(f"Total Tools Exposed: {len(tools)}")

        expected_core = {"execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution"}
        expected_meta = {"search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"}
        expected_seeds = {
            "get_model_info", "select_objects", "get_part_properties",
            "create_beam", "create_column", "create_contour_plate",
            "create_rebar_group", "create_single_rebar", "modify_user_properties",
            "get_reinforcement_info", "list_drawings", "export_ifc"
        }
        expected_all = expected_core | expected_meta | expected_seeds

        missing_core = expected_core - tool_names
        missing_meta = expected_meta - tool_names
        missing_seeds = expected_seeds - tool_names
        extra_tools = tool_names - expected_all

        print(f"Core Tools: {len(expected_core - missing_core)}/4 (Missing: {missing_core})")
        print(f"Meta Tools: {len(expected_meta - missing_meta)}/8 (Missing: {missing_meta})")
        print(f"Seed Tools: {len(expected_seeds - missing_seeds)}/12 (Missing: {missing_seeds})")
        print(f"Extra Tools: {extra_tools}")

        if len(tools) != 24 or missing_core or missing_meta or missing_seeds or extra_tools:
            print("ERROR: Tool surface mismatch!")
            return False

        # Validate tool schemas and descriptions
        for t in tools:
            name = t["name"]
            desc = t.get("description", "")
            schema = t.get("inputSchema", {})
            if not desc or len(desc.strip()) < 10:
                print(f"ERROR: Tool {name} has empty or trivial description")
                return False
            if schema.get("type") != "object":
                print(f"ERROR: Tool {name} schema type is not object")
                return False

        # 3. resources/list
        res_resp = send_rpc("resources/list")
        resources = res_resp.get("result", {}).get("resources", [])
        print(f"Total Resources Exposed: {len(resources)}")
        for r in resources:
            print(f"  - {r.get('uri')} ({r.get('name')})")

        # 4. prompts/list
        prompts_resp = send_rpc("prompts/list")
        prompts = prompts_resp.get("result", {}).get("prompts", [])
        print(f"Total Prompts Exposed: {len(prompts)}")
        for p in prompts:
            print(f"  - {p.get('name')}")

        # 5. Call get_tekla_context (bridge not connected test)
        call_resp = send_rpc("tools/call", {
            "name": "get_tekla_context",
            "arguments": {}
        })
        is_error = call_resp.get("result", {}).get("isError", False)
        content = call_resp.get("result", {}).get("content", [{}])[0].get("text", "")
        print(f"Disconnected get_tekla_context isError: {is_error}")
        print(f"Disconnected guidance snippet: {content[:100]}...")
        if not is_error or "hptekla-mcp-2025" not in content:
            print("ERROR: Expected graceful error guidance with pipe name hptekla-mcp-2025")
            return False

        print(f"Phase 2 ({config}) Result: PASS")
        return True

    finally:
        try:
            proc.stdin.close()
            proc.terminate()
            proc.wait(timeout=2)
        except Exception:
            pass

if __name__ == "__main__":
    p1 = test_compile_all_seeds()
    p2_rel = test_stdio_server("Release")
    p2_dbg = test_stdio_server("Debug")

    print("\n" + "=" * 80)
    print("FINAL FORENSIC AUDIT EMPIRICAL SUMMARY")
    print("=" * 80)
    print(f"1. Roslyn Compilation (12/12 seeds): {'PASS' if p1 else 'FAIL'}")
    print(f"2. MCP Stdio Protocol (Release):      {'PASS' if p2_rel else 'FAIL'}")
    print(f"3. MCP Stdio Protocol (Debug):        {'PASS' if p2_dbg else 'FAIL'}")

    if p1 and p2_rel and p2_dbg:
        print("\nOVERALL STATUS: 100% EMPIRICAL VERIFICATION SUCCEEDED")
        sys.exit(0)
    else:
        print("\nOVERALL STATUS: INTEGRITY / BEHAVIORAL FAILURE DETECTED")
        sys.exit(1)
