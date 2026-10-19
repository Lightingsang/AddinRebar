import openpyxl
import sys
import os

out_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\excel_template_analysis.txt"

files = [
    r"C:\kata_pro\Kata.xlsm",
    r"C:\kata_pro\Kata ENG.xlsm"
]

with open(out_path, "w", encoding="utf-8") as out:
    for fpath in files:
        out.write("=" * 80 + "\n")
        out.write(f"FILE: {fpath}\n")
        out.write("=" * 80 + "\n")
        wb = openpyxl.load_workbook(fpath, data_only=False, keep_vba=True)
        out.write(f"Sheets: {wb.sheetnames}\n\n")
        
        for sname in wb.sheetnames:
            ws = wb[sname]
            out.write(f"\n{'#' * 40}\n")
            out.write(f"SHEET: {sname} (max_row={ws.max_row}, max_column={ws.max_column})\n")
            out.write(f"{'#' * 40}\n")
            
            # Print rows up to 40
            for r in range(1, min(ws.max_row + 1, 40)):
                row_vals = []
                for c in range(1, min(ws.max_column + 1, 30)):
                    val = ws.cell(row=r, column=c).value
                    if val is not None:
                        col_letter = openpyxl.utils.get_column_letter(c)
                        # Check formula vs value
                        row_vals.append(f"{col_letter}{r}: {val}")
                if row_vals:
                    out.write(f"Row {r:3d}: " + " | ".join(row_vals) + "\n")

print(f"Inspection written to {out_path}")
