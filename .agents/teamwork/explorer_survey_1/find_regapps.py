with open(r"C:\Dce Pro\DCE Pro.dll", "rb") as f:
    data = f.read()

import re

# RegApps in AutoCAD DCE Pro typically end with _DCE or _dce or start with DCE_ or contain XDATA
raw_matches = re.findall(rb'[A-Za-z0-9_]{3,35}(?:_DCE|_dce|DCE_|XData|xdata)[A-Za-z0-9_]*', data)
regapps = set()
for m in raw_matches:
    try:
        s = m.decode('ascii')
        if not s.startswith("PADDING"):
            regapps.add(s)
    except:
        pass

# Also search for RegAppTableRecord strings
reg_exact = [
    "DCE_PRO", "Dce_Rebar", "DCE_REBAR", "TAG_DCE", "TagThep_DCE", "DimRebar_Dce",
    "DimRebar_Dce2", "DimNhanh_dce", "TaoField_DCE", "CreateField_DCE", "FieldBlockAndPoly_DCE",
    "ResetSoHieuThep_DCE", "RegenColor_DCE", "XoaLinks_DCE", "XoaDLDataDCE", "DCE_XDATA",
    "DCE_STEEL", "DCE_MBKC", "DCE_AUTOSTR"
]

print("=== Found RegApp Candidates in DCE Pro.dll ===")
for r in sorted(regapps):
    print(" ", r)
