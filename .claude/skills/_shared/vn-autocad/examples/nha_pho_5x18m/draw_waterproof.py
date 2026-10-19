# -*- coding: utf-8 -*-
"""Chi tiet cau tao chong tham, ty le 1/20.

Ba nut hay ro nuoc nhat trong nha o Viet Nam: chan tuong san ve sinh,
senô va tuong chan mai, chan lan can ban cong.

O ty le nay chu ghi chu chiem cho hon ca hinh, nen dung cach cua ho so that:
danh SO LOP tren hinh, con dien giai dat trong bang chu giai ben canh.
"""

from vncad import annotate as an

# Ba chi tiet nay khong vua mot to A3 o ty le 1/10 vi vong tron danh so va
# tieu de chiem cho. Ve o 1/20, van doc ro tung lop nho co bang chu giai.
SC = "1/20"
T_SAN = 100.0            # san BTCT
T_TUONG = 110.0          # tuong xay ngan
R_SO = 70.0              # ban kinh vong tron danh so lop


def _lop(msp, pts, hatch=None, pscale=8.0, layer="A-ELEV", lw=25):
    """Mot lop vat lieu: duong bao + hoa van neu co."""
    msp.add_lwpolyline(pts, close=True, dxfattribs={"layer": layer, "lineweight": lw})
    if hatch:
        h = msp.add_hatch(color=8, dxfattribs={"layer": "A-MAT"})
        h.set_pattern_fill(hatch, scale=pscale)
        h.paths.add_polyline_path(pts, is_closed=True)


