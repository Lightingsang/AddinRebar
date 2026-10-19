import re

with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\revit_lib_members.txt', 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Search for ExtensibleStorage or Schema
print("=== EXTENSIBLE STORAGE / SCHEMA / PARAMETERS ===")
ext_matches = [line for line in text.splitlines() if any(k in line.lower() for k in ['schema', 'extensiblestorage', 'datastorage', 'sharedparameter', 'guid', 'definitiongroup'])]
print(f"Found {len(ext_matches)} matches:")
for m in ext_matches[:30]:
    print(" ", m)

# 2. Extract nested types of Bien_chung
print("\n=== BIEN_CHUNG AND NESTED STRUCTS ===")
bc_matches = re.findall(r'(TYPE:\s+Kata_Class_Lib_Revit\.Bien_chung\+[^\n]+(?:\n\s+(?:FIELD|PROP|METHOD):[^\n]+)+)', text)
for bc in bc_matches:
    print(bc)
    print("-" * 50)
