# -*- coding: utf-8 -*-
"""Cac to ban ve ky thuat: phan ket cau (KC) va phan co dien (ME).

Tach rieng khoi sheets.py de moi file chi lo mot nhom to.
"""

from vncad import sheet as sh, struct, table

from .draw_electrical import KY_HIEU_DIEN, NOTES_DIEN, legend
from .draw_plumbing import KY_HIEU_NUOC, NOTES_NUOC
from .draw_rebar_detail import NOTES_THEP
from .draw_schematic import NOTES_SO_DO
from .draw_stair_structure import NOTES_KC_THANG, REBAR_ROWS_THANG
from .draw_struct_details import CHU_GIAI_NOI_COT, CHU_GIAI_SAN
from .draw_structure import REBAR_ROWS
from .foundation_rebar import REBAR_ROWS_MONG
from .sheet_notes import NOTES_KC
from .sheets import (CG_COLS, PAPER, SHEET, VP_NUA_TREN, VP_TOAN_TRAI,
                     _info, _view)

NOTES_NUT = [
    "Thep chu cot di suot qua nut khung, tuyet doi khong noi trong pham vi nut.",
    "Thep dam neo vao cot toi thieu 30 lan duong kinh, uon moc vuong 90 do dai 12D.",
    "Dai cot day a100 trong pham vi nut va 2h ve hai phia tinh tu mep dam.",
    "Dai dam day a100 trong pham vi 2h tinh tu mep cot.",
    "Be tong nut khung do lien khoi voi dam va san, khong de mach ngung tai nut.",
    "Dam bao lop bao ve 25mm o moi phia, ke con ke be tong truoc khi do.",
    "Kiem tra so luong va vi tri thep tai nut truoc khi nghiem thu cot pha.",
]


def build_ky_thuat(doc, c):
    """Tao cac to KC va ME. Tra ve (danh sach ten to, tong khoi luong thep)."""
    tong_thep = _kc01(doc, c)              # thep mong
    tong_thep += _kc02(doc, c)
    _kc03(doc, c)
    _kc04(doc, c)
    tong_thep += _kc05(doc, c)             # cong ca thep cau thang vao tong
    for ham in (_me01, _me02, _me03, _me04, _me05, _me06):
        ham(doc, c)
    return ["KC-01", "KC-02", "KC-03", "KC-04", "KC-05",
            "ME-01", "ME-02", "ME-03", "ME-04", "ME-05", "ME-06"], tong_thep


# ---------------------------------------------------------------------------
# PHAN KET CAU
# ---------------------------------------------------------------------------
def _kc01(doc, c):
    """Mat bang mong o nua tren, chi tiet dai mong 1/20 o goc duoi trai."""
    lay = sh.new_sheet(doc, "KC-01",
                       _info("MAT BANG MONG COC VA DA KIENG", "KC-01",
                             "1/100, 1/20", "Ket cau"), SHEET)
    _view(lay, VP_NUA_TREN, c["mong"])
    _view(lay, (72.0, 84.0, 98.0, 112.0), c["chi_tiet_m1"], "1/20")
    sh.view_caption(lay, 72.0, 21.0, "Chi tiet dai mong M1", "1/20")
    sh.note_panel(lay, 248, 280, "Ghi chu ket cau", NOTES_KC, width=155)
    _view(lay, (185.0, 54.0, 110.0, 56.0), c["mc_giang"], "1/20")
    sh.view_caption(lay, 185.0, 21.0, "Mat cat giang mong GM", "1/20")
    tong, _ = struct.rebar_schedule(lay, (125.0, 140.0), REBAR_ROWS_MONG, PAPER,
                                    title="BANG THONG KE THEP MONG", row_h=6.0)
    return tong


def _kc02(doc, c):
    """Mat bang dam san o nua tren, bang thong ke cot thep ve thang tren giay."""
    lay = sh.new_sheet(doc, "KC-02",
                       _info("KET CAU DAM SAN TANG 2 VA THONG KE THEP", "KC-02",
                             hang_muc="Ket cau"), SHEET)
    _view(lay, VP_NUA_TREN, c["dam_san"])
    # bang da du thep san nen dai hon, ha chieu cao dong de van nam tren khung ten
    tong, _ = struct.rebar_schedule(lay, (24.0, 140.0), REBAR_ROWS, PAPER,
                                    row_h=6.0)
    return tong


def _kc03(doc, c):
    """Khai trien dam D1 o 1/50, mat cat dam va cot o 1/25."""
    lay = sh.new_sheet(doc, "KC-03",
                       _info("CHI TIET THEP DAM D1, COT C1 VA NOI THEP COT", "KC-03",
                             "1/50, 1/25", "Ket cau"), SHEET)
    _view(lay, (131.0, 254.0, 218.0, 48.0), c["dam_d1"], "1/50")
    _view(lay, (70.0, 200.0, 90.0, 44.0), c["mc_dam"], "1/25")
    _view(lay, (200.0, 148.0, 90.0, 150.0), c["cot_c1"], "1/25")
    _view(lay, (86.0, 92.0, 128.0, 154.0), c["noi_cot"], "1/25")
    sh.note_panel(lay, 248, 280, "Ghi chu cot thep", NOTES_THEP, width=155)
    table.draw(lay, (248.0, 116.0), CG_COLS,
               [[a, b] for a, b in CHU_GIAI_NOI_COT], "CHU GIAI CT6", row_h=5.0)


