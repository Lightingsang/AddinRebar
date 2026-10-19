# -*- coding: utf-8 -*-
"""Chuan do hoa ban ve xay dung Viet Nam.

So lieu trong file nay rut ra tu 34 bo ban ve thi cong thuc te
cua 4 cong trinh dan dung va cong cong o Nam Bo, khong phai uoc luong.
"""

from functools import lru_cache as _lru_cache

# ---------------------------------------------------------------------------
# 1. TY LE BAN VE (thuc te dung: 1/10, 1/15, 1/20, 1/25, 1/50, 1/75, 1/100)
# ---------------------------------------------------------------------------
SCALES = {
    "1/1": 1,       # ve thang tren giay trong Layout (paper space)
    "1/200": 200,   # tong mat bang dinh vi, ban ve quy hoach lo dat
    "1/100": 100,   # mat bang, mat dung, mat cat tong the
    "1/75": 75,
    "1/50": 50,     # mat cat, khai trien WC, cau thang, mat dung thi cong
    "1/25": 25,     # chi tiet dam, cot
    "1/20": 20,     # chi tiet mong, dai coc, lan can
    "1/15": 15,
    "1/10": 10,     # chi tiet nut lien ket, chong tham
}

# ---------------------------------------------------------------------------
# 2. CHIEU CAO CHU (mm tren giay), nhan mau so ty le ra chieu cao Model Space.
# Thuc te: h=200 nhieu nhat (2.0mm @1/100), 250/260 (2.5mm), 350/500 (tieu de)
# ---------------------------------------------------------------------------
TEXT_MM = {
    "dim": 2.0,        # con so kich thuoc
    "note": 2.0,       # ghi chu nho, ky hieu thep
    "room": 2.5,       # ten phong
    "room_area": 2.0,  # dien tich duoi ten phong
    "label": 2.6,      # nhan cau kien D1, C1, M1
    "subtitle": 3.5,   # ten hinh chieu duoi moi ban ve
    "title": 5.0,      # tieu de lon
}


def th(kind, scale="1/100"):
    """Chieu cao chu trong Model Space theo ty le. th('room','1/50') -> 125.0"""
    return TEXT_MM[kind] * SCALES[scale]


def mm(value_mm, scale="1/100"):
    """Doi kich thuoc tren giay (mm) sang Model Space."""
    return value_mm * SCALES[scale]


# ---------------------------------------------------------------------------
# 3. HE LAYER KIEN TRUC (ten, mau ACI, linetype, be day net in mm)
# Nguyen tac be day: cat chiu luc > cat bao > net thay > phu tro > hatch
# ---------------------------------------------------------------------------
ARCH_LAYERS = [
    ("A-AXIS",        1, "CENTER",     0.13),
    ("A-COLS",        2, "Continuous", 0.50),
    ("A-COLS-HATCH",  8, "Continuous", 0.09),
    ("A-WALL",        7, "Continuous", 0.40),
    ("A-WALL-HATCH",  9, "Continuous", 0.09),
    ("A-BEAM",        4, "DASHED",     0.30),
    ("A-DOOR",        4, "Continuous", 0.25),
    ("A-WIND",        3, "Continuous", 0.25),
    ("A-STAIR",       6, "Continuous", 0.25),
    ("A-RAILING",     6, "Continuous", 0.25),
    ("A-FURN",        8, "Continuous", 0.15),
    ("A-SAN",         5, "Continuous", 0.18),
    ("A-FLOR",        9, "Continuous", 0.09),
    ("A-ROOF",        1, "Continuous", 0.30),
    ("A-EQUIP",       3, "Continuous", 0.18),
    ("A-ELEV",        7, "Continuous", 0.30),  # net thay mat dung, mat cat
    ("A-MAT",         9, "Continuous", 0.09),  # hatch vat lieu op lat, son, kinh
    ("A-GROUND",      7, "Continuous", 0.60),  # duong dat tu nhien
    ("A-SITE",        1, "DASHDOT",    0.50),  # ranh gioi lo dat, chi gioi xay dung
    ("A-ROAD",        8, "Continuous", 0.20),  # via he, long duong, tim duong
    ("A-CONC",        8, "Continuous", 0.09),  # hatch be tong tren mat cat
    ("DIM",           4, "Continuous", 0.13),
    ("TEXT",          2, "Continuous", 0.20),
    ("A-GRID-BUBBLE", 2, "Continuous", 0.30),
    ("A-SYMBOL",      6, "Continuous", 0.25),
    ("KHUNG-TEN",     7, "Continuous", 0.60),
    ("MODEL-GUIDE",   8, "Continuous", 0.00),  # khung vung trong model, khong in
    ("DEFPOINTS",     7, "Continuous", 0.00),
]

