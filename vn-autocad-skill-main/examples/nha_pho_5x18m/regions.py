# -*- coding: utf-8 -*-
"""Vi tri tung cum hinh trong model space va tam dat cua so nhin tren giay.

Model space xep theo LUOI O NGAN NAP, moi o mot cum hinh, nhom theo bo mon:

    hang 0 : kien truc, cac mat bang
    hang 1 : kien truc, mat dung va mat cat
    hang 2 : kien truc, khai trien ve sinh va cau thang
    hang 3 : ket cau
    hang 4 : co dien

Nho vay mo ban ve trong AutoCAD roi zoom extents la thay ngay bo cuc,
thay vi mot dai dai hang tram met nhu khi dat hinh tuy tien.
"""

from . import layout as L

PITCH = 11500.0       # khoang tim 2 mat bang xep chong: du cho dim + tieu de tang tren

CELL_W, CELL_H = 32000.0, 30000.0     # kich thuoc mot o luoi
PAD_X = 3600.0                        # chua cho hang kich thuoc doc ben trai

# Ten hang de ghi nhan len ban ve
TEN_HANG = ["KIEN TRUC - MAT BANG", "KIEN TRUC - MAT DUNG VA MAT CAT",
            "KIEN TRUC - KHAI TRIEN VA CHI TIET", "KET CAU", "CO DIEN",
            "SO DO NGUYEN LY"]


def o(col, row, dx=0.0, dy=0.0):
    """Goc dat mot cum hinh trong o luoi (col, row). Hang 0 nam tren cung."""
    return (col * CELL_W + PAD_X + dx, -row * CELL_H + dy)


R = {
    # --- hang 0: cac mat bang kien truc, moi o hai tang xep chong -----------
    "tang1": o(0, 0, dy=8000), "tang2": o(0, 0, dy=8000 + PITCH),
    "tang3": o(1, 0, dy=8000), "mai": o(1, 0, dy=8000 + PITCH),
    "gach_t1": o(2, 0, dy=8000), "gach_t2": o(2, 0, dy=8000 + PITCH),
    "tran_t1": o(3, 0, dy=8000), "tran_t2": o(3, 0, dy=8000 + PITCH),

    # --- hang 1: mat dung va mat cat, goc dat la coc +0.000 ----------------
    "mat_cat": o(0, 1, dy=12000),
    "mat_dung": o(1, 1, dy=12000),
    "mat_dung_ben": o(2, 1, dy=12000),
    "mat_cat_bb": o(3, 1, dy=12000),

    # --- hang 2: khai trien ve sinh va cau thang ---------------------------
    "wc": o(0, 2, dy=15000),
    "wc1": o(1, 2, dy=15000),
    "wc3": o(2, 2, dy=15000),
    "thang_mb": o(3, 2, dy=20000),
    "thang_mc": o(3, 2, dy=8000),
    "bac_thang": o(3, 2, dx=14000, dy=6000),
    "lan_can": o(3, 2, dx=24000, dy=13000),

    # --- hang 3: ket cau ---------------------------------------------------
    "mong": o(0, 3, dy=18000),
    "chi_tiet_m1": o(0, 3, dx=3000, dy=6000),
    "mc_giang": o(0, 3, dx=9000, dy=6000),
    "dam_san": o(1, 3, dy=18000),
    "dam_d1": o(2, 3, dy=22000),
    "mc_dam": o(2, 3, dx=1200, dy=14000),
    "cot_c1": o(2, 3, dx=9000, dy=4000),
    "nut_khung": o(3, 3, dx=3000, dy=5000),
    "thep_san": o(2, 3, dx=18000, dy=14000),
    "noi_cot": o(3, 3, dx=18000, dy=6000),

    # --- hang 4: co dien ---------------------------------------------------
    "dien": o(0, 4, dy=9000),
    "nuoc": o(1, 4, dy=9000),
    "dien_t2": o(2, 4, dy=8000), "dien_t3": o(2, 4, dy=8000 + PITCH),
    "nuoc_t2": o(3, 4, dy=8000), "nuoc_t3": o(3, 4, dy=8000 + PITCH),

    # --- cot 4: bo sung tang 3 va cac nut cau tao chong tham ---------------
    "gach_t3": o(4, 0, dy=8000), "tran_t3": o(4, 0, dy=8000 + PITCH),
    "ct_wc": o(4, 2, dx=1000, dy=22000),
    "ct_seno": o(4, 2, dx=1000, dy=14000),
    "ct_ban_cong": o(4, 2, dx=1000, dy=6000),

    # --- cot 5: tong mat bang, cum hinh rong nhat nen chiem tron mot o -----
    "tong_mb": o(5, 0, dx=5100, dy=12000),

    # --- ket cau cau thang, dat cung hang ket cau --------------------------
    "thang_kc_mb": o(4, 3, dx=3000, dy=22000),
    "thang_kc_mc": o(4, 3, dx=3000, dy=9000),

    # --- hang 5: so do nguyen ly -------------------------------------------
    "sd_dien": o(0, 5, dy=8000),
    "sd_nuoc": o(1, 5, dy=9000),
}

