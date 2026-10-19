import os
import zipfile
import xml.etree.ElementTree as ET
import re

excel_files = [
    r'C:\kata_pro\Kata.xlsm',
    r'C:\kata_pro\Kata ENG.xlsm',
    r'C:\kata_pro\Kata-GCAD.xlsm',
    r'C:\kata_pro\Kata-ZwCad.xlsm',
    r'C:\kata_pro\Iso_QHPlus.xls'
]

print("=== EXCEL WORKBOOKS ANALYSIS ===")
for path in excel_files:
    if not os.path.exists(path):
        print(f"File not found: {path}")
        continue
    size = os.path.getsize(path)
    print(f"\nWorkbook: {os.path.basename(path)} ({size:,} bytes)")
    
    if path.endswith('.xlsm'):
        with zipfile.ZipFile(path, 'r') as z:
            # 1. Sheets in workbook.xml
            wb_xml = z.read('xl/workbook.xml')
            root = ET.fromstring(wb_xml)
            ns = {'main': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main',
                  'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
            sheets = root.findall('.//main:sheet', ns)
            sheet_names = [s.attrib.get('name') for s in sheets]
            print(f"Total Sheets: {len(sheet_names)}")
            print(f"Sheet List: {sheet_names}")
            
            # 2. Check for xl/vbaProject.bin
            if 'xl/vbaProject.bin' in z.namelist():
                vba_data = z.read('xl/vbaProject.bin')
                print(f"vbaProject.bin: PRESENT ({len(vba_data):,} bytes)")
                
                # Check for module names / strings
                # Extract ASCII and UTF-16 strings > 4 chars
                ascii_strings = re.findall(rb'[A-Za-z0-9_]{4,}', vba_data)
                ascii_str_decoded = [s.decode('latin-1') for s in ascii_strings]
                modules = [s for s in ascii_str_decoded if s.startswith(('Module', 'frm', 'ThisWorkbook', 'Sheet'))]
                print(f"Sample VBA Module Identifiers detected: {list(set(modules))[:15]}")
                
                # Check for DPB= (VBA protection)
                dpb_match = re.search(rb'DPB="[^"]+"', vba_data)
                cmg_match = re.search(rb'CMG="[^"]+"', vba_data)
                gc_match = re.search(rb'GC="[^"]+"', vba_data)
                if dpb_match or cmg_match or gc_match:
                    print(f"VBA Project Lock State: PASSWORD PROTECTED / LOCKED (DPB={bool(dpb_match)}, CMG={bool(cmg_match)}, GC={bool(gc_match)})")
                else:
                    print("VBA Project Lock State: UNLOCKED / NO DPB")
            else:
                print("vbaProject.bin: ABSENT")
    elif path.endswith('.xls'):
        # BIFF8 binary file
        with open(path, 'rb') as f:
            data = f.read()
        print(f"Legacy BIFF8 binary format. Header: {data[:8].hex()}")
        # Check for sheet names in BIFF8 (BOUNDSHEET record 0x0085)
        # Scan for bound sheets
        sheet_names = []
        pos = 0
        while pos < len(data) - 4:
            rec_id = int.from_bytes(data[pos:pos+2], 'little')
            rec_len = int.from_bytes(data[pos+2:pos+4], 'little')
            if rec_id == 0x0085: # BOUNDSHEET
                # pos + 4: lbPlyPos (4 bytes), hsState (1 byte), dt (1 byte)
                # pos + 10: cch (1 byte string length)
                cch = data[pos+10]
                flag = data[pos+11]
                if flag == 0 and pos + 12 + cch <= len(data):
                    name = data[pos+12:pos+12+cch].decode('latin-1', errors='ignore')
                    sheet_names.append(name)
                elif flag == 1 and pos + 12 + cch*2 <= len(data):
                    name = data[pos+12:pos+12+cch*2].decode('utf-16le', errors='ignore')
                    sheet_names.append(name)
            pos += 4 + rec_len
        print(f"Total Sheets in BIFF8: {len(sheet_names)}")
        print(f"Sheet List: {sheet_names}")
