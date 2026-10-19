import re

out_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\mined_udf_signatures.txt"

with open(out_path, "w", encoding="utf-8") as out:
    for name, path in [
        ("DCE Excel", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_excel_methods.txt"),
        ("Dce Steel", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dce_steel_methods.txt"),
        ("Dbim Steel Cad", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\dbim_steel_autocad_methods.txt")
    ]:
        out.write(f"\n==================== {name} ====================\n")
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                # Looking for meaningful public / static methods
                if "static" in line:
                    if not any(x in line for x in ["CompilerGenerated", "smethod", "Debugger", "get_", "set_"]):
                        # Filter out obfuscated method names like ihFPpbBcpor1aSFOiWAw
                        parts = line.split("::")
                        if len(parts) == 2:
                            cls = parts[0].strip()
                            method = parts[1].strip()
                            # check if method name looks human readable or domain relevant
                            out.write(line)
                elif any(kw.lower() in line.lower() for kw in ["tinh", "dam", "cot", "vach", "san", "mong", "dai", "coc", "wind", "gio", "steel", "bulong", "lienket", "vetnut", "crack", "pmm"]):
                    if not any(x in line for x in ["CompilerGenerated", "smethod", "Debugger", "get_", "set_"]):
                        out.write(line)

print("Finished writing to mined_udf_signatures.txt")