def _kc04(doc, c):
    """Chi tiet nut khung dam cot ty le 1/20."""
    lay = sh.new_sheet(doc, "KC-04",
                       _info("CHI TIET NUT KHUNG VA THEP MU GOI SAN", "KC-04",
                             "1/20, 1/25", "Ket cau"), SHEET)
    _view(lay, (126.0, 202.0, 208.0, 140.0), c["nut_khung"], "1/20")
    _view(lay, (82.0, 70.0, 120.0, 106.0), c["thep_san"], "1/20")
    sh.note_panel(lay, 248, 280, "Ghi chu nut khung", NOTES_NUT, width=155)
    table.draw(lay, (248.0, 116.0), CG_COLS,
               [[a, b] for a, b in CHU_GIAI_SAN], "CHU GIAI CT5", row_h=5.0)


def _kc05(doc, c):
    """Ket cau cau thang: mat bang 1/50, mat cat ban thang 1/25, thong ke thep."""
    lay = sh.new_sheet(doc, "KC-05",
                       _info("KET CAU CAU THANG BO", "KC-05", "1/50, 1/25",
                             "Ket cau"), SHEET)
    _view(lay, (75.0, 205.0, 110.0, 118.0), c["thang_kc_mb"], "1/50")
    _view(lay, (245.0, 200.0, 196.0, 160.0), c["thang_kc_mc"], "1/25")
    tong, _ = struct.rebar_schedule(lay, (24.0, 130.0), REBAR_ROWS_THANG, PAPER,
                                    title="BANG THONG KE THEP CAU THANG")
    sh.note_panel(lay, 212, 112, "Ghi chu ket cau thang", NOTES_KC_THANG, width=150)
    return tong


# ---------------------------------------------------------------------------
# PHAN CO DIEN
# ---------------------------------------------------------------------------
def _me01(doc, c):
    lay = sh.new_sheet(doc, "ME-01",
                       _info("MAT BANG CAP DIEN CHIEU SANG TANG 1", "ME-01",
                             "1/100", "Co dien"), SHEET)
    _view(lay, (131.0, 195.0, 218.0, 180.0), c["dien"])
    sh.note_panel(lay, 248, 280, "Ghi chu he thong dien", NOTES_DIEN, width=155)
    legend(lay, doc, 248, 218, KY_HIEU_DIEN)


def _me02(doc, c):
    lay = sh.new_sheet(doc, "ME-02",
                       _info("MAT BANG CAP THOAT NUOC TANG 1", "ME-02",
                             "1/100", "Co dien"), SHEET)
    _view(lay, (131.0, 195.0, 218.0, 180.0), c["nuoc"])
    sh.note_panel(lay, 248, 280, "Ghi chu cap thoat nuoc", NOTES_NUOC, width=155)
    legend(lay, doc, 248, 218, KY_HIEU_NUOC)


def _me03(doc, c):
    lay = sh.new_sheet(doc, "ME-03",
                       _info("MAT BANG CAP DIEN TANG 2, TANG 3", "ME-03",
                             "1/100", "Co dien"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["dien23"])
    legend(lay, doc, 248, 280, KY_HIEU_DIEN)


def _me04(doc, c):
    lay = sh.new_sheet(doc, "ME-04",
                       _info("MAT BANG CAP THOAT NUOC TANG 2, TANG 3", "ME-04",
                             "1/100", "Co dien"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["nuoc23"])
    legend(lay, doc, 248, 280, KY_HIEU_NUOC)
    sh.note_panel(lay, 248, 225, "Ghi chu cap thoat nuoc", NOTES_NUOC, width=155)


def _me05(doc, c):
    """So do nguyen ly tu dien tong, khong ve theo ty le hinh hoc."""
    lay = sh.new_sheet(doc, "ME-05",
                       _info("SO DO NGUYEN LY TU DIEN TONG", "ME-05", "-",
                             "Co dien"), SHEET)
    _view(lay, (131.0, 148.5, 218.0, 273.0), c["sd_dien"], "1/75")
    sh.note_panel(lay, 248, 280, "Ghi chu so do", NOTES_SO_DO, width=155)


def _me06(doc, c):
    """So do khong gian cap thoat nuoc suot ba tang."""
    lay = sh.new_sheet(doc, "ME-06",
                       _info("SO DO KHONG GIAN CAP THOAT NUOC", "ME-06", "-",
                             "Co dien"), SHEET)
    _view(lay, (131.0, 148.5, 218.0, 273.0), c["sd_nuoc"], "1/75")
    sh.note_panel(lay, 248, 280, "Ghi chu so do", NOTES_SO_DO, width=155)
