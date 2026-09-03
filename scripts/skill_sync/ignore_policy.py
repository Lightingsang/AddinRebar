"""Centralized deterministic exclusion policy for bundle resources."""

from __future__ import annotations

from pathlib import PurePosixPath


DIRECTORIES = {
    ".venv", "venv", "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache",
    "node_modules", "dist", "build", "coverage", "logs", ".shadowed", ".skill-sync",
}
FILES = {".env", ".DS_Store", "Thumbs.db"}


def ignored(relative_path: str) -> bool:
    path = PurePosixPath(relative_path)
    if any(part in DIRECTORIES for part in path.parts):
        return True
    name = path.name
    return name in FILES or name.endswith(".secret") or name.endswith(".log") or name == "lock.json" or name.startswith("stage-")
