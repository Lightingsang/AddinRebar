# -*- coding: utf-8 -*-
"""Hoa van gach lat san, co the ngat mach quanh cho dat chu.

Tach rieng khoi arch.py vi day la viec cua ban ve HOAN THIEN chu khong phai
cau kien kien truc, va vi phan ngat mach can may ham phu rieng.
"""


def floor_tiles(msp, x0, y0, x1, y1, tile=800, layer="A-FLOR", chua=None):
    """Ke hoa van gach lat trong pham vi hinh chu nhat.

    chua: danh sach o chua cho [(cx0, cy0, cx1, cy1), ...]; mach gach NGAT o
    cac o nay. Dung de chua cho ghi ma vat lieu: nguoi ve tay cung ngat mach
    gach quanh dong chu chu khong de mach chay xuyen qua chu. Phai truyen CA
    danh sach nhan cua moi phong, vi phong lon (sanh, gieng troi) trai gach
    de len ca pham vi phong nho long trong no.
    """
    hop = _danh_sach_hop(chua)
    for x in _buoc(x0, x1, tile):
        for a, b in _ngat(y0, y1, hop, x, ngang=False):
            msp.add_line((x, a), (x, b), dxfattribs={"layer": layer})
    for y in _buoc(y0, y1, tile):
        for a, b in _ngat(x0, x1, hop, y, ngang=True):
            msp.add_line((a, y), (b, y), dxfattribs={"layer": layer})


def _danh_sach_hop(chua):
    """Chap nhan mot o don le hoac mot danh sach o."""
    if not chua:
        return []
    return [chua] if isinstance(chua[0], (int, float)) else list(chua)


def _buoc(a, b, buoc):
    """Cac vach mach gach tu a den b."""
    v, ra = a, []
    while v <= b:
        ra.append(v)
        v += buoc
    return ra


def _ngat(a, b, hop, toa_do, ngang):
    """Chia doan [a, b] thanh cac doan con nam ngoai moi o chua cho."""
    doan = [(a, b)]
    for cx0, cy0, cx1, cy1 in hop:
        doc0, doc1 = (cx0, cx1) if ngang else (cy0, cy1)
        ngang0, ngang1 = (cy0, cy1) if ngang else (cx0, cx1)
        if not (ngang0 <= toa_do <= ngang1):
            continue
        moi = []
        for p, q in doan:
            if q <= doc0 or p >= doc1:
                moi.append((p, q))
                continue
            if p < doc0:
                moi.append((p, doc0))
            if q > doc1:
                moi.append((doc1, q))
        doan = moi
    return doan
