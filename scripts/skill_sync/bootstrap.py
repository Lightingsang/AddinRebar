"""Evidence-gated initialization for an audited pre-existing portable tree."""

from __future__ import annotations

import re
from pathlib import Path, PurePosixPath, PureWindowsPath

from .change_detection import _bundle_record, _record
from .models import ChangePlan, FileState, Finding, PlannedWrite, SkillBundle, SyncConfig
from .paths import safe_artifact_id


def _error(message: str) -> ChangePlan:
    return ChangePlan(manifest={"schema_version": 1, "skills": {}}, findings=[Finding("error", f"bootstrap evidence: {message}")])


def _evidence(config: SyncConfig) -> tuple[dict, str | None]:
    evidence = config.bootstrap
    if not isinstance(evidence, dict):
        return {}, "configuration is missing"
    if "audit_path" in evidence:
        raw = evidence["audit_path"]
        if not isinstance(raw, str) or not raw:
            return {}, "audit_path is invalid"
        path = Path(raw)
        windows, posix = PureWindowsPath(raw), PurePosixPath(raw)
        if windows.drive or windows.root or posix.is_absolute() or ".." in path.parts:
            return {}, "audit_path is not repository-relative"
        try:
            audit_path = (config.root / path).resolve()
            audit_path.relative_to(config.root.resolve())
            text = audit_path.read_text(encoding="utf-8")
        except (OSError, ValueError, UnicodeDecodeError):
            return {}, "audit_path cannot be read"
        identities = re.findall(r"^\| ([^|]+) \| `([^`]+/SKILL\.md)` \|$", text, re.MULTILINE)
        try:
            divergent_table = text.split("| Relative skill | C SHA-256 | P SHA-256 |", 1)[1].split("**Inference:**", 1)[0]
        except IndexError:
            return {}, "audit divergent-pair table is missing"
        divergent = []
        for line in divergent_table.splitlines():
            if not line.startswith("| ") or line.startswith("| ---"):
                continue
            match = re.fullmatch(r"\| ([^|]+) \| `([0-9a-f]{64})` \| `([0-9a-f]{64})` \|", line)
            if match is None:
                return {}, "audit divergent-pair record is malformed"
            divergent.append(match.groups())
        expected_paths = {name: relative.removesuffix("/SKILL.md") for name, relative in identities}
        path_to_id = {path: logical_id for logical_id, path in expected_paths.items()}
        rows = []
        for relative_root, claude_hash, portable_hash in divergent:
            relative_path = f"{relative_root}/SKILL.md"
            logical_id = path_to_id.get(relative_root)
            if logical_id is None:
                return {}, "audit divergent path lacks a declared logical ID"
            rows.append({"logical_id": logical_id, "relative_path": relative_path, "claude_sha256": claude_hash, "portable_sha256": portable_hash})
        evidence = {"expected_skill_count": len(expected_paths), "expected_skill_paths": expected_paths, "expected_divergent_pairs": rows}
    if not isinstance(evidence.get("expected_skill_count"), int) or evidence["expected_skill_count"] < 1:
        return {}, "expected_skill_count is invalid"
    if not isinstance(evidence.get("expected_skill_paths"), dict) or not isinstance(evidence.get("expected_divergent_pairs"), list):
        return {}, "expected paths or divergent pairs are invalid"
    return evidence, None


def _file_map(bundle: SkillBundle) -> dict[str, FileState]:
    return {file.relative_path: file for file in bundle.files}


def _report(rows: list[dict]) -> bytes:
    lines = ["# Bootstrap portable differences", "", "Verified snapshots created before portable normalization.", "", "| Logical ID | Path | Claude SHA-256 | Portable SHA-256 |", "| --- | --- | --- | --- |"]
    lines.extend(f"| {row['logical_id']} | `{row['relative_path']}` | `{row['claude_sha256']}` | `{row['portable_sha256']}` |" for row in rows)
    return ("\n".join(lines) + "\n").encode("utf-8")


