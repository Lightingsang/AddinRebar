import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/survey_dwg_shapes_report.md', 'r', encoding='utf-8') as f:
    text = f.read()

# Search for mentions of block definitions or attributes for shapes
for m in re.finditer(r'(dang\d+[a-z]?|dangCoupler)[^\n]*', text):
    line = m.group(0)
    if 'ATTDEF' in line or 'Attribute' in line or 'Thuộc tính' in line or 'block' in line.lower():
        print(line[:120])
