import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

autocad_src = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad'

commands_detail = []

for root, dirs, files in os.walk(autocad_src):
    if 'bin' in root or 'obj' in root:
        continue
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            with open(filepath, 'r', encoding='utf-8') as cf:
                content = cf.read()
                lines = content.splitlines()
                for idx, line in enumerate(lines):
                    match = re.search(r'\[CommandMethod\s*\(\s*["\']([^"\']+)["\']', line)
                    if match:
                        cname = match.group(1)
                        # Extract method body roughly (until next method or end of file)
                        body_lines = []
                        brace_count = 0
                        started = False
                        for j in range(idx, min(len(lines), idx+150)):
                            cur = lines[j]
                            if '{' in cur:
                                brace_count += cur.count('{')
                                started = True
                            if '}' in cur:
                                brace_count -= cur.count('}')
                            body_lines.append(cur)
                            if started and brace_count == 0:
                                break
                        body = "\n".join(body_lines)
                        rel_path = os.path.relpath(filepath, autocad_src)
                        
                        # Assess mock status
                        is_mock = False
                        mock_reasons = []
                        if 'CalculateSample' in body:
                            is_mock = True
                            mock_reasons.append("Calls CalculateSample*")
                        if 'GenerateSample' in body:
                            is_mock = True
                            mock_reasons.append("Calls GenerateSample*")
                        if 'DceSample' in body:
                            is_mock = True
                            mock_reasons.append("Uses DceSample*")
                        if 'axes = { "TRỤC 1", "TRỤC 2" }' in body or 'TRỤC 1' in body:
                            is_mock = True
                            mock_reasons.append("Hardcoded sample axes")
                        if 'ColumnPmmInput("C1"' in body:
                            is_mock = True
                            mock_reasons.append("Hardcoded sample column input")
                        if 'fake' in body.lower() or 'dummy' in body.lower():
                            is_mock = True
                            mock_reasons.append("Contains dummy/fake indicator")
                            
                        commands_detail.append({
                            'name': cname,
                            'file': rel_path,
                            'line': idx + 1,
                            'is_mock': is_mock,
                            'reasons': ", ".join(mock_reasons) if mock_reasons else "Real logic/UI/Drafting",
                            'body_len': len(body_lines)
                        })

print(f"Total commands analyzed: {len(commands_detail)}")
mock_cmds = [c for c in commands_detail if c['is_mock']]
print(f"Total mock commands: {len(mock_cmds)}")
for c in mock_cmds:
    print(f"  {c['name']} ({c['file']}:{c['line']}): {c['reasons']}")
