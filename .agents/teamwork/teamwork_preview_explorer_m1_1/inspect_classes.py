import json
import os

dir_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_1'

def inspect_classes(json_filename, target_classes):
    file_path = os.path.join(dir_path, json_filename)
    with open(file_path, 'r', encoding='utf-16') as f:
        d = json.load(f)
    nr = d.get('normalized_result', {})
    types = {t.get('token'): t for t in nr.get('types', [])}
    methods = nr.get('methods', [])
    fields = nr.get('fields', [])

    print(f'=== Results for {json_filename} ===')
    for target in target_classes:
        matched = []
        for tok, t in types.items():
            n = t.get('name')
            ns = t.get('namespace') or ''
            full = f'{ns}.{n}' if ns else n
            if target.lower() in full.lower():
                matched.append((tok, full))
        
        for tok, full in matched:
            t_methods = [m for m in methods if m.get('declaring_type_token') == tok]
            t_fields = [fld for fld in fields if fld.get('declaring_type_token') == tok]
            print(f'\nClass: {full} (token: {tok}) - Methods: {len(t_methods)}, Fields: {len(t_fields)}')
            if t_fields:
                print('  Fields (up to 12):')
                for fld in t_fields[:12]:
                    print(f'    - {fld.get("name")}')
            if t_methods:
                print('  Methods (up to 20):')
                for m in t_methods[:20]:
                    print(f'    - {m.get("name")} (token: {m.get("token")})')

inspect_classes('rea_members_Kata_Class_Lib_Revit.json', [
    'Revit_rebar',
    'Get_Kata_Familytype',
    'Get_element',
    'info_revit_thep',
    'Perform_2D'
])

inspect_classes('rea_members_Kata_Class_Lib.json', [
    'Pub_Function',
    'Bien_chung',
    'Info_Beam3D',
    'info_Block_Beam_Rebar',
    'CircleXYZ',
    'CurveXYZ'
])
