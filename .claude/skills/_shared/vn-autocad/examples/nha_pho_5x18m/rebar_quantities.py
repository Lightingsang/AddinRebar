# -*- coding: utf-8 -*-
"""Khoi luong thep TOAN NHA, tinh tu chinh cac bang thong ke tren ban ve.

Du toan lay so tu day chu khong go tay, nen sua bang thong ke tren ban ve thi
du toan tu doi theo, hai noi khong bao gio lech nhau.

Bang tren to chi ghi cho MOT tang dien hinh (KC-02) va MOT doan thang (KC-05),
dung cach ho so that van lam. Toan nha thi phai nhan so lan lap lai.
"""

from vncad.struct import bar_weight

from .draw_stair_structure import REBAR_ROWS_THANG
from .draw_structure import REBAR_ROWS
from .foundation_rebar import REBAR_ROWS_MONG

# San tang 2, san tang 3 va san thuong dung chung mot mat bang ket cau dam san;
# cot trong bang tinh cho mot tang, ba tang cung tiet dien.
SO_TANG_DIEN_HINH = 3
# Thang tu tang 1 len 2, tang 2 len 3, tang 3 len san thuong.
SO_DOAN_THANG = 3
# Dinh muc tach thep theo duong kinh: tu 10mm tro xuong va tren 10mm.
D_RANH_GIOI = 10


def theo_duong_kinh(rows, he_so=1):
    """Cong khoi luong mot bang thong ke, tach (kg D<=10, kg D>10)."""
    nho = lon = 0.0
    for _, _, _, d, dai, sl, so in rows:
        kg = bar_weight(d, dai / 1000.0 * sl * so) * he_so
        if d <= D_RANH_GIOI:
            nho += kg
        else:
            lon += kg
    return nho, lon


def thep_than_nha():
    """(kg D<=10, kg D>10) cua phan than nha, da nhan so lan lap lai.

    Khong gom mong (xem thep_mong) va chua tinh mai che thang.
    """
    san_nho, san_lon = theo_duong_kinh(REBAR_ROWS, SO_TANG_DIEN_HINH)
    thang_nho, thang_lon = theo_duong_kinh(REBAR_ROWS_THANG, SO_DOAN_THANG)
    return san_nho + thang_nho, san_lon + thang_lon


def thep_mong():
    """Tong kg thep mong (dai coc va giang mong), lay tu bang thong ke KC-01."""
    nho, lon = theo_duong_kinh(REBAR_ROWS_MONG)
    return nho + lon


NGUON_MONG = "theo bang thong ke thep mong KC-01"
NGUON = ("theo bang thong ke KC-02 x %d tang va KC-05 x %d doan thang"
         % (SO_TANG_DIEN_HINH, SO_DOAN_THANG))
