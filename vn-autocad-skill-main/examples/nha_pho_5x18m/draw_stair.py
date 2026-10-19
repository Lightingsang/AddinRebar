# -*- coding: utf-8 -*-
"""Khai trien cau thang: mat bang thang 1/50, mat cat thang 1/50, chi tiet bac 1/20."""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, arch, blocks, section as sec, standards as S

SC50, SC20 = "1/50", "1/20"

# Thang 2 ve, chieu cao tang 3650, 21 bac roi vao cung Sinh (4n+1)
SO_BAC = 21
CO_BAC = 3650.0 / SO_BAC        # 173.8mm
MAT_BAC = 270.0
VE_RONG = 1000.0                # be rong mot ve thang
CHIEU_NGHI = 1100.0             # chieu sau chieu nghi
BAC_VE_1 = 11


def draw_stair_plan(msp, doc, origin=(0.0, 0.0)):
    """Mat bang cau thang 2 ve co chieu nghi, danh so bac va mui ten huong di."""
    ox, oy = origin
    _walls(msp, ox, oy)
    dai_ve = MAT_BAC * (BAC_VE_1 - 1)
    # ve 1 di len theo chieu X duong, sat canh duoi
    arch.stair_flight(msp, (ox + 200, oy + 400), (1, 0), VE_RONG, MAT_BAC,
                      BAC_VE_1 - 1, numbering=True, scale=SC50)
    # ve 2 di nguoc lai, sat canh tren
    x2 = ox + 200 + dai_ve + CHIEU_NGHI
    arch.stair_flight(msp, (x2, oy + 400 + VE_RONG + 200), (-1, 0), VE_RONG,
                      MAT_BAC, SO_BAC - BAC_VE_1, numbering=True, scale=SC50,
                      start=BAC_VE_1 + 1)
    blocks.stair_arrow(msp, (ox + 400, oy + 900), (ox + 200 + dai_ve, oy + 900))
    an.leader_note(msp, "UP 21 bac", (ox + 200 + dai_ve, oy + 900),
                   (ox + 200 + dai_ve + 1400, oy + 300), SC50)
    an.dim_chain(msp, doc, [ox + 200, ox + 200 + dai_ve,
                            ox + 200 + dai_ve + CHIEU_NGHI], oy, "h", SC50, side=-1)
    an.view_title(msp, (ox + 2200, oy - 1500), "Mat bang cau thang", "1/50", SC50)


def _walls(msp, ox, oy):
    """Long thang: 2 tuong bao doc va tuong dau chieu nghi."""
    dai = 200 + MAT_BAC * (BAC_VE_1 - 1) + CHIEU_NGHI + 300
    rong = 400 + VE_RONG * 2 + 200
    for p1, p2 in (((0, 0), (dai, 0)), ((dai, 0), (dai, rong)),
                   ((dai, rong), (0, rong)), ((0, rong), (0, 0))):
        arch.wall(msp, (ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]), t=110)


def draw_stair_section(msp, doc, origin=(0.0, 0.0)):
    """Mat cat doc cau thang tang 1 len tang 2, thay ban thang va chieu nghi."""
    ox, oy = origin
    z0, z1 = 0.150, 3.800
    z_nghi = z0 + CO_BAC * (BAC_VE_1 - 1) / 1000.0
    sec.stair(msp, ox, z0, z_nghi, BAC_VE_1 - 1, MAT_BAC, "right")
    x_nghi = ox + MAT_BAC * (BAC_VE_1 - 1)
    sec.slab(msp, x_nghi, x_nghi + CHIEU_NGHI, z_nghi, thk=120)
    sec.stair(msp, x_nghi + CHIEU_NGHI, z_nghi, z1, SO_BAC - BAC_VE_1, MAT_BAC, "right")
    for z in (z0, z1):
        sec.slab(msp, ox - 1200, ox - 200, z, thk=100)
    an.level_mark(msp, doc, (ox - 700, z0 * 1000.0), z0, SC50, plan=False)
    an.level_mark(msp, doc, (x_nghi + 500, z_nghi * 1000.0), round(z_nghi, 3), SC50,
                  plan=False)
    an.dim_chain(msp, doc, [z0 * 1000.0, z_nghi * 1000.0, z1 * 1000.0],
                 ox - 1200, "v", SC50, side=-1)
    an.view_title(msp, (ox + 2400, (z0 - 0.9) * 1000.0), "Mat cat cau thang",
                  "1/50", SC50)


def draw_step_detail(msp, doc, origin=(0.0, 0.0)):
    """Chi tiet cau tao bac thang: ban BTCT, da granite mat bac va co bac."""
    ox, oy = origin
    b, h = MAT_BAC, CO_BAC
    for i in range(3):
        x, y = ox + b * i, oy + h * i
        msp.add_lwpolyline([(x, y), (x + b, y), (x + b, y + h)],
                           dxfattribs={"layer": "A-ELEV", "lineweight": 50})
        msp.add_lwpolyline([(x, y + 30), (x + b + 20, y + 30), (x + b + 20, y + h)],
                           dxfattribs={"layer": "A-MAT"})
    msp.add_lwpolyline([(ox, oy - 120), (ox + b * 3, oy + h * 3 - 120)],
                       dxfattribs={"layer": "A-ELEV", "lineweight": 40})
    for text, target, tp in (
            ("Da Granite mat bac day 30", (ox + b * 1.5, oy + h + 30),
             (ox + b * 3.4, oy + h * 2.2)),
            ("Da Granite co bac day 20", (ox + b * 2 + 10, oy + h * 1.5),
             (ox + b * 3.4, oy + h * 1.3)),
            ("Ban thang BTCT day 120", (ox + b * 1.2, oy + h * 1.2 - 120),
             (ox - 1400, oy + h * 0.6))):
        an.leader_note(msp, text, target, tp, SC20)
    an.dim_chain(msp, doc, [oy, oy + h], ox, "v", SC20, side=-1)
    an.dim_chain(msp, doc, [ox, ox + b], oy, "h", SC20, side=-1)
    an.view_title(msp, (ox + b * 1.5, oy - 700), "Chi tiet bac thang", "1/20", SC20)


NOTES_THANG = [
    "Thang 21 bac roi vao cung Sinh; co bac 174mm, mat bac 270mm, thoa 2h+b=618mm.",
    "Mat bac op da Granite tu nhien day 30mm, co bac day 20mm, mui bac bo tron R10.",
    "Gan chi dong chong truot 2 duong cach mui bac 30mm va 60mm.",
    "Lan can kinh cuong luc 12mm, pat inox 304 bat hong ban thang, tay vin go cam xe.",
    "Chieu cao tay vin 900mm tinh tu mui bac; khe ho giua kinh va bac khong qua 100mm.",
    "Ban thang BTCT day 120mm, thep chiu luc D10a150 hai lop, be tong B20 (M250).",
]
