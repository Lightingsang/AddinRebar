import os
import json

root_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

from verify_m4_clean_code import FILES

with open(r'scratch\all_members_m4.json', 'r', encoding='utf-8') as f:
    members = json.load(f)

print("| File Path | Status | Total Lines | Max Method Lines | Longest Member | Rule C2 (<=300) | Rule M3 (<=50) |")
print("| :--- | :--- | :---: | :---: | :--- | :---: | :---: |")

for rel in FILES:
    full = os.path.join(root_dir, rel)
    with open(full, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    total_lines = len(lines)
    
    file_members = [m for m in members if m['File'] == rel]
    max_m = max([m['Lines'] for m in file_members]) if file_members else 0
    top_m = [m for m in file_members if m['Lines'] == max_m][0] if file_members else None
    
    is_new = "Modified" if "Modified" in rel or rel in [
        r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
        r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.cs",
        r"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
        r"HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs"
    ] else "New"
    
    top_name = f"`{top_m['Member']}` (L{top_m['Start']}-L{top_m['End']})" if top_m else "N/A"
    c2_status = "**PASS**" if total_lines <= 300 else "**FAIL**"
    m3_status = "**PASS**" if max_m <= 50 else f"**FAIL** ({max_m}L)"
    
    clean_path = rel.replace('\\', '/')
    print(f"| `{clean_path}` | {is_new} | {total_lines} | {max_m if max_m > 0 else 'N/A'} | {top_name} | {c2_status} | {m3_status} |")
