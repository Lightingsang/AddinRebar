# -*- coding: utf-8 -*-
"""Soat chat luong ban ve tu dong, thay cho viec mo tung anh ra nhin.

Bon phep soat:
  check_views        : cua so nhin tren giay co de len nhau khong (module sheet)
  check_text_overlap : chu de len chu           (module qc_text)
  check_text_on_lines: chu bi net ve cat qua    (module qc_geom)
  check_cell_bleed   : hinh tran tu o luoi nay sang o luoi khac
  summary            : goi ca bon, tra ve mot danh sach canh bao
"""

from .qc_geom import check_text_on_lines
from .qc_text import LAYERS, NGUONG, check_text_overlap, o_bao

__all__ = ["LAYERS", "NGUONG", "check_text_overlap", "check_text_on_lines",
           "check_cell_bleed", "o_bao", "summary"]


def check_cell_bleed(msp, cells, cell_w, cell_h, bo_qua=("MODEL-GUIDE",),
                     gioi_han=20):
    """Tim cac doi tuong tran tu o luoi nay sang o luoi khac.

    cells: [(col, row, so_hieu, ten), ...] danh sach o dang dung.
    Moi doi tuong duoc quy ve o chua TAM cua no, roi kiem xem o bao co nam
    tron trong o do khong. Day la phep soat truc tiep cho loi "cac phan lan
    vao nhau" khi mo ban ve trong AutoCAD.
    """
    from ezdxf import bbox
    dung = {(c, r): (s or t) for c, r, s, t in cells}
    ket_qua = []
    for e in msp:
        if e.dxf.get("layer", "") in bo_qua:
            continue
        try:
            b = bbox.extents([e], fast=True)
        except Exception:
            continue
        if not b.has_data:
            continue
        cx, cy = (b.extmin.x + b.extmax.x) / 2, (b.extmin.y + b.extmax.y) / 2
        # o hang r chiem dai y tu -r*H den -r*H + H, nen hang 0 nam o y duong
        col = int(cx // cell_w)
        row = 0 if cy >= 0 else int(-cy // cell_h) + 1
        ten = dung.get((col, row))
        if ten is None:
            continue
        x0, y0 = col * cell_w, -row * cell_h
        if (b.extmin.x < x0 - 1 or b.extmax.x > x0 + cell_w + 1
                or b.extmin.y < y0 - 1 or b.extmax.y > y0 + cell_h + 1):
            ket_qua.append("%s: %s tai (%.0f, %.0f) tran ra ngoai o luoi"
                           % (ten, e.dxftype(), b.extmin.x, b.extmin.y))
            if len(ket_qua) >= gioi_han:
                break
    return ket_qua


def summary(doc, layout_names, sheet="A3", cells=None, cell_w=0.0, cell_h=0.0):
    """Soat toan bo. Tra ve danh sach canh bao; rong tuc la ban ve sach."""
    from . import sheet as sh
    msp = doc.modelspace()
    canh_bao = sh.check_views(doc, layout_names, sheet)
    canh_bao += check_text_overlap(msp)
    canh_bao += check_text_on_lines(msp)
    if cells:
        canh_bao += check_cell_bleed(msp, cells, cell_w, cell_h)
    return canh_bao
