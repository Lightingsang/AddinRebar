import zipfile
import glob
import os
import sys
import re

sys.stdout.reconfigure(encoding='utf-8')

dce_base = r"C:\Dce Pro"
xlsm_files = sorted(glob.glob(os.path.join(dce_base, "Excel", "**", "*.xlsm"), recursive=True))

print(f"Inspecting {len(xlsm_files)} XLSM files for hardcoded paths...")

path_re = re.compile(rb'[a-zA-Z]:[\\/][a-zA-Z0-9_.\\/-]+')

findings = []
for p in xlsm_files:
    rel = os.path.relpath(p, dce_base)
    try:
        with zipfile.ZipFile(p, 'r') as z:
            for fname in z.namelist():
                # check xml files and vbaProject.bin
                if fname.endswith('.xml') or fname.endswith('.rels') or fname == 'xl/vbaProject.bin':
                    content = z.read(fname)
                    for m in path_re.finditer(content):
                        match_str = m.group(0).decode('ascii', errors='ignore')
                        if 'dce' in match_str.lower() or 'c:\\' in match_str.lower():
                            findings.append((rel, fname, match_str))
    except Exception as e:
        # print(f"Error reading {rel}: {e}")
        pass

print(f"Total path references found across XLSMs: {len(findings)}")
unique_paths = set(item[2] for item in findings)
print(f"Unique paths ({len(unique_paths)}):")
for u in sorted(unique_paths)[:20]:
    print(f"  * {u}")
