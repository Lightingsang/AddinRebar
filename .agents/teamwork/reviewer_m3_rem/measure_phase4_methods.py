import os
import re

files_to_check = [
    r"HPKata\HPKata.Core\Foundations\Calculators\PileCoordinateGenerator.cs",
    r"HPKata\HPKata.Core.Tests\Stairs\StairRebarCalculatorTests.cs"
]

def analyze_file(filepath):
    print("=" * 80)
    print(f"FILE: {filepath}")
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    print(f"Total physical lines: {len(lines)} (Limit <= 300: {'PASS' if len(lines) <= 300 else 'FAIL'})")
    
    # We will find every method header
    for i, line in enumerate(lines):
        line_num = i + 1
        s = line.strip()
        
        # Check if line contains a method declaration
        # Must have parentheses, not be a control statement, not be class/interface/namespace
        is_candidate = False
        if any(mod in s for mod in ["public", "private", "protected", "internal", "[Fact]"]):
            if "(" in s and not any(kw in s for kw in ["class ", "interface ", "struct ", "enum ", "namespace ", "delegate ", "new "]):
                is_candidate = True
        
        if is_candidate:
            # Find the '{'
            brace_count = 0
            start_line = line_num
            # If line is an attribute [Fact], check next line for method signature
            if s.startswith("["):
                # start_line is line_num
                pass
            
            # Find the opening brace
            curr = i
            found_open = False
            while curr < len(lines):
                if "{" in lines[curr]:
                    found_open = True
                    break
                curr += 1
            
            if found_open:
                # Count braces from opening brace
                end_line = curr + 1
                for j in range(curr, len(lines)):
                    # clean strings/comments
                    cl = re.sub(r'//.*$', '', lines[j])
                    cl = re.sub(r'"(?:\\.|[^"\\])*"', '', cl)
                    for c in cl:
                        if c == '{':
                            brace_count += 1
                        elif c == '}':
                            brace_count -= 1
                            if brace_count == 0:
                                end_line = j + 1
                                break
                    if brace_count == 0 and j >= curr:
                        break
                
                span = end_line - start_line + 1
                status = "PASS" if span <= 50 else "FAIL"
                sig = lines[i].strip()
                if sig.startswith("["):
                    sig += " " + lines[i+1].strip() if i+1 < len(lines) else ""
                print(f"  [{status}] Lines {start_line:>3}-{end_line:>3} ({span:>2} lines): {sig[:65]}")

for f in files_to_check:
    analyze_file(f)
