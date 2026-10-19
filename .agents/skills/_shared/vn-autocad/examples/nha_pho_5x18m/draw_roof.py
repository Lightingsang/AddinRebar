# -*- coding: utf-8 -*-
"""Ve mat bang mai: san thuong, senô thu nuoc, buong thang, huong doc mai."""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, arch, fixtures as fx, standards as S

from . import layout as L

SC = "1/100"
Z_MAI = 10.600
STAIR_BOX = (9500.0, 13000.0)          # buong thang len mai theo phuong X
SENO = 300.0                            # be rong senô quanh mep mai


def draw_roof_plan(msp, doc, origin=(0.0, 0.0)):
    """Mat bang mai tai toa do that, goc dat (ox, oy)."""
    ox, oy = origin
    _parapet(msp, ox, oy)
    _stair_box(msp, doc, ox, oy)
    _drainage(msp, doc, ox, oy)
    _slope_arrows(msp, ox, oy)
    an.room_label(msp, doc, (ox + 4500, oy + 3400), "San thuong", 62.0, Z_MAI, SC)


def _parapet(msp, ox, oy):
    """Tuong chan mai bao quanh, ve 2 net nhu tuong cat."""
    for p1, p2 in (((0, 0), (18000, 0)), ((0, 5000), (18000, 5000)),
                   ((0, 0), (0, 5000)), ((18000, 0), (18000, 5000))):
        arch.wall(msp, (ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]),
                  t=110, hatch=False)
    # duong senô chay sat mep trong tuong chan mai
    x0, y0 = ox + 110, oy + 110
    x1, y1 = ox + 18000 - 110, oy + 5000 - 110
    msp.add_lwpolyline(
        [(x0 + SENO, y0 + SENO), (x1 - SENO, y0 + SENO),
         (x1 - SENO, y1 - SENO), (x0 + SENO, y1 - SENO)],
        close=True, dxfattribs={"layer": "A-ROOF"})


def _stair_box(msp, doc, ox, oy):
    """Buong thang nho tren mai, co cua di ra san thuong."""
    a, b = STAIR_BOX
    for p1, p2 in (((a, 0), (a, 5000)), ((b, 0), (b, 5000))):
        arch.wall(msp, (ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]), t=220)
    ops = arch.wall(msp, (ox + a, oy + 5000), (ox + b, oy + 5000), t=220,
                    openings=[(1200, 900)])
    if ops:
        s, e, u, n, t = ops[0]
        arch.door(msp, s, e, u, n, t)
    an.room_label(msp, doc, (ox + 11250, oy + 2500), "Buong thang", 12.3, 13.400, SC)


def _drainage(msp, doc, ox, oy):
    """Bon pheu thu nuoc mua dat 4 goc san thuong."""
    for x, y in ((900, 700), (17100, 700), (900, 4300), (17100, 4300)):
        fx.place(msp, doc, "PHEU-THU", (ox + x, oy + y))
        an.leader_note(msp, "Pheu thu D90", (ox + x, oy + y),
                       (ox + x + (1800 if x < 9000 else -1800), oy + y + 600), SC)


def _slope_arrows(msp, ox, oy):
    """Mui ten huong doc mai 1 phan tram ve phia pheu thu."""
    h = S.th("note", SC)
    for x0, x1, y in ((5200, 1400, 2500), (12800, 16600, 2500)):
        msp.add_line((ox + x0, oy + y), (ox + x1, oy + y), dxfattribs={"layer": "A-ROOF"})
        sgn = 1 if x1 > x0 else -1
        for dy in (-260, 260):
            msp.add_line((ox + x1, oy + y), (ox + x1 - sgn * 320, oy + y + dy),
                         dxfattribs={"layer": "A-ROOF"})
        msp.add_text("i = 1%", dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": h
        }).set_placement(((ox + x0 + ox + x1) / 2, oy + y + 200), align=TA.BOTTOM_CENTER)
