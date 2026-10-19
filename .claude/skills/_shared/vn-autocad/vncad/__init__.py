# -*- coding: utf-8 -*-
"""vncad: thu vien dung ho so ban ve xay dung chuan Viet Nam bang Python + ezdxf.

Dung nhanh:

    from vncad import document, titleblock as tb, arch, annotate as an, render

    doc = document.new_drawing("arch", "1/100")
    msp = doc.modelspace()
    arch.wall(msp, (0, 0), (18000, 0), 220, openings=[(3000, 900)])
    tb.draw_frame(msp, "A3", "1/100")
    render.save_dxf(doc, "out/KT-01.dxf")
"""

__version__ = "1.0.0"

from . import (acad, annotate, arch, blocks, document, elevation, estimate,
               fixtures, mep, place, project, qc, qc_geom, qc_text, rebar, render,
               section, sheet, standards, struct, table, tiles, titleblock)

__all__ = ["acad", "annotate", "arch", "blocks", "document", "elevation",
           "estimate", "fixtures", "mep", "place", "project", "qc", "qc_geom", "qc_text",
           "rebar", "render", "section", "sheet", "standards", "struct",
           "table", "tiles", "titleblock"]
