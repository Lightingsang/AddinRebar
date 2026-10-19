import os
import re

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
hpkata_dir = os.path.join(root_dir, "HPKata")

# Production Phase 4 folders:
# HPKata.Core: Foundations, Slabs, Stairs
# HPKata.UI: ViewModels, Views (Foundation, Slab, Stair)
# HPKata.AutoCad: Commands (Foundation, Slab, Stair), Controllers
# HPKata.Revit: Commands, Services, Runners (Foundation, Slab, Stair)

production_dirs = [
    os.path.join(hpkata_dir, "HPKata.Core", "Foundations"),
    os.path.join(hpkata_dir, "HPKata.Core", "Slabs"),
    os.path.join(hpkata_dir, "HPKata.Core", "Stairs"),
    os.path.join(hpkata_dir, "HPKata.UI", "ViewModels"),
    os.path.join(hpkata_dir, "HPKata.UI", "Views"),
    os.path.join(hpkata_dir, "HPKata.AutoCad", "Commands"),
    os.path.join(hpkata_dir, "HPKata.AutoCad", "Controllers"),
    os.path.join(hpkata_dir, "HPKata.Revit", "Commands"),
    os.path.join(hpkata_dir, "HPKata.Revit", "Services"),
    os.path.join(hpkata_dir, "HPKata.Revit", "Runners"),
]

prod_files = []
for pdir in production_dirs:
    if os.path.exists(pdir):
        for root, dirs, files in os.walk(pdir):
            if any(p in root.split(os.sep) for p in ["bin", "obj"]):
                continue
            for f in files:
                if f.endswith(".cs"):
                    # Check if Phase 4 related
                    rel = os.path.relpath(os.path.join(root, f), hpkata_dir)
                    if any(k in f for k in ["Foundation", "Footing", "Pile", "Slab", "Stair", "Lintel", "SectionCut", "GenericSection"]):
                        prod_files.append(os.path.join(root, f))
                    elif any(k in rel for k in ["Foundations", "Slabs", "Stairs"]):
                        prod_files.append(os.path.join(root, f))

print(f"Total Phase 4 production files: {len(prod_files)}")

# Let's inspect methods line by line with brace matching
def get_methods_precise(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    methods = []
    # Identify method signature start
    # Match visibility or return type, name, open paren
    method_start_re = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|virtual|override|abstract|async|sealed|unsafe)\s+'
        r'([\w\<\>\[\],\?\s]+?)\s+([A-Za-z0-9_]+)\s*\('
    )

    i = 0
    while i < len(lines):
        line = lines[i]
        sline = line.strip()

        # Skip comments
        if sline.startswith("//") or sline.startswith("/*") or sline.startswith("*"):
            i += 1
            continue

        # Skip class/interface/struct/enum/record/delegate
        if any(re.search(rf'\b{kw}\b', sline) for kw in ["class", "interface", "struct", "enum", "record", "delegate", "namespace"]):
            i += 1
            continue

        m = method_start_re.search(sline)
        if m:
            ret_type = m.group(1).strip()
            name = m.group(2).strip()

            # Skip property declarations with get/set
            if name in ["get", "set", "init"]:
                i += 1
                continue

            # Start of method signature
            sig_start = i + 1
            
            # Find the opening brace or semicolon or arrow
            curr = i
            found_open_brace = False
            is_expression = False
            has_semicolon = False

            while curr < len(lines) and curr - i < 20:
                cline = lines[curr]
                # Strip string literals and comments for brace checking
                clean_line = re.sub(r'//.*$', '', cline)
                clean_line = re.sub(r'"(?:\\.|[^"\\])*"', '', clean_line)

                if "{" in clean_line:
                    found_open_brace = True
                    break
                if "=>" in clean_line and "{" not in clean_line:
                    is_expression = True
                    break
                if ";" in clean_line:
                    has_semicolon = True
                    break
                curr += 1

            if found_open_brace and not has_semicolon:
                brace_count = 0
                found_first = False
                end_line = curr + 1

                for j in range(curr, len(lines)):
                    clean = re.sub(r'//.*$', '', lines[j])
                    clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
                    for ch in clean:
                        if ch == '{':
                            brace_count += 1
                            found_first = True
                        elif ch == '}':
                            brace_count -= 1
                            if found_first and brace_count == 0:
                                end_line = j + 1
                                break
                    if found_first and brace_count == 0:
                        break

                span = end_line - sig_start + 1
                methods.append((name, sig_start, end_line, span))
                i = end_line - 1
            elif is_expression and not has_semicolon:
                end_line = curr + 1
                while end_line <= len(lines) and ";" not in lines[end_line - 1]:
                    end_line += 1
                span = end_line - sig_start + 1
                methods.append((name, sig_start, end_line, span))
                i = end_line - 1

        i += 1
    return methods

all_methods = []
violations = []
for pf in prod_files:
    rel = os.path.relpath(pf, hpkata_dir)
    ms = get_methods_precise(pf)
    for name, s, e, span in ms:
        all_methods.append((rel, name, s, e, span))
        if span > 50:
            violations.append((rel, name, s, e, span))

all_methods.sort(key=lambda x: -x[4])
print(f"Total methods found in Phase 4 production files: {len(all_methods)}")
print("\n--- Longest Methods in Phase 4 Production Code ---")
for rel, name, s, e, span in all_methods[:15]:
    status = "VIOLATION" if span > 50 else "OK"
    print(f"  [{status}] {span:2d} lines: {rel} -> {name} (lines {s}-{e})")

print(f"\nTotal Violations (> 50 lines): {len(violations)}")
for rel, name, s, e, span in violations:
    print(f"  [VIOLATION] {rel} -> {name}: {span} lines (lines {s}-{e})")
