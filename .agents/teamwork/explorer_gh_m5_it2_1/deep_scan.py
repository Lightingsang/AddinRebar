import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

all_flagged = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    for full_decl, name, body in matches:
        body_text = body.strip()
        lines = [l.strip() for l in body_text.split("\n") if l.strip() and not l.strip().startswith("//")]
        
        # Check all statements before Assert
        # If there are only primitive variable declarations, or very few lines
        non_asserts = [l for l in lines if not l.startswith("Assert.")]
        
        # Look for suspicious patterns
        issues = []
        for l in lines:
            # Check for constant/trivial assertions
            if re.search(r'Assert\.(?:True|False)\(\s*(?:true|false)\s*\)', l):
                issues.append(f"Literal boolean assert: {l}")
            if re.search(r'Assert\.Equal\(\s*(\d+|"[^"]*")\s*,\s*\1\s*\)', l):
                issues.append(f"Self-equality assert: {l}")
            if re.search(r'Assert\.True\(\s*\w+\s*(?:<=|>=|<|>|==|!=)\s*\w+\s*\)', l) and len(lines) <= 6:
                issues.append(f"Comparison on local vars in small test: {l}")
            if re.search(r'Assert\.Contains\(\s*"[^"]+"\s*,\s*\w+\s*\)', l):
                # check if the target string was defined in the test as a literal
                target_var = re.search(r'Assert\.Contains\(\s*"[^"]+"\s*,\s*(\w+)\s*\)', l).group(1)
                for na in non_asserts:
                    if na.startswith(f'var {target_var} = "') or na.startswith(f'string {target_var} = "'):
                        issues.append(f"Contains on local literal string variable {target_var}: {l}")
            if re.search(r'Assert\.(?:True|False)\(\s*\w+\s*\)', l):
                var_name = re.search(r'Assert\.(?:True|False)\(\s*(\w+)\s*\)', l).group(1)
                for na in non_asserts:
                    if na.startswith(f'bool {var_name} = ') or na.startswith(f'var {var_name} = '):
                        if 'true' in na or 'false' in na:
                            issues.append(f"Assert on local bool initialized to literal: {na} -> {l}")
            if re.search(r'Assert\.(?:True|False)\(\s*(\w+)\s*(?:==|!=)\s*"[^"]+"\s*(?:\|\|\s*\1\s*(?:==|!=)\s*"[^"]+")*\s*\)', l):
                issues.append(f"Assert on enum/string comparison over local loop: {l}")
            if re.search(r'Assert\.(?:True|False)\(\s*\w+\s*>\s*0\s*\)', l):
                var_name = re.search(r'Assert\.(?:True|False)\(\s*(\w+)\s*>\s*0\s*\)', l).group(1)
                for na in non_asserts:
                    if re.search(rf'\b{var_name}\s*=\s*\d+L?;', na):
                        issues.append(f"Assert on local integer constant > 0: {na} -> {l}")
            if re.search(r'Assert\.Equal\(\s*\d+\s*,\s*\d+\s*[\*\+\-\/]\s*\d+\s*\)', l):
                issues.append(f"Arithmetic inside assert: {l}")
            if re.search(r'Assert\.Equal\(\s*\d+\s*,\s*\w+\s*\)', l):
                var_name = re.search(r'Assert\.Equal\(\s*\d+\s*,\s*(\w+)\s*\)', l).group(1)
                for na in non_asserts:
                    if re.search(rf'\b{var_name}\s*=\s*.*[\*\+\-\/]', na):
                        # check if na only operates on local literals
                        if not any(call in na for call in ['Count', 'Length', 'bridge', 'harness', 'spec', 'Spec', 'Canvas']):
                            issues.append(f"Assert on local arithmetic variable: {na} -> {l}")

        # Also check tests that just test GrasshopperE2EContracts shadow constants
        contracts_only = all(
            ('GrasshopperE2EContracts' in l or 'Assert.' in l or 'var ' in l or 'string ' in l or 'int ' in l)
            for l in lines
        ) and any('GrasshopperE2EContracts' in l for l in lines)
        if contracts_only:
            issues.append("Tests only GrasshopperE2EContracts shadow constants without calling production/engine code")

        if issues or len(lines) <= 4:
            all_flagged.append({
                "file": fname,
                "name": name,
                "issues": issues,
                "lines": lines
            })

print(f"Total potential candidates: {len(all_flagged)}")
for c in all_flagged:
    print(f"\n{c['file']} :: {c['name']}")
    for iss in c['issues']:
        print(f"   [ISSUE] {iss}")
    if not c['issues']:
        print(f"   [SHORT TEST] {len(c['lines'])} lines: {c['lines']}")
