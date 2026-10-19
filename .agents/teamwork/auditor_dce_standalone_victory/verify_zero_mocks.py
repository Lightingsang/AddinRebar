import os, sys

sys.stdout.reconfigure(encoding='utf-8')

src_dir = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src'

suspicious_terms = ['mock', 'placeholder', 'calculatesamplebeam', 'generatesamplecolumnbuffer', 'fake']

findings = []

for root, dirs, files in os.walk(src_dir):
    for f in files:
        if f.endswith('.cs'):
            full_path = os.path.join(root, f)
            with open(full_path, 'r', encoding='utf-8', errors='ignore') as fp:
                for line_idx, line in enumerate(fp, 1):
                    lower = line.lower()
                    # ignore comments if any or check everything
                    for term in suspicious_terms:
                        if term in lower:
                            rel_path = os.path.relpath(full_path, src_dir)
                            findings.append((rel_path, line_idx, term, line.strip()))

print(f"Total suspicious term hits in HPDcePro/src: {len(findings)}")
for rel, line_num, term, content in findings:
    print(f"  {rel}:{line_num} [{term}]: {content}")
