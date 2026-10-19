# -*- coding: utf-8 -*-
"""Chi tiet bo tri cot thep dam va cot, ty le 1/25.

Thep tren MAT CAT ve cham tron to dac. Thep nhin NGANG ve net lien rat dam.
Dai bo tri day a100 vung nut khung va hai dau dam, thua a200 giua nhip,
dung quy dinh khang chan TCVN 5574:2018.
"""

import math

from ezdxf.enums import TextEntityAlignment as TA

from . import standards as S

COVER = 25.0          # lop bao ve dam cot
HOOK = 60.0           # doan moc dai uon 135 do


def bar_dot(msp, pt, dia, layer="S-REBAR"):
    """Mot thanh thep cat ngang: cham tron to dac dung duong kinh that."""
    h = msp.add_hatch(color=1, dxfattribs={"layer": layer})
    h.set_solid_fill()
    e = msp.add_circle(pt, dia / 2.0, dxfattribs={"layer": layer})
    h.paths.add_edge_path().add_arc(pt, dia / 2.0, 0, 360)
    return e


def stirrup(msp, cx, cy, w, h, dia=8.0, layer="S-STIRRUP", cover=COVER):
    """Dai kin quanh tiet dien, co 2 moc uon 135 do o goc tren."""
    x, y = (w - 2 * cover) / 2.0, (h - 2 * cover) / 2.0
    msp.add_lwpolyline([(cx - x, cy - y), (cx + x, cy - y), (cx + x, cy + y),
                        (cx - x, cy + y)], close=True, dxfattribs={"layer": layer})
    d = HOOK * 0.7
    msp.add_line((cx - x, cy + y), (cx - x + d, cy + y - d), dxfattribs={"layer": layer})
    msp.add_line((cx + x, cy + y), (cx + x - d, cy + y - d), dxfattribs={"layer": layer})


def _row(msp, cx, cy, w, n, dia, cover=COVER):
    """Mot hang thep doc chia deu trong be rong tiet dien."""
    inner = w / 2.0 - cover - dia / 2.0
    if n == 1:
        xs = [cx]
    else:
        xs = [cx - inner + 2 * inner * i / (n - 1) for i in range(n)]
    for x in xs:
        bar_dot(msp, (x, cy), dia)


def beam_section(msp, doc, center, w, h, top=(3, 18), bot=(3, 18),
                 stir=(8, 100), label="1-1", scale="1/25", cover=COVER):
    """Mat cat ngang dam: tiet dien, dai kin, thep lop tren va lop duoi.

    cover: lop bao ve; dam cot 25, cau kien mong (giang, dai) phai dung 50.
    """
    cx, cy = center
    msp.add_lwpolyline([(cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2),
                        (cx + w / 2, cy + h / 2), (cx - w / 2, cy + h / 2)],
                       close=True, dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    stirrup(msp, cx, cy, w, h, stir[0], cover=cover)
    _row(msp, cx, cy + h / 2 - cover - top[1] / 2 - stir[0], w, top[0], top[1], cover)
    _row(msp, cx, cy - h / 2 + cover + bot[1] / 2 + stir[0], w, bot[0], bot[1], cover)
    txt = "%dO%d tren / %dO%d duoi / dai O%da%d" % (top[0], top[1], bot[0], bot[1],
                                                    stir[0], stir[1])
    msp.add_text(txt.replace("O", "Ø"), dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("note", scale)
    }).set_placement((cx, cy - h / 2 - 60), align=TA.TOP_CENTER)
    msp.add_text("MAT CAT %s (%dx%d)" % (label, w, h), dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("label", scale)
    }).set_placement((cx, cy + h / 2 + 60), align=TA.BOTTOM_CENTER)


