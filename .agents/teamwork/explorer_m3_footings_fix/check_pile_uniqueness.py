import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
from collections import Counter

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

print(f"Total piles: {len(piles)}")
handles = [p['handle'] for p in piles]
print(f"Unique handles count: {len(set(handles))}")
assert len(handles) == len(set(handles)), "Handles are not unique!"

coords = [(p['revit_x'], p['revit_y']) for p in piles]
print(f"Unique coordinates count: {len(set(coords))}")
assert len(coords) == len(set(coords)), "Coordinates are not unique!"

print("All 196 piles have strictly unique handles and unique (revit_x, revit_y) coordinates!")
