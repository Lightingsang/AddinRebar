# -*- coding: utf-8 -*-
"""Cau hinh chung cho bo kiem thu: them goc repo vao duong dan, dung ho so mau mot lan."""

import os
import sys

import pytest

GOC = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if GOC not in sys.path:
    sys.path.insert(0, GOC)

from vncad import project, standards as S                         # noqa: E402


def co_phong_times():
    """May co phong Times New Roman that khong.

    Bo soat do be rong chu bang chinh file phong. May khong co phong nay (vi du
    Linux chua cai) thi ezdxf dung phong thay the rong hon, ket qua soat se khac;
    cac test phu thuoc phong tu bo qua thay vi bao sai.
    """
    return abs(S.rong_chu("MAT BANG TANG 1", 2.5) - 33.53) < 0.5


can_phong_times = pytest.mark.skipif(not co_phong_times(),
                                     reason="may chua cai phong Times New Roman")


@pytest.fixture(scope="session")
def ho_so():
    """Ho so mau dung trong bo nho, khong render. Dung chung cho moi test."""
    return project.build()
