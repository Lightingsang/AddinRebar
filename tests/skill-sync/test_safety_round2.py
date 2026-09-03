"""Blocking regression proof for Task 1 review Fix Round 2."""

from __future__ import annotations

import json
import os
import socket
import sys
import tempfile
import time
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

from support import REPOSITORY_ROOT

sys.path.insert(0, str(REPOSITORY_ROOT / "scripts"))

from skill_sync.locking import LockHeld, RepositoryLock
from skill_sync.paths import safe_artifact_id
from skill_sync.validation import validate_all
import skill_sync.discovery as discovery
import skill_sync.adapters as adapters
from skill_sync.atomic_io import recover_abandoned_staging, replace
from skill_sync.candidate_validation import _copy
from skill_sync.manifest_store import load_manifest
from skill_sync.models import ManifestFileRecord, ManifestSkillRecord


class Round2LockTests(unittest.TestCase):
    def test_remote_lock_is_report_only_and_forced_replacement_survives_recovery_probe(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            state = Path(temporary)
            lock_path = state / "lock.json"
            old = time.time() - 600
            lock_path.write_text(json.dumps({"pid": 99999999, "host": "remote", "started_at": old, "tool_version": "x", "token": "remote"}))
            with self.assertRaises(LockHeld):
                RepositoryLock(state, 1).acquire()
            self.assertEqual("remote", json.loads(lock_path.read_text())["token"])

            stale = {"pid": 99999999, "host": socket.gethostname(), "started_at": old, "tool_version": "x", "token": "stale"}
            fresh = {"pid": os.getpid(), "host": socket.gethostname(), "started_at": time.time(), "tool_version": "x", "token": "fresh"}
            lock_path.write_text(json.dumps(stale))
            real_replace = os.replace

            def force_replacement(source: Path, destination: Path) -> None:
                lock_path.write_text(json.dumps(fresh))
                real_replace(source, destination)

            lock = RepositoryLock(state, 1)
            with patch("skill_sync.locking.os.replace", side_effect=force_replacement):
                self.assertFalse(lock._recover_if_proven_stale())
            self.assertEqual("fresh", json.loads(lock_path.read_text())["token"])


class Round2PathTests(unittest.TestCase):
    def test_windows_device_names_are_always_encoded_to_safe_artifact_ids(self) -> None:
        names = ["CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))]
        names += ["con.txt", "NUL. ", "com1.log"]
        outputs = [safe_artifact_id(name) for name in names]
        for source, output in zip(names, outputs):
            self.assertNotEqual(source, output)
            self.assertNotIn(output.rstrip(" .").split(".")[0].upper(), {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))})
        self.assertEqual(outputs, [safe_artifact_id(name) for name in names])

    def test_reparse_attribute_is_rejected_without_path_is_junction(self) -> None:
        fake_stat = SimpleNamespace(st_file_attributes=0x400)
        with patch.object(Path, "is_junction", new=None), patch.object(discovery.os, "lstat", return_value=fake_stat):
            self.assertTrue(discovery.is_reparse_point(Path("reparse")))


