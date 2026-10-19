# -*- coding: utf-8 -*-
import json, uuid

def make_guid():
    return str(uuid.uuid4()).replace("-", "")

# 1. Generate GUIDs for nodes and ports
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
id_pt_start_in = make_guid()
id_pt_start_out = make_guid()

id_pt_end = make_guid()
id_pt_end_in = make_guid()
id_pt_end_out = make_guid()

id_lines = make_guid()
id_lines_in_start = make_guid()
id_lines_in_end = make_guid()
id_lines_out = make_guid()

id_lvl = make_guid()
id_lvl_in = make_guid()
id_lvl_out = make_guid()

id_syms = make_guid()
id_syms_in = make_guid()
id_syms_out = make_guid()

id_create_beams = make_guid()
id_create_beams_in_lines = make_guid()
id_create_beams_in_lvl = make_guid()
id_create_beams_in_syms = make_guid()
id_create_beams_out = make_guid()

id_params = make_guid()
id_params_in_beams = make_guid()
id_params_in_marks = make_guid()
id_params_in_zoff = make_guid()
id_params_out = make_guid()

id_disallow = make_guid()
id_disallow_in = make_guid()
id_disallow_out = make_guid()

id_watch = make_guid()
id_watch_in = make_guid()
id_watch_out = make_guid()

# Python Code for Node 2: Đọc file JSON CAD
code_read_json = """# -*- coding: utf-8 -*-
import json

path = IN[0]
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

beams = data['beams']
OUT = {
    'sx': [b['StartX'] for b in beams],
    'sy': [b['StartY'] for b in beams],
    'ex': [b['EndX'] for b in beams],
    'ey': [b['EndY'] for b in beams],
    'types': [b['TypeName'] for b in beams],
    'marks': [b['Mark'] for b in beams],
    'zOffsets': [b.get('ZOffsetMm', 0) for b in beams],
    'levelName': data.get('targetLevel', 'Tầng 1')
}
"""

# Code for Node 4: Point Start
code_pt_start = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import Point

sx_list = IN[0]['sx']
sy_list = IN[0]['sy']
pts = []
for x, y in zip(sx_list, sy_list):
    pts.append(Point.ByCoordinates(float(x) / 304.8, float(y) / 304.8, 0.0))
OUT = pts
"""

# Code for Node 5: Point End
code_pt_end = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import Point

ex_list = IN[0]['ex']
ey_list = IN[0]['ey']
pts = []
for x, y in zip(ex_list, ey_list):
    pts.append(Point.ByCoordinates(float(x) / 304.8, float(y) / 304.8, 0.0))
OUT = pts
"""

# Code for Node 6: Line by Start & End
code_lines = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import Line

starts = IN[0]
ends = IN[1]
lines = []
for s, e in zip(starts, ends):
    lines.append(Line.CreateBound(s, e) if hasattr(Line, 'CreateBound') else Line.ByStartPointEndPoint(s, e))
OUT = lines
"""

# Code for Node 7: Get Level
code_lvl = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')
from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager

doc = DocumentManager.Instance.CurrentDBDocument
name = IN[0]['levelName'] if isinstance(IN[0], dict) else 'Tầng 1'
lvl = None
for l in FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType():
    if l.Name == name:
        lvl = l
        break
OUT = lvl
"""

# Code for Node 8: Get FamilySymbols
code_syms = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')
from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager

doc = DocumentManager.Instance.CurrentDBDocument
type_names = IN[0]['types'] if isinstance(IN[0], dict) else IN[0]
symbols = {}
for s in FilteredElementCollector(doc).OfClass(FamilySymbol).OfCategory(BuiltInCategory.OST_StructuralFraming):
    symbols[s.Name] = s

OUT = [symbols.get(tn) for tn in type_names]
"""

# Code for Node 9: Create Beams
code_create_beams = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')
from Autodesk.Revit.DB import *
from Autodesk.Revit.DB.Structure import StructuralType
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument
lines = IN[0]
level = IN[1]
symbols = IN[2]

created_beams = []
TransactionManager.Instance.EnsureInTransaction(doc)
try:
    for geom_line, sym in zip(lines, symbols):
        if not sym:
            created_beams.append(None)
            continue
        if not sym.IsActive:
            sym.Activate()
        
        # Convert Dynamo Line to Revit Curve if needed
        sp = geom_line.StartPoint
        ep = geom_line.EndPoint
        p1 = XYZ(sp.X, sp.Y, sp.Z)
        p2 = XYZ(ep.X, ep.Y, ep.Z)
        curve = Line.CreateBound(p1, p2)
        
        b = doc.Create.NewFamilyInstance(curve, sym, level, StructuralType.Beam)
        created_beams.append(b)
    TransactionManager.Instance.TransactionTaskDone()
except Exception as e:
    TransactionManager.Instance.ForceCloseTransaction()
    raise e

OUT = created_beams
"""

