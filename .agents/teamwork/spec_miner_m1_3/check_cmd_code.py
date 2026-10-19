import json
from analyze_revit import load_json

attrs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_customattr.json')['rows']
typerefs = {tr['Token']: tr for tr in load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_typeref.json')['rows']}

# Let's also dump MemberRef table to resolve Constructor tokens
# Or we can check ilspycmd output directly for one command class!
