# -*- coding: utf-8 -*-
"""Chi tiet bo tri cot thep dam D1 va cot C1.

Khai trien dam theo chieu dai ve o 1/50 cho vua kho giay; mat cat cau kien
va cot dung ve o 1/25 de doc ro tung thanh thep.
"""

from vncad import annotate as an, rebar

SC50, SC25 = "1/50", "1/25"
NHIP = 5000.0            # nhip dam D1 giua truc 1 va truc 2
H_DAM = 400.0
H_TANG = 3650.0
W_COT = 220.0


def draw_beam_elevation(msp, doc, origin=(0.0, 0.0)):
    """Dam D1 nhin ngang o ty le 1/50, kem vi tri 2 mat cat."""
    ox, oy = origin
    rebar.beam_elevation(msp, doc, ox, ox + NHIP, oy + H_DAM, H_DAM, scale=SC50,
                         top=(3, 18), bot=(3, 18), stir=(8, 100, 200), label="D1")
    an.dim_chain(msp, doc, [ox, ox + NHIP], oy - 400, "h", SC50, side=-1)
    # Cat 1-1 dat o 400 chu khong phai 700: cho 700 la dong ghi chu
    # "2O18 goi L/4", net cat de o do la dam thang vao chu.
    for x, ten in ((ox + 400, "1-1"), (ox + NHIP / 2, "2-2")):
        msp.add_line((x, oy - 300), (x, oy + H_DAM + 300),
                     dxfattribs={"layer": "S-AXIS"})
        msp.add_text(ten, dxfattribs={
            "layer": "S-TEXT", "style": "TCVN", "height": 100
        }).set_placement((x, oy + H_DAM + 380))
    an.view_title(msp, (ox + NHIP / 2, oy - 1300), "Chi tiet dam D1 200x400",
                  "1/50", SC50)


def draw_beam_sections(msp, doc, origin=(0.0, 0.0)):
    """Hai mat cat dam: 1-1 tai goi (thep tren nhieu), 2-2 giua nhip."""
    ox, oy = origin
    rebar.beam_section(msp, doc, (ox, oy), 200, H_DAM,
                       top=(5, 18), bot=(3, 18), stir=(8, 100), label="1-1", scale=SC25)
    rebar.beam_section(msp, doc, (ox + 1200, oy), 200, H_DAM,
                       top=(3, 18), bot=(4, 18), stir=(8, 200), label="2-2", scale=SC25)


def draw_column_c1(msp, doc, origin=(0.0, 0.0)):
    """Cot C1 nhin dung o 1/25: dai day a100 vung nut khung, a200 doan giua."""
    ox, oy = origin
    for s in (-1, 1):
        msp.add_line((ox + s * W_COT / 2, oy), (ox + s * W_COT / 2, oy + H_TANG),
                     dxfattribs={"layer": "S-COLS", "lineweight": 40})
    for dx in (-70, 70):
        msp.add_line((ox + dx, oy + 40), (ox + dx, oy + H_TANG - 40),
                     dxfattribs={"layer": "S-REBAR"})
    _dai_cot(msp, ox, oy)
    an.dim_chain(msp, doc, [oy, oy + H_TANG], ox - W_COT, "v", SC25, side=-1)
    an.view_title(msp, (ox, oy - 700), "Chi tiet cot C1 220x220", "1/25", SC25)
    rebar.column_section(msp, doc, (ox + 1050, oy + H_TANG * 0.5), 220, 220,
                         bars=(4, 18), stir=(8, 100), label="C1", scale=SC25)


def _dai_cot(msp, ox, oy):
    """Dai a100 trong pham vi 1/4 chieu cao tang o chan va dau cot, giua a200."""
    vung = H_TANG / 4
    doan = ((oy + 40, oy + vung, 100, "Ø8a100 nut khung"),
            (oy + vung, oy + H_TANG - vung, 200, "Ø8a200 giua cot"),
            (oy + H_TANG - vung, oy + H_TANG - 40, 100, "Ø8a100 nut khung"))
    for a, b, buoc, ten in doan:
        y = a
        while y <= b:
            msp.add_line((ox - W_COT / 2 + 25, y), (ox + W_COT / 2 - 25, y),
                         dxfattribs={"layer": "S-STIRRUP"})
            y += buoc
        an.leader_note(msp, ten, (ox + W_COT / 2 - 25, (a + b) / 2),
                       (ox - 1300, (a + b) / 2), SC25)
    an.leader_note(msp, "4Ø18 thep chu", (ox + 70, oy + H_TANG * 0.35),
                   (ox + 700, oy + H_TANG * 0.22), SC25)


NOTES_THEP = [
    "Be tong dam cot cap do ben B20 (M250); thep doc CB300-V, thep dai CB240-T.",
    "Lop bao ve be tong dam va cot 25mm tinh tu mep ngoai thep dai.",
    "Dai dam day a100 trong pham vi 2h hai dau dam, a200 doan giua nhip.",
    "Dai cot day a100 trong pham vi 1/4 chieu cao tang o chan va dau cot.",
    "Moc dai uon 135 do, doan neo thang tu 10d va khong nho hon 75mm.",
    "Thep goi tang cuong keo dai toi thieu 1/4 nhip tinh tu mep goi.",
    "Noi chong vung nen 30d, vung keo 40d; noi so le khong qua 50% mot mat cat.",
    "Khong noi thep trong vung co mo men lon (giua nhip lop duoi, goi lop tren).",
]


