import json

beams_spec = [
    {"type": "GM-1", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 8, "role": "Dầm giằng dọc trục chính"},
    {"type": "GM-2", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 8, "role": "Dầm giằng dọc trục chính"},
    {"type": "GM-3", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng ngang trục biên"},
    {"type": "GM-4", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng ngang trục biên"},
    {"type": "GM-4b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng phụ"},
    {"type": "GM-5", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính qua hội trường (nhịp lớn)"},
    {"type": "GM-5a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng ngang trục hội trường"},
    {"type": "GM-5b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng ngang trục hội trường"},
    {"type": "GM-6", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng ngang trục sảnh"},
    {"type": "GM-7", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 6, "role": "Dầm giằng ngang trục sảnh & phụ"},
    {"type": "GM-8", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 2, "role": "Dầm giằng chính qua hội trường (nhịp 12.95m)"},
    {"type": "GM-9", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính qua hội trường (nhịp 7.5m)"},
    {"type": "GM-9a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 1, "role": "Dầm giằng ngang nhịp 5.85m"},
    {"type": "GM-10", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính qua hội trường"},
    {"type": "GM-10a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 5, "role": "Dầm giằng phụ sảnh"},
    {"type": "GM-10b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 3, "role": "Dầm giằng phụ sảnh"},
    {"type": "GM-11", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 1, "role": "Dầm giằng dọc"},
    {"type": "GM-12", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng dọc"},
    {"type": "GM-13", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 1, "role": "Dầm giằng dọc"},
    {"type": "GM-14", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng dọc trục hành lang (nhánh chính 300x400)"},
    {"type": "GM-15", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 5, "role": "Dầm giằng ngang khu vệ sinh & phụ trợ"},
    {"type": "GM-16", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính sân khấu"},
    {"type": "GM-16a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng ngang sân khấu (L=10.5m)"},
    {"type": "GM-17", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng ngang hậu đài (L=5.05m)"},
    {"type": "GM-18", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính sau sân khấu (L=8.4m)"},
    {"type": "GM-18a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 3, "role": "Dầm giằng dọc hậu trường"},
    {"type": "GM-18b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng dọc hậu trường"},
    {"type": "GM-19", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 1, "role": "Dầm giằng chính cánh gà"},
    {"type": "GM-19a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng ngang cánh gà"},
    {"type": "GM-19b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng ngang cánh gà"},
    {"type": "GM-20", "bMm": 300, "hMm": 600, "topZ": -1.600, "botZ": -2.200, "spanCount": 3, "role": "Dầm giằng chính trục biên"},
    {"type": "GM-20a", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng dọc trục biên"},
    {"type": "GM-20b", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Dầm giằng dọc trục biên"},
    {"type": "GM-21", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 18, "role": "Dầm giằng liên kết đài móng chu vi & khu phụ"},
    {"type": "GM-22", "bMm": 300, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 4, "role": "Dầm giằng dọc trục cuối (L=18.8m)"},
    {"type": "BN-1", "bMm": 200, "hMm": 300, "topZ": -1.600, "botZ": -1.900, "spanCount": 17, "role": "Bó nền đỡ tường tầng 1 (tổng chiều dài ~66m)"},
    {"type": "BN-2", "bMm": 200, "hMm": 400, "topZ": -1.600, "botZ": -2.000, "spanCount": 2, "role": "Bó nền đỡ tường khu phụ (tổng chiều dài ~14.8m)"}
]

out_file = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_22_grade_beams.json"
with open(out_file, "w", encoding="utf-8") as f:
    json.dump({
        "totalTypes": len(beams_spec),
        "totalSpans": sum(b["spanCount"] for b in beams_spec),
        "topElevation": -1.600,
        "classification": "NHOM_1",
        "beams": beams_spec
    }, f, indent=2, ensure_ascii=False)

print(f"Wrote {out_file} with {len(beams_spec)} beam types!")
