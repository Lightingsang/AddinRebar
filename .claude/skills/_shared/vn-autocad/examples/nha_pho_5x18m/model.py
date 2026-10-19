# -*- coding: utf-8 -*-
"""Ve TOAN BO hinh cua cong trinh vao model space o kich thuoc that 1:1.

Moi cum hinh dat tai mot vung rieng, kem kich thuoc, bong bong truc va tieu de hinh.
Cac to giay (Layout) chi mo cua so nhin vao vung tuong ung, nen sua hinh o day
thi moi to tu cap nhat.
"""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, place

from . import layout as L
from .draw_elevation import draw_front_elevation, draw_side_elevation
from .draw_finishes import draw_ceiling_plan, draw_tiling_plan
from .draw_floor import dims_and_bubbles, draw_floor
from .draw_roof import draw_roof_plan
from .draw_section import draw_room_tags, draw_section_aa
from .draw_site_plan import draw_site_plan
from .draw_stair_structure import (draw_stair_struct_plan,
                                   draw_stair_struct_section)
from .draw_schematic import so_do_nuoc, so_do_tu_dien
from .draw_section_bb import annotations as bb_annotations, draw_section_bb
from .draw_waterproof import chi_tiet_ban_cong, chi_tiet_seno, chi_tiet_wc
from .draw_stair import draw_stair_plan, draw_stair_section, draw_step_detail
from .draw_stair_railing import draw_railing_detail
from .draw_struct_details import draw_column_splice, draw_slab_rebar
from .draw_structure import beam_slab_plan, footing_detail_m1, foundation_plan
from .foundation_rebar import draw_tie_beam_section
from .draw_electrical import draw_electrical
from .draw_plumbing import draw_plumbing
from .draw_rebar_detail import (draw_beam_elevation, draw_beam_sections,
                                draw_column_c1, draw_joint_detail)
from .draw_wc import draw_wc_sheet
from .regions import CELL_H, CELL_W, O_LUOI, PITCH, R, TEN_HANG, centers
from .sheet_annotations import (elevation_grid_and_dims, plan_grid,
                                section_grid_and_dims)

SC = "1/100"



def build_model(msp, doc):
    """Ve het moi cum hinh. Tra ve dict tam va bao cua tung vung."""
    _khung_o_luoi(msp)
    _floor(msp, doc, 1, R["tang1"], full=True, bottom=True, top=False,
           title="Mat bang kien truc tang 1 (tret)")
    _floor(msp, doc, 2, R["tang2"], full=False, bottom=False, top=True,
           title="Mat bang kien truc tang 2 (lau 1)")
    _floor(msp, doc, 3, R["tang3"], full=True, bottom=True, top=False,
           title="Mat bang kien truc tang 3 (lau 2)")
    _roof(msp, doc, R["mai"])
    _section(msp, doc, R["mat_cat"])
    _elevation(msp, doc, R["mat_dung"])
    _foundation(msp, doc, R["mong"])
    with place.Placed(msp, *R["chi_tiet_m1"]):
        footing_detail_m1(msp, doc, scale="1/20")
    draw_tie_beam_section(msp, doc, R["mc_giang"])
    _beam_slab(msp, doc, R["dam_san"])
    draw_wc_sheet(msp, doc, R["wc"])
    draw_stair_plan(msp, doc, R["thang_mb"])
    # mat cat dung cao do tuyet doi nen phai doi ca cum xuong vung rieng
    with place.Placed(msp, 0.0, R["thang_mc"][1]):
        draw_stair_section(msp, doc, (R["thang_mc"][0], 0.0))
    draw_step_detail(msp, doc, R["bac_thang"])
    draw_railing_detail(msp, doc, R["lan_can"])
    draw_beam_elevation(msp, doc, R["dam_d1"])
    draw_beam_sections(msp, doc, R["mc_dam"])
    draw_column_c1(msp, doc, R["cot_c1"])
    _mep(msp, doc, R["dien"], draw_electrical, "Mat bang cap dien tang 1")
    _mep(msp, doc, R["nuoc"], draw_plumbing, "Mat bang cap thoat nuoc tang 1")
    _side_elev(msp, doc, R["mat_dung_ben"])
    for key, ham, tang, ten in (("gach_t1", draw_tiling_plan, 1, "Mat bang lat gach tang 1"),
                                ("gach_t2", draw_tiling_plan, 2, "Mat bang lat gach tang 2"),
                                ("tran_t1", draw_ceiling_plan, 1, "Mat bang tran tang 1"),
                                ("tran_t2", draw_ceiling_plan, 2, "Mat bang tran tang 2")):
        _theo_tang(msp, doc, R[key], ham, tang, ten)
    for key, tang, ham in (("dien_t2", 2, draw_electrical), ("dien_t3", 3, draw_electrical),
                           ("nuoc_t2", 2, draw_plumbing), ("nuoc_t3", 3, draw_plumbing)):
        ox, oy = R[key]
        ten = ham(msp, doc, R[key], floor=tang)
        line = plan_grid(msp, doc, ox, oy)
        an.view_title(msp, (ox + L.L / 2, line - 2600), ten, "1/100", SC)
    _section_bb(msp, doc, R["mat_cat_bb"])
    draw_wc_sheet(msp, doc, R["wc1"], phong="WC 1")
    draw_wc_sheet(msp, doc, R["wc3"], phong="WC 3")
    draw_joint_detail(msp, doc, R["nut_khung"])
    draw_slab_rebar(msp, doc, R["thep_san"])
    draw_column_splice(msp, doc, R["noi_cot"])
    _theo_tang(msp, doc, R["gach_t3"], draw_tiling_plan, 3, "Mat bang lat gach tang 3")
    _theo_tang(msp, doc, R["tran_t3"], draw_ceiling_plan, 3, "Mat bang tran tang 3")
    chi_tiet_wc(msp, doc, R["ct_wc"])
    chi_tiet_seno(msp, doc, R["ct_seno"])
    chi_tiet_ban_cong(msp, doc, R["ct_ban_cong"])
    draw_site_plan(msp, doc, R["tong_mb"])
    draw_stair_struct_plan(msp, doc, R["thang_kc_mb"])
    draw_stair_struct_section(msp, doc, R["thang_kc_mc"])
    so_do_tu_dien(msp, doc, R["sd_dien"])
    so_do_nuoc(msp, doc, R["sd_nuoc"])
    return centers()


