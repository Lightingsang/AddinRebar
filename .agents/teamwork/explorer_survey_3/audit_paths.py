import os
import sys
import glob
import re

sys.stdout.reconfigure(encoding='utf-8')

dce_base = r"C:\Dce Pro"

path_patterns = [
    re.compile(r'C:[/\\]Dce Pro[/\\]?[A-Za-z0-9_.\-\\]*', re.IGNORECASE),
    re.compile(r'C:[/\\][A-Za-z0-9_.\-\\]+', re.IGNORECASE),
]

print("=== AUDIT OF HARDCODED PATHS IN C:\\Dce Pro ===")

# 1. Audit LSP files
lsp_files = glob.glob(os.path.join(dce_base, "**", "*.lsp"), recursive=True) + glob.glob(os.path.join(dce_base, "**", "*.LSP"), recursive=True)
print(f"\n1. LSP Files ({len(lsp_files)} files):")
for p in lsp_files:
    rel = os.path.relpath(p, dce_base)
    try:
        with open(p, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
        dce_matches = []
        for idx, line in enumerate(lines):
            if 'dce pro' in line.lower() or 'c:\\' in line.lower() or 'c:/' in line.lower():
                dce_matches.append((idx + 1, line.strip()))
        print(f"   * {rel} ({len(lines)} lines) -> {len(dce_matches)} path occurrences")
        for lnum, ltext in dce_matches[:5]:
            print(f"       L{lnum}: {ltext[:100]}")
    except Exception as e:
        print(f"   * Error reading {rel}: {e}")

# 2. Audit System config files
sys_files = glob.glob(os.path.join(dce_base, "System", "*.txt"))
print(f"\n2. System TXT Files ({len(sys_files)} files):")
sys_path_matches = []
for p in sys_files:
    rel = os.path.relpath(p, dce_base)
    try:
        with open(p, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read().strip()
        if 'c:\\' in content.lower() or 'c:/' in content.lower() or 'dce' in content.lower():
            sys_path_matches.append((rel, content))
    except Exception as e:
        pass

print(f"   Files containing paths/dce references ({len(sys_path_matches)} files):")
for rel, content in sys_path_matches[:15]:
    print(f"   * {rel:30s} -> Content: {content[:80]}")

# 3. Audit C# code in HPDcePro for any hardcoded 'C:\\Dce Pro'
hpdce_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src"
cs_files = glob.glob(os.path.join(hpdce_dir, "**", "*.cs"), recursive=True)
print(f"\n3. Audit C# files in HPDcePro/src ({len(cs_files)} files) for 'C:\\Dce Pro':")
cs_hardcoded = []
for p in cs_files:
    rel = os.path.relpath(p, hpdce_dir)
    try:
        with open(p, 'r', encoding='utf-8', errors='ignore') as f:
            for idx, line in enumerate(f):
                if 'c:\\dce pro' in line.lower() or 'c:/dce pro' in line.lower():
                    cs_hardcoded.append((rel, idx + 1, line.strip()))
    except Exception as e:
        pass

if cs_hardcoded:
    print(f"   WARNING: Found {len(cs_hardcoded)} hardcoded occurrences in HPDcePro:")
    for rel, lnum, line in cs_hardcoded:
        print(f"   * {rel}:{lnum}: {line[:100]}")
else:
    print("   EXCELLENT: Zero occurrences of hardcoded 'C:\\Dce Pro' in HPDcePro/src/*.cs!")
