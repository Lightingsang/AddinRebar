# -*- coding: utf-8 -*-
"""Hai chi tiet ket cau con thieu trong bo: thep mu goi san va noi thep cot.

Ca hai ve o 1/20 vi day la cho tho de lam sai nhat khi thi cong: thep mu bi
dam bep xuong luc do be tong, va moi noi thep cot bi dat sai cao do.
"""

from vncad import annotate as an, standards as S

SC = "1/20"
SC_COT = "1/25"          # chi tiet noi thep cot cao hon nen ve nho hon mot bac
R_SO = 3.5 * 20          # ban kinh vong tron danh so: 3,5mm tren giay
R_SO_COT = 3.5 * 25
T_SAN = 100.0            # chieu day san
B_DAM, H_DAM = 200.0, 400.0
COVER = 15.0
D_SAN = 10.0             # duong kinh thep san
VUON = 1250.0            # thep mu vuon ra moi ben, bang L/4 nhip 5m
D_COT, W_COT = 18.0, 220.0
NOI = 40 * D_COT         # chieu dai noi chong 40 lan duong kinh


def _net(msp, pts, layer, lw=25, kin=False):
    msp.add_lwpolyline(pts, close=kin, dxfattribs={"layer": layer, "lineweight": lw})


def _chu(msp, txt, pt, canh=an.TA.MIDDLE_LEFT, layer="S-TEXT"):
    msp.add_text(txt, dxfattribs={"layer": layer, "style": "TCVN",
                                  "height": S.th("note", SC)}).set_placement(pt, align=canh)


# ---------------------------------------------------------------------------
# CT5: THEP MU GOI SAN TAI DAM
# ---------------------------------------------------------------------------
def draw_slab_rebar(msp, doc, origin=(0.0, 0.0)):
    """Mat cat qua san tai goi: thep lop duoi chay suot, thep mu vuon L/4."""
    ox, oy = origin
    # Chi ve 900 moi ben roi cat: ve du L/4 = 1250 thi hinh dai gan 4m ma day
    # san chi 100, ty le 1:40 nhin ra mot vach mo. Chieu dai vuon ghi o chu giai.
    nua = 900.0
    _net(msp, [(ox - nua, oy), (ox + nua, oy)], "S-BEAM", 35)
    _net(msp, [(ox - nua, oy - T_SAN), (ox - B_DAM / 2, oy - T_SAN)], "S-BEAM", 35)
    _net(msp, [(ox + B_DAM / 2, oy - T_SAN), (ox + nua, oy - T_SAN)], "S-BEAM", 35)
    _net(msp, [(ox - B_DAM / 2, oy - T_SAN), (ox - B_DAM / 2, oy - H_DAM),
               (ox + B_DAM / 2, oy - H_DAM), (ox + B_DAM / 2, oy - T_SAN)], "S-BEAM", 35)
    _thep_san(msp, ox, oy, nua)
    _thep_dam(msp, ox, oy)
    for s_ in (-1, 1):
        _ky_hieu_cat(msp, ox + s_ * nua, oy)
    an.dim_chain(msp, doc, [ox - B_DAM / 2, ox + B_DAM / 2], oy + 300, "h", SC, side=1)
    an.view_title(msp, (ox, oy - H_DAM - 700), "CT5 - Thep mu goi san tai dam",
                  "1/20", SC)


def _ky_hieu_cat(msp, x, oy):
    """Net gay hinh chu Z: bao rang hinh con keo dai ra ngoai pham vi ve."""
    msp.add_lwpolyline([(x, oy + 60), (x - 40, oy - 20), (x + 40, oy - 60),
                        (x, oy - T_SAN - 40)],
                       dxfattribs={"layer": "S-BEAM", "lineweight": 18})


def _thep_san(msp, ox, oy, nua):
    """Thep lop duoi chay suot va thep mu goi co chan uon vuong xuong."""
    y_duoi = oy - T_SAN + COVER + D_SAN / 2
    y_tren = oy - COVER - D_SAN / 2
    _net(msp, [(ox - nua, y_duoi), (ox + nua, y_duoi)], "S-REBAR", 40)
    _net(msp, [(ox - nua, y_tren), (ox + nua, y_tren)], "S-REBAR", 40)
    for x in (ox - nua * 0.6, ox + nua * 0.6):         # thep phan bo vuong goc
        for y in (y_duoi, y_tren):
            msp.add_circle((x, y), D_SAN / 2, dxfattribs={"layer": "S-REBAR"})
    for n, sx, sy, tx, ty in ((1, 640, 700, ox + 500, y_tren),
                              (2, -640, 700, ox - 500, y_duoi),
                              (3, -640, -700, ox - 700, y_duoi - 12),
                              (4, 640, -700, ox + B_DAM / 2, oy - H_DAM / 2)):
        an.so_lop(msp, n, (ox + sx, oy + sy), (tx, ty), R_SO, SC)


CHU_GIAI_SAN = [
    ("1", "Thep mu goi O10a150, vuon L/4 = 1250 tinh tu MEP dam (hinh ve cat ngan)"),
    ("2", "Thep san lop duoi O10a150 hai phuong, chay suot qua goi"),
    ("3", "Ke con ke be tong day 15, dat buoc 800 hai phuong"),
    ("4", "Dam do san 200x400, dai O8a100 trong pham vi 2h tu mep cot"),
]


