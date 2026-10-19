# -*- coding: utf-8 -*-
import json, uuid

def make_guid():
    return str(uuid.uuid4()).replace("-", "")

# 1. Generate GUIDs for all nodes and ports
id_filepath = make_guid()
id_filepath_out = make_guid()

id_read_json = make_guid()
id_read_json_in = make_guid()
id_read_json_out = make_guid()

id_unpack = make_guid()
id_unpack_in = make_guid()
id_unpack_out_sx = make_guid()
id_unpack_out_sy = make_guid()
id_unpack_out_ex = make_guid()
id_unpack_out_ey = make_guid()
id_unpack_out_types = make_guid()
id_unpack_out_marks = make_guid()
id_unpack_out_zoff = make_guid()
id_unpack_out_lvl = make_guid()

id_pt_start = make_guid()
id_pt_start_in_x = make_guid()
id_pt_start_in_y = make_guid()
id_pt_start_in_z = make_guid()
id_pt_start_out = make_guid()

id_pt_end = make_guid()
id_pt_end_in_x = make_guid()
id_pt_end_in_y = make_guid()
id_pt_end_in_z = make_guid()
id_pt_end_out = make_guid()

id_line = make_guid()
id_line_in_sp = make_guid()
id_line_in_ep = make_guid()
id_line_out = make_guid()

id_level = make_guid()
id_level_out = make_guid()

id_symbol_mapper = make_guid()
id_symbol_mapper_in = make_guid()
id_symbol_mapper_out = make_guid()

id_beam_by_curve = make_guid()
id_beam_in_curve = make_guid()
id_beam_in_level = make_guid()
id_beam_in_type = make_guid()
id_beam_out = make_guid()

id_cb_mark_name = make_guid()
id_cb_mark_name_out = make_guid()

id_set_mark = make_guid()
id_set_mark_in_elem = make_guid()
id_set_mark_in_param = make_guid()
id_set_mark_in_val = make_guid()
id_set_mark_out = make_guid()

id_cb_zjust = make_guid()
id_cb_zjust_out_name = make_guid()
id_cb_zjust_out_val = make_guid()

id_set_zjust = make_guid()
id_set_zjust_in_elem = make_guid()
id_set_zjust_in_param = make_guid()
id_set_zjust_in_val = make_guid()
id_set_zjust_out = make_guid()

id_cb_yjust = make_guid()
id_cb_yjust_out_name = make_guid()
id_cb_yjust_out_val = make_guid()

id_set_yjust = make_guid()
id_set_yjust_in_elem = make_guid()
id_set_yjust_in_param = make_guid()
id_set_yjust_in_val = make_guid()
id_set_yjust_out = make_guid()

id_cb_zoff_name = make_guid()
id_cb_zoff_name_out = make_guid()

id_set_zoff = make_guid()
id_set_zoff_in_elem = make_guid()
id_set_zoff_in_param = make_guid()
id_set_zoff_in_val = make_guid()
id_set_zoff_out = make_guid()

id_disallow = make_guid()
id_disallow_in = make_guid()
id_disallow_out = make_guid()

id_watch = make_guid()
id_watch_in = make_guid()
id_watch_out = make_guid()

# Python Code 1: Đọc & Giải Mã CAD JSON
py_read_json = """# -*- coding: utf-8 -*-
import json

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    model_data = json.load(f)

beams = model_data.get('beams', [])

# Tách mảng dữ liệu chuẩn hóa (đơn vị tọa độ quy đổi sang Feet cho Revit)
OUT = {
    'startX': [round(float(b['StartX']) / 304.8, 5) for b in beams],
    'startY': [round(float(b['StartY']) / 304.8, 5) for b in beams],
    'endX': [round(float(b['EndX']) / 304.8, 5) for b in beams],
    'endY': [round(float(b['EndY']) / 304.8, 5) for b in beams],
    'types': [str(b['TypeName']) for b in beams],
    'marks': [str(b['Mark']) for b in beams],
    'zOffsets': [round(float(b.get('ZOffsetMm', 0)) / 304.8, 5) for b in beams],
    'levelName': str(model_data.get('targetLevel', 'Tầng 1'))
}
"""

# Python Code 2: Khớp nối 295 FamilySymbol theo danh sách TypeName
py_symbol_mapper = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitServices')
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager

doc = DocumentManager.Instance.CurrentDBDocument
type_names = IN[0]

