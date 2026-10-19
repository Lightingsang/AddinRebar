# -*- coding: utf-8 -*-
"""Xuat ban ve ra DXF, PNG, PDF.

Hai che do xem:
  plot  : nen trang, net den, do dam net theo lineweight cua layer (giong ban in giay)
  cad   : nen den, mau ACI (giong man hinh AutoCAD)
"""

import os


def save_dxf(doc, path):
    os.makedirs(os.path.dirname(os.path.abspath(path)) or ".", exist_ok=True)
    doc.saveas(path)
    return path


def _figure(size, dpi, bg):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    fig = plt.figure(figsize=size, dpi=dpi)
    ax = fig.add_axes([0, 0, 1, 1])
    ax.set_axis_off()
    fig.patch.set_facecolor(bg)
    ax.set_facecolor(bg)
    return plt, fig, ax


def _config(mode):
    from ezdxf.addons.drawing.config import (ColorPolicy, Configuration,
                                             LineweightPolicy)
    # ABSOLUTE: be day net hieu theo mm tren giay, dung khi khung nhin khop kho giay
    cfg = Configuration().with_changes(
        lineweight_policy=LineweightPolicy.ABSOLUTE,
        lineweight_scaling=0.32,
        min_lineweight=0.05,
    )
    if mode == "plot":
        cfg = cfg.with_changes(color_policy=ColorPolicy.BLACK)
    return cfg


def render_image(doc, path, mode="plot", dpi=220, sheet="A3", extents=None):
    """Xuat anh xem truoc dung ty le kho giay.

    extents: (x0, y0, x1, y1) vung Model Space ung voi to giay. Neu bo trong thi
    lay bao cua toan bo ban ve. Khung nhin khop kho giay nen be day net (mm) dung that.
    """
    from ezdxf.addons.drawing import Frontend, RenderContext
    from ezdxf.addons.drawing.matplotlib import MatplotlibBackend

    from . import standards as S

    pw, ph = S.SHEETS[sheet]
    size = (pw / 25.4, ph / 25.4)
    bg = "#FFFFFF" if mode == "plot" else "#1B2229"
    os.makedirs(os.path.dirname(os.path.abspath(path)) or ".", exist_ok=True)
    plt, fig, ax = _figure(size, dpi, bg)

    Frontend(RenderContext(doc), MatplotlibBackend(ax), config=_config(mode)).draw_layout(
        doc.modelspace(), finalize=True)

    if extents is None:
        extents = _auto_extents(doc)
    _fit(ax, extents, pw / ph)
    fig.savefig(path, dpi=dpi, facecolor=bg)
    plt.close(fig)
    return path


def render_sheet(doc, layout_name, path, mode="plot", dpi=220, sheet="A3"):
    """Xuat 1 to ban ve (Layout) ra anh dung kho giay, ke ca noi dung trong cua so nhin."""
    from ezdxf.addons.drawing import Frontend, RenderContext
    from ezdxf.addons.drawing.matplotlib import MatplotlibBackend

    from . import standards as S

    pw, ph = S.SHEETS[sheet]
    bg = "#FFFFFF" if mode == "plot" else "#1B2229"
    os.makedirs(os.path.dirname(os.path.abspath(path)) or ".", exist_ok=True)
    plt, fig, ax = _figure((pw / 25.4, ph / 25.4), dpi, bg)

    lay = doc.layouts.get(layout_name)
    Frontend(RenderContext(doc), MatplotlibBackend(ax), config=_config(mode)).draw_layout(
        lay, finalize=True)
    _fit(ax, (0, 0, pw, ph), pw / ph)
    fig.savefig(path, dpi=dpi, facecolor=bg)
    plt.close(fig)
    return path


def save_sheet_set(doc, dxf_path, out_dir, layouts, mode="plot", dpi=220, sheet="A3"):
    """Luu 1 file DXF chung + xuat anh PNG cho tung to. Tra ve dict duong dan."""
    res = {"dxf": save_dxf(doc, dxf_path)}
    for name in layouts:
        res[name] = render_sheet(doc, name, os.path.join(out_dir, name + ".png"),
                                 mode, dpi, sheet)
    return res


def _auto_extents(doc):
    from ezdxf import bbox
    box = bbox.extents(doc.modelspace(), fast=True)
    return (box.extmin.x, box.extmin.y, box.extmax.x, box.extmax.y)


def _fit(ax, ext, ratio):
    """Dan khung nhin vao dung ty le kho giay, chua tron ban ve."""
    x0, y0, x1, y1 = ext
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    w, h = max(x1 - x0, 1.0) * 1.02, max(y1 - y0, 1.0) * 1.02
    if w / h < ratio:
        w = h * ratio
    else:
        h = w / ratio
    ax.set_xlim(cx - w / 2, cx + w / 2)
    ax.set_ylim(cy - h / 2, cy + h / 2)
    ax.set_aspect("equal")


def save_png(doc, path, mode="plot", dpi=220, sheet="A3", extents=None):
    return render_image(doc, path, mode, dpi, sheet, extents)


def save_pdf(doc, path, sheet="A3", extents=None):
    """Xuat PDF dung kho giay, net den chuan in ho so."""
    return render_image(doc, path, "plot", 300, sheet, extents)


def save_all(doc, stem, sheet="A3", extents=None):
    """Xuat 1 luot: .dxf + _plot.png + .pdf. Tra ve dict duong dan."""
    return {
        "dxf": save_dxf(doc, stem + ".dxf"),
        "png": render_image(doc, stem + ".png", "plot", sheet=sheet, extents=extents),
        "pdf": save_pdf(doc, stem + ".pdf", sheet, extents),
    }
