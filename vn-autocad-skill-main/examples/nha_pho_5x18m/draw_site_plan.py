# -*- coding: utf-8 -*-
"""Tong mat bang dinh vi lo dat, ty le 1/200.

To nay tra loi mot cau duy nhat: cong trinh nam o dau tren lo dat va cach ranh
bao nhieu. Vi vay chi ve duong bao, ranh gioi va kich thuoc dinh vi; khong ve
tuong ngan hay noi that, de nhin la thay ngay quan he nha voi duong.
"""

from ezdxf.enums import TextEntityAlignment as TA

from vncad import annotate as an, blocks, standards as S

SC = "1/200"

# Lo dat 5,0m x 20,0m; nha chiem 18,0m chieu sau, con 2,0m lam san sau.
LO_D, LO_R = 20000.0, 5000.0
NHA_D = 18000.0
VIA_HE = 3000.0
# Chi ve 5,0m long duong roi danh net gay: ve du 8,0m thi cum hinh rong hon
# mot o luoi trong model space. Be rong that ghi bang chu ngay tren long duong.
LONG_DUONG_VE = 5000.0
LONG_DUONG_THAT = 8000.0
LAN_CAN = 6000.0                 # be sau lo lan can ve duoc trong pham vi to

COT_VIA_HE = 0.000
COT_NEN = 0.150
COT_SAN_SAU = 0.100


def _chu(msp, txt, pt, kind="note", canh=TA.MIDDLE_CENTER, layer="TEXT", xoay=0.0):
    msp.add_text(txt, dxfattribs={"layer": layer, "style": "TCVN",
                                  "height": S.th(kind, SC),
                                  "rotation": xoay}).set_placement(pt, align=canh)


def _hop(msp, x0, y0, x1, y1, layer, lw=25, kin=True):
    msp.add_lwpolyline([(x0, y0), (x1, y0), (x1, y1), (x0, y1)],
                       close=kin, dxfattribs={"layer": layer, "lineweight": lw})


def draw_site_plan(msp, doc, origin=(0.0, 0.0)):
    """Tong mat bang: duong, via he, ranh lo, bao nha, san sau, nha lan can."""
    ox, oy = origin
    _duong(msp, ox, oy)
    _lan_can(msp, ox, oy)
    _lo_dat(msp, doc, ox, oy)
    _kich_thuoc(msp, doc, ox, oy)
    blocks.insert(msp, doc, "HUONG-BAC", (ox + LO_D + 1500, oy + LO_R + 3200), SC)
    # tieu de dat DUOI lo lan can: dat o -5600 la nam lot trong o nha ke ben
    an.view_title(msp, (ox + LO_D / 2 - 2000, oy - LAN_CAN - 1500),
                  "Tong mat bang dinh vi", "1/200", SC)


def _duong(msp, ox, oy):
    """Long duong, via he va tim duong chay doc truoc mat lo."""
    y0, y1 = oy - LAN_CAN - 1500, oy + LO_R + LAN_CAN + 1500
    msp.add_line((ox, y0), (ox, y1),
                 dxfattribs={"layer": "A-ROAD", "lineweight": 20})
    msp.add_line((ox - VIA_HE, y0), (ox - VIA_HE, y1),
                 dxfattribs={"layer": "A-ROAD", "lineweight": 35})
    x_gay = ox - VIA_HE - LONG_DUONG_VE
    for y in (y0, y1):                       # net gay: long duong con keo dai ra
        _net_gay(msp, x_gay, y)
    msp.add_line((x_gay, y0), (x_gay, y1),
                 dxfattribs={"layer": "A-ROAD", "linetype": "DASHED"})
    _chu(msp, "DUONG BE TONG NHUA RONG %0.1fm" % (LONG_DUONG_THAT / 1000),
         (x_gay + LONG_DUONG_VE / 2, oy + LO_R / 2), "room", xoay=90.0)
    _chu(msp, "VIA HE 3,0m", (ox - VIA_HE / 2, oy + 1300), xoay=90.0)


def _net_gay(msp, x, y):
    """Ky hieu net gay hinh chu Z: bao rang long duong con keo dai ra ngoai."""
    msp.add_lwpolyline([(x + 400, y), (x + 100, y - 300), (x - 100, y + 300),
                        (x - 400, y)],
                       dxfattribs={"layer": "A-ROAD", "lineweight": 18})


def _lan_can(msp, ox, oy):
    """Hai lo ke ben ve net manh, chi de thay lo dang xet nam giua day pho."""
    # Nhan lo duoi day xuong sat mep xa: giua lo la cho hai tang kich thuoc
    # 18000 va 20000 chay ngang qua, dat o giua la chu nam de len so do.
    for y0, y1, y_chu in ((oy - LAN_CAN, oy, oy - LAN_CAN + 1400),
                          (oy + LO_R, oy + LO_R + LAN_CAN, oy + LO_R + LAN_CAN / 2)):
        _hop(msp, ox, y0, ox + NHA_D, y1, "A-FURN", 13)
        _chu(msp, "NHA LAN CAN", (ox + NHA_D / 2, y_chu), "room")