def _hop(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def _mang(msp, pts):
    """Mang chong tham ve net rat dam de noi bat trong ban ve."""
    msp.add_lwpolyline(pts, dxfattribs={"layer": "A-ROOF", "lineweight": 60})


def _so(msp, n, pt, chi_toi=None):
    """Vong tron danh so lop, dung chung mot kieu voi cac to chi tiet khac."""
    an.so_lop(msp, n, pt, chi_toi, R_SO, SC)


# ---------------------------------------------------------------------------
# CT1: CHAN TUONG SAN VE SINH
# ---------------------------------------------------------------------------
CHU_GIAI_WC = [
    ("1", "San BTCT day 100, be tong B20"),
    ("2", "Chong tham Sika ba lop, vat len chan tuong 300"),
    ("3", "Vua tao doc 1% day 30 den 50"),
    ("4", "Gach lat chong truot 300x300"),
    ("5", "Tuong xay 110 gach tuynel"),
    ("6", "Gach op tuong 300x600 kich tran"),
]


def chi_tiet_wc(msp, doc, origin=(0.0, 0.0)):
    """Chong tham san WC vat len chan tuong 300mm."""
    ox, oy = origin
    _lop(msp, _hop(ox, oy, ox + 900, oy + T_SAN), "AR-CONC", 1.2)
    _lop(msp, _hop(ox, oy + T_SAN, ox + 900, oy + T_SAN + 40), "ANSI31", 3)
    _lop(msp, _hop(ox, oy + T_SAN + 40, ox + 900, oy + T_SAN + 50))
    _lop(msp, _hop(ox, oy, ox + T_TUONG, oy + 700), "ANSI31", 6)
    _lop(msp, _hop(ox + T_TUONG, oy + T_SAN + 50, ox + T_TUONG + 10, oy + 700))
    _mang(msp, [(ox + 900, oy + T_SAN), (ox + T_TUONG, oy + T_SAN),
                (ox + T_TUONG, oy + T_SAN + 300)])
    for n, sx, sy, tx, ty in ((1, 700, -280, 600, 40), (2, 480, 560, 130, 330),
                              (3, 940, 300, 700, 130), (4, 940, 480, 700, 155),
                              (5, -300, 560, 40, 500), (6, 480, 760, 120, 620)):
        _so(msp, n, (ox + sx, oy + sy), (ox + tx, oy + ty))
    an.view_title(msp, (ox + 400, oy - 620), "CT1 - Chan tuong san WC", "1/20", SC)


# ---------------------------------------------------------------------------
# CT2: SENO VA TUONG CHAN MAI
# ---------------------------------------------------------------------------
CHU_GIAI_SENO = [
    ("1", "San mai BTCT day 100"),
    ("2", "Chong tham Sika ba lop phu kin long senô"),
    ("3", "Senô rong 300 sau 120, danh doc ve pheu thu"),
    ("4", "Tuong chan mai xay 110 cao 900"),
    ("5", "Go mu be tong bao dinh tuong chan mai"),
]


def chi_tiet_seno(msp, doc, origin=(0.0, 0.0)):
    """Senô thu nuoc mua sat chan tuong chan mai."""
    ox, oy = origin
    _lop(msp, _hop(ox, oy, ox + 700, oy + T_SAN), "AR-CONC", 1.2)
    _lop(msp, _hop(ox + 700, oy - 120, ox + 1000, oy + T_SAN), "AR-CONC", 1.2)
    _lop(msp, _hop(ox + 1000, oy, ox + 1110, oy + 800), "ANSI31", 6)
    _lop(msp, _hop(ox + 960, oy + 800, ox + 1160, oy + 860))
    _lop(msp, _hop(ox, oy + T_SAN, ox + 700, oy + T_SAN + 40), "ANSI31", 3)
    _mang(msp, [(ox, oy + T_SAN), (ox + 700, oy + T_SAN), (ox + 700, oy - 120),
                (ox + 1000, oy - 120), (ox + 1000, oy + 400)])
    for n, sx, sy, tx, ty in ((1, 300, -320, 350, 40), (2, 620, 420, 700, 200),
                              (3, 850, -350, 850, -120), (4, 1290, 500, 1105, 500),
                              (5, 1290, 860, 1160, 830)):
        _so(msp, n, (ox + sx, oy + sy), (ox + tx, oy + ty))
    an.view_title(msp, (ox + 500, oy - 620), "CT2 - Senô va tuong chan mai",
                  "1/20", SC)


# ---------------------------------------------------------------------------
# CT3: CHAN LAN CAN BAN CONG
# ---------------------------------------------------------------------------
CHU_GIAI_BAN_CONG = [
    ("1", "Ban BTCT ban cong day 100"),
    ("2", "Go chan nuoc do lien khoi voi ban san, cao 150"),
    ("3", "Chong tham vat len het go chan nuoc"),
    ("4", "Kinh cuong luc 12mm, pat inox 304 bat hong"),
    ("5", "Chi ngat nuoc mep duoi ban cong"),
]


def chi_tiet_ban_cong(msp, doc, origin=(0.0, 0.0)):
    """Chan lan can kinh tren ban cong, co go chan nuoc va chi ngat nuoc."""
    ox, oy = origin
    _lop(msp, _hop(ox, oy, ox + 900, oy + T_SAN), "AR-CONC", 1.2)
    _lop(msp, _hop(ox + 700, oy + T_SAN, ox + 900, oy + T_SAN + 150), "AR-CONC", 1.2)
    _lop(msp, _hop(ox, oy + T_SAN, ox + 700, oy + T_SAN + 40), "ANSI31", 3)
    _lop(msp, _hop(ox, oy + T_SAN + 40, ox + 700, oy + T_SAN + 50))
    _mang(msp, [(ox, oy + T_SAN), (ox + 700, oy + T_SAN), (ox + 700, oy + T_SAN + 150)])
    _lop(msp, _hop(ox + 780, oy + T_SAN + 150, ox + 820, oy + 900),
         layer="A-RAILING", lw=35)
    msp.add_line((ox + 900, oy), (ox + 900, oy - 60),
                 dxfattribs={"layer": "A-ELEV", "lineweight": 35})
    for n, sx, sy, tx, ty in ((1, 300, -320, 350, 40), (2, 1120, 300, 900, 180),
                              (3, 400, 420, 500, 110), (4, 1120, 700, 820, 640),
                              (5, 1120, -320, 900, -40)):
        _so(msp, n, (ox + sx, oy + sy), (ox + tx, oy + ty))
    an.view_title(msp, (ox + 450, oy - 620), "CT3 - Chan lan can ban cong",
                  "1/20", SC)


NOTES_CHONG_THAM = [
    "Chong tham Sika Top Seal 107 hoac tuong duong, quet ba lop vuong goc nhau.",
    "Truoc khi quet phai ve sinh sach, bao hoa nuoc be mat, khong de dong nuoc.",
    "Chong tham vat len chan tuong toi thieu 300mm doi voi WC va senô.",
    "Ngam thu nuoc 24 gio truoc khi lang vua tao doc va lat gach.",
    "Mep duoi ban cong va senô phai co chi ngat nuoc rong 10mm sau 10mm.",
    "Vi tri ong xuyen san phai quet chong tham quanh co ong ban kinh 200mm.",
]
