import sys, os

sys.stdout.reconfigure(encoding='utf-8')

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'
truth_matrix_path = os.path.join(repo_root, r'docs\rea-dce-pro\TRUTH_MATRIX.md')

with open(truth_matrix_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

missing_files = []
existing_files = set()
ht_count = 0

for idx, line in enumerate(lines, 1):
    if '|' in line and not line.strip().startswith('|---') and not 'Legacy Identifier' in line and not 'Subsystem Index' in line and not 'Export Symbol' in line and not 'Form / Dialog / Palette' in line and not 'Step Index' in line:
        parts = [p.strip() for p in line.split('|')[1:-1]]
        if len(parts) >= 4:
            status = parts[-1]
            if status == 'HOÀN THÀNH':
                ht_count += 1
                # Target file is usually 2nd to last column
                target_col = parts[-2]
                # extract file path from backticks if present
                if '`' in target_col:
                    file_ref = target_col.split('`')[1]
                else:
                    file_ref = target_col
                # strip line number like :15 or :45
                file_path = file_ref.split(':')[0].strip()
                # Resolve relative to HPDcePro
                full_path = os.path.join(repo_root, 'HPDcePro', file_path.replace('/', os.sep))
                if os.path.isfile(full_path):
                    existing_files.add(full_path)
                else:
                    missing_files.append((idx, parts[0], file_ref, full_path))

print(f"Total HOÀN THÀNH endpoints: {ht_count}")
print(f"Distinct physical files verified on disk: {len(existing_files)}")
print(f"Missing files count: {len(missing_files)}")

if missing_files:
    print("FAILED! Missing files found:")
    for idx, name, ref, p in missing_files:
        print(f"  Line {idx}: {name} -> {ref} ({p})")
else:
    print("PASS: 100% of HOÀN THÀNH endpoints have physical target files on disk!")
