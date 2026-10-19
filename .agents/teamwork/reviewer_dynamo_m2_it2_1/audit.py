import re
import sys

def audit_file(filepath):
    print(f"=== Auditing {filepath} ===")
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    total_lines = len(lines)
    print(f"Total lines: {total_lines} (<= 300: {'PASS' if total_lines <= 300 else 'FAIL'})")

    methods = []
    current_method = None
    brace_depth = 0
    method_pattern = re.compile(r'^\s*(public|private|internal|protected)\s+(static\s+)?[\w\<\>\?,\s]+\s+(\w+)\s*\(')

    for idx, line in enumerate(lines, 1):
        # Look for method definition
        m = method_pattern.match(line)
        if m and brace_depth == 1:
            name = m.group(3)
            current_method = {'name': name, 'start': idx, 'body_start': None}

        if '{' in line:
            brace_depth += line.count('{')
            if current_method and current_method['body_start'] is None and brace_depth >= 2:
                current_method['body_start'] = idx

        if '}' in line:
            brace_depth -= line.count('}')
            if current_method and brace_depth == 1:
                current_method['end'] = idx
                methods.append(current_method)
                current_method = None

    all_pass = True
    for m in methods:
        total_len = m['end'] - m['start'] + 1
        body_len = m['end'] - m['body_start'] + 1 if m['body_start'] else 0
        status = 'PASS' if total_len <= 50 else 'FAIL'
        if status == 'FAIL':
            all_pass = False
        print(f"  {m['name']}: lines {m['start']}-{m['end']} (total={total_len}, body={body_len}) [{status}]")

    print(f"Method limit audit: {'PASS' if all_pass else 'FAIL'}")

    # Check for empty catches
    empty_catches = []
    for idx, line in enumerate(lines, 1):
        if re.search(r'catch\s*\([^\)]*\)\s*\{\s*\}', line):
            empty_catches.append(idx)
    print(f"Empty catches: {len(empty_catches)}")

    # Check for commented-out code heuristics
    commented_code = []
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//") and not stripped.startswith("///"):
            comment_content = stripped[2:].strip()
            # If comment ends with semicolon or looks like C# statement
            if (comment_content.endswith(";") and not comment_content.startswith("http")) or comment_content.startswith("var ") or comment_content.startswith("return "):
                commented_code.append((idx, stripped))
    print(f"Commented-out code lines: {len(commented_code)}")
    if commented_code:
        for idx, text in commented_code:
            print(f"  Line {idx}: {text}")

if __name__ == '__main__':
    audit_file('HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs')
    audit_file('HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs')
    audit_file('HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs')
    audit_file('HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs')
