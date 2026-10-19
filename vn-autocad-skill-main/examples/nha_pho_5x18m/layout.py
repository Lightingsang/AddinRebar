# -*- coding: utf-8 -*-
"""Du lieu mat bang nha pho 5.0 x 18.0m, 3 tang, TP.HCM.

Truc X chay theo chieu sau nha (mat tien tai x=0), truc Y theo chieu ngang.
Moi tuong khai bao kem cac lo mo, nen ham ve tu dong ngat net tai lo cua.
"""

W, L = 5000.0, 18000.0            # ngang x sau
AXIS_X = [("1", 0.0), ("2", 5500.0), ("3", 9500.0), ("4", 13000.0), ("5", 18000.0)]
AXIS_Y = [("A", 0.0), ("B", 5000.0)]
FLOOR_LEVEL = {1: 0.150, 2: 3.800, 3: 7.200}
STOREY_H = 3650.0

# Cao do dung dung cho mat dung va mat cat (met)
LEVELS = [("TRET", 0.150), ("LAU 1", 3.800), ("LAU 2", 7.200),
          ("SAN THUONG", 10.600), ("MAI CHE THANG", 13.400)]
Z_MONG = -1.450        # day dai mong
Z_DAT = -0.050         # coc san hoan thien via he

# Cot BTCT tai giao truc: (x, y, (rong, cao))
COLUMNS = [(x, y, (220, 220)) for _, x in AXIS_X for _, y in AXIS_Y]

# --- Tuong: (p1, p2, day, [(vi_tri_tu_p1, be_rong, loai)]) ------------------
# loai: 'door' cua 1 canh | 'door2' cua 2 canh | 'wind' cua so | 'open' thong phong
FLOOR_WALLS = {
    1: [
        ((0, 0), (18000, 0), 220, [(15600, 1600, "wind")]),                 # tuong hong phai
        ((0, 5000), (18000, 5000), 220, [(6200, 1800, "wind")]),            # tuong hong trai
        ((0, 0), (0, 5000), 220, [(700, 3600, "open")]),                    # mat tien: cua gara
        ((18000, 0), (18000, 5000), 220, [(1800, 900, "door")]),            # tuong hau ra san
        ((5500, 0), (5500, 5000), 110, [(1400, 2200, "open")]),             # gara / khach
        ((9500, 0), (9500, 1900), 110, [(500, 800, "door")]),               # vach WC1
        ((9500, 1900), (11400, 1900), 110, []),                             # vach WC1 ngang
        ((13000, 0), (13000, 5000), 110, [(2000, 2600, "open")]),           # khach / bep
    ],
    2: [
        ((0, 0), (18000, 0), 220, [(4200, 1600, "wind")]),
        ((0, 5000), (18000, 5000), 220, [(4200, 1600, "wind")]),
        ((0, 0), (0, 5000), 220, [(1200, 2600, "wind")]),                   # ban cong
        ((18000, 0), (18000, 5000), 220, [(1500, 1600, "wind")]),
        ((1500, 0), (1500, 5000), 220, [(2100, 900, "door")]),              # vach ra ban cong
        ((7800, 0), (7800, 5000), 110, [(2000, 900, "door")]),              # ngu 1 / sanh
        ((13000, 0), (13000, 5000), 110, [(2000, 900, "door")]),            # sanh / ngu 2
        ((10500, 0), (10500, 2400), 110, [(900, 800, "door")]),             # WC2
        ((10500, 2400), (13000, 2400), 110, []),
    ],
    3: [
        ((0, 0), (18000, 0), 220, [(4200, 1600, "wind")]),
        ((0, 5000), (18000, 5000), 220, [(4200, 1600, "wind")]),
        ((0, 0), (0, 5000), 220, [(1200, 2600, "wind")]),
        ((18000, 0), (18000, 5000), 220, [(1600, 1400, "wind")]),
        ((1500, 0), (1500, 5000), 220, [(2100, 900, "door")]),
        ((7800, 0), (7800, 5000), 110, [(1900, 1600, "door2")]),            # phong tho
        ((13000, 0), (13000, 5000), 110, [(2000, 900, "door")]),
        ((15600, 0), (15600, 2200), 110, [(700, 800, "door")]),             # WC3 phia sau
        ((15600, 2200), (18000, 2200), 110, []),
    ],
}

