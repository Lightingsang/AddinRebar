# -*- coding: utf-8 -*-
import json, uuid

def make_guid():
    return str(uuid.uuid4()).replace("-", "")

# Load the verified model data
model_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json"
with open(model_path, "r", encoding="utf-8") as f:
    model_data = json.load(f)

beams = model_data["beams"]
# Prepare lists for native Dynamo CodeBlocks
# We convert coordinates to feet (Revit internal units)
sx_list = [round(b["StartX"] / 304.8, 5) for b in beams]
sy_list = [round(b["StartY"] / 304.8, 5) for b in beams]
ex_list = [round(b["EndX"] / 304.8, 5) for b in beams]
ey_list = [round(b["EndY"] / 304.8, 5) for b in beams]
marks_list = [b["Mark"] for b in beams]
zoff_list = [round(b.get("ZOffsetMm", 0) / 304.8, 5) for b in beams]

# Node GUIDs
id_cb_start = make_guid()
id_cb_start_out_x = make_guid()
id_cb_start_out_y = make_guid()

id_pt_start = make_guid()
id_pt_start_in_x = make_guid()
id_pt_start_in_y = make_guid()
id_pt_start_in_z = make_guid()
id_pt_start_out = make_guid()

id_cb_end = make_guid()
id_cb_end_out_x = make_guid()
id_cb_end_out_y = make_guid()

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

id_framing_type = make_guid()
id_framing_type_out = make_guid()

id_beam_by_curve = make_guid()
id_beam_in_curve = make_guid()
id_beam_in_level = make_guid()
id_beam_in_type = make_guid()
id_beam_out = make_guid()

id_cb_mark = make_guid()
id_cb_mark_out_name = make_guid()
id_cb_mark_out_val = make_guid()

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

id_cb_zoff = make_guid()
id_cb_zoff_out_name = make_guid()
id_cb_zoff_out_val = make_guid()

id_set_zoff = make_guid()
id_set_zoff_in_elem = make_guid()
id_set_zoff_in_param = make_guid()
id_set_zoff_in_val = make_guid()
id_set_zoff_out = make_guid()

id_watch = make_guid()
id_watch_in = make_guid()
id_watch_out = make_guid()

