# -*- coding: utf-8 -*-
"""Thep mong: mat cat giang mong va bang thong ke thep dai coc, giang mong.

So thanh va chieu dai SINH TU KICH THUOC dai va luoi truc, khong go tay, de
du toan lay thang tu day ma khong lech voi ban ve.

Thong so giang mong GM 250x400 (2D16 tren, 2D16 duoi, dai D8a200) la DE XUAT
theo cau tao thuong dung cho nha pho, can ky su ket cau duyet lai.
"""

import math

from vncad import rebar

from .draw_structure import CAP_H, CAP_W, PILE_OFF, XS, YS

SC = "1/20"
COVER_MONG = 50.0            # lop bao ve mong theo ghi chu KC-01
MOC = 10                     # moc uon hai dau thanh dai coc: 10 lan duong kinh

D_DAI_COC, BUOC_DAI_COC, SO_THANH_DAU_COC = 16, 150, 4

B_GM, H_GM = 250.0, 400.0
D_GM, SO_THANH_GM = 16, 4    # 2 thanh tren + 2 thanh duoi
D_DAI_GM, BUOC_DAI_GM = 8, 200
NEO, NOI = 30, 40            # neo vao dai 30D, noi chong 40D
CAY_THEP = 11700.0           # chieu dai cay thep thuong pham


def _dai_co_noi(dai):
    """Chieu dai thanh sau khi cong cac moi noi, khi dai hon mot cay thep."""
    so_noi = max(0, math.ceil(dai / CAY_THEP) - 1)
    return dai + so_noi * NOI * D_GM, so_noi


def _dai_dai_gm():
    """Chieu dai mot dai kin giang: chu vi trong lop bao ve cong hai moc 10D."""
    return 2 * ((B_GM - 2 * COVER_MONG) + (H_GM - 2 * COVER_MONG)) + 2 * 10 * D_DAI_GM


def _so_dai(nhip, be_dai):
    """So dai tren cac khoang trong giua hai dai coc lien tiep."""
    return sum(int((b - a - be_dai) // BUOC_DAI_GM) + 1 for a, b in zip(nhip, nhip[1:]))


def _hang_mong():
    """Sinh bang thong ke thep mong tu kich thuoc dai va luoi truc."""
    so_dai_coc = len(XS) * len(YS)
    thanh_dai = CAP_H - 2 * COVER_MONG + 2 * MOC * D_DAI_COC
    thanh_ngan = CAP_W - 2 * COVER_MONG + 2 * MOC * D_DAI_COC
    so_doc = int((CAP_W - 2 * COVER_MONG) // BUOC_DAI_COC) + 1
    ngang, noi_ngang = _dai_co_noi(XS[-1] - XS[0] + 2 * NEO * D_GM)
    doc, _ = _dai_co_noi(YS[-1] - YS[0] + 2 * NEO * D_GM)
    dai_gm = _dai_dai_gm()
    return [
        ("Dai M1, M2 600x1400", "1", "Lop duoi phuong dai", D_DAI_COC,
         int(thanh_dai), so_doc, so_dai_coc),
        ("Dai M1, M2 600x1400", "2", "Tren dau coc", D_DAI_COC,
         int(thanh_ngan), SO_THANH_DAU_COC * len(PILE_OFF), so_dai_coc),
        ("Giang GM1, GM2", "3", "Thang, %d moi noi" % noi_ngang, D_GM,
         int(ngang), SO_THANH_GM, len(YS)),
        ("Giang GM1, GM2", "4", "Dai kin", D_DAI_GM, int(dai_gm),
         _so_dai(XS, CAP_W), len(YS)),
        ("Giang GM3 den GM7", "5", "Thang", D_GM, int(doc), SO_THANH_GM, len(XS)),
        ("Giang GM3 den GM7", "6", "Dai kin", D_DAI_GM, int(dai_gm),
         _so_dai(YS, CAP_H), len(XS)),
    ]


REBAR_ROWS_MONG = _hang_mong()


def draw_tie_beam_section(msp, doc, origin=(0.0, 0.0)):
    """Mat cat ngang giang mong GM 250x400 o 1/20, dat tren lop be tong lot."""
    ox, oy = origin
    rebar.beam_section(msp, doc, (ox, oy), B_GM, H_GM, top=(2, D_GM), bot=(2, D_GM),
                       stir=(D_DAI_GM, BUOC_DAI_GM), label="GM", scale=SC,
                       cover=COVER_MONG)
    y = oy - H_GM / 2
    msp.add_lwpolyline([(ox - B_GM / 2 - 100, y), (ox + B_GM / 2 + 100, y),
                        (ox + B_GM / 2 + 100, y - 100), (ox - B_GM / 2 - 100, y - 100)],
                       close=True, dxfattribs={"layer": "S-FOUND", "lineweight": 13})