# --- Phong: (ten, tam_nhan, dien_tich_m2) ----------------------------------
# Moi phong: (ten, tam_nhan, dien_tich_m2, goc_xoay_nhan).
# Goc xoay 90 danh cho phong hep va dai: ban cong rong 1,5m ma dong chu
# "BAN CONG" dai gan 1,9m nen ghi ngang la chu tran ca sang hai ben tuong.
FLOOR_ROOMS = {
    1: [("Gara o to", (2750, 2500), 26.4, 0),
        ("Phong khach", (7500, 3300), 19.2, 0),
        ("Gieng troi + cau thang", (11250, 3600), None, 0),
        # nhan lui len goc tren ben phai de tranh canh cua WC1 quet qua
        ("WC 1", (10600, 1700), 3.0, 0),
        ("Bep + phong an", (15500, 2500), 24.0, 0)],
    2: [("Ban cong", (750, 2500), 3.6, 90),
        ("Phong ngu master 1", (4650, 2500), 30.5, 0),
        ("Sanh + thong tang", (9150, 3600), None, 0),
        ("WC 2", (11750, 2100), 5.6, 0),
        ("Phong ngu master 2", (15500, 3500), 24.0, 0)],
    3: [("Ban cong", (750, 2500), 3.6, 90),
        ("Phong tho gia tien", (4650, 2500), 30.5, 0),
        ("Sanh + thong tang", (9150, 3600), None, 0),
        ("Phong ngu master 3", (14400, 3500), 17.0, 0),
        ("WC 3", (16800, 1900), 5.3, 0),
        ("San phoi", (16850, 4100), None, 0)],
}

# --- Thiet bi / noi that: (ten_block, x, y, goc_xoay) ----------------------
FLOOR_FIXTURES = {
    1: [("XE-OTO", 2600, 2500, 90), ("XE-MAY", 4600, 900, 90),
        ("SOFA", 7300, 4400, 180), ("BAN-TRA", 7300, 3300, 0), ("TIVI", 7300, 700, 0),
        ("BON-CAU", 10100, 800, 90), ("LAVABO", 11000, 500, 0),
        ("BEP-NAU", 14200, 4600, 180), ("CHAU-RUA-CHEN", 15400, 4600, 180),
        ("TU-LANH", 13600, 4600, 180), ("BAN-AN-6", 15900, 2200, 90)],
    2: [("GIUONG-DOI", 4200, 2500, 90), ("TU-QUAN-AO", 6900, 2500, 90),
        ("BON-CAU", 11000, 700, 90), ("LAVABO", 12400, 500, 0),
        ("VOI-SEN", 11200, 1800, 0), ("PHEU-THU", 12000, 1500, 0),
        ("GIUONG-DOI", 15200, 2500, 90), ("TU-QUAN-AO", 17400, 2500, 90),
        ("TIVI", 13400, 2500, 90)],
    3: [("GIUONG-DOI", 14000, 3900, 0), ("TU-QUAN-AO", 13400, 2600, 90),
        ("BON-CAU", 16300, 700, 90), ("LAVABO", 17300, 500, 0),
        ("VOI-SEN", 16600, 1600, 0), ("PHEU-THU", 17400, 1400, 0)],
}

# --- Cau thang: goc dat, huong, so bac, be rong -----------------------------
STAIR = {"origin": (9700, 4700), "dir": (1, 0), "width": 1000,
         "tread": 270, "steps": 10}

