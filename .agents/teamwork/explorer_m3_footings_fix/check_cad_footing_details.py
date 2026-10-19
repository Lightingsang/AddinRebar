import sys
sys.stdout.reconfigure(encoding='utf-8')
import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_footing_details_dc.json', 'r', encoding='utf-8') as f:
    details = json.load(f)

print(f"Total items in cad_footing_details_dc.json: {details.get('totalItems')}")
for item in details.get('items', [])[:20]:
    print(item)
