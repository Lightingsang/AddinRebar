import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"

target_files = []
for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
        continue
    for f in files:
        if f.endswith(".cs"):
            target_files.append(os.path.join(root, f))

def analyze_csharp_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    total_lines = len(lines)
    methods = []
    
    i = 0
    while i < total_lines:
        line = lines[i]
        trimmed = line.strip()
        
        is_type_decl = re.search(r'\b(class|struct|record|interface|enum|namespace)\b', trimmed)
        
        m_sig = re.search(r'^(?:\[.*?\]\s*)*(?:public|private|protected|internal|static|virtual|override|async|sealed|partial|\s)+\s+([\w\<\>\[\],\s\?]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\((.*?)\)', trimmed)
        ctor_sig = re.search(r'^(?:\[.*?\]\s*)*(?:public|private|protected|internal|\s)+\s+([A-Za-z0-9_]+)\s*\((.*?)\)\s*(?::\s*(?:base|this)\(.*?\))?', trimmed)
        
        is_property = re.search(r'\{\s*(?:get|set|init)\b', trimmed)
        
        if (m_sig or ctor_sig) and not is_type_decl and not is_property and not trimmed.startswith('//') and not trimmed.startswith('*'):
            name = m_sig.group(2) if m_sig else ctor_sig.group(1)
            sig_start_line = i + 1
            
            body_start_line = -1
            is_expression_bodied = False
            
            j = i
            while j < min(i + 15, total_lines):
                cur_line = lines[j]
                if '=>' in cur_line:
                    is_expression_bodied = True
                    body_start_line = j
                    break
                elif '{' in cur_line:
                    body_start_line = j
                    break
                j += 1
                
            if body_start_line != -1:
                if is_expression_bodied:
                    k = body_start_line
                    while k < total_lines:
                        if ';' in lines[k]:
                            end_line = k + 1
                            methods.append({
                                'name': name,
                                'start': sig_start_line,
                                'end': end_line,
                                'lines': end_line - sig_start_line + 1,
                                'type': 'expression'
                            })
                            i = k
                            break
                        k += 1
                else:
                    depth = 0
                    k = body_start_line
                    found_open = False
                    while k < total_lines:
                        l = lines[k]
                        for ch in l:
                            if ch == '{':
                                depth += 1
                                found_open = True
                            elif ch == '}':
                                depth -= 1
                                if found_open and depth == 0:
                                    end_line = k + 1
                                    methods.append({
                                        'name': name,
                                        'start': sig_start_line,
                                        'end': end_line,
                                        'lines': end_line - sig_start_line + 1,
                                        'type': 'block'
                                    })
                                    i = k
                                    break
                        if found_open and depth == 0:
                            break
                        k += 1
        i += 1
        
    return total_lines, methods

print("=== METHOD LENGTH AUDIT (RULE M3 <= 50 LINES) ===")
violations_m3 = []
all_collected = []
for tf in sorted(target_files):
    tl, methods = analyze_csharp_methods(tf)
    rel = os.path.relpath(tf, bridge_dir)
    for m in methods:
        all_collected.append((rel, m))
        if m['lines'] > 50:
            violations_m3.append((rel, m))

all_collected.sort(key=lambda x: -x[1]['lines'])
print(f"Total methods analyzed: {len(all_collected)}")
print("\nTop 15 longest methods:")
for rel, m in all_collected[:15]:
    status = "PASS" if m['lines'] <= 50 else "VIOLATION"
    print(f"  [{status}] {rel:<35} -> {m['name']:<30} Lines {m['start']:>3}-{m['end']:>3} ({m['lines']:>2} lines)")

print("\n" + "=" * 80)
if violations_m3:
    print(f"M3 VIOLATIONS DETECTED: {len(violations_m3)}")
    for rel, m in violations_m3:
        print(f"FAIL: {rel} -> {m['name']} (Lines {m['start']}-{m['end']}, {m['lines']} lines)")
else:
    print("M3 AUDIT RESULT: ALL METHODS STRICTLY <= 50 LINES (PASS)")
print("=" * 80)
