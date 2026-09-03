from support import SkillSyncFixtureCase, tree_hashes


class CliReadOnlyTests(SkillSyncFixtureCase):
    fixture_name = "C-claude-edit"

    def test_check_reports_drift_without_mutating_any_fixture_file(self) -> None:
        self.assert_success(self.run_sync("apply"))
        (self.workspace / "claude" / "fixture-edit" / "SKILL.md").write_bytes(
            (self.workspace / "expected" / "claude-after-edit.md").read_bytes()
        )
        before = tree_hashes(self.workspace)
        result = self.run_sync("check", "--json")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(before, tree_hashes(self.workspace))
        self.assertIn("drift", result.stdout.lower())
