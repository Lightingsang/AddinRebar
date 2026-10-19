# -*- coding: utf-8 -*-
"""Khai trien phong ve sinh WC2: mat bang va 4 mat tuong op gach, ty le 1/50.

Ve o kich thuoc that. Mat bang o tren, 4 mat tuong xep 2 hang 2 cot ben duoi
de vua cot giay ben trai, khong lan sang vung khung ten.
"""

from vncad import annotate as an, arch, blocks, fixtures as fx

SC = "1/50"
H_TRAN = 2700.0                  # cao tran WC

# Quy cach tung phong ve sinh: (rong, sau, cao do san, co bon tam dung)
WC_SPECS = {
    "WC 1": (1900.0, 1900.0, 0.100, False),
    "WC 2": (2400.0, 2300.0, 3.750, True),
    "WC 3": (2400.0, 2200.0, 7.150, True),
}
W, H = WC_SPECS["WC 2"][0], WC_SPECS["WC 2"][1]
TILE_W, TILE_H = 600.0, 300.0    # gach op 300x600 dat nam ngang
GRID = [(0.0, -4800.0), (3200.0, -4800.0), (0.0, -9600.0), (3200.0, -9600.0)]
WALLS = ["A", "B", "C", "D"]
WIDTHS = {"A": W, "B": H, "C": W, "D": H}


def draw_wc_sheet(msp, doc, origin=(0.0, 0.0), phong="WC 2"):
    """Ve tron cum khai trien mot phong ve sinh tai goc dat cho truoc."""
    global W, H, WIDTHS
    W, H, cot, co_tam = WC_SPECS[phong]
    WIDTHS = {"A": W, "B": H, "C": W, "D": H}
    ox, oy = origin
    _plan(msp, doc, ox, oy, phong, cot, co_tam)
    for ten, (dx, dy) in zip(WALLS, GRID):
        _wall(msp, doc, ox + dx, oy + dy, WIDTHS[ten], ten, co_tam)


def _plan(msp, doc, ox, oy, phong, cot, co_tam):
    """Mat bang WC: tuong bao, thiet bi, pheu thu, ky hieu huong nhin 4 mat."""
    for p1, p2 in (((0, 0), (W, 0)), ((W, 0), (W, H)),
                   ((W, H), (0, H)), ((0, H), (0, 0))):
        arch.wall(msp, (ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]), t=110)
    fx.place(msp, doc, "BON-CAU", (ox + 480, oy + H - 600), rotation=-90)
    fx.place(msp, doc, "LAVABO", (ox + W - 700, oy + H - 260), rotation=180)
    if co_tam:
        fx.place(msp, doc, "VOI-SEN", (ox + W - 600, oy + 560))
    fx.place(msp, doc, "PHEU-THU", (ox + 700, oy + 500))
    an.room_label(msp, doc, (ox + W / 2 - 50, oy + H / 2 - 100), phong,
                  round(W * H / 1e6, 1), cot, SC)
    _view_marks(msp, doc, ox, oy)
    an.dim_chain(msp, doc, [ox, ox + W], oy, "h", SC, side=-1)
    an.dim_chain(msp, doc, [oy, oy + H], ox, "v", SC, side=-1)
    an.view_title(msp, (ox + W / 2, oy - 1700), "Mat bang %s" % phong, "1/50", SC)


def _view_marks(msp, doc, ox, oy):
    """Ky hieu huong nhin 4 mat tuong, dat sat 4 canh phong."""
    pos = {"A": ((ox + W / 2, oy - 420), 0), "B": ((ox + W + 420, oy + H / 2), 90),
           "C": ((ox + W / 2, oy + H + 420), 180), "D": ((ox - 420, oy + H / 2), 270)}
    for ten, (pt, rot) in pos.items():
        blocks.insert(msp, doc, "KY-HIEU-MAT-CAT", pt, SC, "A-SYMBOL",
                      {"SO": ten}, rotation=rot)


def _wall(msp, doc, ox, oy, rong, ten, co_tam=True):
    """Mot mat tuong khai trien: khung, gach op, thiet bi, kich thuoc chieu cao."""
    msp.add_lwpolyline([(ox, oy), (ox + rong, oy), (ox + rong, oy + H_TRAN),
                        (ox, oy + H_TRAN)], close=True,
                       dxfattribs={"layer": "A-ELEV", "lineweight": 40})
    _tiles(msp, ox, oy, rong)
    ham = {"A": _wall_a, "B": _wall_b, "C": _wall_c, "D": _wall_d}[ten]
    ham(msp, doc, ox, oy, rong, co_tam) if ten == "C" else ham(msp, doc, ox, oy, rong)
    an.dim_chain(msp, doc, [oy, oy + H_TRAN], ox, "v", SC, side=-1)
    an.view_title(msp, (ox + rong / 2, oy - 1000), "Mat tuong %s" % ten, "1/50", SC)


