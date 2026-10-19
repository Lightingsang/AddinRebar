import os
import re
from pathlib import Path

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        if any(part in ('bin', 'obj', '.git', '.vs') for part in Path(root).parts):
            continue
        for f in files:
            if f.endswith('.cs') and not f.endswith('.g.cs') and not f.endswith('.Designer.cs') and not f.endswith('AssemblyInfo.cs'):
                cs_files.append(os.path.join(root, f))
    return cs_files

def find_all_methods(cs_files):
    results = []
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            lines = fp.readlines()
            
        n = len(lines)
        i = 0
        while i < n:
            line = lines[i]
            clean = re.sub(r'//.*', '', line).strip()
            
            # Check if this could be the start of a method
            mods = ('public ', 'private ', 'protected ', 'internal ', 'static ', 'async ', 'virtual ', 'override ', 'sealed ')
            if any(clean.startswith(m) for m in mods) and not any(kw in clean for kw in ('class ', 'interface ', 'struct ', 'enum ', 'record ', 'delegate ', 'event ')):
                # Accumulate until { or ; or =>
                sig_lines = [clean]
                j = i + 1
                while j < n and '{' not in sig_lines[-1] and ';' not in sig_lines[-1] and '=>' not in sig_lines[-1] and j < i + 10:
                    sig_lines.append(re.sub(r'//.*', '', lines[j]).strip())
                    j += 1
                
                full_sig = " ".join(sig_lines)
                if '(' in full_sig and ')' in full_sig and not full_sig.strip().endswith(';') and '=>' not in full_sig:
                    # Look for {
                    k = i
                    brace_pos = -1
                    while k < n and k < i + 10:
                        if '{' in re.sub(r'//.*', '', lines[k]):
                            brace_pos = k
                            break
                        k += 1
                        
                    if brace_pos != -1:
                        # Find matching closing brace
                        brace_count = 0
                        opened = False
                        end_line = -1
                        idx = brace_pos
                        while idx < n:
                            clean_k = re.sub(r'"(\\.|[^"\\])*"', '""', lines[idx])
                            clean_k = re.sub(r"'(\\.|[^'\\])*'", "''", clean_k)
                            clean_k = re.sub(r'//.*', '', clean_k)
                            for c in clean_k:
                                if c == '{':
                                    brace_count += 1
                                    opened = True
                                elif c == '}':
                                    brace_count -= 1
                                    if opened and brace_count == 0:
                                        end_line = idx + 1
                                        break
                            if end_line != -1:
                                break
                            idx += 1
                            
                        if end_line != -1:
                            start_line = i + 1
                            span = end_line - start_line + 1
                            if span > 50:
                                results.append({
                                    'file': f,
                                    'start': start_line,
                                    'end': end_line,
                                    'lines': span,
                                    'sig': full_sig[:60]
                                })
                            i = end_line
                            continue
            i += 1
    return results

def main():
    cs_files = get_cs_files("HPDcePro")
    methods = find_all_methods(cs_files)
    print(f"Total methods > 50 lines: {len(methods)}")
    for m in methods:
        print(f"{Path(m['file']).name}:{m['start']}-{m['end']} ({m['lines']} lines) -> {m['sig']}")

if __name__ == '__main__':
    main()
