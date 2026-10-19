import os
import re

files_to_check = [
    r'HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs',
    r'HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs',
    r'HPKata/HPKata.AutoCad/Controllers/DimensionUtilityController.cs',
    r'HPKata/HPKata.AutoCad/Controllers/QsTakeoffController.cs',
    r'HPKata/HPKata.Core/QuantitySurveying/Calculators/QsTakeoffCalculator.cs',
    r'HPKata/HPKata.Core/QuantitySurveying/Calculators/QsFormworkCalculator.cs',
    r'HPKata/HPKata.Core/DrawingLayout/Calculators/SheetArrangerCalculator.cs',
    r'HPKata/HPKata.Core.Tests/DrawingLayout/DrawingLayoutStressChallengeTests.cs',
    r'HPKata/HPKata.Core.Tests/QuantitySurveying/QsTakeoffCalculatorTests.cs',
    r'HPKata/HPKata.Core.Tests/QuantitySurveying/QsFormworkCalculatorTests.cs',
    r'HPKata/HPKata.AutoCad.Tests/Controllers/Phase5AutoCadTests.cs'
]

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'

# Method extraction using brace matching
def analyze_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    methods = []
    # State machine
    # We find method headers, then track { and } until matching }
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        trimmed = line.strip()

        # Check for method signature: access modifier or return type
        is_candidate = False
        if re.match(r'^\s*(public|private|protected|internal|static|async|virtual|override|sealed)', line):
            # Exclude class, struct, record, interface, enum, namespace, property getter/setter
            if not re.search(r'\b(class|struct|interface|enum|namespace)\b', line):
                if '(' in line and not line.strip().endswith(';'):
                    is_candidate = True

        if is_candidate:
            start_line = i + 1
            header = trimmed
            # Find the opening brace
            brace_count = 0
            found_open = False
            j = i
            while j < n:
                l = lines[j]
                open_b = l.count('{')
                close_b = l.count('}')
                if open_b > 0 and not found_open:
                    found_open = True
                if found_open:
                    brace_count += open_b - close_b
                    if brace_count == 0:
                        end_line = j + 1
                        total_lines = end_line - start_line + 1
                        # extract method name
                        sig_match = re.search(r'([A-Za-z0-9_<>]+)\s*\(', header)
                        name = sig_match.group(1) if sig_match else header
                        methods.append((name, start_line, end_line, total_lines))
                        i = j
                        break
                j += 1
        i += 1
    return methods

all_methods_pass = True
for rel in files_to_check:
    full_path = os.path.join(repo_root, rel)
    methods = analyze_methods(full_path)
    print(f"\n--- {rel} ({len(methods)} methods found) ---")
    for name, s, e, length in methods:
        status = "PASS" if length <= 50 else "FAIL"
        if length > 50:
            all_methods_pass = False
        print(f"  {name:<40} lines {s:3d}-{e:3d} ({length:2d} lines) [{status}]")

print(f"\nOverall M3 status: {'PASS' if all_methods_pass else 'FAIL'}")
