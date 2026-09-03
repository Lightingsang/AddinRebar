"""Three-way, base-hash change planning without filesystem mutation."""

from __future__ import annotations

from .adapters import adapter_for, conversion_id, supported_conversion_ids
from .discovery import sha256
from .models import ChangePlan, FileState, PlannedWrite, SkillBundle, SyncConfig


def _files(bundle: SkillBundle | None) -> dict[str, FileState]:
    return {file.relative_path: file for file in bundle.files} if bundle else {}


def _record(claude: FileState, portable: FileState, adapter_id: str) -> dict:
    semantic = sha256(adapter_for("claude").normalize(claude.relative_path, claude.content))
    return {
        "claude_base_sha256": claude.sha256,
        "portable_base_sha256": portable.sha256,
        "semantic_base_sha256": semantic,
        "adapter": adapter_id,
    }


def _bundle_record(config: SyncConfig, claude: SkillBundle, portable: SkillBundle, files: dict, status: str = "synchronized") -> dict:
    return {
        "claude_path": (config.claude_dir.relative_to(config.root) / claude.relative_root).as_posix(),
        "portable_path": (config.portable_dir.relative_to(config.root) / portable.relative_root).as_posix(),
        "files": files,
        "status": status,
    }


def _semantic(provider: str, relative_path: str, content: bytes) -> bytes:
    return adapter_for(provider).normalize(relative_path, content)


def _conflict(plan: ChangePlan, logical_id: str, relative_path: str, claude: FileState, portable: FileState) -> None:
    plan.conflicts.append({
        "logical_skill_id": logical_id,
        "relative_path": relative_path,
        "claude": claude.content,
        "portable": portable.content,
        "claude_sha256": claude.sha256,
        "portable_sha256": portable.sha256,
        "adapter": conversion_id("claude", "portable"),
    })


def _destination(bundle: SkillBundle, relative_path: str) -> object:
    return bundle.root / relative_path


