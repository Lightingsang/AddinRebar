"""Validate fully materialized destination candidates before replacement."""

from __future__ import annotations

import shutil
from pathlib import Path

from .adapters import adapter_for
from .discovery import discover
from .models import Finding, PlannedWrite, SyncConfig
from .validation import validate_all


def _copy(source: Path, destination: Path) -> None:
    if source.exists():
        # Candidate validation needs file bytes, not source ACL/timestamps.
        # copy2/copytree's default metadata copy can fail on provider-managed
        # trees even when their content is readable.
        shutil.copytree(source, destination, dirs_exist_ok=True, copy_function=shutil.copyfile)
    else:
        destination.mkdir(parents=True, exist_ok=True)


def validate_candidates(config: SyncConfig, staging: Path, writes: list[PlannedWrite]) -> tuple[Finding, ...]:
    """Overlay writes on provider copies, then validate every resulting bundle."""
    candidate = staging / "candidate"
    claude_root, portable_root = candidate / "claude", candidate / "portable"
    _copy(config.claude_dir, claude_root)
    _copy(config.portable_dir, portable_root)
    changed_providers: set[str] = set()
    for write in writes:
        try:
            relative = write.destination.resolve().relative_to(config.claude_dir.resolve())
            target = claude_root / relative
            changed_providers.add("claude")
        except ValueError:
            try:
                relative = write.destination.resolve().relative_to(config.portable_dir.resolve())
                target = portable_root / relative
                changed_providers.add("portable")
            except ValueError:
                continue
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(write.content)
    findings: list[Finding] = []
    for provider, root in (("claude", claude_root), ("portable", portable_root)):
        if provider not in changed_providers:
            continue
        scan = discover(root, provider)
        findings.extend(scan.findings)
        report = validate_all(scan.bundles, root)
        findings.extend(report.findings)
        adapter = adapter_for(provider)
        for bundle in scan.bundles.values():
            for file in bundle.files:
                findings.extend(adapter.validate(provider, bundle, file.relative_path, file.content))
    return tuple(findings)
