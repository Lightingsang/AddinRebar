import os, re, sys

sys.stdout.reconfigure(encoding='utf-8')

unannotated_catches = []
total_catches = 0

for root, _, files in os.walk('HPDcePro'):
    if 'bin' in root or 'obj' in root: continue
    for f in files:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
                for idx, line in enumerate(lines):
                    # Check for catch block
                    m = re.search(r'\bcatch\s*(\([^\)]*\))?\s*\{?', line)
                    if m:
                        total_catches += 1
                        # check next 5 lines for a comment (// Reason: or //)
                        has_comment = False
                        # check same line
                        if '//' in line:
                            has_comment = True
                        else:
                            for offset in range(1, 6):
                                if idx + offset < len(lines):
                                    next_line = lines[idx + offset].strip()
                                    if '//' in next_line:
                                        has_comment = True
                                        break
                                    if next_line == '}':
                                        break
                        if not has_comment:
                            unannotated_catches.append((p, idx + 1, line.strip()))

print(f"Total catch blocks scanned: {total_catches}")
print(f"Unannotated catch blocks: {len(unannotated_catches)}")
for p, l, text in unannotated_catches:
    print(f"  {p}:{l} -> {text}")