def build_plan(config: SyncConfig, claude_bundles: dict[str, SkillBundle], portable_bundles: dict[str, SkillBundle], manifest: object) -> ChangePlan:
    plan = ChangePlan(manifest={"schema_version": 1, "skills": {}})
    old_skills = manifest.to_dict()["skills"] if hasattr(manifest, "to_dict") else manifest.get("skills", {})
    for logical_id in sorted(set(claude_bundles) | set(portable_bundles)):
        claude, portable = claude_bundles.get(logical_id), portable_bundles.get(logical_id)
        old = old_skills.get(logical_id)
        if old and (not claude or not portable):
            plan.deletions.append(f"one-sided-deletion: {logical_id}")
            plan.manifest["skills"][logical_id] = old
            continue
        if claude is None or portable is None:
            source, target = (claude, portable) if claude else (portable, claude)
            assert source is not None
            provider = "claude" if claude else "portable"
            target_root = config.portable_dir if provider == "claude" else config.claude_dir
            target = SkillBundle(source.logical_id, "portable" if provider == "claude" else "claude", target_root / source.relative_root, source.relative_root, ())
            rendered: dict[str, FileState] = {}
            for file in source.files:
                content = adapter_for(target.provider).render(provider, target.provider, source, file.relative_path, file.content)
                rendered[file.relative_path] = FileState(file.relative_path, content, sha256(content))
                plan.writes.append(PlannedWrite(target.root / file.relative_path, content))
                plan.drift.append(f"new-{provider}-skill: {logical_id}/{file.relative_path}")
            source_files = _files(source)
            if provider == "claude":
                adapter_id = conversion_id("claude", "portable")
                plan.manifest["skills"][logical_id] = _bundle_record(config, source, target, {path: _record(file, rendered[path], adapter_id) for path, file in source_files.items()})
            else:
                adapter_id = conversion_id("portable", "claude")
                plan.manifest["skills"][logical_id] = _bundle_record(config, target, source, {path: _record(rendered[path], file, adapter_id) for path, file in source_files.items()})
            continue
        claude_files, portable_files = _files(claude), _files(portable)
        file_records: dict[str, dict] = {}
        old_files = old.get("files", {}) if old else {}
        old_status = old.get("status", "synchronized") if old else "synchronized"
        for relative_path in sorted(set(claude_files) | set(portable_files)):
            current_c, current_p = claude_files.get(relative_path), portable_files.get(relative_path)
            previous = old_files.get(relative_path)
            if current_c is None or current_p is None:
                if previous:
                    plan.deletions.append(f"one-sided-deletion: {logical_id}/{relative_path}")
                    file_records[relative_path] = previous
                    continue
                source, target, source_provider = (current_c, portable, "claude") if current_c else (current_p, claude, "portable")
                assert source is not None
                content = adapter_for(target.provider).render(source_provider, target.provider, current_c or current_p, relative_path, source.content)
                generated = FileState(relative_path, content, sha256(content))
                plan.writes.append(PlannedWrite(_destination(target, relative_path), content))
                plan.drift.append(f"new-file: {logical_id}/{relative_path}")
                adapter_id = conversion_id(source_provider, target.provider)
                file_records[relative_path] = _record(source, generated, adapter_id) if source_provider == "claude" else _record(generated, source, adapter_id)
                continue
            if previous is None:
                if _semantic("claude", relative_path, current_c.content) != _semantic("portable", relative_path, current_p.content):
                    _conflict(plan, logical_id, relative_path, current_c, current_p)
                    continue
                file_records[relative_path] = _record(current_c, current_p, conversion_id("claude", "portable"))
                continue
            c_changed = current_c.sha256 != previous["claude_base_sha256"]
            p_changed = current_p.sha256 != previous["portable_base_sha256"]
            c_semantic = sha256(_semantic("claude", relative_path, current_c.content))
            p_semantic = sha256(_semantic("portable", relative_path, current_p.content))
            adapter_changed = previous.get("adapter") not in supported_conversion_ids()
            seed_or_upgrade = old_status == "bootstrap-pending-seed" or adapter_changed
            if not c_changed and not p_changed and not adapter_changed:
                file_records[relative_path] = previous
            elif not c_changed and not p_changed and seed_or_upgrade:
                portable_content = adapter_for("portable").render("claude", "portable", claude, relative_path, current_c.content)
                generated_p = FileState(relative_path, portable_content, sha256(portable_content))
                # Adapter upgrades migrate the derived portable tree without
                # rewriting provider-owned Claude source metadata.
                generated_c = current_c
                if generated_p.sha256 != current_p.sha256:
                    plan.writes.append(PlannedWrite(_destination(portable, relative_path), portable_content))
                    plan.drift.append(f"adapter-upgrade: {logical_id}/{relative_path}")
                file_records[relative_path] = _record(generated_c, generated_p, conversion_id("claude", "portable"))
            elif c_changed and not p_changed:
                if c_semantic == previous["semantic_base_sha256"] and not adapter_changed:
                    file_records[relative_path] = _record(current_c, current_p, conversion_id("claude", "portable"))
                    continue
                content = adapter_for("portable").render("claude", "portable", claude, relative_path, current_c.content)
                generated = FileState(relative_path, content, sha256(content))
                plan.writes.append(PlannedWrite(_destination(portable, relative_path), content))
                plan.drift.append(f"claude-drift: {logical_id}/{relative_path}")
                file_records[relative_path] = _record(current_c, generated, conversion_id("claude", "portable"))
            elif p_changed and not c_changed:
                if p_semantic == previous["semantic_base_sha256"] and not adapter_changed:
                    file_records[relative_path] = _record(current_c, current_p, conversion_id("portable", "claude"))
                    continue
                content = adapter_for("claude").render("portable", "claude", portable, relative_path, current_p.content)
                generated = FileState(relative_path, content, sha256(content))
                plan.writes.append(PlannedWrite(_destination(claude, relative_path), content))
                plan.drift.append(f"portable-drift: {logical_id}/{relative_path}")
                file_records[relative_path] = _record(generated, current_p, conversion_id("portable", "claude"))
            elif c_semantic == p_semantic:
                file_records[relative_path] = _record(current_c, current_p, conversion_id("claude", "portable"))
                plan.drift.append(f"equivalent-dual-drift: {logical_id}/{relative_path}")
            else:
                _conflict(plan, logical_id, relative_path, current_c, current_p)
        if not any(conflict["logical_skill_id"] == logical_id for conflict in plan.conflicts):
            status = "synchronized" if old_status == "bootstrap-pending-seed" else old_status
            plan.manifest["skills"][logical_id] = _bundle_record(config, claude, portable, file_records, status)
    for logical_id, record in old_skills.items():
        if logical_id not in plan.manifest["skills"] and logical_id not in claude_bundles and logical_id not in portable_bundles:
            plan.deletions.append(f"one-sided-deletion: {logical_id}")
            plan.manifest["skills"][logical_id] = record
    if plan.conflicts:
        plan.manifest = manifest.to_dict() if hasattr(manifest, "to_dict") else manifest
    return plan
