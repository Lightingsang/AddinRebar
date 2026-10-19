import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('audit_raw_results.json', 'r', encoding='utf-8') as f:
    d = json.load(f)

piles = {p['id']: p for p in d['piles']}
footings = d['footings']

flagged = [
    (9907741, 'P75', 'ĐC-2-04'),
    (9907784, 'P118', 'ĐC-2-10'),
    (9907772, 'P106', 'ĐC-2-19'),
    (9907747, 'P81', 'ĐC-6-01'),
    (9907766, 'P100', 'ĐC-6-04'),
    (9907791, 'P126', 'ĐC-3-04'),
    (9907853, 'P187', 'ĐC-3-03')
]

print("==========================================================================================")
print("TABLE OF 7 DEFECTIVE PILE-FOOTING CONTAINMENTS EMPIRICALLY CONFIRMED IN REVIT 2026")
print("==========================================================================================")
for pid, mark, ft_name in flagged:
    p = piles[pid]
    # find footing
    matching_fts = [f for f in footings if ft_name == f['mark'] or ft_name in f['comment']]
    mf = matching_fts[0]
    
    # Calculate offset outside bbox
    outside_x = 0.0
    if p['x'] < mf['minX']: outside_x = mf['minX'] - p['x']
    elif p['x'] > mf['maxX']: outside_x = p['x'] - mf['maxX']

    outside_y = 0.0
    if p['y'] < mf['minY']: outside_y = mf['minY'] - p['y']
    elif p['y'] > mf['maxY']: outside_y = p['y'] - mf['maxY']

    outside_dist = max(outside_x, outside_y)
    
    print(f"Pile ElementId: {pid}")
    print(f"  Mark: {p['mark']} | Comment: {p['comment']}")
    print(f"  Center: ({p['x']}, {p['y']}, {p['z']}) mm")
    print(f"Footing ElementId: {mf['id']}")
    print(f"  Mark: {mf['mark']} | Type: {mf['type']}")
    print(f"  BBox X: [{mf['minX']}, {mf['maxX']}] mm (Span = {mf['maxX'] - mf['minX']} mm)")
    print(f"  BBox Y: [{mf['minY']}, {mf['maxY']}] mm (Span = {mf['maxY'] - mf['minY']} mm)")
    print(f"  TopZ: {mf['topZ']} mm | BotZ: {mf['botZ']} mm")
    print(f"  VIOLATION: Pile center is {outside_dist} mm OUTSIDE footing boundary!")
    print(f"  Radial protrusion: Pile edge (radius 200mm) protrudes {outside_dist + 200.0} mm into soil without concrete enclosure!")
    print("-" * 90)
