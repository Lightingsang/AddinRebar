"""Regenerate the HP clash matrix JSON that HPNavis.BIMCoordinator embeds.

Source of truth: sheet "RuleClash(HP)" of HPBIM_MaTranKiemSoatVaCham.xlsx (the company workbook, copied
beside this script). The sheet is a triangular matrix: rows A1..A9 / S1..S5 / M1..M7 (code in column D,
LOD Y/N in E/F/G), columns S1..S5 / M1..M7 / A1..A9 (codes in row 4). Each pair is written once, in the
row of its "earlier" group (S rows hold S/M/A columns, M rows hold M/A, A rows hold A only); the empty
lower-left part of the A rows carries the legend (priority swatches, notes), which is why only the
data region per row group is read. A cell holds 1/2/3 (HIGH/MEDIUM/LOW) or nothing.

A pair is eligible at a LOD when both of its groups say Y for that LOD; column G covers 350 and 400.
Tolerances come from the note "LOD 200: 50mm / LOD 300: 30mm / LOD 350: 10mm"; LOD 400 has none and
stays null (the engine refuses it until a tolerance is approved).

Usage:
    python generate-clash-matrix.py [--xlsx PATH] [--out PATH] [--compare chatgpt-ruleclash.json]
The output is deterministic (sorted keys, fixed rule order), so a regenerate without a workbook change
leaves git clean. Any unexpected cell content stops the script: a wrong matrix must never be embedded.
"""

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

import openpyxl
from openpyxl.utils import get_column_letter

SHEET = "RuleClash(HP)"
HERE = Path(__file__).resolve().parent
DEFAULT_XLSX = HERE / "HPBIM_MaTranKiemSoatVaCham.xlsx"
DEFAULT_OUT = HERE.parent.parent / "HPNavis.BIMCoordinator" / "Rules" / "hp-clash-matrix.json"

DISCIPLINE_BY_PREFIX = {"A": "ARC", "S": "STR", "M": "MEP"}
# which column groups hold data in a row of a given group (the matrix is written once per pair)
DATA_COLUMNS_BY_ROW_PREFIX = {"S": {"S", "M", "A"}, "M": {"M", "A"}, "A": {"A"}}
LOD_COLUMNS = {"200": "E", "300": "F", "350": "G", "400": "G"}
CODE_PATTERN = re.compile(r"^[ASM][1-9]$")
TOLERANCE_PATTERN = re.compile(r"LOD\s*(\d{3})\s*:\s*(\d+)\s*mm", re.IGNORECASE)


def fail(message):
    print(f"ERROR: {message}", file=sys.stderr)
    sys.exit(1)


def code_key(code):
    # A < M < S: the same order as the discipline pair label (ARC < MEP < STR), so HP_M2_S2 reads as MEP-STR
    return ("AMS".index(code[0]), int(code[1:]))


def split_names(text):
    """Column C holds 'Vietnamese\\nEnglish' (or the reverse); a line with a non-ASCII letter is Vietnamese."""
    lines = [line.strip() for line in str(text or "").splitlines() if line.strip()]
    vietnamese = [line for line in lines if any(ord(ch) > 127 for ch in line)]
    english = [line for line in lines if line not in vietnamese]
    return (english[0] if english else ""), (vietnamese[0] if vietnamese else "")


def read_column_codes(sheet):
    codes = {}
    for cell in sheet[4]:
        value = str(cell.value or "").strip()
        if CODE_PATTERN.match(value):
            codes[cell.column] = value
    if len(codes) != 21:
        fail(f"row 4 should carry 21 group codes, found {len(codes)}")
    return codes


def read_groups(sheet):
    groups = {}
    for row in range(5, sheet.max_row + 1):
        code = str(sheet[f"D{row}"].value or "").strip()
        if not CODE_PATTERN.match(code):
            continue
        lod = {}
        for level, column in LOD_COLUMNS.items():
            flag = str(sheet[f"{column}{row}"].value or "").strip().upper()
            if flag not in ("Y", "N"):
                fail(f"{column}{row}: LOD flag for {code} is '{flag}', expected Y or N")
            lod[level] = flag == "Y"
        english, vietnamese = split_names(sheet[f"C{row}"].value)
        groups[code] = {"row": row, "name": english, "nameVi": vietnamese,
                        "discipline": DISCIPLINE_BY_PREFIX[code[0]], "lod": lod}
    if len(groups) != 21:
        fail(f"column D should carry 21 group codes, found {len(groups)}")
    return groups


def read_tolerances(sheet):
    found = {}
    for row in sheet.iter_rows():
        for cell in row:
            if isinstance(cell.value, str):
                for level, millimetres in TOLERANCE_PATTERN.findall(cell.value):
                    found[level] = int(millimetres)
    for level in ("200", "300", "350"):
        if level not in found:
            fail(f"no tolerance note for LOD {level}")
    return {"200": found["200"], "300": found["300"], "350": found["350"], "400": None}


