# -*- coding: utf-8 -*-
"""Khung ban ve + khung ten hanh chinh Viet Nam.

Bo truong thong tin lay dung theo khung ten trong 34 bo ho so thuc te:
DON VI THIET KE / CHU DAU TU / TEN CONG TRINH / VI TRI / HANG MUC /
TEN BAN VE / TY LE / SO HIEU BAN VE / NGAY HOAN THANH / luoi chu ky 4 vai tro.
"""

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

LAY = "KHUNG-TEN"
TB_W = 160.0     # rong khung ten tren giay (mm)
TB_H = 62.0      # cao khung ten tren giay (mm)


def draw_frame(msp, sheet="A3", scale="1/100", origin=(0, 0)):
    """Ve khung vien ban ve. Tra ve (x0, y0, x1, y1) vung ve kha dung, don vi model."""
    k = S.SCALES[scale]
    pw, ph = S.SHEETS[sheet]
    ox, oy = origin
    # mep giay
    _poly(msp, [(0, 0), (pw, 0), (pw, ph), (0, ph)], k, ox, oy, lw=13)
    # khung trong: le trai 20mm dong gay, 3 le con lai 10mm
    x0, y0 = S.MARGIN_BIND, S.MARGIN_EDGE
    x1, y1 = pw - S.MARGIN_EDGE, ph - S.MARGIN_EDGE
    _poly(msp, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)], k, ox, oy, lw=60)
    return (ox + x0 * k, oy + y0 * k, ox + x1 * k, oy + y1 * k)


def draw_title_block(msp, info, sheet="A3", scale="1/100", origin=(0, 0)):
    """Ve khung ten o goc duoi phai. info la dict cac truong.

    Truong dung: cong_ty, dia_chi, lien_he, chu_dau_tu, cong_trinh, vi_tri,
    hang_muc, ten_ban_ve, giai_doan, ty_le, so_hieu, ngay, chu_ky (dict vai tro -> ten)
    """
    k = S.SCALES[scale]
    pw, _ = S.SHEETS[sheet]
    ox, oy = origin
    bx = pw - S.MARGIN_EDGE - TB_W       # goc trai duoi khung ten (mm giay)
    by = S.MARGIN_EDGE
    _poly(msp, _box(bx, by, TB_W, TB_H), k, ox, oy, lw=60)

    left_w = 78.0
    _line(msp, (bx + left_w, by), (bx + left_w, by + TB_H), k, ox, oy, lw=35)

    _draw_left(msp, info, bx, by, left_w, k, ox, oy)
    _draw_right(msp, info, bx + left_w, by, TB_W - left_w, k, ox, oy)


# ---------------------------------------------------------------------------
def _draw_left(msp, info, bx, by, w, k, ox, oy):
    """Cot trai: don vi thiet ke, luoi chu ky, chu dau tu, ngay."""
    y_sign_top = by + TB_H - 20.0
    y_sign_bot = y_sign_top - 26.0
    _line(msp, (bx, y_sign_top), (bx + w, y_sign_top), k, ox, oy, lw=35)
    _line(msp, (bx, y_sign_bot), (bx + w, y_sign_bot), k, ox, oy, lw=35)
    _line(msp, (bx, by + 8.0), (bx + w, by + 8.0), k, ox, oy, lw=18)

    _txt(msp, info.get("cong_ty", ""), (bx + w / 2, y_sign_top + 13.5), 3.0, k, ox, oy, TA.MIDDLE_CENTER, w - 3)
    _txt(msp, info.get("dia_chi", ""), (bx + w / 2, y_sign_top + 8.0), 1.9, k, ox, oy, TA.MIDDLE_CENTER)
    _txt(msp, info.get("lien_he", ""), (bx + w / 2, y_sign_top + 3.5), 1.9, k, ox, oy, TA.MIDDLE_CENTER)

    roles = list(info.get("chu_ky", {}).items())[:4]
    cw = w / max(len(roles), 1)
    for i, (role, person) in enumerate(roles):
        cx = bx + cw * (i + 0.5)
        if i:
            _line(msp, (bx + cw * i, y_sign_bot), (bx + cw * i, y_sign_top), k, ox, oy, lw=13)
        _txt(msp, role.upper(), (cx, y_sign_top - 4.0), 1.8, k, ox, oy, TA.MIDDLE_CENTER, cw - 1.5)
        _txt(msp, person, (cx, y_sign_bot + 2.5), 1.8, k, ox, oy, TA.MIDDLE_CENTER, cw - 1.5)

    _txt(msp, "CHU DAU TU: " + info.get("chu_dau_tu", ""),
         (bx + 2.0, by + 12.5), 2.2, k, ox, oy, TA.MIDDLE_LEFT, w - 4)
    _txt(msp, "NGAY HOAN THANH: " + info.get("ngay", ""),
         (bx + 2.0, by + 4.0), 2.0, k, ox, oy, TA.MIDDLE_LEFT)


