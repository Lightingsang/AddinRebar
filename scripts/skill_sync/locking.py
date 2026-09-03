"""Exclusive repository lock with token-checked ownership and recovery."""

from __future__ import annotations

import json
import os
import socket
import time
import uuid
from pathlib import Path

from . import TOOL_VERSION
from .advisory_lock import advisory_gate


class LockHeld(RuntimeError):
    pass


def _is_live(pid: int) -> bool:
    if pid <= 0:
        return False
    try:
        os.kill(pid, 0)
    except OSError:
        return False
    return True


class RepositoryLock:
    def __init__(self, state_dir: Path, stale_seconds: int) -> None:
        if stale_seconds <= 0:
            raise ValueError("lock stale threshold must be positive")
        self.path = state_dir / "lock.json"
        self.gate_path = state_dir / "lock.gate"
        self.stale_seconds = stale_seconds
        self.token = uuid.uuid4().hex
        self.owned = False

    def _payload(self) -> dict:
        return {"pid": os.getpid(), "host": socket.gethostname(), "started_at": time.time(), "tool_version": TOOL_VERSION, "token": self.token}

    def _read(self) -> dict | None:
        try:
            data = json.loads(self.path.read_text(encoding="utf-8"))
            return data if isinstance(data, dict) and isinstance(data.get("token"), str) else None
        except (OSError, ValueError, json.JSONDecodeError):
            return None

    def _recover_locked(self) -> bool:
        old = self._read()
        if old is None:
            return False
        try:
            age = time.time() - float(old["started_at"])
            same_host = old.get("host") == socket.gethostname()
            local_live = same_host and _is_live(int(old["pid"]))
        except (KeyError, TypeError, ValueError):
            return False
        if not same_host or local_live or age <= self.stale_seconds:
            return False
        # Re-read the identity immediately before removal; never remove a changed lock.
        current = self._read()
        if current is None or current.get("token") != old["token"]:
            return False
        quarantine = self.path.with_name(f"stale-{old['token']}.json")
        try:
            os.replace(self.path, quarantine)
        except FileNotFoundError:
            return False
        try:
            if json.loads(quarantine.read_text(encoding="utf-8")).get("token") != old["token"]:
                if not self.path.exists():
                    os.replace(quarantine, self.path)
                return False
        finally:
            if quarantine.exists():
                quarantine.unlink()
        return True

    def _recover_if_proven_stale(self) -> bool:
        with advisory_gate(self.gate_path):
            return self._recover_locked()

    def acquire(self) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        with advisory_gate(self.gate_path):
            try:
                descriptor = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
                with os.fdopen(descriptor, "w", encoding="utf-8") as handle:
                    json.dump(self._payload(), handle, sort_keys=True)
                    handle.flush()
                    os.fsync(handle.fileno())
                self.owned = True
                return
            except FileExistsError:
                if not self._recover_locked():
                    raise LockHeld("another apply holds the repository lock")
            descriptor = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            with os.fdopen(descriptor, "w", encoding="utf-8") as handle:
                json.dump(self._payload(), handle, sort_keys=True)
                handle.flush()
                os.fsync(handle.fileno())
            self.owned = True

    def release(self) -> None:
        if self.owned:
            with advisory_gate(self.gate_path):
                current = self._read()
                if current and current.get("token") == self.token:
                    try:
                        self.path.unlink()
                    except FileNotFoundError:
                        pass
            self.owned = False

    def __enter__(self) -> "RepositoryLock":
        self.acquire()
        return self

    def __exit__(self, *_: object) -> None:
        self.release()
