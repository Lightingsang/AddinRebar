import re
import os

opt_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\Optimization"

def check_file(filepath):
    filename = os.path.basename(filepath)
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        content = "".join(lines)
    
    print(f"\n==========================================")
    print(f"File: {filename} ({len(lines)} lines)")
    print(f"==========================================")
    
    # 1. Check C2 (<= 300 lines)
    if len(lines) > 300:
        print(f"FAIL C2: {filename} has {len(lines)} lines > 300")
    else:
        print(f"PASS C2: {len(lines)} <= 300 lines")

    # 2. Check Host References (Rule D1/D4)
    autodesk_refs = re.findall(r'Autodesk\.[A-Za-z0-9_.]+', content)
    if autodesk_refs:
        print(f"FAIL Host Neutrality: Found {autodesk_refs}")
    else:
        print("PASS Host Neutrality: 0 Autodesk references")

    # 3. Check Q-B2 (Unannotated catches)
    catches = re.findall(r'catch\s*(\([^\)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
    for c_sig, c_body in catches:
        cleaned = c_body.strip()
        if not cleaned or not re.search(r'//|/\*', c_body):
            print(f"FAIL Q-B2: Empty or unannotated catch: catch {c_sig} {{ {cleaned} }}")
    if not catches:
        print("PASS Q-B2: 0 catch blocks")

    # 4. Check Q-B1 (Commented out code)
    dead_code = []
    for idx, l in enumerate(lines, 1):
        stripped = l.strip()
        if stripped.startswith("//") and not stripped.startswith("///"):
            comment_text = stripped[2:].strip()
            if re.match(r'^(var|int|double|string|if|for|while|foreach|return|new)\b.*[;{}]', comment_text):
                dead_code.append((idx, stripped))
    if dead_code:
        for idx, dc in dead_code:
            print(f"FAIL Q-B1: Possible commented-out code at line {idx}: {dc}")
    else:
        print("PASS Q-B1: 0 dead/commented-out code detected")

    # 5. Check Method lengths (Rule M3 <= 50 lines)
    # Namespace is depth 1, Class is depth 2, Method is depth 3
    in_method = False
    brace_depth = 0
    method_start = 0
    method_sig = ""
    
    for idx, line in enumerate(lines, 1):
        for char in line:
            if char == '{':
                brace_depth += 1
                if brace_depth == 3 and not in_method:
                    in_method = True
                    method_start = idx
                    # Backtrack up to find signature
                    sig_lines = []
                    for b in range(max(0, idx-5), idx):
                        if lines[b].strip() and not lines[b].strip().startswith("//"):
                            sig_lines.append(lines[b].strip())
                    method_sig = " ".join(sig_lines)
            elif char == '}':
                if brace_depth == 3 and in_method:
                    method_len = idx - method_start + 1
                    status = "PASS" if method_len <= 50 else "FAIL"
                    print(f"  [{status}] Method L{method_start}-L{idx} ({method_len} lines): {method_sig[-60:]}")
                    in_method = False
                brace_depth -= 1

    # 6. Check Facades / Hardcoded values
    suspicious = re.findall(r'(\b150\b|\b2000\b|DEFECT|Scenario)', content)
    print(f"Check Facades / Test keywords: {suspicious if suspicious else 'None (CLEAN)'}")

for fname in os.listdir(opt_dir):
    if fname.endswith(".cs"):
        check_file(os.path.join(opt_dir, fname))
