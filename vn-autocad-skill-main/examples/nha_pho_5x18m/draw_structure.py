# -*- coding: utf-8 -*-
"""Ve mat bang ket cau: dinh vi coc va dai mong, da kieng, dam san tung tang."""

from vncad import annotate as an, struct

from . import layout as L

SC = "1/100"
XS = [x for _, x in L.AXIS_X]
YS = [y for _, y in L.AXIS_Y]

# Dai mong: truc bien dung M1, truc giua dung M2. Moi dai 2 coc ep 250x250.
CAP_W, CAP_H = 600.0, 1400.0
PILE_OFF = [(0.0, -450.0), (0.0, 450.0)]


def giua_nhip(vals, i):
    """Vi tri tuong doi (0 den 1) cua giua nhip thu i tren mot dam chay suot.

    Dat nhan dam ngay giua chieu dai thi nhan roi trung vao truc ngang cat qua,
    net dam ngang se dam thang vao chu. Dat vao giua mot nhip thi nhan nam o
    khoang trong giua hai truc.
    """
    return ((vals[i] + vals[i + 1]) / 2 - vals[0]) / (vals[-1] - vals[0])


def cap_mark(ix):
    return "M1" if ix in (0, len(XS) - 1) else "M2"


def foundation_plan(msp, doc):
    """Mat bang dinh vi coc ep, dai mong va he da kieng khoa cung."""
    _tie_beams(msp)
    for ix, x in enumerate(XS):
        for iy, y in enumerate(YS):
            n = ix * len(YS) + iy + 1
            struct.pile_cap(msp, (x, y), CAP_W, CAP_H, PILE_OFF,
                            label="%s-CC%d" % (cap_mark(ix), n), scale=SC)


def _tie_beams(msp):
    """Da kieng doc theo 2 truc A B va ngang tai moi truc so."""
    for i, y in enumerate(YS):
        struct.tie_beam(msp, (XS[0], y), (XS[-1], y), width=250,
                        label="GM%d 250x400" % (i + 1), scale=SC,
                        label_pos=giua_nhip(XS, 1))
    for i, x in enumerate(XS):
        struct.tie_beam(msp, (x, YS[0]), (x, YS[-1]), width=250,
                        label="GM%d" % (i + 3), scale=SC)


def footing_detail_m1(msp, doc, scale="1/20"):
    """Chi tiet dai mong M1: hinh that 1:1, chu va dim co cho ty le 1/20.

    Tieu de hinh ve tren giay trong Layout, khong ve o day.
    """
    struct.pile_cap(msp, (0, 0), CAP_W, CAP_H, PILE_OFF, scale=scale)
    for dx, dy in PILE_OFF:
        struct.rebar_line(msp, [(dx - 220, dy), (dx + 220, dy)], dia=16, count=4,
                          scale=scale)
    struct.rebar_line(msp, [(-CAP_W / 2 + 50, -CAP_H / 2 + 50),
                            (-CAP_W / 2 + 50, CAP_H / 2 - 50)], dia=16, spacing=150,
                      scale=scale)
    an.dim_chain(msp, doc, [-CAP_H / 2, -450.0, 450.0, CAP_H / 2], -CAP_W / 2,
                 axis="v", scale=scale, side=-1)
    an.dim_chain(msp, doc, [-CAP_W / 2, CAP_W / 2], -CAP_H / 2,
                 axis="h", scale=scale, side=-1)


# ---------------------------------------------------------------------------
# MAT BANG KET CAU DAM SAN
# ---------------------------------------------------------------------------
# Dam phu D3 chia doi o san lon, nen phai tach thanh hai o rieng S1 va S2.
# Mot o san khong duoc de dam chay xuyen qua giua.
SLAB_ZONES = [
    ([(0, 0), (5500, 0), (5500, 2500), (0, 2500)], "S1", 100),
    ([(0, 2500), (5500, 2500), (5500, 5000), (0, 5000)], "S2", 100),
    ([(5500, 0), (9500, 0), (9500, 5000), (5500, 5000)], "S3", 100),
    ([(13000, 0), (18000, 0), (18000, 5000), (13000, 5000)], "S4", 100),
]
VOID = [(9500, 0), (13000, 0), (13000, 5000), (9500, 5000)]