# Code for Node 10: Set Parameters
code_params = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitServices')
from Autodesk.Revit.DB import *
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

doc = DocumentManager.Instance.CurrentDBDocument
beams = IN[0]
marks = IN[1]['marks']
z_offsets = IN[1]['zOffsets']

TransactionManager.Instance.EnsureInTransaction(doc)
try:
    for b, m, z_off in zip(beams, marks, z_offsets):
        if not b: continue
        
        # Mark
        pm = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
        if pm and not pm.IsReadOnly: pm.Set(str(m))
        
        # Y Justification: Origin (2)
        py = b.get_Parameter(BuiltInParameter.Y_JUSTIFICATION)
        if py and not py.IsReadOnly: py.Set(2)
        
        # Z Justification: Top (0)
        pz = b.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)
        if pz and not pz.IsReadOnly: pz.Set(0)
        
        # Z Offset Value
        if float(z_off) != 0.0:
            pzoff = b.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)
            if pzoff and not pzoff.IsReadOnly:
                pzoff.Set(float(z_off) / 304.8)
                
    TransactionManager.Instance.TransactionTaskDone()
except Exception as e:
    TransactionManager.Instance.ForceCloseTransaction()
    raise e

OUT = beams
"""

# Code for Node 11: DisallowJoin
code_disallow = """# -*- coding: utf-8 -*-
import clr
clr.AddReference('RevitAPI')
from Autodesk.Revit.DB.Structure import StructuralFramingUtils

beams = IN[0]
count = 0
for b in beams:
    if b:
        StructuralFramingUtils.DisallowJoinAtEnd(b, 0)
        StructuralFramingUtils.DisallowJoinAtEnd(b, 1)
        count += 1