def build_bootstrap_plan(config: SyncConfig, claude_bundles: dict[str, SkillBundle], portable_bundles: dict[str, SkillBundle]) -> ChangePlan:
    """Create snapshots and first bases only after the complete audit matches disk."""
    evidence, error = _evidence(config)
    if error:
        return _error(error)
    expected_paths = evidence["expected_skill_paths"]
    if set(claude_bundles) != set(portable_bundles) or len(claude_bundles) != evidence["expected_skill_count"]:
        return _error("skill count or provider identity set does not match the audit")
    if set(expected_paths) != set(claude_bundles) or any(
        expected_paths[logical_id] != claude_bundles[logical_id].relative_root or expected_paths[logical_id] != portable_bundles[logical_id].relative_root
        for logical_id in expected_paths
    ):
        return _error("logical IDs or relative skill paths do not match the audit")
    expected: dict[tuple[str, str], dict] = {}
    for row in evidence["expected_divergent_pairs"]:
        if not isinstance(row, dict) or not all(isinstance(row.get(key), str) for key in ("logical_id", "relative_path", "claude_sha256", "portable_sha256")):
            return _error("divergent-pair record is invalid")
        key = (row["logical_id"].casefold(), row["relative_path"])
        if key in expected:
            return _error("divergent-pair records are duplicated")
        expected[key] = row
    actual: list[dict] = []
    for logical_id in sorted(claude_bundles):
        claude, portable = claude_bundles[logical_id], portable_bundles[logical_id]
        claude_files, portable_files = _file_map(claude), _file_map(portable)
        if set(claude_files) != set(portable_files):
            return _error(f"bundle file paths differ for {logical_id}")
        for relative_path in sorted(claude_files):
            current_c, current_p = claude_files[relative_path], portable_files[relative_path]
            if current_c.sha256 == current_p.sha256:
                continue
            row = {"logical_id": logical_id, "relative_path": f"{claude.relative_root}/{relative_path}", "claude_sha256": current_c.sha256, "portable_sha256": current_p.sha256}
            actual.append(row)
            audit = expected.get((logical_id, row["relative_path"]))
            if audit != row or relative_path != "SKILL.md":
                return _error(f"hash or path mismatch for {logical_id}/{relative_path}")
    if {(row["logical_id"], row["relative_path"]) for row in actual} != set(expected):
        return _error("divergent-pair count, paths, or hashes do not match the audit")
    pending_ids = {row["logical_id"] for row in actual}
    plan = ChangePlan(manifest={"schema_version": 1, "skills": {}})
    for row in actual:
        logical_id, claude, portable = row["logical_id"], claude_bundles[row["logical_id"]], portable_bundles[row["logical_id"]]
        c_file = _file_map(claude)["SKILL.md"]
        p_file = _file_map(portable)["SKILL.md"]
        directory = config.state_dir / "conflicts" / "bootstrap" / safe_artifact_id(logical_id)
        plan.writes.extend([
            PlannedWrite(directory / "portable.SKILL.md", p_file.content),
            PlannedWrite(directory / "claude.SKILL.md", c_file.content),
        ])
    if actual:
        plan.writes.append(PlannedWrite(config.state_dir / "reports" / "bootstrap-portable-differences.md", _report(actual)))
    for logical_id in sorted(claude_bundles):
        claude, portable = claude_bundles[logical_id], portable_bundles[logical_id]
        files: dict[str, dict] = {}
        for relative_path, c_file in _file_map(claude).items():
            p_file = _file_map(portable)[relative_path]
            # Bootstrap records raw audited bytes as the legacy representation.
            # The next apply must run the current provider adapter over every bundle,
            # including byte-identical skills that still contain legacy contracts.
            files[relative_path] = _record(c_file, p_file, "claude-to-codex:skill-markdown-v1")
        status = "bootstrap-pending-seed" if logical_id in pending_ids else "synchronized"
        if status == "bootstrap-pending-seed":
            plan.pending.append(logical_id)
        plan.manifest["skills"][logical_id] = _bundle_record(config, claude, portable, files, status)
    return plan
