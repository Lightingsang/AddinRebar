# -*- coding: utf-8 -*-
"""Thu vien BLOCK thiet bi ve sinh, bep, noi that, xe.

Khac voi blocks.py, cac block o day co kich thuoc THUC (mm ngoai doi),
nen chen voi ty le 1.0. Goc chen dat tai TAM cua thiet bi.
Kich thuoc lay theo thiet bi pho thong tren thi truong VN.
"""

_BUILDERS = {}
SAN = {"layer": "A-SAN"}
FURN = {"layer": "A-FURN"}
EQ = {"layer": "A-EQUIP"}


def register(name, builder):
    _BUILDERS[name] = builder


def ensure(doc, name):
    if name not in doc.blocks:
        _BUILDERS[name](doc.blocks.new(name=name))
    return name


def place(msp, doc, name, point, rotation=0, layer=None, mirror_x=False):
    """Chen thiet bi tai toa do thuc. rotation tinh bang do."""
    attribs = {"rotation": rotation, "xscale": -1.0 if mirror_x else 1.0}
    if layer:
        attribs["layer"] = layer
    return msp.add_blockref(ensure(doc, name), point, dxfattribs=attribs)


def _rect(blk, w, h, dxf, r=0):
    """Hinh chu nhat tam tai goc, tuy chon bo goc."""
    x, y = w / 2.0, h / 2.0
    if r <= 0:
        blk.add_lwpolyline([(-x, -y), (x, -y), (x, y), (-x, y)], close=True, dxfattribs=dxf)
        return
    pts = [(-x + r, -y, 0, 0, 0.4142), (x - r, -y, 0, 0, 0.4142),
           (x, -y + r, 0, 0, 0), (x, y - r, 0, 0, 0.4142),
           (x - r, y, 0, 0, 0), (-x + r, y, 0, 0, 0.4142),
           (-x, y - r, 0, 0, 0), (-x, -y + r, 0, 0, 0.4142)]
    blk.add_lwpolyline(pts, format="xyseb", close=True, dxfattribs=dxf)


# --- Thiet bi ve sinh -------------------------------------------------------
def _bon_cau(blk):          # bon cau bet 380 x 700
    _rect(blk, 380, 210, SAN)                       # ket nuoc
    blk.add_ellipse((0, -290), major_axis=(0, 250), ratio=0.66, dxfattribs=SAN)
    blk.add_lwpolyline([(-190, -105), (190, -105)], dxfattribs=SAN)


def _lavabo(blk):           # chau rua mat 500 x 420
    _rect(blk, 500, 420, SAN, r=60)
    blk.add_ellipse((0, -20), major_axis=(180, 0), ratio=0.65, dxfattribs=SAN)
    blk.add_circle((0, 170), 22, dxfattribs=SAN)    # voi


def _voi_sen(blk):          # khu tam dung 900 x 900 co pheu thu
    _rect(blk, 900, 900, SAN)
    blk.add_circle((0, 0), 55, dxfattribs=SAN)
    blk.add_line((-450, -450), (450, 450), dxfattribs=SAN)
    blk.add_line((-450, 450), (450, -450), dxfattribs=SAN)


def _bon_tam(blk):          # bon tam nam 1700 x 750
    _rect(blk, 1700, 750, SAN, r=90)
    _rect(blk, 1520, 590, SAN, r=140)
    blk.add_circle((-640, 0), 45, dxfattribs=SAN)


def _pheu_thu(blk):         # phieu thu san 100 x 100
    _rect(blk, 100, 100, SAN)
    blk.add_line((-50, -50), (50, 50), dxfattribs=SAN)
    blk.add_line((-50, 50), (50, -50), dxfattribs=SAN)


# --- Bep --------------------------------------------------------------------
def _bep_nau(blk):          # bep tu / gas 4 vung, 750 x 450
    _rect(blk, 750, 450, EQ)
    for dx in (-190, 190):
        for dy in (-110, 110):
            blk.add_circle((dx, dy), 78, dxfattribs=EQ)


