import os, re

hpdynamo_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo"

# Let's check catches in raw file (before comment stripping), 
# to see if the catch body has neither code nor comment.

def check_file(path):
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
        
    # Find all 'catch' keywords
    unannotated_empty = []
    # Match catch with optional (Type ex) followed by { ... }
    # We want to find catch blocks where between { and } there are ONLY whitespaces
    pattern = re.compile(r'catch(?:\s*\([^)]*\))?\s*\{([^{}]*)\}', re.MULTILINE)
    for m in pattern.finditer(content):
        body = m.group(1).strip()
        # If body is completely empty (no code, no comments // or /* */)
        if body == "":
            unannotated_empty.append((m.start(), m.group(0)))
            
    return unannotated_empty

results = []
for root, dirs, files in os.walk(hpdynamo_dir):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if f.endswith(".cs"):
            p = os.path.join(root, f)
            findings = check_file(p)
            if findings:
                rel = os.path.relpath(p, hpdynamo_dir)
                results.append((rel, findings))

print("Unannotated empty catches (NO code and NO comments):")
if results:
    for rel, findings in results:
        print(f"File: {rel}")
        for pos, text in findings:
            print(f"  {text}")
else:
    print("  ZERO unannotated empty catches found in entire HPDynamo!")
