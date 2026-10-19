import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import math
from collections import defaultdict

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings = json.load(f)['footings']

# Check how many piles each footing has
print(f"Total footings: {len(footings)}")
total_assigned_stts = sum(len(f['pileStts']) for f in footings)
print(f"Total assigned STTs: {total_assigned_stts}")

# Let's map every single pile in piles_true_bim_model.json to a unique identifier: handle
pile_by_handle = {p['handle']: p for p in piles}

# Let's see which piles were assigned to which footing in verified_72_footings.json
# Note that verified_72_footings.json only stored pileStts!
# But for piles with duplicate STTs, which handle was used?
dup_stts = {52: 2, 75: 2, 106: 2, 118: 2, 126: 2, 187: 2}

# Let's check each footing:
problematic_ids = [
    'ĐC-2-04', 'ĐC-2-10', 'ĐC-2-19', 'ĐC-6-01', 'ĐC-6-04', 
    'ĐC-3-04', 'ĐC-3-03', 'ĐC-3\'-02', 'ĐC-3\'-01', 'ĐC-6-02', 'ĐC-2-09'
]

print("\n--- CHECKING REMAINING 61 FOOTINGS (EXCLUDING PROBLEMATIC ONES) ---")
for ft in footings:
    fid = ft['id']
    if fid in problematic_ids:
        continue
    stts = ft['pileStts']
    # Check if any STT in stts is in dup_stts
    has_dup = any(s in dup_stts for s in stts)
    if has_dup:
        print(f"Footing {fid} has duplicate STT: {stts}")

    # Check pile coordinates vs footing bounding box
    ft_piles = [p for p in piles if p['stt'] in stts]
    # Check if number of matching piles equals ft['pileCount']
    if len(ft_piles) != ft['pileCount']:
        print(f"Warning: Footing {fid} expected {ft['pileCount']} piles, found {len(ft_piles)}")
        continue

    # Compute actual pile centroid
    true_cx = sum(p['revit_x'] for p in ft_piles) / len(ft_piles)
    true_cy = sum(p['revit_y'] for p in ft_piles) / len(ft_piles)
    diff_cx = abs(true_cx - ft['centerX'])
    diff_cy = abs(true_cy - ft['centerY'])
    if diff_cx > 1.0 or diff_cy > 1.0:
        print(f"Footing {fid}: Centroid diff: dX={diff_cx:.1f}, dY={diff_cy:.1f} (stored: {ft['centerX']}, {ft['centerY']} vs true: {true_cx:.1f}, {true_cy:.1f})")

    # Check pile edge clearance to footing boundary
    # Bounding box of footing:
    min_x = min(v['x'] for v in ft['vertices'])
    max_x = max(v['x'] for v in ft['vertices'])
    min_y = min(v['y'] for v in ft['vertices'])
    max_y = max(v['y'] for v in ft['vertices'])

    R = 200.0 # pile radius
    min_clearance = 999999.0
    for p in ft_piles:
        # clearance to edges
        cl_left = (p['revit_x'] - min_x) - R
        cl_right = (max_x - p['revit_x']) - R
        cl_bot = (p['revit_y'] - min_y) - R
        cl_top = (max_y - p['revit_y']) - R
        cl = min(cl_left, cl_right, cl_bot, cl_top)
        if cl < min_clearance:
            min_clearance = cl

    if min_clearance < 149.0: # tolerance
        print(f"Footing {fid}: Minimum pile edge clearance is {min_clearance:.1f}mm (< 150mm)!")
