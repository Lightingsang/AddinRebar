"""Minimal YAML-frontmatter reader without third-party dependencies."""

from __future__ import annotations

import re


_FIELD = re.compile(r"^([A-Za-z][A-Za-z0-9_-]*):\s*(.*?)\s*$")


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
    for line in lines[1:end]:
        match = _FIELD.match(line)
        if match:
            fields[match.group(1)] = match.group(2).strip('"\'')
    if not fields.get("name") or not fields.get("description"):
        raise ValueError("SKILL.md frontmatter requires non-empty name and description")
    return fields


def normalized_name(content: bytes) -> str:
    return parse_frontmatter(content)["name"].strip().casefold()
