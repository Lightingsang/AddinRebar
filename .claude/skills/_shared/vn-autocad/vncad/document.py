# -*- coding: utf-8 -*-
"""Khoi tao ban ve DXF: layer, kieu chu, kieu kich thuoc chuan TCVN."""

import ezdxf

from . import standards as S

FONT_VN = "times.ttf"          # Times New Roman: doc duoc tieng Viet Unicode
FONT_VN_ALT = "arial.ttf"


def new_drawing(discipline="arch", scale="1/100"):
    """Tao ban ve moi da nap san layer + kieu chu + dimstyle theo bo mon.

    discipline: 'arch' | 'struct' | 'mep' | 'all'
    """
    doc = ezdxf.new("R2010", setup=True)
    doc.header["$INSUNITS"] = 4        # milimet
    doc.header["$LUNITS"] = 2          # thap phan
    doc.header["$LUPREC"] = 0          # kich thuoc lam tron 0 chu so (mm nguyen)

    _add_text_styles(doc)
    for name, color, ltype, lw in _layer_set(discipline):
        _add_layer(doc, name, color, ltype, lw)
    _add_dimstyle(doc, scale)
    return doc


def _layer_set(discipline):
    if discipline == "arch":
        return S.ARCH_LAYERS
    if discipline == "struct":
        return S.STRUCT_LAYERS
    if discipline == "mep":
        return S.MEP_LAYERS
    seen, out = set(), []
    for group in (S.ARCH_LAYERS, S.STRUCT_LAYERS, S.MEP_LAYERS):
        for row in group:
            if row[0] not in seen:
                seen.add(row[0])
                out.append(row)
    return out


def _add_layer(doc, name, color, linetype, lineweight_mm):
    if linetype not in doc.linetypes:
        linetype = "Continuous"
    if name in doc.layers:
        layer = doc.layers.get(name)
    else:
        layer = doc.layers.add(name, color=color, linetype=linetype)
    # AutoCAD luu be day net theo don vi 1/100 mm; 0 nghia la khong in
    layer.dxf.lineweight = int(round(lineweight_mm * 100)) if lineweight_mm > 0 else -3
    if name in ("DEFPOINTS", "MODEL-GUIDE"):
        layer.dxf.plot = 0
    return layer


def _add_text_styles(doc):
    for name, font in (("TCVN", FONT_VN), ("TCVN-ARIAL", FONT_VN_ALT)):
        if name not in doc.styles:
            doc.styles.add(name, font=font)


def _add_dimstyle(doc, scale, name="TCVN-100"):
    """Kieu kich thuoc kien truc: mui ten gach cheo, chu nam tren duong dim."""
    k = S.SCALES[scale]
    if name in doc.dimstyles:
        return doc.dimstyles.get(name)
    ds = doc.dimstyles.add(name)
    ds.dxf.dimtxsty = "TCVN"
    ds.dxf.dimtxt = S.TEXT_MM["dim"] * k      # cao chu so kich thuoc
    ds.dxf.dimasz = 2.0 * k                   # kich thuoc gach cheo
    ds.dxf.dimblk = "ARCHTICK"                # dau gach cheo kien truc
    ds.dxf.dimexe = 1.25 * k                  # duong giong nho ra
    ds.dxf.dimexo = 1.0 * k                   # chan duong giong cach cau kien
    ds.dxf.dimgap = 0.8 * k                   # khe ho chu voi duong dim
    ds.dxf.dimtad = 1                         # chu nam tren duong dim
    ds.dxf.dimjust = 0                        # canh giua
    ds.dxf.dimdec = 0                         # kich thuoc so nguyen mm
    ds.dxf.dimlfac = 1.0
    ds.dxf.dimclrt = 2                        # mau chu vang cho de doc
    ds.dxf.dimclrd = 4
    ds.dxf.dimclre = 4
    return ds


def dimstyle_for(doc, scale):
    """Lay (hoac tao) dimstyle ung voi ty le, dung khi 1 to co nhieu ty le."""
    name = "TCVN-%d" % S.SCALES[scale]
    if name not in doc.dimstyles:
        _add_dimstyle(doc, scale, name)
    return name
