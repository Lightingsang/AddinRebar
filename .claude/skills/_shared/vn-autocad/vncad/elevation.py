# -*- coding: utf-8 -*-
"""Mat dung cong trinh: coc tang, duong bao, lo mo, ban cong, mai, hatch vat lieu.

Toa do dung tinh bang MET nhan 1000 (mm) so voi coc +0.000 cua cong trinh,
nen truyen thang tri so cao do trong ho so vao la ve dung vi tri.
"""

from ezdxf.enums import TextEntityAlignment as TA

from . import blocks, standards as S


def storey_grid(msp, doc, levels, x0, x1, scale="1/100", side="left", tail=800.0):
    """Duong coc tang net gach cham keo dai qua 2 ben + ky hieu cao do.

    levels: [(ten_tang, cao_do_met), ...]
    """
    for name, z in levels:
        y = z * 1000.0
        msp.add_line((x0 - tail, y), (x1 + tail, y), dxfattribs={"layer": "A-AXIS"})
        mx = x0 - tail if side == "left" else x1 + tail
        blocks.insert(msp, doc, "CAO-DO", (mx, y), scale, "A-SYMBOL",
                      {"COT": ("+%.3f" if z >= 0 else "%.3f") % z})
        msp.add_text(name, dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": S.th("note", scale)
        }).set_placement((mx, y - S.th("note", scale) * 1.6), align=TA.TOP_CENTER)


def ground_line(msp, x0, x1, z=0.0, over=1200.0, layer="A-GROUND"):
    """Duong dat tu nhien net rat dam, keo dai ra 2 ben cong trinh."""
    y = z * 1000.0
    msp.add_line((x0 - over, y), (x1 + over, y), dxfattribs={"layer": layer})


def outline(msp, pts, closed=True, layer="A-ELEV", lw=50):
    """Duong bao cong trinh tren mat dung, net dam nhat."""
    msp.add_lwpolyline(pts, close=closed, dxfattribs={"layer": layer, "lineweight": lw})


def opening(msp, x, z_bot, w, h, kind="wind", panes=2, layer=None):
    """Lo mo tren mat dung. kind: wind | door | glass | louver."""
    lay = layer or {"wind": "A-WIND", "door": "A-DOOR",
                    "glass": "A-WIND", "louver": "A-WIND"}[kind]
    y = z_bot * 1000.0
    box = [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]
    msp.add_lwpolyline(box, close=True, dxfattribs={"layer": lay})
    if kind in ("wind", "glass"):
        for i in range(1, panes):
            xi = x + w * i / panes
            msp.add_line((xi, y), (xi, y + h), dxfattribs={"layer": lay})
        _glass_sheen(msp, x, y, w, h)
    elif kind == "door":
        msp.add_lwpolyline([(x + 60, y), (x + w - 60, y), (x + w - 60, y + h - 60),
                            (x + 60, y + h - 60)], close=True, dxfattribs={"layer": lay})
    elif kind == "louver":
        step = 180.0
        yy = y + step
        while yy < y + h:
            msp.add_line((x, yy), (x + w, yy), dxfattribs={"layer": lay})
            yy += step
    return box


def _glass_sheen(msp, x, y, w, h):
    """Hai net cheo goi y mang kinh, dung nhu ban ve mat dung that."""
    d = min(w, h) * 0.45
    for k in (0.25, 0.45):
        msp.add_line((x + w * k, y + h), (x + w * k - d, y + h - d),
                     dxfattribs={"layer": "A-MAT"})


def balcony(msp, x0, x1, z_floor, h_rail=900.0, slab=150.0, kind="kinh"):
    """Ban cong tren mat dung: ban san nho ra + lan can kinh hoac song."""
    y = z_floor * 1000.0
    msp.add_lwpolyline([(x0, y), (x1, y), (x1, y - slab), (x0, y - slab)],
                       close=True, dxfattribs={"layer": "A-ELEV"})
    top = y + h_rail
    msp.add_lwpolyline([(x0, y), (x0, top), (x1, top), (x1, y)],
                       dxfattribs={"layer": "A-RAILING"})
    msp.add_line((x0, top - 60), (x1, top - 60), dxfattribs={"layer": "A-RAILING"})
    if kind == "song":
        x = x0 + 120
        while x < x1:
            msp.add_line((x, y), (x, top - 60), dxfattribs={"layer": "A-RAILING"})
            x += 120


def parapet(msp, x0, x1, z_roof, h=900.0, cap=120.0):
    """Tuong chan mai co gờ mu bao dinh."""
    y = z_roof * 1000.0
    msp.add_lwpolyline([(x0, y), (x0, y + h), (x1, y + h), (x1, y)],
                       dxfattribs={"layer": "A-ELEV", "lineweight": 40})
    msp.add_lwpolyline([(x0 - 60, y + h), (x1 + 60, y + h), (x1 + 60, y + h + cap),
                        (x0 - 60, y + h + cap)], close=True,
                       dxfattribs={"layer": "A-ROOF"})


def material(msp, pts, pattern="ANSI31", pscale=260.0, angle=0.0, layer="A-MAT"):
    """To hatch mang vat lieu: da op, gach the, son gai, lam nhom."""
    h = msp.add_hatch(color=9, dxfattribs={"layer": layer})
    h.set_pattern_fill(pattern, scale=pscale, angle=angle)
    h.paths.add_polyline_path(pts, is_closed=True)
    return h


def height_dims(msp, doc, levels, x, scale="1/100", side=1):
    """Gong kich thuoc chieu cao tang + tong chieu cao, dat ben phai mat dung."""
    from .annotate import dim_tiers
    zs = sorted(z * 1000.0 for _, z in levels)
    return dim_tiers(msp, doc, [zs, [zs[0], zs[-1]]], x, "v", scale, side=side)


def label_material(msp, text, target, text_point, scale="1/100"):
    """Duong dan ghi vat lieu hoan thien tren mat dung."""
    from .annotate import leader_note
    leader_note(msp, text, target, text_point, scale)
