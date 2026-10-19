import os
import re
import subprocess

TARGET_FILES = [
    # Modified files
    r"HPRebar\HPRebar\BeamRebar\Service\RebarCreationService.cs",
    r"HPRebar\HPRebar\BeamRebar\View\BeamRebarView.xaml",
    r"HPRebar\HPRebar\BeamRebar\ViewModel\BeamRebarViewModel.cs",
    # Created files in BeamRebar
    r"HPRebar\HPRebar\BeamRebar\Service\BeamCalculationReportExporter.cs",
    r"HPRebar\HPRebar\BeamRebar\Service\ClosedXmlBeamForceReader.cs",
    r"HPRebar\HPRebar\BeamRebar\Service\EtabsBeamForceReader.cs",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DConcreteMeshBuilder.cs",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DMaterials.cs",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DRebarMeshBuilder.cs",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DSceneBuilder.cs",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DViewport.xaml",
    r"HPRebar\HPRebar\BeamRebar\View\Controls\Beam3DViewport.xaml.cs",
]

# Add all files in HPRebar.Core/Tcvn5574
tcvn_dir = r"HPRebar\HPRebar.Core\Tcvn5574"
for root, dirs, files in os.walk(tcvn_dir):
    for f in files:
        if f.endswith(".cs"):
            TARGET_FILES.append(os.path.join(root, f))

print(f"Target files to audit: {len(TARGET_FILES)}")

file_violations = []
method_violations = []
catch_violations = []

def parse_methods(code_lines):
    # Heuristic method parser for C#
    methods = []
    # Match typical C# method declaration: [access_modifier] [static/virtual/override/async] ReturnType MethodName(args)
    # followed by { or =>
    method_sig_pattern = re.compile(
        r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|\s)+\s+[\w\<\>\[\],\s\?]+\s+(\w+)\s*\([^;]*\)\s*$'
    )
    
    in_method = False
    current_method_name = ""
    current_method_start = 0
    brace_depth = 0
    
    for i, line in enumerate(code_lines):
        line_num = i + 1
        stripped = line.strip()
        
        # Simple depth tracking (ignoring strings/comments for general sanity)
        # Check method start
        if not in_method:
            if method_sig_pattern.match(stripped) and not stripped.endswith(";"):
                current_method_name = line.strip()
                current_method_start = line_num
                # peek next line for opening brace if not on this line
                if "{" in stripped:
                    in_method = True
                    brace_depth = line.count("{") - line.count("}")
                    if brace_depth == 0:
                        in_method = False
                continue
            elif current_method_name and "{" in stripped:
                in_method = True
                brace_depth = line.count("{") - line.count("}")
                if brace_depth == 0:
                    length = line_num - current_method_start + 1
                    methods.append((current_method_name, current_method_start, line_num, length))
                    in_method = False
                    current_method_name = ""
                continue
            else:
                current_method_name = ""
        else:
            brace_depth += line.count("{") - line.count("}")
            if brace_depth <= 0:
                length = line_num - current_method_start + 1
                methods.append((current_method_name, current_method_start, line_num, length))
                in_method = False
                current_method_name = ""
                brace_depth = 0
                
    return methods

for rel_path in TARGET_FILES:
    if not os.path.exists(rel_path):
        print(f"WARNING: File not found: {rel_path}")
        continue
    
    with open(rel_path, "r", encoding="utf-8", errors="replace") as f:
        lines = f.readlines()
        
    line_count = len(lines)
    print(f"[{line_count:3d} lines] {rel_path}")
    if line_count > 300:
        file_violations.append((rel_path, line_count))
        
    if rel_path.endswith(".cs"):
        # Check empty catches
        content = "".join(lines)
        # Empty catch pattern: catch (...) { } or catch { }
        # Match catch followed by block
        empty_catch_matches = re.finditer(r'catch\s*(\([^\)]*\))?\s*\{([^\}]*)\}', content)
        for m in empty_catch_matches:
            body = m.group(2).strip()
            # If body has no non-whitespace non-comment code
            body_code = re.sub(r'//.*', '', body)
            body_code = re.sub(r'/\*.*?\*/', '', body_code, flags=re.DOTALL).strip()
            if not body_code:
                # check if there is a comment explaining why
                if not body:
                    # completely empty
                    catch_violations.append((rel_path, m.group(0)))
                else:
                    # has comment
                    pass
                    
        # Check method lengths
        methods = parse_methods(lines)
        for m_sig, start, end, length in methods:
            if length > 50:
                method_violations.append((rel_path, m_sig, start, end, length))

print("\n" + "="*60)
print("AUDIT RESULTS SUMMARY:")
print("="*60)
print(f"Total files audited: {len(TARGET_FILES)}")
print(f"File line count violations (>300 lines): {len(file_violations)}")
for f, lc in file_violations:
    print(f"  - {f}: {lc} lines")

print(f"Method line count violations (>50 lines): {len(method_violations)}")
for f, sig, s, e, l in method_violations:
    print(f"  - {f} (lines {s}-{e}, len={l}): {sig}")

print(f"Empty catch block violations: {len(catch_violations)}")
for f, c in catch_violations:
    print(f"  - {f}: {c}")

if not file_violations and not method_violations and not catch_violations:
    print("\nALL CLEAN CODE METRICS STRICTLY COMPLIANT! (PASS)")
else:
    print("\nFAILURES DETECTED!")
