import re
import sys

with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    text = f.read()

print("--- 1. SECTION 2 COMMAND INVENTORY ---")
sec2_match = re.search(r'## 2\. Comprehensive Command Inventory.*?(?=## 3\.)', text, re.DOTALL)
if not sec2_match:
    print('FAIL: Section 2 not found')
    sys.exit(1)

sec2_text = sec2_match.group(0)
rows = re.findall(r'^\|\s*(\d+)\s*\|([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)\|([^|]+)\|', sec2_text, re.MULTILINE)
print(f'Total command rows parsed: {len(rows)}')

nums = [int(r[0]) for r in rows]
if nums == list(range(1, 137)):
    print('PASS: Command indices are strictly contiguous from 1 to 136!')
else:
    print('FAIL: Command indices mismatch! Found min', min(nums), 'max', max(nums), 'count', len(nums))

# 2. Check 14 LISP commands
lisp_14 = ['tstt', 'rthep', 'lv', 'lv1', 'sst', 'dao', 'ad', 'cht', 'hideatt', 'Unhideatt', 'vemc', 'chuyen', 'pdfkata', 'taomv1']
missing_lisp = []
found_lisp = {}

for cmd in lisp_14:
    matched = False
    for r in rows:
        row_num = r[0]
        alias = r[1].strip().replace('`', '').strip()
        full_cmd = r[2].strip().replace('`', '').strip()
        method = r[3].strip().replace('`', '').strip()
        # check if cmd matches alias, full_cmd, or method, or split aliases
        aliases = [a.strip() for a in alias.split('/')]
        full_cmds = [f.strip() for f in full_cmd.split('/')]
        if any(cmd.lower() == a.lower() for a in aliases) or any(cmd.lower() == f.lower() for f in full_cmds) or cmd.lower() == method.lower():
            matched = True
            found_lisp[cmd] = (row_num, alias, full_cmd, method)
            break
    if not matched:
        missing_lisp.append(cmd)

print(f'\nLISP 14 check: {len(found_lisp)}/14 found. Missing: {missing_lisp}')
for cmd, info in found_lisp.items():
    print(f'  LISP {cmd:<10} -> Row {info[0]:<4} [Alias: {info[1]:<12} Full: {info[2]:<16} Method: {info[3]}]')

# 3. Check 16 .NET CommandMethods
net_16 = ['dao_thep', 'GanTai', 'HoatTai', 'Update_Block_ColumnWall', 'updatecrank', 'updatedai', 'updateleg', 'VMBM_LIGHT', 'phamong', 'drpn', 'openning', 'loadkho', 'upkho', 'tkqs', 'CHIA_TAM_LIGHT', 'OP_GACH_LIGHT']
missing_net = []
found_net = {}

for cmd in net_16:
    matched = False
    for r in rows:
        row_num = r[0]
        alias = r[1].strip().replace('`', '').strip()
        full_cmd = r[2].strip().replace('`', '').strip()
        method = r[3].strip().replace('`', '').strip()
        methods = [m.strip() for m in method.split('/')]
        full_cmds = [f.strip() for f in full_cmd.split('/')]
        if any(cmd.lower() == m.lower() for m in methods) or any(cmd.lower() == f.lower() for f in full_cmds):
            matched = True
            found_net[cmd] = (row_num, full_cmd, method)
            break
    if not matched:
        missing_net.append(cmd)

print(f'\n.NET 16 check: {len(found_net)}/16 found. Missing: {missing_net}')
for cmd, info in found_net.items():
    print(f'  .NET {cmd:<25} -> Row {info[0]:<4} [Full: {info[1]:<25} Method: {info[2]}]')

print("\n--- 2. SECTION 3.2 ENCODING FALLBACK ---")
sec3_match = re.search(r'### 3\.2.*?(?=## 4\.)', text, re.DOTALL)
if sec3_match:
    sec3_text = sec3_match.group(0)
    has_utf8 = 'UTF-8' in sec3_text
    has_win1258 = 'Windows-1258' in sec3_text or '1258' in sec3_text
    has_win1252 = 'Windows-1252' in sec3_text or '1252' in sec3_text
    has_provider = 'CodePagesEncodingProvider' in sec3_text
    print(f'UTF-8 mentioned: {has_utf8}')
    print(f'Windows-1258 mentioned: {has_win1258}')
    print(f'Windows-1252 mentioned: {has_win1252}')
    print(f'CodePagesEncodingProvider mentioned: {has_provider}')
    if has_utf8 and has_win1258:
        print('PASS: Dual character encoding fallback properly documented in Section 3.2!')
    else:
        print('FAIL: Encoding fallback missing in Section 3.2')
else:
    print('FAIL: Section 3.2 not found')

print("\n--- 3. SECTION 7 DUBANG0 BOUNDS ---")
sec7_match = re.search(r'Strategy 1: `DuBang0`.*?(?=2\. \*\*Strategy 2)', text, re.DOTALL)
if sec7_match:
    sec7_text = sec7_match.group(0)
    has_k = '5' in sec7_text and ('|S|' in sec7_text or 'k_{\\max}' in sec7_text or 'cardinality' in sec7_text)
    has_timeout = '1,000' in sec7_text or '1000' in sec7_text
    has_cts = 'CancellationTokenSource' in sec7_text
    has_fallback = 'LapDayCanhDai' in sec7_text or 'BFD' in sec7_text
    print(f'k <= 5 bound: {has_k}')
    print(f'timeout <= 1000 ms: {has_timeout}')
    print(f'CancellationTokenSource: {has_cts}')
    print(f'Fallback to BFD: {has_fallback}')
    if has_k and has_timeout:
        print('PASS: DuBang0 bounds (k <= 5, timeout <= 1000 ms) properly documented in Section 7.2!')
    else:
        print('FAIL: DuBang0 bounds missing in Section 7.2')
else:
    print('FAIL: DuBang0 strategy not found in Section 7')

print("\n--- 4. PROJECT.MD CLEAN CODE GOVERNANCE ---")
with open('PROJECT.md', 'r', encoding='utf-8') as f:
    proj_text = f.read()

has_c2 = 'C2' in proj_text and '300' in proj_text
has_m3 = 'M3' in proj_text and '50' in proj_text
has_qb1 = 'Q-B1' in proj_text
has_qb2 = 'Q-B2' in proj_text
has_d1d4 = 'D1' in proj_text and 'D4' in proj_text
print(f'Rule C2 (<= 300 lines): {has_c2}')
print(f'Rule M3 (<= 50 lines): {has_m3}')
print(f'Rule Q-B1 (Dead code): {has_qb1}')
print(f'Rule Q-B2 (Catch blocks): {has_qb2}')
print(f'Host Neutrality (D1, D4): {has_d1d4}')
if has_c2 and has_m3 and has_qb1 and has_qb2 and has_d1d4:
    print('PASS: Clean Code Governance and Host Neutrality properly documented in PROJECT.md!')
else:
    print('FAIL: Clean Code Governance missing elements in PROJECT.md')
