import json, math
from collections import defaultdict

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)
piles = piles_data.get("piles", [])

# Find components of piles with distance <= 1400mm
adj = defaultdict(list)
for i in range(len(piles)):
    for j in range(i + 1, len(piles)):
        p1 = piles[i]
        p2 = piles[j]
        d = math.hypot(p1["revit_x"] - p2["revit_x"], p1["revit_y"] - p2["revit_y"])
        if d <= 1400:
            adj[i].append(j)
            adj[j].append(i)

visited = set()
components = []
for i in range(len(piles)):
    if i not in visited:
        comp = []
        q = [i]
        visited.add(i)
        while q:
            curr = q.pop()
            comp.append(curr)
            for neighbor in adj[curr]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    q.append(neighbor)
        components.append(comp)

print(f"Total components at threshold 1400mm: {len(components)}")
comp_sizes = defaultdict(int)
for c in components:
    comp_sizes[len(c)] += 1

for sz in sorted(comp_sizes.keys()):
    print(f"  Cap with {sz} piles: {comp_sizes[sz]} caps (total {sz * comp_sizes[sz]} piles)")
