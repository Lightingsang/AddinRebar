import os
import re

bridge_dir = r"HPGrasshopper\HPGrasshopper.McpBridge"

suspicious_patterns = [
    (r'return\s+true\s*;', "Unconditional return true"),
    (r'return\s+"success"', "Hardcoded success string"),
    (r'throw\s+new\s+NotImplementedException', "NotImplementedException"),
    (r'TODO', "TODO placeholder"),
    (r'FIXME', "FIXME placeholder"),
    (r'HACK', "HACK marker"),
    (r'mock', "Mock pattern in production"),
    (r'fake', "Fake pattern in production"),
]

print("=== CHECKING FOR FACADES, SHORTCUTS, OR HARDCODED RESULTS ===")
findings = []

for root, dirs, files in os.walk(bridge_dir):
    if any(x in root for x in ['bin', 'obj']):
        continue
    for f in files:
        if f.endswith('.cs'):
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, bridge_dir)
            with open(full_path, 'r', encoding='utf-8', errors='ignore') as fp:
                content = fp.read()
                lines = content.splitlines()
                for idx, line in enumerate(lines, 1):
                    # Check for suspicious patterns
                    for pat, desc in suspicious_patterns:
                        if re.search(pat, line, re.IGNORECASE):
                            # Filter out harmless occurrences
                            if "return true;" in line and any(k in line for k in ["bool", "IsPointTarget", "SingleItem", "MatchesOutput"]):
                                continue
                            if "Mock" in line or "Fake" in line:
                                continue # only flag if actual stub
                            findings.append((rel_path, idx, desc, line.strip()))

print(f"Total findings: {len(findings)}")
for f in findings[:25]:
    print(f"  {f[0]}:{f[1]} [{f[2]}] -> {f[3]}")
