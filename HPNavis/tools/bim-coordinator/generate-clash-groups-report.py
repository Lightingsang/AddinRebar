"""Write the priority-group analysis of the HP clash matrix (Nhóm 1/2/3 of sheet RuleClash(HP)) as Markdown.

Reads the embedded registry the engine uses (HPNavis.BIMCoordinator/Rules/hp-clash-matrix.json, generated from the
workbook by generate-clash-matrix.py) so the report and the clash tests can never disagree.

Usage:
    python generate-clash-groups-report.py [--matrix PATH] [--out PATH] [--missing A1,A3,M1,M3]
--missing lists the groups whose search set finds nothing in the model at hand (THCSLT by default): their rules are
reported as skipped, the way bim_sync_clash_tests skips them.
"""

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
DEFAULT_MATRIX = HERE.parent.parent / "HPNavis.BIMCoordinator" / "Rules" / "hp-clash-matrix.json"
LODS = ("200", "300", "350-400")
PHASE = {"200": "Thiết kế cơ sở / Basic Design", "300": "Thiết kế kỹ thuật / Technical Design",
         "350-400": "Bản vẽ thi công / Construction Drawings (cột G của sheet ghi \"350/400\")"}
SHEET_KEY = {"200": "200", "300": "300", "350-400": "350"}
MEANING = {
    1: ("HIGH", "Va chạm lớn ảnh hưởng phương án thiết kế và/hoặc tiến độ dự án.",
        "Phải giải quyết **trước buổi họp phối hợp** (coordination meeting)."),
    2: ("MEDIUM", "Va chạm ảnh hưởng thiết kế và/hoặc thi công nhưng mức độ thấp hơn Nhóm 1.",
        "Phải xử lý **trước khi trình nộp bản vẽ** (drawing submittal)."),
    3: ("LOW", "Các va chạm còn lại.",
        "**Cập nhật định kỳ** theo thiết kế và xử lý ngoài hiện trường."),
}


