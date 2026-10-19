# -*- coding: utf-8 -*-
"""Dung TRON BO ho so vao MOT file DXF nhieu Layout, dung cach ho so that to chuc.

Chay:  python -m examples.nha_pho_5x18m.build_ho_so
Them --dwg de in ra doan LISP mo file trong AutoCAD va luu thanh DWG.
"""

import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from vncad import acad, document, qc, render

from . import build_dutoan
from .model import build_model
from .rebar_quantities import thep_than_nha
from .regions import CELL_H, CELL_W, O_LUOI
from .sheets import build_sheets

OUT = "examples/out"
TEN_FILE = "NhaPho_5x18m_3Tang"


def build(out_dir=OUT):
    """Tao 1 file DXF chua ca bo, xuat anh tung to. Tra ve dict duong dan."""
    os.makedirs(out_dir, exist_ok=True)
    doc = document.new_drawing("all", "1/100")
    msp = doc.modelspace()

    tam_vung = build_model(msp, doc)
    ten_cac_to, tong_thep = build_sheets(doc, tam_vung)

    dxf_path = os.path.join(out_dir, TEN_FILE + ".dxf")
    ket_qua = render.save_sheet_set(doc, dxf_path, out_dir, ten_cac_to)
    ket_qua["xlsx"] = build_dutoan.build(out_dir)
    ket_qua["so_doi_tuong"] = len(msp)
    ket_qua["tong_thep_kg"] = round(tong_thep, 1)
    ket_qua["canh_bao"] = qc.summary(doc, ten_cac_to, cells=O_LUOI,
                                     cell_w=CELL_W, cell_h=CELL_H)
    return ket_qua


def lisp_mo_trong_autocad(out_dir=OUT):
    """Doan LISP mo file ho so trong AutoCAD va luu thanh DWG."""
    dwg_dir = os.path.join(out_dir, "dwg")
    os.makedirs(dwg_dir, exist_ok=True)
    return acad.lisp_open_and_convert(
        os.path.join(out_dir, TEN_FILE + ".dxf"),
        os.path.join(dwg_dir, TEN_FILE + ".dwg"))


if __name__ == "__main__":
    kq = build()
    print("\n=== HO SO TRONG MOT FILE ===")
    print("  So doi tuong trong model space:", kq.pop("so_doi_tuong"))
    print("  Thep tren cac bang thong ke (mong + 1 tang + 1 doan thang):",
          kq.pop("tong_thep_kg"), "kg")
    nho, lon = thep_than_nha()
    print("  Thep khung toan nha dua vao du toan: %.1f kg (D<=10: %.1f, D>10: %.1f)"
          % (nho + lon, nho, lon))
    cb = kq.pop("canh_bao")
    print("  Soat bo cuc:", "SACH" if not cb else "%d canh bao" % len(cb))
    for c in cb:
        print("     -", c)
    for ten, path in kq.items():
        print("  %-8s %s" % (ten, os.path.abspath(path)))
    if "--dwg" in sys.argv:
        print("\n=== LISP dua cho AutoCAD MCP ===")
        print(lisp_mo_trong_autocad())
