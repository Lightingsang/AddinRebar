import os
import re
import sys

def check_file_lines(src_dir):
    over_300 = []
    for root, dirs, files in os.walk(src_dir):
        for f in files:
            if f.endswith('.cs'):
                path = os.path.join(root, f)
                with open(path, 'r', encoding='utf-8', errors='ignore') as fp:
                    lines = fp.readlines()
                if len(lines) > 300:
                    over_300.append((os.path.relpath(path, src_dir), len(lines)))
    return over_300

def find_methods_over_50_lines(src_dir):
    # Regex to detect method signature
    method_pattern = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+)+'
        r'(?!class\b|record\b|struct\b|interface\b|enum\b|namespace\b)'
        r'[\w<>\[\],\s\.\?]+\s+([A-Za-z0-9_]+)\s*\([^;{]*\)\s*(?:where\s+[\w\s:<>,]+)?\s*$'
    )
    
    findings = []
    for root, dirs, files in os.walk(src_dir):
        for f in files:
            if not f.endswith('.cs'):
                continue
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
            
            i = 0
            n = len(lines)
            while i < n:
                line = lines[i]
                # Strip comments or raw strings if needed
                m = method_pattern.match(line)
                if m:
                    method_name = m.group(1)
                    start_line = i + 1
                    
                    # Find opening brace
                    j = i
                    brace_found = False
                    while j < n and not brace_found:
                        if '{' in lines[j]:
                            brace_found = True
                            break
                        if ';' in lines[j]: # abstract or interface or expression
                            break
                        j += 1
                    
                    if brace_found:
                        depth = 0
                        k = j
                        end_line = -1
                        while k < n:
                            for ch in lines[k]:
                                if ch == '{':
                                    depth += 1
                                elif ch == '}':
                                    depth -= 1
                                    if depth == 0:
                                        end_line = k + 1
                                        break
                            if depth == 0:
                                break
                            k += 1
                        
                        if end_line != -1:
                            total_lines = end_line - start_line + 1
                            if total_lines > 50:
                                findings.append({
                                    'file': os.path.relpath(path, src_dir),
                                    'method': method_name,
                                    'start': start_line,
                                    'end': end_line,
                                    'lines': total_lines
                                })
                            i = k
                i += 1
    return findings

if __name__ == '__main__':
    src = sys.argv[1] if len(sys.argv) > 1 else 'HPBim/src'
    print(f"Checking C2 (files > 300 lines) in {src}...")
    over_300 = check_file_lines(src)
    for f, count in over_300:
        print(f"  [C2 VIOLATION] {f}: {count} lines")
    if not over_300:
        print("  All files <= 300 lines!")
        
    print(f"\nChecking M3 (methods > 50 lines) in {src}...")
    over_50 = find_methods_over_50_lines(src)
    for m in over_50:
        print(f"  [M3 VIOLATION] {m['file']}::{m['method']}(): {m['lines']} lines (L{m['start']}-L{m['end']})")
    if not over_50:
        print("  All methods <= 50 lines!")
