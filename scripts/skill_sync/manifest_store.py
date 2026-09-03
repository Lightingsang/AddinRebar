"""Deterministic manifest load/build helpers."""

from __future__ import annotations

import json
import re
from pathlib import Path
from pathlib import PurePosixPath, PureWindowsPath

from .models import Manifest, ManifestFileRecord, ManifestSkillRecord


_SHA256 = re.compile(r"^[0-9a-f]{64}$")
_STATUSES = {"synchronized", "bootstrap-pending-seed"}


def _safe_relative_path(value: object) -> bool:
    if not isinstance(value, str) or not value:
        return False
    windows, posix = PureWindowsPath(value), PurePosixPath(value)
    components = value.replace("\\", "/").split("/")
    return not (
        windows.drive
        or windows.root
        or posix.is_absolute()
        or any(part in {"", ".", ".."} for part in components)
        or ".." in windows.parts
        or ".." in posix.parts
    )


def empty_manifest() -> Manifest:
    return Manifest(1, {})


def _validate_manifest(data: object) -> Manifest:
    if not isinstance(data, dict) or data.get("schema_version") != 1 or not isinstance(data.get("skills"), dict):
        raise ValueError("invalid manifest")
    typed_skills: dict[str, ManifestSkillRecord] = {}
    for logical_id, skill in data["skills"].items():
        if not isinstance(logical_id, str) or not logical_id or not isinstance(skill, dict):
            raise ValueError("invalid manifest skill record")
        if not all(isinstance(skill.get(key), str) and skill[key] for key in ("claude_path", "portable_path", "status")):
            raise ValueError("invalid manifest skill paths")
        if skill["status"] not in _STATUSES:
            raise ValueError("invalid manifest skill status")
        for key in ("claude_path", "portable_path"):
            if not _safe_relative_path(skill[key]):
                raise ValueError("invalid manifest provider path")
        files = skill.get("files")
        if not isinstance(files, dict):
            raise ValueError("invalid manifest files")
        typed_files: dict[str, ManifestFileRecord] = {}
        for relative_path, record in files.items():
            if not _safe_relative_path(relative_path):
                raise ValueError("invalid manifest file path")
            if not isinstance(record, dict) or not isinstance(record.get("adapter"), str) or not record["adapter"]:
                raise ValueError("invalid manifest file record")
            for key in ("claude_base_sha256", "portable_base_sha256", "semantic_base_sha256"):
                if not isinstance(record.get(key), str) or not _SHA256.fullmatch(record[key]):
                    raise ValueError("invalid manifest hash")
            typed_files[relative_path] = ManifestFileRecord(
                record["claude_base_sha256"], record["portable_base_sha256"], record["semantic_base_sha256"], record["adapter"]
            )
        typed_skills[logical_id] = ManifestSkillRecord(skill["claude_path"], skill["portable_path"], typed_files, skill["status"])
    return Manifest(1, typed_skills)


def load_manifest(state_dir: Path) -> Manifest:
    path = state_dir / "manifest.json"
    if not path.exists():
        return empty_manifest()
    return _validate_manifest(json.loads(path.read_text(encoding="utf-8")))


def deterministic_json(data: dict | Manifest) -> bytes:
    if isinstance(data, Manifest):
        data = data.to_dict()
    return (json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n").encode("utf-8")
