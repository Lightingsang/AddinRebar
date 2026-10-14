"""Generate the Vietnamese charset golden table from UniKey's original vnconv/data.cpp.

The vendored VNConvert library (HPAutoCad/ThirdParty/VNConvert) is a machine-written port of UniKey's vnconv.
Comparing it against itself proves nothing, so the tests compare it against the tables Pham Kim Long wrote by hand
in vnconv/data.cpp. That C++ file is not committed; pass the path of an upstream checkout and the script refuses any
file whose sha256 differs from the pinned one.

Usage: python -I generate-golden-from-unikey-data.py <path-to-vnconv/data.cpp> <output.json>
"""

import hashlib
import json
import re
import sys

PINNED_SHA256 = "73dd473bd09dbdb4a0940d03a142e261810860fea4592be3cb9a7a3a15f74453"
UPSTREAM = "https://github.com/AutoLISP-Just-Simple/VNConvert/blob/205e569/vnconv/data.cpp"
TOTAL_VNCHARS = 213
TOKEN = re.compile(r"'(\\.|[^'])'|0x[0-9a-fA-F]+|\b\d+\b")


def strip_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    return "\n".join(line.split("//", 1)[0] for line in text.splitlines())


def array_body(text: str, declaration: str) -> str:
    start = text.index(declaration)
    end = text.index("};", start)
    return text[text.index("{", start):end]


def first_group(body: str) -> str:
    """Return the first `{...}` group of a two-dimensional table (the first charset)."""
    open_at = body.index("{", 1)  # body[0] is the outer brace
    return body[open_at + 1:body.index("}", open_at)]


def parse_values(group: str) -> list[int]:
    values = []
    for match in TOKEN.finditer(group):
        token = match.group(0)
        if token.startswith("'"):
            values.append(ord(match.group(1)[-1]))
        elif token.startswith("0x"):
            values.append(int(token, 16))
        else:
            values.append(int(token))
    if len(values) != TOTAL_VNCHARS:
        raise ValueError(f"expected {TOTAL_VNCHARS} values, parsed {len(values)}")
    return values


def main() -> int:
    source_path, output_path = sys.argv[1], sys.argv[2]
    raw = open(source_path, "rb").read()
    digest = hashlib.sha256(raw).hexdigest()
    if digest != PINNED_SHA256:
        print(f"refused: sha256 {digest} is not the pinned upstream data.cpp", file=sys.stderr)
        return 1

    # data.cpp stores TCVN3 letters as raw bytes; latin-1 maps every byte to the code point of the same value.
    text = strip_comments(raw.decode("latin-1"))
    tcvn3 = parse_values(first_group(array_body(text, "unsigned char SingleByteTables")))
    vni = parse_values(first_group(array_body(text, "WORD DoubleByteTables")))
    unicode = parse_values(array_body(text, "UnicodeChar UnicodeTable")[1:])

    entries = [
        {"index": i, "unicode": unicode[i], "tcvn3": tcvn3[i], "vniWin": vni[i]}
        for i in range(TOTAL_VNCHARS)
    ]
    document = {"source": UPSTREAM, "sha256": PINNED_SHA256, "entries": entries}
    # Compute first, then write: a failed parse must never truncate an existing golden file.
    payload = json.dumps(document, indent=1)
    with open(output_path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(payload + "\n")
    print(f"wrote {len(entries)} entries to {output_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
