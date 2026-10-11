"""Read the Navisworks clash report (Report tab, HTML (Tabular), all tests combined) written by the seed
bim_export_clash_snapshot beside its snapshot, and index each clash's picture by (test name, clash name).

The page holds, per test, a summary table (<td class="testName">) followed by the main table: a header row of
generalHeader cells (Image, Clash Name, Status, Distance, Grid Location, Priority, Date Found, Clash Point, ...) and one
row per clash whose cells line up one-to-one with those headers (their colspans mirror each other). Pictures live in
<report>_files/cdNNNNNN.jpg. A main table only belongs to the summary right before it: the test name is forgotten at
the end of each main table, so a table without its own summary yields "no picture", never another test's picture.
"""

from html.parser import HTMLParser
from pathlib import Path

ROW_CLASSES = {"contentRow", "childRow", "childRowLast"}
PICTURE_SUFFIXES = (".jpg", ".jpeg", ".png")


class _ReportParser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.entries = {}
        self.headers_seen = False
        self.test = None
        self.tables = []          # class of every open table, innermost last
        self.headers = []
        self.row_kind = None
        self.cells = []
        self.cell = None
        self.cell_class = None
        self.image = None

    def in_main_row(self):
        return len(self.tables) >= 1 and self.tables[-1] == "mainTable"

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "table":
            self.tables.append(attrs.get("class"))
            if attrs.get("class") == "mainTable":
                self.headers = []
        elif tag == "tr" and self.in_main_row():
            self.row_kind = attrs.get("class")
            self.cells = []
        elif tag == "td":
            if self.in_main_row() or self.tables[-1:] == ["testSummaryTable"]:
                self.cell, self.cell_class, self.image = [], attrs.get("class"), None
        elif tag == "img" and self.cell is not None:
            source = (attrs.get("src") or "").strip()
            if source.lower().endswith(PICTURE_SUFFIXES):
                self.image = source

    def handle_data(self, data):
        if self.cell is not None:
            self.cell.append(data)

    def handle_endtag(self, tag):
        if tag == "td" and self.cell is not None:
            text = " ".join("".join(self.cell).split())
            if self.cell_class == "testName":
                self.test = text
            elif self.in_main_row():
                self.cells.append((self.cell_class, text, self.image))
            self.cell = None
        elif tag == "tr" and self.in_main_row():
            self._row_done()
        elif tag == "table" and self.tables:
            if self.tables.pop() == "mainTable":
                self.test = None

    def _row_done(self):
        if not self.cells:
            return
        texts = [text for _, text, _ in self.cells]
        if "Clash Name" in texts and all(kind == "generalHeader" or (kind or "").startswith("item") for kind, _, _ in self.cells):
            self.headers = texts
            self.headers_seen = True
            return
        if self.row_kind not in ROW_CLASSES or not self.headers or self.test is None:
            return
        values = dict(zip(self.headers, self.cells))
        name = values.get("Clash Name", (None, "", None))[1]
        if name:
            self.entries[(self.test, name)] = {
                "image": values.get("Image", (None, "", None))[2],
                "grid": values.get("Grid Location", (None, "", None))[1],
                "found": values.get("Date Found", (None, "", None))[1],
            }


def clash_pictures(html_path):
    """{(test, clash name): {image: Path|None, grid, found}} from one HTML report and a list of warnings."""
    html_path = Path(html_path)
    if not html_path.is_file():
        return {}, [f"report HTML not found: {html_path}"]
    parser = _ReportParser()
    parser.feed(html_path.read_text(encoding="utf-8", errors="replace"))
    warnings = []
    if not parser.headers_seen:
        warnings.append(f"{html_path.name} has no 'Clash Name' column: not an English HTML (Tabular) clash report")
    for entry in parser.entries.values():
        entry["image"] = (html_path.parent / entry["image"].replace("\\", "/")) if entry["image"] else None
    return parser.entries, warnings
