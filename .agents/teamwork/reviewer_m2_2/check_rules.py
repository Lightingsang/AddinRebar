import os
import re

files = [
    r"McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs",
    r"HPRebar/HPRebar.McpBridge/Service/AutoDismissFailurePreprocessor.cs",
    r"HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs",
    r"HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs",
    r"HPBim/src/HPBim.Client/Pipeline/StageResult.cs",
    r"HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
    r"HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs",
    r"HPBim/src/HPBim.Core/Qc/FoundationAuditModels.cs",
    r"HPBim/src/HPBim.Core/Qc/FoundationLedgerAuditor.cs",
    r"HPBim/src/HPBim.Core/Qc/FoundationGridLevelValidator.cs",
    r"HPBim/src/HPBim.Core/Qc/FoundationQcEngine.cs",
    r"HPBim/src/HPBim.Core/Verification/GeometricVerificationStatus.cs",
    r"HPBim/src/HPBim.Core/Verification/ActualElementGeometry.cs",
    r"HPBim/src/HPBim.Core/Verification/ElementGeometricAudit.cs",
    r"HPBim/src/HPBim.Core/Verification/GeometricAuditReport.cs",
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs",
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs",
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs",
    r"HPBim/src/HPBim.Client/Verification/IMeasuredGeometricVerifier.cs",
    r"HPBim/src/HPBim.Client/Verification/MeasuredGeometryJsonParser.cs",
    r"HPBim/src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs",
    r"HPBim/src/HPBim.Client/Verification/MeasuredGeometricVerifier.cs",
    r"HPBim/tests/HPBim.Client.Tests/Pipeline/DryRunRollbackHonestyTests.cs",
    r"HPBim/tests/HPBim.Core.Tests/FoundationQcEngineTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Verification/MeasuredGeometricVerifierTests.cs",
    r"HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricEngineTests.cs",
    r"HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricToleranceTests.cs"
]

