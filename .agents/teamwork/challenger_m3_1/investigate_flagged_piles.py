import json
import csv
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('audit_raw_results.json', 'r', encoding='utf-8') as f:
    revit_data = json.load(f)

piles = {p['id']: p for p in revit_data['piles']}
footings = revit_data['footings']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\Bang_Toa_Do_Tim_Coc_Chuan_Ky_Thuat.csv', 'r', encoding='utf-8') as f:
    cad_csv = list(csv.DictReader(f))

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json', 'r', encoding='utf-8') as f:
    worker_piles = json.load(f)

# Inspect the 6 piles
flagged_ids = [9907717, 9907741, 9907772, 9907784, 9907791, 9907853, 9907747, 9907766]
print("--- FLAGGED PILES DETAILS IN REVIT ---")
for pid in flagged_ids:
    p = piles[pid]
    print(f"Pile {pid}: Mark='{p['mark']}', Comment='{p['comment']}', X={p['x']}, Y={p['y']}, Z={p['z']}")

print("\n--- WORKER MAPPING FOR THESE PILES ---")
for wp in worker_piles:
    if wp['id'] in flagged_ids or wp.get('elementId') in flagged_ids:
        print("Worker pile:", wp)

print("\n--- NEARBY FOOTINGS FOR PILE 9907741 (X=13950, Y=400) ---")
for ft in footings:
    dx = abs((ft['minX'] + ft['maxX'])/2 - 13950)
    dy = abs((ft['minY'] + ft['maxY'])/2 - 400)
    if dx < 3000 and dy < 3000:
        print(f"Footing ID {ft['id']}, Mark='{ft['mark']}', Comment='{ft['comment']}', X=[{ft['minX']}, {ft['maxX']}], Y=[{ft['minY']}, {ft['maxY']}], TopZ={ft['topZ']}, BotZ={ft['botZ']}")

print("\n--- NEARBY CAD CSV ROWS AROUND (13950, 400) ---")
for r in cad_csv:
    rx, ry = float(r['Tọa độ Revit X (mm)']), float(r['Tọa độ Revit Y (mm)'])
    if abs(rx - 13950) < 2000 and abs(ry - 400) < 2000:
        print(r)