def read_cells(sheet, groups, column_codes):
    pairs = {}
    for row_code, group in groups.items():
        allowed = DATA_COLUMNS_BY_ROW_PREFIX[row_code[0]]
        for column, column_code in column_codes.items():
            if column_code[0] not in allowed:
                continue
            ref = f"{get_column_letter(column)}{group['row']}"
            value = sheet[ref].value
            if value is None or (isinstance(value, str) and not value.strip()):
                continue
            if isinstance(value, float) and value.is_integer():
                value = int(value)
            if not isinstance(value, int) or value not in (1, 2, 3):
                fail(f"{ref}: '{value}' is not a priority 1/2/3")
            left, right = sorted((row_code, column_code), key=code_key)
            key = (left, right)
            if key in pairs:
                if pairs[key]["priority"] != value:
                    fail(f"{ref}: pair {left}-{right} written twice with priorities {pairs[key]['priority']} and {value}")
                pairs[key]["cells"].append(ref)
            else:
                pairs[key] = {"priority": value, "cells": [ref]}
    return pairs


def build_rules(pairs, groups):
    rules = []
    for (left, right) in sorted(pairs, key=lambda k: (code_key(k[0]), code_key(k[1]))):
        disciplines = sorted((groups[left]["discipline"], groups[right]["discipline"]))
        lod = {level: groups[left]["lod"][level] and groups[right]["lod"][level] for level in LOD_COLUMNS}
        rules.append({
            "id": f"HP_{left}_{right}",
            "left": left,
            "right": right,
            "disciplinePair": "-".join(disciplines),
            "priority": pairs[(left, right)]["priority"],
            "cells": pairs[(left, right)]["cells"],
            "lod": lod,
        })
    return rules


def compare(rules, path):
    """Differences against the ChatGPT starter JSON (its rule_id orders codes by plain string sort)."""
    other = json.loads(Path(path).read_text(encoding="utf-8"))["rules"]
    theirs = {tuple(sorted((r["left_code"], r["right_code"]))): r for r in other}
    ours = {tuple(sorted((r["left"], r["right"]))): r for r in rules}
    differences = []
    for key in sorted(set(theirs) | set(ours)):
        a, b = ours.get(key), theirs.get(key)
        if a is None or b is None:
            differences.append(f"{'-'.join(key)}: only in {'starter' if a is None else 'workbook'}")
            continue
        if a["priority"] != b["priority"]:
            differences.append(f"{'-'.join(key)}: priority workbook {a['priority']} vs starter {b['priority']}")
        theirs_lod = {k: bool(v) for k, v in b["eligible_lod"].items()}
        if a["lod"] != theirs_lod:
            differences.append(f"{'-'.join(key)}: lod workbook {a['lod']} vs starter {theirs_lod}")
    return differences


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--xlsx", default=str(DEFAULT_XLSX))
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--compare", help="ChatGPT starter ruleclash_hp.json to diff against")
    options = parser.parse_args()

    workbook_path = Path(options.xlsx)
    sheet = openpyxl.load_workbook(workbook_path, data_only=True)[SHEET]
    groups = read_groups(sheet)
    rules = build_rules(read_cells(sheet, groups, read_column_codes(sheet)), groups)
    document = {
        "schemaVersion": 1,
        "source": {
            "workbook": workbook_path.name,
            "sheet": SHEET,
            "sha256": hashlib.sha256(workbook_path.read_bytes()).hexdigest(),
        },
        "priorityLabels": {"1": "HIGH", "2": "MEDIUM", "3": "LOW"},
        "toleranceMm": read_tolerances(sheet),
        "groups": [
            {"code": code, "name": g["name"], "nameVi": g["nameVi"], "discipline": g["discipline"], "lod": g["lod"]}
            for code, g in sorted(groups.items(), key=lambda item: code_key(item[0]))
        ],
        "rules": rules,
    }
    out = Path(options.out)
    text = json.dumps(document, ensure_ascii=False, indent=2, sort_keys=False) + "\n"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8", newline="\n")

    priorities = [sum(r["priority"] == p for r in rules) for p in (1, 2, 3)]
    per_lod = {level: sum(r["lod"][level] for r in rules) for level in LOD_COLUMNS}
    print(f"{len(rules)} rules, priorities P1/P2/P3 = {priorities}, eligible per LOD = {per_lod}")
    print(f"written {out}")
    if options.compare:
        differences = compare(rules, options.compare)
        print(f"{len(differences)} difference(s) against {Path(options.compare).name}")
        for line in differences:
            print("  " + line)


if __name__ == "__main__":
    main()
