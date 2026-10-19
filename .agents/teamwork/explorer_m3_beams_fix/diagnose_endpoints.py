import json, math, os, sys
from collections import defaultdict
sys.stdout.reconfigure(encoding="utf-8")



def main():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
        spans = json.load(f)

    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        footings_data = json.load(f)
    footings = footings_data.get('footings', footings_data)

    with open('grids.json', 'r', encoding='utf-8') as f:
        grids_data = json.load(f)
    x_grids = grids_data['x_grids']
    y_grids = grids_data['y_grids']

    # Footing bounding boxes & polygons
    f_list = []
    for f in footings:
        xs = [v['x'] for v in f['vertices']]
        ys = [v['y'] for v in f['vertices']]
        f_list.append({
            'id': f['id'],
            'minX': min(xs), 'maxX': max(xs),
            'minY': min(ys), 'maxY': max(ys),
            'cx': sum(xs)/len(xs),
            'cy': sum(ys)/len(ys),
            'poly': [(v['x'], v['y']) for v in f['vertices']]
        })

    def point_in_poly(x, y, poly):
        inside = False
        n = len(poly)
        j = n - 1
        for i in range(n):
            xi, yi = poly[i]
            xj, yj = poly[j]
            if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi):
                inside = not inside
            j = i
        return inside

    def dist_to_poly(x, y, poly):
        if point_in_poly(x, y, poly):
            return 0.0
        min_d = 1e9
        n = len(poly)
        for i in range(n):
            x1, y1 = poly[i]
            x2, y2 = poly[(i+1)%n]
            dx, dy = x2 - x1, y2 - y1
            l2 = dx*dx + dy*dy
            if l2 == 0:
                d = math.hypot(x - x1, y - y1)
            else:
                t = max(0.0, min(1.0, ((x - x1)*dx + (y - y1)*dy) / l2))
                px = x1 + t * dx
                py = y1 + t * dy
                d = math.hypot(x - px, y - py)
            min_d = min(min_d, d)
        return min_d

    print("Detailed Endpoint Diagnosis for 192 Spans:")
    
    # Analyze each span
    for i, s in enumerate(spans):
        is_h = abs(s['y1'] - s['y2']) < 1.0
        dir_str = "H (along X)" if is_h else "V (along Y)"
        
        # Check P1
        p1 = (s['x1'], s['y1'])
        p2 = (s['x2'], s['y2'])
        
        # Distance to footings
        f_d1 = [(f['id'], dist_to_poly(p1[0], p1[1], f['poly']), f) for f in f_list]
        f_d1.sort(key=lambda x: x[1])
        closest_f1 = f_d1[0]

        f_d2 = [(f['id'], dist_to_poly(p2[0], p2[1], f['poly']), f) for f in f_list]
        f_d2.sort(key=lambda x: x[1])
        closest_f2 = f_d2[0]

        # Check nearest intersecting beams
        # For H span (constant Y), intersecting beams are V spans (constant X) whose Y interval covers s['y1']
        # For V span (constant X), intersecting beams are H spans (constant Y) whose X interval covers s['x1']
        other_lines = []
        if is_h:
            y = s['y1']
            for j, os in enumerate(spans):
                if i == j: continue
                if abs(os['x1'] - os['x2']) < 1.0: # V beam
                    ox = os['x1']
                    oy_min = min(os['y1'], os['y2'])
                    oy_max = max(os['y1'], os['y2'])
                    # Does V beam cross or terminate near y?
                    if oy_min - 200.0 <= y <= oy_max + 200.0:
                        other_lines.append((ox, os['name'], j, os['bMm']))
        else:
            x = s['x1']
            for j, os in enumerate(spans):
                if i == j: continue
                if abs(os['y1'] - os['y2']) < 1.0: # H beam
                    oy = os['y1']
                    ox_min = min(os['x1'], os['x2'])
                    ox_max = max(os['x1'], os['x2'])
                    if ox_min - 200.0 <= x <= ox_max + 200.0:
                        other_lines.append((oy, os['name'], j, os['bMm']))

        s['is_h'] = is_h
        s['closest_f1'] = (closest_f1[0], round(closest_f1[1], 1))
        s['closest_f2'] = (closest_f2[0], round(closest_f2[1], 1))
        s['perp_lines'] = other_lines

    # Print summary of endpoints that are NOT inside a footing (d > 0)
    floating_p1 = [i for i, s in enumerate(spans) if s['closest_f1'][1] > 25.0]
    floating_p2 = [i for i, s in enumerate(spans) if s['closest_f2'][1] > 25.0]

    print(f"P1 outside footings (>25mm): {len(floating_p1)}")
    print(f"P2 outside footings (>25mm): {len(floating_p2)}")

    # For each floating endpoint, see what it is near!
    print("\n--- Diagnostic of floating endpoints ---")
    for idx in sorted(set(floating_p1 + floating_p2))[:20]:
        s = spans[idx]
        print(f"Span {idx} ({s['name']}): ({s['x1']}, {s['y1']}) -> ({s['x2']}, {s['y2']}) [{ 'H' if s['is_h'] else 'V' }]")
        if s['closest_f1'][1] > 25.0:
            print(f"  P1 outside footing: dist={s['closest_f1'][1]}mm to {s['closest_f1'][0]}")
        else:
            print(f"  P1 inside footing {s['closest_f1'][0]}")
            
        if s['closest_f2'][1] > 25.0:
            print(f"  P2 outside footing: dist={s['closest_f2'][1]}mm to {s['closest_f2'][0]}")
        else:
            print(f"  P2 inside footing {s['closest_f2'][0]}")
            
        print(f"  Perpendicular intersecting beams count: {len(s['perp_lines'])}")
        for pl in s['perp_lines'][:3]:
            print(f"    coord={pl[0]}, beam={pl[1]}, b={pl[3]}")

if __name__ == '__main__':
    main()