# --- Ghi chu ky thuat (lay tu ho so thi cong that) --------------------------
NOTES = [
    "Kich thuoc ghi bang milimet (mm), cao do ghi bang met (m).",
    "Tuong bao day 220mm, tuong ngan 110mm, gach tuynel M75, vua XM M75.",
    "Be tong dam, cot, san da 10x20 cap do ben B20 (M250); mong coc M300.",
    "Thep chiu luc CB300-V, thep dai CB240-T; lop bao ve dam cot 25mm, san 15mm.",
    "Cot nen WC va ban cong ha 50mm so voi san phong, quet chong tham Sika 3 lop.",
    "Bac thang op da Granite day 30mm, co bac 20mm, mui bac chong truot.",
    "Lan can cau thang va ban cong: kinh cuong luc 12mm, tay vin inox 304, H=900mm.",
    "Truoc khi thi cong phai kiem tra kich thuoc thuc te tai hien truong.",
]

TITLE_INFO = {
    "cong_ty": "CONG TY TU VAN THIET KE (MAU)",
    "dia_chi": "DIA CHI DON VI THIET KE",
    "lien_he": "Tel: (0xx) xxxx xxxx",
    "chu_dau_tu": "Ong Nguyen Van A",
    "cong_trinh": "NHA PHO 3 TANG HIEN DAI (5.0m x 18.0m)",
    "vi_tri": "Dia diem xay dung (mau)",
    "hang_muc": "Kien truc",
    "giai_doan": "TK.BVTC",
    "ngay": "09/2026",
    "chu_ky": {"Giam doc": "KTS. Nguyen Van A",
               "Chu nhiem": "KTS. Tran Van B",
               "Chu tri KT": "KS. Le Van C",
               "Thiet ke": "KS. Pham Van D"},
}

# --- Thong ke cua: (ky hieu, loai, rong, cao, so luong, vat lieu, ghi chu) ---
DOORS = [
    ("D1", "Cua cuon khe thoang gara", 3600, 2600, 1,
     "Nhom khe thoang, motor am tran", "Tang 1, mat tien"),
    ("D2", "Cua di 1 canh phong ngu", 900, 2200, 4,
     "Go cong nghiep phu Melamine", "Tang 2 va tang 3"),
    ("D3", "Cua di 2 canh phong tho", 1600, 2200, 1,
     "Go cam xe tu nhien, hoa van truyen thong", "Tang 3, huong mat tien"),
    ("D4", "Cua di WC", 800, 2100, 4,
     "Nhua composite chiu am", "Khe thoang duoi canh 20mm"),
    ("D5", "Cua di ra san sau", 900, 2200, 1,
     "Nhom Xingfa kinh cuong luc 8mm", "Tang 1, mat hau"),
    ("D6", "Cua di ra ban cong", 900, 2200, 2,
     "Nhom Xingfa kinh dan 6.38mm", "Tang 2 va tang 3"),
    ("S1", "Cua so mo quay 2 canh", 1600, 1400, 8,
     "Nhom Xingfa kinh dan 6.38mm", "Hong nha va mat hau"),
    ("S2", "Cua so lat thong gio WC", 600, 600, 4,
     "Nhom son tinh dien, luoi chong con trung", "Mo ra gieng troi"),
    ("S3", "Vach kinh ban cong", 2600, 2300, 2,
     "Nhom Xingfa kinh dan 8.38mm", "Tang 2 va tang 3"),
]

NOTES_CUA = [
    "Kich thuoc cua go va cua nhom trong bang da tinh ca khuon bao.",
    "Kinh dan an toan 6.38mm cho cua so; 8.38mm cho vach kinh va cua di lon.",
    "Ban le va khoa dung hang inox 304, cua chinh dung khoa the tu.",
    "Cua WC lap khe thoang duoi canh 20mm de thong gio.",
    "Truoc khi dat cua phai do lai kich thuoc lo cho thuc te tai cong trinh.",
]
