"""Bootstrap safety contract for an audited, pre-existing portable corpus."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from support import SkillSyncFixtureCase, tree_hashes
from scripts.skill_sync.adapters import adapter_for
from scripts.skill_sync.bootstrap import _evidence
from scripts.skill_sync.manifest_store import load_manifest
from scripts.skill_sync.models import FileState, SkillBundle
from scripts.skill_sync.paths import load_config


def _sha256(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


class BootstrapTests(SkillSyncFixtureCase):
    fixture_name = "F-divergent-dual-edit"

    def _make_corpus_scale_copy(self) -> tuple[bytes, bytes, dict[str, object]]:
        """Expand fixture F to 60 audited skills, with one portable difference."""
        claude_root, portable_root = self.workspace / "claude", self.workspace / "portable"
        claude_skill = claude_root / "fixture-dual" / "SKILL.md"
        portable_skill = portable_root / "fixture-dual" / "SKILL.md"
        claude_bytes = claude_skill.read_bytes()
        portable_bytes = claude_bytes.replace(b"Shared baseline", b"Portable baseline evidence")
        portable_skill.write_bytes(portable_bytes)
        expected_paths = {"fixture-dual": "fixture-dual"}
        for index in range(1, 60):
            directory = f"skill-{index:02d}"
            content = (
                f"---\nname: {directory}\ndescription: Corpus-scale bootstrap fixture {index}.\n---\n\n# {directory}\n"
            ).encode("utf-8")
            if index == 1:
                content += b"\nLegacy reference: `C:/phase-3-repair-required`\n"
            for root in (claude_root, portable_root):
                target = root / directory / "SKILL.md"
                target.parent.mkdir(parents=True)
                target.write_bytes(content)
            expected_paths[directory] = directory
        evidence: dict[str, object] = {
            "expected_skill_count": 60,
            "expected_skill_paths": expected_paths,
            "expected_divergent_pairs": [
                {
                    "logical_id": "fixture-dual",
                    "relative_path": "fixture-dual/SKILL.md",
                    "claude_sha256": _sha256(claude_bytes),
                    "portable_sha256": _sha256(portable_bytes),
                }
            ],
        }
        return claude_bytes, portable_bytes, evidence

    def _write_config(self, evidence: dict[str, object]) -> None:
        (self.workspace / "config.json").write_text(
            json.dumps(
                {
                    "schema_version": 1,
                    "claude_dir": "claude",
                    "portable_dir": "portable",
                    "state_dir": ".skill-sync",
                    "bootstrap": evidence,
                }
            ),
            encoding="utf-8",
        )

    def test_incomplete_bootstrap_evidence_aborts_before_any_portable_write(self) -> None:
        _, portable_before, evidence = self._make_corpus_scale_copy()
        evidence["expected_divergent_pairs"] = []
        self._write_config(evidence)

        result = self.run_sync("apply", "--json")

        self.assertEqual(2, result.returncode, msg=result.stdout + result.stderr)
        self.assertIn("bootstrap evidence", result.stdout.lower())
        self.assertEqual(portable_before, (self.workspace / "portable" / "fixture-dual" / "SKILL.md").read_bytes())
        self.assertFalse((self.workspace / ".skill-sync" / "manifest.json").exists())

    def test_complete_evidence_creates_pending_manifest_without_portable_seed_writes(self) -> None:
        claude_bytes, portable_before, evidence = self._make_corpus_scale_copy()
        self._write_config(evidence)

        result = self.run_sync("apply", "--json")

        self.assert_success(result)
        snapshot = self.workspace / ".skill-sync" / "conflicts" / "bootstrap" / "fixture-dual"
        self.assertEqual(portable_before, (snapshot / "portable.SKILL.md").read_bytes())
        self.assertEqual(claude_bytes, (snapshot / "claude.SKILL.md").read_bytes())
        self.assertEqual(portable_before, (self.workspace / "portable" / "fixture-dual" / "SKILL.md").read_bytes())
        report = (self.workspace / ".skill-sync" / "reports" / "bootstrap-portable-differences.md").read_text(encoding="utf-8")
        self.assertIn(_sha256(portable_before), report)
        manifest = json.loads((self.workspace / ".skill-sync" / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(60, len(manifest["skills"]))
        self.assertEqual("bootstrap-pending-seed", manifest["skills"]["fixture-dual"]["status"])
        self.assertEqual(59, sum(record["status"] == "synchronized" for record in manifest["skills"].values()))

        second = self.run_sync("apply", "--json")
        self.assert_success(second)
        source_file = FileState("SKILL.md", claude_bytes, _sha256(claude_bytes))
        source_bundle = SkillBundle("fixture-dual", "claude", Path("fixture-dual"), "fixture-dual", (source_file,))
        portable_expected = adapter_for("portable").render("claude", "portable", source_bundle, "SKILL.md", claude_bytes)
        self.assertEqual(portable_expected, (self.workspace / "portable" / "fixture-dual" / "SKILL.md").read_bytes())
        self.assertEqual(claude_bytes, (self.workspace / "claude" / "fixture-dual" / "SKILL.md").read_bytes())
        updated_manifest = json.loads((self.workspace / ".skill-sync" / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual("synchronized", updated_manifest["skills"]["fixture-dual"]["status"])
        self.assertEqual(60, sum(record["status"] == "synchronized" for record in updated_manifest["skills"].values()))
        self.assertEqual(portable_before, (snapshot / "portable.SKILL.md").read_bytes())
        self.assertEqual(claude_bytes, (snapshot / "claude.SKILL.md").read_bytes())
        self.assertIn("fixture-dual", second.stdout)

    def test_malformed_audit_hash_record_is_rejected_as_incomplete_evidence(self) -> None:
        audit = self.workspace / "audit.md"
        audit.write_text(
            "| Declared name | Relative path |\n| --- | --- |\n| fixture-dual | `fixture-dual/SKILL.md` |\n"
            "| Relative skill | C SHA-256 | P SHA-256 |\n| --- | --- | --- |\n| fixture-dual | `too-short` | `also-short` |\n**Inference:**\n",
            encoding="utf-8",
        )
        (self.workspace / "config.json").write_text(
            json.dumps({"schema_version": 1, "claude_dir": "claude", "portable_dir": "portable", "state_dir": ".skill-sync", "bootstrap": {"audit_path": "audit.md"}}),
            encoding="utf-8",
        )

        _, error = _evidence(load_config(Path(self.workspace), self.workspace / "config.json"))

        self.assertEqual("audit divergent-pair record is malformed", error)

    def test_manifest_rejects_unknown_bootstrap_status(self) -> None:
        state = self.workspace / ".skill-sync"
        state.mkdir()
        digest = "0" * 64
        (state / "manifest.json").write_text(
            json.dumps({"schema_version": 1, "skills": {"fixture-dual": {"claude_path": "claude/fixture-dual", "portable_path": "portable/fixture-dual", "status": "unknown-state", "files": {"SKILL.md": {"adapter": "claude-to-codex:skill-markdown-v1", "claude_base_sha256": digest, "portable_base_sha256": digest, "semantic_base_sha256": digest}}}}}),
            encoding="utf-8",
        )

        with self.assertRaises(ValueError):
            load_manifest(state)
