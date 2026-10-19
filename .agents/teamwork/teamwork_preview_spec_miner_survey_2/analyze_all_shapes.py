import json
import os
import re
import glob
import struct

def parse_wmf(filepath):
    if not os.path.exists(filepath):
        return None
    with open(filepath, 'rb') as f:
        data = f.read()
    
    pos = 18
    if data[:4] == b'\xd7\xcd\xc6\x9a':
        pos = 22 + 18
        
    texts = []
    polylines = []
    lines = []
    arcs = []
    
    while pos + 6 <= len(data):
        rd_size, rd_func = struct.unpack('<IH', data[pos:pos+6])
        if rd_size == 0:
            break
        rec_data = data[pos+6:pos + rd_size*2]
        
        if rd_func == 0x0521: # TextOut
            if len(rec_data) >= 2:
                count = struct.unpack('<H', rec_data[:2])[0]
                text = rec_data[2:2+count].decode('latin1', errors='ignore')
                texts.append(text)
        elif rd_func == 0x0a32: # ExtTextOut
            if len(rec_data) >= 8:
                count = struct.unpack('<H', rec_data[4:6])[0]
                opts = struct.unpack('<H', rec_data[6:8])[0]
                offset = 8
                if (opts & 6) != 0:
                    offset += 8
                text = rec_data[offset:offset+count].decode('latin1', errors='ignore')
                texts.append(text)
        elif rd_func == 0x0325: # Polyline
            if len(rec_data) >= 2:
                num_pts = struct.unpack('<H', rec_data[:2])[0]
                pts = []
                for i in range(num_pts):
                    px, py = struct.unpack('<hh', rec_data[2+i*4:6+i*4])
                    pts.append((px, py))
                polylines.append(pts)
        elif rd_func == 0x0213: # LineTo
            if len(rec_data) >= 4:
                py, px = struct.unpack('<hh', rec_data[:4])
                lines.append(('LineTo', px, py))
        elif rd_func == 0x0214: # MoveTo
            if len(rec_data) >= 4:
                py, px = struct.unpack('<hh', rec_data[:4])
                lines.append(('MoveTo', px, py))
        elif rd_func == 0x0817: # Arc
            if len(rec_data) >= 16:
                arcs.append('Arc')
                
        pos += rd_size * 2
        
    return {
        'texts': texts,
        'polylines_count': len(polylines),
        'polylines': polylines,
        'lines_count': len(lines),
        'arcs_count': len(arcs)
    }

def main():
    # Load block_attributes.txt
    with open('block_attributes.txt', 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    blocks = {}
    pattern = re.compile(r'BLOCK: (\w+)\s*\n((?:\s+ATTDEF:[^\n]+\n)+)')
    for match in pattern.finditer(content):
        bname = match.group(1)
        attdefs_raw = match.group(2).strip().split('\n')
        att_list = []
        for line in attdefs_raw:
            m = re.search(r'Tag="([^"]*)"\s+Prompt="([^"]*)"\s+Default="([^"]*)"', line)
            if m:
                att_list.append({
                    'tag': m.group(1),
                    'prompt': m.group(2),
                    'default': m.group(3)
                })
        blocks[bname] = att_list

    # Catalog shapes
    catalog = {}
    
    # Check all shapes 00 to 67 plus variants
    all_codes = [f"{i:02d}" for i in range(68)]
    all_names = []
    for c in all_codes:
        all_names.append(f"dang{c}")
        for v in ['a', 'b', '_1']:
            if f"dang{c}{v}" in blocks or os.path.exists(rf"C:\kata_pro\dangthep\dang{c}{v}.wmf"):
                all_names.append(f"dang{c}{v}")

    for bname in all_names:
        wmf_path = rf"C:\kata_pro\dangthep\{bname}.wmf"
        wmf_info = parse_wmf(wmf_path)
        
        dwg_attrs = blocks.get(bname, [])
        prompts = [a['prompt'] for a in dwg_attrs]
        
        # Legs are prompts matching L1..L6 or goc
        legs = [p for p in prompts if p.startswith('L') or p == 'goc']
        
        catalog[bname] = {
            'block_name': bname,
            'dwg_prompts': prompts,
            'legs': legs,
            'wmf_texts': wmf_info['texts'] if wmf_info else [],
            'has_dwg': bname in blocks,
            'has_wmf': wmf_info is not None,
            'wmf_summary': f"lines={wmf_info['lines_count']}, polys={wmf_info['polylines_count']}, arcs={wmf_info['arcs_count']}" if wmf_info else "none"
        }

    with open('full_shapes_analysis.json', 'w', encoding='utf-8') as f:
        json.dump(catalog, f, indent=2, ensure_ascii=False)

    print(f"Analyzed {len(catalog)} shape blocks. Saved to full_shapes_analysis.json")

if __name__ == '__main__':
    main()
