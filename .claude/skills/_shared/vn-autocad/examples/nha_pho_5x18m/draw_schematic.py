# -*- coding: utf-8 -*-
"""So do nguyen ly: tu dien va he cap thoat nuoc.

So do nguyen ly khong ve theo ty le hinh hoc, chi the hien quan he dau noi.
Ve o kich thuoc phu hop de xem o 1/50 cho chu doc duoc.
"""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, mep, standards as S

# So do tu dien cao gan 16m khi ve o kich thuoc doc duoc, khong vua kho A3
# o ty le 1/50. Ve o 1/75 thi vua chieu cao ma chu van cao 2mm tren giay.
SC = "1/75"
H_CHU = S.th("note", SC)

# Cac nhanh cua tu dien: (ten nhanh, dong dinh muc, tiet dien day)
NHANH = [
    ("Chieu sang tang 1", "MCB 10A", "2x1.5"),
    ("Chieu sang tang 2 va 3", "MCB 10A", "2x1.5"),
    ("O cam tang 1", "RCBO 20A/30mA", "2x2.5"),
    ("O cam tang 2", "RCBO 20A/30mA", "2x2.5"),
    ("O cam tang 3", "RCBO 20A/30mA", "2x2.5"),
    ("Dieu hoa 4 cuc", "MCB 20A", "2x4.0"),
    ("Binh nong lanh 3 WC", "RCBO 20A/30mA", "2x2.5"),
    ("Bep tu va may hut mui", "MCB 25A", "2x4.0"),
]


def _mcb(msp, x, y, w=1300.0, h=600.0):
    """Ky hieu aptomat: hop chu nhat co gach cheo."""
    msp.add_lwpolyline([(x, y), (x + w, y), (x + w, y + h), (x, y + h)],
                       close=True, dxfattribs={"layer": "M-ELEC"})
    msp.add_line((x + 180, y + 120), (x + w - 180, y + h - 120),
                 dxfattribs={"layer": "M-ELEC"})


def _chu(msp, text, pt, cao=None, canh=TA.MIDDLE_LEFT, layer="M-TEXT"):
    msp.add_text(text, dxfattribs={"layer": layer, "style": "TCVN",
                                   "height": cao or H_CHU}).set_placement(pt, align=canh)


def so_do_tu_dien(msp, doc, origin=(0.0, 0.0)):
    """So do nguyen ly tu dien tong: nguon vao, aptomat tong, cac nhanh."""
    ox, oy = origin
    buoc = 2000.0
    cao = buoc * len(NHANH)
    x_truc = ox + 6000

    _chu(msp, "Nguon 1 pha 220V tu dien luc", (ox, oy + cao + 2900))
    msp.add_line((ox, oy + cao + 1400), (x_truc, oy + cao + 1400),
                 dxfattribs={"layer": "M-ELEC"})
    _mcb(msp, ox + 2200, oy + cao + 1100)
    _chu(msp, "MCB tong 63A 2P", (ox + 4200, oy + cao + 2000))
    _chu(msp, "Chong giat tong 63A/100mA", (ox + 2200, oy + cao + 700))
    msp.add_line((x_truc, oy), (x_truc, oy + cao + 1400),
                 dxfattribs={"layer": "M-ELEC", "lineweight": 35})

    for i, (ten, dong, tiet_dien) in enumerate(NHANH):
        y = oy + cao - i * buoc
        msp.add_line((x_truc, y), (x_truc + 7400, y), dxfattribs={"layer": "M-ELEC"})
        _mcb(msp, x_truc + 900, y - 300)
        _chu(msp, dong, (x_truc + 2600, y + 380))
        _chu(msp, "%s  (day %smm2)" % (ten, tiet_dien), (x_truc + 2600, y - 430))
    _tiep_dia(msp, ox, oy, x_truc, cao)
    an.view_title(msp, (ox + 7600, oy - 2600), "So do nguyen ly tu dien tong",
                  "khong ty le", SC)


def _tiep_dia(msp, ox, oy, x_truc, cao):
    """Coc tiep dia noi vao thanh trung tinh cua tu."""
    y = oy - 900
    msp.add_line((x_truc, oy), (x_truc, y), dxfattribs={"layer": "M-ELEC"})
    for i, w in enumerate((1000.0, 660.0, 340.0)):
        msp.add_line((x_truc - w / 2, y - i * 230), (x_truc + w / 2, y - i * 230),
                     dxfattribs={"layer": "M-ELEC"})
    _chu(msp, "Coc tiep dia dong D16 dai 2.4m, dien tro nho hon 4 Ohm",
         (x_truc + 1300, y - 230))


