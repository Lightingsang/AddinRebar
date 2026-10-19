import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import math
from collections import defaultdict

# 1. Load piles
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)
piles = piles_data.get("piles", [])

# 2. Load footing texts
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json", "r", encoding="utf-8") as f:
    cad_el = json.load(f)
footing_texts = cad_el.get("footingTexts", [])

# 3. Known specs for each footing type:
SPECS = {
    "ĐC-1":  {"piles": 1, "B": 700,  "L": 700,  "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-2":  {"piles": 2, "B": 900,  "L": 1600, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-2'": {"piles": 2, "B": 900,  "L": 1600, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-3":  {"piles": 3, "B": 1000, "L": 2200, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-3'": {"piles": 3, "B": 1000, "L": 2200, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 400},
    "ĐC-4":  {"piles": 4, "B": 1600, "L": 1600, "H": 1200, "topZ": -1.600, "botZ": -2.800, "pileTopZ": -2.700, "mac": 300},
    "ĐC-5":  {"piles": 5, "B": 1600, "L": 2500, "H": 1200, "topZ": -1.600, "botZ": -2.800, "pileTopZ": -2.700, "mac": 300},
    "ĐC-6":  {"piles": 6, "B": 1600, "L": 2500, "H": 1300, "topZ": -1.600, "botZ": -2.900, "pileTopZ": -2.800, "mac": 400},
}

print(f"Total piles: {len(piles)}")
print(f"Total footing texts: {len(footing_texts)}")
