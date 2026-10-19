import os
import glob
from verify_m4 import analyze_csharp_members, check_empty_catches, check_commented_dead_code, ROOT_DIR

dirs_to_check = [
    os.path.join(ROOT_DIR, r"HPBim\src\HPBim.Client"),
    os.path.join(ROOT_DIR, r"HPBim\tests\HPBim.Client.Tests"),
]

print("=== SCANNING ALL FILES IN HPBim.Client and HPBim.Client.Tests ===")
violations = []
checked_count = 0

for d in dirs_to_check:
    for root, _, files in os.walk(d):
        for f in files:
            if f.endswith('.cs'):
                checked_count += 1
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, ROOT_DIR)
                with open(full_path, 'r', encoding='utf-8') as fp:
                    content = fp.read()
                
                lines = content.splitlines()
                total_lines = len(lines)
                if total_lines > 300:
                    violations.append((rel_path, f"C2 violation: {total_lines} lines > 300"))
                
                members = analyze_csharp_members(content)
                for m in members:
                    if m[3] > 50:
                        violations.append((rel_path, f"M3 violation: method '{m[0]}' has {m[3]} lines (L{m[1]}-L{m[2]}) > 50"))
                
                catches = check_empty_catches(content)
                if catches:
                    violations.append((rel_path, f"FM violation: empty catch without comment"))

print(f"Total C# files checked: {checked_count}")
if violations:
    print(f"Found {len(violations)} violations:")
    for v in violations:
        print(f"  {v[0]}: {v[1]}")
else:
    print("ALL files in HPBim.Client and HPBim.Client.Tests pass Clean Code limits (C2, M3, FM)!")
