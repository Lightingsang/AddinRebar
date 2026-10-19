import json
import uuid

def generate_dyn():
    # Read beam model data
    with open("scratch/story1_grade_beams_model.json", "r", encoding="utf-8") as f:
        model_data = json.load(f)

    beams = model_data["beams"]
    print(f"Loaded {len(beams)} beams from story1_grade_beams_model.json")

    # Generate unique GUIDs
    graph_id = str(uuid.uuid4())
    node1_id = str(uuid.uuid4())
    node2_id = str(uuid.uuid4())
    node3_id = str(uuid.uuid4())
    conn1_id = str(uuid.uuid4())
    conn2_id = str(uuid.uuid4())

    # Compact beam definitions for Python node: [Mark, TypeName, StartX, StartY, EndX, EndY]
    compact_beams = []
    for b in beams:
        compact_beams.append([
            b["Mark"],
            b["TypeName"],
            b["StartX"],
            b["StartY"],
            b["EndX"],
            b["EndY"]
        ])

    python_code = f"""# -*- coding: utf-8 -*-
# Tu dong dung 295 dam da kieng Tang 1 tu CAD MatBang_DaKieng.dwg
# Ho tro boi HP Dynamo MCP Engine & Revit 2026 API
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
    OUT = {{"error": "Khong tim thay Level " + level_name}}
else:
    # 2. Thu thap cac FamilySymbol thuoc family_name
    symbols = {{}}
    for s in FilteredElementCollector(doc).OfClass(FamilySymbol).OfCategory(BuiltInCategory.OST_StructuralFraming):
        if family_name in s.FamilyName or s.FamilyName == family_name:
            symbols[s.Name] = s

    # 3. Danh sach 295 dam bop tach tu CAD [Mark, TypeName, StartX, StartY, EndX, EndY]
    BEAMS_DATA = {json.dumps(compact_beams)}

    created_ids = []
    type_counts = {{}}
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
            mark, type_name, sx, sy, ex, ey = item[0], item[1], item[2], item[3], item[4], item[5]
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
        OUT = {{
            "status": "SUCCESS",
            "totalCreated": len(created_ids),
            "expectedTotal": len(BEAMS_DATA),
            "level": level.Name,
            "topElevationMm": -100.0,
            "typeDistribution": type_counts,
            "first10Ids": created_ids[:10]
        }}
    except Exception as e:
        TransactionManager.Instance.ForceCloseTransaction()
        OUT = {{"status": "FAILED", "error": str(e)}}
"""

    dyn_graph = {
        "Uuid": graph_id,
        "IsCustomNode": False,
        "Description": "Tự động dựng toàn bộ 295 cấu kiện dầm sàn/đà kiềng Tầng 1 từ bản vẽ CAD MatBang_DaKieng.dwg",
        "Name": "VeHeDamTang1_DaKieng",
        "ElementResolver": {
            "ResolutionMap": {
                "Point": {"Key": "Autodesk.DesignScript.Geometry.Point", "Value": "ProtoGeometry.dll"},
                "Line": {"Key": "Autodesk.DesignScript.Geometry.Line", "Value": "ProtoGeometry.dll"}
            }
        },
        "Inputs": [],
        "Outputs": [],
        "Nodes": [
            {
                "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
                "NodeType": "CodeBlockNodeModel",
                "CodeText": "\"Tầng 1\";\n\"1_STR_Framing_ConcreteRectangular\";",
                "Id": node1_id,
                "Inputs": [],
                "Outputs": [
                    {"Id": str(uuid.uuid4()), "Name": "", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
                    {"Id": str(uuid.uuid4()), "Name": "", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
                ],
                "Replication": "Disabled",
                "Description": "Cấu hình Level và Family Dầm",
                "X": 100.0,
                "Y": 250.0
            },
            {
                "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
                "NodeType": "PythonScriptNode",
                "Code": python_code,
                "Engine": "CPython3",
                "VariableInputPorts": True,
                "Id": node2_id,
                "Inputs": [
                    {"Id": str(uuid.uuid4()), "Name": "IN[0]", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
                    {"Id": str(uuid.uuid4()), "Name": "IN[1]", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
                ],
                "Outputs": [
                    {"Id": str(uuid.uuid4()), "Name": "OUT", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
                ],
                "Replication": "Disabled",
                "Description": "Thực thi thuật toán dựng 295 dầm đà kiềng theo tọa độ CAD",
                "X": 450.0,
                "Y": 250.0
            },
            {
                "ConcreteType": "CoreNodeModels.Watch, CoreNodeModels",
                "NodeType": "ExtensionNode",
                "Id": node3_id,
                "Inputs": [
                    {"Id": str(uuid.uuid4()), "Name": "", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
                ],
                "Outputs": [
                    {"Id": str(uuid.uuid4()), "Name": "", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
                ],
                "Replication": "Disabled",
                "Description": "Hiển thị kết quả kiểm toán tạo dầm",
                "X": 850.0,
                "Y": 250.0
            }
        ],
        "Connectors": [
            {
                "Start": {
                    "Id": node1_id,
                    "Index": 0,
                    "PortType": 0
                },
                "End": {
                    "Id": node2_id,
                    "Index": 0,
                    "PortType": 0
                },
                "Id": conn1_id,
                "IsHidden": "False"
            },
            {
                "Start": {
                    "Id": node2_id,
                    "Index": 0,
                    "PortType": 0
                },
                "End": {
                    "Id": node3_id,
                    "Index": 0,
                    "PortType": 0
                },
                "Id": conn2_id,
                "IsHidden": "False"
            }
        ],
        "Dependencies": [],
        "NodeLibraryDependencies": [],
        "Thumbnail": "",
        "GraphDocumentationURL": None,
        "ExtensionWorkspaceData": [],
        "Author": "HP Revit AI",
        "Linting": {
            "activeLinter": "None",
            "activeLinterId": "7b75fb44-43fd-4631-a878-29f4d5d8399a",
            "warningCount": 0,
            "errorCount": 0
        },
        "Bindings": [],
        "View": {
            "Dynamo": {
                "ScaleFactor": 1.0,
                "HasRunWithoutCrash": True,
                "IsVisibleInDynamoLibrary": True,
                "Version": "3.7.0.11777",
                "RunType": "Manual",
                "RunPeriod": "1000"
            },
            "Camera": {
                "Name": "_Background Preview",
                "EyeX": -17.0,
                "EyeY": 24.0,
                "EyeZ": 50.0,
                "LookX": 12.0,
                "LookY": -13.0,
                "LookZ": -58.0,
                "UpX": 0.0,
                "UpY": 1.0,
                "UpZ": 0.0
            },
            "NodeViews": [
                {
                    "Id": node1_id,
                    "Name": "Cấu hình Đầu vào",
                    "X": 100.0,
                    "Y": 250.0,
                    "ShowGeometry": True,
                    "Excluded": False,
                    "IsSetAsInput": False,
                    "IsSetAsOutput": False
                },
                {
                    "Id": node2_id,
                    "Name": "Dựng 295 Đà Kiềng Tầng 1",
                    "X": 450.0,
                    "Y": 250.0,
                    "ShowGeometry": True,
                    "Excluded": False,
                    "IsSetAsInput": False,
                    "IsSetAsOutput": False
                },
                {
                    "Id": node3_id,
                    "Name": "Báo cáo Kết quả",
                    "X": 950.0,
                    "Y": 250.0,
                    "ShowGeometry": True,
                    "Excluded": False,
                    "IsSetAsInput": False,
                    "IsSetAsOutput": False
                }
            ],
            "Annotations": [
                {
                    "Id": str(uuid.uuid4()),
                    "Title": "QUY TRÌNH DỰNG 295 ĐÀ KIỀNG TẦNG 1 TỪ CAD - HP REVIT AI",
                    "NodesText": f"{node1_id} {node2_id} {node3_id}",
                    "Left": 70.0,
                    "Top": 180.0,
                    "Width": 1150.0,
                    "Height": 450.0,
                    "FontSize": 24.0,
                    "InitialTop": 180.0,
                    "InitialHeight": 450.0,
                    "Text": "Tự động trích xuất tim dầm CAD, khởi tạo StructuralFraming, gán cao độ đỉnh -100mm, căn Justification và khóa liên kết DisallowJoin 2 đầu."
                }
            ],
            "X": 50.0,
            "Y": 50.0,
            "Zoom": 0.9
        }
    }

    target_path = "scratch/VeHeDamTang1_DaKieng.dyn"
    with open(target_path, "w", encoding="utf-8") as f:
        json.dump(dyn_graph, f, indent=2, ensure_ascii=False)

    print(f"Generated {target_path} successfully ({len(json.dumps(dyn_graph))} bytes).")

if __name__ == "__main__":
    generate_dyn()
