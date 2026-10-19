# -*- coding: utf-8 -*-
"""De xuat do doi nho nhat de mot dong chu thoi bi net ve cat qua.

Quet mot luoi quanh vi tri hien tai, tra ve (dx, dy) dau tien khong con dinh
net nao. Dua vao mot hay nhieu noi dung chu dang bi bo soat bao loi.

    python tools/de-xuat-cho-dat-chu.py "Thoat san D60" "Ong cap PPR D25"
"""

import argparse
import os
import sys

GOC = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path[:0] = [os.getcwd(), GOC]

from vncad import document, project, qc_geom, qc_text             # noqa: E402

BUOC = 100.0          # do doi moi nac, mm trong model space
TAM = 30              # quet toi da 30 nac moi phia


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("noi_dung", nargs="+")
    ap.add_argument("--du-an", default=project.DU_AN_MAU)
    args = ap.parse_args()
    doc = document.new_drawing("all", "1/100")
    msp = doc.modelspace()
    project.load(args.du_an).build_model(msp, doc)
    luoi = qc_geom._xep_luoi(msp, qc_geom.O_LUOI)
    # thu cac nac gan truoc, uu tien doi thang theo mot truc
    nac = sorted(((dx, dy) for dx in range(-TAM, TAM + 1) for dy in range(-TAM, TAM + 1)),
                 key=lambda t: (abs(t[0]) + abs(t[1]), abs(t[0] * t[1])))
    for e in msp.query("TEXT"):
        if (e.dxf.text or "") not in args.noi_dung:
            continue
        x0, y0, x1, y1, s = qc_text.o_bao(e)
        lot = min(x1 - x0, y1 - y0) * qc_geom.LOT
        goc = (x0 + lot, y0 + lot, x1 - lot, y1 - lot)
        if not qc_geom._bi_cat(goc, luoi, qc_geom.O_LUOI):
            continue
        tot = None
        for dx, dy in nac:
            h = (goc[0] + dx * BUOC, goc[1] + dy * BUOC,
                 goc[2] + dx * BUOC, goc[3] + dy * BUOC)
            if not qc_geom._bi_cat(h, luoi, qc_geom.O_LUOI):
                tot = (dx * BUOC, dy * BUOC)
                break
        print("%-30s tai (%.0f, %.0f)  ->  doi %s"
              % (s[:30], x0, y0, tot if tot else "khong tim ra trong pham vi quet"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
