import re

path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\mined_udf_signatures.txt"

with open(path, "r", encoding="utf-8") as f:
    lines = f.readlines()

print(f"Total lines in candidates: {len(lines)}")

patterns = [
    r"Col", r"Cot", r"Vach", r"Wall", r"San", r"Slab", r"Mong", r"Dai", r"Footing", r"Pile", r"Coc",
    r"Nut", r"Crack", r"Dam", r"Beam", r"Steel", r"Thep", r"Gio", r"Wind", r"BuLong", r"Bolt", r"LienKet", r"Connect",
    r"PMM", r"MaSat", r"SPT", r"TCVN", r"_xll"
]

matched_by_pattern = {p: [] for p in patterns}

for line in lines:
    line_clean = line.strip()
    # Filter out obfuscated method names (e.g. random letters with numbers)
    parts = line_clean.split("::")
    if len(parts) == 2:
        m_part = parts[1]
        # check if contains readable words
        for p in patterns:
            if re.search(p, m_part, re.IGNORECASE) or re.search(p, parts[0], re.IGNORECASE):
                # avoid purely obfuscated identifiers
                if not re.search(r"\b[A-Za-z0-9]{15,}\b", m_part):
                    matched_by_pattern[p].append(line_clean)

with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\filtered_udfs.txt", "w", encoding="utf-8") as out:
    for p, items in matched_by_pattern.items():
        out.write(f"\n==================== Pattern: {p} (Count: {len(items)}) ====================\n")
        # remove duplicates
        seen = set()
        for item in items:
            if item not in seen:
                seen.add(item)
                out.write(item + "\n")

print("Saved to filtered_udfs.txt")
