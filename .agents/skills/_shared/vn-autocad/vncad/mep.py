# -*- coding: utf-8 -*-
"""Ky hieu va tuyen ong co dien nuoc (MEP) theo thong le ban ve Viet Nam.

Ky hieu dinh nghia theo mm tren GIAY roi nhan he so ty le khi chen,
giong cach lam trong blocks.py, nen doi ty le khong phai ve lai.
"""

import math

from . import standards as S

_B = {}


def _reg(name):
    def deco(fn):
        _B[name] = fn
        return fn
    return deco


def ensure(doc, name):
    if name not in doc.blocks:
        _B[name](doc.blocks.new(name=name))
    return name


def place(msp, doc, name, point, scale="1/100", layer="M-SYMBOL", rotation=0):
    """Chen ky hieu MEP, tu dong nhan he so ty le."""
    k = S.SCALES[scale]
    return msp.add_blockref(ensure(doc, name), point, dxfattribs={
        "layer": layer, "xscale": k, "yscale": k, "zscale": k, "rotation": rotation})


# --- Chieu sang -------------------------------------------------------------
@_reg("DEN-DOWNLIGHT")
def _downlight(blk):
    lay = {"layer": "M-LIGHT"}
    blk.add_circle((0, 0), 2.2, dxfattribs=lay)
    blk.add_line((-2.2, 0), (2.2, 0), dxfattribs=lay)
    blk.add_line((0, -2.2), (0, 2.2), dxfattribs=lay)


@_reg("DEN-OP-TRAN")
def _ceiling_lamp(blk):
    lay = {"layer": "M-LIGHT"}
    blk.add_lwpolyline([(-3, -3), (3, -3), (3, 3), (-3, 3)], close=True, dxfattribs=lay)
    blk.add_circle((0, 0), 2.0, dxfattribs=lay)


@_reg("DEN-TUONG")
def _wall_lamp(blk):
    lay = {"layer": "M-LIGHT"}
    blk.add_circle((0, 1.6), 1.6, dxfattribs=lay)
    blk.add_line((-2.4, 0), (2.4, 0), dxfattribs=lay)


@_reg("QUAT-TRAN")
def _fan(blk):
    lay = {"layer": "M-LIGHT"}
    blk.add_circle((0, 0), 1.0, dxfattribs=lay)
    for a in (0, 90, 180, 270):
        r = math.radians(a)
        blk.add_line((0, 0), (4.2 * math.cos(r), 4.2 * math.sin(r)), dxfattribs=lay)


# --- Dieu khien va o cam ----------------------------------------------------
def _switch(blk, so_cuc):
    lay = {"layer": "M-ELEC"}
    blk.add_circle((0, 0), 1.8, dxfattribs=lay)
    for i in range(so_cuc):
        blk.add_line((0, 1.8), (2.6 + 1.1 * i, 4.0 + 1.1 * i), dxfattribs=lay)


for _n, _c in (("CONG-TAC-1", 1), ("CONG-TAC-2", 2), ("CONG-TAC-3", 3)):
    _B[_n] = (lambda c: (lambda blk: _switch(blk, c)))(_c)


@_reg("O-CAM-DOI")
def _socket(blk):
    lay = {"layer": "M-ELEC"}
    blk.add_arc((0, 0), 2.4, 0, 180, dxfattribs=lay)
    blk.add_line((-2.4, 0), (2.4, 0), dxfattribs=lay)
    blk.add_line((0, 0), (0, 2.4), dxfattribs=lay)


@_reg("TU-DIEN")
def _panel(blk):
    lay = {"layer": "M-ELEC"}
    blk.add_lwpolyline([(-5, -3), (5, -3), (5, 3), (-5, 3)], close=True, dxfattribs=lay)
    for x in (-2.5, 0, 2.5):
        blk.add_line((x, -3), (x, 3), dxfattribs=lay)


@_reg("MAY-LANH")
def _ac(blk):
    lay = {"layer": "M-ELEC"}
    blk.add_lwpolyline([(-5, -1.6), (5, -1.6), (5, 1.6), (-5, 1.6)], close=True,
                       dxfattribs=lay)
    blk.add_line((-5, 0), (5, 0), dxfattribs=lay)


# --- Cap thoat nuoc ---------------------------------------------------------
@_reg("DONG-HO-NUOC")
def _meter(blk):
    lay = {"layer": "M-WATER"}
    blk.add_circle((0, 0), 2.6, dxfattribs=lay)
    blk.add_line((-2.6, 0), (2.6, 0), dxfattribs=lay)
    blk.add_line((-4.2, 0), (-2.6, 0), dxfattribs=lay)
    blk.add_line((2.6, 0), (4.2, 0), dxfattribs=lay)


@_reg("VAN-KHOA")
def _valve(blk):
    lay = {"layer": "M-WATER"}
    blk.add_lwpolyline([(-2.2, -2.0), (0, 0), (-2.2, 2.0)], close=True, dxfattribs=lay)
    blk.add_lwpolyline([(2.2, -2.0), (0, 0), (2.2, 2.0)], close=True, dxfattribs=lay)


@_reg("HO-GA")
def _manhole(blk):
    lay = {"layer": "M-DRAIN"}
    blk.add_lwpolyline([(-4, -4), (4, -4), (4, 4), (-4, 4)], close=True, dxfattribs=lay)
    blk.add_line((-4, -4), (4, 4), dxfattribs=lay)
    blk.add_line((-4, 4), (4, -4), dxfattribs=lay)


@_reg("ONG-DUNG")
def _riser(blk):
    lay = {"layer": "M-DRAIN"}
    blk.add_circle((0, 0), 2.2, dxfattribs=lay)
    blk.add_circle((0, 0), 1.0, dxfattribs=lay)


# --- Tuyen day va tuyen ong -------------------------------------------------
def wire(msp, pts, layer="M-ELEC"):
    """Tuyen day dien am tuong, ve net lien manh noi thiet bi voi cong tac."""
    msp.add_lwpolyline(pts, dxfattribs={"layer": layer})


def pipe(msp, pts, kind="cap", scale="1/100", label=None, label_at=None):
    """Tuyen ong nuoc. kind: 'cap' cap nuoc net lien, 'thoat' net dut.

    label_at: toa do dat chu, de nguoi ve tu tach cac nhan cho khoi de len nhau.
    """
    layer = "M-WATER" if kind == "cap" else "M-DRAIN"
    msp.add_lwpolyline(pts, dxfattribs={"layer": layer})
    if label:
        if label_at is None:
            mid = pts[len(pts) // 2]
            label_at = (mid[0], mid[1] + 120)
        msp.add_text(label, dxfattribs={
            "layer": "M-TEXT", "style": "TCVN", "height": S.th("note", scale)
        }).set_placement(label_at)


def background(msp, walls, columns, layer="M-WALL"):
    """Ve nen kien truc mo (chi tuong va cot) lam nen cho ban ve co dien."""
    for p1, p2, t, _ops in walls:
        dx, dy = p2[0] - p1[0], p2[1] - p1[1]
        L = math.hypot(dx, dy)
        nx, ny = -dy / L * t / 2, dx / L * t / 2
        for s in (1, -1):
            msp.add_line((p1[0] + s * nx, p1[1] + s * ny),
                         (p2[0] + s * nx, p2[1] + s * ny), dxfattribs={"layer": layer})
    for x, y, (w, h) in columns:
        msp.add_lwpolyline([(x - w / 2, y - h / 2), (x + w / 2, y - h / 2),
                            (x + w / 2, y + h / 2), (x - w / 2, y + h / 2)],
                           close=True, dxfattribs={"layer": layer})
