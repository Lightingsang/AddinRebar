"""Minimal YAML-frontmatter reader without third-party dependencies."""

from __future__ import annotations

import re


_FIELD = re.compile(r"^([A-Za-z][A-Za-z0-9_-]*):\s*(.*?)\s*$")
_BLOCK_SCALAR_HEADER = re.compile(r"^[>|][+-]?[0-9]*(?:\s+#.*)?$")


def parse_frontmatter(content: bytes) -> dict[str, str]:
    text = content.decode("utf-8")
    if not text.startswith("---\n") and not text.startswith("---\r\n"):
        raise ValueError("SKILL.md requires YAML frontmatter")
    lines = text.replace("\r\n", "\n").split("\n")
    try:
        end = lines.index("---", 1)
    except ValueError as error:
        raise ValueError("SKILL.md frontmatter is not closed") from error
    fields: dict[str, str] = {}
    index = 1
    while index < end:
        line = lines[index]
        match = _FIELD.match(line)
        if match:
            key, raw_value = match.group(1), match.group(2)
            if raw_value.startswith((">", "|")):
                if not _BLOCK_SCALAR_HEADER.fullmatch(raw_value):
                    raise ValueError(f"SKILL.md frontmatter has invalid YAML block scalar for {key}")
                continuation: list[str] = []
                index += 1
                while index < end and (lines[index].startswith(" ") or not lines[index].strip()):
                    if lines[index].strip():
                        continuation.append(lines[index].strip())
                    index += 1
                fields[key] = " ".join(continuation)
                continue
            fields[key] = raw_value.strip("\"'")
        index += 1
    if not fields.get("name") or not fields.get("description"):
        raise ValueError("SKILL.md frontmatter requires non-empty name and description")
    return fields


def normalized_name(content: bytes) -> str:
    return parse_frontmatter(content)["name"].strip().casefold()
