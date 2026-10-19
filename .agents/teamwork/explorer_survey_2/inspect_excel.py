import zipfile
import xml.etree.ElementTree as ET
import glob
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

def inspect_xlsm(filepath):
    print(f"\n==================================================")
    print(f"FILE: {os.path.basename(filepath)}")
    print(f"==================================================")
    try:
        with zipfile.ZipFile(filepath, 'r') as z:
            # 1. Sheets
            sheet_names = []
            if 'xl/workbook.xml' in z.namelist():
                tree = ET.fromstring(z.read('xl/workbook.xml'))
                # find all sheets
                for elem in tree.iter():
                    if elem.tag.endswith('sheet'):
                        name = elem.attrib.get('name')
                        sheet_id = elem.attrib.get('sheetId')
                        sheet_names.append(f"{name} (id:{sheet_id})")
            print("SHEETS:", ", ".join(sheet_names))

            # 2. Search for custom formulas (UDFs) in sheet xmls
            udf_set = set()
            std_funcs = {'SUM', 'AVERAGE', 'IF', 'VLOOKUP', 'HLOOKUP', 'INDEX', 'MATCH', 'ROUND',
                         'ROUNDUP', 'ROUNDDOWN', 'MAX', 'MIN', 'COUNT', 'COUNTA', 'COUNTIF',
                         'SUMIF', 'SUMIFS', 'IFERROR', 'AND', 'OR', 'NOT', 'ABS', 'SQRT', 'PI',
                         'COS', 'SIN', 'TAN', 'ATAN', 'DEGREES', 'RADIANS', 'INT', 'MOD', 'LEN',
                         'LEFT', 'RIGHT', 'MID', 'CONCATENATE', 'CONCAT', 'TEXT', 'TRIM', 'UPPER',
                         'LOWER', 'PROPER', 'OFFSET', 'INDIRECT', 'ROW', 'COLUMN', 'ROWS', 'COLUMNS',
                         'CHOOSE', 'CEILING', 'FLOOR', 'POWER', 'EXP', 'LN', 'LOG', 'LOG10', 'TRUE',
                         'FALSE', 'ISNA', 'ISERROR', 'ISNUMBER', 'ISTEXT', 'DATE', 'YEAR', 'MONTH',
                         'DAY', 'TODAY', 'NOW', 'LOOKUP', 'PRODUCT', 'SUBTOTAL', 'TRANSPOSE', 'LINEST'}
            
            formula_samples = []
            for name in z.namelist():
                if name.startswith('xl/worksheets/sheet') and name.endswith('.xml'):
                    content = z.read(name).decode('utf-8', errors='ignore')
                    # find all <f>...</f>
                    formulas = re.findall(r'<f[^>]*>(.*?)</f>', content)
                    for f in formulas:
                        if len(formula_samples) < 5 and f not in formula_samples:
                            formula_samples.append(f)
                        # Extract function calls: WORD(
                        func_calls = re.findall(r'([A-Za-z0-9_\.]+)\(', f)
                        for fc in func_calls:
                            fc_upper = fc.upper()
                            if fc_upper not in std_funcs and not fc_upper.startswith('_XLFN'):
                                udf_set.add(fc)
            print(f"SAMPLE FORMULAS: {formula_samples[:3]}")
            if udf_set:
                print("POSSIBLE UDFs / CUSTOM FUNCTIONS:", sorted(list(udf_set)))

            # 3. VBA Project
            if 'xl/vbaProject.bin' in z.namelist():
                vba_bin = z.read('xl/vbaProject.bin')
                # find ASCII / UTF-16 function names in vbaProject.bin
                vba_funcs = set(re.findall(rb'Function\s+([A-Za-z0-9_]+)', vba_bin, re.IGNORECASE))
                vba_subs = set(re.findall(rb'Sub\s+([A-Za-z0-9_]+)', vba_bin, re.IGNORECASE))
                f_list = [f.decode('latin-1') for f in vba_funcs][:15]
                s_list = [s.decode('latin-1') for s in vba_subs][:15]
                print("VBA FUNCTIONS:", f_list)
                print("VBA SUBS:", s_list)
    except Exception as e:
        print("ERROR:", e)

print("--- EXAMINING FILE V.2020 ---")
v2020_dir = r"C:\Dce Pro\Excel\File V.2020"
for fn in [
    "01. DCE_Etabs_2020.xlsm",
    "05. DCE_MongDon.xlsm",
    "08. DCE_ColumnWall_2020.xlsm",
    "09. DCE_Beam_2020.xlsm",
    "10.1 DCE_SctCoc_10304 2025.xlsm",
    "12. DCE_SLab_2020.xlsm",
    "13. DCE_DaiCoc_2020.xlsm",
    "15. DCE_ThongKeThep_2020.xlsm"
]:
    fp = os.path.join(v2020_dir, fn)
    if os.path.exists(fp):
        inspect_xlsm(fp)

print("\n\n--- EXAMINING DCE STEEL ---")
dce_steel_dir = r"C:\Dce Pro\Excel\DceSteel"
for fn in [
    "20 TCVN 5575 Steel Beam Design - Section I.xlsm",
    "200 TCVN 5575 2024 Cot H LienKetKhop.xlsm",
    "210 TCVN 5575 2024 LienKetShear.xlsm",
    "40 TCVN 5575 2024 Cau Kien Dam Chiu Uon Cap 1.xlsm",
    "51 ThietKeXaGoMai.xlsm"
]:
    fp = os.path.join(dce_steel_dir, fn)
    if os.path.exists(fp):
        inspect_xlsm(fp)
