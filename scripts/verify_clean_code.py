import os
import re
import sys

def check_clean_code():
    base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"
    folders = [
        os.path.join(base_dir, "HPKata.AutoCad"),
        os.path.join(base_dir, "HPKata.AutoCad.Tests")
    ]

    violations = []
    file_count = 0

    for folder in folders:
        for root, dirs, files in os.walk(folder):
            if "bin" in root or "obj" in root:
                continue
            for f in files:
                if f.endswith(".cs") or f.endswith(".lsp"):
                    file_count += 1
                    file_path = os.path.join(root, f)
                    with open(file_path, "r", encoding="utf-8", errors="ignore") as fp:
                        lines = fp.readlines()

                    # Rule C2: File <= 300 lines (GoiLenh_HPKata.lsp is 238 lines)
                    if len(lines) > 300:
                        violations.append((f, "C2", f"File has {len(lines)} lines > 300 lines"))

                    # Check for Security.dll references
                    content = "".join(lines)
                    if "Security.dll" in content or "SoftKey" in content or "HardKey" in content:
                        violations.append((f, "CleanRoom", "Contains reference to Security/licensing"))

                    # Check empty catch blocks without comment
                    for i, l in enumerate(lines):
                        stripped = l.strip()
                        if re.match(r"^catch\s*(\([^\)]*\))?\s*\{\s*\}", stripped):
                            violations.append((f, "Q-B2", f"Line {i+1}: Empty catch block without comment"))

    print(f"Total files checked: {file_count}")
    if violations:
        print(f"FAIL: Found {len(violations)} Clean Code violations:")
        for f, rule, desc in violations:
            print(f"  [{rule}] {f}: {desc}")
        return False
    else:
        print("PASS: Zero Clean Code violations across all files in HPKata.AutoCad and HPKata.AutoCad.Tests!")
        return True

if __name__ == "__main__":
    ok = check_clean_code()
    sys.exit(0 if ok else 1)
