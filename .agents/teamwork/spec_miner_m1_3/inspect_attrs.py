import json
from analyze_revit import load_json

attrs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_customattr.json')
typerefs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_typeref.json')
typedefs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_typedef.json')

attr_rows = attrs['rows']
tr_rows = typerefs['rows']
td_rows = typedefs['rows']

print(f"Total CustomAttributes: {len(attr_rows)}")
# Inspect first few attributes
for i, a in enumerate(attr_rows[:10]):
    print(a)
