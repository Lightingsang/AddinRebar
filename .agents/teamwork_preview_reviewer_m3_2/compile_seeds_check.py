import os
import subprocess
import tempfile

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

print(f"Testing compilation of all {len(tools)} seed tools against Tekla Structures 2025.0 assemblies...")

all_passed = True

with tempfile.TemporaryDirectory() as tmpdir:
    for cat, name in tools:
        code_path = os.path.join(seeds_dir, cat, name, "code.cs")
        with open(code_path, "r", encoding="utf-8") as f:
            code_text = f.read()

        cs_source = wrapper_template.format(code=code_text)
        src_path = os.path.join(tmpdir, f"{name}.cs")
        out_dll = os.path.join(tmpdir, f"{name}.dll")

        with open(src_path, "w", encoding="utf-8") as f:
            f.write(cs_source)

        args = [
            "dotnet", csc,
            "-target:library",
            f"-out:{out_dll}",
            "-nologo",
            "-langversion:latest",
            "-nowarn:CS0168,CS0219" # ignore unused variable warnings
        ]
        for r in ref_files:
            args.append(f"-r:{r}")
        args.append(src_path)

        res = subprocess.run(args, capture_output=True, text=True)
        if res.returncode == 0:
            print(f"[COMPILES OK] {cat}/{name}")
        else:
            all_passed = False
            print(f"[COMPILE FAIL] {cat}/{name}")
            print(res.stdout)
            print(res.stderr)

print("\n" + "="*80)
print(f"COMPILATION VERIFICATION: {'100% COMPILED WITH 0 ERRORS' if all_passed else 'FAILURES DETECTED'}")
print("="*80)
