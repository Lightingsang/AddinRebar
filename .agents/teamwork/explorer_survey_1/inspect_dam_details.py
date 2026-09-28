import zipfile
import xml.etree.ElementTree as ET
import sys
import re

sys.stdout.reconfigure(encoding='utf-8')

xlsm_path = r'C:\kata_pro\Kata.xlsm'
z = zipfile.ZipFile(xlsm_path)

# Shared strings
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

# Comments
comments = {}
for name in z.namelist():
    if 'comments' in name:
        comm_tree = ET.fromstring(z.read(name))
        for c in comm_tree.findall('.//{http://schemas.openxmlformats.org/spreadsheetml/2006/main}comment'):
            ref = c.attrib.get('ref')
            txt = "".join(t.text or "" for t in c.findall('.//{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t'))
            comments[ref] = txt

sheet_tree = ET.fromstring(z.read('xl/worksheets/sheet3.xml'))
ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'

cells = {}
for c in sheet_tree.findall(f'.//{ns}c'):
    r = c.attrib.get('r')
    t = c.attrib.get('t')
    v_el = c.find(f'{ns}v')
    v = v_el.text if v_el is not None else None
    f_el = c.find(f'{ns}f')
    f = f_el.text if f_el is not None else None
    
    val = v
    if t == 's' and v is not None:
        idx = int(v)
        val = sst[idx] if idx < len(sst) else v
    elif t == 'str' and v is not None:
        val = v
    
    cells[r] = {
        'val': val,
        'formula': f,
        'type': t,
        'comment': comments.get(r)
    }

print("=== ALL COMMENTS IN SHEET DAM ===")
for ref in sorted(comments.keys(), key=lambda x: (int(re.findall(r'\d+', x)[0]), re.findall(r'[A-Z]+', x)[0])):
    print(f"Cell {ref}: {comments[ref].strip()}")

print("\n=== DATA VALIDATION IN SHEET DAM ===")
for dv in sheet_tree.findall(f'.//{ns}dataValidation'):
    sqref = dv.attrib.get('sqref')
    f1 = dv.find(f'{ns}formula1')
    f1_txt = f1.text if f1 is not None else ""
    print(f"Validation range: {sqref}, formula: {f1_txt}, type: {dv.attrib.get('type')}")

print("\n=== COMPLETE ROWS 1 TO 30 GRID DUMP (Cols A to O) ===")
cols = [chr(c) for c in range(ord('A'), ord('P'))]
for r in range(1, 31):
    items = []
    for col in cols:
        ref = f"{col}{r}"
        if ref in cells and cells[ref]['val'] is not None and str(cells[ref]['val']).strip() != "":
            items.append(f"{col}:{cells[ref]['val']}")
    if items:
        print(f"R{r:02d} | " + " | ".join(items))

print("\n=== ROWS 31 TO 45 GRID DUMP ===")
for r in range(31, 46):
    items = []
    for col in cols:
        ref = f"{col}{r}"
        if ref in cells and cells[ref]['val'] is not None and str(cells[ref]['val']).strip() != "":
            items.append(f"{col}:{cells[ref]['val']}")
    if items:
        print(f"R{r:02d} | " + " | ".join(items))
