# -*- coding: utf-8 -*-
"""Mat bang lat gach va mat bang tran cho tung tang."""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, mep, standards as S, tiles

from . import layout as L

SC = "1/100"

# Vung lat gach: (ten_vung, x0, y0, x1, y1, co_gach, ma_vat_lieu)
TILES = {
    1: [("Gara o to", 0, 0, 5500, 5000, 400, "G1"),
        ("Phong khach", 5500, 0, 9500, 5000, 800, "G2"),
        ("Gieng troi + thang", 9500, 0, 13000, 5000, 600, "G3"),
        ("Bep + phong an", 13000, 0, 18000, 5000, 800, "G2"),
        ("WC 1", 9500, 0, 11400, 1900, 300, "G4")],
    2: [("Ban cong", 0, 0, 1500, 5000, 300, "G4"),
        ("Phong ngu 1", 1500, 0, 7800, 5000, 800, "G2"),
        ("Sanh", 7800, 0, 13000, 5000, 800, "G2"),
        ("Phong ngu 2", 13000, 0, 18000, 5000, 800, "G2"),
        ("WC 2", 10500, 0, 13000, 2400, 300, "G4")],
    3: [("Ban cong", 0, 0, 1500, 5000, 300, "G4"),
        ("Phong tho", 1500, 0, 7800, 5000, 800, "G2"),
        ("Sanh", 7800, 0, 13000, 5000, 800, "G2"),
        ("Phong ngu 3", 13000, 0, 15600, 5000, 800, "G2"),
        ("WC 3", 15600, 0, 18000, 2200, 300, "G4"),
        ("San phoi", 15600, 2200, 18000, 5000, 300, "G4")],
}

# Vai phong lon long ca phong nho ben trong (sanh om WC, gieng troi om WC),
# tam hinh hoc cua no roi dung vao vach ngan. Day nhan sang cho trong.
NHAN_LECH = {
    "Gieng troi + thang": (0, 1400),
    "Sanh": (-1600, 1600),
    "Phong ngu 3": (0, -1200),
}

MA_VAT_LIEU = [
    ("G1", "Gach Granite nham 400x400 chong truot, khu gara va tam cap"),
    ("G2", "Gach Granite bong kien 800x800, phong o va sanh"),
    ("G3", "Gach the nung 600x600 lat gieng troi, chiu mua nang"),
    ("G4", "Gach men chong truot 300x300, WC va ban cong, do doc 1%"),
]


def draw_tiling_plan(msp, doc, floor, origin=(0.0, 0.0)):
    """Mat bang lat gach mot tang: hoa van gach tung phong + ma vat lieu."""
    ox, oy = origin
    _walls_only(msp, ox, oy, floor)
    dat = [_cho_nhan(ox, oy, v) for v in TILES[floor]]
    o_chua = [d[3] for d in dat]           # chua cho MOI nhan trong ca tang
    for (ten, x0, y0, x1, y1, tile, ma), (nhan, tam, kieu, _) in zip(TILES[floor], dat):
        tiles.floor_tiles(msp, ox + x0 + 60, oy + y0 + 60, ox + x1 - 60, oy + y1 - 60,
                         tile=tile, chua=o_chua)
        msp.add_text(nhan, dxfattribs={
            "layer": "TEXT", "style": "TCVN", "height": kieu[0], "rotation": kieu[1]
        }).set_placement(tam, align=TA.MIDDLE_CENTER)


def _cho_nhan(ox, oy, vung):
    """Tinh san noi dung, tam dat, co chu va o chua cho cua mot nhan lat gach."""
    ten, x0, y0, x1, y1, tile, ma = vung
    tam = (ox + (x0 + x1) / 2 + NHAN_LECH.get(ten, (0, 0))[0],
           oy + (y0 + y1) / 2 + NHAN_LECH.get(ten, (0, 0))[1])
    nhan = "%s %dx%d" % (ma, tile, tile)
    cao, xoay = _co_chu(nhan, x1 - x0, y1 - y0)
    rong = S.rong_chu(nhan, cao)
    w, h = (cao + 200, rong + 240) if xoay else (rong + 240, cao + 200)
    return nhan, tam, (cao, xoay), (tam[0] - w / 2, tam[1] - h / 2,
                                    tam[0] + w / 2, tam[1] + h / 2)


def _co_chu(nhan, rong_phong, cao_phong, cao_goc=None):
    """Chon chieu cao chu va goc xoay sao cho dong ma vat lieu nam gon trong phong.

    Phong hep ma dai thi ghi doc; phong nho ca hai chieu thi ha chieu cao chu
    xuong, con hon de chu tran ra de len net tuong.
    """
    cao = cao_goc or S.th("label", SC)
    trong_ngang, trong_doc = rong_phong - 300, cao_phong - 300
    rong = S.rong_chu(nhan, cao)
    if rong <= trong_ngang:
        return cao, 0.0
    if rong <= trong_doc:
        return cao, 90.0
    lon = max(trong_ngang, trong_doc)
    return max(cao * lon / rong, S.th("note", SC) * 0.8), (0.0 if lon == trong_ngang else 90.0)


