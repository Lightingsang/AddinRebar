import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

truth_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md'
hpdce_dir = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro'
repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'

with open(truth_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

print('Total lines in TRUTH_MATRIX.md:', len(lines))

summary_rows = {}
in_summary = False
for line in lines:
    if '### Summary Statistics Table' in line:
        in_summary = True
        continue
    if in_summary:
        if line.startswith('---') or (line.startswith('## ') and not line.startswith('### ')):
            in_summary = False
            continue
        m = re.match(r'\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|', line)
        if m:
            subsys_idx = m.group(1).strip()
            subsys_name = m.group(2).strip()
            total = int(m.group(3))
            ht = int(m.group(4))
            mock = int(m.group(5))
            cgm = int(m.group(6))
            target = m.group(7).strip()
            summary_rows[subsys_idx] = {'name': subsys_name, 'total': total, 'ht': ht, 'mock': mock, 'cgm': cgm, 'target': target}

print('\n=== SUMMARY TABLE PARSED ===')
for k, v in summary_rows.items():
    print(f"  {k}: total={v['total']}, ht={v['ht']}, mock={v['mock']}, cgm={v['cgm']}")

current_subsys = None
subsys_row_counts = {}
row_counts = {'HOÀN THÀNH': 0, 'ĐANG LÀ MOCK': 0, 'CHƯA GIẢI MÃ ĐƯỢC': 0}
missing_files = []
total_parsed_rows = 0

for line in lines:
    m_sub = re.match(r'##\s*(\d+)\.\s*(Subsystem\s*\d+[^(\n]+)', line)
    if m_sub:
        current_subsys = f"Subsystem {m_sub.group(1)}: {m_sub.group(2).strip()}"
        subsys_row_counts[current_subsys] = {'HOÀN THÀNH': 0, 'ĐANG LÀ MOCK': 0, 'CHƯA GIẢI MÃ ĐƯỢC': 0, 'rows': []}
        continue
    elif line.startswith('## 11.') or line.startswith('## 12.'):
        current_subsys = None
        continue
            
    if current_subsys and line.startswith('|') and not line.startswith('|---'):
        parts = [p.strip() for p in line.strip().split('|')[1:-1]]
        if len(parts) >= 4:
            header_keywords = ['Legacy Identifier', 'Action Name', 'Function / Identifier', 
                               'MBKC Identifier', 'Export Name', 'Revit Command', 
                               'Jig Class', 'UI Form / Class', 'Step #']
            if any(hk in parts[0] for hk in header_keywords):
                continue
                
            status_col = parts[-1]
            target_file_col = parts[-2]
            identifier = parts[0]
            
            status_clean = None
            if 'HOÀN THÀNH' in status_col:
                status_clean = 'HOÀN THÀNH'
            elif 'MOCK' in status_col:
                status_clean = 'ĐANG LÀ MOCK'
            elif 'CHƯA GIẢI MÃ' in status_col:
                status_clean = 'CHƯA GIẢI MÃ ĐƯỢC'
                
            if status_clean:
                total_parsed_rows += 1
                row_counts[status_clean] += 1
                subsys_row_counts[current_subsys][status_clean] += 1
                
                if status_clean == 'HOÀN THÀNH':
                    m_file = re.search(r'`([^`]+)`', target_file_col)
                    if m_file:
                        fpath = m_file.group(1).split(':')[0]
                        # check path
                        cand1 = os.path.normpath(os.path.join(hpdce_dir, fpath))
                        cand2 = os.path.normpath(os.path.join(repo_root, fpath))
                        cand3 = os.path.normpath(fpath) if os.path.isabs(fpath) else None
                        
                        exists = os.path.isfile(cand1) or os.path.isfile(cand2) or (cand3 and os.path.isfile(cand3))
                        if not exists:
                            missing_files.append((identifier, fpath, cand1))
                    else:
                        missing_files.append((identifier, 'NO_PATH_SPECIFIED', target_file_col))

print('\n=== ROW COUNTS EMPIRICALLY SCANNED ===')
print(f'Total parsed endpoint rows: {total_parsed_rows}')
print(f"Grand Total status counts: {row_counts}")

print('\n=== BY SUBSYSTEM ===')
for s, c in subsys_row_counts.items():
    tot = c['HOÀN THÀNH'] + c['ĐANG LÀ MOCK'] + c['CHƯA GIẢI MÃ ĐƯỢC']
    print(f"  {s}: Total={tot}, HT={c['HOÀN THÀNH']}, MOCK={c['ĐANG LÀ MOCK']}, CGM={c['CHƯA GIẢI MÃ ĐƯỢC']}")

print(f'\n=== TARGET FILE EXISTENCE FOR HOÀN THÀNH ===')
print(f'Missing files count: {len(missing_files)}')
if missing_files:
    for mf in missing_files:
        print(f'  MISSING: {mf}')
else:
    print('ALL target files for HOÀN THÀNH endpoints physically exist on disk!')

print('\n=== CHECKING FOR ANY OCCURRENCE OF MOCK AS STATUS IN TRUTH_MATRIX ===')
mock_status_lines = []
for idx, l in enumerate(lines):
    if '|' in l and ('ĐANG LÀ MOCK' in l or ' MOCK |' in l or '| MOCK ' in l):
        # exclude summary header and definition
        if 'Summary Statistics Table' not in l and '| Subsystem Index |' not in l and '2. **`ĐANG LÀ MOCK`**' not in l:
            # Check if this row is a table data row
            if not l.startswith('| Subsystem') and not l.startswith('| **Grand Total'):
                mock_status_lines.append((idx + 1, l.strip()))

print(f'Rows in TRUTH_MATRIX data tables with MOCK status: {len(mock_status_lines)}')
for ln, text in mock_status_lines:
    print(f'  Line {ln}: {text}')

print('\n=== CHECKING FOR MOCK IN HPDcePro/src/ CODEBASE ===')
src_dir = os.path.join(hpdce_dir, 'src')
cs_files = []
for root, dirs, files in os.walk(src_dir):
    for f in files:
        if f.endswith('.cs'):
            cs_files.append(os.path.join(root, f))

mock_in_code = []
for cs in cs_files:
    rel = os.path.relpath(cs, src_dir)
    with open(cs, 'r', encoding='utf-8', errors='ignore') as f:
        for idx, line in enumerate(f):
            # check for mock keywords in code logic
            if re.search(r'\b(Mock|CalculateSample|GenerateSample|dummy)\b', line, re.IGNORECASE):
                # ignore comments or test references if any
                mock_in_code.append((rel, idx + 1, line.strip()))

print(f'Occurrences of mock/sample/dummy in HPDcePro/src: {len(mock_in_code)}')
for rel, line_no, content in mock_in_code[:20]:
    print(f'  {rel}:{line_no} -> {content}')

