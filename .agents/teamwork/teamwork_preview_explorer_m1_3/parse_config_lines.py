with open(r'C:\kata_pro\Goi lenh.lsp', 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

config_lines = lines[151:237]

with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\config_lines_dump.txt', 'w', encoding='utf-8') as out:
    out.write(f"Total configuration lines: {len(config_lines)}\n\n")
    for i, line in enumerate(config_lines, start=1):
        val = line.strip().lstrip(';')
        out.write(f"Line {i:2d} (file line {151+i:3d}): '{val}'\n")

print("Done writing to config_lines_dump.txt")
