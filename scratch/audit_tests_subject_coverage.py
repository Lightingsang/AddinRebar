import os
import re

tiers_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers"
test_files = [os.path.join(tiers_dir, f) for f in os.listdir(tiers_dir) if f.endswith(".cs")]

subject_keywords = [
    "bridge", "Bridge", "Harness", "Grasshopper", "Mcp", "ScriptGuard",
    "ScriptQuality", "ToolValidator", "SeedInstaller", "ScriptArgs",
    "HostProfile", "GuardProfile", "AnalyzerProfile", "Pipe", "Runner",
    "runner", "vm", "StatusViewModel", "Roslyn", "Transaction", "Ledger"
]

tests_missing_subjects = []

for tf in test_files:
    fname = os.path.basename(tf)
    with open(tf, "r", encoding="utf-8") as f:
        content = f.read()

    # Split into test methods
    # find [Fact] or [Theory]
    method_matches = list(re.finditer(r'\[(Fact|Theory)\]\s*(public\s+[^\n]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\))', content))
    for i, m in enumerate(method_matches):
        test_name = m.group(3)
        start_pos = m.start()
        end_pos = method_matches[i+1].start() if i + 1 < len(method_matches) else len(content)
        method_body = content[start_pos:end_pos]

        if not any(kw in method_body for kw in subject_keywords):
            tests_missing_subjects.append((fname, test_name))

print(f"Total test methods analyzed across 20 files: {sum(len(re.findall(r'\[(Fact|Theory)\]', open(os.path.join(tiers_dir, f), encoding='utf-8').read())) for f in os.listdir(tiers_dir) if f.endswith('.cs'))}")
print(f"Tests missing deliverable subject calls: {len(tests_missing_subjects)}")
for t in tests_missing_subjects:
    print("  Missing subject:", t)
