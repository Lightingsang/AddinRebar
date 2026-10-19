# -*- coding: utf-8 -*-
"""Quan ly to ban ve theo dung cach ho so that: 1 file, nhieu Layout.

Model space chua toan bo hinh ve o kich thuoc THAT 1:1.
Moi Layout la mot to giay, ve khung ten o kich thuoc that tren giay (mm),
va mo mot hoac nhieu cua so (viewport) nhin vao vung hinh tuong ung.

Nho vay: sua hinh trong model thi moi to tu cap nhat; mot to co the ghep
nhieu ty le khac nhau chi bang cach dat ty le rieng cho tung cua so.
"""

from . import standards as S, titleblock as tb

PAPER = "1/1"       # trong Layout moi thu ve theo mm that tren giay


def new_sheet(doc, name, info=None, sheet="A3"):
    """Tao mot to ban ve moi. Tra ve doi tuong Layout de ve tiep len giay."""
    if name in [lay.name for lay in doc.layouts]:
        doc.layouts.delete(name)
    lay = doc.layouts.new(name)
    pw, ph = S.SHEETS[sheet]
    lay.page_setup(size=(pw, ph),
                   margins=(S.MARGIN_EDGE, S.MARGIN_EDGE, S.MARGIN_EDGE, S.MARGIN_BIND),
                   units="mm")
    tb.draw_frame(lay, sheet, PAPER, origin=(0, 0))
    if info:
        tb.draw_title_block(lay, info, sheet, PAPER, origin=(0, 0))
    return lay


def drop_default_layouts(doc):
    """Xoa cac Layout mac dinh cua ezdxf de file chi con dung cac to da dat ten."""
    for name in ("Layout1", "Layout2"):
        if name in [l.name for l in doc.layouts]:
            doc.layouts.delete(name)


LAYER_AN = ("MODEL-GUIDE",)     # layer chi dung trong model, dong bang o moi cua so


def add_view(lay, paper_center, paper_size, model_center, scale="1/100",
             frame=False, an_layer=LAYER_AN):
    """Mo cua so nhin vao model space.

    paper_center, paper_size: vi tri va kich thuoc cua so tren giay (mm)
    model_center: diem trong model space se nam giua cua so
    scale: ty le cua so, vi du '1/100' thi 1mm giay bang 100mm ngoai doi
    frame: ve vien cua so de kiem tra bo cuc, ban giao thi de False
    """
    k = S.SCALES[scale]
    vp = lay.add_viewport(
        center=paper_center, size=paper_size,
        view_center_point=model_center,
        view_height=paper_size[1] * k,
    )
    vp.dxf.status = 1            # cua so dang bat
    vp.dxf.layer = "DEFPOINTS"   # vien cua so nam tren layer khong in
    if an_layer:
        # khung vung va nhan o luoi chi phuc vu doc trong model, khong len giay
        vp.frozen_layers = list(an_layer)
    if frame:
        w, h = paper_size
        cx, cy = paper_center
        lay.add_lwpolyline(
            [(cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2),
             (cx + w / 2, cy + h / 2), (cx - w / 2, cy + h / 2)],
            close=True, dxfattribs={"layer": "DEFPOINTS"})
    return vp


def check_views(doc, layout_names, sheet="A3"):
    """Tu soat bo cuc tung to: cua so nhin chong nhau, hay lan vao khung ten.

    Tra ve danh sach canh bao dang chuoi. Rong thi bo cuc sach.
    Dung truoc khi ban giao de khoi phai mo tung anh ra nhin.
    """
    pw, ph = S.SHEETS[sheet]
    tb_x0 = pw - S.MARGIN_EDGE - tb.TB_W
    tb_y1 = S.MARGIN_EDGE + tb.TB_H
    canh_bao = []
    for ten in layout_names:
        # bo qua VIEWPORT id=1: day la cua so mac dinh cua Layout, khong phai
        # cua so nhin do minh tao
        vps = [v for v in doc.layouts.get(ten).query("VIEWPORT")
               if v.dxf.status > 0 and v.dxf.get("id", 1) != 1]
        boxes = [(v.dxf.center.x - v.dxf.width / 2, v.dxf.center.y - v.dxf.height / 2,
                  v.dxf.center.x + v.dxf.width / 2, v.dxf.center.y + v.dxf.height / 2)
                 for v in vps]
        for i, a in enumerate(boxes):
            if a[0] < S.MARGIN_BIND - 1 or a[1] < S.MARGIN_EDGE - 1                     or a[2] > pw - S.MARGIN_EDGE + 1 or a[3] > ph - S.MARGIN_EDGE + 1:
                canh_bao.append("%s: cua so %d lot ra ngoai khung ban ve" % (ten, i + 1))
            if a[2] > tb_x0 and a[1] < tb_y1:
                canh_bao.append("%s: cua so %d lan vao vung khung ten" % (ten, i + 1))
            for j, b in enumerate(boxes[i + 1:], i + 1):
                if a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]:
                    canh_bao.append("%s: cua so %d va %d chong nhau" % (ten, i + 1, j + 1))
    return canh_bao


