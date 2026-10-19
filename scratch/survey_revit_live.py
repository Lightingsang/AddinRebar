import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

PIPE_PATH = r'\\.\pipe\hprebar-mcp-r2026'

class RevitPipeSession:
    def __init__(self, pipe_path=PIPE_PATH):
        self.pipe_path = pipe_path
        self.f = open(pipe_path, 'r+b', buffering=0)
        self.msg_id = 100

    def execute(self, code, label="Revit Query", timeout=60):
        self.msg_id += 1
        req = {
            'jsonrpc': '2.0',
            'id': self.msg_id,
            'method': 'revit.execute',
            'params': {
                'code': code,
                'transaction': 'None',
                'dryRun': False,
                'timeoutSeconds': timeout,
                'label': label
            }
        }
        self.f.write((json.dumps(req) + '\n').encode('utf-8'))
        while True:
            line = self.f.readline().decode('utf-8')
            if not line:
                break
            try:
                data = json.loads(line)
            except Exception:
                continue
            if data.get('id') == self.msg_id:
                return data

    def close(self):
        try:
            self.f.close()
        except Exception:
            pass

def main():
    print("=== SURVEYING REVIT MODEL VIA SINGLE SESSION ===")
    session = RevitPipeSession()
    try:
        # 1. Beams detailed query
        beams_code = """
        var collector = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType();
        
        var list = new List<object>();
        foreach (FamilyInstance b in collector)
        {
            var crv = (b.Location as LocationCurve)?.Curve;
            var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
            var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
            double len = crv != null ? crv.Length * 304.8 : 0.0;
            
            list.Add(new {
                id = b.Id.Value,
                family = b.Symbol.FamilyName,
                type = b.Symbol.Name,
                mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                comments = b.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                level = doc.GetElement(b.LevelId)?.Name ?? "",
                lenMm = Math.Round(len, 1),
                zMm = Math.Round(p0.Z * 304.8, 1),
                x1 = Math.Round(p0.X * 304.8, 1),
                y1 = Math.Round(p0.Y * 304.8, 1),
                x2 = Math.Round(p1.X * 304.8, 1),
                y2 = Math.Round(p1.Y * 304.8, 1)
            });
        }
        return new {
            total = list.Count,
            items = list
        };
        """
        
        beams_res = session.execute(beams_code, "Inspect Framing")
        beams_items = beams_res['result']['value']['items']
        print(f"Total Beams: {len(beams_items)}")
        
        type_counts = {}
        z_counts = {}
        for b in beams_items:
            t = f"{b['family']}: {b['type']}"
            type_counts[t] = type_counts.get(t, 0) + 1
            z = str(b['zMm'])
            z_counts[z] = z_counts.get(z, 0) + 1
        print("Beam Types:", json.dumps(type_counts, indent=2))
        print("Beam Z Elevations:", json.dumps(z_counts, indent=2))
        
        # 2. Grids and Levels query
        grids_levels_code = """
        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .Select(g => {
                var c = g.Curve;
                var p0 = c.GetEndPoint(0);
                var p1 = c.GetEndPoint(1);
                return new {
                    id = g.Id.Value,
                    name = g.Name,
                    pinned = g.Pinned,
                    x1 = Math.Round(p0.X * 304.8, 1),
                    y1 = Math.Round(p0.Y * 304.8, 1),
                    x2 = Math.Round(p1.X * 304.8, 1),
                    y2 = Math.Round(p1.Y * 304.8, 1)
                };
            })
            .OrderBy(g => g.name)
            .ToList();
            
        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(l => new {
                id = l.Id.Value,
                name = l.Name,
                elevMm = Math.Round(l.Elevation * 304.8, 1),
                pinned = l.Pinned
            })
            .OrderBy(l => l.elevMm)
            .ToList();
            
        return new { grids = grids, levels = levels };
        """
        gl_res = session.execute(grids_levels_code, "Inspect Grids & Levels")
        gl_data = gl_res['result']['value']
        print(f"Total Grids: {len(gl_data['grids'])}")
        print(f"Total Levels: {len(gl_data['levels'])}")
        for l in gl_data['levels']:
            print(f"  Level '{l['name']}': Elev = {l['elevMm']} mm, Pinned = {l['pinned']}")
        
        # 3. Piles query
        piles_code = """
        var piles = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Select(fi => {
                var loc = (LocationPoint)fi.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                var pL = fi.LookupParameter("L") ?? fi.LookupParameter("Length") ?? fi.LookupParameter("Chiều dài");
                double len = pL != null ? pL.AsDouble() * 304.8 : 0.0;
                return new {
                    id = fi.Id.Value,
                    family = fi.Symbol.FamilyName,
                    type = fi.Symbol.Name,
                    mark = fi.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    xMm = Math.Round(pt.X * 304.8, 1),
                    yMm = Math.Round(pt.Y * 304.8, 1),
                    zMm = Math.Round(pt.Z * 304.8, 1),
                    offsetMm = Math.Round(offsetFt * 304.8, 1),
                    lenMm = Math.Round(len, 1),
                    level = doc.GetElement(fi.LevelId)?.Name ?? ""
                };
            })
            .ToList();
            
        return new {
            total = piles.Count,
            items = piles
        };
        """
        piles_res = session.execute(piles_code, "Inspect Piles")
        piles_items = piles_res['result']['value']['items']
        print(f"Total Piles: {len(piles_items)}")
        
        pile_type_counts = {}
        pile_z_counts = {}
        for p in piles_items:
            t = f"{p['family']}: {p['type']}"
            pile_type_counts[t] = pile_type_counts.get(t, 0) + 1
            z = str(p['zMm'])
            pile_z_counts[z] = pile_z_counts.get(z, 0) + 1
        print("Pile Types:", json.dumps(pile_type_counts, indent=2))
        print("Pile Z Elevations:", json.dumps(pile_z_counts, indent=2))
        
        # 4. Floors / Slabs query
        floors_code = """
        var floors = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .Cast<Floor>()
            .Select(f => {
                var level = (Level)doc.GetElement(f.LevelId);
                double offsetFt = f.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;
                double thickFt = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() ?? 0.0;
                string comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
                return new {
                    id = f.Id.Value,
                    typeName = f.FloorType.Name,
                    comment = comment,
                    level = level?.Name ?? "",
                    thickMm = Math.Round(thickFt * 304.8, 1),
                    topZMm = Math.Round((level != null ? level.Elevation + offsetFt : 0.0) * 304.8, 1),
                    bottomZMm = Math.Round(((level != null ? level.Elevation + offsetFt : 0.0) - thickFt) * 304.8, 1)
                };
            })
            .ToList();
        return new {
            total = floors.Count,
            items = floors
        };
        """
        floors_res = session.execute(floors_code, "Inspect Floors")
        floors_items = floors_res['result']['value']['items']
        print(f"Total Floors/Slabs: {len(floors_items)}")
        floor_types = {}
        for f in floors_items:
            t = f['typeName']
            floor_types[t] = floor_types.get(t, 0) + 1
        print("Floor Types:", json.dumps(floor_types, indent=2))

        # Save all to JSON
        summary = {
            "beams": {
                "total": len(beams_items),
                "type_counts": type_counts,
                "z_counts": z_counts,
                "items": beams_items
            },
            "grids": gl_data['grids'],
            "levels": gl_data['levels'],
            "piles": {
                "total": len(piles_items),
                "type_counts": pile_type_counts,
                "z_counts": pile_z_counts,
                "items": piles_items
            },
            "floors": {
                "total": len(floors_items),
                "type_counts": floor_types,
                "items": floors_items
            }
        }
        out_file = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\revit_live_survey_summary.json"
        with open(out_file, 'w', encoding='utf-8') as f:
            json.dump(summary, f, indent=2, ensure_ascii=False)
        print(f"Saved full survey to {out_file}")

    finally:
        session.close()

if __name__ == '__main__':
    main()
