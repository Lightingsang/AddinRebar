import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import math

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    fts_data = json.load(f)
    footings = fts_data['footings']

# Note: piles_true_bim_model.json has duplicate STTs: {52: 2, 75: 2, 106: 2, 118: 2, 126: 2, 187: 2}
# Let's create unique pile list with index i in 0..195
pile_by_handle = {p['handle']: p for p in piles}

print("=== CHECKING VERIFIED_72_FOOTINGS.JSON ===")
print(f"Total footings: {len(footings)}")

# Check pile assignments in verified_72_footings
# Each footing has pileStts
# But wait, verified_72_footings only has pileStts, not pile handles!
for ft in footings:
    fid = ft['id']
    stts = ft['pileStts']
    # Check if any duplicate in stts
    if len(stts) != len(set(stts)):
        print(f"Footing {fid} has duplicate STTs in pileStts: {stts}")

# Check pile containment using actual pile coordinates from piles_true_bim_model
# For each pile, check clearance to footing bbox/polygon
pile_radius = 200.0 # D400 -> R200

# Let's inspect each footing's bounding box vs its assigned piles
# But since some footings used duplicate stts, let's see which physical piles were intended
