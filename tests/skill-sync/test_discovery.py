from pathlib import Path

from support import SkillSyncFixtureCase


class NestedDiscoveryTests(SkillSyncFixtureCase):
    fixture_name = "I-nested-discovery"

    def test_scan_registers_only_directories_containing_skill_markdown(self) -> None:
        result = self.run_sync("scan", "--json")
        self.assert_success(result)
        self.assertEqual(
            self.expected_json()["logical_skill_ids"],
            __import__("json").loads(result.stdout)["logical_skill_ids"],
        )


class SharedDependencyTests(SkillSyncFixtureCase):
    fixture_name = "J-_shared-dependency"

    def test_validate_accepts_a_skill_dependency_on_shared_runtime_content(self) -> None:
        result = self.run_sync("validate", "--json")
        self.assert_success(result)
        self.assertEqual(["_shared/runtime.py"], __import__("json").loads(result.stdout)["shared_dependencies"])


class IgnorePolicyTests(SkillSyncFixtureCase):
    fixture_name = "K-ignored-.venv"

    def test_apply_does_not_copy_a_virtual_environment(self) -> None:
        result = self.run_sync("apply")
        self.assert_success(result)
        self.assertFalse((self.workspace / "portable" / "ignored" / ".venv").exists())
        self.assertEqual("eligible-resource\n", (self.workspace / "portable" / "ignored" / "template.txt").read_text(encoding="utf-8"))
