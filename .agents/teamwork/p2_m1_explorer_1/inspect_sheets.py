import zipfile
import xml.etree.ElementTree as ET

path = r'C:\kata_pro\Kata.xlsm'
with zipfile.ZipFile(path, 'r') as z:
    wb_xml = z.read('xl/workbook.xml')
    rels_xml = z.read('xl/_rels/workbook.xml.rels')

root = ET.fromstring(wb_xml)
rels_root = ET.fromstring(rels_xml)

ns = {
    'main': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main',
    'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
}
rel_map = {}
for rel in rels_root:
    r_id = rel.attrib.get('Id')
    target = rel.attrib.get('Target')
    rel_map[r_id] = target

sheets = root.find('main:sheets', ns)
print(f"{'Index':<5} {'Tab Name':<25} {'SheetId':<8} {'rId':<8} {'Target Path':<25} {'State':<10}")
print("-" * 85)
for idx, s in enumerate(sheets.findall('main:sheet', ns)):
    name = s.attrib.get('name')
    sheet_id = s.attrib.get('sheetId')
    r_id = s.attrib.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id')
    state = s.attrib.get('state', 'visible')
    target = rel_map.get(r_id, '')
    print(f"{idx+1:<5} {name:<25} {sheet_id:<8} {r_id:<8} {target:<25} {state:<10}")