def _chau_rua_chen(blk):    # chau rua chen doi 800 x 450
    _rect(blk, 800, 450, EQ)
    for dx in (-190, 190):
        _rect_at(blk, dx, -20, 320, 320, EQ, 30)
    blk.add_circle((0, 180), 25, dxfattribs=EQ)


def _rect_at(blk, cx, cy, w, h, dxf, r=0):
    x, y = w / 2.0, h / 2.0
    blk.add_lwpolyline(
        [(cx - x, cy - y), (cx + x, cy - y), (cx + x, cy + y), (cx - x, cy + y)],
        close=True, dxfattribs=dxf)


def _tu_lanh(blk):
    _rect(blk, 700, 700, EQ)
    blk.add_line((-350, 100), (350, 100), dxfattribs=EQ)


# --- Noi that ---------------------------------------------------------------
def _giuong_doi(blk):       # giuong 1m6 x 2m0 + 2 goi + 2 tu dau giuong
    _rect(blk, 1600, 2000, FURN)
    _rect_at(blk, 0, 620, 1500, 700, FURN)          # chan ga gap
    for dx in (-380, 380):
        _rect_at(blk, dx, 830, 620, 300, FURN)      # goi
    for dx in (-1000, 1000):
        _rect_at(blk, dx, 900, 400, 400, FURN)      # tu dau giuong


def _giuong_don(blk):       # giuong 1m2 x 2m0
    _rect(blk, 1200, 2000, FURN)
    _rect_at(blk, 0, 620, 1120, 700, FURN)
    _rect_at(blk, 0, 830, 620, 300, FURN)


def _sofa(blk):             # sofa bang 2m0 x 0m85
    _rect(blk, 2000, 850, FURN, r=60)
    for dx in (-620, 0, 620):
        _rect_at(blk, dx, -60, 560, 560, FURN)
    _rect_at(blk, 0, 330, 2000, 190, FURN)


def _ban_tra(blk):
    _rect(blk, 1100, 600, FURN, r=40)


def _ban_an_6(blk):         # ban an 6 ghe 1m6 x 0m9
    _rect(blk, 1600, 900, FURN, r=50)
    for dx in (-500, 0, 500):
        for dy in (-620, 620):
            blk.add_circle((dx, dy), 210, dxfattribs=FURN)


def _tu_quan_ao(blk):       # tu ao 1m8 sau 0m6
    _rect(blk, 1800, 600, FURN)
    blk.add_line((0, -300), (0, 300), dxfattribs=FURN)


def _tivi(blk):
    _rect(blk, 1400, 120, FURN)
    _rect_at(blk, 0, -220, 1600, 400, FURN)


# --- Xe ---------------------------------------------------------------------
def _xe_oto(blk):           # o to 7 cho 4m7 x 1m8
    _rect(blk, 1800, 4700, FURN, r=260)
    _rect_at(blk, 0, 500, 1560, 1500, FURN)         # cabin
    for dx in (-900, 900):
        for dy in (-1500, 1400):
            _rect_at(blk, dx, dy, 180, 620, FURN)   # banh


def _xe_may(blk):           # xe may 1m9 x 0m7
    _rect(blk, 700, 1900, FURN, r=150)
    blk.add_circle((0, 700), 160, dxfattribs=FURN)
    blk.add_circle((0, -700), 160, dxfattribs=FURN)


for _n, _f in [
    ("BON-CAU", _bon_cau), ("LAVABO", _lavabo), ("VOI-SEN", _voi_sen),
    ("BON-TAM", _bon_tam), ("PHEU-THU", _pheu_thu),
    ("BEP-NAU", _bep_nau), ("CHAU-RUA-CHEN", _chau_rua_chen), ("TU-LANH", _tu_lanh),
    ("GIUONG-DOI", _giuong_doi), ("GIUONG-DON", _giuong_don),
    ("SOFA", _sofa), ("BAN-TRA", _ban_tra), ("BAN-AN-6", _ban_an_6),
    ("TU-QUAN-AO", _tu_quan_ao), ("TIVI", _tivi),
    ("XE-OTO", _xe_oto), ("XE-MAY", _xe_may),
]:
    register(_n, _f)
