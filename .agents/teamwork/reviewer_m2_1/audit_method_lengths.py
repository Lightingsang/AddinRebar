import os
import re

def analyze_method_lengths(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    print(f"\nAnalyzing method lengths in {filepath} (total lines: {len(lines)})")
    
    # Simple brace depth tracker
    in_method = False
    method_name = ""
    method_start = 0
    brace_depth = 0
    class_brace_depth = 0
    
    methods = []
    
    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        
        # Count braces
        open_braces = line.count('{')
        close_braces = line.count('}')
        
        # Check method declaration
        if not in_method:
            # Matches typical method signature: public/private/internal/protected ... ( ... )
            if re.search(r'(public|private|internal|protected|static|async)\s+[\w\<\>\[\]\?]+\s+[\w\<\>]+(\([^\)]*\)|[^\;]*\([^\)]*\))\s*$', stripped) or \
               re.search(r'(public|private|internal|protected)\s+[\w\<\>\[\]\?]+\s+\w+\(', stripped):
                if not stripped.endswith(';') and not 'class ' in stripped and not 'record ' in stripped:
                    in_method = True
                    method_name = stripped
                    method_start = i
                    brace_depth = 0
        
        if in_method:
            brace_depth += open_braces - close_braces
            if open_braces > 0 and brace_depth <= 0:
                length = i - method_start + 1
                methods.append((method_name, method_start, i, length))
                in_method = False
    
    for name, start, end, length in methods:
        status = "PASS" if length <= 50 else "FAIL (>50 lines)"
        print(f"  [{status}] {length:2d} lines (L{start:3d}-L{end:3d}): {name[:50]}")

analyze_method_lengths(r"HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs")
analyze_method_lengths(r"HPBim/src/HPBim.Client/Recovery/ReconcileService.cs")
