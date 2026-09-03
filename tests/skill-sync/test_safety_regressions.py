"""Focused regression coverage for Task 1 safety review findings."""

from __future__ import annotations

import json
import os
import sys
import tempfile
import time
import unittest
from pathlib import Path

from support import REPOSITORY_ROOT

sys.path.insert(0, str(REPOSITORY_ROOT / "scripts"))

from skill_sync.adapters import adapter_for
from skill_sync.cli import _apply
from skill_sync.discovery import discover
from skill_sync.ignore_policy import ignored
from skill_sync.locking import LockHeld, RepositoryLock
from skill_sync.manifest_store import load_manifest
from skill_sync.models import SyncConfig
from skill_sync.paths import load_config
from skill_sync.validation import validate_all


SKILL = b"---\nname: {name}\ndescription: Exercise a safety regression.\n---\n\n# Skill\n"


class SafetyRegressionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        self.claude = self.root / "claude"
        self.portable = self.root / "portable"
        self.state = self.root / ".skill-sync"
        self.claude.mkdir()
        self.portable.mkdir()
        self.config = SyncConfig(self.root, self.claude, self.portable, self.state, 60)

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def write_skill(self, provider: Path, folder: str, name: str) -> Path:
        path = provider / folder / "SKILL.md"
        path.parent.mkdir(parents=True)
        path.write_bytes(SKILL.replace(b"{name}", name.encode()))
        return path

    def test_lock_never_steals_fresh_unverifiable_remote_owner_or_releases_replacement(self) -> None:
        self.state.mkdir()
        lock_path = self.state / "lock.json"
        lock_path.write_text(json.dumps({"pid": 99999, "host": "remote-host", "started_at": time.time(), "tool_version": "x", "token": "remote"}))
        with self.assertRaises(LockHeld):
            RepositoryLock(self.state, 60).acquire()
        lock_path.unlink()
        lock = RepositoryLock(self.state, 60)
        lock.acquire()
        lock_path.write_text(json.dumps({"pid": 1, "host": "other", "started_at": time.time(), "tool_version": "x", "token": "replacement"}))
        lock.release()
        self.assertTrue(lock_path.exists())
        with self.assertRaises(ValueError):
            RepositoryLock(self.state, 0)

    def test_config_and_conflict_ids_are_contained_and_path_safe(self) -> None:
        config_path = self.root / "config.json"
        config_path.write_text('{"schema_version":1,"claude_dir":"C:escape","portable_dir":"portable","state_dir":".skill-sync"}')
        with self.assertRaises(ValueError):
            load_config(self.root, config_path)
        self.write_skill(self.claude, "bs-conflict", "bs:conflict")
        self.write_skill(self.portable, "bs-conflict", "bs:conflict").write_bytes(SKILL.replace(b"{name}", b"bs:conflict") + b"portable")
        code, _ = _apply(self.config, False)
        self.assertEqual(3, code)
        conflicts = list((self.state / "conflicts").iterdir())
        self.assertEqual(1, len(conflicts))
        self.assertNotIn(":", conflicts[0].name)
        self.assertTrue((conflicts[0] / "metadata.json").is_file())

    def test_discovery_rejects_symlinks_and_keeps_nested_boundaries(self) -> None:
        outer = self.write_skill(self.claude, "outer", "outer")
        nested = self.write_skill(self.claude, "outer/nested", "nested")
        (nested.parent / "resource.txt").write_text("private")
        result = discover(self.claude, "claude")
        self.assertNotIn("nested/resource.txt", {item.relative_path for item in result.bundles["outer"].files})
        linked = self.claude / "outer" / "linked.txt"
        try:
            os.symlink(outer, linked)
        except OSError:
            self.skipTest("symlink creation unavailable in this environment")
        rejected = discover(self.claude, "claude")
        self.assertTrue(any("symlink" in finding.message for finding in rejected.findings))

    def test_staged_destination_validation_blocks_all_replacements(self) -> None:
        source = self.write_skill(self.claude, "validate-me", "validate-me")
        before = source.read_bytes()
        adapter = adapter_for("portable")
        original = adapter.validate
        adapter.validate = lambda *args: (type("Finding", (), {"level": "error", "message": "destination rejected", "path": ""})(),)
        try:
            code, _ = _apply(self.config, False)
        finally:
            adapter.validate = original
        self.assertNotEqual(0, code)
        self.assertFalse((self.portable / "validate-me" / "SKILL.md").exists())
        self.assertEqual(before, source.read_bytes())

    def test_reference_validation_covers_relative_absolute_and_dependency_edges(self) -> None:
        path = self.write_skill(self.claude, "shared-user", "shared-user")
        (self.claude / "_shared").mkdir()
        (self.claude / "_shared" / "runtime.py").write_text("pass")
        path.write_bytes(SKILL.replace(b"{name}", b"shared-user") + b"[missing](missing.txt) `../_shared/runtime.py` C:\\Users\\person\\secret")
        report = validate_all(discover(self.claude, "claude").bundles, self.claude)
        self.assertIn(("claude", "shared-user", "_shared/runtime.py"), report.shared_dependencies)
        messages = [finding.message for finding in report.findings]
        self.assertIn("referenced local file is missing", messages)
        self.assertIn("machine-specific absolute path is prohibited", messages)

    def test_adapter_selection_residue_manifest_shape_and_state_ignore_policy(self) -> None:
        self.assertIsNot(adapter_for("claude"), adapter_for("portable"))
        self.state.mkdir()
        (self.state / "stage-abandoned").mkdir()
        self.write_skill(self.claude, "residue", "residue")
        code, payload = _apply(self.config, False)
        self.assertEqual(0, code)
        self.assertIn("stage-abandoned", payload["residue"])
        self.assertIn("stage-abandoned", payload["residue_retained"])
        self.assertTrue((self.state / "stage-abandoned").exists())
        (self.state / "manifest.json").write_text('{"schema_version":1,"skills":{"x":{"files":{"SKILL.md":{}}}}}')
        with self.assertRaises(ValueError):
            load_manifest(self.state)
        self.assertTrue(ignored(".skill-sync/stage-orphan/file"))


if __name__ == "__main__":
    unittest.main()
