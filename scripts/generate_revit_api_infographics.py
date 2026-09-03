#!/usr/bin/env python3
"""Generate Vietnamese Revit API study notes and deterministic infographic PNGs.

The source PDF is a Revit 2014 API guide. This script summarizes and
restructures the material into learning artifacts instead of copying the book.
"""

from __future__ import annotations

import re
import subprocess
from pathlib import Path
from textwrap import dedent

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = Path("/Users/tonyhoang/Desktop/Revit 2014 Platform API Developers Guidlines.pdf")
OUT_DIR = ROOT / "output" / "revit-api-infographics"
NOTES_DIR = OUT_DIR / "02-topic-notes"
PROMPTS_DIR = OUT_DIR / "03-infographic-prompts"
PNG_DIR = OUT_DIR / "04-infographics-png"
SOURCE_DIR = OUT_DIR / "01-source-map"

FONT_REGULAR = Path("/System/Library/Fonts/Supplemental/Arial.ttf")
FONT_BOLD = Path("/System/Library/Fonts/Supplemental/Arial Bold.ttf")
FONT_MONO = Path("/System/Library/Fonts/SFNSMono.ttf")


TOPICS = [
    {
        "id": "01-revit-api-platform-overview",
        "title": "Tổng Quan Revit API Platform",
        "priority": "P0",
        "patterns": ["Welcome to the Revit Platform API", "What Can you do with the Revit Platform API", "Requirements"],
        "summary": "Revit API là lớp .NET cho phép add-in đọc dữ liệu BIM, tạo/chỉnh sửa element, mở rộng UI và tự động hóa workflow trong Revit.",
        "concepts": [
            "API chạy trong tiến trình Revit, chịu ràng buộc bởi document đang mở và trạng thái UI.",
            "Dữ liệu mô hình chủ yếu là Element, ElementType, Category, Parameter và Geometry.",
            "Add-in thường là DLL .NET được đăng ký bằng manifest `.addin`.",
            "Revit API 2014 là nền tảng khái niệm; khi làm Revit 2025-2027 cần kiểm tra breaking changes.",
        ],
        "apis": ["Autodesk.Revit.DB", "Autodesk.Revit.UI", "IExternalCommand", "IExternalApplication", "Document", "Element"],
        "workflow": ["Hiểu mô hình dữ liệu BIM", "Tạo class entry point", "Đăng ký add-in", "Mở Revit để debug", "Đọc/sửa model trong Transaction"],
        "pitfalls": ["Không gọi API ngoài context hợp lệ của Revit", "Không sửa model nếu thiếu Transaction", "Không giả định API 2014 còn y nguyên ở bản mới"],
        "example": "Dùng `IExternalCommand` để quét tất cả Wall trong `Document`, đọc parameter và xuất báo cáo khối lượng.",
    },
    {
        "id": "02-environment-sdk-addin",
        "title": "Cài Môi Trường, SDK, Reference DLL, .addin",
        "priority": "P0",
        "patterns": ["Create a New Project", "Add References", "Create a .addin manifest file", "Copy Local"],
        "summary": "Một add-in Revit cần project Class Library, reference đúng `RevitAPI.dll`/`RevitAPIUI.dll`, manifest `.addin` và cấu hình debug mở `Revit.exe`.",
        "concepts": [
            "Reference DLL lấy từ thư mục cài Revit tương ứng version target.",
            "Đặt `Copy Local = false` để tránh debugger dùng sai bản DLL.",
            "Manifest `.addin` khai báo Assembly, AddInId, FullClassName, Text, VendorId.",
            "Đường dẫn manifest thường nằm trong ProgramData Autodesk Revit Addins theo version.",
        ],
        "apis": ["RevitAPI.dll", "RevitAPIUI.dll", ".addin", "AddInId", "FullClassName", "VendorId"],
        "workflow": ["Tạo Class Library", "Add reference DLL", "Viết entry class", "Build DLL", "Tạo `.addin`", "F5/debug bằng Revit.exe"],
        "pitfalls": ["Sai version DLL", "Copy Local bật", "FullClassName không khớp namespace", "Manifest đặt sai thư mục"],
        "example": "Project `HelloWorld.dll` có `HelloWorld.Class1`; manifest phải trỏ đúng `<Assembly>` và `<FullClassName>`.",
    },
    {
        "id": "03-hello-world-external-command",
        "title": "Hello World ExternalCommand",
        "priority": "P0",
        "patterns": ["Walkthrough: Hello World", "IExternalCommand", "Execute()", "TaskDialog.Show"],
        "summary": "`IExternalCommand` là entry point đơn giản nhất: user bấm lệnh, Revit gọi `Execute`, command trả về `Succeeded`, `Failed` hoặc `Cancelled`.",
        "concepts": [
            "`Execute` nhận `ExternalCommandData`, `message` và `ElementSet` lỗi.",
            "Command có thể đọc `UIApplication`, `UIDocument`, `Document` từ `commandData`.",
            "Thuộc tính `Transaction` quyết định command có được sửa model hay không.",
            "`TaskDialog` phù hợp để hiển thị thông báo Revit-style.",
        ],
        "apis": ["IExternalCommand", "Execute", "ExternalCommandData", "Result.Succeeded", "TaskDialog", "TransactionAttribute"],
        "workflow": ["Tạo class implement interface", "Thêm `[Transaction]`", "Lấy context", "Chạy logic", "Return Result"],
        "pitfalls": ["Quên namespace Autodesk.Revit.UI/DB", "Dùng TransactionMode sai", "Không set message khi fail"],
        "example": "`TaskDialog.Show(\"Revit\", \"Hello World\")` để kiểm tra add-in đã load và command chạy được.",
    },
    {
        "id": "04-command-application-dbapplication",
        "title": "ExternalCommand vs ExternalApplication vs DB ExternalApplication",
        "priority": "P0",
        "patterns": ["External Commands", "External Application", "DB-level External Applications", "IExternalDBApplication"],
        "summary": "Ba loại entry point giải quyết ba nhu cầu: lệnh một lần, bootstrap UI/event khi Revit khởi động, hoặc xử lý database không UI.",
        "concepts": [
            "`ExternalCommand`: chạy khi user gọi lệnh, phù hợp task cụ thể.",
            "`ExternalApplication`: `OnStartup`/`OnShutdown`, tạo ribbon, đăng ký event UI.",
            "`ExternalDBApplication`: không có UI, dùng cho database-level event hoặc automation nền.",
            "Ribbon button thường do Application tạo và trỏ tới Command.",
        ],
        "apis": ["IExternalCommand", "IExternalApplication", "IExternalDBApplication", "OnStartup", "OnShutdown", "UIControlledApplication"],
        "workflow": ["Chọn loại add-in", "Khai báo đúng Type trong manifest", "Tạo UI nếu cần", "Đăng ký/dọn event đúng vòng đời"],
        "pitfalls": ["Tạo UI trong DBApplication", "Đăng ký event nhưng không unregister", "Dồn business logic vào OnStartup"],
        "example": "Application tạo panel 'QA Tools'; PushButton gọi `CheckWarningsCommand` khi user cần kiểm tra model.",
    },
    {
        "id": "05-manifest-loading-debugging",
        "title": "Add-in Manifest, Loading, Debugging",
        "priority": "P0",
        "patterns": ["Add-In Registration", "Debug the Program", "Start external program", "Revit.exe"],
        "summary": "Manifest là hợp đồng để Revit tìm DLL và class entry point; debug hiệu quả cần build đúng path, mở đúng Revit version và đặt breakpoint.",
        "concepts": [
            "Manifest XML có thể đăng ký Command, Application hoặc DBApplication.",
            "`AddInId` nên là GUID ổn định cho add-in.",
            "Debug bằng cách set Start external program tới `Revit.exe`.",
            "Khi DLL nằm ở network share, cấu hình bảo mật có thể cần xử lý riêng.",
        ],
        "apis": ["RevitAddIns", "AddIn Type", "Assembly", "AddInId", "FullClassName", "VendorDescription"],
        "workflow": ["Build", "Copy DLL", "Copy `.addin`", "Launch Revit", "Mở Add-Ins", "Bấm command", "Breakpoint hit"],
        "pitfalls": ["Manifest cache/path cũ", "Sai platform target", "DLL dependency thiếu", "Không restart Revit sau khi build"],
        "example": "Nếu button không xuất hiện, kiểm tra XML hợp lệ, path DLL tồn tại và class name có namespace đầy đủ.",
    },
    {
        "id": "06-ribbon-panels-controls",
        "title": "Ribbon, Panel, PushButton, SplitButton, StackedButton",
        "priority": "P0",
        "patterns": ["Ribbon Panels and Controls", "CreateRibbonPanel", "PushButtonData", "SplitButtonData", "AddStackedItems"],
        "summary": "Ribbon API giúp đưa workflow add-in vào UI Revit bằng tab, panel, button, split button, radio group và contextual help.",
        "concepts": [
            "Ribbon thường được tạo trong `OnStartup` của `IExternalApplication`.",
            "Mỗi control có lớp Data để tạo và lớp Item sau khi add vào panel.",
            "Button trỏ tới assembly và class `IExternalCommand`.",
            "Availability, tooltip, large image và contextual help cải thiện UX.",
        ],
        "apis": ["CreateRibbonTab", "CreateRibbonPanel", "PushButtonData", "SplitButtonData", "RibbonItem", "ContextualHelp"],
        "workflow": ["Create tab", "Create panel", "Create button data", "Add item", "Set icon/tooltip/help", "Bind command"],
        "pitfalls": ["Tên tab trùng", "Icon resource sai path", "Button quá nhiều gây rối ribbon", "Thiếu availability cho command theo context"],
        "example": "Panel 'Rebar Tools' có PushButton 'Create Stirrup' gọi command tạo thép đai cho column đang chọn.",
    },
    {
        "id": "07-application-document-uidocument",
        "title": "Application, UIApplication, Document, UIDocument",
        "priority": "P0",
        "patterns": ["Application and Document", "Application Functions", "Document Functions", "UIDocument"],
        "summary": "Application đại diện môi trường Revit; Document là model file; UIDocument là cầu nối thao tác UI như selection và active view.",
        "concepts": [
            "`Application`/`ControlledApplication` chứa thông tin cấp ứng dụng và events.",
            "`Document` chứa elements, settings, units, file operations và tạo/sửa dữ liệu.",
            "`UIDocument` dùng cho selection, active view và tương tác người dùng.",
            "Phân biệt DB API và UI API giúp code rõ trách nhiệm.",
        ],
        "apis": ["Application", "UIApplication", "ControlledApplication", "Document", "UIDocument", "ActiveUIDocument"],
        "workflow": ["Lấy UIApplication", "Lấy UIDocument", "Lấy Document", "Đọc settings/units", "Thao tác element", "Cập nhật UI khi cần"],
        "pitfalls": ["Dùng UIDocument trong DB-only context", "Giữ reference Document quá lâu", "Không kiểm tra ActiveUIDocument null"],
        "example": "`commandData.Application.ActiveUIDocument.Document` là đường lấy `Document` phổ biến trong command.",
    },
    {
        "id": "08-element-essentials",
        "title": "Element Essentials: Element, ElementId, Category, Type/Instance",
        "priority": "P0",
        "patterns": ["Element Essentials", "Element Classification", "Element Retrieval", "General Properties"],
        "summary": "Element là đơn vị dữ liệu cốt lõi trong Revit; muốn làm API vững phải phân biệt instance/type, category, id, unique id, level, location và parameters.",
        "concepts": [
            "Instance là phần tử đặt trong model; Type định nghĩa kiểu dùng chung.",
            "`ElementId` ổn trong session/model, `UniqueId` hữu ích khi lưu liên kết ngoài.",
            "Category giúp lọc và hiểu ý nghĩa BIM của element.",
            "Một số element có Location, một số chỉ có geometry hoặc data.",
        ],
        "apis": ["Element", "ElementId", "ElementType", "Category", "BuiltInCategory", "Location", "UniqueId"],
        "workflow": ["Lấy ElementId", "GetElement", "Kiểm tra Category/Class", "Đọc type", "Đọc location", "Đọc parameters"],
        "pitfalls": ["Nhầm Type với Instance", "Assume element nào cũng có Location", "Lưu ElementId ra ngoài lâu dài không kiểm chứng"],
        "example": "Từ Wall instance lấy `WallType` qua type id để đọc cấu tạo và thông số dùng chung.",
    },
    {
        "id": "09-parameters",
        "title": "Parameters: BuiltInParameter, StorageType, AsValueString, SetValueString",
        "priority": "P1",
        "patterns": ["Parameters", "Builtin Parameters", "Storage Types", "asValueString", "SetValueString"],
        "summary": "Parameter chứa phần lớn thông tin BIM; đọc/ghi đúng cần biết definition, storage type, unit conversion và binding.",
        "concepts": [
            "`BuiltInParameter` truy cập parameter chuẩn của Revit.",
            "`StorageType` quyết định dùng `AsString`, `AsDouble`, `AsInteger` hay `AsElementId`.",
            "`AsValueString`/`SetValueString` xử lý chuỗi theo unit hiển thị.",
            "Shared/project parameters cần binding vào category phù hợp.",
        ],
        "apis": ["Parameter", "Definition", "BuiltInParameter", "StorageType", "AsValueString", "SetValueString", "BindingMap"],
        "workflow": ["Tìm parameter", "Kiểm tra HasValue/ReadOnly", "Đọc theo StorageType", "Convert unit nếu cần", "Set trong Transaction"],
        "pitfalls": ["Set sai type", "Quên unit nội bộ", "Ghi parameter read-only", "LookupParameter phụ thuộc ngôn ngữ tên"],
        "example": "Đọc chiều dài wall bằng BuiltInParameter, convert từ internal feet sang đơn vị dự án trước khi xuất Excel.",
    },
    {
        "id": "10-selection",
        "title": "Selection: Current Selection, PickObject, PickObjects, PickPoint",
        "priority": "P0",
        "patterns": ["Selection", "Changing the Selection", "User Selection", "PickPoint"],
        "summary": "Selection API xử lý cả selection hiện tại và prompt người dùng chọn element, face, edge hoặc point trong giao diện Revit.",
        "concepts": [
            "`UIDocument.Selection` quản lý tập chọn hiện tại.",
            "`GetElementIds` phù hợp với API hiện đại hơn `Selection.Elements` cũ.",
            "`PickObject`/`PickObjects` trả về `Reference` để truy tới element hoặc geometry.",
            "`PickPoint` có thể dùng object snap để lấy tọa độ chính xác.",
        ],
        "apis": ["UIDocument.Selection", "Selection.GetElementIds", "PickObject", "PickObjects", "PickPoint", "ObjectType"],
        "workflow": ["Lấy selection hiện tại", "Nếu rỗng thì prompt user", "Chuyển Reference thành Element", "Validate category", "Chạy logic"],
        "pitfalls": ["Không xử lý user cancel", "Không lọc category", "Dùng selection UI trong context không UI", "Reference face khác ElementId"],
        "example": "Prompt user chọn một cột bê tông, sau đó lấy `FamilyInstance` để tạo rebar theo bounding box.",
    },
    {
        "id": "11-selection-filter",
        "title": "Selection Filter: ISelectionFilter",
        "priority": "P0",
        "patterns": ["Filtered User Selection", "ISelectionFilter", "AllowElement", "AllowReference"],
        "summary": "`ISelectionFilter` giới hạn thứ user có thể chọn, giúp workflow chính xác hơn và giảm lỗi input.",
        "concepts": [
            "`AllowElement` lọc theo element/category/class.",
            "`AllowReference` lọc geometry reference như planar face.",
            "Filter nên trả false nhanh cho category không hợp lệ.",
            "Dùng message prompt rõ để user biết cần chọn gì.",
        ],
        "apis": ["ISelectionFilter", "AllowElement", "AllowReference", "ObjectType.Face", "Reference", "PlanarFace"],
        "workflow": ["Tạo class filter", "Implement AllowElement", "Implement AllowReference nếu chọn geometry", "Truyền vào PickObject/PickObjects", "Xử lý cancel"],
        "pitfalls": ["Throw exception trong filter", "Không kiểm tra null Category", "Lọc face nhưng không verify PlanarFace"],
        "example": "Filter chỉ cho chọn `PlanarFace` của Wall/Floor để tạo AreaReinforcement đúng host face.",
    },
    {
        "id": "12-filtered-element-collector-foundation",
        "title": "FilteredElementCollector Foundation",
        "priority": "P0",
        "patterns": ["Create a FilteredElementCollector", "FilteredElementCollector collector", "Getting filtered elements"],
        "summary": "`FilteredElementCollector` là công cụ chính để lấy element từ Document hoặc từ một View, kết hợp class/category/filter để có tập dữ liệu cần xử lý.",
        "concepts": [
            "Collector có thể quét toàn document hoặc chỉ element visible trong view.",
            "Nên dùng filter native trước rồi mới LINQ để tối ưu.",
            "Có thể lấy `Element`, `ElementId`, hoặc element đầu tiên.",
            "Collector hỗ trợ chaining fluent như `OfClass`, `OfCategory`, `WhereElementIsNotElementType`.",
        ],
        "apis": ["FilteredElementCollector", "OfClass", "OfCategory", "WherePasses", "ToElements", "ToElementIds"],
        "workflow": ["Tạo collector", "Giới hạn scope", "Apply native filters", "Loại type/instance", "Materialize kết quả", "Iterate"],
        "pitfalls": ["Quét cả document quá rộng", "LINQ trước filter native", "Nhầm element type với instance"],
        "example": "`new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType()` để lấy wall instances.",
    },
    {
        "id": "13-advanced-filtering",
        "title": "Filtering Nâng Cao: Class, Category, Rule, LINQ, Bounding Box, Intersection",
        "priority": "P0",
        "patterns": ["Applying Filters", "LINQ Queries", "Bounding Box filters", "Element Intersection Filters"],
        "summary": "Advanced filtering kết hợp quick filters, slow filters, parameter rules, bounding box và intersection để tìm đúng phần tử mà vẫn giữ hiệu năng.",
        "concepts": [
            "Quick filters giảm candidate trước khi load element đầy đủ.",
            "Parameter filters dùng rule để lọc theo giá trị BIM.",
            "Bounding box filter phù hợp tìm element trong vùng không gian.",
            "Intersection filter cần solid geometry và có giới hạn với element không có solid như rebar.",
        ],
        "apis": ["ElementClassFilter", "ElementCategoryFilter", "ElementParameterFilter", "BoundingBoxIntersectsFilter", "ElementIntersectsElementFilter", "LogicalAndFilter"],
        "workflow": ["Chọn scope", "Apply quick filters", "Apply rule/intersection khi cần", "LINQ post-process", "Kiểm tra performance"],
        "pitfalls": ["Intersection không bắt element không có solid", "BoundingBox không thay thế geometry chính xác", "Filter rule sai unit"],
        "example": "Tìm door giao với wall: lọc door instances, dùng intersection/bounding box để phát hiện xung đột sơ bộ.",
    },
    {
        "id": "14-transactions",
        "title": "Transactions: Transaction, SubTransaction, TransactionGroup",
        "priority": "P0",
        "patterns": ["Transactions", "Transaction Classes", "SubTransactions", "Failure Handling Options"],
        "summary": "Mọi thay đổi model cần Transaction hợp lệ; TransactionGroup gom nhiều thao tác, SubTransaction chia nhỏ rollback trong một transaction lớn.",
        "concepts": [
            "`TransactionMode.Manual` cho phép code tự kiểm soát transaction.",
            "`Transaction` phải Start rồi Commit hoặc RollBack.",
            "`SubTransaction` hữu ích khi thử từng thao tác trong transaction đang mở.",
            "Failure handling quyết định cách xử lý warning/error khi commit.",
        ],
        "apis": ["Transaction", "SubTransaction", "TransactionGroup", "TransactionMode.Manual", "FailureHandlingOptions", "Commit"],
        "workflow": ["Validate input", "Start transaction", "Create/edit elements", "Handle failures", "Commit/RollBack", "Report result"],
        "pitfalls": ["ModificationOutsideTransactionException", "Nested transaction sai", "Không rollback khi lỗi", "Commit warning không kiểm soát"],
        "example": "Tạo rebar cho nhiều cột trong một TransactionGroup; rollback từng cột lỗi bằng SubTransaction.",
    },
    {
        "id": "15-editing-elements",
        "title": "Editing Elements: Move, Copy, Rotate, Mirror, Array, Delete, Pinned",
        "priority": "P1",
        "patterns": ["Editing Elements", "Moving Elements", "Copying Elements", "Rotating Elements", "Pinned Elements"],
        "summary": "Editing API thao tác element bằng transform và document operations; cần kiểm tra pinned, constraints, groups và transaction.",
        "concepts": [
            "`ElementTransformUtils` xử lý move/copy/rotate/mirror cho nhiều element.",
            "Delete trả về tập ElementId bị xóa, có thể nhiều hơn input do dependency.",
            "Pinned/constraints/group membership có thể chặn chỉnh sửa.",
            "Array/group mirror tạo element mới nên cần quản lý id kết quả.",
        ],
        "apis": ["ElementTransformUtils", "MoveElement", "CopyElement", "RotateElement", "MirrorElement", "Document.Delete", "Pinned"],
        "workflow": ["Collect ids", "Check pinned/group", "Start transaction", "Apply transform", "Capture new/deleted ids", "Commit"],
        "pitfalls": ["Xóa dependency ngoài ý muốn", "Mirror đổi handedness family", "Move element pinned", "Group member không sửa trực tiếp"],
        "example": "Copy một dãy family instance theo vector tầng, sau đó gán lại parameter Mark cho từng instance mới.",
    },
    {
        "id": "16-views",
        "title": "Views: View3D, ViewPlan, ViewSheet, ViewSchedule, Crop, UIView",
        "priority": "P1",
        "patterns": ["Views", "View3D", "ViewPlan", "ViewSchedule", "UIView"],
        "summary": "View API kiểm soát cách nhìn và trình bày model: tạo 3D/plan/sheet/schedule, crop, filter và thao tác với view đang mở.",
        "concepts": [
            "Collector theo view chỉ lấy element visible trong view đó.",
            "ViewSchedule có TableData và field/sort/filter riêng.",
            "Crop box/section box hỗ trợ focus vùng mô hình.",
            "`UIView` thuộc UI API, dùng cho zoom/center và view window.",
        ],
        "apis": ["View", "View3D", "ViewPlan", "ViewSheet", "ViewSchedule", "TableData", "UIView"],
        "workflow": ["Chọn loại view", "Tạo/cấu hình view", "Apply filters/crop", "Đặt lên sheet nếu cần", "Dùng collector theo view"],
        "pitfalls": ["View template khóa setting", "Collector theo view bỏ element invisible", "Schedule API khác model element API"],
        "example": "Tạo 3D view section box quanh selection để user kiểm tra va chạm MEP trong một vùng.",
    },
    {
        "id": "17-family-instance-symbol",
        "title": "Family Instance và Family Symbol",
        "priority": "P1",
        "patterns": ["Family Instances", "FamilySymbol", "Family Documents", "Family Instances"],
        "summary": "FamilySymbol là type có thể đặt; FamilyInstance là object đã đặt trong model với host, location, level và parameters riêng.",
        "concepts": [
            "Symbol cần active trước khi tạo instance ở nhiều version API.",
            "FamilyInstance có thể host-based, level-based hoặc face-based.",
            "Family document khác project document, có API tạo form/annotation/parameter riêng.",
            "Type parameter nằm ở symbol/type, instance parameter nằm ở instance.",
        ],
        "apis": ["Family", "FamilySymbol", "FamilyInstance", "NewFamilyInstance", "FamilyDocument", "LoadFamily"],
        "workflow": ["Load/tìm family", "Chọn symbol", "Activate nếu cần", "Start transaction", "Place instance", "Set parameters"],
        "pitfalls": ["Symbol chưa active", "Sai overload placement", "Nhầm instance/type parameter", "Host không hợp lệ"],
        "example": "Đặt hanger family lên duct bằng face reference, sau đó set spacing/diameter parameter cho từng instance.",
    },
    {
        "id": "18-geometry-overview",
        "title": "Geometry Overview: Options, GeometryElement, GeometryObject",
        "priority": "P1",
        "patterns": ["Geometry", "Retrieve Geometry Data from a Wall", "Geometry Object Class", "GeometryElement"],
        "summary": "Geometry API đọc hình học hiển thị/physical của element qua `Options`, trả về `GeometryElement` chứa solid, curve, mesh, point hoặc instance geometry.",
        "concepts": [
            "`Options` quyết định detail level, references và view context.",
            "Geometry có thể khác nhau theo view, detail level và family instance transform.",
            "`GeometryInstance` cần transform để đưa symbol geometry về model coordinates.",
            "Không phải element nào cũng có solid geometry.",
        ],
        "apis": ["Options", "Element.get_Geometry", "GeometryElement", "GeometryObject", "GeometryInstance", "Transform"],
        "workflow": ["Tạo Options", "Get geometry", "Iterate objects", "Handle GeometryInstance", "Extract solid/curve", "Transform coordinates"],
        "pitfalls": ["Quên IncludeNonVisibleObjects/ComputeReferences khi cần", "Dùng geometry null", "Bỏ qua transform instance"],
        "example": "Đọc geometry wall để lấy faces, edges và tính diện tích bề mặt phục vụ bóc tách vật liệu.",
    },
    {
        "id": "19-geometry-details",
        "title": "Geometry Chi Tiết: Curve, Solid, Face, Edge, Mesh, GeometryInstance",
        "priority": "P1",
        "patterns": ["Curves", "Solids, Faces and Edges", "Meshes", "GeometryInstances", "Face analysis"],
        "summary": "Làm geometry hiệu quả cần hiểu từng object: curve có parameterization, solid có face/edge, face có UV, mesh dùng cho biểu diễn tam giác.",
        "concepts": [
            "Curve có Line, Arc, Ellipse, HermiteSpline và các phép evaluate/project.",
            "Solid gồm Faces và Edges; volume/area dùng cho phân tích.",
            "Face dùng UV parameter, normal và triangulation.",
            "Mesh hữu ích để visualize hoặc phân tích hình học phức tạp nhẹ.",
        ],
        "apis": ["Curve", "Line", "Arc", "Solid", "Face", "PlanarFace", "Edge", "Mesh"],
        "workflow": ["Phân loại GeometryObject", "Lọc Solid volume > 0", "Iterate faces", "Đọc edges/loops", "Triangulate nếu cần", "Tính toán"],
        "pitfalls": ["So sánh double tuyệt đối", "Assume face phẳng", "Không xử lý nested instance", "Dùng mesh thay solid cho tính chính xác cao"],
        "example": "Tìm planar face lớn nhất của floor để đặt area reinforcement hoặc annotation theo mặt.",
    },
    {
        "id": "20-geometry-use-cases",
        "title": "Geometry Use Cases: Ray Projection, Intersection, Room/Space Geometry",
        "priority": "P1",
        "patterns": ["Finding geometry by ray projection", "Room and Space Geometry", "Extrusion Analysis", "Find Nearby Walls"],
        "summary": "Các use case geometry thường gặp gồm bắn tia tìm element, kiểm tra giao cắt, phân tích extrusion và lấy boundary/solid của room-space.",
        "concepts": [
            "Ray projection hỗ trợ tìm đối tượng theo hướng nhìn/đường bắn.",
            "Intersection có thể dùng filter hoặc boolean/solid tùy độ chính xác.",
            "Room/Space geometry cần boundary options và phase/view phù hợp.",
            "Extrusion analysis giúp nhận diện profile và hướng đùn của solid.",
        ],
        "apis": ["ReferenceIntersector", "ElementIntersectsSolidFilter", "BooleanOperationsUtils", "SpatialElementGeometryCalculator", "Room", "Space"],
        "workflow": ["Xác định câu hỏi hình học", "Chọn ray/filter/solid calculator", "Giới hạn scope", "Tính kết quả", "Highlight/report"],
        "pitfalls": ["Thiếu 3D view cho ray", "Room boundary không kín", "Element không có solid", "Hiệu năng kém khi boolean quá nhiều"],
        "example": "Từ một điểm treo thiết bị MEP, bắn ray lên trên để tìm slab gần nhất và tính chiều dài ty treo.",
    },
    {
        "id": "21-walls-floors-roofs-openings",
        "title": "Walls, Floors, Roofs, Openings và Compound Structure",
        "priority": "P1",
        "patterns": ["Walls, Floors, Ceilings, Roofs and Openings", "Compound Structure", "Opening", "Walls"],
        "summary": "Các building elements có type, host, layer/compound structure, opening và material layer; API cho phép tạo, đọc cấu tạo và phân tích thông tin xây dựng.",
        "concepts": [
            "Wall/Floor/Roof là system family với creation API và type riêng.",
            "CompoundStructure mô tả layer vật liệu, độ dày, function.",
            "Opening có thể gắn host và ảnh hưởng geometry/khối lượng.",
            "Thermal/material properties phục vụ phân tích năng lượng và quantity.",
        ],
        "apis": ["Wall", "Floor", "RoofBase", "Opening", "CompoundStructure", "HostObject", "WallType"],
        "workflow": ["Lọc host objects", "Đọc type", "Đọc compound layers", "Tính geometry/material", "Tạo opening nếu cần"],
        "pitfalls": ["Layer index sai", "Opening làm thay đổi geometry", "HostObject không phải mọi category", "Unit thickness nội bộ"],
        "example": "Bóc tách diện tích từng lớp wall bằng compound structure và material ids.",
    },
    {
        "id": "22-materials",
        "title": "Materials: Data, Quantities, Paint Face",
        "priority": "P1",
        "patterns": ["Material", "Material Management", "Element Material", "Material quantities", "Painting the Face"],
        "summary": "Material là element chứa thông tin vật liệu, appearance/physical data và liên kết với element hoặc face để bóc tách khối lượng.",
        "concepts": [
            "Element có thể có material từ type, layer hoặc face paint.",
            "Material quantity có thể lấy theo area/volume tùy element hỗ trợ.",
            "Paint face gán material ở cấp face, khác material cấu tạo.",
            "Material properties dùng internal units và schema theo version.",
        ],
        "apis": ["Material", "GetMaterialIds", "GetMaterialArea", "GetMaterialVolume", "Document.Paint", "Document.IsPainted"],
        "workflow": ["Lấy material ids", "Đọc name/properties", "Tính area/volume", "Kiểm tra painted faces", "Xuất schedule/report"],
        "pitfalls": ["Nhầm painted material với layer material", "Element không hỗ trợ quantity", "Đơn vị nội bộ", "Material asset API thay đổi theo version"],
        "example": "Tính diện tích sơn từng phòng bằng painted faces và xuất bảng kiểm tra vật liệu hoàn thiện.",
    },
    {
        "id": "23-revit-structure-analytical",
        "title": "Revit Structure: Structural Elements và Analytical Model",
        "priority": "P1",
        "patterns": ["Revit Structure", "Structural Model Elements", "Analytical Model", "Loads"],
        "summary": "Structure API bổ sung structural element, analytical model, load, boundary condition và liên kết phân tích cho workflow kết cấu.",
        "concepts": [
            "Structural elements gồm beam, column, brace, foundation, rebar và reinforcement.",
            "Analytical model có thể khác physical model và phục vụ phân tích kết cấu.",
            "Load/Boundary/Analytical links cần kiểm tra product/version support.",
            "Host và geometry ảnh hưởng tạo reinforcement.",
        ],
        "apis": ["FamilyInstance", "StructuralType", "AnalyticalModel", "Load", "BoundaryConditions", "RebarHostData"],
        "workflow": ["Lọc structural category", "Đọc structural type", "Kiểm tra analytical model", "Đọc/tạo loads", "Liên kết reinforcement"],
        "pitfalls": ["Analytical model disabled", "StructuralType sai", "Product-specific API", "Host không hỗ trợ reinforcement"],
        "example": "Quét cột/dầm structural, kiểm tra analytical model missing và tạo report QA cho kỹ sư kết cấu.",
    },
    {
        "id": "24-rebar-reinforcement",
        "title": "Rebar/Reinforcement: Rebar, Area, Path, Cover, Host",
        "priority": "P1",
        "patterns": ["Rebar", "AreaReinforcement", "PathReinforcement", "RebarHostData", "RebarCoverType"],
        "summary": "Rebar API tạo thanh thép từ curves hoặc shape, quản lý layout, hook, cover và host; Area/Path reinforcement dùng cho sàn/tường theo mặt phẳng.",
        "concepts": [
            "`Rebar.CreateFromCurves` tạo thép từ các curve cùng mặt phẳng.",
            "`CreateFromRebarShape` dùng shape có sẵn và scale/location.",
            "`AreaReinforcement`/`PathReinforcement` cần host planar face phù hợp.",
            "`RebarHostData` và `RebarCoverType` quản lý host/các lớp cover.",
        ],
        "apis": ["Rebar", "Rebar.CreateFromCurves", "RebarBarType", "RebarHookType", "RebarShape", "AreaReinforcement", "PathReinforcement", "RebarHostData"],
        "workflow": ["Chọn host", "Lấy planar face/cover", "Chọn bar type/hook/shape", "Tạo curves", "Create rebar", "Set layout rule"],
        "pitfalls": ["Curves không đồng phẳng", "Host không hợp lệ", "Hook orientation sai", "ElementIntersects filter không bắt rebar vì không có solid"],
        "example": "Tạo thép đai cột: lấy bounding box column, tạo loop curves, gọi `Rebar.CreateFromCurves`, set spacing layout.",
    },
    {
        "id": "25-shared-parameters-extensible-storage",
        "title": "Shared Parameters và Extensible Storage",
        "priority": "P2",
        "patterns": ["Shared Parameters", "Definition File", "Binding", "Extensible Storage"],
        "summary": "Shared Parameters dùng cho dữ liệu BIM hiển thị/schedule; Extensible Storage dùng lưu schema custom ẩn/riêng cho add-in.",
        "concepts": [
            "Shared parameter cần definition file, group, external definition và binding.",
            "Instance/type binding quyết định dữ liệu nằm ở instance hay type.",
            "Extensible Storage lưu entity theo schema GUID trên element.",
            "Dữ liệu public cho người dùng nên là parameter; dữ liệu nội bộ nên cân nhắc storage.",
        ],
        "apis": ["DefinitionFile", "ExternalDefinitionCreationOptions", "BindingMap", "InstanceBinding", "TypeBinding", "Schema", "Entity"],
        "workflow": ["Tạo/đọc definition file", "Tạo definition", "Chọn categories", "Bind parameter", "Set parameter hoặc Entity"],
        "pitfalls": ["GUID/name trùng", "Binding sai category", "Lạm dụng hidden storage", "Không version schema"],
        "example": "Lưu mã QA public bằng shared parameter; lưu metadata scan nội bộ bằng Extensible Storage schema versioned.",
    },
    {
        "id": "26-events-dmu-failure",
        "title": "Events, Dynamic Model Update, Failure Posting",
        "priority": "P2",
        "patterns": ["Events", "Dynamic Model Update", "Failure Posting and Handling", "DocumentChanged event"],
        "summary": "Events và DMU cho phép add-in phản ứng khi document/UI thay đổi; failure API giúp post và xử lý cảnh báo/lỗi theo chuẩn Revit.",
        "concepts": [
            "Database events như DocumentChanged dùng để quan sát thay đổi.",
            "UI events xử lý lifecycle và tương tác người dùng.",
            "Dynamic Model Update dùng Updater để phản ứng khi element thay đổi.",
            "Failure handling cần cẩn trọng để không che lỗi quan trọng.",
        ],
        "apis": ["DocumentChanged", "Idling", "IUpdater", "UpdaterRegistry", "FailureMessage", "IFailuresPreprocessor"],
        "workflow": ["Đăng ký event/updater", "Lọc trigger", "Giữ handler nhẹ", "Thực hiện thay đổi hợp lệ", "Unregister khi shutdown"],
        "pitfalls": ["Event handler quá nặng", "Vòng lặp update", "Sửa model trong event không cho phép", "Nuốt failure nguy hiểm"],
        "example": "Updater tự gắn parameter kiểm tra khi wall mới được tạo, nhưng chỉ chạy trên category và change type cần thiết.",
    },
    {
        "id": "27-mep-overview",
        "title": "Revit MEP: Pipes, Ducts, Connectors, Systems",
        "priority": "P2",
        "patterns": ["Revit MEP", "MEP Element Creation", "Connectors", "Routing Preferences", "Mechanical Settings", "Electrical Settings"],
        "summary": "MEP API tập trung vào tạo pipe/duct, placeholder, system, connector, family connector và routing preference.",
        "concepts": [
            "Connector là điểm kết nối logic/geometry giữa MEP elements.",
            "System gom các element theo luồng HVAC, piping hoặc electrical.",
            "Routing preferences quyết định fitting/type khi route.",
            "Settings mechanical/electrical ảnh hưởng tạo và validate MEP elements.",
        ],
        "apis": ["Pipe", "Duct", "Connector", "ConnectorManager", "MEPSystem", "RoutingPreferenceManager", "MechanicalSettings", "ElectricalSettings"],
        "workflow": ["Chọn type/system/level", "Tạo pipe/duct", "Lấy connectors", "Connect elements", "Validate route/system", "Report clashes"],
        "pitfalls": ["Connector domain mismatch", "Level/system type thiếu", "Routing preference không đủ fitting", "Slope/offset sai unit"],
        "example": "Tạo đoạn pipe nối hai thiết bị bằng connector gần nhất, sau đó kiểm tra system và đường kính.",
    },
    {
        "id": "28-learning-roadmap",
        "title": "Roadmap Học Revit API Từ Beginner Đến Production Add-in",
        "priority": "P0",
        "patterns": ["Getting Started", "Basic Topics", "Advanced Topics", "Product Specific"],
        "summary": "Roadmap học hiệu quả bắt đầu từ add-in lifecycle, document/element/parameter, sau đó selection/filtering/transaction, rồi geometry và domain-specific APIs.",
        "concepts": [
            "Beginner: Hello World, manifest, command, document, element.",
            "Core: selection, filtering, parameter, transaction, editing.",
            "Modeling: family, views, geometry, materials.",
            "Production: events, failure handling, tests, packaging, version compatibility.",
        ],
        "apis": ["IExternalCommand", "FilteredElementCollector", "Transaction", "Parameter", "GeometryElement", "Rebar", "IUpdater"],
        "workflow": ["Tuần 1 basics", "Tuần 2 element data", "Tuần 3 editing/transactions", "Tuần 4 geometry", "Tuần 5 domain APIs", "Tuần 6 production"],
        "pitfalls": ["Học geometry quá sớm", "Bỏ qua transaction", "Không build sample nhỏ", "Không kiểm tra version API hiện đại"],
        "example": "Capstone: add-in chọn cột, tạo rebar, xuất report parameter, tạo 3D view kiểm tra và ghi log lỗi.",
    },
]


