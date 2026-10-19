import os
import re

tiers_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"
test_files = [os.path.join(tiers_dir, f) for f in os.listdir(tiers_dir) if f.endswith(".cs")]

print(f"Auditing {len(test_files)} Tier test files...")

local_functions = []
suspicious_patterns = []

for tf in test_files:
    fname = os.path.basename(tf)
    with open(tf, "r", encoding="utf-8") as f:
        content = f.read()

    # Look for local function declarations: 'static bool ...' or 'static void ...' or local functions
    # inside methods
    for m in re.finditer(r'static\s+(bool|int|void|string|double|object)\s+([A-Za-z0-9_]+)\s*\(', content):
        lineno = content[:m.start()].count('\n') + 1
        local_functions.append((fname, lineno, m.group(0)))

    # Look for inline ternary evaluating a local string length only
    for m in re.finditer(r'var\s+[a-zA-Z0-9_]+\s*=\s*[a-zA-Z0-9_]+\.Length\s*>\s*\d+\s*\?', content):
        lineno = content[:m.start()].count('\n') + 1
        suspicious_patterns.append((fname, lineno, m.group(0), "Inline ternary on length"))

print("Local functions detected:", len(local_functions))
for lf in local_functions:
    print("  Local function:", lf)

print("Suspicious inline ternaries detected:", len(suspicious_patterns))
for sp in suspicious_patterns:
    print("  Suspicious ternary:", sp)
