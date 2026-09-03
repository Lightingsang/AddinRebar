import json

from support import SkillSyncFixtureCase, sha256_file


class ClaudeToPortableAdapterTests(SkillSyncFixtureCase):
    fixture_name = "A-claude-create"

    def test_apply_creates_the_portable_representation_of_a_claude_only_skill(self) -> None:
        result = self.run_sync("apply")
        self.assert_success(result)
        self.assertEqual(
            (self.workspace / "expected" / "portable" / "new-skill" / "SKILL.md").read_text(encoding="utf-8"),
            (self.workspace / "portable" / "new-skill" / "SKILL.md").read_text(encoding="utf-8"),
        )


class PortableToClaudeAdapterTests(SkillSyncFixtureCase):
    fixture_name = "B-portable-create"

    def test_apply_creates_the_claude_representation_of_a_portable_only_skill(self) -> None:
        result = self.run_sync("apply")
        self.assert_success(result)
        self.assertEqual(
            (self.workspace / "expected" / "claude" / "new-skill" / "SKILL.md").read_text(encoding="utf-8"),
            (self.workspace / "claude" / "new-skill" / "SKILL.md").read_text(encoding="utf-8"),
        )


class SemanticConflictTests(SkillSyncFixtureCase):
    fixture_name = "E-semantically-equal-dual-edit"

    def test_apply_accepts_equivalent_dual_edits_and_refreshes_the_base(self) -> None:
        self.assert_success(self.run_sync("apply"))
        claude = self.workspace / "claude" / "fixture-dual" / "SKILL.md"
        portable = self.workspace / "portable" / "fixture-dual" / "SKILL.md"
        claude.write_bytes((self.workspace / "expected" / "claude-after-edit.md").read_bytes())
        portable.write_bytes((self.workspace / "expected" / "portable-after-edit.md").read_bytes())
        self.assertNotEqual(claude.read_bytes(), portable.read_bytes())

        result = self.run_sync("apply")
        self.assert_success(result)
        self.assertFalse((self.workspace / ".skill-sync" / "conflicts" / "fixture-dual").exists())
        manifest = json.loads((self.workspace / ".skill-sync" / "manifest.json").read_text(encoding="utf-8"))
        record = manifest["skills"][self.expected_json()["logical_skill_id"]]["files"]["SKILL.md"]
        self.assertEqual(sha256_file(self.workspace / "expected" / "claude-after-edit.md"), record["claude_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "portable-after-edit.md"), record["portable_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "semantic-after-edit.md"), record["semantic_base_sha256"])


class DivergentConflictTests(SkillSyncFixtureCase):
    fixture_name = "F-divergent-dual-edit"

    def test_apply_reports_a_divergent_dual_edit_without_overwriting_either_side(self) -> None:
        self.assert_success(self.run_sync("apply"))
        claude = self.workspace / "claude" / "fixture-dual" / "SKILL.md"
        portable = self.workspace / "portable" / "fixture-dual" / "SKILL.md"
        claude.write_bytes((self.workspace / "expected" / "claude-after-edit.md").read_bytes())
        portable.write_bytes((self.workspace / "expected" / "portable-after-edit.md").read_bytes())
        before_claude = claude.read_bytes()
        before_portable = portable.read_bytes()
        result = self.run_sync("apply", "--json")
        expectations = self.expected_json()
        self.assertEqual(expectations["exit_code"], result.returncode, msg=result.stdout + result.stderr)
        self.assertEqual(before_claude, claude.read_bytes())
        self.assertEqual(before_portable, portable.read_bytes())
        self.assertTrue((self.workspace / expectations["conflict_directory"]).is_dir())