def analyze_file(filepath):
    full_path = os.path.normpath(os.path.join(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar", filepath))
    with open(full_path, "r", encoding="utf-8") as f:
        text = f.read()
        lines = text.splitlines()

    # Track methods using bracket nesting
    methods = []
    stack = []
    depth = 0
    in_single_line_comment = False
    in_multi_line_comment = False
    in_string = False
    in_verbatim = False
    in_char = False

    for line_idx, line in enumerate(lines):
        col = 0
        while col < len(line):
            c = line[col]
            if in_multi_line_comment:
                if c == '*' and col + 1 < len(line) and line[col+1] == '/':
                    in_multi_line_comment = False
                    col += 2
                    continue
                col += 1
                continue
            if in_string:
                if c == '\\':
                    col += 2
                    continue
                if c == '"':
                    in_string = False
                col += 1
                continue
            if in_verbatim:
                if c == '"':
                    if col + 1 < len(line) and line[col+1] == '"':
                        col += 2
                        continue
                    in_verbatim = False
                col += 1
                continue
            if in_char:
                if c == '\\':
                    col += 2
                    continue
                if c == "'":
                    in_char = False
                col += 1
                continue

            if c == '/' and col + 1 < len(line):
                if line[col+1] == '/':
                    break
                elif line[col+1] == '*':
                    in_multi_line_comment = True
                    col += 2
                    continue
            if c == '@' and col + 1 < len(line) and line[col+1] == '"':
                in_verbatim = True
                col += 2
                continue
            if c == '$' and col + 1 < len(line) and line[col+1] == '"':
                in_string = True
                col += 2
                continue
            if c == '"':
                in_string = True
                col += 1
                continue
            if c == "'":
                in_char = True
                col += 1
                continue

            if c == '{':
                depth += 1
                # Check signature
                sig_lines = []
                for k in range(max(0, line_idx - 6), line_idx + 1):
                    sig_lines.append(lines[k].strip())
                sig_text = ' '.join(sig_lines).split('{')[0]

                # Check if this block looks like a method or constructor
                is_type = re.search(r'\b(class|struct|interface|enum|namespace|record)\b', sig_text)
                is_flow = re.search(r'\b(if|else|for|foreach|while|do|switch|try|catch|finally|lock|using)\b\s*(\(|{|$)', sig_text)
                is_prop_acc = re.search(r'\b(get|set|init)\b\s*$', sig_text.strip())

                method_match = None
                if not is_type and not is_flow and not is_prop_acc:
                    # Look for method pattern: modifiers return_type MethodName(args)
                    m = re.search(r'([A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)', sig_text)
                    if m and m.group(2) not in ['if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'lock']:
                        method_match = m.group(2)
                    else:
                        # constructor pattern
                        m2 = re.search(r'([A-Za-z0-9_]+)\s*\([^\)]*\)(\s*:\s*(base|this)\([^\)]*\))?', sig_text)
                        if m2 and m2.group(1) not in ['if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'lock', 'typeof', 'sizeof', 'nameof']:
                            method_match = m2.group(1)

                if method_match:
                    start_l = line_idx + 1
                    for k in range(line_idx, max(-1, line_idx - 6), -1):
                        stripped = lines[k].strip()
                        if any(kw in stripped for kw in ['public', 'private', 'protected', 'internal', 'static', 'async', 'override', 'virtual', '[Fact]', '[Theory]', 'void', 'Task']):
                            start_l = k + 1
                            break
                    stack.append((method_match, start_l, depth))
                else:
                    stack.append((None, line_idx + 1, depth))
            elif c == '}':
                if stack:
                    name, start_l, open_d = stack.pop()
                    if name is not None and open_d == depth:
                        length = (line_idx + 1) - start_l + 1
                        methods.append((name, filepath, start_l, line_idx + 1, length))
                depth -= 1
            col += 1

    return methods, len(lines)

def check_empty_catch_and_commented_code(filepath):
    full_path = os.path.normpath(os.path.join(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar", filepath))
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    empty_catches = []
    commented_code = []

    for idx, line in enumerate(lines):
        # Empty catch check: catch (...) { }
        if re.search(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', line):
            empty_catches.append((idx + 1, line.strip()))
        # Also multi-line catch with only whitespace inside
        if 'catch' in line and '{' in line:
            # check next lines
            pass
        # Commented code heuristics: // if (, // var , // return , // {
        stripped = line.strip()
        if stripped.startswith('//') and not stripped.startswith('///'):
            content = stripped[2:].strip()
            if re.match(r'^(var|return|if\s*\(|for\s*\(|foreach\s*\(|public|private|throw|new\s+[A-Z])', content):
                commented_code.append((idx + 1, stripped))

    return empty_catches, commented_code

all_methods = []
files_over_300 = []
methods_over_50 = []
all_empty_catches = []
all_commented_code = []

for f in files:
    m_list, line_count = analyze_file(f)
    all_methods.extend(m_list)
    if line_count > 300:
        files_over_300.append((f, line_count))
    ec, cc = check_empty_catch_and_commented_code(f)
    if ec:
        all_empty_catches.append((f, ec))
    if cc:
        all_commented_code.append((f, cc))

for m in all_methods:
    if m[4] > 50:
        methods_over_50.append(m)

print(f"=== RULE C2 ANALYSIS (<= 300 lines) ===")
print(f"Files over 300 lines: {len(files_over_300)}")
for f, lc in files_over_300:
    print(f"  FAIL: {f} has {lc} lines")
if not files_over_300:
    print("  PASS: All 27 files are <= 300 lines.")

print(f"\n=== RULE M3 ANALYSIS (<= 50 lines per method) ===")
print(f"Total methods analyzed: {len(all_methods)}")
print(f"Methods over 50 lines: {len(methods_over_50)}")
for m in sorted(all_methods, key=lambda x: x[4], reverse=True)[:15]:
    print(f"  {m[4]:2d} lines | {m[0]:30} | {os.path.basename(m[1])}:{m[2]}-{m[3]}")

if methods_over_50:
    print("\nVIOLATIONS FOUND FOR RULE M3:")
    for m in methods_over_50:
        print(f"  FAIL: {m[1]}:{m[2]} {m[0]} is {m[4]} lines (>50)")
else:
    print("  PASS: All methods in all 27 files are <= 50 lines.")

print(f"\n=== EMPTY CATCHES ===")
print(f"Empty catches found: {len(all_empty_catches)}")
for f, ec in all_empty_catches:
    print(f"  {f}: {ec}")

print(f"\n=== COMMENTED OUT CODE ===")
print(f"Commented out code found: {len(all_commented_code)}")
for f, cc in all_commented_code:
    print(f"  {f}: {cc}")
