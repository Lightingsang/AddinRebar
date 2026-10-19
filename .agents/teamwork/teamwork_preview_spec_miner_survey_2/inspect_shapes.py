import json
import os
import glob
import re

def main():
    with open('dwg_blocks_inventory.json', 'r', encoding='utf-8') as f:
        blocks = json.load(f)

    dang_blocks = {k: v for k, v in blocks.items() if k.lower().startswith('dang')}
    print(f'Total dang blocks in DWG: {len(dang_blocks)}')

    wmf_files = glob.glob(r'C:\kata_pro\dangthep\dang*.wmf')
    print(f'Total dang WMF files in C:\\kata_pro\\dangthep: {len(wmf_files)}')

    # Let's inspect each shape from 00 to 67
    shapes_info = {}
    for i in range(68):
        code_str = f"{i:02d}"
        block_name = f"dang{code_str}"
        wmf_name = f"dang{code_str}.wmf"
        wmf_path = os.path.join(r'C:\kata_pro\dangthep', wmf_name)
        
        info = {
            'code': code_str,
            'block_name': block_name,
            'has_block': block_name in dang_blocks,
            'has_wmf': os.path.exists(wmf_path),
            'attributes': [],
            'prompts': {},
            'variants': []
        }
        
        # Check variants like 02a, etc.
        for suffix in ['a', 'b', '_1', 'c']:
            var_name = f"dang{code_str}{suffix}"
            if var_name in dang_blocks:
                info['variants'].append(var_name)
        
        if info['has_block']:
            attrs = dang_blocks[block_name]
            info['attributes'] = [a.get('tag', '') for a in attrs]
            info['prompts'] = {a.get('tag', ''): a.get('prompt', '') for a in attrs if a.get('prompt')}
            info['defaults'] = {a.get('tag', ''): a.get('default', '') for a in attrs if a.get('default')}
            
        shapes_info[code_str] = info

    # Also check other dang blocks not strictly 00..67
    other_blocks = []
    for b in dang_blocks:
        match = re.match(r'dang(\d+)', b, re.IGNORECASE)
        if not match:
            other_blocks.append(b)
        else:
            num = int(match.group(1))
            if num >= 68:
                other_blocks.append(b)
    print(f'Other dang blocks: {other_blocks}')

    with open('shapes_catalog_raw.json', 'w', encoding='utf-8') as f:
        json.dump(shapes_info, f, indent=2, ensure_ascii=False)

    print("Saved shapes_catalog_raw.json successfully")

if __name__ == '__main__':
    main()
