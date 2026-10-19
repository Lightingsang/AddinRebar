import os
import re

files_to_audit = [
    r"HPBim\src\HPBim.Core\Coordination\CompositeSourceKeyJsonConverter.cs",
    r"HPBim\src\HPBim.Core\Coordination\CompositeSourceKey.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.Mappings.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerSerializer.cs",
    r"HPBim\src\HPBim.Core\Execution\ElementExecutionStatus.cs",
    r"HPBim\src\HPBim.Core\Execution\ElementExecutionRecord.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Columns.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.MepAndRebar.cs",
    r"HPBim\src\HPBim.Client\ScriptGenerators\Stage7MepGenerator.cs",
    r"HPBim\src\HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs",
    r"HPBim\src\HPBim.Client\TypeResolver\TypeResolutionRules.cs",
    r"HPBim\src\HPBim.Client\Pipeline\ExecutionPlanBuilder.ArchitectureAndMep.cs"
]

repo_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("=== METHOD LINE COUNT AUDIT (PRODUCTION CODE) ===")

def analyze_file(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    methods = []
    # Simple brace-counting method extractor
    in_method = False
    brace_depth = 0
    method_name = ""
    start_line = 0
    
    method_sig_pattern = re.compile(r"^\s*(public|private|protected|internal|static|async|override|sealed|\s)*\s+[A-Za-z0-9_<>?,\[\]]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)")
    
    for idx, line in enumerate(lines):
        clean = line.strip()
        if not in_method:
            match = method_sig_pattern.match(line)
            # ignore class, struct, record, interface, enum declarations
            if match and not any(k in clean for k in ["class ", "record ", "struct ", "interface ", "enum ", "namespace "]):
                method_name = match.group(2)
                start_line = idx + 1
                brace_depth = line.count("{") - line.count("}")
                if "=>" in line and ";" in line:
                    # expression-bodied single line method
                    methods.append((method_name, start_line, 1))
                elif brace_depth > 0:
                    in_method = True
        else:
            brace_depth += line.count("{") - line.count("}")
            if brace_depth <= 0:
                end_line = idx + 1
                body_lines = end_line - start_line + 1
                methods.append((method_name, start_line, body_lines))
                in_method = False
                
    return methods

for rel_path in files_to_audit:
    full_path = os.path.join(repo_root, rel_path)
    if not os.path.exists(full_path):
        continue
    methods = analyze_file(full_path)
    long_methods = [m for m in methods if m[2] > 50]
    print(f"\n{os.path.basename(rel_path)}: {len(methods)} methods found")
    if long_methods:
        for name, start, length in long_methods:
            print(f"  [>50 lines] {name} at line {start} ({length} lines)")
    else:
        print("  All methods <= 50 lines.")

