# -*- coding: utf-8 -*-
"""Mat cat B-B: cat NGANG nha theo be rong 5m, nhin ve phia mat tien.

Mat phang cat di qua truc 2 (giua phong ngu master 1), thay 3 san, dam ngang,
tuong hai ben va tuong chan mai.
"""

from vncad import annotate as an, elevation as el, section as sec

from . import layout as L

SC = "1/100"
Y0, Y1 = 0.0, 5000.0            # be rong nha theo truc A den B
T_TUONG = 220.0


def draw_section_bb(msp, doc):
    """Ve tron mat cat B-B tai toa do that (coc +0.000 la goc, truc Y la be ngang)."""
    el.ground_line(msp, Y0, Y1, L.Z_DAT)
    _mong(msp)
    _san_va_dam(msp)
    _tuong_bao(msp)
    _phong(msp, doc)
    el.storey_grid(msp, doc, L.LEVELS, Y0, Y1, SC, side="left", tail=1600)


def _mong(msp):
    """Dai mong va coc duoi hai cot bien, kem da kieng noi hai dai."""
    for y in (Y0, Y1):
        sec.footing(msp, y, L.Z_MONG, w=1400, h=700)
        sec.pile(msp, y, L.Z_MONG, length=1200)
    sec.slab(msp, Y0 + 700, Y1 - 700, L.Z_MONG + 0.700, thk=350, layer="A-WALL")


def _san_va_dam(msp):
    """San BTCT tung tang va dam ngang D1 tai hai dau nhip."""
    for _, z in L.LEVELS[:4]:
        sec.slab(msp, Y0, Y1, z, thk=100)
        for y in (Y0, Y1):
            sec.beam(msp, y, z, w=200, h=400, label="D1", scale=SC)


def _tuong_bao(msp):
    """Tuong hong hai ben chay suot ba tang, tuong chan mai tren cung."""
    top = L.LEVELS[3][1]
    for y in (Y0, Y1):
        sec.wall(msp, y, L.Z_DAT, top, t=T_TUONG)
    el.parapet(msp, Y0 - T_TUONG / 2, Y1 + T_TUONG / 2, top, h=900)
    # cua so hong nha o moi tang, nhin thay phia sau mat cat
    for _, z in L.LEVELS[:3]:
        for y in (Y0, Y1):
            sec.column_back(msp, y, z + 1.05, z + 2.65, w=T_TUONG * 0.9)


def _phong(msp, doc):
    """Ten khong gian va cao do tran tung tang tren mat cat."""
    # khong dat nhan cho san thuong vi da co ten cot tang o hang cao do
    ten_tang = [("GARA O TO", 0.150), ("PHONG NGU MASTER 1", 3.800),
                ("PHONG THO GIA TIEN", 7.200)]
    for ten, z in ten_tang:
        sec.room_tag(msp, ((Y0 + Y1) / 2, (z + 1.6) * 1000.0), ten, SC)
    # ca hai ghi chu deu dat ve ben phai vi ben trai chi con le hep cua o luoi
    an.leader_note(msp, "Tuong hong xay 220 gach tuynel",
                   (Y1, 5.5 * 1000.0), (Y1 + 3200, 6.6 * 1000.0), SC)
    an.leader_note(msp, "Tuong chan mai cao 900",
                   (Y1, 11.0 * 1000.0), (Y1 + 3200, 12.0 * 1000.0), SC)


def annotations(msp, doc, ox, oy):
    """Bong bong truc chu A B o duoi, kich thuoc chieu cao tang o ben phai."""
    y_base = oy - 2.35 * 1000.0
    xs = [ox + y for _, y in L.AXIS_Y]
    line = an.dim_tiers(msp, doc, [xs, [xs[0], xs[-1]]], y_base, "h", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, ox + y) for n, y in L.AXIS_Y], line, "h", SC,
                    base_coord=y_base)
    zs = [oy + L.Z_DAT * 1000.0] + [oy + z * 1000.0 for _, z in L.LEVELS]
    an.dim_tiers(msp, doc, [zs, [zs[0], zs[-1]]], ox + L.W, "v", SC, side=+1)
    an.view_title(msp, (ox + L.W / 2, line - 2600), "Mat cat B-B", "1/100", SC)
    return line