# Khung vung va nhan cho tung o luoi: (col, row, so_hieu_to, ten_cum)
O_LUOI = [
    (0, 0, "KT-01", "Mat bang tang 1 va tang 2"),
    (1, 0, "KT-02", "Mat bang tang 3 va mat bang mai"),
    (2, 0, "KT-07", "Mat bang lat gach tang 1 va tang 2"),
    (3, 0, "KT-08", "Mat bang tran tang 1 va tang 2"),
    (0, 1, "KT-03a", "Mat cat A-A"),
    (1, 1, "KT-03b", "Mat dung chinh"),
    (2, 1, "KT-06", "Mat dung ben truc 1-5"),
    (3, 1, "KT-10", "Mat cat B-B"),
    (0, 2, "KT-04", "Khai trien WC 2"),
    (1, 2, "KT-11", "Khai trien WC 1"),
    (2, 2, "KT-12", "Khai trien WC 3"),
    (3, 2, "KT-05", "Khai trien cau thang"),
    (0, 3, "KC-01", "Mat bang mong va chi tiet dai M1"),
    (1, 3, "KC-02", "Mat bang dam san tang 2"),
    (2, 3, "KC-03", "Chi tiet thep dam D1 va cot C1"),
    (3, 3, "KC-04", "Chi tiet nut khung dam cot"),
    (0, 4, "ME-01", "Cap dien tang 1"),
    (1, 4, "ME-02", "Cap thoat nuoc tang 1"),
    (2, 4, "ME-03", "Cap dien tang 2 va tang 3"),
    (3, 4, "ME-04", "Cap thoat nuoc tang 2 va tang 3"),
    (4, 0, "KT-13", "Lat gach va tran tang 3"),
    (4, 2, "KT-14", "Chi tiet chong tham"),
    (5, 0, "KT-15", "Tong mat bang dinh vi"),
    (4, 3, "KC-05", "Ket cau cau thang"),
    (0, 5, "ME-05", "So do nguyen ly tu dien"),
    (1, 5, "ME-06", "So do khong gian cap thoat nuoc"),
]


