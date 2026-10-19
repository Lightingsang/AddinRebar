# -*- coding: utf-8 -*-
"""Soat toan bo ban ve MA KHONG render anh, nen chay het trong vai giay.

Dung khi dang sua bo cuc: build day du phai ve hang chuc anh PNG nen mat vai
phut, con cong cu nay chi dung hinh trong bo nho roi chay bon phep soat.

    python tools/soat-ban-ve-khong-render.py
    python tools/soat-ban-ve-khong-render.py --du-an du_an.nha_ong_4x16

Chay tu thu muc chua goi du an. In canh bao gom theo tung to. Ma thoat 0 la sach.
"""

import argparse
import collections
import os
import re
import sys

GOC = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
# thu muc dang dung truoc de nap duoc du an cua nguoi dung, roi toi goc bo cong cu
sys.path[:0] = [os.getcwd(), GOC]

from vncad import project                                         # noqa: E402


def gom_theo_to(canh_bao, da):
    """Quy moi canh bao ve o luoi chua toa do, de biet phai sua o to nao."""
    dung = {(c, r): s for c, r, s, t in da.o_luoi}
    nhom = collections.OrderedDict()
    for x in canh_bao:
        m = re.search(r"tai \((-?\d+), (-?\d+)\)", x)
        if m:
            cx, cy = float(m.group(1)), float(m.group(2))
            col = int(cx // da.cell_w)
            row = 0 if cy >= 0 else int(-cy // da.cell_h) + 1
            ten = dung.get((col, row), "ngoai o luoi")
        else:
            ten = "tren giay"
        nhom.setdefault(ten, []).append(x)
    return nhom


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--du-an", default=project.DU_AN_MAU,
                    help="goi Python cua du an (mac dinh: %(default)s)")
    args = ap.parse_args()
    doc, ten_cac_to, da = project.build(args.du_an)
    canh_bao = project.qc_all(doc, ten_cac_to, da)
    if not canh_bao:
        print("SACH: %d to, khong con cho nao chong nhau." % len(ten_cac_to))
        return 0
    print("TONG CANH BAO:", len(canh_bao))
    for ten, ds in gom_theo_to(canh_bao, da).items():
        print("=== %s (%d) ===" % (ten, len(ds)))
        for x in ds:
            print("   ", x)
    return 1


if __name__ == "__main__":
    sys.exit(main())
