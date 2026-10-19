import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('audit_raw_results.json', 'r', encoding='utf-8') as f:
    d = json.load(f)

piles = {p['id']: p for p in d['piles']}
footings = d['footings']

flagged = [9907741, 9907747, 9907766, 9907772, 9907784, 9907791, 9907853]
for pid in flagged:
    p = piles[pid]
    print(f"=== PILE {pid}: Mark={p['mark']}, Comment={p['comment']}, X={p['x']}, Y={p['y']}, Z={p['z']} ===")
    matching_fts = [f for f in footings if p['comment'] in f['mark'] or p['comment'] in f['comment']]
    for mf in matching_fts:
        print(f"   Matching Footing: ID={mf['id']}, Mark={mf['mark']}, Type={mf['type']}")
        print(f"   Footing BBox X: [{mf['minX']}, {mf['maxX']}], Y: [{mf['minY']}, {mf['maxY']}]")
        print(f"   Dx to minX: {p['x'] - mf['minX']}, Dx to maxX: {mf['maxX'] - p['x']}")
        print(f"   Dy to minY: {p['y'] - mf['minY']}, Dy to maxY: {mf['maxY'] - p['y']}")
