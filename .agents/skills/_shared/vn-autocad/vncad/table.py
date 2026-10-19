# -*- coding: utf-8 -*-
"""Bang bieu ve thang len giay: thong ke cua, thong ke vat lieu, bang bat ky.

Dung chung cho moi loai bang. Toa do va be rong cot tinh bang mm tren giay
khi ve trong Layout (ty le '1/1'), hoac nhan he so ty le khi ve trong model.
"""

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

ALIGN = {"l": TA.MIDDLE_LEFT, "c": TA.MIDDLE_CENTER, "r": TA.MIDDLE_RIGHT}


def draw(lay, origin, cols, rows, title=None, scale="1/1", row_h=8.0,
         layer="TEXT", head_fill=True):
    """Ve mot bang.

    cols: [(tieu_de, be_rong_mm, can_le)] voi can_le la 'l', 'c' hoac 'r'
    rows: [[o1, o2, ...], ...] moi phan tu la mot dong
    Tra ve (toa_do_day_bang, tong_be_rong).
    """
    k = S.SCALES[scale]
    x0, y0 = origin
    rh = row_h * k
    total = sum(c[1] for c in cols) * k
    h_head = S.th("note", scale) * 0.95
    h_cell = S.th("note", scale) * 0.9

    if title:
        lay.add_text(title.upper(), dxfattribs={
            "layer": layer, "style": "TCVN", "height": S.th("subtitle", scale)
        }).set_placement((x0 + total / 2, y0 + rh * 0.5), align=TA.BOTTOM_CENTER)

    n = len(rows) + 1
    _grid(lay, x0, y0, cols, n, rh, k, total, layer)

    cx = x0
    for ten, w, _ in cols:
        lay.add_text(ten, dxfattribs={
            "layer": layer, "style": "TCVN", "height": h_head
        }).set_placement((cx + w * k / 2, y0 - rh / 2), align=TA.MIDDLE_CENTER)
        cx += w * k

    y = y0
    for row in rows:
        y -= rh
        cx = x0
        for (_, w, al), val in zip(cols, row):
            pad = 1.5 * k
            px = {"l": cx + pad, "c": cx + w * k / 2, "r": cx + w * k - pad}[al]
            lay.add_text(str(val), dxfattribs={
                "layer": layer, "style": "TCVN", "height": h_cell
            }).set_placement((px, y - rh / 2), align=ALIGN[al])
            cx += w * k
    return y - rh, total


def _grid(lay, x0, y0, cols, n_rows, rh, k, total, layer):
    """Ke duong ngang va duong doc cua bang."""
    for i in range(n_rows + 1):
        y = y0 - rh * i
        lw = 40 if i in (0, 1, n_rows) else 13
        lay.add_line((x0, y), (x0 + total, y),
                     dxfattribs={"layer": layer, "lineweight": lw})
    cx = x0
    for _, w, _ in cols:
        lay.add_line((cx, y0), (cx, y0 - rh * n_rows),
                     dxfattribs={"layer": layer, "lineweight": 25})
        cx += w * k
    lay.add_line((cx, y0), (cx, y0 - rh * n_rows),
                 dxfattribs={"layer": layer, "lineweight": 25})


# ---------------------------------------------------------------------------
# BANG THONG KE CUA (theo mau ho so thuc te)
# ---------------------------------------------------------------------------
DOOR_COLS = [("KY HIEU", 18, "c"), ("LOAI CUA", 42, "l"), ("RONG (mm)", 20, "c"),
             ("CAO (mm)", 20, "c"), ("SL", 12, "c"), ("VAT LIEU", 48, "l"),
             ("GHI CHU", 34, "l")]


def door_schedule(lay, origin, doors, scale="1/1", title="BANG THONG KE CUA"):
    """Bang thong ke cua di va cua so. Tu cong tong so luong o dong cuoi."""
    rows = [[d[0], d[1], d[2], d[3], d[4], d[5], d[6]] for d in doors]
    tong = sum(d[4] for d in doors)
    rows.append(["", "TONG CONG", "", "", tong, "", ""])
    return draw(lay, origin, DOOR_COLS, rows, title, scale, row_h=7.5)
