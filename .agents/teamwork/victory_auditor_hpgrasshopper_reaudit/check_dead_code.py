import os, re

cs_files = []
for r, d, files in os.walk('HPGrasshopper'):
    if 'bin' in r.split(os.sep) or 'obj' in r.split(os.sep):
        continue
    for f in files:
        if f.endswith('.cs'):
            cs_files.append(os.path.join(r, f))

print(f"Checking line comments across {len(cs_files)} files for commented-out dead code (Rule Q-B1)...")

# Patterns indicating C# statements in comments
code_statement_patterns = [
    re.compile(r'^\s*//\s*(?:var|int|string|bool|double|float|long|void|return|throw|new|if|for|foreach|while|using|await)\s+[a-zA-Z0-9_<>]+\s*='),
    re.compile(r'^\s*//\s*(?:return|throw)\s+[^;]+;'),
    re.compile(r'^\s*//\s*[a-zA-Z0-9_.]+\s*\([^)]*\)\s*;'), # function call ending in ;
]

allowlist_keywords = [
    'example', 'sample', 'e.g.', 'todo', 'multi-version', 'tested', 'expected', 
    'simulate', 'rule', 'note', 'see', 'ref:', 'pcc', 'adr'
]

potential_violations = []

for fp in cs_files:
    with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()

    for idx, line in enumerate(lines, 1):
        s = line.strip()
        if not s.startswith('//'):
            continue
        
        # Check if line matches code statement patterns
        for pat in code_statement_patterns:
            if pat.search(line):
                lower = s.lower()
                if any(kw in lower for kw in allowlist_keywords):
                    continue
                potential_violations.append((fp, idx, s))
                break

print(f"Potential dead code instances: {len(potential_violations)}")
for fp, idx, s in potential_violations:
    print(f"  {fp}:{idx} -> {s}")
