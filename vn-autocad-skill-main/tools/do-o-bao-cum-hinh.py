# -*- coding: utf-8 -*-
"""Do o bao that cua mot cum hinh, de chon kich thuoc va tam cua so nhin.

Khong phai render roi nhin xem hinh co bi cat khong: cong cu in ra kich thuoc
tren giay theo tung ty le, va TAM o bao de dien vao regions.centers().

    python tools/do-o-bao-cum-hinh.py draw_stair_railing.draw_railing_detail
    python tools/do-o-bao-cum-hinh.py draw_site_plan.draw_site_plan --du-an du_an.nha_ong_4x16

Ham ve phai co dang ham(msp, doc, origin).
"""

import argparse
import importlib
import os
import sys

GOC = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path[:0] = [os.getcwd(), GOC]

from ezdxf import bbox                                            # noqa: E402
from vncad import document, project                               # noqa: E402

TY_LE = (10, 20, 25, 50, 100, 200)


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("ham", help="<module>.<ham> trong goi du an")
    ap.add_argument("--du-an", default=project.DU_AN_MAU)
    args = ap.parse_args()
    ten_mo_dun, ten_ham = args.ham.rsplit(".", 1)
    mo_dun = importlib.import_module(args.du_an + "." + ten_mo_dun)
    doc = document.new_drawing("all", "1/100")
    msp = doc.modelspace()
    getattr(mo_dun, ten_ham)(msp, doc, (0.0, 0.0))
    b = bbox.extents(msp, fast=False)
    w, h = b.extmax.x - b.extmin.x, b.extmax.y - b.extmin.y
    print("o bao (%.0f %.0f) - (%.0f %.0f)" % (b.extmin.x, b.extmin.y,
                                               b.extmax.x, b.extmax.y))
    print("kich thuoc that %.0f x %.0f mm" % (w, h))
    print("tam so voi goc dat: (%.0f, %.0f)  <- dien vao centers()"
          % ((b.extmin.x + b.extmax.x) / 2, (b.extmin.y + b.extmax.y) / 2))
    for k in TY_LE:
        print("  ty le 1/%-3d ->  %7.1f x %7.1f mm tren giay" % (k, w / k, h / k))
    return 0


if __name__ == "__main__":
    sys.exit(main())
