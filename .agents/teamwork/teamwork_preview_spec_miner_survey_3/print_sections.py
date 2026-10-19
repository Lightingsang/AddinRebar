with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\findings_report.txt", "r", encoding="utf-8") as f:
    text = f.read()

sections = text.split("==================== CATEGORY: ")

for sec in sections:
    if not sec.strip(): continue
    lines = sec.strip().splitlines()
    header = lines[0].replace(" ====================", "")
    print(f"\n>>> SECTION: {header} ({len(lines)-1} lines)")
    for l in lines[1:25]:
        print("  ", l)
    if len(lines) > 25:
        print(f"   ... and {len(lines) - 25} more")
