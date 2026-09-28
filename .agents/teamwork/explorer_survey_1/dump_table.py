import zipfile
import xml.etree.ElementTree as ET
import sys

sys.stdout.reconfigure(encoding='utf-8')
xlsm_path = r'C:\kata_pro\Kata.xlsm'
z = zipfile.ZipFile(xlsm_path)

sst = []
if 'xl/sharedStrings.xml' in z.namelist():
    sst_tree = ET.fromstring(z.read('xl/sharedStrings.xml'))
    for si in sst_tree.findall('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}si'):
        t_el = si.find('{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t')
        if t_el is not None and t_el.text:
            sst.append(t_el.text)
        else:
            txt = "".join(t.text or "" for t in si.findall('.//{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t'))
            sst.append(txt)

sheet_tree = ET.fromstring(z.read('xl/worksheets/sheet3.xml'))
ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'

cells = {}
for c in sheet_tree.findall(f'.//{ns}c'):
    r = c.attrib.get('r')
    t = c.attrib.get('t')
    v_el = c.find(f'{ns}v')
    v = v_el.text if v_el is not None else None
    
    val = v
    if t == 's' and v is not None:
        idx = int(v)
        val = sst[idx] if idx < len(sst) else v
    
    cells[r] = val

cols = [chr(c) for c in range(ord('A'), ord('P'))]
print("=== DUMP ROWS 10 to 30 TABLE ===")
header = ["Row", "Label (A)", "B"] + cols[2:]
print(f"{'Row':>4} | {'Col A':<30} | {'Col B':<12} | " + " | ".join(f"{c:<8}" for c in cols[2:]))
print("-" * 160)

for r in range(10, 31):
    a = str(cells.get(f'A{r}', ''))
    b = str(cells.get(f'B{r}', ''))
    rest = [str(cells.get(f'{c}{r}', '')) for c in cols[2:]]
    print(f"{r:>4} | {a:<30} | {b:<12} | " + " | ".join(f"{x:<8}" for x in rest))
