import os
import sys

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"
target_exts = {".cs", ".xaml", ".lsp", ".csproj", ".xml", ".addin", ".ps1"}
exclude_dirs = {"bin", "obj", ".vs", ".git", "TestResults", ".idea"}

prod_violations = []
all_source_violations = []

print("Scanning HPKata source files...")

for root, dirs, files in os.walk(root_dir):
    dirs[:] = [d for d in dirs if d not in exclude_dirs]
    for file in files:
        ext = os.path.splitext(file)[1].lower()
        if ext in target_exts:
            file_path = os.path.join(root, file)
            rel_path = os.path.relpath(file_path, root_dir)
            is_test_or_tool = any(part.endswith(".Tests") or part == "tools" for part in rel_path.split(os.sep))
            
            try:
                with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                    for line_num, line in enumerate(f, 1):
                        if "kata_pro" in line.lower():
                            item = f"{rel_path}:{line_num}: {line.strip()}"
                            all_source_violations.append(item)
                            if not is_test_or_tool:
                                prod_violations.append(item)
            except Exception as e:
                print(f"Error reading {file_path}: {e}")

print(f"Production violations: {len(prod_violations)}")
for v in prod_violations:
    print(f"  PROD: {v}")

print(f"Total source occurrences (including tests/tools): {len(all_source_violations)}")
for v in all_source_violations:
    print(f"  ALL: {v}")

# Check C:\HPKata\Goi lenh.lsp and GoiLenh_HPKata.lsp
for lsp in [r"C:\HPKata\Goi lenh.lsp", r"C:\HPKata\GoiLenh_HPKata.lsp"]:
    if os.path.exists(lsp):
        with open(lsp, "r", encoding="utf-8", errors="ignore") as f:
            for line_num, line in enumerate(f, 1):
                if "kata_pro" in line.lower():
                    print(f"VIOLATION in {lsp}:{line_num}: {line.strip()}")
    else:
        print(f"WARNING: {lsp} not found on disk!")
