import subprocess
import os

ilspycmd = os.path.expandvars(r"%USERPROFILE%\.dotnet\tools\ilspycmd.exe")
dll_path = r"C:\kata_pro\Kata_Class_Lib_Revit.dll"
out_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\revit_decomp\bien_chung"
os.makedirs(out_dir, exist_ok=True)

nested_types = [
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_thep",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_moc_thep",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_dai",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_i_dai",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_slab",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_dim_ro",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_cat_ngang",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_tag",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_mc_doc_dam",
    "Kata_Class_Lib_Revit.Bien_chung+info_revit_mc_ngang_dam"
]

for t in nested_types:
    sname = t.split("+")[-1]
    raw_out = os.path.join(out_dir, f"{sname}.raw.cs")
    final_out = os.path.join(out_dir, f"{sname}.cs")
    cmd = [ilspycmd, "-t", t, dll_path]
    with open(raw_out, "wb") as f:
        subprocess.run(cmd, stdout=f, stderr=subprocess.PIPE)
    with open(raw_out, "rb") as f:
        raw = f.read()
    try:
        content = raw.decode("utf-16le")
    except:
        content = raw.decode("utf-8", errors="ignore")
    with open(final_out, "w", encoding="utf-8") as f:
        f.write(content)
    os.remove(raw_out)
    print(f"Decompiled {sname}: {len(content.splitlines())} lines")

print("Done nested types!")
