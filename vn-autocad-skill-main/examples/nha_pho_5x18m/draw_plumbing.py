# -*- coding: utf-8 -*-
"""Mat bang cap thoat nuoc cho tung tang.

Tang 1 co dong ho nuoc, be tu hoai va ho ga. Tang tren chi co nhanh re tu
ong dung vao khu ve sinh, va cac tuyen thoat noi vao ong dung chung.
"""

from vncad import annotate as an, mep

from . import layout as L
from .draw_electrical import _nen

SC = "1/100"
X_ONG_DUNG = {1: 10200, 2: 11600, 3: 16600}      # vi tri ong dung tung tang
Y_ONG_DUNG = {1: 1200, 2: 1400, 3: 1300}


def draw_plumbing(msp, doc, origin=(0.0, 0.0), floor=1):
    """Mat bang cap thoat nuoc mot tang."""
    ox, oy = origin
    _nen(msp, ox, oy, floor)
    if floor == 1:
        _tang_tret(msp, doc, ox, oy)
    else:
        _tang_tren(msp, doc, ox, oy, floor)
    return "Mat bang cap thoat nuoc tang %d" % floor


def _tang_tret(msp, doc, ox, oy):
    """Tang tret: dong ho, van tong, tuyen cap chinh, thoat ve be tu hoai."""
    mep.place(msp, doc, "DONG-HO-NUOC", (ox + 300, oy + 4600), SC)
    an.leader_note(msp, "Dong ho nuoc + van khoa", (ox + 300, oy + 4600),
                   (ox + 2600, oy + 6200), SC)
    mep.place(msp, doc, "VAN-KHOA", (ox + 1200, oy + 4600), SC)
    mep.pipe(msp, [(ox + 600, oy + 4600), (ox + 14000, oy + 4600),
                   (ox + 14000, oy + 4200)], "cap", SC, "Ong cap PPR D25",
             label_at=(ox + 5600, oy + 4680))
    mep.pipe(msp, [(ox + 10200, oy + 4600), (ox + 10200, oy + 1400)], "cap", SC)
    _ong_dung(msp, doc, ox, oy, 1, "Ong dung cap nuoc len tang",
              (ox + 8900, oy + 2400))
    mep.pipe(msp, [(ox + 10100, oy + 900), (ox + 16500, oy + 900),
                   (ox + 16500, oy + 300)], "thoat", SC, "Thoat bon cau D114",
             label_at=(ox + 10500, oy + 180))
    mep.pipe(msp, [(ox + 11000, oy + 400), (ox + 15900, oy + 400),
                   (ox + 15900, oy + 4400)], "thoat", SC, "Thoat san D60",
             label_at=(ox + 13100, oy - 320))
    mep.place(msp, doc, "HO-GA", (ox + 17200, oy + 300), SC)
    an.leader_note(msp, "Ho ga 800x800 + be phot 3 ngan",
                   (ox + 17200, oy + 300), (ox + 13400, oy + 6200), SC)


def _tang_tren(msp, doc, ox, oy, floor):
    """Tang tren: nhanh cap re vao WC, thoat noi ve ong dung xuong tang duoi."""
    xd, yd = X_ONG_DUNG[floor], Y_ONG_DUNG[floor]
    _ong_dung(msp, doc, ox, oy, floor, "Ong dung xuyen san",
              (ox + xd - 3900, oy + yd + 1700))
    mep.pipe(msp, [(ox + xd, oy + yd + 200), (ox + xd, oy + 4600),
                   (ox + 1800, oy + 4600)], "cap", SC, "Nhanh cap PPR D20",
             label_at=(ox + 2300, oy + 4680))
    mep.place(msp, doc, "VAN-KHOA", (ox + xd - 700, oy + 4600), SC)
    mep.pipe(msp, [(ox + xd - 1400, oy + 700), (ox + xd, oy + 700),
                   (ox + xd, oy + yd - 200)], "thoat", SC, "Thoat bon cau D114",
             label_at=(ox + xd - 6000, oy - 300))
    mep.pipe(msp, [(ox + xd - 900, oy + 1900), (ox + xd + 700, oy + 1900),
                   (ox + xd + 700, oy + yd)], "thoat", SC, "Thoat san D60",
             label_at=(ox + xd - 3500, oy + 1950))
    an.leader_note(msp, "Binh nong lanh 20L", (ox + xd - 900, oy + 2800),
                   (ox + xd - 4200, oy + 3400), SC)


def _ong_dung(msp, doc, ox, oy, floor, ghi_chu, diem_chu):
    """Ong dung xuyen san, dat dung mot truc doc suot ba tang."""
    xd, yd = X_ONG_DUNG[floor], Y_ONG_DUNG[floor]
    mep.place(msp, doc, "ONG-DUNG", (ox + xd, oy + yd), SC)
    an.leader_note(msp, ghi_chu, (ox + xd, oy + yd), diem_chu, SC)


NOTES_NUOC = [
    "Ong cap nuoc PPR PN10 han nhiet, di am tuong, thu ap 6 bar trong 24 gio.",
    "Ong thoat PVC Binh Minh: bon cau D114, chau rua va thoat san D60, do doc 2%.",
    "Moi thiet bi ve sinh co van khoa rieng de sua chua khong phai cat nuoc toan nha.",
    "Be tu hoai 3 ngan xay gach the vua M75, dung tich 3.0m3, co ong thong hoi len mai.",
    "Ho ga lang cat 800x800x1000 dat cuoi tuyen truoc khi dau vao cong thoat chung.",
    "Ong dung xuyen san phai dat truoc khi do be tong, chen kin bang vua khong co ngot.",
    "Binh nong lanh 20L moi WC, dat ong nuoc nong PPR chiu nhiet rieng.",
]

KY_HIEU_NUOC = [
    ("DONG-HO-NUOC", "Dong ho nuoc va van tong"),
    ("VAN-KHOA", "Van khoa nhanh"),
    ("ONG-DUNG", "Ong dung xuyen san"),
    ("HO-GA", "Ho ga lang cat 800x800"),
]
