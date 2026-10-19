import os
import re

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"
exclude_dirs = {"bin", "obj", ".vs", ".git", "TestResults", ".idea", "Distribution"}

c2_violations = []
m3_violations = []
qb1_violations = []
qb2_violations = []

# Regex for methods (approximate for C#)
# A method line like: [access] [modifiers] [return_type] MethodName(...)
method_sig_regex = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where[^{]+)?\{')

# Regex for commented-out code: lines starting with // followed by typical C# statements like var, if, for, return, Assert, etc.
commented_code_regex = re.compile(r'^\s*//\s*(?:var\s+|if\s*\(|for\s*\(|foreach\s*\(|return\s+|Assert\.|throw\s+new|public\s+|private\s+)[^;]*;')

# Regex for empty catch: catch (...) { }
empty_catch_regex = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{\s*\}')

for root, dirs, files in os.walk(root_dir):
    dirs[:] = [d for d in dirs if d not in exclude_dirs]
    for file in files:
        if file.endswith(".cs"):
            file_path = os.path.join(root, file)
            rel_path = os.path.relpath(file_path, root_dir)
            with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
                lines = f.readlines()
            
            # Rule C2: <= 300 lines
            if len(lines) > 300:
                c2_violations.append((rel_path, len(lines)))
            
            # Check methods (Rule M3)
            # Find open braces and track method lengths
            i = 0
            while i < len(lines):
                line = lines[i]
                
                # Check Q-B1: commented-out code
                if commented_code_regex.match(line):
                    qb1_violations.append((rel_path, i + 1, line.strip()))
                
                # Check Q-B2: empty catch
                if empty_catch_regex.search(line):
                    qb2_violations.append((rel_path, i + 1, line.strip()))
                
                match = method_sig_regex.match(line)
                if match:
                    method_name = match.group(1)
                    start_line = i + 1
                    brace_count = line.count('{') - line.count('}')
                    j = i + 1
                    while j < len(lines) and brace_count > 0:
                        brace_count += lines[j].count('{') - lines[j].count('}')
                        j += 1
                    method_len = j - i
                    if method_len > 50:
                        m3_violations.append((rel_path, method_name, start_line, method_len))
                    i = j
                else:
                    i += 1

print(f"Rule C2 (<= 300 lines) Violations: {len(c2_violations)}")
for rel, count in c2_violations:
    print(f"  {rel}: {count} lines")

print(f"Rule M3 (<= 50 lines) Violations in modified/new files:")
m1_new_or_modified = [
    "IKataInstallEnvironment.cs",
    "DefaultKataInstallEnvironment.cs",
    "KataInstallEnvironment.cs",
    "KataInstallEnvironmentTests.cs",
    "CodebasePurgeIntegrityTests.cs",
    "SetupController.cs",
    "KataConfigParserTests.cs"
]
for rel, name, start, length in m3_violations:
    if any(m in rel for m in m1_new_or_modified):
        print(f"  M1 File: {rel}:{start} method '{name}' has {length} lines")

print(f"Total M3 across entire HPKata: {len(m3_violations)}")

print(f"Rule Q-B1 (commented-out code) in M1 files: {len([q for q in qb1_violations if any(m in q[0] for m in m1_new_or_modified)])}")
for rel, line_num, content in qb1_violations:
    if any(m in rel for m in m1_new_or_modified):
        print(f"  {rel}:{line_num}: {content}")

print(f"Rule Q-B2 (empty catch blocks) in M1 files: {len([q for q in qb2_violations if any(m in q[0] for m in m1_new_or_modified)])}")
for rel, line_num, content in qb2_violations:
    if any(m in rel for m in m1_new_or_modified):
        print(f"  {rel}:{line_num}: {content}")