# ---------------------------------------------------------------------------
# 4. HE LAYER KET CAU
# ---------------------------------------------------------------------------
STRUCT_LAYERS = [
    ("S-AXIS",     1, "CENTER",     0.13),
    ("S-COLS",     2, "Continuous", 0.40),
    ("S-FOUND",    6, "Continuous", 0.40),
    ("S-BEAM",     3, "Continuous", 0.35),
    ("S-SLAB",     7, "Continuous", 0.20),
    ("S-REBAR",    1, "Continuous", 0.50),
    ("S-STIRRUP",  4, "Continuous", 0.25),
    ("S-HATCH",    8, "Continuous", 0.09),
    ("S-DIM",      4, "Continuous", 0.13),
    ("S-TEXT",     2, "Continuous", 0.20),
    ("S-SCHEDULE", 7, "Continuous", 0.25),
    ("KHUNG-TEN",  7, "Continuous", 0.60),
]

# ---------------------------------------------------------------------------
# 5. HE LAYER CO DIEN (MEP)
# ---------------------------------------------------------------------------
MEP_LAYERS = [
    ("M-AXIS",    1, "CENTER",     0.13),
    ("M-WALL",    8, "Continuous", 0.15),
    ("M-ELEC",    2, "Continuous", 0.35),
    ("M-LIGHT",   3, "Continuous", 0.30),
    ("M-WATER",   5, "Continuous", 0.35),
    ("M-DRAIN",   4, "DASHED",     0.35),
    ("M-SYMBOL",  6, "Continuous", 0.25),
    ("M-TEXT",    2, "Continuous", 0.20),
    ("DIM",       4, "Continuous", 0.13),
    ("KHUNG-TEN", 7, "Continuous", 0.60),
]

# ---------------------------------------------------------------------------
# 6. KICH THUOC CAU KIEN MAC DINH (nha pho / biet thu VN)
# ---------------------------------------------------------------------------
WALL_EXT = 220
WALL_INT = 110
COL_TYP = (220, 220)
COL_MID = (220, 300)
BEAM_MAIN = (200, 400)
BEAM_SUB = (200, 300)
SLAB_T = 100
PILE = 250
CONCRETE_COVER = {"mong": 50, "dam": 25, "cot": 25, "san": 15}

# ---------------------------------------------------------------------------
# 7. KHO GIAY (mm) va le khung ban ve
# ---------------------------------------------------------------------------
SHEETS = {
    "A4": (297, 210), "A3": (420, 297), "A2": (594, 420),
    "A1": (841, 594), "A0": (1189, 841),
}
MARGIN_BIND = 20
MARGIN_EDGE = 10

# ---------------------------------------------------------------------------
# 8. KHOANG CACH GIONG KICH THUOC (mm tren giay, nhan ty le khi dung)
# Lop 1 cach tuong 8mm, moi lop tiep cach nhau 8mm, bong bong truc cach 10mm
# ---------------------------------------------------------------------------
DIM_TIER_GAP = 8.0
DIM_FIRST_OFFSET = 8.0
BUBBLE_GAP = 10.0
BUBBLE_R = 4.0          # ban kinh bong bong truc tren giay
FLOOR_GAP_MM = 30.0     # khoang ho giua 2 mat bang xep chung 1 to

# ---------------------------------------------------------------------------
# DO BE RONG DONG CHU
# ---------------------------------------------------------------------------
@_lru_cache(maxsize=256)
def _phong(ten_file, cao):
    """Doi tuong phong da nap san; moi lan nap file phong kha cham."""
    from ezdxf.fonts import fonts
    return fonts.make_font(ten_file, cao)


def rong_chu(text, cao, font="times.ttf"):
    """Be rong that cua mot dong chu, do tren chinh file phong se dung de in.

    Uoc theo he so nhan voi so ky tu thi sai kha nhieu: dong "MAT BANG TANG 1"
    cao 2,5mm thuc te rong 33,5mm chu khong phai 22,5mm nhu he so 0,60 cho ra.
    Do that thi cho chua san cho dong chu moi vua dung.
    """
    return _phong(font, cao).text_width(text) if text else 0.0
