import os
import struct
import math
import json

def parse_wmf_geometry(filepath):
    if not os.path.exists(filepath):
        return None
    with open(filepath, 'rb') as f:
        data = f.read()
    
    pos = 18
    if data[:4] == b'\xd7\xcd\xc6\x9a':
        pos = 22 + 18
        
    texts = []
    polylines = []
    
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
        pos += rd_size * 2
        
    # Find the main rebar polyline (usually the one with thickest or longest or most vertices)
    main_poly = []
    max_len = 0
    for poly in polylines:
        # compute perimeter
        length = 0
        for i in range(len(poly)-1):
            dx = poly[i+1][0] - poly[i][0]
            dy = poly[i+1][1] - poly[i][1]
            length += math.hypot(dx, dy)
        if length > max_len:
            max_len = length
            main_poly = poly
            
    return {
        'texts': texts,
        'main_poly_vertices': len(main_poly),
        'main_poly_pts': main_poly
    }

def main():
    shapes = {}
    for i in range(68):
        code = f"{i:02d}"
        for name in [f"dang{code}", f"dang{code}a"]:
            p = rf"C:\kata_pro\dangthep\{name}.wmf"
            if not os.path.exists(p):
                p = rf"C:\kata_pro\dangthep\{code}.wmf"
            if os.path.exists(p):
                geom = parse_wmf_geometry(p)
                shapes[name] = geom

    with open('wmf_geometries.json', 'w', encoding='utf-8') as f:
        json.dump(shapes, f, indent=2)

    print(f"Extracted geometry for {len(shapes)} shapes")

if __name__ == '__main__':
    main()
