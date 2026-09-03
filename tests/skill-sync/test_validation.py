import json

from support import SkillSyncFixtureCase, sha256_file


class ValidationTests(SkillSyncFixtureCase):
    fixture_name = "D-portable-edit"

    def test_apply_adapts_an_established_portable_edit_and_refreshes_manifest_bases(self) -> None:
        self.assert_success(self.run_sync("apply"))
        portable = self.workspace / "portable" / "fixture-portable-edit" / "SKILL.md"
        portable.write_bytes((self.workspace / "expected" / "portable-after-edit.md").read_bytes())

        result = self.run_sync("apply")
        self.assert_success(result)
        self.assertEqual(
            (self.workspace / "expected" / "claude-after-edit.md").read_bytes(),
            (self.workspace / "claude" / "fixture-portable-edit" / "SKILL.md").read_bytes(),
        )
        manifest = json.loads((self.workspace / ".skill-sync" / "manifest.json").read_text(encoding="utf-8"))
        record = manifest["skills"][self.expected_json()["logical_skill_id"]]["files"]["SKILL.md"]
        self.assertEqual(sha256_file(self.workspace / "expected" / "claude-after-edit.md"), record["claude_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "portable-after-edit.md"), record["portable_base_sha256"])
        self.assertEqual(sha256_file(self.workspace / "expected" / "semantic-after-edit.md"), record["semantic_base_sha256"])
