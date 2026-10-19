# -*- coding: utf-8 -*-
"""Ve mat dung chinh (mat tien rong 5m) nhin tu ngoai duong vao."""

from vncad import elevation as el

from . import layout as L

SC = "1/100"
X0, X1 = 0.0, 5000.0


def draw_front_elevation(msp, doc):
    """Mat dung chinh: gara tret, 2 ban cong, tuong chan mai, mang op da."""
    el.ground_line(msp, X0, X1, L.Z_DAT)
    top = L.LEVELS[3][1]
    el.outline(msp, [(X0, L.Z_DAT * 1000.0), (X1, L.Z_DAT * 1000.0),
                     (X1, top * 1000.0), (X0, top * 1000.0)])

    _ground_floor(msp)
    _balconies(msp)
    _material_zones(msp)
    el.parapet(msp, X0 - 110, X1 + 110, top, h=900)
    _stair_box(msp, top)
    el.storey_grid(msp, doc, L.LEVELS, X0, X1, SC, side="left", tail=1400)
    _material_notes(msp)


def _ground_floor(msp):
    """Cua gara cuon 3.6m + cua di phu + o thoang."""
    el.opening(msp, 700, L.Z_DAT, 3600, 2600, kind="louver")
    el.opening(msp, 4400, L.Z_DAT, 400, 2600, kind="glass", panes=1)


def _balconies(msp):
    """Ban cong lau 1 va lau 2, lan can kinh cuong luc."""
    for _, z in L.LEVELS[1:3]:
        el.balcony(msp, X0 + 300, X1 - 300, z, h_rail=900, kind="kinh")
        el.opening(msp, 1200, z + 0.05, 2600, 2300, kind="glass", panes=3)


def _material_zones(msp):
    """Mang op da granite 2 ben mat tien, tao khoi dac chac."""
    for x0 in (X0, X1 - 700):
        pts = [(x0, L.Z_DAT * 1000.0), (x0 + 700, L.Z_DAT * 1000.0),
               (x0 + 700, L.LEVELS[3][1] * 1000.0), (x0, L.LEVELS[3][1] * 1000.0)]
        el.material(msp, pts, pattern="ANSI31", pscale=340.0, angle=45.0)


def _stair_box(msp, top):
    """Buong thang nho tren san thuong lui vao trong."""
    z_top = L.LEVELS[4][1]
    el.outline(msp, [(1200, top * 1000.0), (3800, top * 1000.0),
                     (3800, z_top * 1000.0), (1200, z_top * 1000.0)], lw=35)
    el.opening(msp, 1900, top + 0.15, 1200, 1600, kind="louver")


NOTES = [
    ("SON NUOC NGOAI THAT", (2500, 9.4), (5800, 10.9)),
    ("OP DA GRANITE D.20", (350, 6.2), (5800, 7.9)),
    ("LAN CAN KINH 12mm", (2500, 4.25), (5800, 5.2)),
    ("CUA CUON NHOM", (2500, 0.9), (5800, 1.9)),
]


def _material_notes(msp):
    for text, (tx, tz), (nx, nz) in NOTES:
        el.label_material(msp, text, (tx, tz * 1000.0), (nx, nz * 1000.0), SC)


# ---------------------------------------------------------------------------
# MAT DUNG BEN TRUC 1-5 (nhin tu hong nha, dai 18m)
# ---------------------------------------------------------------------------
def draw_side_elevation(msp, doc):
    """Mat dung ben: duong bao theo chieu sau nha, buong thang nho len, ban cong."""
    top = L.LEVELS[3][1]
    z_top = L.LEVELS[4][1]
    el.ground_line(msp, 0, L.L, L.Z_DAT)
    el.outline(msp, [(0, L.Z_DAT * 1000.0), (L.L, L.Z_DAT * 1000.0),
                     (L.L, top * 1000.0), (0, top * 1000.0)])
    _side_balconies(msp)
    _side_openings(msp)
    el.parapet(msp, -110, L.L + 110, top, h=900)
    el.outline(msp, [(9500, top * 1000.0), (13000, top * 1000.0),
                     (13000, z_top * 1000.0), (9500, z_top * 1000.0)], lw=35)
    # mang op da o goc sau nha, khong de len ban cong phia truoc
    el.material(msp, [(16500, L.Z_DAT * 1000.0), (L.L, L.Z_DAT * 1000.0),
                      (L.L, top * 1000.0), (16500, top * 1000.0)],
                pattern="ANSI31", pscale=340.0, angle=45.0)
    el.storey_grid(msp, doc, L.LEVELS, -1200, L.L, SC, side="left", tail=900)
    for text, tx, tz, nx, nz in (
            ("Ban cong nho ra 1500", -750, 4.7, 20600, 8.4),
            ("Buong thang len mai", 11250, 12.0, 15600, 12.8),
            ("Op da Granite goc sau", 17200, 5.5, 19400, 7.4)):
        el.label_material(msp, text, (tx, tz * 1000.0), (nx, nz * 1000.0), SC)


def _side_balconies(msp):
    """Ban cong nho RA PHIA TRUOC nha (truc 1), tuc ve phia x am tren mat dung ben."""
    for _, z in L.LEVELS[1:3]:
        y = z * 1000.0
        msp.add_lwpolyline([(-1500, y), (0, y), (0, y - 150), (-1500, y - 150)],
                           close=True, dxfattribs={"layer": "A-ELEV"})
        msp.add_lwpolyline([(-1500, y - 150), (-1500, y + 900), (0, y + 900)],
                           dxfattribs={"layer": "A-RAILING"})
        msp.add_line((-1500, y + 840), (0, y + 840), dxfattribs={"layer": "A-RAILING"})


def _side_openings(msp):
    """Cua so hong nha mo ra gieng troi va san sau."""
    for _, z in L.LEVELS[0:3]:
        el.opening(msp, 9800, z + 1.05, 1400, 1600, kind="wind", panes=2)
        el.opening(msp, 15600, z + 1.05, 1600, 1600, kind="wind", panes=2)
