import os
import sys
import glob
import re

sys.stdout.reconfigure(encoding='utf-8')

dce_dir = r"C:\Dce Pro"

print("--- Searching for Revit occurrences in C:\Dce Pro ---")
matches = []
for root, dirs, files in os.walk(dce_dir):
    for f in files:
        fpath = os.path.join(root, f)
        ext = os.path.splitext(f)[1].lower()
        if ext in ['.txt', '.xml', '.json', '.lsp', '.config', '.md', '.bat']:
            try:
                with open(fpath, 'r', encoding='utf-8', errors='ignore') as fp:
                    content = fp.read()
                    if 'revit' in content.lower() or 'dbim_rvt' in content.lower():
                        matches.append(fpath)
            except Exception as e:
                pass

print(f"Found {len(matches)} text/config files mentioning Revit:")
for m in matches[:30]:
    print(" ", m)