def run(cmd: list[str]) -> str:
    return subprocess.run(cmd, check=True, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout


def slug_title(topic: dict) -> str:
    return topic["id"]


def ensure_dirs() -> None:
    for path in [OUT_DIR, NOTES_DIR, PROMPTS_DIR, PNG_DIR, SOURCE_DIR]:
        path.mkdir(parents=True, exist_ok=True)


def load_pdf_pages() -> list[str]:
    text = run(["pdftotext", "-layout", str(PDF_PATH), "-"])
    return text.split("\f")


def detect_page_hits(pages: list[str], patterns: list[str]) -> list[int]:
    hits: list[int] = []
    lowered_patterns = [p.lower() for p in patterns]
    for i, page in enumerate(pages, start=1):
        low = page.lower()
        if any(pattern in low for pattern in lowered_patterns):
            hits.append(i)
    return hits


def compact_pages(pages: list[int], limit: int = 10) -> str:
    if not pages:
        return "không tự động phát hiện; xem mục lục/heading liên quan"
    if len(pages) <= limit:
        return ", ".join(str(p) for p in pages)
    return ", ".join(str(p) for p in pages[:limit]) + f", ... (+{len(pages) - limit} trang khác)"


def clean_md(text: str) -> str:
    """Remove code indentation introduced by nested Python formatting."""
    return dedent(text).replace("\n        ", "\n").lstrip()


def make_topic_note(topic: dict, pages: list[int], png_name: str) -> str:
    def bullets(items: list[str]) -> str:
        return "\n".join(f"- {item}" for item in items)

    return clean_md(
        f"""\
        # {topic["title"]}

        > Priority: **{topic["priority"]}**  
        > Source PDF: `Revit 2014 Platform API Developers Guidelines`  
        > Source page hits: {compact_pages(pages)}
        > Version note: Nội dung dựa trên Revit API 2014; khi áp dụng cho Revit 2025-2027 cần đối chiếu API docs hiện hành.

        ## Tóm tắt
        {topic["summary"]}

        ## Khái niệm chính
        {bullets(topic["concepts"])}

        ## Class/API cần nhớ
        {", ".join(f"`{api}`" for api in topic["apis"])}

        ## Workflow
        {bullets(topic["workflow"])}

        ## Lỗi thường gặp
        {bullets(topic["pitfalls"])}

        ## Ví dụ Revit API thực tế
        {topic["example"]}

        ## Infographic
        - PNG: `../04-infographics-png/{png_name}`
        - Prompt: `../03-infographic-prompts/{topic["id"]}.md`
        """
    )


def make_prompt(topic: dict, png_name: str) -> str:
    concept_text = "\n".join(f"- {item}" for item in topic["concepts"])
    api_text = ", ".join(topic["apis"])
    workflow_text = "\n".join(f"- {item}" for item in topic["workflow"])
    pitfall_text = "\n".join(f"- {item}" for item in topic["pitfalls"])
    return clean_md(
        f"""\
        # Prompt Infographic: {topic["title"]}

        ## Output đã render
        `../04-infographics-png/{png_name}`

        ## ImageGen Prompt Chi Tiết

        ```text
        Use case: infographic-diagram
        Asset type: vertical Vietnamese educational infographic poster for BIM developers learning Autodesk Revit API.

        Primary request:
        Create a polished vertical technical infographic titled "{topic["title"]}". The poster must explain the topic clearly for a developer who knows C# but is learning Revit API. The visual should feel like a premium developer documentation poster, not a marketing landing page.

        Audience:
        Vietnamese BIM developers, Revit API add-in developers, C# engineers working with Autodesk Revit.

        Subject:
        Revit API learning poster about "{topic["title"]}".

        Core message:
        {topic["summary"]}

        Required visible text, verbatim:
        "{topic["title"]}"
        "Nền tảng Revit API 2014"
        "Kiểm tra lại với Revit API version đang dùng"
        "Khái niệm chính"
        "API cần nhớ"
        "Workflow"
        "Lỗi thường gặp"
        "Ví dụ Revit API"

        Required concept bullets, keep short and readable:
        {concept_text}

        Required API/class/method names, keep spelling exactly:
        {api_text}

        Required workflow bullets:
        {workflow_text}

        Required common mistakes:
        {pitfall_text}

        Required example:
        {topic["example"]}

        Composition/framing:
        - Portrait poster, 9:16 or 2:3 aspect ratio.
        - Top header band with a large topic number, large title, and a small badge "Revit API 2014".
        - Main body uses 5 stacked sections/cards:
          1. Tóm tắt: one short paragraph.
          2. Khái niệm chính: 3-4 large bullets with simple icons.
          3. API cần nhớ: pill-shaped code chips for class/method names.
          4. Workflow: left-to-right or top-to-bottom numbered process.
          5. Lỗi thường gặp + Ví dụ: warning callouts and one practical Revit API example.
        - Add thin connector lines between sections to show learning flow.
        - Leave generous margins; no crowded layout; all text must fit inside its panel.

        Visual style:
        - Modern flat technical infographic, crisp vector-like raster artwork.
        - Clean Swiss/documentation typography.
        - Subtle BIM/Revit visual language: generic building model wireframe, C# brackets, database/document icon, ribbon/command icon when relevant.
        - Use generic icons only. Do not use official Autodesk/Revit/GitHub logos unless already allowed by the model; generic line icons are preferred.
        - No photorealistic people. No cartoon characters. No fake software screenshots.

        Typography:
        - Vietnamese-compatible sans-serif font similar to Be Vietnam Pro, Inter, Arial, or SF Pro.
        - Code/API chips in monospace style similar to JetBrains Mono.
        - Large readable text: title very large, section headers medium-large, body bullets short.
        - Preserve Vietnamese diacritics perfectly: ă, â, ê, ô, ơ, ư, đ, á, à, ả, ã, ạ, ấ, ầ, ẩ, ẫ, ậ.
        - Preserve API names exactly, including casing and punctuation.

        Color palette:
        - Background: deep charcoal/navy #0F172A with subtle grid lines.
        - Cards: white or near-white #F8FAFC.
        - Text: dark slate #111827 on cards, white on header.
        - Accents: cyan #38BDF8, green #22C55E, amber #F59E0B, red #EF4444, violet #A78BFA.
        - Avoid a one-color blue/purple gradient. Use balanced accent colors.

        Layout constraints:
        - No overlapping text.
        - No clipped title.
        - No tiny paragraphs.
        - No lorem ipsum.
        - No gibberish pseudo-code.
        - Do not invent unrelated API names.
        - Keep each text block concise; if too much text, summarize visually instead of shrinking font.
        - Every panel must have enough breathing room.

        Negative prompt / avoid:
        Avoid unreadable text, misspelled Vietnamese, broken diacritics, fake code, malformed API names, overstuffed poster, blurry typography, watermark, signature, random logos, decorative bokeh blobs, dark low-contrast text, overlapping cards, cropped footer, excessive 3D clutter.

        Quality target:
        High-resolution clean educational poster suitable for a Vietnamese Revit API training deck and printed handout.
        ```

        ## Ghi chú sử dụng
        Prompt này phù hợp để tạo biến thể bằng ImageGen. Nếu model ảnh render chữ chưa đạt, hãy giảm số bullet hoặc chỉ giữ title + API chips + diagram, rồi dùng bản PNG deterministic trong `04-infographics-png` cho nội dung chính xác.
        """
    )


def font(size: int, bold: bool = False, mono: bool = False) -> ImageFont.FreeTypeFont:
    path = FONT_MONO if mono else (FONT_BOLD if bold else FONT_REGULAR)
    return ImageFont.truetype(str(path), size=size)


def wrap_text(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = word if not current else f"{current} {word}"
        if draw.textbbox((0, 0), candidate, font=fnt)[2] <= max_width:
            current = candidate
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def draw_wrapped(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill: str,
    max_width: int,
    line_gap: int = 8,
    bullet: bool = False,
    max_lines: int | None = None,
) -> int:
    x, y = xy
    lines = wrap_text(draw, text, fnt, max_width - (26 if bullet else 0))
    if max_lines is not None and len(lines) > max_lines:
        lines = lines[:max_lines]
        lines[-1] = lines[-1].rstrip(".") + "..."
    for i, line in enumerate(lines):
        if bullet and i == 0:
            draw.ellipse((x, y + 12, x + 8, y + 20), fill="#22C55E")
            tx = x + 26
        elif bullet:
            tx = x + 26
        else:
            tx = x
        draw.text((tx, y), line, font=fnt, fill=fill)
        y += fnt.size + line_gap
    return y


def card(draw: ImageDraw.ImageDraw, x: int, y: int, w: int, h: int, title: str, accent: str) -> tuple[int, int, int]:
    draw.rounded_rectangle((x + 8, y + 10, x + w + 8, y + h + 10), radius=26, fill=(0, 0, 0, 35))
    draw.rounded_rectangle((x, y, x + w, y + h), radius=26, fill="#FFFFFF")
    draw.rounded_rectangle((x, y, x + 18, y + h), radius=18, fill=accent)
    draw.text((x + 42, y + 28), title, font=font(34, bold=True), fill="#0F172A")
    return x + 42, y + 82, w - 84


def draw_api_chips(draw: ImageDraw.ImageDraw, x: int, y: int, apis: list[str], max_width: int) -> int:
    f = font(25, mono=True)
    row_x = x
    for api in apis:
        label = api
        tw = draw.textbbox((0, 0), label, font=f)[2]
        chip_w = min(tw + 30, max_width)
        if row_x + chip_w > x + max_width:
            row_x = x
            y += 52
        draw.rounded_rectangle((row_x, y, row_x + chip_w, y + 38), radius=12, fill="#E0F2FE", outline="#38BDF8", width=2)
        draw.text((row_x + 14, y + 7), label, font=f, fill="#075985")
        row_x += chip_w + 12
    return y + 54


def render_png(topic: dict, index: int, pages: list[int]) -> str:
    width, height = 1400, 2100
    img = Image.new("RGB", (width, height), "#0F172A")
    draw = ImageDraw.Draw(img, "RGBA")

    for x in range(0, width, 70):
        draw.line((x, 0, x, height), fill=(56, 189, 248, 22), width=1)
    for y in range(0, height, 70):
        draw.line((0, y, width, y), fill=(56, 189, 248, 16), width=1)
    draw.ellipse((-260, -260, 720, 520), fill=(45, 127, 249, 48))
    draw.ellipse((820, 1550, 1700, 2400), fill=(34, 197, 94, 35))

    margin = 74
    draw.rounded_rectangle((margin, 54, width - margin, 224), radius=34, fill="#111827", outline="#38BDF8", width=2)
    draw.text((margin + 38, 82), f"{index:02d}", font=font(56, bold=True), fill="#22C55E")
    title_font = font(48, bold=True)
    title_lines = wrap_text(draw, topic["title"], title_font, 740)
    line_step = 54
    if len(title_lines) > 2:
        title_font = font(42, bold=True)
        title_lines = wrap_text(draw, topic["title"], title_font, 740)
        line_step = 46
    ty = 74
    for line in title_lines[:3]:
        draw.text((margin + 140, ty), line, font=title_font, fill="#F8FAFC")
        ty += line_step
    draw.rounded_rectangle((width - margin - 300, 82, width - margin - 34, 132), radius=18, fill="#F59E0B")
    draw.text((width - margin - 280, 92), "Revit API 2014 base", font=font(25, bold=True), fill="#111827")
    source_label = f"{topic['priority']} | source hits: {len(pages)}"
    draw.text((width - margin - 300, 148), source_label, font=font(24), fill="#CBD5E1")

    y = 260
    x = margin
    w = width - 2 * margin
    gap = 28
    sections = [
        ("Tóm tắt", "#38BDF8", [topic["summary"]], 220),
        ("Khái niệm chính", "#22C55E", topic["concepts"][:4], 360),
        ("API cần nhớ", "#A78BFA", [], 250),
        ("Workflow", "#F59E0B", topic["workflow"][:6], 310),
        ("Lỗi thường gặp + ví dụ", "#EF4444", topic["pitfalls"][:3] + [topic["example"]], 440),
    ]

    for title, accent, lines, h in sections:
        cx, cy, cw = card(draw, x, y, w, h, title, accent)
        if title == "API cần nhớ":
            draw_api_chips(draw, cx, cy + 8, topic["apis"][:9], cw)
        else:
            current_y = cy
            for item in lines:
                current_y = draw_wrapped(
                    draw,
                    (cx, current_y),
                    item,
                    font(28),
                    "#1F2937",
                    cw,
                    line_gap=7,
                    bullet=title != "Tóm tắt",
                    max_lines=3 if title == "Lỗi thường gặp + ví dụ" else None,
                )
                current_y += 8
        y += h + gap

    footer_y = height - 120
    draw.rounded_rectangle((margin, footer_y, width - margin, footer_y + 70), radius=22, fill="#111827", outline="#334155", width=1)
    draw.text((margin + 30, footer_y + 12), "Nền tảng khái niệm từ Revit 2014 Platform API Developers Guidelines", font=font(24), fill="#CBD5E1")
    draw.text((margin + 30, footer_y + 42), "Kiểm tra lại với Revit API version đang dùng trước khi code production.", font=font(24, bold=True), fill="#FDE68A")

    png_name = f"{topic['id']}.png"
    img.save(PNG_DIR / png_name, optimize=True)
    return png_name


def write_index(page_hits: dict[str, list[int]]) -> None:
    rows = []
    for i, topic in enumerate(TOPICS, start=1):
        note = f"02-topic-notes/{topic['id']}.md"
        png = f"04-infographics-png/{topic['id']}.png"
        prompt = f"03-infographic-prompts/{topic['id']}.md"
        rows.append(
            f"| {i:02d} | {topic['priority']} | [{topic['title']}]({note}) | [PNG]({png}) | [Prompt]({prompt}) | {compact_pages(page_hits[topic['id']], 4)} |"
        )

    content = clean_md(
        f"""\
        # Revit API 2014 Developers Guidelines - Vietnamese Infographic Knowledge Base

        Nguồn: `{PDF_PATH}`  
        Đầu ra: ghi chú Markdown + infographic PNG tiếng Việt. Nội dung được tóm tắt/diễn giải lại để học Revit API, không tái bản nguyên văn sách.

        ## Lộ trình học khuyến nghị
        1. Bắt đầu với poster 01-07 để hiểu add-in lifecycle, môi trường, command/application, ribbon và document context.
        2. Học poster 08-14 để nắm dữ liệu cốt lõi: Element, Parameter, Selection, Filtering, Transaction.
        3. Học poster 15-22 để làm việc với model: edit element, view, family, geometry, wall/floor/material.
        4. Học poster 23-27 theo domain: Structure, Rebar, Shared Parameters/Storage, Events/DMU, MEP.
        5. Dùng poster 28 như roadmap để biến kiến thức thành production add-in.

        ## Danh sách infographic
        | # | Priority | Topic note | PNG | Prompt | Source page hits |
        |---|---|---|---|---|---|
        {chr(10).join(rows)}

        ## Ghi chú version
        Tài liệu nguồn thuộc Revit API 2014. Các class cốt lõi vẫn hữu ích về mặt tư duy, nhưng khi code cho Revit 2025-2027 cần kiểm tra lại chữ ký method, namespace, unit API, transaction behavior và API bị deprecated.
        """
    )
    (OUT_DIR / "00-index.md").write_text(content, encoding="utf-8")


def write_core_concepts() -> None:
    p0 = [topic for topic in TOPICS if topic["priority"] == "P0"]
    content = [
        "# Core Concepts - Revit API",
        "",
        "Tài liệu này gom các khái niệm nền tảng cần nắm trước khi viết add-in thực tế.",
        "",
    ]
    for topic in p0:
        content.append(f"## {topic['title']}")
        content.append(topic["summary"])
        content.append("")
        content.append("API trọng tâm: " + ", ".join(f"`{api}`" for api in topic["apis"][:6]))
        content.append("")
    (OUT_DIR / "01-core-concepts.md").write_text("\n".join(content), encoding="utf-8")


def write_source_map(pages: list[str], page_hits: dict[str, list[int]]) -> None:
    info = run(["pdfinfo", str(PDF_PATH)])
    heading_patterns = [
        "Introduction",
        "Getting Started",
        "Add-In Integration",
        "Application and Document",
        "Element Essentials",
        "Filtering",
        "Selection",
        "Parameters",
        "Editing Elements",
        "Views",
        "Geometry",
        "Material",
        "Revit Structure",
        "Rebar",
        "Revit MEP",
        "Transactions",
        "Events",
        "Dynamic Model Update",
        "Failure Posting and Handling",
        "Analysis Visualization",
        "Appendix",
    ]
    heading_rows = []
    for heading in heading_patterns:
        hits = detect_page_hits(pages, [heading])
        heading_rows.append(f"| {heading} | {compact_pages(hits, 8)} |")

    topic_rows = []
    for topic in TOPICS:
        topic_rows.append(f"| {topic['title']} | {topic['priority']} | {compact_pages(page_hits[topic['id']], 8)} |")

    content = clean_md(
        f"""\
        # Source Map

        ## PDF metadata
        ```text
        {info.strip()}
        ```

        ## Major heading page hits
        | Heading | Page hits |
        |---|---|
        {chr(10).join(heading_rows)}

        ## Topic page hits used by this knowledge base
        | Topic | Priority | Page hits |
        |---|---|---|
        {chr(10).join(topic_rows)}

        ## Extraction policy
        - The PDF was processed with `pdftotext -layout`.
        - Notes are Vietnamese summaries and Revit API learning explanations.
        - Long verbatim copying from the source book is intentionally avoided.
        """
    )
    (SOURCE_DIR / "pdf-source-map.md").write_text(content, encoding="utf-8")


def main() -> None:
    if not PDF_PATH.exists():
        raise FileNotFoundError(PDF_PATH)
    ensure_dirs()
    pages = load_pdf_pages()
    page_hits: dict[str, list[int]] = {}
    for topic in TOPICS:
        page_hits[topic["id"]] = detect_page_hits(pages, topic["patterns"])

    for i, topic in enumerate(TOPICS, start=1):
        png_name = render_png(topic, i, page_hits[topic["id"]])
        (NOTES_DIR / f"{topic['id']}.md").write_text(make_topic_note(topic, page_hits[topic["id"]], png_name), encoding="utf-8")
        (PROMPTS_DIR / f"{topic['id']}.md").write_text(make_prompt(topic, png_name), encoding="utf-8")

    write_index(page_hits)
    write_core_concepts()
    write_source_map(pages, page_hits)

    summary = clean_md(
        f"""\
        Generated Revit API infographic knowledge base.

        - Topics: {len(TOPICS)}
        - Notes: {len(list(NOTES_DIR.glob('*.md')))}
        - Prompts: {len(list(PROMPTS_DIR.glob('*.md')))}
        - PNGs: {len(list(PNG_DIR.glob('*.png')))}
        - Output: {OUT_DIR}
        """
    )
    (OUT_DIR / "README.md").write_text(summary, encoding="utf-8")
    print(summary)


if __name__ == "__main__":
    main()
