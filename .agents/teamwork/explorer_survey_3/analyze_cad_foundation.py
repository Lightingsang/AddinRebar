import json, sys
from collections import Counter

sys.stdout.reconfigure(encoding="utf-8")

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json", "r", encoding="utf-8") as f:
    data = json.load(f)

print("totalFootings:", data.get("totalFootings"))
print("totalFootingTexts:", data.get("totalFootingTexts"))
print("totalBeamTexts:", data.get("totalBeamTexts"))

ft_texts = data.get("footingTexts", [])
counter = Counter(t["text"] for t in ft_texts)
print("\nFooting text breakdown:")
for k, v in sorted(counter.items()):
    print(f"  {k}: {v}")

print(f"\nTotal footing texts: {len(ft_texts)}")

# Check beam texts
bm_texts = data.get("beamTexts", [])
bm_counter = Counter()
for b in bm_texts:
    txt = b["text"]
    if "GM-" in txt or "GM" in txt or "BN-" in txt or "BN" in txt:
        bm_counter[txt] += 1

print("\nBeam text breakdown (top 30):")
for k, v in bm_counter.most_common(30):
    print(f"  {k}: {v}")
