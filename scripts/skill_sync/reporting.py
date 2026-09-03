"""Stable human and JSON reports."""

from __future__ import annotations

import json


def emit(payload: dict, as_json: bool) -> str:
    if as_json:
        return json.dumps(payload, ensure_ascii=False, sort_keys=True) + "\n"
    lines: list[str] = []
    for key in sorted(payload):
        value = payload[key]
        if isinstance(value, list):
            lines.append(f"{key}: {', '.join(str(item) for item in value) if value else 'none'}")
        else:
            lines.append(f"{key}: {value}")
    return "\n".join(lines) + "\n"