def centers():
    """Tam vung hinh de dat cua so nhin. Da chua san cho kich thuoc va tieu de."""
    def hai_tang(key_duoi):
        ox, oy = R[key_duoi]
        # tu tieu de tang duoi (-7000) toi bong bong dinh tang tren (+PITCH+6800)
        return (ox + L.L / 2 - 1500, oy + (PITCH + 6800 - 7000) / 2)

    def mot_mat_bang(key):
        ox, oy = R[key]
        # lui trai 800 de nhan cau kien o truc cuoi khong cham mep cua so
        return (ox + L.L / 2 - 800, oy + (5000 - 7000) / 2)

    return {
        "kt01": hai_tang("tang1"),
        "kt02": hai_tang("tang3"),
        "gach": hai_tang("gach_t1"),
        "tran": hai_tang("tran_t1"),
        "dien23": hai_tang("dien_t2"),
        "nuoc23": hai_tang("nuoc_t2"),
        # lui tam sang trai va xuong duoi de ky hieu cao do va tieu de khong bi cat
        "mat_cat": (R["mat_cat"][0] + L.L / 2 - 900, R["mat_cat"][1] + 4200),
        "mat_dung": (R["mat_dung"][0] + L.W / 2, R["mat_dung"][1] + 4200),
        "mat_dung_ben": (R["mat_dung_ben"][0] + L.L / 2 - 3200,
                         R["mat_dung_ben"][1] + 4200),
        "mat_cat_bb": (R["mat_cat_bb"][0] + L.W / 2 - 1400,
                       R["mat_cat_bb"][1] + 4200),
        "mong": mot_mat_bang("mong"),
        "dam_san": mot_mat_bang("dam_san"),
        "dien": (R["dien"][0] + L.L / 2 - 800, R["dien"][1] + 800),
        "nuoc": (R["nuoc"][0] + L.L / 2 - 800, R["nuoc"][1] + 800),
        "chi_tiet_m1": (R["chi_tiet_m1"][0], R["chi_tiet_m1"][1] - 200),
        "mc_giang": (R["mc_giang"][0], R["mc_giang"][1] + 8),
        "wc": (R["wc"][0] + 2400, R["wc"][1] - 3800),
        "wc1": (R["wc1"][0] + 2400, R["wc1"][1] - 3800),
        "wc3": (R["wc3"][0] + 2400, R["wc3"][1] - 3800),
        "thang_mb": (R["thang_mb"][0] + 2200, R["thang_mb"][1] + 200),
        "thang_mc": (R["thang_mc"][0] + 1800, R["thang_mc"][1] + 1000),
        "bac_thang": (R["bac_thang"][0] + 400, R["bac_thang"][1] + 200),
        "dam_d1": (R["dam_d1"][0] + 2500, R["dam_d1"][1] - 500),
        "mc_dam": (R["mc_dam"][0] + 600, R["mc_dam"][1] + 60),
        "cot_c1": (R["cot_c1"][0] + 500, R["cot_c1"][1] + 1600),
        "nut_khung": (R["nut_khung"][0] + 18, R["nut_khung"][1] + 522),
        # do bang _do_vung.py: tam o bao that cua tung cum so voi goc dat
        "lan_can": (R["lan_can"][0] + 114, R["lan_can"][1] + 106),
        "thep_san": (R["thep_san"][0], R["thep_san"][1] - 248),
        "noi_cot": (R["noi_cot"][0], R["noi_cot"][1] + 516),
        "tong_mb": (R["tong_mb"][0] + 7150, R["tong_mb"][1] + 1822),
        "thang_kc_mb": (R["thang_kc_mb"][0] + 1854, R["thang_kc_mb"][1] + 21),
        "thang_kc_mc": (R["thang_kc_mc"][0] + 1311, R["thang_kc_mc"][1] + 238),
        "gach_tran_t3": hai_tang("gach_t3"),
        "ct_wc": (R["ct_wc"][0] + 700, R["ct_wc"][1] + 100),
        "ct_seno": (R["ct_seno"][0] + 700, R["ct_seno"][1] + 100),
        "ct_ban_cong": (R["ct_ban_cong"][0] + 700, R["ct_ban_cong"][1] + 100),
        "sd_dien": (R["sd_dien"][0] + 8000, R["sd_dien"][1] + 7000),
        "sd_nuoc": (R["sd_nuoc"][0] + 6000, R["sd_nuoc"][1] + 2600),
    }
