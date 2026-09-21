import os
import sys
import json
import re
import subprocess
import tempfile

REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
SEEDS_DIR = os.path.join(REPO_ROOT, r"HPTekla\HPTekla.Mcp.Server\Registry\SeedLibrary")
SERVER_EXE = os.path.join(REPO_ROOT, r"HPTekla\HPTekla.Mcp.Server\bin\Release\net10.0\HPTekla.Mcp.Server.exe")

CSC_PATH = r"C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll"
NET48_DIR = r"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
TEKLA_BIN = r"C:\Program Files\Tekla Structures\2025.0\bin"
BRIDGE_TESTS_BIN = os.path.join(REPO_ROOT, r"HPTekla\HPTekla.McpBridge.Tests\bin\Debug\net48")
MCP_CORE_NET48 = os.path.join(REPO_ROOT, r"McpShared\HPRebar.McpBridge.Core\bin\Release\net48\HPRebar.McpBridge.Core.dll")
MCP_CONTRACTS_NET48 = os.path.join(REPO_ROOT, r"McpShared\HPRebar.Mcp.Contracts\bin\Release\net48\HPRebar.Mcp.Contracts.dll")

TOOLS = [
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

print("=" * 80)
print("EMPIRICAL ADVERSARIAL CHALLENGE SUITE — MILESTONE 3 (ITERATION 2)")
print("=" * 80)

failures = []

# ----------------------------------------------------------------------
# SUITE 1: Obsolete Identifiers & Defective Token Audit
# ----------------------------------------------------------------------
print("\n[SUITE 1] Scanning for obsolete / defective identifiers across all 12 seeds...")
prohibited_tokens = [
    (r"\bMath\s*\.\s*Clamp\b", "Math.Clamp (unavailable in net48)"),
    (r"\bModelObjectEnum\s*\.\s*REBAR\b", "ModelObjectEnum.REBAR (invalid enum member)"),
    (r"\bproj\s*\.\s*ProjectName\b", "proj.ProjectName (invalid property on ProjectInfo)"),
    (r"\bIFC2X3_COORDINATION_VIEW\b", "IFC2X3_COORDINATION_VIEW (invalid enum member)"),
    (r"\bIFC4_DESIGN_TRANSFER_VIEW\b", "IFC4_DESIGN_TRANSFER_VIEW (invalid enum member)"),
    (r"\bBasePointCurrentWorkPlane\b", "BasePointCurrentWorkPlane (invalid enum member)"),
    (r"\bIFCExportFlags\s*\.\s*None\b", "IFCExportFlags.None (invalid static member)"),
    (r"\bmodel\s*\.\s*CommitChanges\s*\(", "model.CommitChanges() (bridge owns transaction commit)"),
    (r"\bMessageBox\b", "MessageBox (modal UI dialog blocked by guard)"),
    (r"\bPicker\b", "Picker (interactive picker blocked by guard)"),
    (r"#r\s+", "#r directive (blocked by ScriptGuard)"),
    (r"#load\s+", "#load directive (blocked by ScriptGuard)"),
]

suite1_passed = True
for cat, name in TOOLS:
    code_path = os.path.join(SEEDS_DIR, cat, name, "code.cs")
    with open(code_path, "r", encoding="utf-8") as f:
        code = f.read()

    for pattern, desc in prohibited_tokens:
        matches = list(re.finditer(pattern, code))
        if matches:
            suite1_passed = False
            err = f"Seed {cat}/{name} contains prohibited token: {desc} (line {code[:matches[0].start()].count(chr(10))+1})"
            failures.append(err)
            print(f"  [FAIL] {err}")

if suite1_passed:
    print("  [PASS] All 12 seeds are 100% clean of obsolete tokens and defective APIs.")

# ----------------------------------------------------------------------
# SUITE 2: Roslyn Script Compilation (.NET 4.8 + Tekla 2025.0)
# ----------------------------------------------------------------------
print("\n[SUITE 2] Compiling all 12 seed scripts against Tekla 2025.0 on net48...")

ref_files = [
    os.path.join(NET48_DIR, "mscorlib.dll"),
    os.path.join(NET48_DIR, "System.dll"),
    os.path.join(NET48_DIR, "System.Core.dll"),
    os.path.join(NET48_DIR, "Microsoft.CSharp.dll"),
    os.path.join(TEKLA_BIN, "Tekla.Structures.dll"),
    os.path.join(TEKLA_BIN, "Tekla.Structures.Model.dll"),
    os.path.join(TEKLA_BIN, "Tekla.Structures.Drawing.dll"),
    os.path.join(TEKLA_BIN, "Tekla.Structures.Catalogs.dll"),
    os.path.join(TEKLA_BIN, "Tekla.Structures.Datatype.dll"),
    MCP_CORE_NET48,
    MCP_CONTRACTS_NET48
]

for rf in ref_files:
    if not os.path.exists(rf):
        print(f"  [FATAL] Reference assembly missing: {rf}")
        sys.exit(1)

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

suite2_passed = True
with tempfile.TemporaryDirectory() as tmpdir:
    for cat, name in TOOLS:
        code_path = os.path.join(SEEDS_DIR, cat, name, "code.cs")
        with open(code_path, "r", encoding="utf-8") as f:
            code_text = f.read()

        cs_source = wrapper_template.format(code=code_text)
        src_path = os.path.join(tmpdir, f"{name}.cs")
        out_dll = os.path.join(tmpdir, f"{name}.dll")

        with open(src_path, "w", encoding="utf-8") as f:
            f.write(cs_source)

        cmd = [
            "dotnet", CSC_PATH,
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
            print(f"  [PASS] {cat}/{name} compiled cleanly.")
        else:
            suite2_passed = False
            err = f"Seed {cat}/{name} compilation failed: {res.stdout.strip()}"
            failures.append(err)
            print(f"  [FAIL] {cat}/{name} compilation failed!")
            print(res.stdout)

# ----------------------------------------------------------------------
# SUITE 3: AST Guard Compliance & Adversarial Stress Test (ScriptGuard.Check)
# ----------------------------------------------------------------------
print("\n[SUITE 3] Executing ScriptGuard.Check with GuardProfile.Tekla and adversarial attacks...")

guard_runner_source = """using System;
using System.IO;
using System.Reflection;
using System.Linq;

public class Program
{
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name + ".dll";
            var dirs = new[]
            {
                @"g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPTekla\\HPTekla.McpBridge.Tests\\bin\\Debug\\net48",
                @"C:\\Program Files\\dotnet\\sdk\\10.0.400\\Roslyn\\bincore",
                @"C:\\Program Files\\Tekla Structures\\2025.0\\bin",
                @"g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\McpShared\\HPRebar.McpBridge.Core\\bin\\Release\\net48",
                @"g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\McpShared\\HPRebar.Mcp.Contracts\\bin\\Release\\net48"
            };
            foreach (var d in dirs)
            {
                var p = Path.Combine(d, name);
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            return null;
        };

        return RunInternal(args);
    }

    private static int RunInternal(string[] args)
    {
        string seedsDir = args[0];
        string[] toolDirs = args.Skip(1).ToArray();

        int failCount = 0;

        Console.WriteLine("--- Testing 12 Seed Scripts with GuardProfile.Tekla ---");
        foreach (var relPath in toolDirs)
        {
            string codePath = Path.Combine(seedsDir, relPath, "code.cs");
            string code = File.ReadAllText(codePath);
            var diags = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(code, HPRebar.McpBridge.Core.Scripting.GuardProfile.Tekla);
            if (diags.Count > 0)
            {
                failCount++;
                Console.WriteLine($"[GUARD FAIL] {relPath}: {diags.Count} violations");
                foreach (var d in diags)
                {
                    Console.WriteLine($"   {d.Id}: {d.Message} (Line {d.Line})");
                }
            }
            else
            {
                Console.WriteLine($"  [GUARD PASS] {relPath}");
            }
        }

        Console.WriteLine("\\n--- Adversarial Attack Scenarios (Must be BLOCKED) ---");
        var attacks = new (string Name, string Code)[]
        {
            ("Picker.PickPoint", "var p = new Tekla.Structures.Model.UI.Picker(); p.PickPoint(); return 1;"),
            ("MessageBox.Show", "System.Windows.Forms.MessageBox.Show(\\\"test\\\"); return 1;"),
            ("Process.Start", "System.Diagnostics.Process.Start(\\\"calc.exe\\\"); return 1;"),
            ("File.Delete", "System.IO.File.Delete(\\\"c:\\\\\\\\temp\\\\\\\\test.txt\\\"); return 1;"),
            ("global::System.IO", "global::System.IO.File.ReadAllText(\\\"c:\\\\\\\\temp\\\\\\\\test.txt\\\"); return 1;"),
            ("#r directive", "#r \\\"SomeDll.dll\\\"\\nreturn 1;"),
            ("#load directive", "#load \\\"SomeScript.csx\\\"\\nreturn 1;"),
            ("model.CommitChanges", "model.CommitChanges(); return 1;"),
            ("(model).CommitChanges", "(model).CommitChanges(); return 1;"),
            ("Tekla.Structures.Dialog", "var d = new Tekla.Structures.Dialog.PluginDialogForm(); return 1;")
        };

        foreach (var (attName, attCode) in attacks)
        {
            var diags = HPRebar.McpBridge.Core.Scripting.ScriptGuard.Check(attCode, HPRebar.McpBridge.Core.Scripting.GuardProfile.Tekla);
            if (diags.Count == 0)
            {
                failCount++;
                Console.WriteLine($"  [ATTACK BYPASSED GUARD!] Scenario: {attName}");
            }
            else
            {
                Console.WriteLine($"  [ATTACK BLOCKED OK] Scenario: {attName} -> {diags[0].Message}");
            }
        }

        return failCount;
    }
}
"""

suite3_passed = True
with tempfile.TemporaryDirectory() as tmpdir:
    runner_src = os.path.join(tmpdir, "GuardRunner.cs")
    runner_exe = os.path.join(tmpdir, "GuardRunner.exe")
    with open(runner_src, "w", encoding="utf-8") as f:
        f.write(guard_runner_source)

    roslyn_dir = r"C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore"
    cmd = [
        "dotnet", CSC_PATH,
        "-target:exe",
        f"-out:{runner_exe}",
        "-nologo",
        "-langversion:latest",
        f"-r:{os.path.join(NET48_DIR, 'mscorlib.dll')}",
        f"-r:{os.path.join(NET48_DIR, 'System.dll')}",
        f"-r:{os.path.join(NET48_DIR, 'System.Core.dll')}",
        f"-r:{MCP_CORE_NET48}",
        f"-r:{MCP_CONTRACTS_NET48}",
        f"-r:{os.path.join(roslyn_dir, 'Microsoft.CodeAnalysis.dll')}",
        f"-r:{os.path.join(roslyn_dir, 'Microsoft.CodeAnalysis.CSharp.dll')}",
        runner_src
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        print("  [FATAL] Failed to build GuardRunner:", res.stdout, res.stderr)
        sys.exit(1)

    runner_args = [runner_exe, SEEDS_DIR]
    for cat, name in TOOLS:
        runner_args.append(f"{cat}\\{name}")

    res = subprocess.run(runner_args, capture_output=True, text=True)
    print(res.stdout)
    if res.returncode != 0:
        suite3_passed = False
        failures.append(f"GuardRunner reported {res.returncode} violations or bypasses")

# ----------------------------------------------------------------------
# SUITE 4: Schema & Examples & Parameter Extraction
# ----------------------------------------------------------------------
print("\n[SUITE 4] Verifying tool.json, examples.json, and parameter binding...")
suite4_passed = True

def validate_type(val, expected_type):
    if expected_type == "string":
        return isinstance(val, str)
    elif expected_type == "number":
        return isinstance(val, (int, float)) and not isinstance(val, bool)
    elif expected_type == "integer":
        return isinstance(val, int) and not isinstance(val, bool)
    elif expected_type == "boolean":
        return isinstance(val, bool)
    elif expected_type == "array":
        return isinstance(val, list)
    elif expected_type == "object":
        return isinstance(val, dict)
    return True

for cat, name in TOOLS:
    t_dir = os.path.join(SEEDS_DIR, cat, name)
    f_tool = os.path.join(t_dir, "tool.json")
    f_ex = os.path.join(t_dir, "examples.json")
    f_code = os.path.join(t_dir, "code.cs")

    with open(f_tool, "r", encoding="utf-8") as f:
        t_data = json.load(f)
    with open(f_ex, "r", encoding="utf-8") as f:
        e_data = json.load(f)
    with open(f_code, "r", encoding="utf-8") as f:
        c_text = f.read()

    # Verify tool metadata
    if t_data.get("name") != name:
        suite4_passed = False
        failures.append(f"{cat}/{name}: tool.json name mismatch")
    if t_data.get("host") != "tekla":
        suite4_passed = False
        failures.append(f"{cat}/{name}: tool.json host is not 'tekla'")
    if t_data.get("hostVersions") != ["2025"]:
        suite4_passed = False
        failures.append(f"{cat}/{name}: hostVersions is not ['2025']")
    if t_data.get("category") != cat:
        suite4_passed = False
        failures.append(f"{cat}/{name}: category mismatch")

    schema = t_data.get("inputSchema", {})
    props = schema.get("properties", {})
    req = schema.get("required", [])

    # Verify examples
    if not isinstance(e_data, list) or len(e_data) < 2:
        suite4_passed = False
        failures.append(f"{cat}/{name}: examples.json has fewer than 2 examples")

    for idx, ex in enumerate(e_data):
        ex_args = ex.get("args") if "args" in ex else ex
        for r in req:
            if r not in ex_args:
                suite4_passed = False
                failures.append(f"{cat}/{name}: Example {idx} missing required prop '{r}'")
        for k, v in ex_args.items():
            if k in props:
                exp_t = props[k].get("type")
                if exp_t and not validate_type(v, exp_t):
                    suite4_passed = False
                    failures.append(f"{cat}/{name}: Example {idx} prop '{k}' type mismatch")

    # Verify code argument accesses match declared props
    arg_calls = re.findall(r'args\s*\.\s*(?:Str|Int|Double|Bool|List|Obj(?:<[^>]+>)?)\s*\(\s*"([^"]+)"', c_text)
    for ac in arg_calls:
        if ac not in props:
            suite4_passed = False
            failures.append(f"{cat}/{name}: code accesses undeclared arg '{ac}'")

    print(f"  [PASS] {cat}/{name}: tool.json ({len(props)} props, {len(req)} req), {len(e_data)} examples, {len(arg_calls)} code args matched.")

# ----------------------------------------------------------------------
# SUITE 5: Stdio MCP Protocol Handshake & Tools Listing (24 Tools)
# ----------------------------------------------------------------------
print("\n[SUITE 5] Verifying HPTekla.Mcp.Server over stdio JSON-RPC...")
suite5_passed = True

def run_stdio_test():
    proc = subprocess.Popen(
        [SERVER_EXE],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8"
    )

    def send_request(req_id, method, params=None):
        payload = {"jsonrpc": "2.0", "id": req_id, "method": method}
        if params is not None:
            payload["params"] = params
        proc.stdin.write(json.dumps(payload) + "\n")
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
                if msg.get("id") == req_id:
                    return msg
            except json.JSONDecodeError:
                pass

    # Step 1: Initialize
    init_resp = send_request(1, "initialize", {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {"name": "ChallengerTestClient", "version": "1.0.0"}
    })
    if not init_resp or "result" not in init_resp:
        proc.kill()
        return False, f"Initialize failed: {init_resp}"

    server_info = init_resp["result"].get("serverInfo", {})
    print(f"  [HANDSHAKE] Server: {server_info.get('name')} v{server_info.get('version')}")

    # Step 2: notifications/initialized
    proc.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
    proc.stdin.flush()

    # Step 3: tools/list
    tools_resp = send_request(2, "tools/list", {})
    if not tools_resp or "result" not in tools_resp:
        proc.kill()
        return False, f"tools/list failed: {tools_resp}"

    tools_list = tools_resp["result"].get("tools", [])
    print(f"  [DISCOVERY] Discovered {len(tools_list)} tools over stdio.")

    # Check tool count
    if len(tools_list) != 24:
        proc.kill()
        return False, f"Expected 24 tools, found {len(tools_list)}"

    # Check 4 Core Tools
    core_tools = {"execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution"}
    discovered_names = {t["name"] for t in tools_list}
    missing_core = core_tools - discovered_names
    if missing_core:
        proc.kill()
        return False, f"Missing core tools: {missing_core}"

    # Check 12 Seed Tools
    expected_seeds = {name for _, name in TOOLS}
    missing_seeds = expected_seeds - discovered_names
    if missing_seeds:
        proc.kill()
        return False, f"Missing seed tools: {missing_seeds}"

    # Check 8 Registry Meta Tools
    expected_meta = len(tools_list) - len(core_tools) - len(expected_seeds)
    if expected_meta != 8:
        proc.kill()
        return False, f"Expected 8 meta tools, found {expected_meta}"

    # Step 4: resources/list
    res_resp = send_request(3, "resources/list", {})
    res_count = len(res_resp.get("result", {}).get("resources", [])) if res_resp else 0

    # Step 5: prompts/list
    prompts_resp = send_request(4, "prompts/list", {})
    prompts_count = len(prompts_resp.get("result", {}).get("prompts", [])) if prompts_resp else 0

    print(f"  [DISCOVERY] Discovered {res_count} resources and {prompts_count} prompts.")

    proc.stdin.close()
    proc.terminate()
    proc.wait(timeout=3)
    return True, None

stdio_ok, stdio_err = run_stdio_test()
if not stdio_ok:
    suite5_passed = False
    failures.append(f"Stdio test failed: {stdio_err}")
    print(f"  [FAIL] {stdio_err}")
else:
    print("  [PASS] Stdio handshake and tool discovery verified (24/24 tools present).")

# ----------------------------------------------------------------------
# FINAL SUMMARY
# ----------------------------------------------------------------------
print("\n" + "=" * 80)
if failures:
    print(f"VERDICT: REQUEST_CHANGES — {len(failures)} failures detected:")
    for f in failures:
        print(f"  - {f}")
else:
    print("VERDICT: APPROVE — ALL 5 SUITES PASSED 100% WITH 0 ERRORS")
print("=" * 80)