def _lo_dat(msp, doc, ox, oy):
    """Ranh lo, bao nha ba tang, san sau va cot cao do."""
    _hop(msp, ox, oy, ox + LO_D, oy + LO_R, "A-SITE", 50)
    _hop(msp, ox, oy, ox + NHA_D, oy + LO_R, "A-WALL", 60)
    h = msp.add_hatch(color=9, dxfattribs={"layer": "A-WALL-HATCH"})
    h.set_pattern_fill("ANSI31", scale=30)
    h.paths.add_polyline_path([(ox, oy), (ox + NHA_D, oy),
                               (ox + NHA_D, oy + LO_R), (ox, oy + LO_R)],
                              is_closed=True)
    _chu(msp, "NHA O 3 TANG", (ox + NHA_D / 2, oy + LO_R / 2 + 600), "room")
    _chu(msp, "DTXD 90,0 m2", (ox + NHA_D / 2, oy + LO_R / 2 - 300))
    _chu(msp, "SAN SAU", (ox + NHA_D + 1000, oy + LO_R / 2 + 900), xoay=90.0)
    an.level_mark(msp, doc, (ox + NHA_D + 1300, oy + 1000), COT_SAN_SAU, SC)
    an.level_mark(msp, doc, (ox + NHA_D / 2 - 2600, oy + 1300), COT_NEN, SC)
    an.level_mark(msp, doc, (ox - VIA_HE / 2 + 600, oy + 1300), COT_VIA_HE, SC)
    _cong(msp, ox, oy)


def _cong(msp, ox, oy):
    """Loi xe vao gara o mat tien: khoang mo tren ranh va mui ten huong vao."""
    msp.add_line((ox, oy + 700), (ox, oy + 4300),
                 dxfattribs={"layer": "A-DOOR", "lineweight": 50})
    msp.add_line((ox - 2800, oy + 3400), (ox - 200, oy + 3400),
                 dxfattribs={"layer": "A-SYMBOL"})
    for dy in (-260.0, 260.0):
        msp.add_line((ox - 200, oy + 3400), (ox - 700, oy + 3400 + dy),
                     dxfattribs={"layer": "A-SYMBOL"})
    # Ghi ngan gon vi be rong via he chi 3,0m, ghi dai la chu tran qua ranh lo
    _chu(msp, "Loi xe vao", (ox - 300, oy + 4100), canh=TA.MIDDLE_RIGHT)


def _kich_thuoc(msp, doc, ox, oy):
    """Kich thuoc dinh vi: chieu sau nha, san sau, be rong lo va be rong via he."""
    an.dim_chain(msp, doc, [ox - VIA_HE, ox, ox + NHA_D, ox + LO_D], oy, "h",
                 SC, side=-1)
    # tang ngoai la chieu sau LO DAT tu ranh truoc toi ranh sau, khong tinh via he
    an.dim_chain(msp, doc, [ox, ox + LO_D], oy, "h", SC, side=-1,
                 offset_mm=S.DIM_FIRST_OFFSET + S.DIM_TIER_GAP)
    an.dim_chain(msp, doc, [oy, oy + LO_R], ox + LO_D, "v", SC, side=1)


NOTES_TONG_MB = [
    "Kich thuoc dinh vi lay tu ranh lo, doi chieu ban do dia chinh truoc khi dinh vi.",
    "Cot via he lay lam moc 0.000; nen tang tret +0.150, san sau +0.100 doc ra ho ga.",
    "Nha xay sat ranh hai ben theo hien trang day pho, khong co khoang lui hong.",
    "Truoc khi dao mong phai chong do bao ve hai nha lan can va chup anh hien trang.",
    "Nuoc mua san sau va nuoc thai sau be tu hoai deu dau vao cong chung tren via he.",
    "Cham moc va cang day dinh vi hai truc bien truoc, chu dau tu nghiem thu roi moi dao.",
]

# Bang chi tieu quy hoach, ve thang tren giay o Layout
CHI_TIEU = [
    ["Dien tich lo dat", "5,0 x 20,0 = 100,0 m2"],
    ["Dien tich xay dung", "5,0 x 18,0 = 90,0 m2"],
    ["Mat do xay dung", "90 %"],
    ["Tong dien tich san", "90,0 x 3 = 270,0 m2"],
    ["He so su dung dat", "2,7 lan"],
    ["So tang", "3 tang, co san thuong va mai che thang"],
    ["Chieu cao", "10,6m den san thuong; 13,4m den dinh mai che thang"],
]
