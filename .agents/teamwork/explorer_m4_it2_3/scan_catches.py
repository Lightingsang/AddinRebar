import os
import re

root = r'HPGrasshopper'
all_catches = []

for dirpath, dirnames, filenames in os.walk(root):
    for f in filenames:
        if f.endswith('.cs'):
            full_path = os.path.join(dirpath, f)
            with open(full_path, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
            for i, line in enumerate(lines):
                if re.search(r'\bcatch\b', line):
                    # extract lines from catch statement to closing brace
                    start = i
                    brace_count = 0
                    found_open = False
                    block_lines = []
                    for j in range(i, min(len(lines), i + 25)):
                        block_lines.append(lines[j])
                        if '{' in lines[j]:
                            brace_count += lines[j].count('{')
                            found_open = True
                        if '}' in lines[j]:
                            brace_count -= lines[j].count('}')
                        if found_open and brace_count == 0:
                            break
                    block_text = ''.join(block_lines)
                    m = re.search(r'\{([\s\S]*)\}', block_text)
                    body = m.group(1).strip() if m else ''
                    has_comment = '//' in body or '/*' in body
                    has_code = bool(re.sub(r'//.*|/\*[\s\S]*?\*/', '', body).strip())
                    all_catches.append({
                        'file': os.path.relpath(full_path, root).replace('\\', '/'),
                        'line': i + 1,
                        'decl': line.strip(),
                        'full_block': block_text.strip(),
                        'body': body.replace('\n', ' '),
                        'has_code': has_code,
                        'has_comment': has_comment,
                        'empty': len(body) == 0,
                        'unannotated': not has_comment and not has_code
                    })

print(f"Total catch blocks found: {len(all_catches)}")
unannotated = [c for c in all_catches if c['unannotated']]
print(f"Unannotated empty catches: {len(unannotated)}")
for u in unannotated:
    print(f"VIOLATION: {u['file']}:{u['line']}: {u['decl']}")

print("\n--- All catch blocks with no code (comment-only) ---")
comment_only = [c for c in all_catches if not c['has_code'] and c['has_comment']]
print(f"Comment-only catches: {len(comment_only)}")
for c in comment_only:
    print(f"COMMENT_ONLY: {c['file']}:{c['line']}: {c['decl']} -> {c['body'][:80]}")

print("\n--- All catch blocks with code ---")
with_code = [c for c in all_catches if c['has_code']]
print(f"Catches with code: {len(with_code)}")
for c in with_code:
    # check if any with_code swallows or does trivial logging
    print(f"WITH_CODE: {c['file']}:{c['line']}: {c['decl']} -> {c['body'][:80]}")
