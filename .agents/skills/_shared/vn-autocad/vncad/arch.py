# -*- coding: utf-8 -*-
"""Cau kien kien truc: tuong, cot, cua di, cua so, cau thang, lan can, san.

Tuong tu dong NGAT NET tai lo cua va tai mep cot, dung nhu ban ve thi cong thuc te.
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S


def _uv(p1, p2):
    dx, dy = p2[0] - p1[0], p2[1] - p1[1]
    L = math.hypot(dx, dy)
    return (dx / L, dy / L), (-dy / L, dx / L), L


def _at(p, u, d):
    return (p[0] + u[0] * d, p[1] + u[1] * d)


# ---------------------------------------------------------------------------
# TUONG
# ---------------------------------------------------------------------------
def wall(msp, p1, p2, t=S.WALL_EXT, openings=None, layer="A-WALL",
         hatch_layer="A-WALL-HATCH", hatch=True):
    """Ve tuong day t giua 2 diem TIM tuong, tu dong ngat net tai cac lo mo.

    openings: [(khoang_cach_tu_p1, be_rong), ...]
    Tra ve danh sach lo mo da quy doi ra toa do de ve cua.
    """
    u, n, L = _uv(p1, p2)
    cuts = sorted(openings or [])
    spans, cur = [], 0.0
    for d, w in cuts:
        if d > cur:
            spans.append((cur, d))
        cur = d + w
    if cur < L:
        spans.append((cur, L))

    for a, b in spans:
        if b - a < 1e-6:
            continue
        c = [_at(_at(p1, u, a), n, t / 2), _at(_at(p1, u, b), n, t / 2),
             _at(_at(p1, u, b), n, -t / 2), _at(_at(p1, u, a), n, -t / 2)]
        msp.add_lwpolyline(c, close=True, dxfattribs={"layer": layer})
        if hatch:
            h = msp.add_hatch(color=9, dxfattribs={"layer": hatch_layer})
            h.set_pattern_fill("ANSI31", scale=t * 0.12, angle=0)
            h.paths.add_polyline_path(c, is_closed=True)
    return [( _at(p1, u, d), _at(p1, u, d + w), u, n, t) for d, w in cuts]


def column(msp, center, size=S.COL_TYP, rotation=0, layer="A-COLS",
           hatch_layer="A-COLS-HATCH"):
    """Cot BTCT: net dam + to dac (SOLID). Luon ve SAU tuong de de len tuong."""
    w, h = size
    cx, cy = center
    ca, sa = math.cos(math.radians(rotation)), math.sin(math.radians(rotation))
    pts = []
    for dx, dy in ((-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)):
        pts.append((cx + dx * ca - dy * sa, cy + dx * sa + dy * ca))
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer})
    hz = msp.add_hatch(color=8, dxfattribs={"layer": hatch_layer})
    hz.set_solid_fill()
    hz.paths.add_polyline_path(pts, is_closed=True)
    return pts


# ---------------------------------------------------------------------------
# CUA DI / CUA SO
# ---------------------------------------------------------------------------
def door(msp, p_start, p_end, u, n, t, swing="left", layer="A-DOOR"):
    """Cua di 1 canh: khuon + canh + cung tron ban kinh mo."""
    w = math.dist(p_start, p_end)
    side = 1 if swing == "left" else -1
    hinge = _at(_at(p_start, n, side * t / 2), u, 0)
    leaf_end = _at(hinge, n, -side * w)
    msp.add_line(hinge, leaf_end, dxfattribs={"layer": layer})
    a0 = math.degrees(math.atan2(-side * n[1], -side * n[0]))
    a1 = math.degrees(math.atan2(u[1], u[0]))
    if side < 0:
        a0, a1 = a1, a0
    msp.add_arc(hinge, w, a0, a1, dxfattribs={"layer": layer})
    for s in (1, -1):
        msp.add_line(_at(p_start, n, s * t / 2), _at(p_end, n, s * t / 2),
                     dxfattribs={"layer": layer, "lineweight": 13})


def door_double(msp, p_start, p_end, u, n, t, layer="A-DOOR"):
    """Cua di 2 canh mo doi xung."""
    w = math.dist(p_start, p_end)
    mid = ((p_start[0] + p_end[0]) / 2, (p_start[1] + p_end[1]) / 2)
    for src, side in ((p_start, 1), (p_end, -1)):
        hinge = _at(src, n, t / 2)
        msp.add_line(hinge, _at(hinge, n, -w / 2), dxfattribs={"layer": layer})
        a = math.degrees(math.atan2(-n[1], -n[0]))
        b = math.degrees(math.atan2(side * u[1], side * u[0]))
        msp.add_arc(hinge, w / 2, min(a, b), max(a, b), dxfattribs={"layer": layer})
    msp.add_line(_at(mid, n, t / 2), _at(mid, n, -t / 2),
                 dxfattribs={"layer": layer, "lineweight": 13})


def window(msp, p_start, p_end, n, t, panes=2, layer="A-WIND"):
    """Cua so: khuon 2 net bien + cac net kinh song song ben trong."""
    for i in range(panes + 1):
        off = -t / 2 + t * i / panes
        msp.add_line(_at(p_start, n, off), _at(p_end, n, off), dxfattribs={"layer": layer})


# ---------------------------------------------------------------------------
# CAU THANG
# ---------------------------------------------------------------------------
def stair_flight(msp, origin, run_dir, width, tread=270, steps=10,
                 layer="A-STAIR", numbering=False, scale="1/100", start=1):
    """Mot ve thang thang: cac bac + duong bao. run_dir la (ux, uy) don vi."""
    u = run_dir
    n = (-u[1], u[0])
    for i in range(steps + 1):
        a = _at(origin, u, tread * i)
        msp.add_line(_at(a, n, -width / 2), _at(a, n, width / 2), dxfattribs={"layer": layer})
        if numbering and i < steps:
            c = _at(_at(origin, u, tread * (i + 0.5)), n, width / 2 - 180)
            msp.add_text(str(start + i), dxfattribs={
                "layer": "TEXT", "style": "TCVN", "height": S.th("note", scale) * 0.8
            }).set_placement(c, align=TA.MIDDLE_CENTER)
    end = _at(origin, u, tread * steps)
    for s in (1, -1):
        msp.add_line(_at(origin, n, s * width / 2), _at(end, n, s * width / 2),
                     dxfattribs={"layer": layer})
    return end


def railing(msp, pts, offset=60, layer="A-RAILING"):
    """Lan can 2 net: tay vin + duong chan, ve doc theo tuyen pts."""
    for s in (1, -1):
        out = []
        for i, p in enumerate(pts):
            q = pts[min(i + 1, len(pts) - 1)] if i + 1 < len(pts) else pts[i - 1]
            u, n, _ = _uv(p, q) if i + 1 < len(pts) else _uv(q, p)
            out.append(_at(p, n, s * offset / 2))
        msp.add_lwpolyline(out, dxfattribs={"layer": layer})


# ---------------------------------------------------------------------------
# SAN
# ---------------------------------------------------------------------------
def slab_edge(msp, pts, layer="A-WALL", lw=35):
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": lw})
