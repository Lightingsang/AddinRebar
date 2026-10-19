# -*- coding: utf-8 -*-
"""Lap bang du toan chi phi xay dung nha pho 5x18m 3 tang ra file Excel."""

import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from vncad.estimate import build_estimate
from .rebar_quantities import (NGUON, NGUON_MONG, thep_mong,
                                                     thep_than_nha)

# Thep khung than nha lay tu bang thong ke tren ban ve, khong go tay
THEP_NHO, THEP_LON = thep_than_nha()
THEP_MONG = thep_mong()

PROJECT = {"ten": "NHA PHO 3 TANG HIEN DAI (5.0m x 18.0m)",
           "dia_diem": "Dia diem xay dung (mau)",
           "chu_dau_tu": "Ong Nguyen Van A", "ngay": "09/2026"}

ROWS = [
    ("SECTION", "PHAN I: PHAN NGAM VA MONG"),
    ("AB.111", "Ep coc BTCT 250x250, chieu sau ep 25m", "m dai", 750, 280000),
    ("AB.121", "Dao dat mong, dai mong, ho ga, be phot", "m3", 120, 180000),
    ("AF.111", "Be tong lot mong da 4x6 M100 day 100mm", "m3", 14.5, 1250000),
    ("AF.122", "Be tong dai mong va giang mong da 1x2 M250", "m3", 38, 1650000),
    ("AF.611", "Van khuon dai mong va giang mong", "m2", 95, 240000),
    ("AF.612", "Cot thep mong CB300-V va thep dai CB240-T, " + NGUON_MONG,
     "kg", round(THEP_MONG, 1), 18500),
    ("AB.241", "Cat lap ton nen mong dam chat K=0.9", "m3", 65, 260000),

    ("SECTION", "PHAN II: KHUNG BE TONG COT THEP THAN NHA"),
    ("AF.211", "Be tong cot cac tang da 1x2 M250", "m3", 18.5, 1750000),
    ("AF.221", "Be tong dam san cac tang va mai da 1x2 M250", "m3", 68, 1680000),
    ("AF.621", "Van khuon cot, dam, san phu phim", "m2", 540, 260000),
    ("AF.622", "Cot thep dam cot san CB300-V (D lon hon 10mm), " + NGUON,
     "kg", round(THEP_LON, 1), 18500),
    ("AF.623", "Cot thep san va thep dai CB240-T (D tu 10mm tro xuong), " + NGUON,
     "kg", round(THEP_NHO, 1), 18000),

    ("SECTION", "PHAN III: XAY TO VA HOAN THIEN KIEN TRUC"),
    ("AK.111", "Xay tuong bao day 220mm gach tuynel M75", "m3", 52, 1650000),
    ("AK.112", "Xay tuong ngan day 110mm gach tuynel M75", "m3", 46, 1550000),
    ("AK.211", "Trat tuong trong va ngoai vua XM M75 day 15mm", "m2", 880, 145000),
    ("AK.511", "Chong tham san WC, ban cong, san thuong Sika 3 lop", "m2", 85, 220000),
    ("AK.521", "Lat gach Granite 800x800 san khach, bep, sanh", "m2", 185, 380000),
    ("AK.522", "Op lat gach men chong truot phong ve sinh 300x600", "m2", 96, 350000),
    ("AK.611", "Op da Granite den tu nhien cau thang va tam cap", "m2", 38, 1150000),
    ("AK.711", "Tran thach cao chim khung xuong giat cap chong am", "m2", 220, 225000),
    ("AK.811", "Son ba tuong matit 2 lop, lot khang kiem, phu 2 lop", "m2", 880, 95000),
    ("AK.911", "Cua nhom Xingfa nhap khau kinh an toan 8.38mm", "m2", 65, 2200000),
    ("AK.921", "Lan can kinh cuong luc 12mm tay vin inox 304 H=900", "m dai", 28, 1350000),

    ("SECTION", "PHAN IV: HE THONG CO DIEN NUOC (MEP)"),
    ("ME.101", "He thong dien am tuong (day Cadivi, ong Nano, tu dien)", "goi", 1, 65000000),
    ("ME.201", "He thong cap thoat nuoc (ong PPR, PVC Tien Phong)", "goi", 1, 48000000),
    ("ME.301", "Thiet bi ve sinh 4 WC (bon tam, bet, lavabo Inax)", "goi", 1, 68000000),
]


def build(out_dir="examples/out"):
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "DT-01_DuToan_NhaPho_5x18m.xlsx")
    return build_estimate(path, PROJECT, ROWS)


if __name__ == "__main__":
    print(os.path.abspath(build()))
