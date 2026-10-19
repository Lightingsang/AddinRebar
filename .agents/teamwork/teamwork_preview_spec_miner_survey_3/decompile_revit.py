import subprocess
import os

types_to_decompile = [
    "Kata_Class_Lib_Revit.Bien_chung",
    "Kata_Class_Lib_Revit.Revit_rebar",
    "Kata_Class_Lib_Revit.Get_Kata_Familytype",
    "Kata_Class_Lib_Revit.Get_element",
    "Kata_Class_Lib_Revit.Element_Select",
    "Kata_Class_Lib_Revit.Perform_2D",
    "Kata_Class_Lib_Revit.info_thep",
    "Kata_Class_Lib_Revit.info_mc",
    "Kata_Class_Lib_Revit.info_vedam",
    "Kata_Class_Lib_Revit.Info_Beam3D",
    "Kata_Class_Lib_Revit.Info_Slab3D",
    "Kata_Class_Lib_Revit.Info_ColumnWall3D"
]

ilspycmd = os.path.expandvars(r"%USERPROFILE%\.dotnet\tools\ilspycmd.exe")
dll_path = r"C:\kata_pro\Kata_Class_Lib_Revit.dll"
out_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\revit_decomp"
os.makedirs(out_dir, exist_ok=True)

for t in types_to_decompile:
    simple_name = t.split(".")[-1]
    raw_out = os.path.join(out_dir, f"{simple_name}.raw.cs")
    final_out = os.path.join(out_dir, f"{simple_name}.cs")
    print(f"Decompiling {t}...")
    cmd = [ilspycmd, "-t", t, dll_path]
    with open(raw_out, "wb") as f:
        res = subprocess.run(cmd, stdout=f, stderr=subprocess.PIPE)
    
    with open(raw_out, "rb") as f:
        raw = f.read()
    try:
        content = raw.decode("utf-16le")
    except:
        content = raw.decode("utf-8", errors="ignore")
    with open(final_out, "w", encoding="utf-8") as f:
        f.write(content)
    os.remove(raw_out)
    print(f"  -> {final_out} ({len(content.splitlines())} lines)")

print("Done decompiling Revit types!")
