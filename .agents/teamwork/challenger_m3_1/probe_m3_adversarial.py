import json
import os
import sys
import math

REVIT_PIPE = r'\\.\pipe\hprebar-mcp-r2026'
ACAD_PIPE = r'\\.\pipe\hpautocad-mcp-2026'

class PipeClient:
    def __init__(self, pipe_path, method_prefix):
        self.pipe_path = pipe_path
        self.method_prefix = method_prefix
        self.f = open(pipe_path, 'r+b', buffering=0)
        self.msg_id = 300

    def execute(self, code, transaction="none", timeout=120, label="Adversarial Probe", args=None):
        self.msg_id += 1
        params = {
            'code': code,
            'transaction': transaction,
            'dryRun': False,
            'timeoutSeconds': timeout,
            'label': label
        }
        if args is not None:
            params['args'] = args
        req = {
            'jsonrpc': '2.0',
            'id': self.msg_id,
            'method': f'{self.method_prefix}.execute',
            'params': params
        }
        self.f.write((json.dumps(req) + '\n').encode('utf-8'))
        buf = b""
        while True:
            chunk = self.f.read(65536)
            if not chunk:
                break
            buf += chunk
            while b'\n' in buf:
                raw_line, buf = buf.split(b'\n', 1)
                raw_line = raw_line.strip()
                if not raw_line:
                    continue
                try:
                    data = json.loads(raw_line.decode('utf-8'))
                    if data.get('id') == self.msg_id:
                        return data
                except Exception:
                    continue

    def close(self):
        try:
            self.f.close()
        except Exception:
            pass

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

def parse_val(res):
    if res.get('isError') or res.get('result', {}).get('isError'):
        raise RuntimeError(f"RPC Error: {res}")
    val = res['result']['value']
    if isinstance(val, str):
        try:
            return json.loads(val)
        except Exception:
            return val
    return val

def run_adversarial_probe():
    results = {}
    with PipeClient(REVIT_PIPE, 'revit') as r_cli:
        print("=== PROBING LIVE REVIT 2026 VIA NAMED PIPE ===")

        # 1. Grids & Levels
        print("1. Probing Grids & Levels...")
        code_gl = """
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().Select(g => {
            var crv = g.Curve;
            var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
            var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
            return new {
                id = g.Id.Value,
                name = g.Name,
                pinned = g.Pinned,
                x0 = Math.Round(p0.X * 304.8, 2),
                y0 = Math.Round(p0.Y * 304.8, 2),
                x1 = Math.Round(p1.X * 304.8, 2),
                y1 = Math.Round(p1.Y * 304.8, 2)
            };
        }).OrderBy(g => g.name).ToList();

        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(l => new {
            id = l.Id.Value,
            name = l.Name,
            elevMm = Math.Round(l.Elevation * 304.8, 2),
            pinned = l.Pinned
        }).OrderBy(l => l.elevMm).ToList();

        return new { grids = grids, levels = levels };
        """
        res_gl = parse_val(r_cli.execute(code_gl, label="Probe Grids & Levels"))
        results['grids'] = res_gl['grids']
        results['levels'] = res_gl['levels']
        print(f"   Grids: {len(results['grids'])}, Levels: {len(results['levels'])}")

        # 2. Framing (Beams)
        print("2. Probing Structural Framing (Beams)...")
        code_b = """
        var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().Cast<FamilyInstance>()
            .Select(b => {
                var sym = b.Symbol;
                var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                double bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 2) : 0.0;
                double hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 2) : 0.0;
                var bb = b.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                var crv = (b.Location as LocationCurve)?.Curve;
                var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
                var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
                double lenMm = crv != null ? Math.Round(crv.Length * 304.8, 2) : 0.0;
                return new {
                    id = b.Id.Value,
                    name = b.Name,
                    mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = b.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    symName = sym.Name,
                    bMm = bMm,
                    hMm = hMm,
                    topZ = Math.Round(topZ, 2),
                    botZ = Math.Round(botZ, 2),
                    lenMm = lenMm,
                    x1 = Math.Round(p0.X * 304.8, 2),
                    y1 = Math.Round(p0.Y * 304.8, 2),
                    z1 = Math.Round(p0.Z * 304.8, 2),
                    x2 = Math.Round(p1.X * 304.8, 2),
                    y2 = Math.Round(p1.Y * 304.8, 2),
                    z2 = Math.Round(p1.Z * 304.8, 2)
                };
            }).OrderBy(b => b.id).ToList();
        return beams;
        """
        results['beams'] = parse_val(r_cli.execute(code_b, label="Probe Beams"))
        print(f"   Beams count: {len(results['beams'])}")

        # 3. Piles (OST_StructuralFoundation)
        print("3. Probing Piles...")
        code_p = """
        var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;
                var bb = p.get_BoundingBox(null);
                double bbMaxZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double bbMinZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                return new {
                    id = p.Id.Value,
                    sym = p.Symbol.Name,
                    fam = p.Symbol.FamilyName,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    z = Math.Round(zTopMm, 2),
                    bbMaxZ = Math.Round(bbMaxZ, 2),
                    bbMinZ = Math.Round(bbMinZ, 2)
                };
            }).OrderBy(p => p.id).ToList();
        return piles;
        """
        results['piles'] = parse_val(r_cli.execute(code_p, label="Probe Piles"))
        print(f"   Piles count: {len(results['piles'])}")

        # 4. Footings (OST_Floors)
        print("4. Probing Footings...")
        code_f = """
        var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                double minX = bb != null ? bb.Min.X * 304.8 : 0.0;
                double minY = bb != null ? bb.Min.Y * 304.8 : 0.0;
                double maxX = bb != null ? bb.Max.X * 304.8 : 0.0;
                double maxY = bb != null ? bb.Max.Y * 304.8 : 0.0;
                double thick = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0;
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    type = f.FloorType.Name,
                    topZ = Math.Round(topZ, 2),
                    botZ = Math.Round(botZ, 2),
                    thickMm = Math.Round(thick, 2),
                    minX = Math.Round(minX, 2),
                    minY = Math.Round(minY, 2),
                    maxX = Math.Round(maxX, 2),
                    maxY = Math.Round(maxY, 2)
                };
            }).OrderBy(f => f.id).ToList();
        return footings;
        """
        results['footings'] = parse_val(r_cli.execute(code_f, label="Probe Footings"))
        print(f"   Footings count: {len(results['footings'])}")

    # Save raw query result to temporary audit file
    with open('audit_raw_results.json', 'w', encoding='utf-8') as out_f:
        json.dump(results, out_f, indent=2)
    print("Raw query results saved to audit_raw_results.json")
    return results

if __name__ == '__main__':
    run_adversarial_probe()
