# -*- coding: utf-8 -*-
"""Tim cac cho CHU DE LEN NET VE.

Truoc day bo soat chi bat duoc chu de len chu. Chu nam de len duong tuong,
duong kich thuoc hay hoa van vat lieu cung kho doc y het, nen them phep nay.

Cach lam: bam moi hinh thanh cac doan thang ngan, xep chung vao mot luoi o
vuong de tra cho nhanh, roi voi tung dong chu chi kiem nhung doan thang nam
cung o voi no. Neu kiem thang tung doan voi tung dong chu thi phai chay ba
chuc trieu phep so sanh, xep luoi truoc thi con vai chuc nghin.
"""

from . import qc_text as qt

# hinh khong can soat: DIMENSION tu chua chu va tu chua khoang ho cho chu,
# HATCH la hoa van nen mo nhat, TEXT da co phep soat rieng
BO_QUA_LOAI = ("DIMENSION", "HATCH", "TEXT", "MTEXT", "ATTDEF", "VIEWPORT")
BO_QUA_LAYER = ("MODEL-GUIDE", "DEFPOINTS")
O_LUOI = 2000.0        # canh o tra cuu, mm
LOT = 0.14             # thu o chu vao 14% chieu cao moi be, vi net chu khong
                       # cham sat vien o bao nen cham vien chua phai la de


def _doan_thang(e):
    """Bam mot doi tuong thanh cac doan thang (x0, y0, x1, y1)."""
    t = e.dxftype()
    if t == "LINE":
        a, b = e.dxf.start, e.dxf.end
        yield (a.x, a.y, b.x, b.y)
        return
    try:
        if t in ("ARC", "CIRCLE", "ELLIPSE"):
            pts = list(e.flattening(e.dxf.get("radius", 100.0) / 24.0 or 1.0))
        elif t == "LWPOLYLINE":
            pts = list(e.flattening(4.0))
        elif t == "POLYLINE":
            pts = [v.dxf.location for v in e.vertices]
        elif t == "SPLINE":
            pts = list(e.flattening(4.0))
        else:
            return
    except Exception:
        return
    for a, b in zip(pts, pts[1:]):
        yield (a.x, a.y, b.x, b.y)


def _cat_hop(seg, hop):
    """Doan thang co cat qua hinh chu nhat khong (thuat toan Liang-Barsky)."""
    x0, y0, x1, y1 = seg[:4]
    dx, dy = x1 - x0, y1 - y0
    t0, t1 = 0.0, 1.0
    for p, q in ((-dx, x0 - hop[0]), (dx, hop[2] - x0),
                 (-dy, y0 - hop[1]), (dy, hop[3] - y0)):
        if p == 0.0:
            if q < 0.0:
                return False
        else:
            r = q / p
            if p < 0.0:
                if r > t1:
                    return False
                t0 = max(t0, r)
            else:
                if r < t0:
                    return False
                t1 = min(t1, r)
    return True


def _xep_luoi(msp, canh):
    """Do moi doan thang vao cac o luoi ma no di qua, de tra cho nhanh."""
    luoi = {}
    for e in msp:
        if e.dxftype() in BO_QUA_LOAI or e.dxf.get("layer", "") in BO_QUA_LAYER:
            continue
        nhan = "%s tren lop %s" % (e.dxftype(), e.dxf.get("layer", "0"))
        for doan in _doan_thang(e):
            seg = doan + (nhan,)
            cx0 = int(min(seg[0], seg[2]) // canh)
            cx1 = int(max(seg[0], seg[2]) // canh)
            cy0 = int(min(seg[1], seg[3]) // canh)
            cy1 = int(max(seg[1], seg[3]) // canh)
            # doan qua dai (vd truc dinh vi) trai het ban ve thi bo, no khong
            # phai net hinh gay kho doc ma la duong dan huong
            if (cx1 - cx0) + (cy1 - cy0) > 60:
                continue
            for cx in range(cx0, cx1 + 1):
                for cy in range(cy0, cy1 + 1):
                    luoi.setdefault((cx, cy), []).append(seg)
    return luoi


def _bi_cat(hop, luoi, canh):
    """Ten net dau tien cat qua o chu nay, None neu khong co."""
    da_xet = set()
    for cx in range(int(hop[0] // canh), int(hop[2] // canh) + 1):
        for cy in range(int(hop[1] // canh), int(hop[3] // canh) + 1):
            for seg in luoi.get((cx, cy), ()):
                if seg in da_xet:
                    continue
                da_xet.add(seg)
                if _cat_hop(seg, hop):
                    return seg[4]
    return None


def check_text_on_lines(msp, layers=qt.LAYERS, canh=O_LUOI, gioi_han=20):
    """Tim cac dong chu bi net ve cat ngang qua."""
    luoi = _xep_luoi(msp, canh)
    ket_qua = []
    for x0, y0, x1, y1, noi_dung in qt.cac_o_bao(msp, layers):
        lot = min(x1 - x0, y1 - y0) * LOT
        hop = (x0 + lot, y0 + lot, x1 - lot, y1 - lot)
        if hop[2] <= hop[0] or hop[3] <= hop[1]:
            continue
        thu_pham = _bi_cat(hop, luoi, canh)
        if thu_pham:
            ket_qua.append('%s cat qua chu "%s" tai (%.0f, %.0f)'
                           % (thu_pham, noi_dung[:28], x0, y0))
            if len(ket_qua) >= gioi_han:
                break
    return ket_qua
