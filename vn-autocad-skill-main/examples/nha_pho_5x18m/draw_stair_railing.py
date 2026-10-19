# -*- coding: utf-8 -*-
"""Chi tiet lan can cau thang, ty le 1/20.

Cat ngang qua mot bac thang de thay ro cach bat pat inox vao hong ban thang,
do day kinh, va chieu cao tay vin do tu mui bac len.
"""

from vncad import annotate as an

SC = "1/10"                # nut nho va cao, ve 1/10 moi doc ro do day kinh
R_SO = 3.5 * 10
MAT_BAC, CO_BAC = 270.0, 173.8
T_BAN = 120.0            # ban thang BTCT
T_KINH = 12.0            # kinh cuong luc
H_TAY_VIN = 900.0        # tu mui bac len mep tren tay vin
H_PAT = 150.0            # chan pat inox bat hong ban thang


def _bao(msp, pts, layer="A-ELEV", lw=25, kin=True):
    msp.add_lwpolyline(pts, close=kin, dxfattribs={"layer": layer, "lineweight": lw})


def _bac_va_ban(msp, ox, oy):
    """Mot bac thang va ban BTCT nghieng ben duoi, lam nen cho chi tiet.

    Chi ve mot bac: o 1/10 hai bac da chiem het be ngang o giay danh cho nut nay,
    ma nut can doc la cho bat pat chu khong phai hinh dang bac.
    """
    _bao(msp, [(ox, oy), (ox + MAT_BAC, oy), (ox + MAT_BAC, oy + CO_BAC)],
         lw=50, kin=False)
    _bao(msp, [(ox, oy + 30), (ox + MAT_BAC + 20, oy + 30),
               (ox + MAT_BAC + 20, oy + CO_BAC)], layer="A-MAT", lw=13, kin=False)
    _bao(msp, [(ox - 120, oy - T_BAN - 77), (ox + MAT_BAC + 120, oy - T_BAN + 77)],
         lw=40, kin=False)


def _pat_va_kinh(msp, ox, oy):
    """Pat inox bat hong ban thang, kinh cuong luc va tay vin go."""
    xp = ox + MAT_BAC * 0.5
    yp = oy + 30
    # than pat: hop inox 60x150 om lay mep kinh
    _bao(msp, [(xp - 30, yp), (xp + 30, yp), (xp + 30, yp + H_PAT), (xp - 30, yp + H_PAT)],
         layer="A-RAILING", lw=35)
    for dy in (40.0, 105.0):                 # hai bu long no bat vao ban thang
        msp.add_line((xp - 30, yp + dy), (xp - 130, yp + dy),
                     dxfattribs={"layer": "A-RAILING", "lineweight": 35})
    y_kinh = yp + H_PAT - 60
    y_dinh = oy + H_TAY_VIN - 60
    _bao(msp, [(xp - T_KINH / 2, y_kinh), (xp + T_KINH / 2, y_kinh),
               (xp + T_KINH / 2, y_dinh), (xp - T_KINH / 2, y_dinh)],
         layer="A-RAILING", lw=35)
    # tay vin go 60x50 chup len dinh kinh
    _bao(msp, [(xp - 30, y_dinh), (xp + 30, y_dinh),
               (xp + 30, y_dinh + 50), (xp - 30, y_dinh + 50)],
         layer="A-RAILING", lw=40)
    return xp, y_kinh, y_dinh


CHU_GIAI = [
    ("1", "Ban thang BTCT day 120, thep D10a150 hai lop"),
    ("2", "Pat inox 304 hop 60x150, hai bu long no M10 bat hong ban"),
    ("3", "Kinh cuong luc 12mm, mep mai canh, khe ho duoi khong qua 100"),
    ("4", "Tay vin go cam xe 60x50, bo tron canh, danh vec ni ba lop"),
    ("5", "Da Granite mat bac day 30 va co bac day 20, mui bac bo tron R10"),
]


def draw_railing_detail(msp, doc, origin=(0.0, 0.0)):
    """Chi tiet lan can kinh tren ban thang, kem duong kich thuoc chieu cao."""
    ox, oy = origin
    _bac_va_ban(msp, ox, oy)
    xp, y_kinh, y_dinh = _pat_va_kinh(msp, ox, oy)
    for n, sx, sy, tx, ty in (
            (1, -300, -180, ox - 60, oy - T_BAN - 30),
            (2, -300, 200, xp - 30, oy + 130),
            (3, -300, 560, xp - T_KINH / 2, oy + 560),
            (4, -300, H_TAY_VIN - 20, xp - 30, oy + H_TAY_VIN - 35),
            (5, -300, 60, ox + 60, oy + 45)):
        an.so_lop(msp, n, (ox + sx, oy + sy), (tx, ty), R_SO, SC)
    an.dim_chain(msp, doc, [oy, oy + H_TAY_VIN], ox + MAT_BAC + 200, "v", SC, side=1)
    an.view_title(msp, (ox + MAT_BAC / 2, oy - 620), "CT4 - Lan can cau thang",
                  "1/10", SC)
