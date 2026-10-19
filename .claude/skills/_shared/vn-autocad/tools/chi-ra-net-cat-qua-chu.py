# -*- coding: utf-8 -*-
"""Chi ra CHINH XAC doan thang nao dang cat qua mot dong chu.

Bo soat chi bao "co net cat qua chu X"; cong cu nay in ra tung doan thang thu
pham kem lop va toa do, de biet phai dich nhan hay dich net.

    python tools/chi-ra-net-cat-qua-chu.py "GM1 250x400"
    python tools/chi-ra-net-cat-qua-chu.py "WC 1" --du-an du_an.nha_ong_4x16
"""

import argparse
import os
import sys

GOC = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path[:0] = [os.getcwd(), GOC]

from vncad import document, project, qc_geom, qc_text             # noqa: E402


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("noi_dung", help="noi dung dong chu, ghi dung chu hoa thuong")
    ap.add_argument("--du-an", default=project.DU_AN_MAU)
    args = ap.parse_args()
    doc = document.new_drawing("all", "1/100")
    msp = doc.modelspace()
    project.load(args.du_an).build_model(msp, doc)
    thay = 0
    for e in msp.query("TEXT"):
        if (e.dxf.text or "") != args.noi_dung:
            continue
        thay += 1
        x0, y0, x1, y1, s = qc_text.o_bao(e)
        lot = min(x1 - x0, y1 - y0) * qc_geom.LOT
        hop = (x0 + lot, y0 + lot, x1 - lot, y1 - lot)
        print('CHU "%s" o bao (%.0f %.0f)-(%.0f %.0f) cao %.0f'
              % (s, x0, y0, x1, y1, e.dxf.height))
        for g in msp:
            if g.dxftype() in qc_geom.BO_QUA_LOAI:
                continue
            for seg in qc_geom._doan_thang(g):
                if qc_geom._cat_hop(seg, hop):
                    print("   %-11s lop %-11s (%.0f,%.0f)-(%.0f,%.0f)"
                          % (g.dxftype(), g.dxf.get("layer", "0"), *seg[:4]))
        print()
    if not thay:
        print("Khong tim thay dong chu nao co noi dung dung nhu vay.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
