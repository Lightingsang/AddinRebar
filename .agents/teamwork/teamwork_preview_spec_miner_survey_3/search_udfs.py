import re
import sys

keywords = [
    "TinhCot", "TinhVach", "TinhSan", "TinhDai", "TinhMong", "SucChiuTai", "VetNut",
    "TinhDamThep", "TinhCotThep", "TinhLienKet", "TinhBuLong", "TinhTaiTrongGio",
    "Beam_", "Col_", "Slab_", "Foundation_", "Crack", "PMM", "MaSat",
    "DCEProp", "TCVN", "Bimoment", "BPThkd", "FINDCV", "noisuy", "TRONGTAM"
]

files = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_excel_methods.txt",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_steel_methods.txt",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dbim_steel_autocad_methods.txt"
]

for fpath in files:
    print(f"\n==================== {fpath} ====================")
    with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            for kw in keywords:
                if kw.lower() in line.lower():
                    print(line.strip())
                    break