OUT = {
    'status': 'SUCCESS',
    'totalBeams': count,
    'message': 'Đã khởi tạo và khóa liên kết 2 đầu 295/295 dầm thành công!'
}
"""

nodes = [
    # Node 1: File Path
    {
        "ConcreteType": "CoreNodeModels.Input.StringInput, CoreNodeModels",
        "Id": id_filepath,
        "NodeType": "StringInputNode",
        "Inputs": [],
        "Outputs": [
            {
                "Id": id_filepath_out,
                "Name": "",
                "Description": "String",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Đường dẫn file dữ liệu CAD JSON bóc tách",
        "InputValue": r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\story1_grade_beams_model.json"
    },
    # Node 2: Đọc JSON
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_read_json,
        "Inputs": [
            {
                "Id": id_read_json_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_read_json_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Đọc & Phân tích dữ liệu 295 dầm CAD từ file JSON",
        "Code": code_read_json,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 3: Point Start
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_pt_start,
        "Inputs": [
            {
                "Id": id_pt_start_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_pt_start_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Tạo danh sách 295 Start Points",
        "Code": code_pt_start,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 4: Point End
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_pt_end,
        "Inputs": [
            {
                "Id": id_pt_end_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_pt_end_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Tạo danh sách 295 End Points",
        "Code": code_pt_end,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 5: Lines ByStartPointEndPoint
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_lines,
        "Inputs": [
            {
                "Id": id_lines_in_start,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            },
            {
                "Id": id_lines_in_end,
                "Name": "IN[1]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_lines_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Dựng 295 đường tim dầm (Line.ByStartPointEndPoint)",
        "Code": code_lines,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 6: Level ByName
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_lvl,
        "Inputs": [
            {
                "Id": id_lvl_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_lvl_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Lấy đối tượng Level 'Tầng 1'",
        "Code": code_lvl,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 7: FamilySymbols
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_syms,
        "Inputs": [
            {
                "Id": id_syms_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_syms_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Khớp nối 295 FamilySymbol tương ứng từng tiết diện",
        "Code": code_syms,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 8: StructuralFraming.BeamByCurve
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_create_beams,
        "Inputs": [
            {
                "Id": id_create_beams_in_lines,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            },
            {
                "Id": id_create_beams_in_lvl,
                "Name": "IN[1]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            },
            {
                "Id": id_create_beams_in_syms,
                "Name": "IN[2]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_create_beams_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Khởi tạo 295 phần tử Dầm (StructuralFraming.BeamByCurve)",
        "Code": code_create_beams,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 9: Set Parameters
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_params,
        "Inputs": [
            {
                "Id": id_params_in_beams,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            },
            {
                "Id": id_params_in_marks,
                "Name": "IN[1]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_params_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Gán Mark, Căn chỉnh Top/Origin & Z-Offset (-900mm cho DK.a')",
        "Code": code_params,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 10: DisallowJoin
    {
        "ConcreteType": "PythonNodeModels.PythonNode, PythonNodeModels",
        "NodeType": "PythonScriptNode",
        "Id": id_disallow,
        "Inputs": [
            {
                "Id": id_disallow_in,
                "Name": "IN[0]",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_disallow_out,
                "Name": "OUT",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Khóa liên kết nút 2 đầu dầm (DisallowJoinAtEnd)",
        "Code": code_disallow,
        "Engine": "CPython3",
        "VariableInputPorts": True
    },
    # Node 11: Watch Node
    {
        "ConcreteType": "CoreNodeModels.Watch, CoreNodeModels",
        "WatchWidth": 260.0,
        "WatchHeight": 110.0,
        "Id": id_watch,
        "NodeType": "ExtensionNode",
        "Inputs": [
            {
                "Id": id_watch_in,
                "Name": "",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Outputs": [
            {
                "Id": id_watch_out,
                "Name": "",
                "Type": "unknown",
                "UsingDefaultValue": False,
                "Level": 2,
                "UseLevels": False,
                "KeepListStructure": False
            }
        ],
        "Replication": "Disabled",
        "Description": "Hiển thị kết quả kiểm toán nghiệm thu"
    }
]

connectors = [
    # 1. FilePath -> ReadJSON
    {
        "Start": {"Id": id_filepath, "Index": 0, "PortType": 0},
        "End": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 2. ReadJSON -> PtStart
    {
        "Start": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "End": {"Id": id_pt_start, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 3. ReadJSON -> PtEnd
    {
        "Start": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "End": {"Id": id_pt_end, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 4. PtStart -> Lines[0]
    {
        "Start": {"Id": id_pt_start, "Index": 0, "PortType": 0},
        "End": {"Id": id_lines, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 5. PtEnd -> Lines[1]
    {
        "Start": {"Id": id_pt_end, "Index": 0, "PortType": 0},
        "End": {"Id": id_lines, "Index": 1, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 6. ReadJSON -> Level
    {
        "Start": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "End": {"Id": id_lvl, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 7. ReadJSON -> Symbols
    {
        "Start": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "End": {"Id": id_syms, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 8. Lines -> CreateBeams[0]
    {
        "Start": {"Id": id_lines, "Index": 0, "PortType": 0},
        "End": {"Id": id_create_beams, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 9. Level -> CreateBeams[1]
    {
        "Start": {"Id": id_lvl, "Index": 0, "PortType": 0},
        "End": {"Id": id_create_beams, "Index": 1, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 10. Symbols -> CreateBeams[2]
    {
        "Start": {"Id": id_syms, "Index": 0, "PortType": 0},
        "End": {"Id": id_create_beams, "Index": 2, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 11. CreateBeams -> SetParams[0]
    {
        "Start": {"Id": id_create_beams, "Index": 0, "PortType": 0},
        "End": {"Id": id_params, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 12. ReadJSON -> SetParams[1]
    {
        "Start": {"Id": id_read_json, "Index": 0, "PortType": 0},
        "End": {"Id": id_params, "Index": 1, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 13. SetParams -> DisallowJoin
    {
        "Start": {"Id": id_params, "Index": 0, "PortType": 0},
        "End": {"Id": id_disallow, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    },
    # 14. DisallowJoin -> Watch
    {
        "Start": {"Id": id_disallow, "Index": 0, "PortType": 0},
        "End": {"Id": id_watch, "Index": 0, "PortType": 0},
        "Id": make_guid(),
        "IsHidden": "False"
    }
]

# Layout Positions (X, Y)
node_views = [
    # Group 1: Inputs
    {"Id": id_filepath, "Name": "File Path: CAD JSON Model", "X": 100.0, "Y": 250.0},
    {"Id": id_read_json, "Name": "Đọc & Phân Tích CAD JSON", "X": 500.0, "Y": 250.0},
    
    # Group 2: Geometry
    {"Id": id_pt_start, "Name": "Point.ByCoordinates (Start)", "X": 950.0, "Y": 150.0},
    {"Id": id_pt_end, "Name": "Point.ByCoordinates (End)", "X": 950.0, "Y": 400.0},
    {"Id": id_lines, "Name": "Line.ByStartPointEndPoint", "X": 1350.0, "Y": 275.0},
    
    # Group 3: Level & Types
    {"Id": id_lvl, "Name": "Level.ByName ('Tầng 1')", "X": 950.0, "Y": 650.0},
    {"Id": id_syms, "Name": "FamilySymbol (295 Tiết Diện)", "X": 1350.0, "Y": 650.0},
    
    # Group 4: Framing & Params
    {"Id": id_create_beams, "Name": "StructuralFraming.BeamByCurve", "X": 1800.0, "Y": 350.0},
    {"Id": id_params, "Name": "Gán Tham Số: Mark, Căn Chỉnh, Z-Offset", "X": 2250.0, "Y": 350.0},
    
    # Group 5: DisallowJoin & Audit
    {"Id": id_disallow, "Name": "Khóa Nút Liên Kết (DisallowJoin)", "X": 2700.0, "Y": 350.0},
    {"Id": id_watch, "Name": "Nghiệm Thu Hệ Dầm", "X": 3100.0, "Y": 350.0}
]

annotations = [
    # Group 1: Vàng
    {
        "Id": make_guid(),
        "Title": "1. DỮ LIỆU ĐẦU VÀO TỪ CAD (INPUTS & CAD DATA)",
        "Nodes": [id_filepath, id_read_json],
        "Left": 50.0,
        "Top": 100.0,
        "Width": 800.0,
        "Height": 450.0,
        "FontSize": 32.0,
        "Background": "#FFFFC048"
    },
    # Group 2: Xanh Lá
    {
        "Id": make_guid(),
        "Title": "2. XỬ LÝ HÌNH HỌC TIM DẦM (GEOMETRY PIPELINE)",
        "Nodes": [id_pt_start, id_pt_end, id_lines],
        "Left": 900.0,
        "Top": 50.0,
        "Width": 800.0,
        "Height": 550.0,
        "FontSize": 32.0,
        "Background": "#FF20BF6B"
    },
    # Group 3: Xanh Dương
    {
        "Id": make_guid(),
        "Title": "3. THIẾT LẬP LEVEL & FAMILY SYMBOLS",
        "Nodes": [id_lvl, id_syms],
        "Left": 900.0,
        "Top": 620.0,
        "Width": 800.0,
        "Height": 350.0,
        "FontSize": 32.0,
        "Background": "#FF2D98DA"
    },
    # Group 4: Tím
    {
        "Id": make_guid(),
        "Title": "4. KHỞI TẠO DẦM & THIẾT LẬP THAM SỐ (STRUCTURAL FRAMING)",
        "Nodes": [id_create_beams, id_params],
        "Left": 1750.0,
        "Top": 200.0,
        "Width": 850.0,
        "Height": 500.0,
        "FontSize": 32.0,
        "Background": "#FF8854D0"
    },
    # Group 5: Đỏ/Hồng
    {
        "Id": make_guid(),
        "Title": "5. KHÓA NÚT LIÊN KẾT & NGHIỆM THU (DISALLOW JOIN & WATCH)",
        "Nodes": [id_disallow, id_watch],
        "Left": 2650.0,
        "Top": 200.0,
        "Width": 800.0,
        "Height": 500.0,
        "FontSize": 32.0,
        "Background": "#FFEB3B5A"
    }
]

dyn_data = {
    "Uuid": "da28c993-04c7-46a6-be35-ee05c430eb47",
    "IsCustomNode": False,
    "Description": "Đồ thị trực quan (Visual Nodes & Wires) tự động dựng toàn bộ 295 cấu kiện dầm sàn/đà kiềng Tầng 1 từ bản vẽ CAD (đã chuẩn hóa 100% tiết diện và cao độ theo chi tiết kết cấu CAD DK.a, DK.a', DK.b)",
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
        "X": 100.0,
        "Y": 100.0,
        "Zoom": 0.8
    }
}

target_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"
with open(target_file, "w", encoding="utf-8") as f:
    json.dump(dyn_data, f, indent=2, ensure_ascii=False)

print(f"SUCCESS: Generated visual Dynamo graph with {len(nodes)} nodes, {len(connectors)} wires, and {len(annotations)} color-coded groups!")
