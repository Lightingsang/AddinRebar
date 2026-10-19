import os
import re

HPDCEPRO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"

def find_long_methods():
    cs_files = []
    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        if 'bin' in dirs: dirs.remove('bin')
        if 'obj' in dirs: dirs.remove('obj')
        if '.git' in dirs: dirs.remove('.git')
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(root, f))

    long_methods = []

    for path in cs_files:
        rel_path = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()

        in_method = False
        method_name = ""
        method_start = 0
        depth = 0
        method_depth = 0

        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            clean = re.sub(r'".*?"', '', stripped)
            clean = re.sub(r'//.*', '', clean)

            # Heuristic for method signature inside a class (depth >= 2)
            if not in_method and depth >= 2:
                if any(k in stripped for k in ['public ', 'private ', 'protected ', 'internal ']) and '(' in stripped and not stripped.startswith('//'):
                    if not stripped.endswith(';') and not 'record ' in stripped and not 'class ' in stripped and not 'interface ' in stripped:
                        method_name = stripped
                        method_start = idx
                        in_method = True
                        method_depth = depth

            for ch in clean:
                if ch == '{':
                    depth += 1
                elif ch == '}':
                    depth -= 1
                    if in_method and depth == method_depth:
                        length = idx - method_start + 1
                        if length > 50:
                            long_methods.append((rel_path, method_start, idx, length, method_name[:60]))
                        in_method = False

    print(f"Long methods (> 50 lines): {len(long_methods)}")
    for m in long_methods:
        print(f"  {m[0]}:{m[1]}-{m[2]} ({m[3]} lines): {m[4]}")

if __name__ == '__main__':
    find_long_methods()
