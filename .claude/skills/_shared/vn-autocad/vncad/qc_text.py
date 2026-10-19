# -*- coding: utf-8 -*-
"""Do o bao cua chu va tim cac cho chu de len chu.

Truoc day be rong chu duoc uoc theo he so 0,60 lan chieu cao. He so do sai
kha nhieu: dong "MAT BANG TANG 1" cao 2,5mm thuc te rong 33,5mm chu khong
phai 22,5mm nhu uoc luong, tuc thieu mot phan ba. Nay do thang tren chinh
file phong se dung de in nen o bao dung voi cai mat nguoi ta nhin thay.
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

# ty le dien tich giao nhau toi thieu de coi la chong, tinh tren o chu nho hon.
# Dat thap vi chu de len nhau du it cung lam ban ve kho doc khi in ra giay.
NGUONG = 0.18
# cac layer chua chu can soat; bo qua chu trong DIMENSION vi ezdxf tu xep
LAYERS = ("TEXT", "S-TEXT", "M-TEXT")
FONT_MAC_DINH = "times.ttf"


def _ten_phong(e):
    """Lay ten file phong (vd times.ttf) tu kieu chu ma doi tuong dang dung."""
    try:
        return e.doc.styles.get(e.dxf.style).dxf.font or FONT_MAC_DINH
    except Exception:
        return FONT_MAC_DINH


def be_rong(e):
    """Be rong that cua mot dong chu, do bang chinh phong se in ra."""
    s = e.dxf.text or ""
    if not s:
        return 0.0
    return S.rong_chu(s, e.dxf.height, _ten_phong(e)) * (e.dxf.width or 1.0)


def o_bao(e):
    """O bao (x0, y0, x1, y1, noi_dung) cua mot doi tuong TEXT.

    Co tinh ca goc xoay: nhan dam dung duoc ghi xoay 90 do nen o bao cua no
    phai cao va hep, khong phai thap va rong. Bo qua goc xoay thi bo soat bao
    nham nhung cho khong he chong nhau.
    """
    h = e.dxf.height
    w = be_rong(e)
    p = e.dxf.align_point if e.dxf.halign or e.dxf.valign else e.dxf.insert
    ax, ay = 0.0, 0.0          # goc trai duoi cua o chu so voi diem neo
    al = e.get_align_enum()
    if al in (TA.MIDDLE_CENTER, TA.TOP_CENTER, TA.BOTTOM_CENTER, TA.CENTER):
        ax = -w / 2
    elif al in (TA.MIDDLE_RIGHT, TA.TOP_RIGHT, TA.BOTTOM_RIGHT, TA.RIGHT):
        ax = -w
    if al in (TA.MIDDLE_CENTER, TA.MIDDLE_LEFT, TA.MIDDLE_RIGHT, TA.MIDDLE):
        ay = -h / 2
    elif al in (TA.TOP_CENTER, TA.TOP_LEFT, TA.TOP_RIGHT):
        ay = -h
    goc = ((ax, ay), (ax + w, ay), (ax + w, ay + h), (ax, ay + h))
    r = math.radians(e.dxf.rotation or 0.0)
    if r:
        c, si = math.cos(r), math.sin(r)
        goc = tuple((gx * c - gy * si, gx * si + gy * c) for gx, gy in goc)
    xs = [p.x + gx for gx, _ in goc]
    ys = [p.y + gy for _, gy in goc]
    return (min(xs), min(ys), max(xs), max(ys), e.dxf.text or "")


def cac_o_bao(msp, layers=LAYERS):
    """O bao cua moi dong chu dang soat, da bo cac dong rong."""
    return [o_bao(e) for e in msp.query("TEXT")
            if e.dxf.layer in layers and (e.dxf.text or "").strip()]


def _giao(a, b):
    """Dien tich giao nhau cua hai o bao."""
    dx = min(a[2], b[2]) - max(a[0], b[0])
    dy = min(a[3], b[3]) - max(a[1], b[1])
    return dx * dy if dx > 0 and dy > 0 else 0.0


def check_text_overlap(msp, layers=LAYERS, nguong=NGUONG, gioi_han=25):
    """Tim cac cap chu de len nhau qua muc cho phep.

    Tra ve danh sach chuoi mo ta, moi chuoi la mot cap chu bi chong.
    gioi_han: chi bao toi da bay nhieu cap de khoi tran man hinh.
    """
    boxes = sorted(cac_o_bao(msp, layers), key=lambda b: b[0])
    ket_qua = []
    for i, a in enumerate(boxes):
        for b in boxes[i + 1:]:
            if b[0] >= a[2]:
                break                      # da sap xep theo x nen khong con giao
            s = _giao(a, b)
            if s <= 0:
                continue
            nho = min((a[2] - a[0]) * (a[3] - a[1]), (b[2] - b[0]) * (b[3] - b[1]))
            if nho > 0 and s / nho >= nguong:
                ket_qua.append('chu "%s" de len "%s" tai (%.0f, %.0f)'
                               % (a[4][:28], b[4][:28], a[0], a[1]))
                if len(ket_qua) >= gioi_han:
                    return ket_qua
    return ket_qua