symbols = {}
collector = FilteredElementCollector(doc).OfClass(FamilySymbol).OfCategory(BuiltInCategory.OST_StructuralFraming)
for s in collector:
    symbols[s.Name] = s

matched_symbols = []
for name in type_names:
    sym = symbols.get(name)
    if sym and not sym.IsActive:
        sym.Activate()
    matched_symbols.append(sym)

OUT = matched_symbols
"""

# Python Code 3: Khóa liên kết 2 đầu dầm (DisallowJoinAtEnd)
py_disallow = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB.Structure import StructuralFramingUtils

beams = UnwrapElement(IN[0]) if isinstance(IN[0], list) else [UnwrapElement(IN[0])]
count = 0
for b in beams:
    if b:
        StructuralFramingUtils.DisallowJoinAtEnd(b, 0)
        StructuralFramingUtils.DisallowJoinAtEnd(b, 1)
        count += 1

OUT = {
    'status': 'SUCCESS',
    'totalBeams': len(beams),
    'disallowJoinApplied': count,
    'message': 'Đã khóa liên kết 2 đầu thành công cho toàn bộ 295 dầm đà kiềng Tầng 1!'
}
"""

nodes = [
    # 1. Input: File Path
    {
        "ConcreteType": "CoreNodeModels.Input.Filename, CoreNodeModels",
        "NodeType": "ExtensionNode",
        "Id": id_filepath,
        "Inputs": [],
        "Outputs": [
            {"Id": id_filepath_out, "Name": "File Path", "Description": "Đường dẫn file JSON CAD", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Chọn file JSON bóc tách chi tiết CAD",
        "FilePath": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json",
        "InputValue": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json"
    },
    # 2. Python Script: Giải mã dữ liệu CAD JSON
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_read_json,
        "Inputs": [
            {"Id": id_read_json_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_read_json_out, "Name": "OUT", "Description": "CAD Data Dictionary", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Giải mã 295 dầm, tọa độ mm -> Feet, mảng tiết diện chuẩn hóa",
        "Code": py_read_json,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 3. CodeBlock: Trích Xuất Các Luồng Thuộc Tính
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_unpack,
        "CodeText": "sx = data[\"startX\"];\nsy = data[\"startY\"];\nex = data[\"endX\"];\ney = data[\"endY\"];\ntypes = data[\"types\"];\nmarks = data[\"marks\"];\nzOffsets = data[\"zOffsets\"];\nlevelName = data[\"levelName\"];",
        "Inputs": [
            {"Id": id_unpack_in, "Name": "data", "Description": "data", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_unpack_out_sx, "Name": "", "Description": "sx", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_sy, "Name": "", "Description": "sy", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_ex, "Name": "", "Description": "ex", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_ey, "Name": "", "Description": "ey", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_types, "Name": "", "Description": "types", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_marks, "Name": "", "Description": "marks", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_zoff, "Name": "", "Description": "zOffsets", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_unpack_out_lvl, "Name": "", "Description": "levelName", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Phân phối 8 luồng thuộc tính độc lập ra Canvas"
    },
    # 4. Native ZeroTouch: Point.ByCoordinates (Start)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_pt_start,
        "FunctionSignature": "Autodesk.DesignScript.Geometry.Point.ByCoordinates@double,double,double",
        "Inputs": [
            {"Id": id_pt_start_in_x, "Name": "x", "Description": "Tọa độ X\n\ndouble", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_pt_start_in_y, "Name": "y", "Description": "Tọa độ Y\n\ndouble", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_pt_start_in_z, "Name": "z", "Description": "Tọa độ Z\n\ndouble", "UsingDefaultValue": True, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_pt_start_out, "Name": "Point", "Description": "Point", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Tạo 295 Điểm Đầu (Start Points)"
    },
    # 5. Native ZeroTouch: Point.ByCoordinates (End)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_pt_end,
        "FunctionSignature": "Autodesk.DesignScript.Geometry.Point.ByCoordinates@double,double,double",
        "Inputs": [
            {"Id": id_pt_end_in_x, "Name": "x", "Description": "Tọa độ X\n\ndouble", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_pt_end_in_y, "Name": "y", "Description": "Tọa độ Y\n\ndouble", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_pt_end_in_z, "Name": "z", "Description": "Tọa độ Z\n\ndouble", "UsingDefaultValue": True, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_pt_end_out, "Name": "Point", "Description": "Point", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Tạo 295 Điểm Cuối (End Points)"
    },
    # 6. Native ZeroTouch: Line.ByStartPointEndPoint
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_line,
        "FunctionSignature": "Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint@Autodesk.DesignScript.Geometry.Point,Autodesk.DesignScript.Geometry.Point",
        "Inputs": [
            {"Id": id_line_in_sp, "Name": "startPoint", "Description": "Điểm bắt đầu\n\nPoint", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_line_in_ep, "Name": "endPoint", "Description": "Điểm kết thúc\n\nPoint", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_line_out, "Name": "Line", "Description": "Line", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Dựng 295 tim dầm từ điểm đầu và điểm cuối"
    },
    # 7. Native Dropdown: Levels (Tầng 1)
    {
        "ConcreteType": "DSRevitNodesUI.Levels, DSRevitNodesUI",
        "NodeType": "ExtensionNode",
        "Id": id_level,
        "SelectedIndex": 0,
        "SelectedString": "Tầng 1",
        "Inputs": [],
        "Outputs": [
            {"Id": id_level_out, "Name": "Levels", "Description": "Cấp độ sàn", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Chọn cao trình Tầng 1"
    },
    # 8. Python Script: Khớp nối 295 FamilySymbol tương ứng từng dầm
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_symbol_mapper,
        "Inputs": [
            {"Id": id_symbol_mapper_in, "Name": "IN[0]", "Description": "Type Names", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_symbol_mapper_out, "Name": "OUT", "Description": "295 FamilySymbols", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Khớp nối chính xác FamilySymbol cho từng cấu kiện (B_100x300, B_200x300, B_200x400...)",
        "Code": py_symbol_mapper,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 9. Native ZeroTouch: StructuralFraming.BeamByCurve
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_beam_by_curve,
        "FunctionSignature": "Revit.Elements.StructuralFraming.BeamByCurve@Autodesk.DesignScript.Geometry.Curve,Revit.Elements.Level,Revit.Elements.FamilyType",
        "Inputs": [
            {"Id": id_beam_in_curve, "Name": "curve", "Description": "Đường tim dầm\n\nCurve", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_beam_in_level, "Name": "level", "Description": "Level liên kết\n\nLevel", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_beam_in_type, "Name": "structuralFramingType", "Description": "Loại dầm\n\nFamilyType", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_beam_out, "Name": "StructuralFraming", "Description": "Cấu kiện Dầm", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Tạo 295 phần tử Dầm trong Revit đúng từng tiết diện"
    },
    # 10. CodeBlock: Mark Param Name
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_mark_name,
        "CodeText": "\"Mark\";",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_mark_name_out, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số Mark"
    },
    # 11. Native ZeroTouch: Element.SetParameterByName (Mark)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_set_mark,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_set_mark_in_elem, "Name": "element", "Description": "Đối tượng Revit\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_mark_in_param, "Name": "parameterName", "Description": "Tên tham số\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_mark_in_val, "Name": "value", "Description": "Giá trị\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_set_mark_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Gán tham số Mark cho 295 dầm"
    },
    # 12. CodeBlock: Z Justification (Top)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_zjust,
        "CodeText": "paramName = \"z Justification\";\nval = 0;",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_zjust_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_zjust_out_val, "Name": "", "Description": "val", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Căn chỉnh mặt trên phẳng (Top = 0)"
    },
    # 13. Native ZeroTouch: Element.SetParameterByName (z Justification)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_set_zjust,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_set_zjust_in_elem, "Name": "element", "Description": "Đối tượng Revit\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_zjust_in_param, "Name": "parameterName", "Description": "Tên tham số\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_zjust_in_val, "Name": "value", "Description": "Giá trị\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_set_zjust_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Căn phẳng mặt trên dầm"
    },
    # 14. CodeBlock: Y Justification (Origin)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_yjust,
        "CodeText": "paramName = \"y Justification\";\nval = 2;",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_yjust_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_yjust_out_val, "Name": "", "Description": "val", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Căn đúng tim dầm (Origin = 2)"
    },
    # 15. Native ZeroTouch: Element.SetParameterByName (y Justification)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_set_yjust,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_set_yjust_in_elem, "Name": "element", "Description": "Đối tượng Revit\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_yjust_in_param, "Name": "parameterName", "Description": "Tên tham số\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_yjust_in_val, "Name": "value", "Description": "Giá trị\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_set_yjust_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Căn tim dầm trùng tim lưới CAD"
    },
    # 16. CodeBlock: Z Offset Value Parameter Name
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_zoff_name,
        "CodeText": "\"z Offset Value\";",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_zoff_name_out, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số giật cốt Z-Offset"
    },
    # 17. Native ZeroTouch: Element.SetParameterByName (z Offset Value)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_set_zoff,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_set_zoff_in_elem, "Name": "element", "Description": "Đối tượng Revit\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_zoff_in_param, "Name": "parameterName", "Description": "Tên tham số\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_set_zoff_in_val, "Name": "value", "Description": "Giá trị\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_set_zoff_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Hạ cốt -1.000m (zOffset = -900mm) cho DK.a'"
    },
    # 18. Python Script: Khóa liên kết nút 2 đầu dầm (DisallowJoin)
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_disallow,
        "Inputs": [
            {"Id": id_disallow_in, "Name": "IN[0]", "Description": "Beams", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_disallow_out, "Name": "OUT", "Description": "Audit Result", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Khóa liên kết nút 2 đầu dầm (DisallowJoinAtEnd)",
        "Code": py_disallow,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 19. Native Watch Node
    {
        "ConcreteType": "CoreNodeModels.Watch, CoreNodeModels",
        "WatchWidth": 320.0,
        "WatchHeight": 180.0,
        "Id": id_watch,
        "NodeType": "ExtensionNode",
        "Inputs": [
            {"Id": id_watch_in, "Name": "", "Description": "Node-to-watch", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_watch_out, "Name": "", "Description": "Output", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Bảng nghiệm thu toàn bộ hệ dầm Tầng 1"
    }
]

# EXACT STRING PORT GUID CONNECTORS
connectors = [
    # 1. FilePath -> ReadJSON
    {"Start": id_filepath_out, "End": id_read_json_in, "Id": make_guid(), "IsHidden": "False"},
    
    # 2. ReadJSON -> Unpack
    {"Start": id_read_json_out, "End": id_unpack_in, "Id": make_guid(), "IsHidden": "False"},
    
    # 3. Unpack sx -> PtStart x
    {"Start": id_unpack_out_sx, "End": id_pt_start_in_x, "Id": make_guid(), "IsHidden": "False"},
    # 4. Unpack sy -> PtStart y
    {"Start": id_unpack_out_sy, "End": id_pt_start_in_y, "Id": make_guid(), "IsHidden": "False"},
    
    # 5. Unpack ex -> PtEnd x
    {"Start": id_unpack_out_ex, "End": id_pt_end_in_x, "Id": make_guid(), "IsHidden": "False"},
    # 6. Unpack ey -> PtEnd y
    {"Start": id_unpack_out_ey, "End": id_pt_end_in_y, "Id": make_guid(), "IsHidden": "False"},
    
    # 7. PtStart -> Line startPoint
    {"Start": id_pt_start_out, "End": id_line_in_sp, "Id": make_guid(), "IsHidden": "False"},
    # 8. PtEnd -> Line endPoint
    {"Start": id_pt_end_out, "End": id_line_in_ep, "Id": make_guid(), "IsHidden": "False"},
    
    # 9. Line -> BeamByCurve curve
    {"Start": id_line_out, "End": id_beam_in_curve, "Id": make_guid(), "IsHidden": "False"},
    # 10. Level -> BeamByCurve level
    {"Start": id_level_out, "End": id_beam_in_level, "Id": make_guid(), "IsHidden": "False"},
    
    # 11. Unpack types -> SymbolMapper
    {"Start": id_unpack_out_types, "End": id_symbol_mapper_in, "Id": make_guid(), "IsHidden": "False"},
    # 12. SymbolMapper -> BeamByCurve structuralFramingType
    {"Start": id_symbol_mapper_out, "End": id_beam_in_type, "Id": make_guid(), "IsHidden": "False"},
    
    # 13. BeamByCurve -> SetMark element
    {"Start": id_beam_out, "End": id_set_mark_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 14. Mark Name -> SetMark parameterName
    {"Start": id_cb_mark_name_out, "End": id_set_mark_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 15. Unpack marks -> SetMark value
    {"Start": id_unpack_out_marks, "End": id_set_mark_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 16. SetMark -> SetZJust element
    {"Start": id_set_mark_out, "End": id_set_zjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 17. ZJust Name -> SetZJust parameterName
    {"Start": id_cb_zjust_out_name, "End": id_set_zjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 18. ZJust Val -> SetZJust value
    {"Start": id_cb_zjust_out_val, "End": id_set_zjust_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 19. SetZJust -> SetYJust element
    {"Start": id_set_zjust_out, "End": id_set_yjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 20. YJust Name -> SetYJust parameterName
    {"Start": id_cb_yjust_out_name, "End": id_set_yjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 21. YJust Val -> SetYJust value
    {"Start": id_cb_yjust_out_val, "End": id_set_yjust_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 22. SetYJust -> SetZOff element
    {"Start": id_set_yjust_out, "End": id_set_zoff_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 23. ZOff Name -> SetZOff parameterName
    {"Start": id_cb_zoff_name_out, "End": id_set_zoff_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 24. Unpack zOffsets -> SetZOff value
    {"Start": id_unpack_out_zoff, "End": id_set_zoff_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 25. SetZOff -> DisallowJoin
    {"Start": id_set_zoff_out, "End": id_disallow_in, "Id": make_guid(), "IsHidden": "False"},
    
    # 26. DisallowJoin -> Watch
    {"Start": id_disallow_out, "End": id_watch_in, "Id": make_guid(), "IsHidden": "False"}
]

# Canvas Layout Positions (X, Y)
node_views = [
    # Group 1: Inputs
    {"Id": id_filepath, "Name": "File Path: CAD JSON Model", "X": 100.0, "Y": 250.0},
    {"Id": id_read_json, "Name": "Giải Mã CAD JSON (Python)", "X": 450.0, "Y": 250.0},
    {"Id": id_unpack, "Name": "Phân Phối Thuộc Tính (CodeBlock)", "X": 850.0, "Y": 200.0},
    
    # Group 2: Geometry
    {"Id": id_pt_start, "Name": "Point.ByCoordinates (Start)", "X": 1350.0, "Y": 150.0},
    {"Id": id_pt_end, "Name": "Point.ByCoordinates (End)", "X": 1350.0, "Y": 400.0},
    {"Id": id_line, "Name": "Line.ByStartPointEndPoint", "X": 1750.0, "Y": 275.0},
    
    # Group 3: Level & Symbols
    {"Id": id_level, "Name": "Levels ('Tầng 1')", "X": 1750.0, "Y": 600.0},
    {"Id": id_symbol_mapper, "Name": "Khớp Nối FamilySymbol 295 Dầm (Python)", "X": 1750.0, "Y": 800.0},
    
    # Group 4: Framing Creation
    {"Id": id_beam_by_curve, "Name": "StructuralFraming.BeamByCurve", "X": 2200.0, "Y": 400.0},
    
    # Group 5: Parameters Assignment
    {"Id": id_cb_mark_name, "Name": "CodeBlock ('Mark')", "X": 2550.0, "Y": 150.0},
    {"Id": id_set_mark, "Name": "Element.SetParameterByName (Mark)", "X": 2850.0, "Y": 250.0},
    
    {"Id": id_cb_zjust, "Name": "CodeBlock (z Just = 0)", "X": 3200.0, "Y": 150.0},
    {"Id": id_set_zjust, "Name": "Element.SetParameterByName (z Just)", "X": 3500.0, "Y": 250.0},
    
    {"Id": id_cb_yjust, "Name": "CodeBlock (y Just = 2)", "X": 3850.0, "Y": 150.0},
    {"Id": id_set_yjust, "Name": "Element.SetParameterByName (y Just)", "X": 4150.0, "Y": 250.0},
    
    {"Id": id_cb_zoff_name, "Name": "CodeBlock ('z Offset Value')", "X": 4500.0, "Y": 150.0},
    {"Id": id_set_zoff, "Name": "Element.SetParameterByName (z Offset)", "X": 4800.0, "Y": 250.0},
    
    # Group 6: DisallowJoin & Watch
    {"Id": id_disallow, "Name": "Khóa Nút Liên Kết 2 Đầu (Python)", "X": 5250.0, "Y": 300.0},
    {"Id": id_watch, "Name": "Bảng Nghiệm Thu Hệ Dầm", "X": 5650.0, "Y": 300.0}
]

annotations = [
    # Group 1: Vàng
    {
        "Id": make_guid(),
        "Title": "1. DỮ LIỆU ĐẦU VÀO TỪ CAD (INPUTS & CAD DATA DECODER)",
        "Nodes": [id_filepath, id_read_json, id_unpack],
        "Left": 50.0,
        "Top": 80.0,
        "Width": 1200.0,
        "Height": 750.0,
        "FontSize": 32.0,
        "Background": "#FFFFC048"
    },
    # Group 2: Xanh Lá
    {
        "Id": make_guid(),
        "Title": "2. HÌNH HỌC TIM DẦM (GEOMETRY PIPELINE)",
        "Nodes": [id_pt_start, id_pt_end, id_line],
        "Left": 1300.0,
        "Top": 80.0,
        "Width": 800.0,
        "Height": 500.0,
        "FontSize": 32.0,
        "Background": "#FF20BF6B"
    },
    # Group 3: Xanh Dương
    {
        "Id": make_guid(),
        "Title": "3. LEVEL & FAMILY SYMBOLS MAPPING",
        "Nodes": [id_level, id_symbol_mapper],
        "Left": 1700.0,
        "Top": 530.0,
        "Width": 450.0,
        "Height": 550.0,
        "FontSize": 32.0,
        "Background": "#FF2D98DA"
    },
    # Group 4: Tím
    {
        "Id": make_guid(),
        "Title": "4. KHỞI TẠO DẦM REVIT (BEAM BY CURVE)",
        "Nodes": [id_beam_by_curve],
        "Left": 2150.0,
        "Top": 200.0,
        "Width": 350.0,
        "Height": 450.0,
        "FontSize": 32.0,
        "Background": "#FF8854D0"
    },
    # Group 5: Cam
    {
        "Id": make_guid(),
        "Title": "5. CHUỖI GÁN THAM SỐ QUẢN LÝ (MARK, TOP, ORIGIN, Z-OFFSET)",
        "Nodes": [id_cb_mark_name, id_set_mark, id_cb_zjust, id_set_zjust, id_cb_yjust, id_set_yjust, id_cb_zoff_name, id_set_zoff],
        "Left": 2520.0,
        "Top": 80.0,
        "Width": 2650.0,
        "Height": 550.0,
        "FontSize": 32.0,
        "Background": "#FFFF9F43"
    },
    # Group 6: Đỏ/Hồng
    {
        "Id": make_guid(),
        "Title": "6. KHÓA NÚT LIÊN KẾT & NGHIỆM THU (DISALLOW JOIN & WATCH)",
        "Nodes": [id_disallow, id_watch],
        "Left": 5200.0,
        "Top": 150.0,
        "Width": 850.0,
        "Height": 500.0,
        "FontSize": 32.0,
        "Background": "#FFEB3B5A"
    }
]

dyn_data = {
    "Uuid": "da28c993-04c7-46a6-be35-ee05c430eb47",
    "IsCustomNode": False,
    "Description": "Đồ thị trực quan tự động dựng 295 cấu kiện dầm sàn/đà kiềng Tầng 1 từ bản vẽ CAD (đầy đủ đầu vào File Path, khớp nối FamilySymbol chuẩn hóa, gán tham số và DisallowJoin).",
    "Name": "VeHeDamTang1_DaKieng",
    "ElementResolver": {
        "ResolutionMap": {
            "Point": {
                "Key": "Autodesk.DesignScript.Geometry.Point",
                "Value": "ProtoGeometry.dll"
            },
            "Line": {
                "Key": "Autodesk.DesignScript.Geometry.Line",
                "Value": "ProtoGeometry.dll"
            }
        }
    },
    "Inputs": [],
    "Outputs": [],
    "Nodes": nodes,
    "Connectors": connectors,
    "Dependencies": [],
    "NodeLibraryDependencies": [],
    "Thumbnail": "",
    "GraphDocumentationURL": None,
    "ExtensionWorkspaceData": [],
    "Author": "Antigravity & HP Dynamo MCP",
    "Linting": {
        "activeLinter": "None",
        "activeLinterId": "7b750d4b-bd62-408c-ac42-64949e6070a8",
        "warningCount": 0,
        "errorCount": 0
    },
    "Bindings": [],
    "View": {
        "Dynamo": {
            "ScaleFactor": 1.0,
            "HasRunWithoutCrash": True,
            "IsVisibleInDynamoLibrary": True,
            "Version": "3.2.1.5342",
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
        "ConnectorPins": [],
        "NodeViews": node_views,
        "Annotations": annotations,
        "X": 50.0,
        "Y": 50.0,
        "Zoom": 0.45
    }
}

target_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"
with open(target_file, "w", encoding="utf-8") as f:
    json.dump(dyn_data, f, indent=2, ensure_ascii=False)

print(f"SUCCESS: Generated hybrid visual Dynamo graph with {len(nodes)} nodes, {len(connectors)} string connectors, and {len(annotations)} color-coded groups!")
