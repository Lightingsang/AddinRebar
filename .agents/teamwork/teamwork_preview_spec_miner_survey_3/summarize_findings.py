import re

path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\filtered_udfs.txt"

with open(path, "r", encoding="utf-8") as f:
    content = f.read()

categories = {
    "PMM & Column": [r"PMM", r"DCE_Col_Function", r"TinhHamLuongTrenDuongKinh", r"DCE_Col_Design", r"Composite_Design"],
    "Wall & Core Wall": [r"Vach", r"Wall", r"ShearWall"],
    "Two-Way Slab": [r"Slab_Function", r"DCE_Slab_Design", r"Slab_Crack", r"TrongtamSlab", r"San"],
    "Pile Cap & Footing": [r"DaiCoc", r"Foundation_TinhHeSoCChiuCat", r"MongDon", r"PadFooting", r"PileCap"],
    "Pile Capacity & Friction": [r"SucChiuTaiCoc", r"TinhToanSCTCoc", r"qb_SPT", r"MaSat", r"NoiSuySPT"],
    "Crack Width": [r"VetNut", r"Crack", r"BeRong", r"Slab_Crack_Function"],
    "Steel Beam & Column TCVN 5575": [r"DCEPropSectI", r"DCEPropIQuat", r"DCETCVNPhuLucFHeSoOnDinh", r"DamThep", r"CotThep", r"Bimoment"],
    "Steel Moment Connection": [r"LK250", r"LienKet", r"DrawBanBichMC", r"Moment", r"EndPlate"],
    "Anchor Bolt & Base Plate": [r"BPThkd", r"Setup_BoltL", r"LK9_VN", r"BuLong", r"BasePlate"],
    "Wind Load TCVN 2737": [r"Wind", r"TCVN2737", r"TCVNCoeffVerticalWall", r"TaiTrongGio"]
}

with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\findings_report.txt", "w", encoding="utf-8") as out:
    for cat, pats in categories.items():
        out.write(f"\n==================== CATEGORY: {cat} ====================\n")
        found = set()
        for line in content.splitlines():
            if any(re.search(p, line, re.IGNORECASE) for p in pats):
                if not line.startswith("==="):
                    found.add(line.strip())
        for item in sorted(found):
            out.write(item + "\n")

print("Saved to findings_report.txt")