def table(header, rows):
    lines = ["| " + " | ".join(header) + " |", "|" + "|".join("---" for _ in header) + "|"]
    lines += ["| " + " | ".join(str(c) for c in row) + " |" for row in rows]
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--matrix", default=str(DEFAULT_MATRIX))
    parser.add_argument("--out", required=True)
    parser.add_argument("--missing", default="A1,A3,M1,M3")
    options = parser.parse_args()

    matrix = json.loads(Path(options.matrix).read_text(encoding="utf-8"))
    groups = {g["code"]: g for g in matrix["groups"]}
    rules = matrix["rules"]
    tolerance = matrix["toleranceMm"]
    missing = {code.strip() for code in options.missing.split(",") if code.strip()}
    for code in missing:
        if code not in groups:
            sys.exit(f"ERROR: --missing names unknown group {code}")

    def label(code):
        return f"{code} {groups[code]['nameVi']} ({groups[code]['name']})"

    out = ["# Ma trận va chạm HP — phân tích Nhóm 1 / 2 / 3", "",
           f"Nguồn: sheet `{matrix['source']['sheet']}` của `{matrix['source']['workbook']}` (sha256 `{matrix['source']['sha256'][:12]}…`), "
           "qua `Rules/hp-clash-matrix.json` mà engine dùng để tạo clash test.", ""]

    out += ["## 1. Nhóm là gì", "",
            "\"Nhóm\" là **mức ưu tiên** ghi trong từng ô của ma trận (số 1, 2 hoặc 3 tại giao của hai nhóm cấu kiện), "
            "không phải nhóm cấu kiện A/S/M. Ô trống = *tùy dự án hoặc không áp dụng* → không tạo test.", ""]
    out.append(table(["Nhóm", "Mức", "Ý nghĩa", "Thời hạn xử lý"],
                     [(f"Nhóm {p}", m[0], m[1], m[2]) for p, m in MEANING.items()]))
    out += ["", "Một cặp được kiểm ở một LOD khi **cả hai** nhóm cấu kiện ghi `Y` ở cột LOD đó. "
            "Tên test `HP|LOD<lod>|P<nhóm>|<bộ môn>|<A5 Doors>-<S2 Structural Walls>`: sắp Clash Detective theo tên là đọc theo LOD rồi theo Nhóm "
            "(API Navisworks 2026 không tạo được folder test — mọi loại folder bị từ chối \"unsupported type of SavedItem\").", ""]

    out += ["## 2. LOD và dung sai", ""]
    out.append(table(["LOD", "Giai đoạn", "Dung sai", "Số cặp", "Nhóm 1", "Nhóm 2", "Nhóm 3"],
                     [(lod, PHASE[lod], f"{tolerance[SHEET_KEY[lod]]} mm", len(rs := [r for r in rules if r["lod"][SHEET_KEY[lod]]]),
                       *(sum(r["priority"] == p for r in rs) for p in (1, 2, 3))) for lod in LODS]))
    out += ["", "- Dung sai lấy từ ghi chú của sheet (50/30/10 mm). LOD350 và LOD400 là **một giai đoạn** (cột G ghi \"350/400\"), "
            "một bộ test `LOD350-400` @ 10 mm.",
            "- Mỗi model chỉ giữ bộ test của giai đoạn hồ sơ hiện tại (skill `/hp-bim-clash-phase co-so|ky-thuat|thi-cong`); "
            "test HP của giai đoạn khác bị xoá sau khi duyệt.", ""]

    out += ["## 3. Nhóm cấu kiện và LOD được kiểm", ""]
    out.append(table(["Mã", "Cấu kiện", "Bộ môn", "200", "300", "350/400", "Số cặp tham gia"],
                     [(c, f"{g['nameVi']} / {g['name']}", g["discipline"], *("Y" if g["lod"][l] else "N" for l in ("200", "300", "350")),
                       sum(c in (r["left"], r["right"]) for r in rules)) for c, g in groups.items()]))
    out.append("")

    for lod in LODS:
        rs = [r for r in rules if r["lod"][SHEET_KEY[lod]]]
        out += [f"## 4.{LODS.index(lod) + 1} LOD{lod} @ {tolerance[SHEET_KEY[lod]]} mm — {PHASE[lod].split(' / ')[0]} — {len(rs)} cặp", ""]
        pairs = sorted({r["disciplinePair"] for r in rs})
        out.append(table(["Cặp bộ môn"] + [f"Nhóm {p}" for p in (1, 2, 3)] + ["Tổng"],
                         [(dp, *(sum(r["disciplinePair"] == dp and r["priority"] == p for r in rs) for p in (1, 2, 3)),
                           sum(r["disciplinePair"] == dp for r in rs)) for dp in pairs]))
        out.append("")
        for p in (1, 2, 3):
            chosen = [r for r in rs if r["priority"] == p]
            out += [f"**Nhóm {p} – {MEANING[p][0]} ({len(chosen)} cặp)**", ""]
            out += [f"- `{r['left']}-{r['right']}` {label(r['left'])} × {label(r['right'])} — {r['disciplinePair']}"
                    + ("  _(skip trên THCSLT: set rỗng)_" if {r['left'], r['right']} & missing else "") for r in chosen]
            out.append("")

    out += ["## 5. Áp lên THCSLT", "",
            f"Set rỗng trên model: {', '.join(sorted(missing))} → cặp có một trong các nhóm này bị **skip** (không tạo test, "
            "không mở rộng sang cả model).", ""]
    out.append(table(["LOD", "Cặp", "Tạo", "Skip"],
                     [(lod, len(rs := [r for r in rules if r["lod"][SHEET_KEY[lod]]]),
                       sum(not ({r['left'], r['right']} & missing) for r in rs),
                       sum(bool({r['left'], r['right']} & missing) for r in rs)) for lod in LODS]))
    out.append("")

    out += ["## 6. Nhận xét về sheet", ""]
    self_pairs = [r for r in rules if r["left"] == r["right"]]
    lod200_off = [c for c, g in groups.items() if not g["lod"]["200"]]
    only_350 = [c for c, g in groups.items() if not g["lod"]["300"] and g["lod"]["350"]]
    by_group = Counter(c for r in rules for c in {r["left"], r["right"]})
    out += [
        f"- {len(lod200_off)}/21 nhóm ghi `N` ở LOD200 ({', '.join(lod200_off)}) → LOD200 chỉ còn {sum(r['lod']['200'] for r in rules)} cặp, "
        "chỉ giữa các nhóm " + ", ".join(label(c) for c, g in groups.items() if g["lod"]["200"]) + ".",
        f"- Nhóm chỉ kiểm từ LOD350: {', '.join(only_350)} (nội thất, chiếu sáng).",
        f"- {len(self_pairs)} cặp tự va chạm cùng nhóm ({', '.join(r['left'] + '-' + r['right'] for r in self_pairs)}).",
        f"- Nhóm tham gia nhiều cặp nhất: {', '.join(f'{c} ({n})' for c, n in by_group.most_common(4))}; ít nhất: "
        f"{', '.join(f'{c} ({n})' for c, n in sorted(by_group.items(), key=lambda kv: kv[1])[:3])}.",
        "- Ghi chú phương pháp 3 của sheet: không kiểm ống gió mềm, ống mềm và ống D < 32 mm. Ống mềm bị loại theo category; "
        "các cặp có M4 chọn set `M4.9 Clash scope - pipes D >= 32 mm` (ống D ≥ 32 mm + toàn bộ phụ kiện, thiết bị PCCC, sprinkler) "
        "thay cho M4 — 1 479 ống nhỏ của THCSLT không vào test.",
        "- Ghi chú 1 (lỗ mở sàn KT/KC qua góc nhìn đỉnh/đáy) và 2 (kiểm mọi phần tử trong phòng máy) không phải cặp ma trận → không sinh test.",
        ""]

    Path(options.out).write_text("\n".join(out), encoding="utf-8", newline="\n")
    print(f"written {options.out}")


if __name__ == "__main__":
    main()