nodes = [
    # 1. CodeBlock: Start Coordinates (X, Y)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_start,
        "CodeText": f"// 295 Tọa độ X1, Y1 (Start Points)\nstartX = {json.dumps(sx_list)};\nstartY = {json.dumps(sy_list)};",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_start_out_x, "Name": "", "Description": "startX", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_start_out_y, "Name": "", "Description": "startY", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Dữ liệu 295 tọa độ điểm đầu từ CAD"
    },
    # 2. Native ZeroTouch: Point.ByCoordinates (Start)
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
    # 3. CodeBlock: End Coordinates (X, Y)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_end,
        "CodeText": f"// 295 Tọa độ X2, Y2 (End Points)\nendX = {json.dumps(ex_list)};\nendY = {json.dumps(ey_list)};",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_end_out_x, "Name": "", "Description": "endX", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_end_out_y, "Name": "", "Description": "endY", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Dữ liệu 295 tọa độ điểm cuối từ CAD"
    },
    # 4. Native ZeroTouch: Point.ByCoordinates (End)
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
    # 5. Native ZeroTouch: Line.ByStartPointEndPoint
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
        "Description": "Dựng 295 đường tim dầm từ điểm đầu và điểm cuối"
    },
    # 6. Native Dropdown: Levels (Tầng 1)
    {
        "ConcreteType": "DSRevitNodesUI.Levels, DSRevitNodesUI",
        "NodeType": "ExtensionNode",
        "Id": id_level,
        "SelectedIndex": 0,
        "SelectedString": "Tầng 1",
        "Inputs": [],
        "Outputs": [
            {"Id": id_level_out, "Name": "Levels", "Description": "Cấp độ sàn đã chọn", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Chọn cao trình Tầng 1"
    },
    # 7. Native Dropdown: StructuralFramingTypes (B_200x400)
    {
        "ConcreteType": "DSRevitNodesUI.StructuralFramingTypes, DSRevitNodesUI",
        "NodeType": "ExtensionNode",
        "Id": id_framing_type,
        "SelectedIndex": 0,
        "SelectedString": "B_200x400",
        "Inputs": [],
        "Outputs": [
            {"Id": id_framing_type_out, "Name": "Framing Types", "Description": "Tiết diện dầm", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Chọn tiết diện khung bê tông"
    },
    # 8. Native ZeroTouch: StructuralFraming.BeamByCurve
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
        "Description": "Tạo 295 phần tử Dầm trong Revit"
    },
    # 9. CodeBlock: Mark Parameter
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_mark,
        "CodeText": f"paramName = \"Mark\";\nmarks = {json.dumps(marks_list)};",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_mark_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_mark_out_val, "Name": "", "Description": "marks", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Tham số Mark 295 dầm"
    },
    # 10. Native ZeroTouch: Element.SetParameterByName (Mark)
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
    # 11. CodeBlock: Z Justification (Top)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_zjust,
        "CodeText": "paramName = \"z Justification\";\nvalue = 0;",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_zjust_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_zjust_out_val, "Name": "", "Description": "value", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Căn chỉnh mặt trên phẳng (Top = 0)"
    },
    # 12. Native ZeroTouch: Element.SetParameterByName (z Justification)
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
    # 13. CodeBlock: Y Justification (Origin)
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_yjust,
        "CodeText": "paramName = \"y Justification\";\nvalue = 2;",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_yjust_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_yjust_out_val, "Name": "", "Description": "value", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Căn đúng tim dầm (Origin = 2)"
    },
    # 14. Native ZeroTouch: Element.SetParameterByName (y Justification)
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
    # 15. CodeBlock: Z Offset Value (-900mm cho DK.a')
    {
        "ConcreteType": "Dynamo.Graph.Nodes.CodeBlockNodeModel, DynamoCore",
        "NodeType": "CodeBlockNodeModel",
        "Id": id_cb_zoff,
        "CodeText": f"paramName = \"z Offset Value\";\nzOffsets = {json.dumps(zoff_list)};",
        "Inputs": [],
        "Outputs": [
            {"Id": id_cb_zoff_out_name, "Name": "", "Description": "paramName", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False},
            {"Id": id_cb_zoff_out_val, "Name": "", "Description": "zOffsets", "UsingDefaultValue": False, "Level": 2, "UseLevels": False, "KeepListStructure": False}
        ],
        "Replication": "Disabled",
        "Description": "Hạ cốt -1.000m (zOffset = -900mm) cho DK.a'"
    },
    # 16. Native ZeroTouch: Element.SetParameterByName (z Offset Value)
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
        "Description": "Giật cao trình Z-Offset dầm"
    },
    # 17. Native Watch Node
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
        "Description": "Hiển thị danh sách 295 phần tử Dầm đã khởi tạo"
    }
]

