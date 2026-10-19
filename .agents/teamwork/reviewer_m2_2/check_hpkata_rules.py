import os
import re

core_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(core_dir):
        if "obj" in root or "bin" in root:
            continue
        for file in files:
            if file.endswith(".cs"):
                cs_files.append(os.path.join(root, file))
    return cs_files

def check_files(cs_files):
    print(f"Total C# files found: {len(cs_files)}")
    c2_violations = []
    autodesk_refs = []
    catch_blocks = []
    dead_code_suspects = []

    for path in sorted(cs_files):
        rel_path = os.path.relpath(path, core_dir)
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        
        line_count = len(lines)
        if line_count > 300:
            c2_violations.append((rel_path, line_count))

        for idx, line in enumerate(lines):
            # check autodesk
            if "Autodesk." in line:
                autodesk_refs.append((rel_path, idx + 1, line.strip()))
            
            # check catch
            m_catch = re.search(r'\bcatch\b(?:\s*\([^)]*\))?\s*\{?', line)
            if m_catch and not line.strip().startswith("//") and not line.strip().startswith("*"):
                # look at following lines for comments
                comment_found = False
                for look in range(max(0, idx - 2), min(len(lines), idx + 6)):
                    if "//" in lines[look] or "/*" in lines[look]:
                        comment_found = True
                        break
                catch_blocks.append((rel_path, idx + 1, line.strip(), comment_found))

            # check commented out code suspects (Q-B1)
            # like // var x = ..., // if (..., // return ..., // ;
            trimmed = line.strip()
            if trimmed.startswith("//"):
                comment_body = trimmed.lstrip("/ ").strip()
                if re.match(r'^(var |int |string |double |bool |if\s*\(|for\s*\(|while\s*\(|return |throw |new |Console\.|\w+\s*=\s*|await )', comment_body):
                    dead_code_suspects.append((rel_path, idx + 1, trimmed))

    print("\n--- Rule C2 (File <= 300 lines) ---")
    if not c2_violations:
        print("PASS: All files <= 300 lines.")
    else:
        for f, cnt in c2_violations:
            print(f"FAIL: {f} has {cnt} lines (> 300)")

    print("\n--- Host Neutrality (No Autodesk.*) ---")
    if not autodesk_refs:
        print("PASS: 0 references to Autodesk.*")
    else:
        for f, l, content in autodesk_refs:
            print(f"FAIL: {f}:{l} -> {content}")

    print("\n--- Rule Q-B2 (Catch blocks with explanation comments) ---")
    for f, l, content, has_comm in catch_blocks:
        status = "ANNOTATED" if has_comm else "UNANNOTATED"
        print(f"[{status}] {f}:{l} -> {content}")

    print("\n--- Rule Q-B1 (Commented-out code suspects) ---")
    if not dead_code_suspects:
        print("PASS: 0 commented-out code suspects.")
    else:
        for f, l, content in dead_code_suspects:
            print(f"SUSPECT: {f}:{l} -> {content}")

    return cs_files

if __name__ == "__main__":
    files = get_cs_files()
    check_files(files)
