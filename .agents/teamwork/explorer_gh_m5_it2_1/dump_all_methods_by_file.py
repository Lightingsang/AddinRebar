import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    print(f"\n==========================================")
    print(f"FILE: {fname} ({len(matches)} tests)")
    print(f"==========================================")
    
    for full_decl, name, body in matches:
        lines = [l.strip() for l in body.split("\n") if l.strip() and not l.strip().startswith("//")]
        print(f"\n[{name}]")
        for l in lines:
            print(f"    {l}")