def work_area(sheet="A3", title_block=True):
    """Vung giay con trong de dat cua so nhin, tra ve (x0, y0, x1, y1) theo mm.

    Neu to co khung ten thi tru luon dai khung ten o goc duoi phai.
    """
    pw, ph = S.SHEETS[sheet]
    x0, y0 = S.MARGIN_BIND + 2, S.MARGIN_EDGE + 2
    x1, y1 = pw - S.MARGIN_EDGE - 2, ph - S.MARGIN_EDGE - 2
    if title_block:
        y0 = S.MARGIN_EDGE + tb.TB_H + 3
    return (x0, y0, x1, y1)


def full_view(lay, model_center, scale="1/100", sheet="A3", title_block=True,
              frame=False):
    """Mo mot cua so chiem tron vung giay con trong. Dung cho to chi co 1 hinh."""
    x0, y0, x1, y1 = work_area(sheet, title_block)
    return add_view(lay, ((x0 + x1) / 2, (y0 + y1) / 2), (x1 - x0, y1 - y0),
                    model_center, scale, frame)


def side_by_side(lay, left_center, right_center, split=0.62, scale="1/100",
                 sheet="A3", gap=6.0, frame=False):
    """Chia vung giay lam 2 cua so trai phai. Tra ve (viewport_trai, viewport_phai)."""
    x0, y0, x1, y1 = work_area(sheet)
    w = x1 - x0 - gap
    wl, wr = w * split, w * (1 - split)
    h = y1 - y0
    vl = add_view(lay, (x0 + wl / 2, (y0 + y1) / 2), (wl, h), left_center, scale, frame)
    vr = add_view(lay, (x1 - wr / 2, (y0 + y1) / 2), (wr, h), right_center, scale, frame)
    return vl, vr


def note_panel(lay, x, y_top, title, lines, width=95.0, h_title=3.2, h_line=2.2):
    """Khoi ghi chu ve THANG len giay, khong qua cua so nhin."""
    from ezdxf.enums import TextEntityAlignment as TA
    lay.add_text(title.upper(), dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": h_title
    }).set_placement((x, y_top), align=TA.TOP_LEFT)
    lay.add_line((x, y_top - h_title * 1.3), (x + width, y_top - h_title * 1.3),
                 dxfattribs={"layer": "TEXT"})
    cy = y_top - h_title * 2.2
    for i, ln in enumerate(lines, 1):
        lay.add_text("%d. %s" % (i, ln), dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": h_line
        }).set_placement((x, cy), align=TA.TOP_LEFT)
        cy -= h_line * 1.7
    return cy


def view_caption(lay, x, y, title, scale_label, h=3.5):
    """Tieu de hinh chieu ve tren giay, ngay duoi cua so nhin."""
    from ezdxf.enums import TextEntityAlignment as TA
    lay.add_text(title.upper(), dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": h
    }).set_placement((x, y), align=TA.MIDDLE_CENTER)
    w = len(title) * h * 0.62
    for i, dy in enumerate((-h * 0.55, -h * 0.85)):
        lay.add_line((x - w / 2, y + dy), (x + w / 2, y + dy),
                     dxfattribs={"layer": "TEXT", "lineweight": 50 if i == 0 else 13})
    lay.add_text("TY LE : %s" % scale_label, dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": h * 0.6
    }).set_placement((x, y - h * 1.9), align=TA.MIDDLE_CENTER)
