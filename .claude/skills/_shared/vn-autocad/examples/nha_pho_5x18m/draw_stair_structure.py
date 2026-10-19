# -*- coding: utf-8 -*-
"""Ket cau cau thang: mat bang ket cau 1/50 va mat cat ban thang 1/25.

To kien truc KT-05 cho biet buoc bac va vat lieu op; to nay cho biet ban thang
day bao nhieu, gac len dam nao va thep chay theo huong nao.
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, standards as S, struct

SC50, SC25 = "1/50", "1/25"

# Lay theo cau thang da ve o KT-05: 21 bac, co bac 173,8; ve 1 len chieu nghi
# 10 bac, ve 2 len san tang tren 11 bac.
CO_BAC, MAT_BAC = 173.8, 270.0
BAC_VE_1, BAC_VE_2 = 10, 11
DAI_VE = MAT_BAC * BAC_VE_1        # 2700
CAO_VE_1 = CO_BAC * BAC_VE_1       # 1738
T_BAN = 120.0                      # ban thang BTCT
B_DAM, H_DAM = 200.0, 300.0
COVER = 20.0

X_DCT, X_DCN = 200.0, 200.0 + DAI_VE     # tim dam chan thang va dam chieu nghi
LONG_R = 2600.0                          # be rong long thang


def _chu(msp, txt, pt, sc, kind="note", canh=TA.MIDDLE_CENTER, layer="S-TEXT"):
    msp.add_text(txt, dxfattribs={"layer": layer, "style": "TCVN",
                                  "height": S.th(kind, sc)}).set_placement(pt, align=canh)


# ---------------------------------------------------------------------------
# MAT BANG KET CAU CAU THANG
# ---------------------------------------------------------------------------
def draw_stair_struct_plan(msp, doc, origin=(0.0, 0.0)):
    """Mat bang ket cau: hai ban thang, chieu nghi, dam chan thang va chieu nghi."""
    ox, oy = origin
    for x, ten in ((X_DCT, "DCT 200x300"), (X_DCN, "DCN 200x300")):
        struct.beam(msp, (ox + x, oy), (ox + x, oy + LONG_R), width=B_DAM,
                    label=ten, scale=SC50, label_pos=0.22)
    _ban(msp, ox, oy, 100.0, 1200.0, "BT1", "Ve 1 len chieu nghi")
    _ban(msp, ox, oy, 1400.0, 2500.0, "BT2", "Ve 2 len san tang tren")
    _chieu_nghi(msp, ox, oy)
    _huong_thep(msp, ox, oy)
    an.dim_chain(msp, doc, [ox, ox + X_DCT, ox + X_DCN, ox + X_DCN + 1200], oy,
                 "h", SC50, side=-1)
    an.dim_chain(msp, doc, [oy, oy + LONG_R], ox, "v", SC50, side=-1)
    an.view_title(msp, (ox + 2200, oy - 2200), "Mat bang ket cau cau thang",
                  "1/50", SC50)


def _ban(msp, ox, oy, y0, y1, ma, ghi_chu):
    """Mot ban thang nghieng, nhin tren mat bang la mot o chu nhat."""
    struct.slab_zone(msp, [(ox + X_DCT, oy + y0), (ox + X_DCN, oy + y0),
                           (ox + X_DCN, oy + y1), (ox + X_DCT, oy + y1)],
                     ma, thickness=int(T_BAN), scale=SC50)
    _chu(msp, ghi_chu, (ox + (X_DCT + X_DCN) / 2, oy + (y0 + y1) / 2 - 420), SC50)


def _chieu_nghi(msp, ox, oy):
    """Ban chieu nghi gac len dam chieu nghi va hai tuong long thang."""
    struct.slab_zone(msp, [(ox + X_DCN, oy + 100), (ox + X_DCN + 1200, oy + 100),
                           (ox + X_DCN + 1200, oy + 2500), (ox + X_DCN, oy + 2500)],
                     "CN", thickness=int(T_BAN), scale=SC50)


def _huong_thep(msp, ox, oy):
    """Net thep chiu luc chay doc huong len va thep phan bo chay ngang."""
    for y in (650.0, 1950.0):
        struct.rebar_line(msp, [(ox + X_DCT + 150, oy + y), (ox + X_DCN - 150, oy + y)],
                          dia=10, spacing=150, scale=SC50)
    # thep phan bo dat lech ve dau ban: dat o 900 thi ghi chu cua no dinh lien
    # vao ghi chu thep chiu luc nam giua ban, doc thanh mot chuoi
    struct.rebar_line(msp, [(ox + X_DCT + 450, oy + 200), (ox + X_DCT + 450, oy + 1100)],
                      dia=8, spacing=200, scale=SC50)


# ---------------------------------------------------------------------------
# MAT CAT BAN THANG
# ---------------------------------------------------------------------------
def draw_stair_struct_section(msp, doc, origin=(0.0, 0.0)):
    """Mat cat doc ve 1: dam chan thang, ban nghieng, dam chieu nghi va thep."""
    ox, oy = origin
    u, n = _huong()
    _dam_hop(msp, ox, oy, 0.0, 0.0)                       # dam chan thang
    _dam_hop(msp, ox, oy, DAI_VE, CAO_VE_1)               # dam chieu nghi
    _ban_nghieng(msp, ox, oy, u, n)
    _bac_xay(msp, ox, oy)
    _chieu_nghi_cat(msp, ox, oy)
    _thep_ban(msp, ox, oy, u, n)
    an.dim_chain(msp, doc, [oy, oy + CAO_VE_1], ox - 700, "v", SC25, side=-1)
    an.view_title(msp, (ox + DAI_VE / 2, oy - 1500),
                  "Mat cat doc ban thang ve 1", "1/25", SC25)


def _huong():
    """Vecto don vi doc theo ban thang va vecto phap tuyen huong len."""
    d = math.hypot(DAI_VE, CAO_VE_1)
    return (DAI_VE / d, CAO_VE_1 / d), (-CAO_VE_1 / d, DAI_VE / d)


def _dam_hop(msp, ox, oy, dx, dy, layer="S-BEAM"):
    """Tiet dien dam 200x300 nam duoi cao do dat ban."""
    x, y = ox + dx, oy + dy
    msp.add_lwpolyline([(x - B_DAM / 2, y), (x + B_DAM / 2, y),
                        (x + B_DAM / 2, y - H_DAM), (x - B_DAM / 2, y - H_DAM)],
                       close=True, dxfattribs={"layer": layer, "lineweight": 40})


def _ban_nghieng(msp, ox, oy, u, n):
    """Hai mat cua ban thang nghieng, day do theo phuong vuong goc."""
    a = (ox, oy)
    b = (ox + DAI_VE, oy + CAO_VE_1)
    duoi = [(p[0] - n[0] * T_BAN, p[1] - n[1] * T_BAN) for p in (a, b)]
    msp.add_lwpolyline([a, b], dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    msp.add_lwpolyline(duoi, dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    for p, q in ((a, duoi[0]), (b, duoi[1])):
        msp.add_line(p, q, dxfattribs={"layer": "S-BEAM", "lineweight": 40})


def _bac_xay(msp, ox, oy):
    """Bac xay gach dat tren ban nghieng, ve net manh vi khong chiu luc."""
    for i in range(BAC_VE_1):
        x, y = ox + MAT_BAC * i, oy + CO_BAC * i
        msp.add_lwpolyline([(x, y), (x, y + CO_BAC), (x + MAT_BAC, y + CO_BAC)],
                           dxfattribs={"layer": "S-TEXT", "lineweight": 13})


def _chieu_nghi_cat(msp, ox, oy):
    """Doan ban chieu nghi nam ngang gac tren dam chieu nghi."""
    x, y = ox + DAI_VE, oy + CAO_VE_1
    msp.add_lwpolyline([(x, y), (x + 900, y)],
                       dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    msp.add_lwpolyline([(x, y - T_BAN), (x + 900, y - T_BAN)],
                       dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    _chu(msp, "Ban chieu nghi day 120", (x + 500, y + 420), SC25)


def _thep_ban(msp, ox, oy, u, n):
    """Thep lop duoi chay suot va thep mu tai hai goi, deu neo vao dam."""
    phan_tu = 0.25          # thep mu vuon L/4 tinh tu moi goi
    _tuyen(msp, ox, oy, u, n, T_BAN - COVER - 5, 0.0, 1.0, "S-REBAR", 40)
    _tuyen(msp, ox, oy, u, n, COVER + 5, 0.0, phan_tu, "S-REBAR", 40)
    _tuyen(msp, ox, oy, u, n, COVER + 5, 1.0 - phan_tu, 1.0, "S-REBAR", 40)
    an.leader_note(msp, "Thep chiu luc O10a150 lop duoi, neo vao dam 30D",
                   (ox + DAI_VE * 0.45, oy + CAO_VE_1 * 0.45 - 95),
                   (ox + DAI_VE * 0.30, oy - 900), SC25)
    an.leader_note(msp, "Thep mu O10a150 tai hai goi, vuon L/4",
                   (ox + DAI_VE * 0.16, oy + CAO_VE_1 * 0.16 - 20),
                   (ox + DAI_VE * 0.55, oy + CAO_VE_1 + 200), SC25)


def _tuyen(msp, ox, oy, u, n, tu_mat_tren, t0, t1, layer, lw):
    """Mot tuyen thep song song mat ban, tu ty le doc t0 den t1.

    tu_mat_tren: khoang cach tu MAT TREN ban xuong tim thanh thep. Truoc day
    tinh nguoc (T_BAN - gia tri), lam thep chiu luc nam o lop tren va thep mu
    nam o lop duoi, sai han ve chiu luc.
    """
    d = math.hypot(DAI_VE, CAO_VE_1)
    pts = []
    for t in (t0, t1):
        pts.append((ox + u[0] * d * t - n[0] * tu_mat_tren,
                    oy + u[1] * d * t - n[1] * tu_mat_tren))
    msp.add_lwpolyline(pts, dxfattribs={"layer": layer, "lineweight": lw})


# So luong tinh tu kich thuoc: ban rong 1100 buoc 150 ra 8 thanh; chieu dai ban
# nghieng 3211 buoc 200 ra 17 thanh phan bo; thanh chiu luc dai 3211 cong hai
# dau neo 30D = 300 ra 3800. Dai dam 200x300 buoc 100 tren nhip 2600 ra 27 cai.
REBAR_ROWS_THANG = [
    ("Ban thang BT1", "1", "Thang moc", 10, 3800, 8, 1),
    ("Ban thang BT1", "2", "Thang", 8, 1100, 17, 1),
    ("Ban thang BT2", "3", "Thang moc", 10, 3800, 8, 1),
    ("Ban thang BT2", "4", "Thang", 8, 1100, 17, 1),
    ("Ban chieu nghi CN", "5", "Thang moc", 10, 2600, 9, 1),
    ("Dam DCT 200x300", "6", "Thang", 16, 3000, 4, 1),
    ("Dam DCN 200x300", "7", "Thang", 16, 3000, 4, 1),
    ("Dai dam DCT va DCN", "8", "Dai kin", 8, 900, 27, 2),
]

NOTES_KC_THANG = [
    "Ban thang BTCT day 120mm, be tong B20 (M250), thep chiu luc CB300-V.",
    "Thep chiu luc dat lop duoi chay suot theo huong len, neo vao dam 30 lan duong kinh.",
    "Thep mu tai goi vuon L/4 tinh tu mep dam, phai co ke con ke giu dung cao do.",
    "Dam chan thang va dam chieu nghi do lien khoi voi ban thang, khong de mach ngung.",
    "Bac xay gach dac dat tren ban nghieng sau khi thao cot pha, khong do lien khoi.",
    "Cot pha ban nghieng phai chong do chac, kiem tra do dong truoc khi do be tong.",
    "Lop bao ve thep ban 20mm, thep dam 25mm.",
]
