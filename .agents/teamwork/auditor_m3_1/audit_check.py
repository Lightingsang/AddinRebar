import os
import re

test_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests"

fact_count = 0
theory_count = 0
inline_data_count = 0
assert_count = 0
suspicious_asserts = []
apis_used = set()

for root, dirs, files in os.walk(test_dir):
    if "obj" in root or "bin" in root:
        continue
    for file in sorted(files):
        if not file.endswith(".cs"):
            continue
        filepath = os.path.join(root, file)
        with open(filepath, "r", encoding="utf-8") as f:
            lines = f.readlines()

        for idx, line in enumerate(lines, 1):
            s = line.strip()
            if "[Fact]" in s:
                fact_count += 1
            if "[Theory]" in s:
                theory_count += 1
            if "[InlineData" in s:
                inline_data_count += 1
            if "Assert." in s:
                assert_count += 1
                # check for trivial / tautological assertions
                if (re.search(r"Assert\.True\s*\(\s*true\s*\)", s, re.IGNORECASE) or
                    re.search(r"Assert\.False\s*\(\s*false\s*\)", s, re.IGNORECASE) or
                    re.search(r"Assert\.Equal\s*\(\s*(\d+)\s*,\s*\1\s*\)", s) or
                    re.search(r'Assert\.Equal\s*\(\s*"([^"]+)"\s*,\s*"\1"\s*\)', s)):
                    suspicious_asserts.append((file, idx, s))

            # extract HPKata.Core API usages
            for m in re.finditer(r"\b(Rebar[A-Za-z0-9_]+|Kata[A-Za-z0-9_]+|CuttingStock[A-Za-z0-9_]+|Qr[A-Za-z0-9_]+|ClosedXml[A-Za-z0-9_]+)\b", s):
                apis_used.add(m.group(1))

print(f"[Fact] count: {fact_count}")
print(f"[Theory] count: {theory_count}")
print(f"[InlineData] count: {inline_data_count}")
print(f"Total Assert.* calls: {assert_count}")
print(f"Suspicious / trivial asserts: {len(suspicious_asserts)}")
for sa in suspicious_asserts:
    print("  ", sa)
print(f"\nHPKata.Core APIs referenced ({len(apis_used)} types/methods):")
for api in sorted(apis_used):
    print("  ", api)
