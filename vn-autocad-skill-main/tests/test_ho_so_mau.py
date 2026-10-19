# -*- coding: utf-8 -*-
"""Kiem thu tren ho so mau nha pho 5x18m: du to, bo cuc sach, dung quy uoc."""

from conftest import can_phong_times
from vncad import project, qc, sheet


def test_du_26_to_moi_to_mot_layout(ho_so):
    doc, ten_cac_to, _ = ho_so
    assert len(ten_cac_to) == 26
    assert [t for t in ten_cac_to if t.startswith("KT")] == ["KT-%02d" % i for i in range(1, 16)]
    assert [t for t in ten_cac_to if t.startswith("KC")] == ["KC-%02d" % i for i in range(1, 6)]
    assert [t for t in ten_cac_to if t.startswith("ME")] == ["ME-%02d" % i for i in range(1, 7)]
    for ten in ten_cac_to:
        assert doc.layouts.get(ten) is not None


def test_da_bo_layout_mac_dinh(ho_so):
    doc, _, _ = ho_so
    assert "Layout1" not in doc.layouts.names()


def test_cua_so_nhin_khong_chong_khong_lan_khung_ten(ho_so):
    doc, ten_cac_to, _ = ho_so
    assert sheet.check_views(doc, ten_cac_to, "A3") == []


def test_moi_cum_hinh_nam_gon_trong_o_luoi(ho_so):
    doc, _, da = ho_so
    assert qc.check_cell_bleed(doc.modelspace(), da.o_luoi, da.cell_w, da.cell_h) == []


def test_lop_huong_dan_khong_in(ho_so):
    doc, _, _ = ho_so
    assert doc.layers.get("MODEL-GUIDE").dxf.plot == 0


@can_phong_times
def test_bon_phep_soat_deu_sach(ho_so):
    doc, ten_cac_to, da = ho_so
    assert project.qc_all(doc, ten_cac_to, da) == []