def _thep_dam(msp, ox, oy):
    """Thep chu va dai cua dam do san, ve don gian de thay quan he voi thep san."""
    for dy in (COVER + 9, H_DAM - T_SAN - COVER - 9):
        y = oy - H_DAM + dy
        for dx in (-B_DAM / 2 + COVER + 9, B_DAM / 2 - COVER - 9):
            msp.add_circle((ox + dx, y), 9, dxfattribs={"layer": "S-REBAR"})
    _net(msp, [(ox - B_DAM / 2 + COVER, oy - H_DAM + COVER),
               (ox + B_DAM / 2 - COVER, oy - H_DAM + COVER),
               (ox + B_DAM / 2 - COVER, oy - COVER),
               (ox - B_DAM / 2 + COVER, oy - COVER)], "S-STIRRUP", 25, kin=True)


# ---------------------------------------------------------------------------
# CT6: NOI THEP COT
# ---------------------------------------------------------------------------
def draw_column_splice(msp, doc, origin=(0.0, 0.0)):
    """Noi chong thep cot phia tren mat san, so le 50% de khong noi cung cao do."""
    ox, oy = origin
    cao = NOI * 2 + 900
    for s in (-1, 1):
        _net(msp, [(ox + s * W_COT / 2, oy - 500), (ox + s * W_COT / 2, oy + cao)],
             "S-COLS", 35)
    for s in (-1, 1):                       # mat san hai ben cot, cat ngan lai
        _net(msp, [(ox + s * (W_COT / 2 + 450), oy), (ox + s * W_COT / 2, oy)],
             "S-BEAM", 35)
        _net(msp, [(ox + s * (W_COT / 2 + 450), oy - T_SAN),
                   (ox + s * W_COT / 2, oy - T_SAN)], "S-BEAM", 35)
        for i in range(4):                  # gach cheo bao san be tong bi cat
            x = ox + s * (W_COT / 2 + 90 + i * 90)
            _net(msp, [(x, oy), (x - s * 60, oy - T_SAN)], "S-BEAM", 13)
    _thep_noi(msp, ox, oy)
    _dai_vung_noi(msp, ox, oy, cao)
    an.dim_chain(msp, doc, [oy + 500, oy + 500 + NOI], ox - W_COT / 2 - 120, "v",
                 SC_COT, side=-1)
    an.view_title(msp, (ox, oy - 1100), "CT6 - Noi thep cot tren mat san",
                  "1/25", SC_COT)


def _thep_noi(msp, ox, oy):
    """Hai cap thep: cap noi thap va cap noi cao hon mot doan noi."""
    x = W_COT / 2 - COVER - D_COT / 2
    # Trong thuc te hai thanh noi ap sat nhau; o ty le nay ve sat la thanh mot
    # net, nen tach ra 60mm cho doc duoc. Chu giai co ghi ro dieu do.
    tach = 90.0
    for dx, chan in ((-x, 500.0), (x, 500.0 + NOI)):
        huong = 1 if dx < 0 else -1
        _net(msp, [(ox + dx, oy - 500), (ox + dx, oy + chan + NOI)], "S-REBAR", 50)
        _net(msp, [(ox + dx + huong * tach, oy + chan),
                   (ox + dx + huong * tach, oy + NOI * 2 + 900)], "S-REBAR", 50)
        for y in (oy + chan, oy + chan + NOI):         # danh dau hai dau moi noi
            _net(msp, [(ox + dx - 40, y), (ox + dx + huong * tach + 40, y)],
                 "S-REBAR", 18)
    for n, sx, sy, tx, ty in ((1, 900, NOI * 2 + 400, ox + x, oy + 1400),
                              (2, 900, 300, ox + W_COT / 2 - COVER, oy + 300),
                              (3, -900, -300, ox - W_COT / 2 - 400, oy - T_SAN / 2)):
        an.so_lop(msp, n, (ox + sx, oy + sy), (tx, ty), R_SO_COT, SC_COT)


CHU_GIAI_NOI_COT = [
    ("1", "Noi chong 40D = 720mm, so le 50 phan tram so thanh tren mot mat cat"),
    ("*", "Hai thanh noi thuc te ap sat nhau, hinh ve tach ra cho de doc"),
    ("2", "Dai O8a100 suot pham vi noi, ra ngoai vung noi moi thua a200"),
    ("3", "Mat san hoan thien; tuyet doi khong dat moi noi trong pham vi dam"),
]


def _dai_vung_noi(msp, ox, oy, cao):
    """Dai a100 suot pham vi noi, ra ngoai vung noi moi thua ra a200."""
    y, buoc = oy - 400, 100.0
    while y < oy + cao:
        _net(msp, [(ox - W_COT / 2 + COVER, y), (ox + W_COT / 2 - COVER, y)],
             "S-STIRRUP", 13)
        y += buoc if y < oy + NOI * 2 + 500 else 200.0



NOTES_CHI_TIET_KC = [
    "Thep mu goi phai co ke con ke va thanh chong, khong duoc dam bep khi do be tong.",
    "Chieu dai vuon cua thep mu tinh tu MEP DAM, khong tinh tu tim dam.",
    "Lop bao ve thep san 15mm, thep dam 25mm, kiem tra truoc khi nghiem thu cot pha.",
    "Noi chong thep cot 40 lan duong kinh, so le 50 phan tram so thanh tren mot mat cat.",
    "Khong duoc noi thep cot trong pham vi nut khung va trong pham vi dam.",
    "Dai cot day a100 suot pham vi noi va 2h ve hai phia tinh tu mep dam.",
]
