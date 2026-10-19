import re

with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it2_1\all_tests_dump.txt", "r", encoding="utf-8") as f:
    text = f.read()

# Split by "--- Method: "
methods = text.split("--- Method: ")
print(f"Total methods in dump: {len(methods) - 1}")

short_or_suspicious = []

for m in methods[1:]:
    lines = m.split("\n")
    name = lines[0].split(" ---")[0].strip()
    body_lines = [l.strip() for l in lines[1:] if l.strip()]
    
    # Check assertions in body
    assert_lines = [l for l in body_lines if "Assert." in l]
    # Check if there are only 1 or 2 statements before Assert
    stmt_lines = [l for l in body_lines if not l.startswith("Assert.") and not l.startswith("Body (") and not l.startswith("}")]
    
    # If the test has very few statements and no method calls with arguments
    suspicious = False
    reasons = []
    
    # check for trivial asserts
    for al in assert_lines:
        if "Assert.True(true)" in al or "Assert.False(false)" in al:
            suspicious = True
            reasons.append("Literal bool in assert")
        if re.search(r'Assert\.\w+\(\s*(\d+|"[^"]*")\s*,\s*\1\s*\)', al):
            suspicious = True
            reasons.append(f"Identical expected and actual: {al}")
        if re.search(r'Assert\.\w+\([^,)]*[\*\+\-\/][^,)]*\)', al):
            suspicious = True
            reasons.append(f"Math in assert: {al}")
        if re.search(r'Assert\.(?:True|False)\(\s*\w+\s*\)', al):
            # check what this var is
            var = re.search(r'Assert\.(?:True|False)\(\s*(\w+)\s*\)', al).group(1)
            for sl in stmt_lines:
                if f"bool {var} =" in sl or f"var {var} =" in sl:
                    if "true" in sl or "false" in sl or "is" in sl or "==" in sl or "!=" in sl or ">" in sl or "<" in sl:
                        # check if sl calls an external function
                        if not any(fn in sl for fn in ["Check", "Validate", "Execute", "Verify", "Run", "Invoke", "Contains", "Parse"]):
                            suspicious = True
                            reasons.append(f"Assert on local var {var}: {sl} -> {al}")

    if suspicious:
        short_or_suspicious.append((name, reasons, body_lines))

print(f"Total found with strict heuristic: {len(short_or_suspicious)}")
for name, reasons, body in short_or_suspicious:
    print(f"\nMethod: {name}")
    for r in reasons:
        print(f"  Reason: {r}")
