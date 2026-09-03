"""Command line entrypoint for the safe skill synchronization engine."""

from __future__ import annotations

import argparse
import os
from pathlib import Path

from .atomic_io import cleanup, create_staging, recover_abandoned_staging, replace, stage_file
from .bootstrap import build_bootstrap_plan
from .candidate_validation import validate_candidates
from .change_detection import build_plan
from .discovery import discover
from .frontmatter import parse_frontmatter
from .locking import LockHeld, RepositoryLock
from .manifest_store import deterministic_json, load_manifest
from .paths import load_config, safe_artifact_id
from .reporting import emit
from .validation import validate_all


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="sync-agent-skills")
    parser.add_argument("command", choices=("scan", "status", "check", "apply", "validate"))
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--config", type=Path)
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--fault-after-stage", action="store_true", help=argparse.SUPPRESS)
    return parser


def _scan(config: object):
    claude = discover(config.claude_dir, "claude")
    portable = discover(config.portable_dir, "portable")
    return claude, portable


def _plan(config: object):
    claude, portable = _scan(config)
    manifest = load_manifest(config.state_dir)
    plan = build_plan(config, claude.bundles, portable.bundles, manifest)
    plan.findings.extend(claude.findings + portable.findings)
    validation = validate_all(claude.bundles, config.claude_dir)
    portable_validation = validate_all(portable.bundles, config.portable_dir)
    plan.findings.extend(validation.findings + portable_validation.findings)
    return claude, portable, manifest, plan, validation, portable_validation


def _payload(command: str, claude: object, portable: object, plan: object, shared: tuple[tuple[str, str, str], ...] = ()) -> dict:
    return {
        "command": command,
        "logical_skill_ids": sorted(set(claude.bundles) | set(portable.bundles)),
        "drift": sorted(plan.drift),
        "deletions": sorted(plan.deletions),
        "conflicts": sorted({conflict["logical_skill_id"] for conflict in plan.conflicts}),
        "findings": [finding.message for finding in plan.findings],
        "pending": sorted(plan.pending),
        "shared_dependencies": sorted({edge[2] for edge in shared}),
        "shared_dependency_edges": [list(edge) for edge in sorted(shared)],
    }


def _write_conflicts(config: object, plan: object) -> None:
    for conflict in plan.conflicts:
        directory = config.state_dir / "conflicts" / safe_artifact_id(conflict["logical_skill_id"])
        relative = Path(conflict["relative_path"])
        if relative.is_absolute() or ".." in relative.parts:
            raise ValueError("unsafe conflict relative path")
        metadata = {
            "adapter": conflict["adapter"],
            "claude_sha256": conflict["claude_sha256"],
            "logical_skill_id": conflict["logical_skill_id"],
            "portable_sha256": conflict["portable_sha256"],
            "relative_path": relative.as_posix(),
            "resolution": "Inspect both preserved copies, reconcile intentionally, then rerun apply.",
        }
        staging = create_staging(config.state_dir)
        try:
            staged = [
                (stage_file(staging, directory / "claude" / relative, conflict["claude"], config.state_dir), directory / "claude" / relative),
                (stage_file(staging, directory / "portable" / relative, conflict["portable"], config.state_dir), directory / "portable" / relative),
                (stage_file(staging, directory / "metadata.json", deterministic_json(metadata), config.state_dir), directory / "metadata.json"),
            ]
            for source, destination in staged:
                replace(source, destination)
        finally:
            cleanup(staging)


def _apply(config: object, fault_after_stage: bool) -> tuple[int, dict]:
    if os.environ.get("SKILL_SYNC_ACTIVE") == "1":
        return 4, {"command": "apply", "error": "reentrant synchronization is blocked"}
    previous = os.environ.get("SKILL_SYNC_ACTIVE")
    os.environ["SKILL_SYNC_ACTIVE"] = "1"
    try:
        with RepositoryLock(config.state_dir, config.lock_stale_seconds):
            residue = recover_abandoned_staging(config.state_dir, config.lock_stale_seconds)
            claude, portable, old_manifest, plan, validation, portable_validation = _plan(config)
            manifest_path = config.state_dir / "manifest.json"
            if not manifest_path.exists() and config.bootstrap:
                plan = build_bootstrap_plan(config, claude.bundles, portable.bundles)
            payload = _payload("apply", claude, portable, plan, tuple(sorted(set(validation.shared_dependencies) | set(portable_validation.shared_dependencies))))
            payload["residue"] = sorted(residue["recovered"] + residue["retained"])
            payload["residue_recovered"] = residue["recovered"]
            payload["residue_retained"] = residue["retained"]
            pending_noop = bool(plan.pending) and not plan.writes
            # Existing provider findings may be the exact debt repaired by a
            # planned adapter upgrade. Permit staging only when writes exist;
            # the fully overlaid candidate is validated before any replacement.
            if not plan.valid and not plan.writes and not pending_noop:
                return 2, payload
            if plan.conflicts:
                _write_conflicts(config, plan)
                return 3, payload
            staging = create_staging(config.state_dir)
            try:
                staged = []
                for write in plan.writes:
                    if write.destination.name == "SKILL.md":
                        parse_frontmatter(write.content)
                    staged.append((stage_file(staging, write.destination, write.content, config.root), write.destination))
                provider_writes = [
                    write for write in plan.writes
                    if _provider_destination(config, write.destination)
                ]
                candidate_findings = validate_candidates(config, staging, provider_writes) if provider_writes else ()
                if candidate_findings:
                    plan.findings.extend(candidate_findings)
                    payload["findings"] = [finding.message for finding in plan.findings]
                    return 2, payload
                manifest_changed = deterministic_json(plan.manifest) != deterministic_json(old_manifest)
                manifest_destination = config.state_dir / "manifest.json"
                if manifest_changed:
                    staged_manifest = stage_file(staging, manifest_destination, deterministic_json(plan.manifest), config.root)
                else:
                    staged_manifest = None
                if fault_after_stage:
                    raise RuntimeError("fault injected after staging")
                for source, destination in staged:
                    replace(source, destination)
                if staged_manifest:
                    replace(staged_manifest, manifest_destination)
            finally:
                cleanup(staging)
            return 0, payload
    except LockHeld as error:
        return 4, {"command": "apply", "error": str(error)}
    finally:
        if previous is None:
            os.environ.pop("SKILL_SYNC_ACTIVE", None)
        else:
            os.environ["SKILL_SYNC_ACTIVE"] = previous


def _provider_destination(config: object, destination: Path) -> bool:
    try:
        destination.resolve().relative_to(config.claude_dir.resolve())
        return True
    except ValueError:
        try:
            destination.resolve().relative_to(config.portable_dir.resolve())
            return True
        except ValueError:
            return False


def main(argv: list[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        config = load_config(args.root, args.config)
        if args.command == "apply":
            code, payload = _apply(config, args.fault_after_stage)
        else:
            claude, portable, _, plan, validation, portable_validation = _plan(config)
            shared = tuple(sorted(set(validation.shared_dependencies) | set(portable_validation.shared_dependencies)))
            payload = _payload(args.command, claude, portable, plan, shared)
            if args.command == "scan":
                code = 0 if plan.valid else 2
            elif args.command == "validate":
                code = 0 if plan.valid else 2
            elif args.command == "check":
                code = 0 if plan.valid and not plan.drift and not plan.deletions and not plan.conflicts and not plan.pending else 1
            else:
                code = 0 if plan.valid and not plan.conflicts else 3
        print(emit(payload, args.json), end="")
        return code
    except Exception as error:
        print(emit({"command": args.command, "error": str(error)}, args.json), end="")
        return 2
