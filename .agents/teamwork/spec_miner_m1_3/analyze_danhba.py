import json

def analyze_catalog(path, name):
    with open(path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    items = data.get('muc', [])
    print(f"=== {name}: {len(items)} items ===")
    
    categories = {}
    classes = {}
    for i, it in enumerate(items):
        nhom = it.get('nhom', 'Unknown')
        categories.setdefault(nhom, []).append(it)
        lop = it.get('lop', 'Unknown')
        classes.setdefault(lop, []).append(it)
        
    print(f"Categories ({len(categories)}):")
    for k, v in categories.items():
        print(f"  {k}: {len(v)} items")
        
    print(f"Classes ({len(classes)}):")
    for k, v in classes.items():
        print(f"  {k}: {len(v)} items")
    return items

excel_items = analyze_catalog(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json', 'Excel Ribbon Actions')
thep_items = analyze_catalog(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json', 'DceSteel Functions')
mbkc_items = analyze_catalog(r'C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json', 'MBKC Functions')
