import json
import math

with open('full_shapes_analysis.json', 'r', encoding='utf-8') as f:
    shapes_analysis = json.load(f)

with open('wmf_geometries.json', 'r', encoding='utf-8') as f:
    wmf_geoms = json.load(f)

with open('block_attributes.txt', 'r', encoding='utf-8', errors='ignore') as f:
    block_raw = f.read()

# Let's inspect each shape from 00 to 67
results = []

for i in range(68):
    code = f"{i:02d}"
    bname = f"dang{code}"
    
    analysis = shapes_analysis.get(bname, {})
    geom = wmf_geoms.get(bname, {})
    
    legs = analysis.get('legs', [])
    wmf_texts = analysis.get('wmf_texts', [])
    
    # Also check variants
    variants = []
    for var in [f"dang{code}a", f"dang{code}b"]:
        if var in shapes_analysis:
            variants.append(var)
            
    results.append({
        'code': code,
        'bname': bname,
        'legs': legs,
        'wmf_texts': wmf_texts,
        'variants': variants,
        'geom_pts': geom.get('main_poly_pts', [])
    })

print(f"Loaded {len(results)} base shapes.")
