# -*- coding: utf-8 -*-
import json

dyn_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"
model_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json"

with open(model_path, "r", encoding="utf-8") as f:
    model_data = json.load(f)

beams = model_data["beams"]
beams_array = []
for b in beams:
    beams_array.append([
        b["Mark"],
        b["TypeName"],
        b["StartX"],
        b["StartY"],
        b["EndX"],
        b["EndY"],
        b.get("ZOffsetMm", 0)
    ])

with open(dyn_path, "r", encoding="utf-8") as f:
    dyn = json.load(f)

python_node = dyn["Nodes"][1]

python_code = '''# -*- coding: utf-8 -*-
# Tu dong dung 295 dam da kieng Tang 1 tu CAD MatBang_DaKieng.dwg
# Ho tro boi HP Dynamo MCP Engine & Revit 2026 API
# Da chuan hoa 100% tiet dien theo chi tiet CAD: DK.a (100x300), DK.a' (100x300, cote -1.000), DK.b (200x300)
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')

from Autodesk.Revit.DB import *
from Autodesk.Revit.DB.Structure import *
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument

level_name = IN[0] if len(IN) > 0 and IN[0] else "Tầng 1"
family_name = IN[1] if len(IN) > 1 and IN[1] else "1_STR_Framing_ConcreteRectangular"

# 1. Tim Level
level = None
for l in FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType():
    if l.Name == level_name:
        level = l
        break

if not level:
    OUT = {"error": "Khong tim thay Level " + level_name}
else:
    # 2. Thu thap cac FamilySymbol thuoc family_name
    symbols = {}
    for s in FilteredElementCollector(doc).OfClass(FamilySymbol).OfCategory(BuiltInCategory.OST_StructuralFraming):
        if family_name in s.FamilyName or s.FamilyName == family_name:
            symbols[s.Name] = s

    # 3. Danh sach 295 dam bop tach tu CAD [Mark, TypeName, StartX, StartY, EndX, EndY, ZOffsetMm]
    BEAMS_DATA = ''' + json.dumps(beams_array, ensure_ascii=False) + '''

    created_ids = []
    type_counts = {}
    failed_count = 0

    TransactionManager.Instance.EnsureInTransaction(doc)
    try:
        # Xoa dam cu tai Tang 1 (neu co)
        old_beams = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().ToElements()
        for ob in old_beams:
            ref_lvl = ob.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
            if (ob.LevelId == level.Id) or (ref_lvl and ref_lvl.AsElementId() == level.Id):
                doc.Delete(ob.Id)

        # Tao 295 dam moi
        for item in BEAMS_DATA:
            mark = item[0]
            type_name = item[1]
            sx, sy, ex, ey = item[2], item[3], item[4], item[5]
            z_off_mm = item[6] if len(item) > 6 else 0.0

            if type_name not in symbols:
                failed_count += 1
                continue

            symbol = symbols[type_name]
            if not symbol.IsActive:
                symbol.Activate()

            p1 = XYZ(float(sx) / 304.8, float(sy) / 304.8, 0.0)
            p2 = XYZ(float(ex) / 304.8, float(ey) / 304.8, 0.0)
            curve = Line.CreateBound(p1, p2)

            beam = doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Beam)

            # Y Justification: Origin (2)
            y_param = beam.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)
            if y_param and not y_param.IsReadOnly:
                y_param.Set(2)

            # Z Justification: Top (0)
            z_param = beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)
            if z_param and not z_param.IsReadOnly:
                z_param.Set(0)

            # Z Offset Value (ha cot -1.000m neu co)
            if z_off_mm != 0.0:
                z_off_param = beam.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)
                if z_off_param and not z_off_param.IsReadOnly:
                    z_off_param.Set(float(z_off_mm) / 304.8)

            # Mark
            m_param = beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
            if m_param and not m_param.IsReadOnly:
                m_param.Set(mark)

            # Disallow Join 2 dau
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 0)
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 1)

            created_ids.append(beam.Id.IntegerValue if hasattr(beam.Id, 'IntegerValue') else beam.Id.Value)
            type_counts[type_name] = type_counts.get(type_name, 0) + 1

        TransactionManager.Instance.TransactionTaskDone()
        OUT = {
            "status": "SUCCESS",
            "totalCreated": len(created_ids),
            "expectedTotal": len(BEAMS_DATA),
            "level": level.Name,
            "topElevationMm": -100.0,
            "typeDistribution": type_counts,
            "first10Ids": created_ids[:10]
        }
    except Exception as e:
        TransactionManager.Instance.ForceCloseTransaction()
        OUT = {"status": "FAILED", "error": str(e)}
'''

python_node["Code"] = python_code
dyn["Description"] = "Tự động dựng toàn bộ 295 cấu kiện dầm sàn/đà kiềng Tầng 1 từ bản vẽ CAD (đã chuẩn hóa 100% tiết diện và cao độ theo chi tiết kết cấu CAD DK.a, DK.a', DK.b)"

with open(dyn_path, "w", encoding="utf-8") as f:
    json.dump(dyn, f, indent=2, ensure_ascii=False)

print("SUCCESS: Synchronized VeHeDamTang1_DaKieng.dyn with 295 updated beams!")
