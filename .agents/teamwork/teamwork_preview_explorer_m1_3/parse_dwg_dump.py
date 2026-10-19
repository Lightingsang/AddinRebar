with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\dwg_dump.txt', 'r', encoding='utf-8') as f:
    text = f.read()

sections = text.split('=== ')
for sec in sections:
    if not sec.strip(): continue
    lines = sec.strip().split('\n')
    header = lines[0].replace(' ===', '')
    items = lines[1:]
    print(f"Section: {header} -> Total items: {len(items)}")
    if header == 'BLOCKS':
        named_blocks = [b.replace('BLOCK: ', '') for b in items if not b.startswith('BLOCK: *')]
        anon_blocks = [b.replace('BLOCK: ', '') for b in items if b.startswith('BLOCK: *')]
        print(f"  Named Blocks ({len(named_blocks)}): {named_blocks}")
        print(f"  Anonymous Blocks ({len(anon_blocks)}): {anon_blocks}")
    elif header == 'LAYERS':
        layers = [l.replace('LAYER: ', '') for l in items]
        print(f"  Layers ({len(layers)}): {layers}")
    elif header == 'DIMSTYLES':
        dims = [d.replace('DIMSTYLE: ', '') for d in items]
        print(f"  Dimstyles ({len(dims)}): {dims}")
    elif header == 'TEXT STYLES':
        styles = [s.replace('STYLE: ', '') for s in items]
        print(f"  Text Styles ({len(styles)}): {styles}")
