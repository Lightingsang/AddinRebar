"""Regenerate the colour-set registry HPNavis.BIMCoordinator embeds (Colors/hp-color-sets.json).

Names, order, discipline group, purpose and RGB come from sheet "ColorSearchSet(DSC)" of the company workbook
(HPBIM_MaTranKiemSoatVaCham.xlsx beside this script); which elements each set takes comes from the hand-written
color-set-mapping.json beside it. Every sheet row needs a mapping entry and every mapping entry a sheet row, so a
renamed or added colour set stops the script instead of silently losing its colour. "<Default>" = no override.

Usage:
    python generate-color-sets.py [--xlsx PATH] [--mapping PATH] [--out PATH]
The output is deterministic, so regenerating without a source change leaves git clean.
"""

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

import openpyxl

SHEET = "ColorSearchSet(DSC)"
HERE = Path(__file__).resolve().parent
DEFAULT_XLSX = HERE / "HPBIM_MaTranKiemSoatVaCham.xlsx"
DEFAULT_MAPPING = HERE / "color-set-mapping.json"
DEFAULT_OUT = HERE.parent.parent / "HPNavis.BIMCoordinator" / "Colors" / "hp-color-sets.json"
CODE_PATTERN = re.compile(r"^C\d{2,3}$")
RGB_PATTERN = re.compile(r"^\s*(\d{1,3})\s*-\s*(\d{1,3})\s*-\s*(\d{1,3})\s*$")
MAPPING_KEYS = {"code", "discipline", "roles", "categories", "conditions", "alsoCategories", "verified", "pending", "evidence"}


def fail(message):
    print(f"ERROR: {message}", file=sys.stderr)
    sys.exit(1)


def read_rows(sheet):
    """Rows 3.. of columns A (no.), B (system, merged downwards), C (set name), D (purpose), E (RGB)."""
    rows, system = [], None
    for row in range(3, sheet.max_row + 1):
        name = str(sheet[f"C{row}"].value or "").strip()
        if not name:
            continue
        if not name.startswith("HP_"):
            fail(f"C{row}: '{name}' is not an HP_ colour set name")
        system = str(sheet[f"B{row}"].value or "").strip() or system
        rgb_text = str(sheet[f"E{row}"].value or "").strip()
        if rgb_text == "<Default>":
            rgb = None
        else:
            match = RGB_PATTERN.match(rgb_text)
            if not match:
                fail(f"E{row}: '{rgb_text}' is neither R-G-B nor <Default>")
            rgb = [int(part) for part in match.groups()]
            if any(channel > 255 for channel in rgb):
                fail(f"E{row}: '{rgb_text}' has a channel above 255")
        rows.append({"row": row, "system": system, "name": name, "purpose": str(sheet[f"D{row}"].value or "").strip(), "rgb": rgb})
    names = [r["name"] for r in rows]
    if len(names) != len(set(names)):
        fail("a colour set name appears twice in the sheet")
    return rows


def build(rows, mapping):
    missing = [r["name"] for r in rows if r["name"] not in mapping]
    extra = [name for name in mapping if name not in {r["name"] for r in rows}]
    if missing:
        fail(f"no mapping for: {', '.join(missing)}")
    if extra:
        fail(f"mapping entries not in the sheet: {', '.join(extra)}")
    sets, codes = [], set()
    for order, r in enumerate(rows, start=1):
        entry = mapping[r["name"]]
        unknown = set(entry) - MAPPING_KEYS
        if unknown:
            fail(f"{r['name']}: unknown mapping key(s) {', '.join(sorted(unknown))}")
        for key in ("code", "discipline", "categories"):
            if key not in entry:
                fail(f"{r['name']}: mapping key '{key}' is required")
        # the code is the set's approval handle (allowUpdate codes, paint codes): it lives in the mapping so a row
        # inserted in the sheet never renumbers the sets below it
        code = entry["code"]
        if not CODE_PATTERN.match(code):
            fail(f"{r['name']}: code '{code}' is not C followed by two or three digits")
        if code in codes:
            fail(f"{r['name']}: code '{code}' is used by another colour set")
        codes.add(code)
        item = {
            "code": code,
            "displayName": r["name"],
            "name": r["name"],
            "order": order,
            "system": r["system"],
            "purpose": r["purpose"],
            "rgb": r["rgb"],
            "discipline": entry["discipline"],
            "roles": entry.get("roles", []),
            "categories": entry["categories"],
            "conditions": entry.get("conditions", []),
            "alsoCategories": entry.get("alsoCategories", []),
            "verified": bool(entry.get("verified", False)),
            "evidence": entry.get("evidence", ""),
            "sourceCell": f"C{r['row']}",
        }
        if entry.get("pending"):
            item["pending"] = entry["pending"]
        sets.append(item)
    return sets


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--xlsx", default=str(DEFAULT_XLSX))
    parser.add_argument("--mapping", default=str(DEFAULT_MAPPING))
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    options = parser.parse_args()

    workbook = Path(options.xlsx)
    rows = read_rows(openpyxl.load_workbook(workbook, data_only=True)[SHEET])
    mapping_path = Path(options.mapping)
    mapping = json.loads(mapping_path.read_text(encoding="utf-8"))["sets"]
    document = {
        "schemaVersion": 1,
        "source": {
            "workbook": workbook.name,
            "sheet": SHEET,
            "sha256": hashlib.sha256(workbook.read_bytes()).hexdigest(),
            "mapping": mapping_path.name,
        },
        "folder": "Color",
        "sets": build(rows, mapping),
    }
    out = Path(options.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    sets = document["sets"]
    print(f"{len(sets)} colour sets: {sum(s['rgb'] is not None for s in sets)} coloured, "
          f"{sum(s['rgb'] is None for s in sets)} <Default>, {sum('pending' in s for s in sets)} pending, "
          f"{sum(s['verified'] for s in sets)} verified")
    print(f"written {out}")


if __name__ == "__main__":
    main()
