import json, math, os, sys
from collections import defaultdict

def main():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
        cad_spans = json.load(f)

    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        footings_data = json.load(f)
    footings = footings_data.get('footings', footings_data)

    print(f"Loaded {len(cad_spans)} CAD spans, {len(footings)} footings.")

    # Check footings polygon map
    footing_polys = []
    for f in footings:
        poly = [(v['x'], v['y']) for v in f['vertices']]
        footing_polys.append((f['id'], poly))

    # Helper point in polygon
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

    # Helper dist point to poly
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

    # Test all cad_spans endpoints
    beam_endpoints = []
    for s in cad_spans:
        beam_endpoints.append((s['x1'], s['y1']))
        beam_endpoints.append((s['x2'], s['y2']))

    unsupported_cad = []
    for idx, s in enumerate(cad_spans):
        for pt_name, (x, y) in [('p1', (s['x1'], s['y1'])), ('p2', (s['x2'], s['y2']))]:
            # In footing?
            d_footing = min(dist_to_poly(x, y, poly) for _, poly in footing_polys)
            # Touching another beam endpoint?
            d_beam = min(math.hypot(x - ox, y - oy) for ox, oy in beam_endpoints if (abs(x - ox) > 0.1 or abs(y - oy) > 0.1))
            
            # Distance to another beam segment (T-junction)?
            d_seg = 1e9
            for o_idx, os in enumerate(cad_spans):
                if o_idx == idx: continue
                # segment
                vx, vy = os['x2'] - os['x1'], os['y2'] - os['y1']
                l = math.hypot(vx, vy)
                if l > 0:
                    t = max(0.0, min(1.0, ((x - os['x1'])*vx + (y - os['y1'])*vy) / (l*l)))
                    px = os['x1'] + t * vx
                    py = os['y1'] + t * vy
                    d_seg = min(d_seg, math.hypot(x - px, y - py))

            unsupported_cad.append({
                'span_idx': idx,
                'name': s['name'],
                'pt_name': pt_name,
                'pt': (x, y),
                'd_footing': d_footing,
                'd_beam_end': d_beam,
                'd_beam_seg': d_seg
            })

    # Floating endpoints: d_footing > 25mm AND d_beam_end > 25mm AND d_beam_seg > 25mm
    floating = [u for u in unsupported_cad if u['d_footing'] > 25.0 and u['d_beam_end'] > 25.0 and u['d_beam_seg'] > 25.0]
    print(f"CAD spans floating endpoints (>25mm from footing, beam end, beam seg): {len(floating)}")

    # What if only d_footing > 25 and d_beam_end > 25 (QA/QC audit test)?
    floating_audit_style = [u for u in unsupported_cad if u['d_footing'] > 25.0 and u['d_beam_end'] > 25.0]
    print(f"CAD spans floating endpoints per QA/QC audit rule (footing or other beam END within 25mm): {len(floating_audit_style)}")

    # Let's inspect the distance distribution of these floating endpoints
    dist_ranges = defaultdict(int)
    for u in floating:
        d = min(u['d_footing'], u['d_beam_end'], u['d_beam_seg'])
        if d <= 50: dist_ranges['25-50mm'] += 1
        elif d <= 100: dist_ranges['50-100mm'] += 1
        elif d <= 150: dist_ranges['100-150mm'] += 1
        elif d <= 200: dist_ranges['150-200mm'] += 1
        else: dist_ranges['>200mm'] += 1
    print("Distance distribution to nearest support (footing or beam):", dict(dist_ranges))

    print("\nSample floating endpoints:")
    for u in floating[:15]:
        print(f"  Span {u['span_idx']} {u['name']} {u['pt_name']} at {u['pt']}: d_footing={u['d_footing']:.1f}mm, d_beam_end={u['d_beam_end']:.1f}mm, d_beam_seg={u['d_beam_seg']:.1f}mm")

if __name__ == '__main__':
    main()
