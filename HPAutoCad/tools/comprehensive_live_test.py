import sys, os, json

sys.path.insert(0, r"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\McpShared\tools")
from harness_common import Server, Checklist, utf8_console

exe = r"F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe"

def run_comprehensive_suite():
    utf8_console()
    cl = Checklist()
    server = Server(exe)
    server.initialize()

    print("\n=======================================================")
    print("   HPAutoCad MCP Comprehensive Live Verification Suite")
    print("=======================================================\n")

    # 1. Context check
    print("1. Testing get_autocad_context...")
    try:
        ctx = server.tool("get_autocad_context", {})
        doc_title = ctx.get("docTitle", "")
        units = ctx.get("units", {}).get("length", "")
        current_layer = ctx.get("autocad", {}).get("currentLayer", "")
        exec_enabled = ctx.get("executionEnabled", False)

        print(f"   Document: {doc_title}")
        print(f"   Units: {units}")
        print(f"   Current Layer: {current_layer}")
        print(f"   Execution Enabled: {exec_enabled}")

        cl.check("get_autocad_context", bool(doc_title and exec_enabled), f"doc={doc_title}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("get_autocad_context", False, str(ex))

    # 2. Drawing Info
    print("\n2. Testing get_drawing_info...")
    try:
        res = server.tool("get_drawing_info", {})
        info = res.get("value", {})
        ms_count = info.get("modelSpaceEntities", 0)
        layer_count = info.get("layerCount", 0)
        counts = info.get("countsByType", {})
        print(f"   ModelSpace Entities: {ms_count}")
        print(f"   Layers: {layer_count}")
        print(f"   LWPOLYLINE: {counts.get('LWPOLYLINE', 0)}, POINT: {counts.get('POINT', 0)}")
        cl.check("get_drawing_info", ms_count > 0 and layer_count > 0, f"entities={ms_count}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("get_drawing_info", False, str(ex))

    # 3. AEC Classification
    print("\n3. Testing classify_aec_entities...")
    try:
        res = server.tool("classify_aec_entities", {})
        val = res.get("value", {})
        summary = val.get("summary", {})
        examined = summary.get("examined", 0)
        classified = summary.get("classified", 0)
        grids = summary.get("byAecType", {}).get("StructuralGrid", 0)
        print(f"   Examined: {examined}, Classified: {classified} (StructuralGrid: {grids})")
        cl.check("classify_aec_entities", examined > 0, f"examined={examined}, grids={grids}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("classify_aec_entities", False, str(ex))

    # 4. CAD Standards Check
    print("\n4. Testing cad_standards_check...")
    try:
        res = server.tool("cad_standards_check", {})
        val = res.get("value", {})
        summary = val.get("summary", {})
        examined = summary.get("examined", 0)
        issues = summary.get("issues", 0)
        by_sev = summary.get("bySeverity", {})
        print(f"   Examined: {examined}, Issues: {issues} (Warnings: {by_sev.get('warning', 0)})")
        cl.check("cad_standards_check", examined > 0, f"examined={examined}, issues={issues}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("cad_standards_check", False, str(ex))

    # 5. Geometric Issues Detection
    print("\n5. Testing detect_geometry_issues...")
    try:
        res = server.tool("detect_geometry_issues", {})
        val = res.get("value", {})
        summary = val.get("summary", {})
        examined = summary.get("examined", 0)
        issues = summary.get("issues", 0)
        print(f"   Examined: {examined}, Geometry Issues: {issues}")
        cl.check("detect_geometry_issues", examined > 0, f"examined={examined}, issues={issues}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("detect_geometry_issues", False, str(ex))

    # 6. Execute C# Code: Query RanhDat and DOCAO survey geometry
    print("\n6. Testing execute_autocad_code: Query Boundary & Survey Points...")
    code_query_geo = """
    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    var db = doc.Database;
    var bt = (Autodesk.AutoCAD.DatabaseServices.BlockTable)tr.GetObject(db.BlockTableId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
    var ms = (Autodesk.AutoCAD.DatabaseServices.BlockTableRecord)tr.GetObject(bt[Autodesk.AutoCAD.DatabaseServices.BlockTableRecord.ModelSpace], Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);

    int ranhDatCount = 0;
    int vertexCount = 0;
    double area = 0.0;
    double length = 0.0;
    var vertices = new System.Collections.Generic.List<object>();

    int pointCount = 0;
    var surveyPoints = new System.Collections.Generic.List<object>();

    foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId id in ms)
    {
        var ent = tr.GetObject(id, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Entity;
        if (ent == null) continue;

        if (ent.Layer == "RanhDat" && ent is Autodesk.AutoCAD.DatabaseServices.Polyline pl)
        {
            ranhDatCount++;
            vertexCount = pl.NumberOfVertices;
            area = pl.Area;
            length = pl.Length;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                vertices.Add(new { index = i, x = pt.X, y = pt.Y });
            }
        }
        else if (ent.Layer == "DOCAO" && ent is Autodesk.AutoCAD.DatabaseServices.DBPoint pt)
        {
            pointCount++;
            surveyPoints.Add(new { x = pt.Position.X, y = pt.Position.Y, z = pt.Position.Z });
        }
    }

    return new
    {
        ranhDatFound = ranhDatCount > 0,
        vertices = vertexCount,
        area = area,
        length = length,
        coordinates = vertices,
        surveyPointsFound = pointCount,
        pointsSample = surveyPoints.Count > 3 ? surveyPoints.GetRange(0, 3) : surveyPoints
    };
    """
    try:
        res = server.tool("execute_autocad_code", {"code": code_query_geo, "transaction": "auto"})
        res_data = res.get("value")
        if res_data and res_data.get("ranhDatFound"):
            coords = res_data.get("coordinates", [])
            print(f"   Boundary Validated: {len(coords)} vertices, Area: {res_data.get('area', 0):.2f} m², Perimeter: {res_data.get('length', 0):.2f} m")
            print(f"   Survey Points on DOCAO: {res_data.get('surveyPointsFound', 0)}")
            cl.check("execute_autocad_code_boundary", True, f"vertices={len(coords)}, area={res_data.get('area', 0):.2f}")
        else:
            cl.check("execute_autocad_code_boundary", False, f"Boundary polygon not found: {res}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("execute_autocad_code_boundary", False, str(ex))

    # 7. Check Drawing Extents & VN-2000 Coordinate Integrity
    print("\n7. Testing execute_autocad_code: VN-2000 Coordinate System Validation...")
    code_check_crs = """
    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
    var db = doc.Database;
    var bt = (Autodesk.AutoCAD.DatabaseServices.BlockTable)tr.GetObject(db.BlockTableId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
    var ms = (Autodesk.AutoCAD.DatabaseServices.BlockTableRecord)tr.GetObject(bt[Autodesk.AutoCAD.DatabaseServices.BlockTableRecord.ModelSpace], Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);

    double minX = double.MaxValue, minY = double.MaxValue;
    double maxX = double.MinValue, maxY = double.MinValue;
    int sampled = 0;

    foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId id in ms)
    {
        var ent = tr.GetObject(id, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Entity;
        if (ent == null) continue;

        if (ent.Layer == "RanhDat" && ent is Autodesk.AutoCAD.DatabaseServices.Polyline pl)
        {
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
                sampled++;
            }
        }
    }

    // Check if boundary coordinates fall within Vietnam / Binh Duong geographic bounds in VN-2000
    // Binh Duong meridian 105°45' or 105°00': X around 580000 - 620000, Y around 1220000 - 1250000
    bool isVN2000 = minX > 500000 && maxX < 700000 && minY > 1100000 && maxY < 1300000;

    return new
    {
        min = new { x = minX, y = minY },
        max = new { x = maxX, y = maxY },
        width = maxX - minX,
        height = maxY - minY,
        sampledVertices = sampled,
        isVN2000 = isVN2000
    };
    """
    try:
        res = server.tool("execute_autocad_code", {"code": code_check_crs, "transaction": "auto"})
        res_data = res.get("value")
        if res_data and res_data.get("isVN2000"):
            print(f"   VN-2000 Boundary Extents Confirmed: X [{res_data['min']['x']:.1f} .. {res_data['max']['x']:.1f}], Y [{res_data['min']['y']:.1f} .. {res_data['max']['y']:.1f}]")
            print(f"   Footprint Size: {res_data.get('width', 0):.1f}m x {res_data.get('height', 0):.1f}m")
            cl.check("coordinate_validation_vn2000", True, f"X span={res_data.get('width', 0):.1f}m, Y span={res_data.get('height', 0):.1f}m")
        else:
            cl.check("coordinate_validation_vn2000", False, f"Coordinates not VN-2000: {res_data}")
    except Exception as ex:
        print("   FAILED:", ex)
        cl.check("coordinate_validation_vn2000", False, str(ex))

    server.close()

    print("\n=======================================================")
    print("                    Summary")
    print("=======================================================")
    exit_code = cl.finish()
    if exit_code == 0:
        print("\n>>> ALL LIVE MCP CHECKS PASSED WITH ZERO ERRORS! <<<")
    else:
        print(f"\n>>> CHECKS FAILED: {cl.failed} <<<")
    sys.exit(exit_code)

if __name__ == "__main__":
    run_comprehensive_suite()
