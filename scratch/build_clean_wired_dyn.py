# -*- coding: utf-8 -*-
import json, uuid, os

def make_guid():
    return str(uuid.uuid4()).replace("-", "")

# 1. Generate GUIDs for nodes and ports
id_filepath = make_guid()
id_filepath_out = make_guid()

id_pts_start = make_guid()
id_pts_start_in = make_guid()
id_pts_start_out = make_guid()

id_pts_end = make_guid()
id_pts_end_in = make_guid()
id_pts_end_out = make_guid()

id_lines = make_guid()
id_lines_in_sp = make_guid()
id_lines_in_ep = make_guid()
id_lines_out = make_guid()

id_level = make_guid()
id_level_out = make_guid()

id_syms = make_guid()
id_syms_in = make_guid()
id_syms_out = make_guid()

id_beam = make_guid()
id_beam_in_curve = make_guid()
id_beam_in_lvl = make_guid()
id_beam_in_sym = make_guid()
id_beam_out = make_guid()

id_marks = make_guid()
id_marks_in = make_guid()
id_marks_out = make_guid()

id_cb_param_mark = make_guid()
id_cb_param_mark_out = make_guid()

id_sp_mark = make_guid()
id_sp_mark_in_elem = make_guid()
id_sp_mark_in_param = make_guid()
id_sp_mark_in_val = make_guid()
id_sp_mark_out = make_guid()

id_cb_param_zjust = make_guid()
id_cb_param_zjust_out = make_guid()
id_cb_val_zjust = make_guid()
id_cb_val_zjust_out = make_guid()

id_sp_zjust = make_guid()
id_sp_zjust_in_elem = make_guid()
id_sp_zjust_in_param = make_guid()
id_sp_zjust_in_val = make_guid()
id_sp_zjust_out = make_guid()

id_cb_param_yjust = make_guid()
id_cb_param_yjust_out = make_guid()
id_cb_val_yjust = make_guid()
id_cb_val_yjust_out = make_guid()

id_sp_yjust = make_guid()
id_sp_yjust_in_elem = make_guid()
id_sp_yjust_in_param = make_guid()
id_sp_yjust_in_val = make_guid()
id_sp_yjust_out = make_guid()

id_zoffsets = make_guid()
id_zoffsets_in = make_guid()
id_zoffsets_out = make_guid()

id_cb_param_zoff = make_guid()
id_cb_param_zoff_out = make_guid()

id_sp_zoff = make_guid()
id_sp_zoff_in_elem = make_guid()
id_sp_zoff_in_param = make_guid()
id_sp_zoff_in_val = make_guid()
id_sp_zoff_out = make_guid()

id_disallow = make_guid()
id_disallow_in = make_guid()
id_disallow_out = make_guid()

id_watch = make_guid()
id_watch_in = make_guid()
id_watch_out = make_guid()

# Python scripts
py_start_pts = """# -*- coding: utf-8 -*-
import json, clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import Point

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

pts = []
for b in data.get('beams', []):
    x = float(b['StartX']) / 304.8
    y = float(b['StartY']) / 304.8
    pts.append(Point.ByCoordinates(x, y, 0.0))
OUT = pts
"""

py_end_pts = """# -*- coding: utf-8 -*-
import json, clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import Point

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

pts = []
for b in data.get('beams', []):
    x = float(b['EndX']) / 304.8
    y = float(b['EndY']) / 304.8
    pts.append(Point.ByCoordinates(x, y, 0.0))
OUT = pts
"""

py_syms = """# -*- coding: utf-8 -*-
import json, clr
clr.AddReference('RevitServices')
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

doc = DocumentManager.Instance.CurrentDBDocument
type_names = [b['TypeName'] for b in data.get('beams', [])]

collector = FilteredElementCollector(doc).OfClass(FamilySymbol).OfCategory(BuiltInCategory.OST_StructuralFraming)
symbol_map = {s.Name: s for s in collector}

symbols = []
for name in type_names:
    sym = symbol_map.get(name)
    if sym and not sym.IsActive:
        sym.Activate()
    symbols.append(sym)
OUT = symbols
"""

py_marks = """# -*- coding: utf-8 -*-
import json

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

OUT = [str(b['Mark']) for b in data.get('beams', [])]
"""

