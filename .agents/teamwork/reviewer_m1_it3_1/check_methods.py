import re
import sys

def analyze_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    methods = []
    method_sig_pattern = re.compile(
        r'^\s*(?:public|private|internal|protected)\s+(?:static\s+|virtual\s+|override\s+|async\s+)*(?:[\w<>\[\],\.\?]+)\s+([A-Za-z0-9_]+)\s*\('
    )
    
    i = 0
    while i < len(lines):
        line = lines[i]
        clean = re.sub(r'//.*$', '', line)
        m = method_sig_pattern.match(clean)
        if m and m.group(1) not in ['if', 'for', 'foreach', 'while', 'switch']:
            method_name = m.group(1)
            start_line = i + 1
            if '=>' in clean and ';' in clean:
                end_line = i + 1
                methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                i += 1
                continue
            
            depth = 0
            found_open = False
            j = i
            while j < len(lines):
                l_clean = re.sub(r'//.*$', '', lines[j])
                l_clean = re.sub(r'\"(?:[^\"\\]|\\.)*\"', '\"\"', l_clean)
                for char in l_clean:
                    if char == '{':
                        depth += 1
                        found_open = True
                    elif char == '}':
                        depth -= 1
                        if found_open and depth == 0:
                            end_line = j + 1
                            methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                            i = j
                            break
                if found_open and depth == 0:
                    break
                if not found_open and '=>' in l_clean:
                    k = j
                    while k < len(lines):
                        if ';' in lines[k]:
                            end_line = k + 1
                            methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                            i = k
                            break
                        k += 1
                    break
                j += 1
        i += 1
    return methods

if __name__ == '__main__':
    files = [
        ('HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs', 31),
        ('HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs', 34),
        ('HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs', 35),
        ('HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs', 29)
    ]

    all_pass = True
    for filepath, max_target in files:
        res = analyze_methods(filepath)
        print(f'=== {filepath} (Target <= {max_target}) ===')
        for name, s, e, l in res:
            status = 'PASS' if l <= max_target else 'FAIL'
            if status == 'FAIL':
                all_pass = False
            print(f'  {name:30}: L{s:3}-L{e:3} ({l:2} lines) -> {status}')

    if not all_pass:
        print('OVERALL VERDICT: FAIL')
        sys.exit(1)
    else:
        print('OVERALL VERDICT: PASS')
        sys.exit(0)
