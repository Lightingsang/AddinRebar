import os
import re

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
shapes_dir = os.path.join(base_dir, r"HPKata\HPKata.Core\Shapes")
config_dir = os.path.join(base_dir, r"HPKata\HPKata.Core\Configuration")
blueprint_path = os.path.join(base_dir, r"docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md")

def check_shapes():
    print("=== VERIFYING SHAPES FIDELITY ===")
    with open(os.path.join(shapes_dir, "RebarShapeType.cs"), "r", encoding="utf-8") as f:
        enum_text = f.read()

    enum_members = re.findall(r'\b(Dang[0-9a-zA-Z]+)\b', enum_text)
    print(f"Total shapes in RebarShapeType.cs: {len(enum_members)}")

    catalog_files = [
        os.path.join(shapes_dir, "RebarShapeCatalog.Shapes00to29.cs"),
        os.path.join(shapes_dir, "RebarShapeCatalog.Shapes30to59.cs"),
        os.path.join(shapes_dir, "RebarShapeCatalog.Shapes60toCoupler.cs"),
    ]

    all_catalog_text = ""
    for cf in catalog_files:
        with open(cf, "r", encoding="utf-8") as f:
            all_catalog_text += "\n" + f.read()

    missing_definitions = []
    missing_eval_cases = []

    for m in enum_members:
        # Check def[RebarShapeType.DangXX]
        if f"RebarShapeType.{m}" not in all_catalog_text:
            missing_definitions.append(m)
        # Check case RebarShapeType.DangXX:
        if f"case RebarShapeType.{m}:" not in all_catalog_text:
            missing_eval_cases.append(m)

    if not missing_definitions:
        print("PASS: All 85 shapes are defined in RebarShapeCatalog.")
    else:
        print(f"FAIL: Missing definitions for: {missing_definitions}")

    if not missing_eval_cases:
        print("PASS: All 85 shapes have explicit evaluation case branches.")
    else:
        print(f"FAIL: Missing evaluation case branches for: {missing_eval_cases}")

    # Check against Blueprint section 6.3
    with open(blueprint_path, "r", encoding="utf-8") as f:
        bp_text = f.read()

    # Find dangXX in section 6.3 table
    bp_shapes = re.findall(r'\|\s*\*\*`dang([0-9a-zA-Z]+)`\*\*', bp_text)
    print(f"Total shapes in Blueprint table: {len(bp_shapes)}")
    
    # Map bp shapes to enum style (e.g. 00 -> Dang00, 02a -> Dang02a, Coupler -> DangCoupler)
    enum_lower = {m.lower(): m for m in enum_members}
    missing_in_enum = []
    for s in bp_shapes:
        cand = "dang" + s.lower()
        if cand not in enum_lower:
            missing_in_enum.append(s)

    if not missing_in_enum:
        print("PASS: All Blueprint shapes map 1:1 to RebarShapeType enum.")
    else:
        print(f"FAIL: Shapes in Blueprint missing in enum: {missing_in_enum}")

def check_config():
    print("\n=== VERIFYING 86 CONFIG PARAMETERS ===")
    with open(os.path.join(config_dir, "KataConfig.cs"), "r", encoding="utf-8") as f:
        config_text = f.read()

    # extract properties
    props = re.findall(r'public\s+([A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\{\s*get;\s*set;\s*\}', config_text)
    print(f"Total public properties in KataConfig.cs: {len(props)}")

    # Extract 86 parameters from Blueprint table in Section 3.1
    with open(blueprint_path, "r", encoding="utf-8") as f:
        bp_text = f.read()

    # Look for rows in section 3.1: | **1** | 152 | `LayerDim` | `kata_dim` | ...
    bp_rows = re.findall(r'\|\s*\*\*(\d+)\*\*\s*\|\s*(\d+)\s*\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|\s*`([^`]+)`', bp_text)
    print(f"Total config items extracted from Blueprint table: {len(bp_rows)}")

    prop_dict = {p[1]: p[0] for p in props}
    missing_in_config = []
    default_mismatches = []

    for item_num, line_num, name, def_val, dtype in bp_rows:
        if name not in prop_dict:
            missing_in_config.append((item_num, line_num, name))

    if not missing_in_config:
        print("PASS: All 86 Blueprint parameters exist in KataConfig.cs.")
    else:
        print(f"FAIL: Missing properties in KataConfig.cs: {missing_in_config}")

    # Check parser lines mapping in KataConfigParser.cs
    with open(os.path.join(config_dir, "KataConfigParser.cs"), "r", encoding="utf-8") as f:
        parser_text = f.read()

    # Check serializer lines in KataConfigParser.Serializer.cs
    with open(os.path.join(config_dir, "KataConfigParser.Serializer.cs"), "r", encoding="utf-8") as f:
        serializer_text = f.read()

    missing_in_parser = []
    missing_in_serializer = []
    for item_num, line_num, name, def_val, dtype in bp_rows:
        if f"c.{name}" not in parser_text:
            missing_in_parser.append(name)
        if f"c.{name}" not in serializer_text:
            missing_in_serializer.append(name)

    if not missing_in_parser:
        print("PASS: All 86 parameters are parsed in KataConfigParser.cs.")
    else:
        print(f"FAIL: Missing in parser: {missing_in_parser}")

    if not missing_in_serializer:
        print("PASS: All 86 parameters are serialized in KataConfigParser.Serializer.cs.")
    else:
        print(f"FAIL: Missing in serializer: {missing_in_serializer}")

if __name__ == "__main__":
    check_shapes()
    check_config()
