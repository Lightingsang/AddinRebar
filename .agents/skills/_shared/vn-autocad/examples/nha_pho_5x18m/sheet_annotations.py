# -*- coding: utf-8 -*-
"""Gong kich thuoc va bong bong truc cho tung loai hinh chieu trong model space.

Tach rieng khoi model.py de moi ham chi lo mot viec.
Tieu de hinh chieu KHONG ve o day: tieu de thuoc ve to giay, xem sheets.py.
"""

from vncad import annotate as an

from . import layout as L

SC = "1/100"
Z_DIM_SECTION = -2.35     # hang kich thuoc ngang cua mat cat, thap hon day mong


def _title(msp, x, line, title):
    if title:
        an.view_title(msp, (x, line - 2600), title, SC.replace("1/", "1/"), SC)


def plan_grid(msp, doc, ox, oy, full=False, title=None):
    """Kich thuoc va bong bong truc cho mot mat bang ket cau."""
    xs = [ox + x for _, x in L.AXIS_X]
    tiers = [xs, [xs[0], xs[-1]]]
    line = an.dim_tiers(msp, doc, tiers, oy, "h", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, ox + x) for n, x in L.AXIS_X], line, "h", SC,
                    base_coord=oy)
    ys = [oy + y for _, y in L.AXIS_Y]
    vline = an.dim_tiers(msp, doc, [ys], ox, "v", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, oy + y) for n, y in L.AXIS_Y], vline, "v", SC,
                    base_coord=ox)
    _title(msp, ox + L.L / 2, line, title)
    return line


def section_grid_and_dims(msp, doc, ox, oy, title=None):
    """Mat cat: bong bong truc so o duoi, kich thuoc chieu cao tang o ben phai."""
    y_base = oy + Z_DIM_SECTION * 1000.0
    xs = [ox + x for _, x in L.AXIS_X]
    line = an.dim_tiers(msp, doc, [xs, [xs[0], xs[-1]]], y_base, "h", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, ox + x) for n, x in L.AXIS_X], line, "h", SC,
                    base_coord=y_base)
    _height_dims(msp, doc, ox + L.L, oy)
    _title(msp, ox + L.L / 2, line, title)
    return line


def elevation_grid_and_dims(msp, doc, ox, oy, title=None):
    """Mat dung: bong bong truc chu A B o duoi, kich thuoc chieu cao o ben phai."""
    y_base = oy + L.Z_DAT * 1000.0
    xs = [ox + y for _, y in L.AXIS_Y]
    line = an.dim_tiers(msp, doc, [xs, [xs[0], xs[-1]]], y_base, "h", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, ox + y) for n, y in L.AXIS_Y], line, "h", SC,
                    base_coord=y_base)
    _height_dims(msp, doc, ox + L.W, oy)
    _title(msp, ox + L.W / 2, line, title)
    return line


def _height_dims(msp, doc, x_right, oy):
    """Hai lop kich thuoc dung: chieu cao tung tang va tong chieu cao."""
    zs = [oy + L.Z_DAT * 1000.0] + [oy + z * 1000.0 for _, z in L.LEVELS]
    an.dim_tiers(msp, doc, [zs, [zs[0], zs[-1]]], x_right, "v", SC, side=+1)
