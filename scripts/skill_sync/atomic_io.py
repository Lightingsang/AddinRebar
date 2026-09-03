"""Same-filesystem staging and atomic replacement primitives."""

from __future__ import annotations

import os
import json
import shutil
import socket
import tempfile
import time
import uuid
from pathlib import Path

from . import TOOL_VERSION
from .discovery import is_reparse_point


def create_staging(state_dir: Path) -> Path:
    state_dir.mkdir(parents=True, exist_ok=True)
    staging = Path(tempfile.mkdtemp(prefix="stage-", dir=state_dir))
    metadata = {"host": socket.gethostname(), "pid": os.getpid(), "started_at": time.time(), "token": uuid.uuid4().hex, "tool_version": TOOL_VERSION}
    (staging / ".stage-owner.json").write_text(json.dumps(metadata, sort_keys=True), encoding="utf-8")
    return staging


def stage_file(staging: Path, destination: Path, content: bytes, root: Path) -> Path:
    relative = destination.resolve().relative_to(root.resolve())
    staged = staging / relative
    staged.parent.mkdir(parents=True, exist_ok=True)
    with staged.open("wb") as handle:
        handle.write(content)
        handle.flush()
        os.fsync(handle.fileno())
    return staged


def replace(staged: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    try:
        os.replace(staged, destination)
    except PermissionError:
        # Some Windows-managed project trees grant file write but deny
        # delete/rename. The candidate has already been fully validated, so
        # preserve progress with a durable in-place write rather than bypassing
        # validation or changing directory ACLs.
        content = staged.read_bytes()
        with destination.open("wb") as handle:
            handle.write(content)
            handle.flush()
            os.fsync(handle.fileno())


def cleanup(staging: Path | None) -> None:
    if staging and staging.exists():
        shutil.rmtree(staging, ignore_errors=True)


def _is_live(pid: int) -> bool:
    if pid <= 0:
        return False
    try:
        os.kill(pid, 0)
    except OSError:
        return False
    return True


def recover_abandoned_staging(state_dir: Path, stale_seconds: int) -> dict[str, list[str]]:
    """Recover only same-host, dead, old stages with valid ownership proof."""
    if not state_dir.exists():
        return {"recovered": [], "retained": []}
    recovered: list[str] = []
    retained: list[str] = []
    for path in sorted(state_dir.iterdir()):
        if not path.name.startswith("stage-"):
            continue
        if path.is_symlink() or is_reparse_point(path) or not path.is_dir():
            retained.append(path.name)
            continue
        try:
            metadata = json.loads((path / ".stage-owner.json").read_text(encoding="utf-8"))
            pid = int(metadata["pid"])
            age = time.time() - float(metadata["started_at"])
            proven = (
                isinstance(metadata.get("token"), str)
                and bool(metadata["token"])
                and metadata.get("tool_version") == TOOL_VERSION
                and metadata.get("host") == socket.gethostname()
                and not _is_live(pid)
                and age > stale_seconds > 0
            )
        except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError):
            proven = False
        if proven:
            shutil.rmtree(path, ignore_errors=False)
            recovered.append(path.name)
        else:
            retained.append(path.name)
    return {"recovered": recovered, "retained": retained}
