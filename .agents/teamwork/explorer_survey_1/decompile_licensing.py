import subprocess
import os

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
out_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1"

targets = [
    (r"C:\Dce Pro\DCE Pro.dll", "ClassLibrary1.LicenseMoi", "DCEPro_LicenseMoi.cs"),
    (r"C:\Dce Pro\DCE Pro.dll", "ClassLibrary1.LicenseCheckAPI", "DCEPro_LicenseCheckAPI.cs"),
    (r"C:\Dce Pro\DCE Pro.dll", "ClassLibrary1.DceSerinumber", "DCEPro_DceSerinumber.cs"),
    (r"C:\Dce Pro\DCE Pro.dll", "ClassLibrary1.DbimTaiKhoanApi", "DCEPro_DbimTaiKhoanApi.cs"),
    (r"C:\Dce Pro\DCE Pro.dll", "ClassLibrary1.DceFingerprint", "DCEPro_DceFingerprint.cs"),
    (r"C:\Dce Pro\V.2020\DCE Excel.dll", "DCE_Excel.LicenseMoi", "Excel_LicenseMoi.cs"),
    (r"C:\Dce Pro\V.2020\DCE Excel.dll", "DCE_Excel.LicenseCheckAPI", "Excel_LicenseCheckAPI.cs"),
    (r"C:\Dce Pro\V.2020\DCE Excel.dll", "DCE_Excel.DceSerinumber", "Excel_DceSerinumber.cs"),
    (r"C:\Dce Pro\V.2020\DCE Excel.dll", "DCE_Excel.DbimTaiKhoanApi", "Excel_DbimTaiKhoanApi.cs"),
    (r"C:\Dce Pro\AddinSteel\DceSteel.dll", "DceSteel.Chung_License", "Steel_Chung_License.cs")
]

for dll, typ, out_name in targets:
    out_path = os.path.join(out_dir, out_name)
    print(f"Decompiling {typ} from {dll}...")
    cmd = [ilspy, dll, "-t", typ]
    res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
    if res.returncode == 0:
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(res.stdout)
        print(f"  -> Wrote {out_name} ({len(res.stdout.splitlines())} lines)")
    else:
        print(f"  -> Error decompiling {typ}: {res.stderr}")
