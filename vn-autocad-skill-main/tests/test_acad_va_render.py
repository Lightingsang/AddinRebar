# -*- coding: utf-8 -*-
"""Kiem thu doan LISP gui AutoCAD va duong render anh."""

import re

from vncad import acad, render


def test_lisp_chuyen_dwg_duong_dan_chi_dung_gach_xuoi():
    lisp = acad.lisp_open_and_convert("examples/out/a.dxf", "examples/out/dwg/a.dwg")
    duong_dan = re.findall(r'"([^"]+\.(?:dwg|dxf))"', lisp)
    assert duong_dan and all("\\" not in p for p in duong_dan)


def test_lisp_chuyen_dwg_nha_khoa_file_dich():
    lisp = acad.lisp_open_and_convert("examples/out/a.dxf", "examples/out/dwg/a.dwg")
    # AutoCAD tra ten file bang gach nguoc: phai doi ve cung kieu roi moi so
    assert "vl-string-translate" in lisp
    # chinh tai lieu dang chay LISP la file dich: luu no sang file giu cho
    assert '"BUSY"' in lisp and "vla-SaveAs hien" in lisp


def test_render_mot_to_ra_anh(ho_so, tmp_path):
    doc, _, _ = ho_so
    anh = tmp_path / "KT-01.png"
    render.render_sheet(doc, "KT-01", str(anh), "plot", 40, "A3")
    assert anh.exists() and anh.stat().st_size > 5000
