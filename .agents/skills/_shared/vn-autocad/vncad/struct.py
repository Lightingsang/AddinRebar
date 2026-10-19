# -*- coding: utf-8 -*-
"""Cau kien ket cau: coc, dai mong, gian mong, cot, dam, san, bang thong ke thep.

Ma hieu cau kien dat theo dung quy uoc trong ho so thuc te:
M1..M4 (dai mong), CC1..CC7 (cum coc), C1..C4 (cot), GM1..GM18 (gian mong),
DK1 (da kieng), D1/D2 (dam), S1..S6 (o san).
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

UNIT_WEIGHT = {6: 0.222, 8: 0.395, 10: 0.617, 12: 0.888, 14: 1.208,
               16: 1.578, 18: 1.998, 20: 2.466, 22: 2.984, 25: 3.853}


def bar_weight(dia_mm, length_m):
    """Trong luong 1 thanh thep: W = D^2 / 162.2 * L (kg)."""
    return dia_mm * dia_mm / 162.2 * length_m


# ---------------------------------------------------------------------------
# MONG
# ---------------------------------------------------------------------------
def pile(msp, center, size=S.PILE, layer="S-FOUND"):
    """Coc ep BTCT tiet dien vuong, ky hieu 2 duong cheo."""
    cx, cy = center
    h = size / 2.0
    pts = [(cx - h, cy - h), (cx + h, cy - h), (cx + h, cy + h), (cx - h, cy + h)]
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer})
    msp.add_line(pts[0], pts[2], dxfattribs={"layer": layer})
    msp.add_line(pts[1], pts[3], dxfattribs={"layer": layer})


def pile_cap(msp, center, w, h, pile_offsets, label=None, scale="1/100",
             layer="S-FOUND", lean=100, label_goc=(1, 1)):
    """Dai mong coc: vien be tong lot (net dut ngoai) + dai + cac tim coc.

    pile_offsets: [(dx, dy), ...] vi tri tim coc so voi tam dai.
    label_goc: goc dat ma hieu, (1, 1) la goc tren ben phai. Ma hieu phai dat
    o GOC chu khong dat thang tren dinh dai, vi da kieng chay doc theo truc
    di ngay qua dinh dai, dat thang tren la nhan nam de len net da kieng.
    """
    cx, cy = center
    _rect(msp, cx, cy, w + 2 * lean, h + 2 * lean, layer, lw=13)
    _rect(msp, cx, cy, w, h, layer, lw=40)
    for dx, dy in pile_offsets:
        pile(msp, (cx + dx, cy + dy), layer=layer)
    if label:
        sx, sy = label_goc
        canh = TA.BOTTOM_LEFT if sx > 0 else TA.BOTTOM_RIGHT
        msp.add_text(label, dxfattribs={
            "layer": "S-TEXT", "style": "TCVN", "height": S.th("label", scale)
        }).set_placement((cx + sx * (w / 2 + lean + 150),
                          cy + sy * (h / 2 + lean + 150)), align=canh)


def tie_beam(msp, p1, p2, width=250, label=None, scale="1/100", layer="S-FOUND",
             label_pos=0.5):
    """Gian mong / da kieng: 2 net bien + ma hieu GM../DK..

    label_pos: vi tri dat nhan doc theo dam, 0 la dau, 1 la cuoi, 0.5 la giua.
    Dung de tach nhan khi hai dam giao nhau.
    """
    dx, dy = p2[0] - p1[0], p2[1] - p1[1]
    L = math.hypot(dx, dy)
    nx, ny = -dy / L * width / 2, dx / L * width / 2
    for s in (1, -1):
        msp.add_line((p1[0] + s * nx, p1[1] + s * ny), (p2[0] + s * nx, p2[1] + s * ny),
                     dxfattribs={"layer": layer})
    if label:
        mx = p1[0] + (p2[0] - p1[0]) * label_pos
        my = p1[1] + (p2[1] - p1[1]) * label_pos
        ang = math.degrees(math.atan2(dy, dx))
        h = S.th("label", scale)
        # Day nhan ra khoi long dam theo phuong vuong goc: nua be rong dam,
        # cong nua chieu cao chu, cong mot khoang ho. Truoc day chi cong theo
        # truc Y nen dam dung bi nhan nam de len chinh net bien cua no.
        d = (width / 2 + h * 0.85) / (width / 2)
        msp.add_text(label, dxfattribs={
            "layer": "S-TEXT", "style": "TCVN", "height": h,
            "rotation": ang if -90 < ang <= 90 else ang + 180,
        }).set_placement((mx + nx * d, my + ny * d), align=TA.MIDDLE_CENTER)


# ---------------------------------------------------------------------------
# KHUNG DAM COT SAN
# ---------------------------------------------------------------------------
def beam(msp, p1, p2, width=200, label=None, scale="1/100", layer="S-BEAM",
         label_pos=0.5):
    """Dam tren mat bang ket cau: 2 net bien + ma hieu kem tiet dien."""
    tie_beam(msp, p1, p2, width, label, scale, layer, label_pos)


def slab_zone(msp, pts, label=None, level=None, thickness=None, scale="1/100",
              layer="S-SLAB"):
    """O san: duong bao + ma hieu S.. + chieu day + cao do."""
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer})
    if not label:
        return
    cx = sum(p[0] for p in pts) / len(pts)
    cy = sum(p[1] for p in pts) / len(pts)
    h = S.th("label", scale)
    msp.add_text(label, dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": h}).set_placement((cx, cy), align=TA.MIDDLE_CENTER)
    extra = []
    if thickness:
        extra.append("h=%dmm" % thickness)
    if level is not None:
        extra.append(("+%.3f" if level >= 0 else "%.3f") % level)
    if extra:
        msp.add_text(" / ".join(extra), dxfattribs={
            "layer": "S-TEXT", "style": "TCVN", "height": S.th("note", scale)
        }).set_placement((cx, cy - h * 1.4), align=TA.MIDDLE_CENTER)


def rebar_line(msp, pts, dia, spacing=None, count=None, layer="S-REBAR", scale="1/100"):
    """Duong thep chiu luc + ghi chu Ø..a.. dat doc theo thanh."""
    msp.add_lwpolyline(pts, dxfattribs={"layer": layer})
    txt = "%s%d%s" % ("%d" % count if count else "", dia, "a%d" % spacing if spacing else "")
    # tuyen 2 diem thi pts[1] la dau cuoi, phai lay trung diem that de dat chu
    mid = (pts[len(pts) // 2] if len(pts) > 2 else
           ((pts[0][0] + pts[1][0]) / 2, (pts[0][1] + pts[1][1]) / 2))
    msp.add_text("Ø" + txt if not count else "%dØ%d%s" % (
        count, dia, "a%d" % spacing if spacing else ""), dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("note", scale)
    }).set_placement((mid[0], mid[1] + 60), align=TA.BOTTOM_CENTER)


# ---------------------------------------------------------------------------
# BANG THONG KE COT THEP
# ---------------------------------------------------------------------------
SCHEDULE_COLS = [
    ("STT", 10), ("CAU KIEN", 26), ("HIEU THANH", 16), ("HINH DANG", 26),
    ("D (mm)", 13), ("DAI 1 THANH (mm)", 26), ("SL/CK", 12), ("SO CK", 12),
    ("TONG DAI (m)", 21), ("KL (kg)", 18),
]


def rebar_schedule(msp, origin, rows, scale="1/100", layer="S-SCHEDULE", title="BANG THONG KE COT THEP", row_h=7.0):
    """Ve bang thong ke cot thep.

    rows: [(cau_kien, hieu, hinh_dang, dia, dai_mm, sl_ck, so_ck), ...]
    Tra ve tong khoi luong kg va toa do day bang.
    """
    k = S.SCALES[scale]
    x0, y0 = origin
    rh = row_h * k
    total_w = sum(w for _, w in SCHEDULE_COLS) * k
    h_head = S.th("note", scale)

    msp.add_text(title, dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("subtitle", scale)
    }).set_placement((x0 + total_w / 2, y0 + rh * 0.6), align=TA.BOTTOM_CENTER)

    y = y0
    _row_lines(msp, x0, y, total_w, rh, layer)
    cx = x0
    for name, w in SCHEDULE_COLS:
        msp.add_text(name, dxfattribs={
            "layer": "S-TEXT", "style": "TCVN", "height": h_head * 0.9
        }).set_placement((cx + w * k / 2, y - rh / 2), align=TA.MIDDLE_CENTER)
        msp.add_line((cx, y), (cx, y - rh * (len(rows) + 1)), dxfattribs={"layer": layer})
        cx += w * k
    msp.add_line((cx, y), (cx, y - rh * (len(rows) + 1)), dxfattribs={"layer": layer})

    grand = 0.0
    for i, (ck, hieu, hinh, dia, dai, sl, so) in enumerate(rows, 1):
        y -= rh
        _row_lines(msp, x0, y, total_w, rh, layer)
        tong_dai = dai / 1000.0 * sl * so
        kl = bar_weight(dia, tong_dai)
        grand += kl
        vals = [str(i), ck, hieu, hinh, str(dia), "%d" % dai, str(sl), str(so),
                "%.2f" % tong_dai, "%.1f" % kl]
        cx = x0
        for (name, w), v in zip(SCHEDULE_COLS, vals):
            msp.add_text(v, dxfattribs={
                "layer": "S-TEXT", "style": "TCVN", "height": h_head * 0.85
            }).set_placement((cx + w * k / 2, y - rh / 2), align=TA.MIDDLE_CENTER)
            cx += w * k
    y -= rh * 1.4
    msp.add_text("TONG KHOI LUONG THEP: %.1f kg" % grand, dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("label", scale)
    }).set_placement((x0 + total_w, y), align=TA.TOP_RIGHT)
    return grand, y


def _row_lines(msp, x0, y, w, rh, layer):
    msp.add_line((x0, y), (x0 + w, y), dxfattribs={"layer": layer})
    msp.add_line((x0, y - rh), (x0 + w, y - rh), dxfattribs={"layer": layer})


def _rect(msp, cx, cy, w, h, layer, lw=25):
    msp.add_lwpolyline(
        [(cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2),
         (cx + w / 2, cy + h / 2), (cx - w / 2, cy + h / 2)],
        close=True, dxfattribs={"layer": layer, "lineweight": lw})
