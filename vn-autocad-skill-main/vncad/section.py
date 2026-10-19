# -*- coding: utf-8 -*-
"""Mat cat cong trinh: san cat, dam cat, tuong cat, cau thang cat, mong.

Cau kien BE TONG bi mat phang cat di qua thi to hatch AR-CONC va ve net dam nhat.
Cau kien nam sau mat cat ve net manh tren layer A-ELEV.
"""

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

CUT_LW = 50      # net cat chiu luc
BACK_LW = 18     # net thay phia sau mat cat


def _box(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def _concrete(msp, pts, layer="A-CONC"):
    h = msp.add_hatch(color=8, dxfattribs={"layer": layer})
    h.set_pattern_fill("AR-CONC", scale=6.0)
    h.paths.add_polyline_path(pts, is_closed=True)


def slab(msp, x0, x1, z_top, thk=S.SLAB_T, hatch=True, layer="A-WALL"):
    """San BTCT bi cat: mat tren dung dung cao do hoan thien."""
    y1 = z_top * 1000.0
    pts = _box(x0, y1 - thk, x1, y1)
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": CUT_LW})
    if hatch:
        _concrete(msp, pts)
    return pts


def beam(msp, x_center, z_top, w=200.0, h=400.0, label=None, scale="1/100",
         layer="A-WALL"):
    """Dam BTCT bi cat ngang, day dam treo duoi mat san."""
    y1 = z_top * 1000.0
    pts = _box(x_center - w / 2, y1 - h, x_center + w / 2, y1)
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": CUT_LW})
    _concrete(msp, pts)
    if label:
        msp.add_text(label, dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": S.th("note", scale)
        }).set_placement((x_center, y1 - h - 150), align=TA.TOP_CENTER)
    return pts


def wall(msp, x_center, z_bot, z_top, t=S.WALL_EXT, layer="A-WALL", hatch=True):
    """Tuong xay bi cat doc, hatch gach nghieng 45 do."""
    pts = _box(x_center - t / 2, z_bot * 1000.0, x_center + t / 2, z_top * 1000.0)
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": 40})
    if hatch:
        h = msp.add_hatch(color=9, dxfattribs={"layer": "A-WALL-HATCH"})
        h.set_pattern_fill("ANSI31", scale=t * 0.12)
        h.paths.add_polyline_path(pts, is_closed=True)
    return pts


def column_back(msp, x_center, z_bot, z_top, w=220.0, layer="A-ELEV"):
    """Cot nam phia sau mat phang cat, ve net manh khong hatch."""
    msp.add_lwpolyline(_box(x_center - w / 2, z_bot * 1000.0, x_center + w / 2,
                            z_top * 1000.0), close=True,
                       dxfattribs={"layer": layer, "lineweight": BACK_LW})


def footing(msp, x_center, z_bot, w=1400.0, h=700.0, lean=100.0, layer="A-WALL"):
    """Dai mong BTCT tren mat cat, kem lop be tong lot."""
    y0 = z_bot * 1000.0
    lot = _box(x_center - w / 2 - lean, y0 - lean, x_center + w / 2 + lean, y0)
    msp.add_lwpolyline(lot, close=True, dxfattribs={"layer": layer, "lineweight": 13})
    pts = _box(x_center - w / 2, y0, x_center + w / 2, y0 + h)
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": CUT_LW})
    _concrete(msp, pts)
    return pts


def pile(msp, x_center, z_top, length=8000.0, size=S.PILE, layer="A-ELEV"):
    """Coc ep BTCT tren mat cat, ve net dut ngat o day de bao hieu con dai."""
    y1 = z_top * 1000.0
    pts = _box(x_center - size / 2, y1 - length, x_center + size / 2, y1)
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": 30})
    y = y1 - length
    for dx in (-size / 2, size / 2):
        msp.add_line((x_center + dx, y), (x_center + dx - 120, y - 220),
                     dxfattribs={"layer": layer})


def stair(msp, x_start, z_bot, z_top, steps, tread=270.0, going="right",
          thk=120.0, layer="A-STAIR"):
    """Ve thang tren mat cat: duong rang cua bac + ban thang BTCT phia duoi."""
    rise = (z_top - z_bot) * 1000.0 / steps
    sgn = 1 if going == "right" else -1
    x, y = x_start, z_bot * 1000.0
    pts = [(x, y)]
    for _ in range(steps):
        y += rise
        pts.append((x, y))
        x += sgn * tread
        pts.append((x, y))
    msp.add_lwpolyline(pts, dxfattribs={"layer": layer, "lineweight": 30})
    # ban thang BTCT: duong song song duong noi mui bac, lech xuong theo phuong dung
    import math
    dy_ban = thk * math.hypot(rise, tread) / tread
    msp.add_line((x_start, z_bot * 1000.0 - dy_ban), (x, y - dy_ban),
                 dxfattribs={"layer": layer, "lineweight": CUT_LW})
    return (x, y)


def railing(msp, x0, x1, z_floor, h=900.0, layer="A-RAILING"):
    """Lan can tren mat cat: 2 net dung + tay vin ngang."""
    y = z_floor * 1000.0
    for x in (x0, x1):
        msp.add_line((x, y), (x, y + h), dxfattribs={"layer": layer})
    msp.add_line((x0, y + h), (x1, y + h), dxfattribs={"layer": layer})
    msp.add_line((x0, y + h - 60), (x1, y + h - 60), dxfattribs={"layer": layer})


def room_tag(msp, point, name, scale="1/100"):
    """Ten khong gian ghi trong long mat cat."""
    msp.add_text(name.upper(), dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": S.th("room", scale)
    }).set_placement(point, align=TA.MIDDLE_CENTER)
