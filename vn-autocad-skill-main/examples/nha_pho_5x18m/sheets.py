# -*- coding: utf-8 -*-
"""Cac to ban ve phan kien truc (KT) va ham dung chung cho moi to.

Tieu de tung hinh chieu ve trong model space ngay duoi hinh (xem model.py),
giong cach ho so that lam. Tren giay chi con khung ten, ghi chu va bang bieu.
Cac to ket cau va co dien nam o sheets_ky_thuat.py.
"""

from vncad import sheet as sh, table

from . import layout as L
from .draw_finishes import MA_VAT_LIEU, NOTES_LAT_GACH, NOTES_TRAN
from .draw_site_plan import CHI_TIEU, NOTES_TONG_MB
from .draw_stair import NOTES_THANG
from .draw_waterproof import (CHU_GIAI_BAN_CONG, CHU_GIAI_SENO,
                              CHU_GIAI_WC, NOTES_CHONG_THAM)
from .draw_wc import NOTES_WC
from .sheet_notes import NOTES_MAI, NOTES_MAT_NGOAI

SHEET, PAPER = "A3", "1/1"

# Vung dat cua so nhin tren giay (mm): (tam_x, tam_y, rong, cao)
VP_TOAN_TRAI = (131.0, 148.5, 218.0, 273.0)     # cot trai cao het to, tranh khung ten
VP_NUA_TREN = (131.0, 217.5, 218.0, 135.0)      # nua tren to, chua bang o duoi


def _info(ten_ban_ve, so_hieu, ty_le="1/100", hang_muc="Kien truc"):
    """Gop thong tin khung ten cho mot to."""
    d = dict(L.TITLE_INFO)
    d.update({"ten_ban_ve": ten_ban_ve, "so_hieu": so_hieu,
              "ty_le": ty_le, "hang_muc": hang_muc})
    return d


def _view(lay, rect, center, scale="1/100"):
    """Mo mot cua so nhin. rect la (tam_x, tam_y, rong, cao) tren giay."""
    cx, cy, w, h = rect
    return sh.add_view(lay, (cx, cy), (w, h), center, scale)


def build_sheets(doc, c):
    """Tao het cac to. c la dict tam vung hinh lay tu regions.centers()."""
    from .sheets_ky_thuat import build_ky_thuat
    for ham in (_kt01, _kt02, _kt03, _kt04, _kt05, _kt06, _kt07, _kt08,
                _kt09, _kt10, _kt11, _kt12, _kt13, _kt14, _kt15):
        ham(doc, c)
    ten_ky_thuat, tong_thep = build_ky_thuat(doc, c)
    sh.drop_default_layouts(doc)
    return ["KT-%02d" % i for i in range(1, 16)] + ten_ky_thuat, tong_thep


