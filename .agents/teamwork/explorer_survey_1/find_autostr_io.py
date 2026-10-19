with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

io_types = [l for l in lines if any(k in l.lower() for k in ["autostrconfig", "autostrio", "autostrserialize", "autostrproject", "autostrfile"])]
for t in io_types:
    print("  " + t)
