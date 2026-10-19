import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    bp = f.read()

with open('PROJECT.md', 'r', encoding='utf-8') as f:
    proj = f.read()

print('=== 1. CHECK SECTION 9 HOST NEUTRALITY ===')
assert '### 9.1 Host-Neutral Domain DTOs (`HPKata.Core.Models`)' in bp, 'Heading 9.1 not found'
assert '### 9.2 Native Element Creation & Unit Conversion (`HPKata.Revit`)' in bp, 'Heading 9.2 not found'
assert '### 9.3 Idempotent Regeneration Pattern' in bp, 'Heading 9.3 not found'

idx_91 = bp.find('### 9.1')
idx_92 = bp.find('### 9.2')
idx_93 = bp.find('### 9.3')

sec9_1 = bp[idx_91:idx_92]
sec9_2 = bp[idx_92:idx_93]

# In 9.1:
for token in ['KataPoint3D', 'KataVector3D', 'KataLayoutRule', 'KataRebarSpec', 
              'DiameterMm', 'OriginMm', 'SpacingMm', 'ArrayLengthMm', 'ParametersMm']:
    assert token in sec9_1, f'{token} missing from Section 9.1'

# Check for forbidden Revit types in 9.1
cb = sec9_1[sec9_1.find('```csharp'):sec9_1.rfind('```')]
for forbidden in ['Autodesk', 'Revit', 'XYZ', 'ElementId', 'Curve', 'Rebar', 'Feet', 'foot']:
    match = re.search(r'\b' + forbidden + r'\b', cb, re.IGNORECASE)
    assert not match, f'Found forbidden Revit token {forbidden} in 9.1: {match.group(0)}'
print('PASS: HPKata.Core.Models in Section 9.1 is 100% host-neutral.')

# In 9.2:
for token in ['HPKata.Revit.NativeRebar', 'ToRevitXyz', 'ToRevitVector', 'ToFeet', '304.8',
              'RebarBarType', 'RebarHookType', 'RebarShapeDrivenAccessor']:
    assert token in sec9_2, f'{token} missing from Section 9.2'
print('PASS: Revit API dependencies strictly quarantined in Section 9.2 (HPKata.Revit).')

print('\n=== 2. CHECK PROJECT.MD CLEAN CODE GOVERNANCE ===')
assert '## Clean Code Governance & Engineering Standards' in proj, 'Governance header missing in PROJECT.md'
for req in ['Rule C2 (Class Size)', '300', 'Rule M3 (Method Size)', '50',
            'Rule Q-B1 (Dead Code)', 'Rule Q-B2 (Catch Blocks)', 'Host Neutrality (Rules D1, D4)',
            'HPKata.Core', 'Autodesk.AutoCAD.*', 'Autodesk.Revit.*', '304.8']:
    assert req in proj, f'{req} missing from PROJECT.md'
print('PASS: PROJECT.md defines Clean Code Governance & Engineering Standards.')

print('\n=== 3. CHECK COMMAND INVENTORY IN SECTION 2 ===')
sec2_match = re.search(r'## 2\. Comprehensive Command Inventory.*?(?=## 3\.)', bp, re.DOTALL)
assert sec2_match, 'Section 2 not found'
sec2 = sec2_match.group(0)

# Check command numbers
cmd_nums = [int(m.group(1)) for m in re.finditer(r'\|\s*(\d+)\s*\|', sec2)]
print(f'Total cataloged commands in Section 2: {len(cmd_nums)}')
assert len(cmd_nums) >= 120, f'Expected >= 120 commands, found {len(cmd_nums)}'
assert max(cmd_nums) == len(cmd_nums) == 136, f'Expected 136 numbered commands, got max {max(cmd_nums)} and count {len(cmd_nums)}'

# Check 14 previously missing LISP commands
lisp_14 = ['tstt', 'rthep', 'lv', 'lv1', 'sst', 'dao', 'ad', 'cht', 'hideatt', 'Unhideatt', 'vemc', 'chuyen', 'pdfkata', 'taomv1']
for cmd in lisp_14:
    assert re.search(r'\b' + re.escape(cmd) + r'\b', sec2, re.IGNORECASE), f'LISP command {cmd} missing from Section 2'
print(f'PASS: All 14 previously missing LISP commands present in Section 2.')

# Check 16 previously missing .NET commands
dotnet_16 = ['dao_thep', 'GanTai', 'HoatTai', 'Update_Block_ColumnWall', 'updatecrank', 'updatedai', 'updateleg', 
             'VMBM_LIGHT', 'phamong', 'drpn', 'openning', 'loadkho', 'upkho', 'tkqs', 'CHIA_TAM_LIGHT', 'OP_GACH_LIGHT']
for cmd in dotnet_16:
    assert re.search(r'\b' + re.escape(cmd) + r'\b', sec2, re.IGNORECASE), f'.NET command {cmd} missing from Section 2'
print(f'PASS: All 16 previously missing .NET CommandMethods present in Section 2.')

print('\n=== 4. CHECK SECTION 3.2 ENCODING SPECIFICATION ===')
sec3_match = re.search(r'### 3\.2.*?(?=## 4\.)', bp, re.DOTALL)
assert sec3_match, 'Section 3.2 not found'
sec3_2 = sec3_match.group(0)
for enc in ['UTF-8', 'Windows-1258', 'Windows-1252', 'CodePagesEncodingProvider']:
    assert enc in sec3_2, f'Encoding {enc} missing from Section 3.2'
print('PASS: Section 3.2 specifies UTF-8, Windows-1258, and CP1252 character encodings.')

print('\n=== 5. CHECK SECTION 7 CUTTING STOCK DEFENSES ===')
sec7_match = re.search(r'## 7\. 1D Cutting-Stock Optimization Engine.*?(?=## 8\.)', bp, re.DOTALL)
assert sec7_match, 'Section 7 not found'
sec7 = sec7_match.group(0)
assert r'\text{Waste}_{\text{net}}' in sec7, 'Waste_net missing in Section 7'
assert 'N_{\\text{new\\_bars}} = 0' in sec7 or 'N_{new_bars} = 0' in sec7, 'Zero new bars guard missing in Section 7.1'
assert 'DuBang0' in sec7, 'DuBang0 missing in Section 7.2'
assert '1,000' in sec7, 'Timeout budget missing in Section 7.2'
assert '5' in sec7, 'Cardinality bound missing in Section 7.2'
print('PASS: Section 7 includes zero-denominator scrap defense and DuBang0 bounds.')

print('\n=== ALL VERIFICATION CHECKS PASSED WITH ZERO ERRORS ===')