def _section_bb(msp, doc, org):
    """Mat cat B-B dung cao do tuyet doi nen phai doi ca cum ve vung rieng."""
    ox, oy = org
    with place.Placed(msp, ox, oy):
        draw_section_bb(msp, doc)
    bb_annotations(msp, doc, ox, oy)


def _khung_o_luoi(msp):
    """Khung vung va nhan cho tung o luoi trong model space.

    Chi de nguoi mo ban ve trong AutoCAD nhin ra bo cuc. Layer MODEL-GUIDE
    khong in va da dong bang trong moi cua so nhin nen khong len to giay.
    """
    lay = {"layer": "MODEL-GUIDE"}
    for col, row, so_hieu, ten in O_LUOI:
        x0, y0 = col * CELL_W, -row * CELL_H
        x1, y1 = x0 + CELL_W, y0 + CELL_H
        msp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)],
                           close=True, dxfattribs=lay)
        msp.add_text("%s  %s" % (so_hieu, ten), dxfattribs={
            "layer": "MODEL-GUIDE", "style": "TCVN", "height": 700
        }).set_placement((x0 + 500, y1 - 1200), align=TA.TOP_LEFT)
    for row, ten in enumerate(TEN_HANG):
        msp.add_text(ten, dxfattribs={
            "layer": "MODEL-GUIDE", "style": "TCVN", "height": 1600
        }).set_placement((-2500, -row * CELL_H + CELL_H / 2), align=TA.MIDDLE_RIGHT)


def _theo_tang(msp, doc, org, ham_ve, tang, ten):
    """Ve mot mat bang chuyen de (lat gach, tran) kem kich thuoc va tieu de."""
    ox, oy = org
    ham_ve(msp, doc, tang, org)
    line = plan_grid(msp, doc, ox, oy)
    an.view_title(msp, (ox + L.L / 2, line - 2600), ten, "1/100", SC)


def _side_elev(msp, doc, org):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        draw_side_elevation(msp, doc)
    section_grid_and_dims(msp, doc, ox, oy, title="Mat dung ben truc 1-5")


def _mep(msp, doc, org, ham_ve, title):
    """Ve mot mat bang co dien, tieu de dat duoi hang bong bong truc."""
    ox, oy = org
    ten = ham_ve(msp, doc, org) or title
    line = plan_grid(msp, doc, ox, oy)
    an.view_title(msp, (ox + L.L / 2, line - 2600), ten, "1/100", SC)


def _floor(msp, doc, so_tang, org, full, bottom, top, title):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        draw_floor(msp, doc, so_tang, (0.0, 0.0))
    line = dims_and_bubbles(msp, doc, oy, full=full, bottom_bubbles=bottom,
                            top_bubbles=top, ox=ox)
    an.view_title(msp, (ox + L.L / 2, line - 2600), title, "1/100", SC)


def _roof(msp, doc, org):
    ox, oy = org
    draw_roof_plan(msp, doc, org)
    line = dims_and_bubbles(msp, doc, oy, full=False, bottom_bubbles=False,
                            top_bubbles=True, ox=ox)
    an.view_title(msp, (ox + L.L / 2, line - 2600), "Mat bang mai", "1/100", SC)


def _section(msp, doc, org):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        draw_section_aa(msp, doc)
        draw_room_tags(msp)
    section_grid_and_dims(msp, doc, ox, oy, title="Mat cat A-A")


def _elevation(msp, doc, org):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        draw_front_elevation(msp, doc)
    elevation_grid_and_dims(msp, doc, ox, oy, title="Mat dung chinh")


def _foundation(msp, doc, org):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        foundation_plan(msp, doc)
    plan_grid(msp, doc, ox, oy, title="Mat bang dinh vi coc ep va dai mong")


def _beam_slab(msp, doc, org):
    ox, oy = org
    with place.Placed(msp, ox, oy):
        beam_slab_plan(msp, doc, floor=2)
    plan_grid(msp, doc, ox, oy,
              title="Mat bang ket cau dam san tang 2 (cot +3.800)")
