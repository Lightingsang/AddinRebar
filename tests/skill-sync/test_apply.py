import subprocess
import sys
import json

from support import ENGINE, SkillSyncFixtureCase, sha256_file, tree_hashes


class ConcurrentApplyTests(SkillSyncFixtureCase):
    fixture_name = "G-concurrent-apply"

    def test_simultaneous_apply_attempts_leave_one_complete_manifest_and_no_corruption(self) -> None:
        if not ENGINE.is_file():
            self.run_sync("apply")
        command = [sys.executable, str(ENGINE), "apply", "--root", str(self.workspace), "--config", str(self.workspace / "config.json")]
        first = subprocess.Popen(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        second = subprocess.Popen(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        first_stdout, first_stderr = first.communicate(timeout=10)
        second_stdout, second_stderr = second.communicate(timeout=10)
        expectations = self.expected_json()
        allowed = expectations["allowed_exit_codes"]
        self.assertIn(first.returncode, allowed, msg=first_stdout + first_stderr)
        self.assertIn(second.returncode, allowed, msg=second_stdout + second_stderr)
        self.assertIn(0, [first.returncode, second.returncode])
        manifest = json.loads((self.workspace / expectations["manifest"]).read_text(encoding="utf-8"))
        record = manifest["skills"][expectations["logical_skill_id"]]["files"]["SKILL.md"]
        expected_claude = self.workspace / "claude" / "concurrent" / "SKILL.md"
        expected_portable = self.workspace / "expected" / "portable" / "concurrent" / "SKILL.md"
        self.assertEqual(sha256_file(expected_claude), record["claude_base_sha256"])
        self.assertEqual(sha256_file(expected_portable), record["portable_base_sha256"])
        self.assertEqual(sha256_file(expected_portable), record["semantic_base_sha256"])
        self.assertEqual(expected_portable.read_bytes(), (self.workspace / "portable" / "concurrent" / "SKILL.md").read_bytes())


class InterruptedApplyTests(SkillSyncFixtureCase):
    fixture_name = "H-interrupted-write"

    def test_staging_failure_preserves_the_previous_destination_and_manifest(self) -> None:
        self.assert_success(self.run_sync("apply"))
        (self.workspace / "claude" / "interrupted" / "SKILL.md").write_bytes(
            (self.workspace / "expected" / "claude-after-edit.md").read_bytes()
        )
        before = tree_hashes(self.workspace / "portable")
        manifest_before = (self.workspace / ".skill-sync" / "manifest.json").read_bytes()
        result = self.run_sync("apply", "--fault-after-stage")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(before, tree_hashes(self.workspace / "portable"))
        self.assertEqual(manifest_before, (self.workspace / ".skill-sync" / "manifest.json").read_bytes())


class DeletionTests(SkillSyncFixtureCase):
    fixture_name = "L-one-sided-deletion"

    def test_apply_reports_one_sided_deletion_without_deleting_the_other_representation(self) -> None:
        self.assert_success(self.run_sync("apply"))
        (self.workspace / "claude" / "retained-skill" / "SKILL.md").unlink()
        result = self.run_sync("apply", "--json")
        self.assert_success(result)
        self.assertTrue((self.workspace / "portable" / "retained-skill" / "SKILL.md").is_file())
        self.assertIn("one-sided-deletion", result.stdout)


class IdempotencyTests(SkillSyncFixtureCase):
    fixture_name = "M-idempotent-apply-check"

    def test_apply_apply_check_is_idempotent_and_check_is_read_only(self) -> None:
        self.assert_success(self.run_sync("apply"))
        after_first_apply = tree_hashes(self.workspace)
        self.assert_success(self.run_sync("apply"))
        after_second_apply = tree_hashes(self.workspace)
        self.assertEqual(after_first_apply, after_second_apply)
        self.assert_success(self.run_sync("check"))
        self.assertEqual(after_second_apply, tree_hashes(self.workspace))
