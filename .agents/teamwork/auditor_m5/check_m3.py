import os
import re

prod_dirs = ['HPGrasshopper/HPGrasshopper.McpBridge', 'HPGrasshopper/HPGrasshopper.Mcp.Server']
files = [os.path.join(r, f) for d in prod_dirs for r, ds, fs in os.walk(d) for f in fs if f.endswith('.cs') and '\\bin\\' not in r and '\\obj\\' not in r]

method_sig = re.compile(r'^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed)\s+)+(?:[\w\<\>\[\]\?\s,]+)\s+(\w+)\s*\(')
m3_bad = []

for f in files:
    lines = open(f, encoding='utf-8').readlines()
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # skip class, struct, record, interface, delegate
        if any(kw in line for kw in ['class ', 'struct ', 'record ', 'interface ', 'delegate ']):
            i += 1
            continue
        m = method_sig.match(line)
        if m and not line.endswith(';'):
            name = m.group(1)
            start_line = i
            brace_count = 0
            found_open = False
            curr = i
            while curr < len(lines):
                cl = re.sub(r'"(?:\\.|[^"\\])*"', '', lines[curr])
                cl = re.sub(r'//.*', '', cl)
                brace_count += cl.count('{') - cl.count('}')
                if '{' in cl:
                    found_open = True
                if found_open and brace_count == 0:
                    method_len = curr - start_line + 1
                    if method_len > 50:
                        m3_bad.append((f, name, start_line + 1, method_len))
                    break
                curr += 1
            i = curr
        i += 1

print('Total M3 violations (> 50 lines) in production code:', len(m3_bad))
for f, n, ln, l in m3_bad:
    print(f'  {f}:{ln} {n}() -> {l} lines')
