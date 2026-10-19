import os, re

tiers_dir = r"HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers"
test_files = [os.path.join(tiers_dir, f) for f in os.listdir(tiers_dir) if f.endswith(".cs")]
test_files.sort()

# Scan each method in each test file
method_re = re.compile(r"public\s+(?:async\s+Task|void)\s+([A-Za-z0-9_]+)\s*\(")

findings = []

for tf in test_files:
    fname = os.path.basename(tf)
    with open(tf, "r", encoding="utf-8") as f:
        content = f.read()

    lines = content.splitlines()
    current_method = None
    method_lines = []
    method_start = 0

    for i, line in enumerate(lines, 1):
        m = method_re.search(line)
        if m:
            if current_method:
                findings.append((fname, current_method, method_start, method_lines))
            current_method = m.group(1)
            method_start = i
            method_lines = [line]
        elif current_method:
            method_lines.append(line)
            if line.strip() == "}":
                # Check brace balance if needed, or approximate
                pass
    if current_method:
        findings.append((fname, current_method, method_start, method_lines))

print(f"Total test methods indexed: {len(findings)}")

suspicious = []
for fname, mname, start, mlines in findings:
    body = "\n".join(mlines)
    # Check for trivial assertions
    # 1. Asserting local bool literals or self
    if re.search(r"Assert\.True\(\s*(?:true|[a-zA-Z0-9_]+\s*==\s*[a-zA-Z0-9_]+)\s*\)", body):
        # check if it's comparing local literals
        pass
    # 2. Arithmetic in assertions e.g. 12 * 6
    if re.search(r"Assert\.Equal\(\s*\d+\s*,\s*\d+\s*[\*\+\-\/]", body):
        suspicious.append((fname, mname, start, "Arithmetic in assertion"))
    # 3. Local string literal contains
    if re.search(r'var\s+([a-zA-Z0-9_]+)\s*=\s*"[^"]+".*Assert\.Contains\("[^"]+",\s*\1\)', body, re.DOTALL):
        suspicious.append((fname, mname, start, "String literal contains check"))

print(f"Suspicious method patterns found: {len(suspicious)}")
for s in suspicious:
    print(s)