def beam_elevation(msp, doc, x0, x1, z_top, h=400.0, supports=(), scale="1/25",
                   top=(3, 18), bot=(3, 18), stir=(8, 100, 200), label="D1"):
    """Dam nhin ngang: bao dam, thep lop tren duoi, thep gia cuong goi, dai."""
    y1 = z_top
    y0 = y1 - h
    for y in (y0, y1):
        msp.add_line((x0, y), (x1, y), dxfattribs={"layer": "S-BEAM", "lineweight": 35})
    for x in (x0, x1):
        msp.add_line((x, y0), (x, y1), dxfattribs={"layer": "S-BEAM", "lineweight": 35})

    yt = y1 - COVER - top[1] / 2
    yb = y0 + COVER + bot[1] / 2
    msp.add_line((x0 + COVER, yt), (x1 - COVER, yt), dxfattribs={"layer": "S-REBAR"})
    msp.add_line((x0 + COVER, yb), (x1 - COVER, yb), dxfattribs={"layer": "S-REBAR"})
    _label(msp, x0 + (x1 - x0) * 0.62, y1 + 70, "%dO%d lop tren" % (top[0], top[1]), scale)
    _label(msp, x0 + (x1 - x0) * 0.62, y0 - 200, "%dO%d lop duoi" % (bot[0], bot[1]), scale)

    nhip = x1 - x0
    for xs in (x0, x1):
        sgn = 1 if xs == x0 else -1
        msp.add_line((xs + COVER * sgn, yt - 70),
                     (xs + sgn * nhip / 4, yt - 70), dxfattribs={"layer": "S-REBAR"})
    _label(msp, x0 + nhip * 0.16, y1 + 70, "2O18 goi L/4", scale)
    _stirrups(msp, x0, x1, y0, y1, stir, scale)
    msp.add_text("%s %dx%d" % (label, 200, int(h)), dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("label", scale)
    }).set_placement((x0 + (x1 - x0) * 0.78, y1 + 280), align=TA.BOTTOM_CENTER)


def _stirrups(msp, x0, x1, y0, y1, stir, scale):
    """Dai a100 trong pham vi 2h hai dau dam, a200 doan giua nhip."""
    dia, s_dau, s_giua = stir
    h = y1 - y0
    vung = min(2 * h, (x1 - x0) / 3)
    for a, b, buoc in ((x0 + COVER, x0 + vung, s_dau),
                       (x0 + vung, x1 - vung, s_giua),
                       (x1 - vung, x1 - COVER, s_dau)):
        x = a
        while x <= b:
            msp.add_line((x, y0 + COVER), (x, y1 - COVER),
                         dxfattribs={"layer": "S-STIRRUP"})
            x += buoc
    # Ghi buoc dai day o DAU BEN PHAI: dau ben trai la cho dat net cat 1-1,
    # hai dau deu a100 nen ghi ben nao cung dung.
    _label(msp, x1 - vung / 2, y0 - 200, "O%da%d" % (dia, s_dau), scale)
    _label(msp, x0 + (x1 - x0) * 0.42, y0 - 200, "O%da%d" % (dia, s_giua), scale)


def column_section(msp, doc, center, w, h, bars=(4, 18), stir=(8, 100),
                   label="C1", scale="1/25"):
    """Mat cat cot: tiet dien, dai kin, thep chu dat 4 goc hoac chia deu 2 canh."""
    cx, cy = center
    msp.add_lwpolyline([(cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2),
                        (cx + w / 2, cy + h / 2), (cx - w / 2, cy + h / 2)],
                       close=True, dxfattribs={"layer": "S-COLS", "lineweight": 40})
    stirrup(msp, cx, cy, w, h, stir[0])
    n, dia = bars
    moi_canh = max(n // 2, 2)
    for y in (cy - h / 2 + COVER + dia / 2 + stir[0],
              cy + h / 2 - COVER - dia / 2 - stir[0]):
        _row(msp, cx, y, w, moi_canh, dia)
    dong = [("%s %dx%d" % (label, w, h)),
            ("%dO%d + dai O%da%d" % (n, dia, stir[0], stir[1])).replace("O", "Ø")]
    hn = S.th("note", scale)
    for i, d in enumerate(dong):
        msp.add_text(d, dxfattribs={"layer": "S-TEXT", "style": "TCVN",
                                    "height": hn}
                     ).set_placement((cx, cy - h / 2 - 70 - i * hn * 1.5),
                                     align=TA.TOP_CENTER)


def _label(msp, x, y, text, scale):
    msp.add_text(text.replace("O", "Ø"), dxfattribs={
        "layer": "S-TEXT", "style": "TCVN", "height": S.th("note", scale)
    }).set_placement((x, y), align=TA.BOTTOM_CENTER)
