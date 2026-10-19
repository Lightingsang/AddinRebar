import sys
import re

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

with open('docs/rea-dce-pro/TRUTH_MATRIX.md', encoding='utf-8') as f:
    lines = f.readlines()

# Filter only rows from the 9 subsystem tables (exclude summary table)
table_lines = []
in_summary = False
for l in lines:
    l_str = l.strip()
    if '### Summary Statistics Table' in l_str:
        in_summary = True
    elif in_summary and l_str.startswith('---'):
        in_summary = False
    elif not in_summary and l_str.startswith('|') and not l_str.startswith('|---') and not l_str.startswith('| Legacy Identifier'):
        table_lines.append(l_str)

print(f"Total table rows extracted: {len(table_lines)}")

status_counts = {}
for l in table_lines:
    parts = [p.strip() for p in l.split('|') if p.strip()]
    if not parts:
        continue
    status = parts[-1]
    status_counts[status] = status_counts.get(status, 0) + 1

print("Status counts from table rows:")
for s, c in sorted(status_counts.items()):
    print(f"  {s}: {c}")

# Check all mock usages
print("\nChecking any rows marked HOÀN THÀNH that mention mock/sample/dummy in Inspection Result or Target File:")
hoan_thanh_violations = []
for l in table_lines:
    parts = [p.strip() for p in l.split('|') if p.strip()]
    if len(parts) >= 5 and parts[-1] == 'HOÀN THÀNH':
        inspection = parts[3]
        target = parts[4]
        for kw in ['sample', 'mock', 'dummy', 'calculatesamplebeam', 'generatesamplecolumnbuffer']:
            if kw in inspection.lower() or kw in target.lower():
                hoan_thanh_violations.append((parts[0], kw, l))

print(f"Violations found in HOÀN THÀNH: {len(hoan_thanh_violations)}")
for v in hoan_thanh_violations:
    print(v)

# Check all ĐANG LÀ MOCK entries
dang_la_mock_rows = [l for l in table_lines if l.split('|')[-2].strip() == 'ĐANG LÀ MOCK']
print(f"\nTotal ĐANG LÀ MOCK rows: {len(dang_la_mock_rows)}")

missing_file = []
for l in dang_la_mock_rows:
    parts = [p.strip() for p in l.split('|') if p.strip()]
    ident = parts[0]
    target = parts[4]
    if not re.search(r'\.(cs|xaml)', target):
        missing_file.append((ident, target))

print(f"Mocks missing target file: {len(missing_file)}")
for m in missing_file:
    print(m)

# Check all CHƯA GIẢI MÃ ĐƯỢC entries
chua_giai_ma_rows = [l for l in table_lines if l.split('|')[-2].strip() == 'CHƯA GIẢI MÃ ĐƯỢC']
print(f"\nTotal CHƯA GIẢI MÃ ĐƯỢC rows: {len(chua_giai_ma_rows)}")

missing_token_or_reason = []
for l in chua_giai_ma_rows:
    parts = [p.strip() for p in l.split('|') if p.strip()]
    ident = parts[0]
    token = parts[2]
    inspection = parts[3]
    if not token or token == '-' or not inspection:
        missing_token_or_reason.append((ident, token, inspection))

print(f"CHƯA GIẢI MÃ ĐƯỢC missing token/RVA or reason: {len(missing_token_or_reason)}")
for m in missing_token_or_reason:
    print(m)
