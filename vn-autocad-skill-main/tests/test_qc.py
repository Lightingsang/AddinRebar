# -*- coding: utf-8 -*-
"""Kiem thu bo soat tren cac tinh huong dung tay, biet truoc ket qua dung.

Moi test ung voi mot loi that da gap khi lam ho so mau.
"""

from ezdxf.enums import TextEntityAlignment as TA

from conftest import can_phong_times
from vncad import document, qc, standards as S


def _ban_ve():
    doc = document.new_drawing("all", "1/100")
    return doc, doc.modelspace()


def _chu(msp, noi_dung, pt, cao=250.0, xoay=0.0):
    msp.add_text(noi_dung, dxfattribs={"layer": "TEXT", "style": "TCVN", "height": cao,
                                       "rotation": xoay}).set_placement(
        pt, align=TA.MIDDLE_CENTER)


@can_phong_times
def test_do_be_rong_chu_bang_phong_that():
    # he so uoc cu 0,60 cho 22,5mm; do tren phong that la 33,5mm
    assert abs(S.rong_chu("MAT BANG TANG 1", 2.5) - 33.53) < 0.5


def test_net_cat_qua_chu_bi_bat():
    doc, msp = _ban_ve()
    _chu(msp, "PHONG KHACH", (0, 0))
    msp.add_line((0, -2000), (0, 2000), dxfattribs={"layer": "A-WALL"})
    assert len(qc.check_text_on_lines(msp)) == 1


def test_net_nam_ben_canh_chu_khong_bi_bat():
    doc, msp = _ban_ve()
    _chu(msp, "PHONG KHACH", (0, 0))
    msp.add_line((-3000, 600), (3000, 600), dxfattribs={"layer": "A-WALL"})
    assert qc.check_text_on_lines(msp) == []


def test_chu_xoay_90_do_tinh_dung_o_bao():
    # Nhan dam dung xoay 90 do: o bao cao va hep. Net doc cach tam 400 KHONG cat
    # chu, du no se cat neu tinh o bao nam ngang (loi cu gay 11 bao dong gia).
    doc, msp = _ban_ve()
    _chu(msp, "D1 200x400", (0, 0), xoay=90.0)
    msp.add_line((400, -3000), (400, 3000), dxfattribs={"layer": "S-BEAM"})
    assert qc.check_text_on_lines(msp) == []


def test_chu_de_len_chu_bi_bat():
    doc, msp = _ban_ve()
    _chu(msp, "BAN CONG", (0, 0))
    _chu(msp, "BAN CONG", (150, 50))
    assert len(qc.check_text_overlap(msp)) == 1


def test_net_tren_lop_huong_dan_duoc_bo_qua():
    # khung o luoi ve tren MODEL-GUIDE, khong in, khong duoc tinh la net ve
    doc, msp = _ban_ve()
    _chu(msp, "PHONG KHACH", (0, 0))
    msp.add_line((0, -2000), (0, 2000), dxfattribs={"layer": "MODEL-GUIDE"})
    assert qc.check_text_on_lines(msp) == []
