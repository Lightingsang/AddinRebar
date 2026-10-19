import os, re, sys

sys.stdout.reconfigure(encoding='utf-8')

src_dir = r'HPDcePro/src'

method_decl_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w<>\[\],]+\s+([A-Z]\w*)\s*\([^;]*\)\s*(?:where[^{]+)?\s*\{?$')

long_methods = []

for root, _, files in os.walk(src_dir):
    for f in files:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
            
            in_method = False
            method_name = ""
            start_line = 0
            brace_depth = 0
            
            for idx, line in enumerate(lines, 1):
                clean = line.split('//')[0].strip()
                if not in_method:
                    m = method_decl_pattern.match(line)
                    if m and not clean.startswith('class ') and not clean.startswith('interface ') and not clean.startswith('namespace '):
                        method_name = m.group(1)
                        start_line = idx
                        in_method = True
                        brace_depth = line.count('{') - line.count('}')
                else:
                    brace_depth += line.count('{') - line.count('}')
                    if brace_depth <= 0:
                        length = idx - start_line + 1
                        if length > 50:
                            long_methods.append((p, method_name, start_line, length))
                        in_method = False

print(f"Total methods exceeding 50 lines: {len(long_methods)}")
for p, m, start, length in long_methods:
    print(f"  {p}:{start} {m}() -> {length} lines")
