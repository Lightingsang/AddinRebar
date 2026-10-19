import os
import re

cs_files = [
    r'McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs',
    r'McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs',
    r'McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs',
    r'McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs',
    r'McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs',
    r'McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs',
    r'McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs',
    r'McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs',
    r'McpShared/HPRebar.Mcp.Server.Core.Tests/DynamoProfileTests.cs',
    r'McpShared/HPRebar.Mcp.Server.Core.Tests/DynamoTestProfile.cs',
    r'McpShared/HPRebar.Mcp.Server.Core.Tests/ContextServiceTests.cs',
    r'McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs',
    r'McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs',
]

def analyze_file(filepath):
    if not os.path.exists(filepath):
        print(f"FILE MISSING: {filepath}")
        return
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    total_lines = len(lines)
    print(f"\n--- {filepath} ({total_lines} lines) ---")
    if total_lines > 300:
        print(f"VIOLATION: File exceeds 300 lines: {total_lines}")
    
    # Check method line counts
    in_method = False
    method_name = ""
    start_line = 0
    brace_depth = 0
    method_brace_depth = 0
    
    for idx, line in enumerate(lines, 1):
        clean_line = re.sub(r'\"(\\.|[^\"])*\"', '', line)
        clean_line = re.sub(r'//.*', '', clean_line)
        
        # Check method start
        if not in_method:
            m = re.search(r'(public|private|protected|internal|static|async)\s+[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^;]*\)', line)
            if m and not line.strip().endswith(';'):
                method_name = m.group(2)
                start_line = idx

        for ch in clean_line:
            if ch == '{':
                brace_depth += 1
                if method_name and not in_method:
                    in_method = True
                    method_brace_depth = brace_depth
            elif ch == '}':
                if in_method and brace_depth == method_brace_depth:
                    method_len = idx - start_line + 1
                    if method_len > 50:
                        print(f"VIOLATION: Method > 50 lines in {filepath}:{start_line} - {method_name}() has {method_len} lines")
                    else:
                        print(f"  OK: {method_name}() = {method_len} lines (L{start_line}-L{idx})")
                    in_method = False
                    method_name = ""
                brace_depth -= 1

for f in cs_files:
    analyze_file(f)