def _walls_only(msp, ox, oy, floor):
    """Nen tuong va cot, khong ve noi that de nhin ro hoa van gach."""
    walls = [((ox + p1[0], oy + p1[1]), (ox + p2[0], oy + p2[1]), t, o)
             for p1, p2, t, o in L.FLOOR_WALLS[floor]]
    cols = [(ox + x, oy + y, sz) for x, y, sz in L.COLUMNS]
    mep.background(msp, walls, cols, layer="A-WALL")


# ---------------------------------------------------------------------------
# MAT BANG TRAN
# ---------------------------------------------------------------------------
CEILING = {
    1: [("Gara: tran phang son nuoc", 0, 0, 5500, 5000, 0),
        ("Tran giat cap 2 lop", 5500, 0, 9500, 5000, 2),
        ("Thong tang, khong tran", 9500, 0, 13000, 5000, -1),
        ("Tran giat cap 1 lop", 13000, 0, 18000, 5000, 1)],
    2: [("Ban cong: tran phang", 0, 0, 1500, 5000, 0),
        ("Tran giat cap 2 lop", 1500, 0, 7800, 5000, 2),
        ("Thong tang, khong tran", 9500, 0, 13000, 5000, -1),
        ("Tran giat cap 1 lop", 13000, 0, 18000, 5000, 1)],
    3: [("Ban cong: tran phang", 0, 0, 1500, 5000, 0),
        ("Tran giat cap 2 lop phong tho", 1500, 0, 7800, 5000, 2),
        ("Thong tang, khong tran", 9500, 0, 13000, 5000, -1),
        ("Tran giat cap 1 lop", 13000, 0, 15600, 5000, 1),
        ("San phoi: khong tran", 15600, 2200, 18000, 5000, -1)],
}
CAO_TRAN = {1: 3.300, 2: 3.100, 3: 3.100}


def draw_ceiling_plan(msp, doc, floor, origin=(0.0, 0.0)):
    """Mat bang tran: ranh vung tran, duong giat cap, den am tran, cao do tran."""
    ox, oy = origin
    _walls_only(msp, ox, oy, floor)
    for ten, x0, y0, x1, y1, cap in CEILING[floor]:
        _zone(msp, doc, ox + x0, oy + y0, ox + x1, oy + y1, ten, cap, floor)


def _zone(msp, doc, x0, y0, x1, y1, ten, cap, floor):
    """Mot vung tran: vien vung, cac cap giat, den, chu thich."""
    msp.add_lwpolyline([(x0 + 80, y0 + 80), (x1 - 80, y0 + 80),
                        (x1 - 80, y1 - 80), (x0 + 80, y1 - 80)],
                       close=True, dxfattribs={"layer": "A-ROOF"})
    for i in range(1, max(cap, 0) + 1):
        d = 80 + 300 * i
        msp.add_lwpolyline([(x0 + d, y0 + d), (x1 - d, y0 + d),
                            (x1 - d, y1 - d), (x0 + d, y1 - d)],
                           close=True, dxfattribs={"layer": "A-ROOF"})
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    if cap >= 0:
        # vung hep (ban cong) chi dat 2 den doc theo chieu dai, tranh chong nhau
        cot = (0,) if (x1 - x0) < 2500 else (-1, 0, 1)
        for dx in cot:
            for dy in (-1, 1):
                mep.place(msp, doc, "DEN-DOWNLIGHT",
                          (cx + dx * (x1 - x0) * 0.25, cy + dy * (y1 - y0) * 0.25), SC)
        an.level_mark(msp, doc, (cx, cy - 700), CAO_TRAN[floor], SC)
    cao, xoay = _co_chu(ten, x1 - x0, y1 - y0, S.th("note", SC))
    lech = (300, 0) if xoay else (0, 300)
    msp.add_text(ten, dxfattribs={
        "layer": "TEXT", "style": "TCVN", "height": cao, "rotation": xoay
    }).set_placement((cx + lech[0], cy + lech[1]), align=TA.MIDDLE_CENTER)


NOTES_LAT_GACH = [
    "Gach lat can mach deu 2mm, chit mach bang keo chuyen dung cung mau gach.",
    "Bat dau lat tu tim phong ra hai ben de vien gach hai canh doi xung.",
    "San WC va ban cong ha 50mm, do doc 1% ve pheu thu, lat gach chong truot.",
    "Gach vien chan tuong cao 100mm cung loai gach san, mach thang hang voi gach san.",
    "Nghiem thu do phang bang thuoc nhom 2m, sai lech khong qua 3mm.",
    "Kiem tra mau va lo gach tren cung mot lo hang truoc khi lat dai tra.",
]

NOTES_TRAN = [
    "Tran thach cao khung xuong chim Vinh Tuong, tam chong am cho WC va bep.",
    "Cao do tran tang 1 la +3.300, tang 2 la +3.100 tinh tu cot san hoan thien.",
    "Giat cap tran cao 300mm moi cap, hoc den am hat sang len tran.",
    "Kiem tra cao do dam va duong ong ky thuat truoc khi treo khung xuong tran.",
    "Ty treo khung xuong buoc 800mm, bat vao san be tong bang tac ke dan no.",
    "Xu ly mach noi bang bang luoi va bot ba chuyen dung truoc khi son.",
]

NOTES_HOAN_THIEN = NOTES_LAT_GACH
