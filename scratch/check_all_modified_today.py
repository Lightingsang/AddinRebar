import os
import re
from check_clean_code_rules import analyze_csharp, all_modified_today, base_dir

print("=================================================================")
print("=== DETAILED STATIC ANALYSIS ON ALL FILES MODIFIED TODAY ===")
print("=================================================================")
for rel_path in all_modified_today:
    full_path = os.path.join(base_dir, rel_path)
    if not os.path.exists(full_path):
        continue
    total_lines, long_methods, dead_code, unannotated_catches = analyze_csharp(full_path)
    print(f"\nFile: {rel_path} ({total_lines} lines)")
    if total_lines > 300:
        print(f"  [C2 VIOLATION] File exceeds 300 lines ({total_lines} lines)")
    else:
        print(f"  [C2 PASS] {total_lines} <= 300")
        
    if long_methods:
        print(f"  [M3 VIOLATION] Methods > 50 lines:")
        for m_name, s, e, l in long_methods:
            print(f"    - {m_name}: lines {s}-{e} ({l} lines)")
    else:
        print("  [M3 PASS] No methods > 50 lines")
    
    if dead_code:
        print(f"  [Q-B1 VIOLATION] Commented-out code detected:")
        for l_num, text in dead_code:
            print(f"    - Line {l_num}: {text}")
    else:
        print("  [Q-B1 PASS] 0 commented-out code")
        
    if unannotated_catches:
        print(f"  [Q-B2 VIOLATION] Unannotated / silent catch blocks:")
        for l_num, desc in unannotated_catches:
            print(f"    - Line {l_num}: {desc}")
    else:
        print("  [Q-B2 PASS] 0 unannotated catch blocks")
