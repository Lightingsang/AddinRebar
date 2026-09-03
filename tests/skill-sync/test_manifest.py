import json

from support import SkillSyncFixtureCase, sha256_file


class ManifestTests(SkillSyncFixtureCase):
    fixture_name = "C-claude-edit"

    def test_apply_records_distinct_provider_and_semantic_base_hashes(self) -> None:
        self.assert_success(self.run_sync("apply"))
        (self.workspace / "claude" / "fixture-edit" / "SKILL.md").write_bytes(
            (self.workspace / "expected" / "claude-after-edit.md").read_bytes()
        )
        result = self.run_sync("apply")
        self.assert_success(result)
        manifest = json.loads((self.workspace / ".skill-sync" / "manifest.json").read_text(encoding="utf-8"))
        expectations = self.expected_json()
        record = manifest["skills"][expectations["logical_skill_id"]]["files"]["SKILL.md"]
        self.assertEqual(sha256_file(self.workspace / "expected" / "claude-after-edit.md"), record["claude_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "portable-after-edit.md"), record["portable_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "semantic-after-edit.md"), record["semantic_base_sha256"])
        self.assertEqual(expectations["adapter"], record["adapter"])