def _draw_right(msp, info, bx, by, w, k, ox, oy):
    """Cot phai: cong trinh, vi tri, hang muc, ten ban ve, ty le + so hieu."""
    rows = [16.0, 8.0, 8.0, 14.0, 16.0]      # tu duoi len: so hieu, ten bv, hang muc, vi tri, cong trinh
    y = by
    edges = []
    for h in rows:
        edges.append(y)
        y += h
    for e in edges[1:]:
        _line(msp, (bx, e), (bx + w, e), k, ox, oy, lw=18)

    _txt(msp, info.get("cong_trinh", ""), (bx + w / 2, edges[4] + 8.0), 3.0, k, ox, oy, TA.MIDDLE_CENTER, w - 4)
    _txt(msp, "VI TRI: " + info.get("vi_tri", ""), (bx + 2.0, edges[3] + 4.0), 2.0, k, ox, oy, TA.MIDDLE_LEFT, w - 4)
    _txt(msp, "HANG MUC: " + info.get("hang_muc", ""), (bx + 2.0, edges[2] + 4.0), 2.0, k, ox, oy, TA.MIDDLE_LEFT, w - 4)
    _txt(msp, info.get("ten_ban_ve", ""), (bx + w / 2, edges[1] + 7.0), 3.6, k, ox, oy, TA.MIDDLE_CENTER, w - 4)

    # hang duoi cung chia 2: trai = giai doan + ty le, phai = so hieu ban ve
    xs = bx + w * 0.52
    _line(msp, (xs, by), (xs, edges[1]), k, ox, oy, lw=18)
    _txt(msp, "GIAI DOAN: " + info.get("giai_doan", "TK.BVTC"),
         (bx + 2.0, by + 11.0), 1.9, k, ox, oy, TA.MIDDLE_LEFT)
    _txt(msp, "TY LE: " + info.get("ty_le", "1/100"),
         (bx + 2.0, by + 4.5), 2.2, k, ox, oy, TA.MIDDLE_LEFT)
    _txt(msp, "SO HIEU BAN VE", (xs + (bx + w - xs) / 2, by + 12.5), 1.8, k, ox, oy, TA.MIDDLE_CENTER)
    _txt(msp, info.get("so_hieu", "KT-01"), (xs + (bx + w - xs) / 2, by + 5.5), 5.5, k, ox, oy, TA.MIDDLE_CENTER)


# ---------------------------------------------------------------------------
def _box(x, y, w, h):
    return [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]


def _poly(msp, pts_mm, k, ox, oy, lw=25):
    msp.add_lwpolyline([(ox + p[0] * k, oy + p[1] * k) for p in pts_mm],
                       close=True, dxfattribs={"layer": LAY, "lineweight": lw})


def _line(msp, a, b, k, ox, oy, lw=25):
    msp.add_line((ox + a[0] * k, oy + a[1] * k), (ox + b[0] * k, oy + b[1] * k),
                 dxfattribs={"layer": LAY, "lineweight": lw})


def _txt(msp, text, pt_mm, h_mm, k, ox, oy, align=TA.MIDDLE_LEFT, max_w=None):
    """Ghi chu trong khung ten. max_w (mm) neu co thi tu thu nho chu cho vua o."""
    if not text:
        return
    if max_w:
        est = len(text) * h_mm * 0.92
        if est > max_w:
            h_mm = max(h_mm * max_w / est, 1.3)
    msp.add_text(text, dxfattribs={"layer": LAY, "style": "TCVN", "height": h_mm * k}
                 ).set_placement((ox + pt_mm[0] * k, oy + pt_mm[1] * k), align=align)
