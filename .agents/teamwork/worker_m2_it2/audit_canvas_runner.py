import re
import sys

target = r'HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs'

with open(target, 'r', encoding='utf-8') as f:
    lines = f.readlines()

total_lines = len(lines)
print(f"File: {target}")
print(f"Total lines: {total_lines} (Rule C2 <= 300: {'PASS' if total_lines <= 300 else 'FAIL'})")

# Rule Q-B1: Check commented-out code
commented_lines = []
for idx, line in enumerate(lines, 1):
    s = line.strip()
    if s.startswith('//'):
        # Check if it looks like code
        code_indicators = [';', '{', '}', 'var ', 'return ', 'if (', 'throw ']
        if any(ci in s for ci in code_indicators):
            commented_lines.append((idx, s))

print(f"Commented code lines: {len(commented_lines)} (Rule Q-B1: {'PASS' if len(commented_lines) == 0 else 'FAIL'})")
for idx, s in commented_lines:
    print(f"  Line {idx}: {s}")

# Rule Q-B2: Empty catch blocks
empty_catches = []
for idx, line in enumerate(lines, 1):
    s = line.strip()
    if s.startswith('catch') and '{' in s and '}' in s:
        inner = s[s.find('{')+1:s.rfind('}')].strip()
        if not inner:
            empty_catches.append((idx, s))

print(f"Empty catch blocks: {len(empty_catches)} (Rule Q-B2: {'PASS' if len(empty_catches) == 0 else 'FAIL'})")

# Rule M3: Method lengths
methods = []
depth = 0
method_start = None
method_name = None

for idx, line in enumerate(lines, 1):
    s = line.strip()
    clean = re.sub(r'"(?:\\.|[^"\\])*"', '', s)
    clean = re.sub(r'//.*$', '', clean)

    open_b = clean.count('{')
    close_b = clean.count('}')

    if depth == 1 and not method_start:
        if any(s.startswith(k) for k in [
            'public ', 'private ', 'protected ', 'internal ', 'static '
        ]):
            if '(' in s and not any(k in s for k in ['class ', 'struct ', 'record ', 'interface ']):
                method_start = idx
                method_name = s.split('(')[0].split()[-1]

    depth += open_b - close_b

    if method_start and depth == 1 and close_b > 0:
        length = idx - method_start + 1
        methods.append((method_name, method_start, idx, length))
        method_start = None
        method_name = None

print("\n--- Methods (Rule M3 <= 50 lines) ---")
all_methods_pass = True
for name, start, end, count in methods:
    status = "PASS" if count <= 50 else "FAIL"
    if count > 50:
        all_methods_pass = False
    print(f"  {name} (L{start}-L{end}): {count} lines [{status}]")

print(f"\nOverall Method Length Status: {'PASS' if all_methods_pass else 'FAIL'}")

if total_lines <= 300 and all_methods_pass and len(commented_lines) == 0 and len(empty_catches) == 0:
    print("\nALL CLEAN CODE CHECKS PASSED!")
    sys.exit(0)
else:
    print("\nSOME CHECKS FAILED!")
    sys.exit(1)
