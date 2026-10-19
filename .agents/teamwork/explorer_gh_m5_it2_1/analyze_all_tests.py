import os
import glob
import re

tier_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"

report = []

flagged_methods = []

for filepath in sorted(glob.glob(os.path.join(tier_dir, "*.cs"))):
    fname = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    pattern = r"(\[(?:Fact|Theory)\][\s\S]*?(?:public void|public async Task)\s+(\w+)\s*\([^)]*\)[\s\S]*?\{([\s\S]*?\n    \}))"
    matches = re.findall(pattern, content)
    
    for full_decl, name, body in matches:
        body_text = body.strip()
        lines = [l.strip() for l in body_text.split("\n") if l.strip() and not l.strip().startswith("//")]
        
        reasons = []
        
        # Check 1: local arithmetic inside Assert or just before
        if any(re.search(r'Assert\.\w+\([^,)]*\s*[\+\*\-\/]\s*[^,)]*\)', l) for l in lines):
            reasons.append("Arithmetic in Assert expression")
        if any(re.search(r'\b\w+\s*=\s*\d+\s*[\*\/+-]\s*\d+', l) for l in lines):
            reasons.append("Local arithmetic calculation")
            
        # Check 2: Assert on local boolean or literal
        if any(re.search(r'Assert\.(?:True|False)\(\s*(?:isReadOnly|threadActive|requiresDryRun|timeout|durationMs|targetMaxLines|targetMaxMethodLines)\s*\)', l) for l in lines):
            reasons.append("Assert on local variable flag/threshold")
            
        # Check 3: Self-certifying string operations
        if any('preview' in l and 'doc.Create' in l for l in lines) and any('Assert.Contains("doc.Create", preview)' in l for l in lines):
            reasons.append("Assert string literal contains its own substring")
        if any('argsJson' in l and 'Assert.Contains("2.5", argsJson)' in l for l in lines):
            reasons.append("Assert JSON string literal contains substring without parsing")
        if any('"Light"' in l and '"Dark"' in l and 'Assert.True(theme == "Light" || theme == "Dark")' in l for l in lines):
            reasons.append("Assert array elements match array definition literals")
            
        # Check 4: Check if test does not invoke any genuine method or simulator method
        # E.g. No call to bridge, simulator, ScriptGuard, HostProfile, PipeNaming, etc.
        calls = [l for l in lines if '(' in l and not l.startswith('Assert.') and not l.startswith('var ') and not l.startswith('int ') and not l.startswith('string ') and not l.startswith('bool ')]
        
        # Check specific suspect tests by name or patterns
        for l in lines:
            if "Assert.True(isReadOnly)" in l:
                reasons.append("Assert.True(isReadOnly)")
            if "Assert.True(timeout <= 600)" in l:
                reasons.append("Assert.True(timeout <= 600)")
            if "Assert.True(targetMaxLines < maxAllowedLines)" in l:
                reasons.append("Assert.True(targetMaxLines < maxAllowedLines)")
            if "Assert.True(targetMaxMethodLines < maxAllowedMethodLines)" in l:
                reasons.append("Assert.True(targetMaxMethodLines < maxAllowedMethodLines)")
            if "Assert.Equal(72, expectedPanels)" in l:
                reasons.append("Assert.Equal(72, expectedPanels)")
            if "Assert.Equal(288, expectedCornerPoints)" in l:
                reasons.append("Assert.Equal(288, expectedCornerPoints)")
            if "Assert.True(durationMs > 0)" in l:
                reasons.append("Assert.True(durationMs > 0)")
            if "Assert.Equal(2000, widthScaled)" in l:
                reasons.append("Assert.Equal(2000, widthScaled)")
            if "Interlocked.Exchange(ref busy, 0)" in l:
                reasons.append("Local Interlocked.Exchange")
            if "is \"auto\" or \"manual\"" in l:
                reasons.append("Pattern match on local string literal")
            if "Assert.Contains(\"doc.Create\", preview)" in l:
                reasons.append("Assert.Contains on local preview string")
            if "Assert.Contains(\"2.5\", argsJson)" in l:
                reasons.append("Assert.Contains on local argsJson string")

        if reasons:
            flagged_methods.append({
                "file": fname,
                "name": name,
                "reasons": list(set(reasons)),
                "lines": lines
            })

print(f"Total flagged methods with suspect patterns: {len(flagged_methods)}")
for item in flagged_methods:
    print(f"{item['file']} :: {item['name']} -> {item['reasons']}")
