import os, re, glob

doc_files = glob.glob(r'docs/rea-dce-pro/*.md')
print(f'Checking {len(doc_files)} files for placeholders and prohibited patterns...')

prohibited_patterns = [
    r'\bTODO\b',
    r'\bTBD\b',
    r'\bFIXME\b',
    r'\bXXX\b',
    r'placeholder',
    r'lorem ipsum',
    r'coming soon',
    r'to be determined',
    r'to be implemented',
    r'dummy'
]

findings = []

for df in doc_files:
    with open(df, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    for idx, line in enumerate(lines, 1):
        for pat in prohibited_patterns:
            if re.search(pat, line, re.IGNORECASE):
                findings.append((df, idx, line.strip(), pat))

print(f'Total occurrences found: {len(findings)}')
for df, idx, line, pat in findings:
    print(f'  {os.path.basename(df)}:{idx} [{pat}] -> {line[:100]}')