# EXACT DYNAMO 2.x & 3.x CONNECTORS FORMAT (Start and End are port IDs directly as strings)
connectors = [
    # 1. Start X -> PtStart X
    {"Start": id_cb_start_out_x, "End": id_pt_start_in_x, "Id": make_guid(), "IsHidden": "False"},
    # 2. Start Y -> PtStart Y
    {"Start": id_cb_start_out_y, "End": id_pt_start_in_y, "Id": make_guid(), "IsHidden": "False"},
    
    # 3. End X -> PtEnd X
    {"Start": id_cb_end_out_x, "End": id_pt_end_in_x, "Id": make_guid(), "IsHidden": "False"},
    # 4. End Y -> PtEnd Y
    {"Start": id_cb_end_out_y, "End": id_pt_end_in_y, "Id": make_guid(), "IsHidden": "False"},
    
    # 5. PtStart -> Line StartPoint
    {"Start": id_pt_start_out, "End": id_line_in_sp, "Id": make_guid(), "IsHidden": "False"},
    # 6. PtEnd -> Line EndPoint
    {"Start": id_pt_end_out, "End": id_line_in_ep, "Id": make_guid(), "IsHidden": "False"},
    
    # 7. Line -> BeamByCurve Curve
    {"Start": id_line_out, "End": id_beam_in_curve, "Id": make_guid(), "IsHidden": "False"},
    # 8. Level -> BeamByCurve Level
    {"Start": id_level_out, "End": id_beam_in_level, "Id": make_guid(), "IsHidden": "False"},
    # 9. FramingType -> BeamByCurve Type
    {"Start": id_framing_type_out, "End": id_beam_in_type, "Id": make_guid(), "IsHidden": "False"},
    
    # 10. Beam -> SetMark Element
    {"Start": id_beam_out, "End": id_set_mark_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 11. Mark ParamName -> SetMark ParameterName
    {"Start": id_cb_mark_out_name, "End": id_set_mark_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 12. Marks List -> SetMark Value
    {"Start": id_cb_mark_out_val, "End": id_set_mark_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 13. SetMark Element -> SetZJust Element
    {"Start": id_set_mark_out, "End": id_set_zjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 14. ZJust ParamName -> SetZJust ParameterName
    {"Start": id_cb_zjust_out_name, "End": id_set_zjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 15. ZJust Value -> SetZJust Value
    {"Start": id_cb_zjust_out_val, "End": id_set_zjust_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 16. SetZJust Element -> SetYJust Element
    {"Start": id_set_zjust_out, "End": id_set_yjust_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 17. YJust ParamName -> SetYJust ParameterName
    {"Start": id_cb_yjust_out_name, "End": id_set_yjust_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 18. YJust Value -> SetYJust Value
    {"Start": id_cb_yjust_out_val, "End": id_set_yjust_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 19. SetYJust Element -> SetZOff Element
    {"Start": id_set_yjust_out, "End": id_set_zoff_in_elem, "Id": make_guid(), "IsHidden": "False"},
    # 20. ZOff ParamName -> SetZOff ParameterName
    {"Start": id_cb_zoff_out_name, "End": id_set_zoff_in_param, "Id": make_guid(), "IsHidden": "False"},
    # 21. ZOff List -> SetZOff Value
    {"Start": id_cb_zoff_out_val, "End": id_set_zoff_in_val, "Id": make_guid(), "IsHidden": "False"},
    
    # 22. SetZOff Element -> Watch
    {"Start": id_set_zoff_out, "End": id_watch_in, "Id": make_guid(), "IsHidden": "False"}
]

# Coordinates layout on Canvas (X, Y)
node_views = [
    # Group 1: Start & End Points
    {"Id": id_cb_start, "Name": "Code Block (Tọa độ X1, Y1)", "X": 100.0, "Y": 150.0},
    {"Id": id_pt_start, "Name": "Point.ByCoordinates (Start)", "X": 600.0, "Y": 150.0},
    {"Id": id_cb_end, "Name": "Code Block (Tọa độ X2, Y2)", "X": 100.0, "Y": 500.0},
    {"Id": id_pt_end, "Name": "Point.ByCoordinates (End)", "X": 600.0, "Y": 500.0},
    
    # Group 2: Tim Dầm (Lines)
    {"Id": id_line, "Name": "Line.ByStartPointEndPoint", "X": 1000.0, "Y": 300.0},
    
    # Group 3: Level & Framing Type
    {"Id": id_level, "Name": "Levels ('Tầng 1')", "X": 1000.0, "Y": 650.0},
    {"Id": id_framing_type, "Name": "Structural Framing Types ('B_200x400')", "X": 1000.0, "Y": 850.0},
    
    # Group 4: Tạo Dầm (BeamByCurve)
    {"Id": id_beam_by_curve, "Name": "StructuralFraming.BeamByCurve", "X": 1500.0, "Y": 400.0},
    
    # Group 5: Gán Mark
    {"Id": id_cb_mark, "Name": "Code Block (Mark)", "X": 1850.0, "Y": 150.0},
    {"Id": id_set_mark, "Name": "Element.SetParameterByName (Mark)", "X": 2250.0, "Y": 250.0},
    
    # Group 6: Căn Chỉnh Z Justification (Top)
    {"Id": id_cb_zjust, "Name": "Code Block (z Justification = 0)", "X": 2600.0, "Y": 150.0},
    {"Id": id_set_zjust, "Name": "Element.SetParameterByName (z Justification)", "X": 2950.0, "Y": 250.0},
    
    # Group 7: Căn Chỉnh Y Justification (Origin)
    {"Id": id_cb_yjust, "Name": "Code Block (y Justification = 2)", "X": 3300.0, "Y": 150.0},
    {"Id": id_set_yjust, "Name": "Element.SetParameterByName (y Justification)", "X": 3650.0, "Y": 250.0},
    
    # Group 8: Giật Cốt Z-Offset
    {"Id": id_cb_zoff, "Name": "Code Block (z Offset Value)", "X": 4000.0, "Y": 150.0},
    {"Id": id_set_zoff, "Name": "Element.SetParameterByName (z Offset Value)", "X": 4350.0, "Y": 250.0},
    
    # Group 9: Watch
    {"Id": id_watch, "Name": "Watch (295 Dầm Đã Dựng)", "X": 4800.0, "Y": 250.0}
]

annotations = [
    # Group 1: Hình học Điểm & Tim Dầm
    {
        "Id": make_guid(),
        "Title": "1. HÌNH HỌC TIM DẦM (POINTS & LINES BY COORDINATES)",
        "Nodes": [id_cb_start, id_pt_start, id_cb_end, id_pt_end, id_line],
        "Left": 50.0,
        "Top": 80.0,
        "Width": 1300.0,
        "Height": 750.0,
        "FontSize": 32.0,
        "Background": "#FF20BF6B"
    },
    # Group 2: Level & Tiết diện Dầm
    {
        "Id": make_guid(),
        "Title": "2. THIẾT LẬP CAO TRÌNH & TIẾT DIỆN (LEVELS & FRAMING TYPES)",
        "Nodes": [id_level, id_framing_type],
        "Left": 950.0,
        "Top": 580.0,
        "Width": 400.0,
        "Height": 450.0,
        "FontSize": 32.0,
        "Background": "#FF2D98DA"
    },
    # Group 3: Khởi tạo phần tử dầm
    {
        "Id": make_guid(),
        "Title": "3. KHỞI TẠO CẤU KIỆN DẦM REVIT (BEAM BY CURVE)",
        "Nodes": [id_beam_by_curve],
        "Left": 1450.0,
        "Top": 300.0,
        "Width": 350.0,
        "Height": 400.0,
        "FontSize": 32.0,
        "Background": "#FF8854D0"
    },
    # Group 4: Chuỗi Gán Tham Số Quản Lý & Định Vị
    {
        "Id": make_guid(),
        "Title": "4. CHUỖI GÁN THAM SỐ TỰ ĐỘNG (SET PARAMETER BY NAME: MARK, TOP, ORIGIN, Z-OFFSET)",
        "Nodes": [id_cb_mark, id_set_mark, id_cb_zjust, id_set_zjust, id_cb_yjust, id_set_yjust, id_cb_zoff, id_set_zoff],
        "Left": 1820.0,
        "Top": 80.0,
        "Width": 2900.0,
        "Height": 550.0,
        "FontSize": 32.0,
        "Background": "#FFFFC048"
    },
    # Group 5: Nghiệm Thu
    {
        "Id": make_guid(),
        "Title": "5. BẢNG NGHIỆM THU (WATCH)",
        "Nodes": [id_watch],
        "Left": 4750.0,
        "Top": 150.0,
        "Width": 400.0,
        "Height": 450.0,
        "FontSize": 32.0,
        "Background": "#FFEB3B5A"
    }
]

dyn_data = {
    "Uuid": "da28c993-04c7-46a6-be35-ee05c430eb47",
    "IsCustomNode": False,
    "Description": "Đồ thị 100% Visual Native Nodes Dynamo & Revit (Point.ByCoordinates, Line.ByStartPointEndPoint, StructuralFraming.BeamByCurve, Element.SetParameterByName) tự nối hoàn toàn không dùng Python Script.",
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
        "Zoom": 0.5
    }
}

target_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"
with open(target_file, "w", encoding="utf-8") as f:
    json.dump(dyn_data, f, indent=2, ensure_ascii=False)

print(f"SUCCESS: Generated 100% Visual Native Dynamo graph with string port connectors! {len(nodes)} nodes, {len(connectors)} wires!")
