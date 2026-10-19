# -*- coding: utf-8 -*-
"""Ghi chu ban ve: kich thuoc 3 lop, ten phong, cao do, ky hieu thep, ghi chu.

Quy tac chong va cham: moi lop kich thuoc tu day ra ngoai 1 khoang co dinh,
bong bong truc luon nam ngoai lop ngoai cung nen khong bao gio bi dim cat qua.
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from . import blocks, standards as S
from .document import dimstyle_for


# ---------------------------------------------------------------------------
# 1. KICH THUOC 3 LOP
# ---------------------------------------------------------------------------
def dim_chain(msp, doc, coords, base_coord, axis="h", scale="1/100",
              side=-1, offset_mm=None, layer="DIM"):
    """Ve mot day kich thuoc noi tiep.

    coords: danh sach toa do cac diem gong (theo truc dang do)
    base_coord: toa do canh doi dien (y neu axis='h', x neu axis='v')
    side: -1 do phia duoi/trai, +1 do phia tren/phai
    Tra ve toa do duong kich thuoc vua ve.
    """
    k = S.SCALES[scale]
    off = (S.DIM_FIRST_OFFSET if offset_mm is None else offset_mm) * k
    line = base_coord + side * off
    style = dimstyle_for(doc, scale)
    for a, b in zip(coords, coords[1:]):
        if abs(b - a) < 1e-6:
            continue
        if axis == "h":
            p1, p2, bs = (a, base_coord), (b, base_coord), (0, line)
        else:
            p1, p2, bs = (base_coord, a), (base_coord, b), (line, 0)
        dim = msp.add_linear_dim(
            base=bs, p1=p1, p2=p2, angle=0 if axis == "h" else 90,
            dimstyle=style, dxfattribs={"layer": layer},
        )
        dim.render()
    return line


def dim_tiers(msp, doc, tiers, base_coord, axis="h", scale="1/100", side=-1, layer="DIM"):
    """Ve nhieu lop kich thuoc: lop 0 chi tiet, lop 1 tim truc, lop 2 phu bi.

    Tra ve toa do NGOAI CUNG de dat hang bong bong truc.
    """
    k = S.SCALES[scale]
    off = S.DIM_FIRST_OFFSET
    last = base_coord
    for tier in tiers:
        last = dim_chain(msp, doc, tier, base_coord, axis, scale, side, off, layer)
        off += S.DIM_TIER_GAP
    return last + side * S.BUBBLE_GAP * k


def grid_bubbles(msp, doc, named_coords, line_coord, axis="h", scale="1/100",
                 base_coord=None, layer="A-GRID-BUBBLE"):
    """Dat hang bong bong truc va keo duong giong ve cong trinh.

    named_coords: [("1", 0.0), ("2", 3000.0), ...]
    line_coord: toa do tam hang bong bong (lay tu dim_tiers)
    base_coord: neu co, ve duong giong tu bong bong toi canh cong trinh
    """
    for name, c in named_coords:
        pt = (c, line_coord) if axis == "h" else (line_coord, c)
        blocks.insert(msp, doc, "BONGBONG-TRUC", pt, scale, layer, {"TEN": name})
        if base_coord is not None:
            r = S.BUBBLE_R * S.SCALES[scale]
            sgn = 1 if base_coord > line_coord else -1
            if axis == "h":
                msp.add_line((c, line_coord + sgn * r), (c, base_coord),
                             dxfattribs={"layer": "A-AXIS"})
            else:
                msp.add_line((line_coord + sgn * r, c), (base_coord, c),
                             dxfattribs={"layer": "A-AXIS"})


# ---------------------------------------------------------------------------
# 2. NHAN PHONG, CAO DO, TIEU DE HINH
# ---------------------------------------------------------------------------
def room_label(msp, doc, point, name, area=None, level=None, scale="1/100",
               rotation=0.0):
    """Ten phong + dien tich + cao do, xep chong 3 dong canh giua.

    rotation=90 de ghi doc theo phong: phong hep va dai nhu ban cong hay hanh
    lang khong du be ngang cho mot dong chu nam ngang, ghi ngang la chu tran
    ca ra hai ben tuong.
    """
    k = S.SCALES[scale]
    x, y = point
    r = math.radians(rotation)
    def dat(txt, cao, buoc):
        """Dat mot dong chu lui xuong 'buoc' theo phuong vuong goc voi dong chu."""
        px = x + buoc * math.sin(r)
        py = y + buoc * math.cos(r)
        msp.add_text(txt, dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": cao, "rotation": rotation
        }).set_placement((px, py), align=TA.MIDDLE_CENTER)
        return px, py
    dat(name.upper(), S.th("room", scale), 0.0)
    dy = -S.th("room", scale) * 1.35
    if area is not None:
        dat("DT: %.1f m2" % area, S.th("room_area", scale), dy)
        dy -= S.th("room_area", scale) * 1.35
    if level is not None:
        px, py = x + dy * math.sin(r), y + dy * math.cos(r)
        blocks.insert(msp, doc, "CAO-DO-MB", (px - 2.2 * k, py - 1.8 * k), scale,
                      "A-SYMBOL", {"COT": _fmt_level(level)})


def level_mark(msp, doc, point, value, scale="1/100", plan=True):
    """Ky hieu cao do. value tinh bang met, in ra 3 chu so thap phan."""
    name = "CAO-DO-MB" if plan else "CAO-DO"
    return blocks.insert(msp, doc, name, point, scale, "A-SYMBOL",
                         {"COT": _fmt_level(value)})


def _fmt_level(v):
    return ("+%.3f" if v >= 0 else "%.3f") % v


def view_title(msp, point, title, scale_label, scale="1/100"):
    """Tieu de hinh chieu + ty le + gach chan kep, dat duoi moi ban ve."""
    x, y = point
    h = S.th("subtitle", scale)
    msp.add_text(title.upper(), dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": h
    }).set_placement((x, y), align=TA.MIDDLE_CENTER)
    w = len(title) * h * 0.62
    for i, dy in enumerate((-h * 0.55, -h * 0.85)):
        msp.add_line((x - w / 2, y + dy), (x + w / 2, y + dy),
                     dxfattribs={"layer": "TEXT", "lineweight": 50 if i == 0 else 13})
    msp.add_text("TY LE : %s" % scale_label, dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": S.th("note", scale)
    }).set_placement((x, y - h * 1.9), align=TA.MIDDLE_CENTER)


# ---------------------------------------------------------------------------
# 3. GHI CHU KY THUAT / KY HIEU THEP
# ---------------------------------------------------------------------------
def rebar(dia, spacing=None, count=None):
    """Chuoi ghi thep chuan ban ve VN: rebar(8,150) -> 'Ø8a150'; rebar(16,count=2) -> '2Ø16'."""
    s = "Ø%d" % dia
    if spacing:
        s += "a%d" % spacing
    if count:
        s = "%d%s" % (count, s)
    return s


def leader_note(msp, text, target, text_point, scale="1/100", layer="TEXT"):
    """Duong dan chu: gap khuc 2 doan + chu nam tren doan ngang."""
    k = S.SCALES[scale]
    tx, ty = text_point
    elbow = (tx - 4 * k if tx > target[0] else tx + 4 * k, ty)
    msp.add_lwpolyline([target, elbow, (tx, ty)], dxfattribs={"layer": "A-SYMBOL"})
    align = TA.BOTTOM_LEFT if tx > target[0] else TA.BOTTOM_RIGHT
    msp.add_text(text, dxfattribs={
        "layer": layer, "style": "TCVN", "height": S.th("note", scale)
    }).set_placement((tx, ty + 0.3 * k), align=align)


def note_block(msp, point, title, lines, scale="1/100", layer="TEXT"):
    """Khoi GHI CHU danh so, dat canh ban ve."""
    k = S.SCALES[scale]
    x, y = point
    ht = S.th("label", scale)
    hn = S.th("note", scale)
    msp.add_text(title.upper(), dxfattribs={
        "layer": layer, "style": "TCVN", "height": ht}).set_placement((x, y), align=TA.TOP_LEFT)
    msp.add_line((x, y - ht * 1.25), (x + 62 * k, y - ht * 1.25), dxfattribs={"layer": layer})
    cy = y - ht * 2.1
    for i, ln in enumerate(lines, 1):
        msp.add_text("%d. %s" % (i, ln), dxfattribs={
            "layer": layer, "style": "TCVN", "height": hn}).set_placement((x, cy), align=TA.TOP_LEFT)
        cy -= hn * 1.75
    return cy

def so_lop(msp, n, pt, chi_toi=None, r=70.0, scale="1/20", layer="A-SYMBOL"):
    """Vong tron danh so lop cau tao, co the keo mot net chi toi lop tuong ung.

    Net chi xuat phat tu VIEN vong tron chu khong tu tam: keo tu tam thi net
    di xuyen qua chinh con so, nhin ra mot con so bi gach mot vach.
    """
    msp.add_circle(pt, r, dxfattribs={"layer": layer})
    msp.add_text(str(n), dxfattribs={
        "layer": layer, "style": "TCVN", "height": S.th("note", scale) * 0.8
    }).set_placement(pt, align=TA.MIDDLE_CENTER)
    if chi_toi:
        dx, dy = chi_toi[0] - pt[0], chi_toi[1] - pt[1]
        d = max(math.hypot(dx, dy), 1e-6)
        if d > r:
            msp.add_line((pt[0] + dx / d * r, pt[1] + dy / d * r), chi_toi,
                         dxfattribs={"layer": layer})
