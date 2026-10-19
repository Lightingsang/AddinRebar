import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

output_lines = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    # Regex to find test methods
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    output_lines.append(f"\n=======================================================")
    output_lines.append(f"FILE: {fname} (Tests found: {len(matches)})")
    output_lines.append(f"=======================================================")
    
    for full_decl, name, body in matches:
        lines = [l.strip() for l in body.split("\n") if l.strip() and not l.strip().startswith("//")]
        asserts = [l for l in lines if l.startswith("Assert.")]
        
        # Check signs of trivial/self-certifying tests:
        # e.g., Assert on local variables without external calls, or literal comparisons
        has_external_call = any(
            ("bridge." in l or "harness." in l or "ScriptGuard." in l or "HostProfile." in l or
             "PipeNaming." in l or "Grasshopper" in l or "File." in l or "Directory." in l or
             "Tool" in l or "Catalog" in l or "Runner" in l)
            for l in lines if not l.startswith("Assert.")
        )
        
        output_lines.append(f"\n--- Method: {name} ---")
        output_lines.append(f"Body ({len(lines)} lines):")
        for l in lines:
            output_lines.append(f"    {l}")

with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_1\all_tests_dump.txt", "w", encoding="utf-8") as out:
    out.write("\n".join(output_lines))

print(f"Dumped all tests to all_tests_dump.txt")
