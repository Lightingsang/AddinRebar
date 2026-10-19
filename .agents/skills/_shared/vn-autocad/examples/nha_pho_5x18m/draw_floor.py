# -*- coding: utf-8 -*-
"""Ve mot mat bang kien truc tu du lieu trong layout.py."""

from vncad import annotate as an, arch, blocks, fixtures as fx

from . import layout as L

SC = "1/100"


def draw_floor(msp, doc, floor, origin=(0.0, 0.0)):
    """Ve tron ven 1 mat bang tang. Tra ve (y_duoi_cung, y_tren_cung) da ve."""
    ox, oy = origin
    _walls_and_openings(msp, floor, ox, oy)
    for x, y, size in L.COLUMNS:
        arch.column(msp, (ox + x, oy + y), size)          # cot ve sau de de len tuong
    _stair(msp, doc, floor, ox, oy)
    for name, x, y, rot in L.FLOOR_FIXTURES[floor]:
        fx.place(msp, doc, name, (ox + x, oy + y), rotation=rot)
    for name, (x, y), area, xoay in L.FLOOR_ROOMS[floor]:
        an.room_label(msp, doc, (ox + x, oy + y), name, area,
                      L.FLOOR_LEVEL[floor], SC, rotation=xoay)
    return oy, oy + L.W


def _walls_and_openings(msp, floor, ox, oy):
    for p1, p2, t, ops in L.FLOOR_WALLS[floor]:
        a = (ox + p1[0], oy + p1[1])
        b = (ox + p2[0], oy + p2[1])
        cuts = [(d, w) for d, w, _ in ops]
        made = arch.wall(msp, a, b, t, openings=cuts)
        for (s, e, u, n, tt), (_, _, kind) in zip(made, ops):
            if kind == "door":
                arch.door(msp, s, e, u, n, tt)
            elif kind == "door2":
                arch.door_double(msp, s, e, u, n, tt)
            elif kind == "wind":
                arch.window(msp, s, e, n, tt, panes=2)


def _stair(msp, doc, floor, ox, oy):
    st = L.STAIR
    o = (ox + st["origin"][0], oy + st["origin"][1])
    end = arch.stair_flight(msp, o, st["dir"], st["width"], st["tread"],
                            st["steps"], numbering=True, scale=SC)
    # mui ten huong di: tang 1 va 2 di len, tang tren cung di xuong
    if floor < 3:
        blocks.stair_arrow(msp, o, (end[0] + 400, end[1]))
        an.leader_note(msp, "LEN LAU %d" % floor, (end[0], end[1]),
                       (end[0] + 1800, end[1] + 500), SC)
    else:
        blocks.stair_arrow(msp, end, (o[0] - 400, o[1]))
        an.leader_note(msp, "DN (XUONG LAU 1)", (o[0], o[1]),
                       (o[0] - 2200, o[1] + 500), SC)
    # lan can kinh bao ho thong tang
    y0 = oy + st["origin"][1] - st["width"]
    arch.railing(msp, [(o[0] - 200, y0), (end[0] + 600, y0)], offset=60)


def dims_and_bubbles(msp, doc, oy, full=True, bottom_bubbles=True,
                     top_bubbles=False, ox=0.0):
    """Gong kich thuoc + bong bong truc cho mat bang dat tai goc (ox, oy).

    Quy tac xep nhieu tang tren 1 to: hang bong bong truc so CHI dat o day tang duoi
    cung va dinh tang tren cung, cac tang giua chi gong kich thuoc.
    """
    xs = [ox + x for _, x in L.AXIS_X]
    detail = sorted({ox + v for v in
                     (0, 700, 4300, 5500, 9500, 11400, 13000, 15600, 17200, 18000)})
    tiers = [detail, xs, [xs[0], xs[-1]]] if full else [xs, [xs[0], xs[-1]]]
    line = an.dim_tiers(msp, doc, tiers, oy, "h", SC, side=-1)
    if bottom_bubbles:
        an.grid_bubbles(msp, doc, [(n, ox + x) for n, x in L.AXIS_X], line, "h", SC,
                        base_coord=oy)
    ys = [oy + y for _, y in L.AXIS_Y]
    vline = an.dim_tiers(msp, doc, [ys], ox, "v", SC, side=-1)
    an.grid_bubbles(msp, doc, [(n, oy + y) for n, y in L.AXIS_Y], vline, "v", SC,
                    base_coord=ox)
    if top_bubbles:
        tline = an.dim_tiers(msp, doc, [xs], oy + L.W, "h", SC, side=+1)
        an.grid_bubbles(msp, doc, [(n, ox + x) for n, x in L.AXIS_X], tline, "h", SC,
                        base_coord=oy + L.W)
    return line
