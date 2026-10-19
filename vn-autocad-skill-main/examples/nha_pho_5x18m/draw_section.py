# -*- coding: utf-8 -*-
"""Ve mat cat A-A cat doc nha theo chieu sau 18m, di qua gieng troi va cau thang."""

from vncad import elevation as el, section as sec

from . import layout as L

SC = "1/100"
X0, X1 = 0.0, 18000.0
BEAM_X = [x for _, x in L.AXIS_X]                 # dam ngang tai moi truc
SLAB_SPANS = [(0.0, 9500.0), (13000.0, 18000.0)]  # chua o thong tang giua nha


def draw_section_aa(msp, doc):
    """Ve tron mat cat A-A tai toa do that (coc +0.000 la goc)."""
    el.ground_line(msp, X0, X1, L.Z_DAT)
    _foundation(msp)
    _floors(msp)
    _stairs(msp)
    _envelope(msp)
    el.storey_grid(msp, doc, L.LEVELS, X0, X1, SC, side="left", tail=1600)


def _foundation(msp):
    """Dai mong duoi moi truc + coc ep + da kieng noi cac dai."""
    for x in BEAM_X:
        sec.footing(msp, x, L.Z_MONG, w=1400, h=700)
        sec.pile(msp, x, L.Z_MONG, length=1200)   # ve ngat, coc that dai 8m
    for a, b in zip(BEAM_X, BEAM_X[1:]):
        sec.slab(msp, a + 700, b - 700, L.Z_MONG + 0.700, thk=350, layer="A-WALL")


def _floors(msp):
    """San BTCT tung tang, dam ngang tai truc, tuong ngan phia sau mat cat."""
    for _, z in L.LEVELS[:4]:
        for a, b in (SLAB_SPANS if z > 1.0 else [(X0, X1)]):
            sec.slab(msp, a, b, z, thk=100)
        for x in BEAM_X:
            inside = any(a - 1 <= x <= b + 1 for a, b in
                         (SLAB_SPANS if z > 1.0 else [(X0, X1)]))
            if inside:
                sec.beam(msp, x, z, w=200, h=400, label="D1" if x in (X0, X1) else None,
                         scale=SC)
    # cot nam sau mat phang cat
    for z0, z1 in zip([L.Z_MONG + 0.7] + [z for _, z in L.LEVELS[:3]],
                      [z for _, z in L.LEVELS[:4]]):
        for x in (X0, X1):
            sec.column_back(msp, x, z0, z1, w=220)
    # lan can bao ho thong tang tai 2 tang tren
    for _, z in L.LEVELS[1:3]:
        sec.railing(msp, 9500, 13000, z, h=900)


def _stairs(msp):
    """Ve thang tu tret len lau 2, dat trong khoang gieng troi."""
    pairs = list(zip(L.LEVELS[:3], L.LEVELS[1:4]))
    for (_, z0), (_, z1) in pairs:
        sec.stair(msp, 9700, z0, z1, steps=10, tread=270, going="right")


def _envelope(msp):
    """Tuong bao truoc sau, tuong chan mai, phong bao ngoai."""
    top = L.LEVELS[3][1]
    sec.wall(msp, X0, L.Z_DAT, top, t=220)
    sec.wall(msp, X1, L.Z_DAT, top, t=220)
    el.parapet(msp, X0 - 110, X1 + 110, top, h=900)
    # buong thang len mai che
    sec.wall(msp, 9500, top, L.LEVELS[4][1], t=220)
    sec.wall(msp, 13000, top, L.LEVELS[4][1], t=220)
    sec.slab(msp, 9500 - 110, 13000 + 110, L.LEVELS[4][1], thk=100)


ROOM_TAGS = [
    ((4500, 1.9), "GARA O TO"), ((15500, 1.9), "BEP + PHONG AN"),
    ((4500, 5.5), "PHONG NGU MASTER 1"), ((15500, 5.5), "PHONG NGU MASTER 2"),
    ((4500, 8.9), "PHONG THO"), ((15500, 8.9), "PHONG NGU MASTER 3"),
    # Nhan gieng troi lech sang trai va len cao 6.0: dat giua o 5.5 thi ban
    # thang tu tang 2 len tang 3 chay cheo dung qua giua dong chu.
    ((10400, 6.0), "GIENG TROI"), ((11200, 12.0), "BUONG THANG"),
]


def draw_room_tags(msp):
    for (x, z), name in ROOM_TAGS:
        sec.room_tag(msp, (x, z * 1000.0), name, SC)
