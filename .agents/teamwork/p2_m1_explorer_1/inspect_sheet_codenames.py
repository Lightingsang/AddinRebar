import zipfile
import xml.etree.ElementTree as ET

path = r'C:\kata_pro\Kata.xlsm'
with zipfile.ZipFile(path, 'r') as z:
    for i in range(1, 16):
        sheet_path = f'xl/worksheets/sheet{i}.xml'
        sheet_xml = z.read(sheet_path)
        root = ET.fromstring(sheet_xml)
        ns = {'main': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
        sheet_pr = root.find('main:sheetPr', ns)
        code_name = sheet_pr.attrib.get('codeName') if sheet_pr is not None else 'None'
        print(f"sheet{i}.xml -> codeName: {code_name}")
