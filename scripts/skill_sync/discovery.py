"""Recursive, contained bundle discovery and content hashing."""

from __future__ import annotations

import hashlib
import os
import stat
from pathlib import Path

from .frontmatter import normalized_name
from .ignore_policy import ignored
from .models import FileState, Finding, ScanResult, SkillBundle


def _hash(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def _contained(path: Path, root: Path) -> bool:
    try:
        path.resolve().relative_to(root.resolve())
        return True
    except ValueError:
        return False


def _resolved_contained(path: Path, root: Path) -> bool:
    try:
        path.relative_to(root)
        return True
    except ValueError:
        return False


def is_reparse_point(path: Path) -> bool:
    junction_check = getattr(path, "is_junction", None)
    if callable(junction_check):
        try:
            if junction_check():
                return True
        except OSError:
            return True
    try:
        attributes = getattr(os.lstat(path), "st_file_attributes", 0)
    except OSError:
        return False
    return bool(attributes & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400))


def _safe_files(root: Path) -> tuple[list[Path], list[Finding]]:
    files: list[Path] = []
    findings: list[Finding] = []
    for directory, names, file_names in os.walk(root, followlinks=False):
        base = Path(directory)
        safe_names: list[str] = []
        for name in names:
            path = base / name
            relative = path.relative_to(root).as_posix()
            if ignored(relative):
                continue
            if path.is_symlink() or is_reparse_point(path) or not _contained(path, root):
                findings.append(Finding("error", "symlinked or escaping directory rejected", relative))
            else:
                safe_names.append(name)
        names[:] = safe_names
        for name in file_names:
            path = base / name
            relative = path.relative_to(root).as_posix()
            if ignored(relative):
                continue
            if path.is_symlink() or is_reparse_point(path) or not _contained(path, root):
                findings.append(Finding("error", "symlinked or escaping file rejected", relative))
            else:
                files.append(path)
    return sorted(files), findings


def discover(root: Path, provider: str) -> ScanResult:
    bundles: dict[str, SkillBundle] = {}
    if not root.exists():
        return ScanResult(bundles)
    if root.is_symlink() or is_reparse_point(root):
        return ScanResult(bundles, (Finding("error", "symlinked provider root rejected", root.name),))
    files, findings = _safe_files(root)
    skill_files = [path for path in files if path.name == "SKILL.md"]
    resolved_files = {path: path.resolve() for path in files}
    skill_roots = {resolved_files[path].parent for path in skill_files}
    for skill_file in skill_files:
        try:
            logical_id = normalized_name(skill_file.read_bytes())
        except (UnicodeDecodeError, ValueError) as error:
            findings.append(Finding("error", str(error), skill_file.relative_to(root).as_posix()))
            continue
        if logical_id in bundles:
            findings.append(Finding("error", f"duplicate declared skill name: {logical_id}", skill_file.relative_to(root).as_posix()))
            continue
        bundle_root = resolved_files[skill_file].parent
        contents: list[FileState] = []
        for path in files:
            resolved_path = resolved_files[path]
            if not _resolved_contained(resolved_path, bundle_root):
                continue
            if any(other != bundle_root and _resolved_contained(resolved_path, other) for other in skill_roots):
                continue
            # Both sides resolved: on Windows the provider root can arrive as an
            # 8.3 short path (C:\Users\ABC~1) while bundle_root is the long form.
            relative = resolved_path.relative_to(bundle_root).as_posix()
            content = path.read_bytes()
            contents.append(FileState(relative, content, _hash(content)))
        bundles[logical_id] = SkillBundle(logical_id, provider, bundle_root, bundle_root.relative_to(root.resolve()).as_posix(), tuple(contents))
    return ScanResult(bundles, tuple(findings))


def sha256(content: bytes) -> str:
    return _hash(content)
