# -*- coding: utf-8 -*-
"""Nap mot du an ban ve theo quy uoc chung, de cong cu va test dung cho moi du an.

Mot du an la mot goi Python (vi du `examples.nha_pho_5x18m`) co ba module:

    model.build_model(msp, doc)   -> dict tam cac vung hinh
    sheets.build_sheets(doc, tam) -> (danh sach ten to, tong thep tren bang)
    regions.CELL_W, regions.CELL_H, regions.O_LUOI

Du an moi chi can theo dung ba ten nay la dung duoc ngay bo cong cu trong `tools/`.
"""

import importlib
from collections import namedtuple

DU_AN_MAU = "examples.nha_pho_5x18m"

DuAn = namedtuple("DuAn", "ten build_model build_sheets cell_w cell_h o_luoi")


def load(goi=DU_AN_MAU):
    """Nap ba module cua du an, tra ve mot DuAn."""
    model = importlib.import_module(goi + ".model")
    sheets = importlib.import_module(goi + ".sheets")
    regions = importlib.import_module(goi + ".regions")
    return DuAn(goi, model.build_model, sheets.build_sheets,
                regions.CELL_W, regions.CELL_H, regions.O_LUOI)


def build(goi=DU_AN_MAU):
    """Dung model va cac to trong bo nho, KHONG render anh.

    Tra ve (doc, ten_cac_to, du_an). Chay trong vai giay, dung cho soat va test.
    """
    from . import document
    da = load(goi)
    doc = document.new_drawing("all", "1/100")
    ten_cac_to, _ = da.build_sheets(doc, da.build_model(doc.modelspace(), doc))
    return doc, ten_cac_to, da


def qc_all(doc, ten_cac_to, da, gioi_han=9999):
    """Chay du bon phep soat, khong cat bot so canh bao. Rong tuc la sach."""
    from . import qc, sheet
    msp = doc.modelspace()
    canh_bao = sheet.check_views(doc, ten_cac_to, "A3")
    canh_bao += qc.check_text_overlap(msp, gioi_han=gioi_han)
    canh_bao += qc.check_text_on_lines(msp, gioi_han=gioi_han)
    canh_bao += qc.check_cell_bleed(msp, da.o_luoi, da.cell_w, da.cell_h,
                                    gioi_han=gioi_han)
    return canh_bao