# ---------------------------------------------------------------------------
# CHI TIET NUT KHUNG DAM COT, TY LE 1/20
# ---------------------------------------------------------------------------
SC20 = "1/20"
L_DAM = 1200.0          # doan dam ve quanh nut, du de the hien neo thep


def _net_ngat(msp, x, y_tren, y_duoi):
    """Net ngat hinh: bao hieu dam con keo dai ra ngoai pham vi ve."""
    msp.add_lwpolyline([(x, y_tren), (x + 90, (y_tren + y_duoi) / 2 + 60),
                        (x - 90, (y_tren + y_duoi) / 2 - 60), (x, y_duoi)],
                       dxfattribs={"layer": "S-BEAM", "lineweight": 13})


def draw_joint_detail(msp, doc, origin=(0.0, 0.0)):
    """Nut giao dam voi cot: thep cot di suot, thep dam neo vao cot, dai day a100.

    Ve o ty le 1/20 vi day la nut chiu luc quan trong nhat cua khung.
    """
    ox, oy = origin
    h_cot, h_dam = 1600.0, 400.0
    # chi lay doan dam 1.2m quanh nut, dung ve het nhip dam
    _hinh_hoc_nut(msp, ox, oy, h_cot, h_dam)
    _thep_cot(msp, ox, oy, h_cot)
    _thep_dam(msp, ox, oy, h_cot, h_dam)
    _dai_nut(msp, ox, oy, h_cot, h_dam)
    an.view_title(msp, (ox + 400, oy - 620), "Chi tiet nut khung dam cot",
                  "1/20", SC20)


def _hinh_hoc_nut(msp, ox, oy, h_cot, h_dam):
    """Bao cot chay doc va bao dam dam ngang vao cot."""
    for s in (-1, 1):
        msp.add_line((ox + s * W_COT / 2, oy), (ox + s * W_COT / 2, oy + h_cot),
                     dxfattribs={"layer": "S-COLS", "lineweight": 40})
    y_dam = oy + h_cot * 0.62
    for dy in (0, -h_dam):
        msp.add_line((ox + W_COT / 2, y_dam + dy), (ox + L_DAM, y_dam + dy),
                     dxfattribs={"layer": "S-BEAM", "lineweight": 40})
    _net_ngat(msp, ox + L_DAM, y_dam, y_dam - h_dam)
    an.leader_note(msp, "Dam D1 200x400", (ox + 900, y_dam - h_dam / 2),
                   (ox + 1500, y_dam + 620), SC20)
    an.leader_note(msp, "Cot C1 220x220", (ox, oy + h_cot * 0.18),
                   (ox - 1350, oy + h_cot * 0.08), SC20)


def _thep_cot(msp, ox, oy, h_cot):
    """Thep chu cot di thang qua nut, khong duoc noi trong nut khung."""
    for dx in (-70, 70):
        msp.add_line((ox + dx, oy + 40), (ox + dx, oy + h_cot - 40),
                     dxfattribs={"layer": "S-REBAR"})
    an.leader_note(msp, "4Ø18 di suot, cam noi", (ox + 70, oy + h_cot * 0.9),
                   (ox - 1350, oy + h_cot * 0.98), SC20)


def _thep_dam(msp, ox, oy, h_cot, h_dam):
    """Thep dam neo vao cot, uon moc vuong 90 do dai tu 12 lan duong kinh."""
    y_dam = oy + h_cot * 0.62
    for dy, huong in ((-25 - 9, -1), (-h_dam + 25 + 9, 1)):
        y = y_dam + dy
        msp.add_lwpolyline([(ox + L_DAM, y), (ox - W_COT / 2 + 25, y),
                            (ox - W_COT / 2 + 25, y + huong * 260)],
                           dxfattribs={"layer": "S-REBAR"})
    an.leader_note(msp, "Neo 30Ø, moc vuong 12Ø", (ox - 60, y_dam - 34),
                   (ox - 1350, y_dam + 780), SC20)


def _dai_nut(msp, ox, oy, h_cot, h_dam):
    """Dai cot day a100 trong pham vi nut va 2h ve hai phia."""
    y_dam = oy + h_cot * 0.62
    a, b = y_dam - h_dam - 2 * h_dam, y_dam + 2 * h_dam
    y = max(a, oy + 40)
    while y <= min(b, oy + h_cot - 40):
        msp.add_line((ox - W_COT / 2 + 25, y), (ox + W_COT / 2 - 25, y),
                     dxfattribs={"layer": "S-STIRRUP"})
        y += 100
    an.leader_note(msp, "Dai Ø8a100 vung nut",
                   (ox + W_COT / 2 - 25, y_dam - h_dam - 300),
                   (ox + 1500, y_dam - h_dam - 780), SC20)