def beam_slab_plan(msp, doc, floor=2):
    """Mat bang ket cau dam san mot tang: dam chinh, dam phu, o san, lo thong tang."""
    z = L.FLOOR_LEVEL[floor]
    for x in XS:
        # 0.72 chu khong phai 0.5: giua chieu dai dam doc chinh la cao do dam
        # phu D3 chay ngang, dat nhan o do la nhan bi D3 dam qua
        struct.beam(msp, (x, YS[0]), (x, YS[-1]), width=200,
                    label="D1 200x400", scale=SC, label_pos=0.72)
    for y in YS:
        struct.beam(msp, (XS[0], y), (XS[-1], y), width=200,
                    label="D2 200x300", scale=SC, label_pos=giua_nhip(XS, 1))
    struct.beam(msp, (0, 2500), (5500, 2500), width=200, label="D3 200x300",
                scale=SC, label_pos=0.5)
    for pts, name, thk in SLAB_ZONES:
        struct.slab_zone(msp, pts, name, level=z, thickness=thk, scale=SC)
    struct.slab_zone(msp, VOID, "LO THONG TANG", scale=SC)
    for x, y, _ in L.COLUMNS:
        struct.pile_cap(msp, (x, y), 220, 220, [], scale=SC, layer="S-COLS", lean=0)


# ---------------------------------------------------------------------------
# BANG THONG KE COT THEP DAM SAN TANG 2
# ---------------------------------------------------------------------------
_THEP_DAM = [
    ("Dam D1 200x400", "1", "Thang", 18, 5200, 3, 5),
    ("Dam D1 200x400", "2", "Thang", 18, 5200, 3, 5),
    ("Dam D1 200x400", "3", "Dai kin", 8, 1150, 28, 5),
    ("Dam D2 200x300", "4", "Thang", 16, 18400, 3, 2),
    ("Dam D2 200x300", "5", "Thang", 16, 18400, 3, 2),
    ("Dam D2 200x300", "6", "Dai kin", 8, 950, 95, 2),
    ("Dam D3 200x300", "7", "Thang", 16, 9700, 2, 1),
    ("Dam D3 200x300", "8", "Dai kin", 8, 950, 50, 1),
]

# Thep san lay dung theo chi tiet CT5 (to KC-04): lop duoi O10a150 hai phuong,
# thep mu O10a150 vuon 1250 tu mep dam tren moi canh goi. Truoc day bang chi ghi
# mot phuong va khong co thep mu, thieu khoang hai phan ba khoi luong thep san.
BUOC_SAN, NEO_SAN, D_SAN = 150, 150, 10
DAI_MU = 1250 + 100 + 70            # vuon + nua be rong dam + chan moc
LOAI_SAN = [("San S1, S2", 5500, 2500, 2), ("San S3", 4000, 5000, 1),
            ("San S4", 5000, 5000, 1)]      # S1 va S2 giong het nhau nen gop


def _hang_thep_san(stt0):
    """Sinh dong thong ke thep san tu kich thuoc o san, khong go tay so thanh."""
    rows, n = [], stt0
    for ten, lx, ly, so_ck in LOAI_SAN:
        nx, ny = ly // BUOC_SAN + 1, lx // BUOC_SAN + 1   # so thanh moi phuong
        for hinh, dai, sl in (("Lop duoi phuong X", lx + 2 * NEO_SAN, nx),
                              ("Lop duoi phuong Y", ly + 2 * NEO_SAN, ny),
                              ("Mu goi bon canh", DAI_MU, 2 * (nx + ny))):
            rows.append((ten + " h=100", str(n), hinh, D_SAN, dai, sl, so_ck))
            n += 1
    return rows


_THEP_SAN = _hang_thep_san(len(_THEP_DAM) + 1)
_THEP_COT = [("Cot C1 220x220", "Thang", 18, 3800, 4, 10),
             ("Cot C1 220x220", "Dai kin", 8, 800, 22, 10)]
REBAR_ROWS = _THEP_DAM + _THEP_SAN + [
    (ck, str(len(_THEP_DAM) + len(_THEP_SAN) + i + 1), hinh, d, dai, sl, so)
    for i, (ck, hinh, d, dai, sl, so) in enumerate(_THEP_COT)]
