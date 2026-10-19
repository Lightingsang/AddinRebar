# -*- coding: utf-8 -*-
"""Thu vien BLOCK ky hieu ban ve.

Ky hieu chu thich (bong bong truc, cao do, mat cat...) dinh nghia theo mm tren GIAY,
khi chen thi nhan he so ty le. Nho vay doi ty le 1/100 sang 1/50 khong phai ve lai.
"""

import math

from . import standards as S

_SYMBOL_BUILDERS = {}


def register(name):
    def deco(fn):
        _SYMBOL_BUILDERS[name] = fn
        return fn
    return deco


def ensure(doc, name):
    """Tao block trong ban ve neu chua co. Tra ve ten block."""
    if name not in doc.blocks:
        _SYMBOL_BUILDERS[name](doc.blocks.new(name=name))
    return name


def insert(msp, doc, name, point, scale="1/100", layer="A-SYMBOL", attribs=None, rotation=0):
    """Chen ky hieu chu thich, tu dong nhan he so ty le."""
    k = S.SCALES[scale]
    ref = msp.add_blockref(
        ensure(doc, name), point,
        dxfattribs={"layer": layer, "xscale": k, "yscale": k, "zscale": k, "rotation": rotation},
    )
    if attribs:
        ref.add_auto_attribs(attribs)
    return ref


# ---------------------------------------------------------------------------
# Bong bong truc: vong tron rong 4mm + chu ten truc o tam
# ---------------------------------------------------------------------------
@register("BONGBONG-TRUC")
def _grid_bubble(blk):
    r = S.BUBBLE_R
    blk.add_circle((0, 0), r, dxfattribs={"layer": "A-GRID-BUBBLE"})
    blk.add_attdef(
        "TEN", (0, 0),
        dxfattribs={"layer": "A-GRID-BUBBLE", "height": 2.4, "style": "TCVN",
                    "halign": 1, "valign": 2, "align_point": (0, 0)},
    )


# ---------------------------------------------------------------------------
# Ky hieu cao do: tam giac rong + duong ngang + tri so (vd +5.485)
# ---------------------------------------------------------------------------
@register("CAO-DO")
def _level_mark(blk):
    lay = {"layer": "A-SYMBOL"}
    blk.add_lwpolyline([(-1.6, 1.6), (0, 0), (1.6, 1.6)], dxfattribs=lay)
    blk.add_line((-4.5, 1.6), (4.5, 1.6), dxfattribs=lay)
    blk.add_attdef(
        "COT", (0, 2.2),
        dxfattribs={"layer": "TEXT", "height": 2.0, "style": "TCVN",
                    "halign": 1, "valign": 0, "align_point": (0, 2.2)},
    )


# ---------------------------------------------------------------------------
# Ky hieu cao do dang lap day (dung tren mat bang, hinh vuong xoay 45 do)
# ---------------------------------------------------------------------------
@register("CAO-DO-MB")
def _level_mark_plan(blk):
    lay = {"layer": "A-SYMBOL"}
    d = 1.8
    blk.add_lwpolyline([(0, d), (d, 0), (0, -d), (-d, 0)], close=True, dxfattribs=lay)
    blk.add_solid([(0, d), (d, 0), (0, 0)], dxfattribs=lay)
    blk.add_attdef(
        "COT", (2.6, 0.4),
        dxfattribs={"layer": "TEXT", "height": 2.0, "style": "TCVN"},
    )


# ---------------------------------------------------------------------------
# Ky hieu mat cat: mui ten + so hieu trong vong tron
# ---------------------------------------------------------------------------
@register("KY-HIEU-MAT-CAT")
def _section_mark(blk):
    lay = {"layer": "A-SYMBOL"}
    blk.add_circle((0, 0), 3.5, dxfattribs=lay)
    blk.add_lwpolyline([(0, -3.5), (0, -7.5)], dxfattribs=lay)
    blk.add_solid([(-1.2, -7.5), (1.2, -7.5), (0, -10.0)], dxfattribs=lay)
    blk.add_attdef(
        "SO", (0, 0),
        dxfattribs={"layer": "TEXT", "height": 2.4, "style": "TCVN",
                    "halign": 1, "valign": 2, "align_point": (0, 0)},
    )


# ---------------------------------------------------------------------------
# Bong bong chi tiet: vong tron net dut + so hieu / so to
# ---------------------------------------------------------------------------
@register("BONGBONG-CHI-TIET")
def _detail_bubble(blk):
    lay = {"layer": "A-SYMBOL"}
    blk.add_circle((0, 0), 5.0, dxfattribs=lay)
    blk.add_line((-5.0, 0), (5.0, 0), dxfattribs=lay)
    blk.add_attdef("SO", (0, 1.0), dxfattribs={
        "layer": "TEXT", "height": 2.4, "style": "TCVN",
        "halign": 1, "valign": 0, "align_point": (0, 1.0)})
    blk.add_attdef("TO", (0, -3.2), dxfattribs={
        "layer": "TEXT", "height": 2.0, "style": "TCVN",
        "halign": 1, "valign": 0, "align_point": (0, -3.2)})


# ---------------------------------------------------------------------------
# Mui ten huong Bac
# ---------------------------------------------------------------------------
@register("HUONG-BAC")
def _north(blk):
    lay = {"layer": "A-SYMBOL"}
    blk.add_circle((0, 0), 6.0, dxfattribs=lay)
    blk.add_solid([(0, 6.0), (-2.0, -2.5), (0, -0.8)], dxfattribs=lay)
    blk.add_lwpolyline([(0, 6.0), (2.0, -2.5), (0, -0.8)], close=True, dxfattribs=lay)
    blk.add_text("B", dxfattribs={"layer": "TEXT", "height": 2.6, "style": "TCVN"}).set_placement(
        (0, 7.0), align=__import__("ezdxf").enums.TextEntityAlignment.BOTTOM_CENTER)


# ---------------------------------------------------------------------------
# Dau gach cheo kien truc dung cho duong kich thuoc
# ---------------------------------------------------------------------------
@register("ARCHTICK-VN")
def _tick(blk):
    blk.add_line((-0.9, -0.9), (0.9, 0.9), dxfattribs={"layer": "DIM"})


# ---------------------------------------------------------------------------
# Mui ten huong len / xuong cau thang
# ---------------------------------------------------------------------------
def stair_arrow(msp, start, end, layer="A-STAIR"):
    """Ve mui ten chi huong di cua ve thang (toa do thuc, khong theo giay)."""
    msp.add_line(start, end, dxfattribs={"layer": layer})
    dx, dy = end[0] - start[0], end[1] - start[1]
    ang = math.atan2(dy, dx)
    size = 220.0
    for sign in (1, -1):
        a = ang + sign * math.radians(155)
        msp.add_line(end, (end[0] + size * math.cos(a), end[1] + size * math.sin(a)),
                     dxfattribs={"layer": layer})
