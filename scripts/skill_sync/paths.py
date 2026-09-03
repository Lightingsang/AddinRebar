"""Configuration and repository-relative path handling."""

from __future__ import annotations

import json
import os
from pathlib import Path
from pathlib import PurePosixPath, PureWindowsPath

from .models import SyncConfig


def load_config(root: Path, config_path: Path | None) -> SyncConfig:
    root = root.resolve()
    if root.is_symlink():
        raise ValueError("repository root cannot be a symlink")
    source = (config_path or root / ".skill-sync" / "config.json").resolve()
    data = json.loads(source.read_text(encoding="utf-8"))
    if data.get("schema_version") != 1:
        raise ValueError("config schema_version must be 1")
    def relative(key: str) -> Path:
        raw = data[key]
        if not isinstance(raw, str) or not raw:
            raise ValueError(f"{key} must be a non-empty repository-relative path")
        windows, posix, value = PureWindowsPath(raw), PurePosixPath(raw), Path(raw)
        if windows.drive or windows.root or posix.is_absolute() or value.is_absolute() or ".." in value.parts:
            raise ValueError(f"{key} must be a repository-relative path")
        candidate = root / value
        resolved = candidate.resolve()
        try:
            resolved.relative_to(root)
        except ValueError as error:
            raise ValueError(f"{key} escapes repository root") from error
        cursor = root
        for part in value.parts:
            cursor /= part
            if cursor.exists() and os.path.islink(cursor):
                raise ValueError(f"{key} cannot use symlinked paths")
        return candidate
    return SyncConfig(
        root=root,
        claude_dir=relative("claude_dir"),
        portable_dir=relative("portable_dir"),
        state_dir=relative("state_dir"),
        lock_stale_seconds=int(data.get("lock_stale_seconds", 3600)),
        bootstrap=data.get("bootstrap"),
    )


def display_path(root: Path, path: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def safe_artifact_id(logical_id: str) -> str:
    """Return a deterministic Windows-safe identifier without trusting a name."""
    import hashlib

    readable = "".join(char if char.isalnum() or char in "-_" else "-" for char in logical_id).strip("-") or "skill"
    device_stem = logical_id.rstrip(" .").split(".", 1)[0].upper()
    reserved = device_stem in {"CON", "PRN", "AUX", "NUL"} or device_stem in {f"COM{i}" for i in range(1, 10)} or device_stem in {f"LPT{i}" for i in range(1, 10)}
    if readable == logical_id and len(readable) <= 64 and not reserved:
        return readable
    return f"{readable[:48]}-{hashlib.sha256(logical_id.encode('utf-8')).hexdigest()[:12]}"
