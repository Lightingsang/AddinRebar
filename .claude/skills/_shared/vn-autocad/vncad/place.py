# -*- coding: utf-8 -*-
"""Dat mot hinh chieu vao dung cho tren to giay.

Mat dung va mat cat ve theo toa do THAT (cao do +0.150, +3.800 ...), nhung tren to giay
phai day ca cum len mot khoang. Dung khoi `with Placed(...)` de ve xong roi doi ca cum,
nho vay ham ve khong phai gong them tham so goc dat.
"""


class Placed:
    """Ve trong khoi nay xong thi moi doi tuong vua tao duoc doi di (dx, dy).

    Vi du:
        with Placed(msp, dx=3400, dy=9000):
            ve_mat_cat(msp, doc)
    """

    def __init__(self, msp, dx=0.0, dy=0.0, scale=1.0):
        """scale: he so phong to hinh chieu. Ve chi tiet o ty le 1/20 roi dat len
        to giay 1/100 thi truyen scale=5 (bang 100/20)."""
        self.msp = msp
        self.dx = dx
        self.dy = dy
        self.scale = scale
        self._start = 0

    def __enter__(self):
        self._start = len(self.msp)
        return self

    def __exit__(self, exc_type, exc, tb):
        if exc_type is not None:
            return False
        if self.scale != 1.0:
            from ezdxf.math import Matrix44
            m = Matrix44.scale(self.scale, self.scale, self.scale)
            m = m @ Matrix44.translate(self.dx, self.dy, 0)
            for e in list(self.msp)[self._start:]:
                e.transform(m)
        elif self.dx or self.dy:
            for e in list(self.msp)[self._start:]:
                e.translate(self.dx, self.dy, 0)
        return False


def bbox_of(msp, start_index):
    """Bao cua cac doi tuong tao tu vi tri start_index tro di."""
    from ezdxf import bbox
    ents = list(msp)[start_index:]
    if not ents:
        return None
    box = bbox.extents(ents, fast=True)
    return (box.extmin.x, box.extmin.y, box.extmax.x, box.extmax.y)