def _tiles(msp, ox, oy, rong):
    """Hoa van gach op 300x600 tu san len kich tran, mach thang hang."""
    y = oy + TILE_H
    while y < oy + H_TRAN - 1:
        msp.add_line((ox, y), (ox + rong, y), dxfattribs={"layer": "A-MAT"})
        y += TILE_H
    x = ox + TILE_W
    while x < ox + rong - 1:
        msp.add_line((x, oy), (x, oy + H_TRAN), dxfattribs={"layer": "A-MAT"})
        x += TILE_W


def _rect(msp, x, y, w, h, layer="A-SAN"):
    msp.add_lwpolyline([(x, y), (x + w, y), (x + w, y + h), (x, y + h)],
                       close=True, dxfattribs={"layer": layer})


def _wall_a(msp, doc, ox, oy, rong):
    """Mat co cua di vao WC."""
    x = ox + rong / 2 - 400
    _rect(msp, x, oy, 800, 2100, "A-DOOR")
    _rect(msp, x + 60, oy + 60, 680, 1980, "A-DOOR")
    # Chu dan len PHIA TREN mat co cua chu khong keo sang phai: keo sang phai
    # la chu roi vao pham vi mat tuong ke ben, nam de len mach gach cua no.
    an.leader_note(msp, "Cua nhua chiu am", (x + 400, oy + 1900),
                   (ox + rong - 200, oy + 2950), SC)


def _wall_b(msp, doc, ox, oy, rong):
    """Mat dat lavabo va guong soi."""
    cx = ox + rong / 2
    _rect(msp, cx - 250, oy + 800, 500, 180)          # chau rua
    _rect(msp, cx - 60, oy + 980, 120, 300)           # chan chau
    _rect(msp, cx - 300, oy + 1250, 600, 800, "A-MAT")  # guong
    an.leader_note(msp, "Guong 600x800", (cx, oy + 1650),
                   (ox + rong + 600, oy + 2200), SC)
    an.leader_note(msp, "Lavabo cao 800", (cx, oy + 880),
                   (ox + rong + 600, oy + 600), SC)


def _wall_c(msp, doc, ox, oy, rong, co_tam=True):
    """Mat co cua so thong gio va khu tam dung sen cay."""
    _rect(msp, ox + rong - 900, oy + 1900, 600, 600, "A-WIND")
    if not co_tam:
        an.leader_note(msp, "Cua so lat", (ox + rong - 600, oy + 2200),
                       (ox + rong + 600, oy + 2450), SC)
        return
    x = ox + 700
    msp.add_line((x, oy + 300), (x, oy + 2150), dxfattribs={"layer": "A-SAN"})
    msp.add_line((x, oy + 2150), (x + 260, oy + 2150), dxfattribs={"layer": "A-SAN"})
    _rect(msp, x + 200, oy + 2050, 180, 100)
    an.leader_note(msp, "Sen cay", (x, oy + 2150),
                   (ox - 900, oy + 2450), SC)
    an.leader_note(msp, "Cua so lat", (ox + rong - 600, oy + 2200),
                   (ox + rong + 600, oy + 2450), SC)


def _wall_d(msp, doc, ox, oy, rong):
    """Mat dat bon cau va vach kinh chan nuoc."""
    x = ox + 500
    _rect(msp, x, oy, 700, 400)
    _rect(msp, x + 480, oy + 400, 220, 500)
    an.leader_note(msp, "Bon cau bet", (x + 350, oy + 300),
                   (ox + rong + 600, oy + 500), SC)
    msp.add_line((ox + rong - 100, oy), (ox + rong - 100, oy + 1900),
                 dxfattribs={"layer": "A-WIND"})
    an.leader_note(msp, "Vach kinh 8mm", (ox + rong - 100, oy + 1500),
                   (ox + rong + 600, oy + 2100), SC)


NOTES_WC = [
    "Op gach men chong tham 300x600 kich tran cao 2700mm, mach op 2mm vua chuyen dung.",
    "Lat gach chong truot 300x300, do doc 1% ve pheu thu san D90.",
    "Cot nen WC ha 50mm so voi san phong, quet chong tham Sika Top Seal 107 ba lop.",
    "Chong tham vat len chan tuong cao 300mm truoc khi op gach.",
    "Ong thoat bon cau D114 di thang vao be tu hoai; thoat san D60 qua pheu chong hoi.",
    "Cua WC nhua composite hoac go cong nghiep chiu am, khe thoang duoi canh 20mm.",
]
