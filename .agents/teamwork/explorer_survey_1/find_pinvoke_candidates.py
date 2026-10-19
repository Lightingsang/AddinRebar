import subprocess
import re

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
entities_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt"

with open(entities_file, "r", encoding="utf-8") as f:
    entities = [line.strip() for line in f if line.strip()]

print(f"Total entities: {len(entities)}")

# Find candidates matching math, geom, analysis, dllimport
keywords = ["analysis", "pinvoke", "native", "geom", "math", "dceanalyssiss", "dcepro", "dceanalysis"]
candidates = []
for e in entities:
    e_low = e.lower()
    if any(k in e_low for k in keywords):
        candidates.append(e)

print(f"Found {len(candidates)} candidate entities matching keywords:")
for c in candidates[:50]:
    print(f"  {c}")