# ---------------------------------------------------------------------------
# SO DO KHONG GIAN CAP THOAT NUOC
# ---------------------------------------------------------------------------
# So do nguyen ly nen buoc tang lay vua du de doc, khong theo cao do that
TANG = [("Tang 1  cot +0.150", 0.0), ("Tang 2  cot +3.800", 3600.0),
        ("Tang 3  cot +7.200", 7200.0)]


def so_do_nuoc(msp, doc, origin=(0.0, 0.0)):
    """So do khong gian: ong dung cap va thoat chay suot ba tang."""
    ox, oy = origin
    x_cap, x_thoat = ox + 2600, ox + 7200
    _truc_dung(msp, x_cap, oy - 1200, oy + 9000, "Ong dung cap PPR D32", "M-WATER")
    _truc_dung(msp, x_thoat, oy - 2400, oy + 9000, "Ong dung thoat PVC D114", "M-DRAIN")
    for ten, dy in TANG:
        y = oy + dy
        _san_tang(msp, doc, ox, y, ten, x_cap, x_thoat)
    _be_phot(msp, doc, ox, oy, x_cap, x_thoat)
    an.view_title(msp, (ox + 5200, oy - 4600), "So do khong gian cap thoat nuoc",
                  "khong ty le", SC)


def _truc_dung(msp, x, y0, y1, ten, layer):
    msp.add_line((x, y0), (x, y1), dxfattribs={"layer": layer, "lineweight": 35})
    _chu(msp, ten, (x - 200, y1 + 500), canh=TA.BOTTOM_LEFT)


def _san_tang(msp, doc, ox, y, ten, x_cap, x_thoat):
    """Mot tang: duong san, nhanh cap vao thiet bi, nhanh thoat ve ong dung."""
    msp.add_line((ox, y), (ox + 10400, y),
                 dxfattribs={"layer": "M-WALL", "lineweight": 25})
    _chu(msp, ten, (ox - 200, y + 260), canh=TA.MIDDLE_RIGHT)
    msp.add_lwpolyline([(x_cap, y + 900), (ox + 9400, y + 900)],
                       dxfattribs={"layer": "M-WATER"})
    mep.place(msp, doc, "VAN-KHOA", (x_cap + 900, y + 900), SC)
    _chu(msp, "Nhanh cap D20 vao WC va bep", (x_cap + 1600, y + 1200))
    msp.add_lwpolyline([(ox + 9400, y + 400), (x_thoat, y + 400)],
                       dxfattribs={"layer": "M-DRAIN"})
    _chu(msp, "Thoat D114 va D60, doc 2%", (x_thoat + 400, y + 620))


def _be_phot(msp, doc, ox, oy, x_cap, x_thoat):
    """Be tu hoai ba ngan va ho ga o day tuyen thoat."""
    y = oy - 3600
    msp.add_lwpolyline([(ox + 5200, y), (ox + 9600, y), (ox + 9600, y + 1200),
                        (ox + 5200, y + 1200)], close=True,
                       dxfattribs={"layer": "M-DRAIN", "lineweight": 35})
    for dx in (6600.0, 8000.0):
        msp.add_line((ox + dx, y), (ox + dx, y + 1200), dxfattribs={"layer": "M-DRAIN"})
    _chu(msp, "Be tu hoai 3 ngan 3.0m3", (ox + 5000, y + 1600))
    msp.add_line((x_thoat, oy - 2400), (x_thoat, y + 1200),
                 dxfattribs={"layer": "M-DRAIN"})
    mep.place(msp, doc, "HO-GA", (ox + 10200, y + 600), SC)
    msp.add_line((ox + 9600, y + 600), (ox + 10000, y + 600),
                 dxfattribs={"layer": "M-DRAIN"})
    _chu(msp, "Ho ga lang cat roi ra cong chung", (ox + 10600, y + 600))
    mep.place(msp, doc, "DONG-HO-NUOC", (x_cap, oy - 1600), SC)
    _chu(msp, "Dong ho nuoc va van tong", (x_cap + 700, oy - 1600))


NOTES_SO_DO = [
    "So do nguyen ly khong ve theo ty le, chi the hien quan he dau noi.",
    "Moi nhanh o cam va binh nong lanh deu qua aptomat chong giat 30mA.",
    "Day nguon vao tu dien 2x6.0mm2, aptomat tong 63A hai cuc.",
    "Ong dung cap D32 va ong dung thoat D114 dat trong hop ky thuat sat WC.",
    "Ong thong hoi be tu hoai D60 dan len qua mai, cao hon mai 700mm.",
    "Truoc khi do be tong san phai dat san ong cho xuyen san dung vi tri.",
]