py_zoffsets = """# -*- coding: utf-8 -*-
import json

file_path = IN[0]
with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Quy đổi ZOffset từ mm sang Feet cho Revit
OUT = [float(b.get('ZOffsetMm', 0)) / 304.8 for b in data.get('beams', [])]
"""

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
    'message': f'Đã khóa liên kết 2 đầu thành công cho {count} dầm đà kiềng Tầng 1!'
}
"""

nodes = [
    # 1. File Path Node
    {
        "ConcreteType": "CoreNodeModels.Input.Filename, CoreNodeModels",
        "NodeType": "ExtensionNode",
        "Id": id_filepath,
        "Inputs": [],
        "Outputs": [
            {"Id": id_filepath_out, "Name": "", "Description": "File Path", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Duyệt chọn file JSON bóc tách chi tiết CAD",
        "HintPath": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json",
        "InputValue": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json"
    },
    # 2. Python: Tạo 295 Điểm Đầu (Start Points)
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_pts_start,
        "Inputs": [
            {"Id": id_pts_start_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_pts_start_out, "Name": "OUT", "Description": "Start Points", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Đọc CAD JSON -> Tạo 295 Điểm Đầu (Start Points)",
        "Code": py_start_pts,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 3. Python: Tạo 295 Điểm Cuối (End Points)
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_pts_end,
        "Inputs": [
            {"Id": id_pts_end_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_pts_end_out, "Name": "OUT", "Description": "End Points", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Đọc CAD JSON -> Tạo 295 Điểm Cuối (End Points)",
        "Code": py_end_pts,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 4. Native ZeroTouch: Line.ByStartPointEndPoint
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_lines,
        "FunctionSignature": "Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint@Autodesk.DesignScript.Geometry.Point,Autodesk.DesignScript.Geometry.Point",
        "Inputs": [
            {"Id": id_lines_in_sp, "Name": "startPoint", "Description": "Start Point\n\nPoint", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_lines_in_ep, "Name": "endPoint", "Description": "End Point\n\nPoint", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_lines_out, "Name": "Line", "Description": "Line", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Dựng 295 đường tim dầm (Line.ByStartPointEndPoint)"
    },
    # 5. Native Dropdown: Levels (Tầng 1)
    {
        "ConcreteType": "DSRevitNodesUI.Levels, DSRevitNodesUI",
        "NodeType": "ExtensionNode",
        "Id": id_level,
        "SelectedIndex": 0,
        "SelectedString": "Tầng 1",
        "Inputs": [],
        "Outputs": [
            {"Id": id_level_out, "Name": "Levels", "Description": "Levels", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Chọn cao trình Tầng 1"
    },
    # 6. Python: Lấy 295 FamilySymbol tương ứng từng dầm
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_syms,
        "Inputs": [
            {"Id": id_syms_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_syms_out, "Name": "OUT", "Description": "FamilySymbols", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Đọc CAD JSON -> Khớp đúng 295 FamilySymbol (B_100x300, B_200x300, B_200x400...)",
        "Code": py_syms,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 7. Native ZeroTouch: StructuralFraming.BeamByCurve
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_beam,
        "FunctionSignature": "Revit.Elements.StructuralFraming.BeamByCurve@Autodesk.DesignScript.Geometry.Curve,Revit.Elements.Level,Revit.Elements.FamilyType",
        "Inputs": [
            {"Id": id_beam_in_curve, "Name": "curve", "Description": "Curve\n\nCurve", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_beam_in_lvl, "Name": "level", "Description": "Level\n\nLevel", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_beam_in_sym, "Name": "structuralFramingType", "Description": "FamilyType\n\nFamilyType", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_beam_out, "Name": "StructuralFraming", "Description": "StructuralFraming", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Tạo 295 dầm Revit đúng vị trí & đúng từng tiết diện"
    },
    # 8. Python: Lấy danh sách 295 mã Mark
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_marks,
        "Inputs": [
            {"Id": id_marks_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_marks_out, "Name": "OUT", "Description": "Marks List", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Đọc CAD JSON -> Trích xuất 295 mã Mark",
        "Code": py_marks,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 9. CodeBlock: "Mark"
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_param_mark,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_param_mark_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số Mark",
        "Code": "\"Mark\";"
    },
    # 10. Native ZeroTouch: Element.SetParameterByName (Mark)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_sp_mark,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_sp_mark_in_elem, "Name": "element", "Description": "Element\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_mark_in_param, "Name": "parameterName", "Description": "parameterName\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_mark_in_val, "Name": "value", "Description": "value\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_sp_mark_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Gán tham số Mark cho 295 dầm"
    },
    # 11. CodeBlock: "z Justification"
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_param_zjust,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_param_zjust_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số z Justification",
        "Code": "\"z Justification\";"
    },
    # 12. CodeBlock: 0 (Top)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_val_zjust,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_val_zjust_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Giá trị Top (0)",
        "Code": "0;"
    },
    # 13. Native ZeroTouch: Element.SetParameterByName (z Justification)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_sp_zjust,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_sp_zjust_in_elem, "Name": "element", "Description": "Element\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_zjust_in_param, "Name": "parameterName", "Description": "parameterName\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_zjust_in_val, "Name": "value", "Description": "value\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_sp_zjust_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Căn phẳng mặt trên dầm (Top = 0)"
    },
    # 14. CodeBlock: "y Justification"
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_param_yjust,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_param_yjust_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số y Justification",
        "Code": "\"y Justification\";"
    },
    # 15. CodeBlock: 2 (Origin)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_val_yjust,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_val_yjust_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Giá trị Origin (2)",
        "Code": "2;"
    },
    # 16. Native ZeroTouch: Element.SetParameterByName (y Justification)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_sp_yjust,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_sp_yjust_in_elem, "Name": "element", "Description": "Element\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_yjust_in_param, "Name": "parameterName", "Description": "parameterName\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_yjust_in_val, "Name": "value", "Description": "value\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_sp_yjust_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Căn tim dầm trùng tim lưới CAD (Origin = 2)"
    },
    # 17. Python: Lấy 295 Z-Offsets
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_zoffsets,
        "Inputs": [
            {"Id": id_zoffsets_in, "Name": "IN[0]", "Description": "File Path", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_zoffsets_out, "Name": "OUT", "Description": "Z-Offsets (Feet)", "Type": "unknown", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Đọc CAD JSON -> Lấy mảng Z-Offset (-900mm cho DK.a')",
        "Code": py_zoffsets,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 18. CodeBlock: "z Offset Value"
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNode",
        "Id": id_cb_param_zoff,
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_param_zoff_out, "Name": "", "Description": "Value at Line 1", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tên tham số z Offset Value",
        "Code": "\"z Offset Value\";"
    },
    # 19. Native ZeroTouch: Element.SetParameterByName (z Offset Value)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.ZeroTouch.DSFunction, DynamoCore",
        "NodeType": "FunctionNode",
        "Id": id_sp_zoff,
        "FunctionSignature": "Revit.Elements.Element.SetParameterByName@string,var",
        "Inputs": [
            {"Id": id_sp_zoff_in_elem, "Name": "element", "Description": "Element\n\nElement", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_zoff_in_param, "Name": "parameterName", "Description": "parameterName\n\nstring", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_sp_zoff_in_val, "Name": "value", "Description": "value\n\nvar", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Outputs": [
            {"Id": id_sp_zoff_out, "Name": "Element", "Description": "Element", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Auto",
        "Description": "Giật hạ cốt đỉnh dầm xuống -1.000m (-900mm) cho DK.a'"
    },
    # 20. Python: Khóa liên kết nút 2 đầu dầm (DisallowJoin)
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
        "Description": "Khóa liên kết nút 2 đầu dầm (StructuralFramingUtils.DisallowJoinAtEnd)",
        "Code": py_disallow,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # 21. Native Watch Node
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

# EXACT STRING PORT GUID CONNECTORS (Autodesk Dynamo 2.x & 3.x Standard)
connectors = [
    # 1. FilePath -> StartPts
    {"Start": id_filepath_out, "End": id_pts_start_in, "Id": make_guid(), "IsHidden": "False"},
    # 2. FilePath -> EndPts
    {"Start": id_filepath_out, "End": id_pts_end_in, "Id": make_guid(), "IsHidden": "False"},
    # 3. FilePath -> Symbols
    {"Start": id_filepath_out, "End": id_syms_in, "Id": make_guid(), "IsHidden": "False"},
    # 4. FilePath -> Marks
    {"Start": id_filepath_out, "End": id_marks_in, "Id": make_guid(), "IsHidden": "False"},
    # 5. FilePath -> ZOffsets
    {"Start": id_filepath_out, "End": id_zoffsets_in, "Id": make_guid(), "IsHidden": "False"},

    # 6. StartPts -> Line startPoint
    {"Start": id_pts_start_out, "End": id_lines_in_sp, "Id": make_guid(), "IsHidden": "False"},
    # 7. EndPts -> Line endPoint
    {"Start": id_pts_end_out, "End": id_lines_in_ep, "Id": make_guid(), "IsHidden": "False"},

    # 8. Lines -> Beam curve
    {"Start": id_lines_out, "End": id_beam_in_curve, "Id": make_guid(), "IsHidden": "False"},
    # 9. Level -> Beam level
    {"Start": id_level_out, "End": id_beam_in_lvl, "Id": make_guid(), "IsHidden": "False"},
    # 10. Symbols -> Beam structuralFramingType
    {"Start": id_syms_out, "End": id_beam_in_sym, "Id": make_guid(), "IsHidden": "False"},

    # 11. Beam -> SetMark element
    {"Start": id_beam_out, "End": id_sp_mark_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 12. CB "Mark" -> SetMark parameterName
    {"Start": id_cb_param_mark_out, "End": id_sp_mark_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 13. Marks List -> SetMark value
    {"Start": id_marks_out, "End": id_sp_mark_in_val, "Id": make_guid(), "IsHidden": "False"},

    # 14. SetMark -> SetZJust element
    {"Start": id_sp_mark_out, "End": id_sp_zjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 15. CB "z Justification" -> SetZJust parameterName
    {"Start": id_cb_param_zjust_out, "End": id_sp_zjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 16. CB 0 -> SetZJust value
    {"Start": id_cb_val_zjust_out, "End": id_sp_zjust_in_val, "Id": make_guid(), "IsHidden": "False"},

    # 17. SetZJust -> SetYJust element
    {"Start": id_sp_zjust_out, "End": id_sp_yjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 18. CB "y Justification" -> SetYJust parameterName
    {"Start": id_cb_param_yjust_out, "End": id_sp_yjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 19. CB 2 -> SetYJust value
    {"Start": id_cb_val_yjust_out, "End": id_sp_yjust_in_val, "Id": make_guid(), "IsHidden": "False"},

    # 20. SetYJust -> SetZOff element
    {"Start": id_sp_yjust_out, "End": id_sp_zoff_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 21. CB "z Offset Value" -> SetZOff parameterName
    {"Start": id_cb_param_zoff_out, "End": id_sp_zoff_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 22. ZOffsets List -> SetZOff value
    {"Start": id_zoffsets_out, "End": id_sp_zoff_in_val, "Id": make_guid(), "IsHidden": "False"},

    # 23. SetZOff -> DisallowJoin
    {"Start": id_sp_zoff_out, "End": id_disallow_in, "Id": make_guid(), "IsHidden": "False"},

    # 24. DisallowJoin -> Watch
    {"Start": id_disallow_out, "End": id_watch_in, "Id": make_guid(), "IsHidden": "False"}
]

# Canvas Layout Positions (X, Y)
node_views = [
    # Group 1: Inputs
    {"Id": id_filepath, "Name": "File Path: CAD JSON Model", "IsSetAsInput": True, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 100.0, "Y": 350.0},
    {"Id": id_pts_start, "Name": "1. Tạo 295 Điểm Đầu (Start Points)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 550.0, "Y": 150.0},
    {"Id": id_pts_end, "Name": "2. Tạo 295 Điểm Cuối (End Points)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 550.0, "Y": 350.0},
    {"Id": id_syms, "Name": "3. Khớp 295 FamilySymbol", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 550.0, "Y": 550.0},
    {"Id": id_marks, "Name": "4. Lấy 295 Mã Mark", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 550.0, "Y": 750.0},
    {"Id": id_zoffsets, "Name": "5. Lấy 295 Cao Độ Z-Offset", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 550.0, "Y": 950.0},

    # Group 2: Tim dầm & Level
    {"Id": id_lines, "Name": "Line.ByStartPointEndPoint", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 1050.0, "Y": 250.0},
    {"Id": id_level, "Name": "Levels ('Tầng 1')", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 1050.0, "Y": 500.0},

    # Group 3: Tạo dầm
    {"Id": id_beam, "Name": "StructuralFraming.BeamByCurve", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 1500.0, "Y": 350.0},

    # Group 4: Tham số Mark
    {"Id": id_cb_param_mark, "Name": "CodeBlock ('Mark')", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 1850.0, "Y": 150.0},
    {"Id": id_sp_mark, "Name": "Element.SetParameterByName (Mark)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 2150.0, "Y": 250.0},

    # Group 5: Căn Top
    {"Id": id_cb_param_zjust, "Name": "CodeBlock ('z Justification')", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 2500.0, "Y": 150.0},
    {"Id": id_cb_val_zjust, "Name": "CodeBlock (0)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 2500.0, "Y": 300.0},
    {"Id": id_sp_zjust, "Name": "Element.SetParameterByName (z Justification)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 2800.0, "Y": 250.0},

    # Group 6: Căn Origin
    {"Id": id_cb_param_yjust, "Name": "CodeBlock ('y Justification')", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 3150.0, "Y": 150.0},
    {"Id": id_cb_val_yjust, "Name": "CodeBlock (2)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 3150.0, "Y": 300.0},
    {"Id": id_sp_yjust, "Name": "Element.SetParameterByName (y Justification)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 3450.0, "Y": 250.0},

    # Group 7: Giật cốt
    {"Id": id_cb_param_zoff, "Name": "CodeBlock ('z Offset Value')", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 3800.0, "Y": 150.0},
    {"Id": id_sp_zoff, "Name": "Element.SetParameterByName (z Offset Value)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 4100.0, "Y": 250.0},

    # Group 8: DisallowJoin & Watch
    {"Id": id_disallow, "Name": "Khóa Nút Liên Kết 2 Đầu (Python)", "IsSetAsInput": False, "IsSetAsOutput": False, "Excluded": False, "ShowGeometry": True, "X": 4550.0, "Y": 250.0},
    {"Id": id_watch, "Name": "Bảng Nghiệm Thu Hệ Dầm", "IsSetAsInput": False, "IsSetAsOutput": True, "Excluded": False, "ShowGeometry": True, "X": 4950.0, "Y": 250.0}
]

annotations = [
    # Group 1: Vàng - Đầu Vào & Trích Xuất CAD
    {
        "Id": make_guid(),
        "Title": "1. ĐẦU VÀO DỮ LIỆU TỪ CAD (FILE PATH & EXTRACTORS)",
        "DescriptionText": None,
        "IsExpanded": True,
        "WidthAdjustment": 0.0,
        "HeightAdjustment": 0.0,
        "Nodes": [id_filepath, id_pts_start, id_pts_end, id_syms, id_marks, id_zoffsets],
        "HasNestedGroups": False,
        "Left": 50.0,
        "Top": 80.0,
        "Width": 900.0,
        "Height": 1050.0,
        "FontSize": 36.0,
        "GroupStyleId": "00000000-0000-0000-0000-000000000000",
        "InitialTop": 80.0,
        "InitialHeight": 1050.0,
        "TextblockHeight": 45.0,
        "Background": "#FFFFC048"
    },
    # Group 2: Xanh Lá - Hình Học Tim Dầm & Level
    {
        "Id": make_guid(),
        "Title": "2. HÌNH HỌC TIM DẦM & LEVEL (GEOMETRY & LEVEL)",
        "DescriptionText": None,
        "IsExpanded": True,
        "WidthAdjustment": 0.0,
        "HeightAdjustment": 0.0,
        "Nodes": [id_lines, id_level],
        "HasNestedGroups": False,
        "Left": 1000.0,
        "Top": 150.0,
        "Width": 420.0,
        "Height": 550.0,
        "FontSize": 36.0,
        "GroupStyleId": "00000000-0000-0000-0000-000000000000",
        "InitialTop": 150.0,
        "InitialHeight": 550.0,
        "TextblockHeight": 45.0,
        "Background": "#FF20BF6B"
    },
    # Group 3: Tím - Khởi Tạo Dầm
    {
        "Id": make_guid(),
        "Title": "3. KHỞI TẠO CẤU KIỆN DẦM REVIT (BEAM BY CURVE)",
        "DescriptionText": None,
        "IsExpanded": True,
        "WidthAdjustment": 0.0,
        "HeightAdjustment": 0.0,
        "Nodes": [id_beam],
        "HasNestedGroups": False,
        "Left": 1450.0,
        "Top": 200.0,
        "Width": 350.0,
        "Height": 450.0,
        "FontSize": 36.0,
        "GroupStyleId": "00000000-0000-0000-0000-000000000000",
        "InitialTop": 200.0,
        "InitialHeight": 450.0,
        "TextblockHeight": 45.0,
        "Background": "#FF8854D0"
    },
    # Group 4: Cam - Chuỗi Gán Tham Số Quản Lý
    {
        "Id": make_guid(),
        "Title": "4. CHUỖI GÁN THAM SỐ TỰ ĐỘNG (MARK, TOP, ORIGIN, Z-OFFSET)",
        "DescriptionText": None,
        "IsExpanded": True,
        "WidthAdjustment": 0.0,
        "HeightAdjustment": 0.0,
        "Nodes": [id_cb_param_mark, id_sp_mark, id_cb_param_zjust, id_cb_val_zjust, id_sp_zjust, id_cb_param_yjust, id_cb_val_yjust, id_sp_yjust, id_cb_param_zoff, id_sp_zoff],
        "HasNestedGroups": False,
        "Left": 1820.0,
        "Top": 80.0,
        "Width": 2650.0,
        "Height": 550.0,
        "FontSize": 36.0,
        "GroupStyleId": "00000000-0000-0000-0000-000000000000",
        "InitialTop": 80.0,
        "InitialHeight": 550.0,
        "TextblockHeight": 45.0,
        "Background": "#FFFF9F43"
    },
    # Group 5: Đỏ/Hồng - Nghiệm Thu & Khóa Nút
    {
        "Id": make_guid(),
        "Title": "5. KHÓA NÚT LIÊN KẾT & BẢNG NGHIỆM THU (DISALLOW JOIN & WATCH)",
        "DescriptionText": None,
        "IsExpanded": True,
        "WidthAdjustment": 0.0,
        "HeightAdjustment": 0.0,
        "Nodes": [id_disallow, id_watch],
        "HasNestedGroups": False,
        "Left": 4500.0,
        "Top": 150.0,
        "Width": 850.0,
        "Height": 500.0,
        "FontSize": 36.0,
        "GroupStyleId": "00000000-0000-0000-0000-000000000000",
        "InitialTop": 150.0,
        "InitialHeight": 500.0,
        "TextblockHeight": 45.0,
        "Background": "#FFEB3B5A"
    }
]

dyn_data = {
    "Uuid": "da28c993-04c7-46a6-be35-ee05c430eb47",
    "IsCustomNode": False,
    "Description": "Đồ thị trực quan tự động dựng toàn bộ 295 cấu kiện dầm sàn/đà kiềng Tầng 1 từ bản vẽ CAD (đầy đủ đầu vào File Path, tự động nối liên hoàn 24 dây nối giữa 21 nodes).",
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
    "Inputs": [
        {
            "Id": id_filepath,
            "Name": "File Path: CAD JSON Model",
            "Type": "string",
            "Type2": "string",
            "Value": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json",
            "Description": "Duyệt chọn file JSON bóc tách chi tiết CAD"
        }
    ],
    "Outputs": [
        {
            "Id": id_watch,
            "Name": "Watch: Bảng Nghiệm Thu Hệ Dầm",
            "Type": "unknown",
            "InitialValue": "",
            "Description": "Bảng nghiệm thu toàn bộ hệ dầm Tầng 1"
        }
    ],
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

print(f"SUCCESS: Generated 100% interconnected Dynamo graph with {len(nodes)} nodes, {len(connectors)} wires, and {len(annotations)} groups!")