class Round2ReferenceTests(unittest.TestCase):
    def test_command_and_path_classification_matrix(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            bundle = root / "matrix"
            (bundle / "scripts").mkdir(parents=True)
            (bundle / "local.txt").write_text("local")
            (bundle / "scripts" / "tool.py").write_text("pass")
            content = """---
name: matrix
description: Exercise command and path classification.
---
`git` `python` `/bs:plan` [local](local.txt) `scripts/tool.py`
`/etc/passwd` `\\\\server\\share` `C:\\x` `C:x`
`w:ins>` `a:clrScheme>` `x:Name` `/api/health` `/view?file=<path>` `/`
"""
            (bundle / "SKILL.md").write_text(content)
            report = validate_all(discovery.discover(root, "claude").bundles, root)
            paths = {finding.path for finding in report.findings}
            self.assertFalse({"git", "python", "/bs:plan"} & paths)
            self.assertEqual({"/etc/passwd", "\\\\server\\share", "C:\\x"}, paths)
            self.assertTrue(all(finding.message == "machine-specific absolute path is prohibited" for finding in report.findings))

    def test_examples_globs_and_fenced_code_are_not_treated_as_missing_bundle_files(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            bundle = root / "examples"
            (bundle / "references").mkdir(parents=True)
            (bundle / "references" / "guide.md").write_text("guide")
            content = r"""---
name: examples
description: Exercise corpus-style examples without hiding real bad references.
---

`src/api/users.ts` `/HARD-GATE-SCOUT-FIRST` `/usr/bin/env` `/dev/null`
`/path/to/project` `references/*.md` [guide](references/guide.md)
`assets/{type}/` `assets/showoff/<mission-name>/content.md` [placeholder](url)

```xml
<w:ins><w:r><w:t>example</w:t></w:r></w:ins>
```

[missing](references/missing.md) `C:\Users\person\secret`
"""
            (bundle / "SKILL.md").write_text(content)

            report = validate_all(discovery.discover(root.resolve(), "portable").bundles, root.resolve())
            paths = {finding.path for finding in report.findings}

            self.assertEqual({"references/missing.md", r"C:\Users\person\secret"}, paths)


class Round2CandidateCopyTests(unittest.TestCase):
    def test_candidate_copy_uses_content_only_copy_without_source_acl_metadata(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / "source"
            destination = Path(temporary) / "destination"
            source.mkdir()
            with patch("skill_sync.candidate_validation.shutil.copytree") as copytree:
                _copy(source, destination)

            self.assertEqual("copyfile", copytree.call_args.kwargs["copy_function"].__name__)


class Round2ReplaceFallbackTests(unittest.TestCase):
    def test_permission_denied_replace_falls_back_to_fsynced_in_place_content(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            staged = root / "staged"
            destination = root / "destination"
            staged.write_bytes(b"new-content")
            destination.write_bytes(b"old-content")

            with patch("skill_sync.atomic_io.os.replace", side_effect=PermissionError("acl denies delete")):
                replace(staged, destination)

            self.assertEqual(b"new-content", destination.read_bytes())


class Round2AdapterTests(unittest.TestCase):
    def test_provider_and_direction_adapter_ids_are_distinct(self) -> None:
        identities = {adapters.adapter_for(provider).identity for provider in ("claude", "codex", "antigravity")}
        self.assertEqual(3, len(identities))
        self.assertEqual("claude-to-codex:skill-markdown-v3", adapters.conversion_id("claude", "portable"))
        self.assertEqual("codex-to-claude:skill-markdown-v3", adapters.conversion_id("portable", "claude"))
        self.assertNotEqual(adapters.conversion_id("claude", "portable"), adapters.conversion_id("portable", "claude"))


class Round2ResidueTests(unittest.TestCase):
    def test_only_proven_stale_same_host_stage_is_removed(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            state = Path(temporary)
            old, now = time.time() - 600, time.time()

            def stage(name: str, payload: object) -> None:
                directory = state / name
                directory.mkdir()
                (directory / ".stage-owner.json").write_text(json.dumps(payload) if not isinstance(payload, str) else payload)

            base = {"host": socket.gethostname(), "tool_version": "1.0", "token": "token"}
            stage("stage-stale", {**base, "pid": 99999999, "started_at": old})
            stage("stage-young", {**base, "pid": 99999999, "started_at": now})
            stage("stage-live", {**base, "pid": os.getpid(), "started_at": old})
            stage("stage-remote", {**base, "pid": 99999999, "host": "remote", "started_at": old})
            stage("stage-malformed", "not-json")

            report = recover_abandoned_staging(state, 60)
            self.assertEqual(["stage-stale"], report["recovered"])
            self.assertEqual(["stage-live", "stage-malformed", "stage-remote", "stage-young"], report["retained"])
            self.assertFalse((state / "stage-stale").exists())
            self.assertTrue(all((state / name).exists() for name in report["retained"]))


class Round2ManifestTests(unittest.TestCase):
    def test_manifest_has_typed_nested_records_and_rejects_provider_path_escape(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            state = Path(temporary)
            path = state / "manifest.json"
            record = {
                "schema_version": 1,
                "skills": {
                    "x": {
                        "claude_path": "claude/x",
                        "portable_path": "portable/x",
                        "status": "synchronized",
                        "files": {
                            "SKILL.md": {
                                "claude_base_sha256": "0" * 64,
                                "portable_base_sha256": "1" * 64,
                                "semantic_base_sha256": "2" * 64,
                                "adapter": "claude-to-codex:skill-markdown-v1",
                            }
                        },
                    }
                },
            }
            path.write_text(json.dumps(record))
            manifest = load_manifest(state)
            self.assertIsInstance(manifest.skills["x"], ManifestSkillRecord)
            self.assertIsInstance(manifest.skills["x"].files["SKILL.md"], ManifestFileRecord)
            for bad in ("../escape", "C:escape", "C:\\escape", "/absolute", "\\\\server\\share"):
                record["skills"]["x"]["claude_path"] = bad
                path.write_text(json.dumps(record))
                with self.subTest(path=bad), self.assertRaises(ValueError):
                    load_manifest(state)

    def test_manifest_rejects_windows_and_mixed_separator_traversal_for_every_path_field(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            state = Path(temporary)
            path = state / "manifest.json"
            valid = {
                "schema_version": 1,
                "skills": {
                    "nested": {
                        "claude_path": "claude/document-skills/docx",
                        "portable_path": "portable/document-skills/docx",
                        "status": "synchronized",
                        "files": {
                            "references/nested/file.md": {
                                "claude_base_sha256": "0" * 64,
                                "portable_base_sha256": "1" * 64,
                                "semantic_base_sha256": "2" * 64,
                                "adapter": "claude-to-codex:skill-markdown-v1",
                            }
                        },
                    }
                },
            }
            path.write_text(json.dumps(valid))
            self.assertIsInstance(load_manifest(state).skills["nested"], ManifestSkillRecord)

            invalid = (
                r"..\outside",
                r"x\..\..\outside",
                r"nested/..\outside",
                r"nested\../outside",
                r"C:outside",
                r"C:\outside",
                r"\\server\share",
                r"\rooted",
                "/absolute",
                "",
                ".",
                "nested//file",
                "nested/./file",
                "nested/",
            )
            for field in ("claude_path", "portable_path"):
                for bad in invalid:
                    candidate = json.loads(json.dumps(valid))
                    candidate["skills"]["nested"][field] = bad
                    path.write_text(json.dumps(candidate))
                    with self.subTest(field=field, path=bad), self.assertRaises(ValueError):
                        load_manifest(state)
            for bad in invalid:
                candidate = json.loads(json.dumps(valid))
                record = candidate["skills"]["nested"]["files"].pop("references/nested/file.md")
                candidate["skills"]["nested"]["files"][bad] = record
                path.write_text(json.dumps(candidate))
                with self.subTest(field="file", path=bad), self.assertRaises(ValueError):
                    load_manifest(state)


if __name__ == "__main__":
    unittest.main()
