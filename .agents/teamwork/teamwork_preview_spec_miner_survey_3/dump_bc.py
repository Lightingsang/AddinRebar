with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\revit_lib_members.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

in_target = False
target_lines = []

for line in lines:
    if 'TYPE: Kata_Class_Lib_Revit.Bien_chung' in line:
        in_target = True
        target_lines.append(line)
    elif line.startswith('TYPE:') and not 'Kata_Class_Lib_Revit.Bien_chung' in line:
        in_target = False
    elif in_target:
        target_lines.append(line)

print('Captured lines:', len(target_lines))
with open('bien_chung_dump.txt', 'w', encoding='utf-8') as out:
    out.writelines(target_lines)

print("Saved to bien_chung_dump.txt")