def _kt01(doc, c):
    lay = sh.new_sheet(doc, "KT-01",
                       _info("MAT BANG KIEN TRUC TANG 1, TANG 2", "KT-01"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["kt01"])
    sh.note_panel(lay, 248, 280, "Ghi chu ky thuat thi cong", L.NOTES, width=155)


def _kt02(doc, c):
    lay = sh.new_sheet(doc, "KT-02",
                       _info("MAT BANG TANG 3 VA MAT BANG MAI", "KT-02"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["kt02"])
    sh.note_panel(lay, 248, 280, "Ghi chu mai va chong tham", NOTES_MAI, width=155)


def _kt03(doc, c):
    """Mat cat chiem cot trai cao het to, mat dung nam cot phai tren khung ten."""
    lay = sh.new_sheet(doc, "KT-03",
                       _info("MAT CAT A-A VA MAT DUNG CHINH", "KT-03"), SHEET)
    _view(lay, (134.0, 148.5, 224.0, 273.0), c["mat_cat"])
    _view(lay, (327.0, 182.0, 152.0, 206.0), c["mat_dung"])


def _kt04(doc, c):
    """Khai trien WC2: mat bang va 4 mat tuong op gach, cung ty le 1/50."""
    lay = sh.new_sheet(doc, "KT-04",
                       _info("KHAI TRIEN PHONG VE SINH WC 2", "KT-04", "1/50"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["wc"], "1/50")
    sh.note_panel(lay, 248, 280, "Ghi chu khai trien ve sinh", NOTES_WC, width=155)


def _kt05(doc, c):
    """Khai trien cau thang: mat bang, mat cat 1/50 va chi tiet bac 1/20."""
    lay = sh.new_sheet(doc, "KT-05",
                       _info("KHAI TRIEN CAU THANG BO VA CHI TIET LAN CAN", "KT-05",
                             "1/50, 1/20, 1/10"), SHEET)
    _view(lay, (78.0, 232.0, 108.0, 100.0), c["thang_mb"], "1/50")
    _view(lay, (222.0, 232.0, 160.0, 100.0), c["thang_mc"], "1/50")
    _view(lay, (100.0, 108.0, 152.0, 106.0), c["bac_thang"], "1/20")
    _view(lay, (352.0, 178.0, 98.0, 176.0), c["lan_can"], "1/10")
    sh.note_panel(lay, 186, 140, "Ghi chu cau thang", NOTES_THANG, width=108)


def _kt06(doc, c):
    """Mat dung ben nhin theo chieu sau nha, truc 1 den truc 5."""
    lay = sh.new_sheet(doc, "KT-06", _info("MAT DUNG BEN TRUC 1-5", "KT-06"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["mat_dung_ben"])
    sh.note_panel(lay, 248, 280, "Ghi chu vat lieu mat ngoai", NOTES_MAT_NGOAI,
                  width=155)


def _kt07(doc, c):
    """Mat bang lat gach tang 1 va tang 2, kem bang ma vat lieu."""
    lay = sh.new_sheet(doc, "KT-07",
                       _info("MAT BANG LAT GACH TANG 1, TANG 2", "KT-07"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["gach"])
    table.draw(lay, (248.0, 275.0),
               [("MA", 16, "c"), ("QUY CACH VA VI TRI LAT", 139, "l")],
               [[m, t] for m, t in MA_VAT_LIEU], "BANG MA VAT LIEU LAT", row_h=8.0)
    sh.note_panel(lay, 248, 215, "Ghi chu lat gach", NOTES_LAT_GACH, width=155)


def _kt08(doc, c):
    lay = sh.new_sheet(doc, "KT-08",
                       _info("MAT BANG TRAN TANG 1, TANG 2", "KT-08"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["tran"])
    sh.note_panel(lay, 248, 280, "Ghi chu tran thach cao", NOTES_TRAN, width=155)


def _kt09(doc, c):
    """Bang thong ke cua: ve thang tren giay, khong can cua so nhin."""
    lay = sh.new_sheet(doc, "KT-09",
                       _info("BANG THONG KE CUA DI VA CUA SO", "KT-09", "-"), SHEET)
    table.door_schedule(lay, (24.0, 270.0), L.DOORS)
    sh.note_panel(lay, 24, 160, "Ghi chu cua", L.NOTES_CUA, width=200)


def _kt10(doc, c):
    """Mat cat B-B cat ngang nha theo be rong 5m."""
    lay = sh.new_sheet(doc, "KT-10", _info("MAT CAT B-B", "KT-10"), SHEET)
    _view(lay, (110.0, 148.5, 176.0, 273.0), c["mat_cat_bb"])
    sh.note_panel(lay, 248, 280, "Ghi chu mat cat", NOTES_MAT_NGOAI, width=155)


def _kt11(doc, c):
    """Khai trien WC1 tang tret, mat bang va 4 mat tuong."""
    lay = sh.new_sheet(doc, "KT-11",
                       _info("KHAI TRIEN PHONG VE SINH WC 1", "KT-11", "1/50"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["wc1"], "1/50")
    sh.note_panel(lay, 248, 280, "Ghi chu khai trien ve sinh", NOTES_WC, width=155)


def _kt12(doc, c):
    """Khai trien WC3 tang 3, mat bang va 4 mat tuong."""
    lay = sh.new_sheet(doc, "KT-12",
                       _info("KHAI TRIEN PHONG VE SINH WC 3", "KT-12", "1/50"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["wc3"], "1/50")
    sh.note_panel(lay, 248, 280, "Ghi chu khai trien ve sinh", NOTES_WC, width=155)


def _kt13(doc, c):
    """Mat bang lat gach va mat bang tran tang 3 tren cung mot to."""
    lay = sh.new_sheet(doc, "KT-13",
                       _info("MAT BANG LAT GACH VA TRAN TANG 3", "KT-13"), SHEET)
    _view(lay, VP_TOAN_TRAI, c["gach_tran_t3"])
    table.draw(lay, (248.0, 275.0),
               [("MA", 16, "c"), ("QUY CACH VA VI TRI LAT", 139, "l")],
               [[m, t] for m, t in MA_VAT_LIEU], "BANG MA VAT LIEU LAT", row_h=8.0)
    sh.note_panel(lay, 248, 215, "Ghi chu lat gach va tran", NOTES_TRAN, width=155)


CG_COLS = [("SO", 11, "c"), ("LOP VAT LIEU", 101, "l")]
CT_COLS = [("CHI TIEU QUY HOACH", 62, "l"), ("GIA TRI", 78, "l")]


def _kt14(doc, c):
    """Ba nut cau tao chong tham o 1/20, moi nut co bang chu giai lop rieng."""
    lay = sh.new_sheet(doc, "KT-14",
                       _info("CHI TIET CAU TAO CHONG THAM", "KT-14", "1/20"), SHEET)
    cum = ((c["ct_wc"], CHU_GIAI_WC, 235.0, 272.0, "CHU GIAI CT1"),
           (c["ct_seno"], CHU_GIAI_SENO, 150.0, 190.0, "CHU GIAI CT2"),
           (c["ct_ban_cong"], CHU_GIAI_BAN_CONG, 66.0, 108.0, "CHU GIAI CT3"))
    for tam, chu_giai, y_hinh, y_bang, tieu_de in cum:
        _view(lay, (86.0, y_hinh, 128.0, 78.0), tam, "1/20")
        table.draw(lay, (158.0, y_bang), CG_COLS,
                   [[a, b] for a, b in chu_giai], tieu_de, row_h=5.0)


def _kt15(doc, c):
    """Tong mat bang dinh vi o 1/200, kem bang chi tieu quy hoach."""
    lay = sh.new_sheet(doc, "KT-15",
                       _info("TONG MAT BANG DINH VI", "KT-15", "1/200"), SHEET)
    _view(lay, VP_NUA_TREN, c["tong_mb"], "1/200")
    table.draw(lay, (24.0, 132.0), CT_COLS, CHI_TIEU, "BANG CHI TIEU QUY HOACH",
               row_h=5.5)
    sh.note_panel(lay, 248, 280, "Ghi chu dinh vi", NOTES_TONG_MB, width=155)
