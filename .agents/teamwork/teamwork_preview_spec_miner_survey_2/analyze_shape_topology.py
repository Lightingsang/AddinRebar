import json
import math

with open('wmf_geometries.json', 'r', encoding='utf-8') as f:
    geoms = json.load(f)

with open('full_shapes_analysis.json', 'r', encoding='utf-8') as f:
    shapes_info = json.load(f)

def analyze_poly(pts):
    if not pts or len(pts) < 2:
        return "empty", []
    
    # simplify consecutive duplicate points
    clean = [pts[0]]
    for p in pts[1:]:
        if p[0] != clean[-1][0] or p[1] != clean[-1][1]:
            clean.append(p)
            
    if len(clean) < 2:
        return "single point", []
        
    segments = []
    for i in range(len(clean)-1):
        dx = clean[i+1][0] - clean[i][0]
        dy = clean[i+1][1] - clean[i][1]
        length = math.hypot(dx, dy)
        angle = math.degrees(math.atan2(dy, dx))
        segments.append((dx, dy, length, angle))
        
    bends = []
    for i in range(len(segments)-1):
        a1 = segments[i][3]
        a2 = segments[i+1][3]
        diff = (a2 - a1) % 360
        if diff > 180:
            diff -= 360
        bends.append(round(diff, 1))
        
    # classify topology
    num_segs = len(segments)
    is_closed = math.hypot(clean[-1][0] - clean[0][0], clean[-1][1] - clean[0][1]) < 10
    
    desc = f"{num_segs} segs, closed={is_closed}, bends={bends}"
    return desc, bends

for i in range(68):
    code = f"{i:02d}"
    bname = f"dang{code}"
    g = geoms.get(bname, {})
    pts = g.get('main_poly_pts', [])
    desc, bends = analyze_poly(pts)
    s_info = shapes_info.get(bname, {})
    wmf_txt = s_info.get('wmf_texts', [])
    legs = s_info.get('legs', [])
    print(f"[{code}] {bname:8} | Legs: {str(legs):25} | WMF: {str(wmf_txt):30} | {desc}")
