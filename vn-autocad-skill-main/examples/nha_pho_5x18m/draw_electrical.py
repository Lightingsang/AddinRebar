# -*- coding: utf-8 -*-
"""Mat bang cap dien chieu sang va o cam cho tung tang."""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, mep

from . import layout as L

SC = "1/100"


def _nen(msp, ox, oy, floor=1):
    """Nen kien truc mo: chi tuong va cot, khong ve noi that."""
    walls = [((ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]), t, o)
             for p1, p2, t, o in L.FLOOR_WALLS[floor]]
    cols = [(ox + x, oy + y, sz) for x, y, sz in L.COLUMNS]
    mep.background(msp, walls, cols)


# ---------------------------------------------------------------------------
# ME-01: DIEN CHIEU SANG VA O CAM
# ---------------------------------------------------------------------------
DEN = {
    1: [(2750, 2500), (2750, 1200), (2750, 3800), (7500, 3600), (7500, 1400),
        (11250, 2500), (15500, 3600), (15500, 1400), (10400, 950)],
    2: [(750, 2500), (4650, 3600), (4650, 1400), (9150, 3600), (9150, 1400),
        (11750, 1200), (15500, 3600), (15500, 1400)],
    3: [(750, 2500), (4650, 3600), (4650, 1400), (9150, 2500),
        (14300, 3400), (16800, 1100), (16800, 3600)],
}
CONG_TAC = {
    1: [(5300, 4700, "CONG-TAC-2"), (9300, 4700, "CONG-TAC-3"),
        (12800, 4700, "CONG-TAC-2"), (9700, 1900, "CONG-TAC-1")],
    2: [(1700, 4700, "CONG-TAC-1"), (7600, 4700, "CONG-TAC-2"),
        (12800, 4700, "CONG-TAC-2"), (10700, 2200, "CONG-TAC-1")],
    3: [(1700, 4700, "CONG-TAC-1"), (7600, 4700, "CONG-TAC-3"),
        (12800, 4700, "CONG-TAC-2"), (15800, 2000, "CONG-TAC-1")],
}
O_CAM = {
    1: [(1200, 300), (7000, 300), (8800, 4700), (13600, 300), (17400, 300),
        (17400, 4700), (14200, 4700)],
    2: [(3000, 300), (6600, 4700), (11000, 4700), (14200, 300), (17400, 300),
        (17400, 4700)],
    3: [(3000, 300), (6600, 4700), (13600, 4700), (17400, 300)],
}
MAY_LANH = {
    1: [(6200, 4700), (16400, 4700)],
    2: [(4650, 300), (15500, 300)],
    3: [(4650, 300), (14300, 4700)],
}


def draw_electrical(msp, doc, origin=(0.0, 0.0), floor=1):
    """Mat bang cap dien mot tang: tu dien, den, cong tac, o cam, may lanh."""
    ox, oy = origin
    _nen(msp, ox, oy, floor)
    if floor == 1:
        mep.place(msp, doc, "TU-DIEN", (ox + 5200, oy + 4600), SC)
        an.leader_note(msp, "Tu dien tong 12 lo", (ox + 5200, oy + 4600),
                       (ox + 3200, oy + 6200), SC)
        _tuyen_den(msp, ox, oy)
    for x, y in DEN[floor]:
        mep.place(msp, doc, "DEN-DOWNLIGHT", (ox + x, oy + y), SC)
    mep.place(msp, doc, "QUAT-TRAN", (ox + 7500, oy + 2500), SC)
    for x, y, ten in CONG_TAC[floor]:
        mep.place(msp, doc, ten, (ox + x, oy + y), SC)
    for x, y in O_CAM[floor]:
        mep.place(msp, doc, "O-CAM-DOI", (ox + x, oy + y), SC)
    for x, y in MAY_LANH[floor]:
        mep.place(msp, doc, "MAY-LANH", (ox + x, oy + y), SC)
    return "Mat bang cap dien chieu sang tang %d" % floor


def _tuyen_den(msp, ox, oy):
    """Tuyen day noi cong tac voi cum den phu trach."""
    nhom = [[(5300, 4700), (2750, 4700), (2750, 3800), (2750, 2500), (2750, 1200)],
            [(9300, 4700), (7500, 4700), (7500, 3600), (7500, 2500), (7500, 1400)],
            [(12800, 4700), (11250, 4700), (11250, 2500)],
            [(12800, 4700), (15500, 4700), (15500, 3600), (15500, 1400)],
            [(9700, 1900), (10400, 1900), (10400, 950)]]
    for tuyen in nhom:
        mep.wire(msp, [(ox + x, oy + y) for x, y in tuyen])


KY_HIEU_DIEN = [
    ("DEN-DOWNLIGHT", "Den LED downlight am tran 9W"),
    ("DEN-OP-TRAN", "Den op tran / den WC"),
    ("QUAT-TRAN", "Quat tran"),
    ("CONG-TAC-1", "Cong tac 1 cuc"),
    ("CONG-TAC-2", "Cong tac 2 cuc"),
    ("CONG-TAC-3", "Cong tac 3 cuc"),
    ("O-CAM-DOI", "O cam doi 3 chau co nap"),
    ("MAY-LANH", "Cuc lanh dieu hoa treo tuong"),
    ("TU-DIEN", "Tu dien tong am tuong"),
]

def legend(lay, doc, x, y_top, items, title="Bang ky hieu"):
    """Ve bang ky hieu THANG len giay, ky hieu dung dung co that tren giay."""
    lay.add_text(title.upper(), dxfattribs={
        "layer": "M-TEXT", "style": "TCVN", "height": 3.2
    }).set_placement((x, y_top), align=TA.TOP_LEFT)
    lay.add_line((x, y_top - 4.2), (x + 96, y_top - 4.2), dxfattribs={"layer": "M-TEXT"})
    y = y_top - 12.0
    for ten, mo_ta in items:
        mep.place(lay, doc, ten, (x + 8, y), "1/1")
        lay.add_text(mo_ta, dxfattribs={
            "layer": "M-TEXT", "style": "TCVN", "height": 2.2
        }).set_placement((x + 20, y), align=TA.MIDDLE_LEFT)
        y -= 10.0
    return y


NOTES_DIEN = [
    "Day dan Cadivi loi dong luon ong nhua PVC am tuong, am san.",
    "Tuyen chieu sang dung day 2x1.5mm2; tuyen o cam dung 2x2.5mm2; may lanh 2x4.0mm2.",
    "Tu dien tong dat cao 1.5m, co aptomat chong giat 30mA cho tuyen o cam va WC.",
    "O cam dat cach san hoan thien 350mm; cong tac dat cach san 1250mm.",
    "Den LED downlight 9W am tran, khoang cach bo tri 1.2 den 1.5m.",
    "Tiep dia he thong dien: coc dong D16 dai 2.4m, dien tro tiep dat nho hon 4 Ohm.",
]
