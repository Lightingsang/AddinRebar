import os
import re

def parse_methods_in_file(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    methods = []
    # Find method bodies
    # A method typically has an access modifier (public, private, protected, internal),
    # a return type, a name, parentheses (...), and then an opening brace '{'.
    # We can track brace depth.
    
    brace_depth = 0
    in_method = False
    current_method_name = ""
    method_start_line = 0
    method_brace_depth = 0
    
    # Simple regex for method declaration:
    method_sig = re.compile(r"^\s*(public|private|protected|internal|static|\s)+[\w\<\>\[\]\?,\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where[^{]+)?$")
    
    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        
        # Check if line looks like method signature before '{'
        if not in_method and ("(" in stripped and ")" in stripped) and not stripped.startswith("//") and not stripped.startswith("/*"):
            # Exclude class, struct, record, interface, enum, delegate, if, for, while, switch, catch
            first_word = stripped.split()[0] if stripped.split() else ""
            if first_word not in ["class", "struct", "record", "interface", "enum", "delegate", "if", "for", "foreach", "while", "switch", "catch", "using", "return"]:
                # Check if it has access modifier
                if any(stripped.startswith(mod) for mod in ["public ", "private ", "protected ", "internal ", "static "]):
                    # could be a method or property
                    # if it ends with ';' it's an abstract/interface method or expression-bodied without braces
                    if not stripped.endswith(";"):
                        current_method_name = stripped
                        method_start_line = i

        for char in line:
            if char == '{':
                brace_depth += 1
                if current_method_name and not in_method:
                    in_method = True
                    method_brace_depth = brace_depth
            elif char == '}':
                if in_method and brace_depth == method_brace_depth:
                    span = i - method_start_line + 1
                    methods.append((current_method_name, method_start_line, i, span))
                    in_method = False
                    current_method_name = ""
                brace_depth -= 1

    return methods

# Scan all files in HPKata.Revit and HPKata.Revit.Tests
targets = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests"
]

m3_files = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Services\RevitElementGeometryReader.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Commands\FramingRebarCommands.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Commands\ColumnRebarCommands.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Commands\WallRebarCommands.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests\Services\RevitElementGeometryReaderTests.cs",
]

print("=== METHOD LENGTH AUDIT (Rule M3 <= 50 lines) ON MILESTONE M3 DELIVERABLES ===")
m3_violations = 0
for f in m3_files:
    rel = os.path.basename(f)
    methods = parse_methods_in_file(f)
    print(f"\n--- {rel} ({len(methods)} methods found) ---")
    for name, start, end, span in methods:
        status = "FAIL (>50)" if span > 50 else "PASS"
        if span > 50:
            m3_violations += 1
        # Truncate method signature for display
        sig_short = (name[:60] + "...") if len(name) > 60 else name
        print(f"  {span:3d} lines | {status:10s} | lines {start:3d}-{end:3d} | {sig_short}")

print(f"\nTotal M3 Deliverable Violations of Rule M3: {m3_violations}")
