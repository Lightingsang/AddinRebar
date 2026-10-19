import re

for name, path in [
    ("DCE Excel", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_excel_methods.txt"),
    ("Dce Steel", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_steel_methods.txt")
]:
    print(f"=== {name} ===")
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if "static" in line:
                # filter out compiler generated or internal stuff
                if "CompilerGenerated" not in line and "smethod" not in line and "Debugger" not in line:
                    print(line.strip())
