import zipfile
import xml.etree.ElementTree as ET
import sys
import re

sys.stdout.reconfigure(encoding='utf-8')

xlsm_path = r'C:\kata_pro\Kata.xlsm'
z = zipfile.ZipFile(xlsm_path)

# 1. Load shared strings
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

# 2. Check all comment files
comments = {}
for name in z.namelist():
    if 'comments' in name:
        comm_tree = ET.fromstring(z.read(name))
        for c in comm_tree.findall('.//{http://schemas.openxmlformats.org/spreadsheetml/2006/main}comment'):
            ref = c.attrib.get('ref')
            txt = "".join(t.text or "" for t in c.findall('.//{http://schemas.openxmlformats.org/spreadsheetml/2006/main}t'))
            comments[ref] = txt

# 3. Load sheet3.xml (Dam)
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

print("=== COLUMN A LABELS (Rows 1 to 45) ===")
for row in range(1, 46):
    ref = f'A{row}'
    if ref in cells and cells[ref]['val']:
        print(f"Row {row:2d} (A{row}): {cells[ref]['val']}")

print("\n=== HEADER & CONFIG CELLS (Columns A..J, Rows 1 to 10) ===")
for r_idx in range(1, 11):
    row_vals = []
    for col_letter in ['A','B','C','D','E','F','G','H','I','J','K','L','M','N','O']:
        ref = f'{col_letter}{r_idx}'
        if ref in cells and cells[ref]['val'] is not None:
            comm = f" [Comment: {cells[ref]['comment']}]" if cells[ref]['comment'] else ""
            row_vals.append(f"{ref}='{cells[ref]['val']}'{comm}")
    if row_vals:
        print(f"Row {r_idx:2d}: " + " | ".join(row_vals))

print("\n=== ROWS 11 to 35 (Sample data in Columns A..H) ===")
for r_idx in range(11, 36):
    row_vals = []
    for col_letter in ['A','B','C','D','E','F','G','H','I','J']:
        ref = f'{col_letter}{r_idx}'
        if ref in cells and cells[ref]['val'] is not None:
            comm = f" [/* {cells[ref]['comment']} */]" if cells[ref]['comment'] else ""
            form = f" [={cells[ref]['formula']}]" if cells[ref]['formula'] else ""
            row_vals.append(f"{ref}='{cells[ref]['val']}'{form}{comm}")
    if row_vals:
        print(f"Row {r_idx:2d}: " + " | ".join(row_vals))
