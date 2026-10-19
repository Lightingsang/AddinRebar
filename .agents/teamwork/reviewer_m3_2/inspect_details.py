import json
import sys
import os
import math

sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

sys.stdout.reconfigure(encoding="utf-8")

import time
ipc = None
for attempt in range(10):
    try:
        ipc = RevitIPC()
        break
    except Exception as e:
        time.sleep(0.5)

if ipc is None:
    print("Could not connect to Revit pipe after 10 attempts")
    sys.exit(1)

with ipc:
    # Query footings with actual curve loops or bounding boxes
    q_f = """
    var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
        .Select(f => {
            var bb = f.get_BoundingBox(null);
            return new {
                id = f.Id.Value,
                mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                type = f.FloorType.Name,
                minX = bb != null ? Math.Round(bb.Min.X * 304.8, 1) : 0.0,
                maxX = bb != null ? Math.Round(bb.Max.X * 304.8, 1) : 0.0,
                minY = bb != null ? Math.Round(bb.Min.Y * 304.8, 1) : 0.0,
                maxY = bb != null ? Math.Round(bb.Max.Y * 304.8, 1) : 0.0,
                botZ = bb != null ? Math.Round(bb.Min.Z * 304.8, 1) : 0.0,
                topZ = bb != null ? Math.Round(bb.Max.Z * 304.8, 1) : 0.0
            };
        }).ToList();
    return footings;
    """
    rf = ipc.execute(q_f, transaction='none', label='Inspect Footings')
    footings = rf['result']['value']
    if isinstance(footings, str): footings = json.loads(footings)

    # Query piles
    q_p = """
    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
        .Select(p => {
            var loc = (LocationPoint)p.Location;
            var pt = loc != null ? loc.Point : XYZ.Zero;
            return new {
                id = p.Id.Value,
                mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                x = Math.Round(pt.X * 304.8, 1),
                y = Math.Round(pt.Y * 304.8, 1),
                z = Math.Round(pt.Z * 304.8, 1)
            };
        }).ToList();
    return piles;
    """
    rp = ipc.execute(q_p, transaction='none', label='Inspect Piles')
    piles = rp['result']['value']
    if isinstance(piles, str): piles = json.loads(piles)

    # Query beams
    q_b = """
    var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().Cast<FamilyInstance>()
        .Select(b => {
            var crv = (b.Location as LocationCurve)?.Curve;
            var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
            var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
            return new {
                id = b.Id.Value,
                mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                sym = b.Symbol.Name,
                x1 = Math.Round(p0.X * 304.8, 1),
                y1 = Math.Round(p0.Y * 304.8, 1),
                x2 = Math.Round(p1.X * 304.8, 1),
                y2 = Math.Round(p1.Y * 304.8, 1)
            };
        }).ToList();
    return beams;
    """
    rb = ipc.execute(q_b, transaction='none', label='Inspect Beams')
    beams = rb['result']['value']
    if isinstance(beams, str): beams = json.loads(beams)

footings_by_mark = {f['mark']: f for f in footings}

print("=== 1. DETAIL OF 5 PILE CONTAINMENT VIOLATIONS ===")
for p in piles:
    f_mark = p['comment']
    f = footings_by_mark.get(f_mark)
    if f:
        margin = 50.0
        in_x = f['minX'] - margin <= p['x'] <= f['maxX'] + margin
        in_y = f['minY'] - margin <= p['y'] <= f['maxY'] + margin
        if not (in_x and in_y):
            print(f"Pile ID {p['id']} (Mark: {p['mark']}) assigned to {f_mark}:")
            print(f"  Pile: ({p['x']}, {p['y']})")
            print(f"  Footing BBox X: [{f['minX']}, {f['maxX']}], Y: [{f['minY']}, {f['maxY']}]")
            print(f"  Delta X min: {p['x'] - f['minX']}, max: {f['maxX'] - p['x']}")
            print(f"  Delta Y min: {p['y'] - f['minY']}, max: {f['maxY'] - p['y']}")
            # Also check if pile is inside ANY other footing!
            inside_others = [of['mark'] for of in footings if of['minX'] - margin <= p['x'] <= of['maxX'] + margin and of['minY'] - margin <= p['y'] <= of['maxY'] + margin]
            print(f"  Actually inside other footing bbox: {inside_others}\n")
    else:
        print(f"Pile ID {p['id']} has unknown footing mark '{f_mark}'")

print("\n=== 2. DETAIL OF 17 UNCONNECTED BEAM ENDS ===")
# Let's inspect each of the 17 beam ends
def find_nearest_footing(pt, f_list):
    px, py = pt
    nearest = None
    min_d = 999999.0
    for f in f_list:
        cx = (f['minX'] + f['maxX']) / 2.0
        cy = (f['minY'] + f['maxY']) / 2.0
        # Distance to bbox
        dx = max(f['minX'] - px, 0, px - f['maxX'])
        dy = max(f['minY'] - py, 0, py - f['maxY'])
        d = math.hypot(dx, dy)
        if d < min_d:
            min_d = d
            nearest = (f['mark'], d, cx, cy)
    return nearest

for b in beams:
    # check start
    for end_name, pt in [('start', (b['x1'], b['y1'])), ('end', (b['x2'], b['y2']))]:
        px, py = pt
        # check footing within 200mm
        in_f = any(f['minX'] - 200 <= px <= f['maxX'] + 200 and f['minY'] - 200 <= py <= f['maxY'] + 200 for f in footings)
        # check other beams
        in_b = False
        for ob in beams:
            if ob['id'] == b['id']: continue
            d0 = math.hypot(px - ob['x1'], py - ob['y1'])
            d1 = math.hypot(px - ob['x2'], py - ob['y2'])
            if d0 <= 50 or d1 <= 50:
                in_b = True
                break
            vx, vy = ob['x2'] - ob['x1'], ob['y2'] - ob['y1']
            l = math.hypot(vx, vy)
            if l > 1.0:
                ux, uy = vx / l, vy / l
                proj = (px - ob['x1']) * ux + (py - ob['y1']) * uy
                if 0 <= proj <= l:
                    perp = abs((px - ob['x1']) * uy - (py - ob['y1']) * ux)
                    if perp <= 50:
                        in_b = True
                        break
        if not (in_f or in_b):
            nf = find_nearest_footing(pt, footings)
            print(f"Beam {b['mark']} (ID {b['id']}, type {b['sym']}) {end_name} at ({px}, {py}):")
            print(f"  Nearest footing: {nf[0]} at distance {nf[1]:.1f} mm (center {nf[2]:.1f}, {nf[3]:.1f})")
