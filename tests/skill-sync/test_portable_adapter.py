"""Phase 3 provider-aware rendering contracts."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

from support import REPOSITORY_ROOT

sys.path.insert(0, str(REPOSITORY_ROOT / "scripts"))

from skill_sync.adapters import adapter_for, conversion_id
from skill_sync.models import FileState, SkillBundle


SOURCE = b"""---
name: portable-example
description: Create or update Claude skills.
when_to_use: Use when a reusable workflow is needed.
keywords: [skills, workflow]
---

# Portable example

New skills live in `./.claude/skills/`, never `~/.claude/skills/`.
Read `./.claude/rules/primary-workflow.md` and `CLAUDE.md`.
Use `TaskCreate`, `TaskUpdate`, `TaskGet`, `TaskList`, or `TodoWrite`.

- [Agent Skills Docs](https://docs.claude.com/en/docs/claude-code/skills.md)
- [Best Practices](https://docs.claude.com/en/docs/agents-and-tools/agent-skills/best-practices.md)
- [Plugin Marketplaces](https://code.claude.com/docs/en/plugin-marketplaces.md)
"""


def _bundle(provider: str, content: bytes) -> SkillBundle:
    file = FileState("SKILL.md", content, "unused")
    return SkillBundle("portable-example", provider, Path("portable-example"), "portable-example", (file,))


class PortableAdapterTests(unittest.TestCase):
    def test_claude_skill_renders_to_shared_codex_antigravity_contract(self) -> None:
        adapter = adapter_for("portable")

        rendered = adapter.render("claude", "portable", _bundle("claude", SOURCE), "SKILL.md", SOURCE)
        text = rendered.decode("utf-8")

        self.assertIn("Portable host contract", text)
        self.assertIn("Codex", text)
        self.assertIn("Antigravity", text)
        self.assertIn("./.agents/skills/", text)
        self.assertIn("./.agents/rules/primary-workflow.md", text)
        self.assertIn("AGENTS.md", text)
        self.assertIn("Use when a reusable workflow is needed", text.split("---", 2)[1])
        self.assertNotIn("when_to_use:", text.split("---", 2)[1])
        self.assertNotIn("keywords:", text.split("---", 2)[1])
        self.assertRegex(text.splitlines()[2], r'^description: ".*"$')
        self.assertNotIn(".Codex", text)
        self.assertNotIn(".claude/skills", text)
        self.assertNotIn("~/.agents/skills", text)
        self.assertNotIn("TaskCreate", text)
        self.assertNotIn("TaskUpdate", text)
        self.assertNotIn("TaskGet", text)
        self.assertNotIn("TaskList", text)
        self.assertNotIn("TodoWrite", text)
        self.assertNotIn("docs.Codex.com", text)
        self.assertNotIn("code.Codex.com", text)
        self.assertEqual((), adapter.validate("portable", _bundle("portable", rendered), "SKILL.md", rendered))

    def test_portable_render_is_reversible_for_shared_paths_and_contract_header(self) -> None:
        portable = adapter_for("portable").render("claude", "portable", _bundle("claude", SOURCE), "SKILL.md", SOURCE)

        restored = adapter_for("claude").render("portable", "claude", _bundle("portable", portable), "SKILL.md", portable)
        text = restored.decode("utf-8")

        self.assertNotIn("Portable host contract", text)
        self.assertIn("./.claude/skills/", text)
        self.assertIn("./.claude/rules/primary-workflow.md", text)
        self.assertIn("CLAUDE.md", text)

    def test_portable_render_is_byte_idempotent_with_content_after_contract(self) -> None:
        once = adapter_for("portable").render("claude", "portable", _bundle("claude", SOURCE), "SKILL.md", SOURCE)
        with_trailing_content = once + b"<!-- retained-content -->\n"

        twice = adapter_for("portable").render(
            "portable", "portable", _bundle("portable", with_trailing_content), "SKILL.md", with_trailing_content
        )
        thrice = adapter_for("portable").render(
            "portable", "portable", _bundle("portable", twice), "SKILL.md", twice
        )

        self.assertEqual(twice, thrice)

    def test_portable_render_is_idempotent_for_quoted_descriptions(self) -> None:
        descriptions = {
            "unquoted with inner quotes": b'description: Use when the user says "grill me" or "challenge me".',
            "double-quoted with escapes": b'description: "Use when the user says \\"grill me\\" or a \\\\ path."',
            "single-quoted yaml": b"description: 'Use when the user''s prompt says \"grill me\".'",
        }
        for label, line in descriptions.items():
            with self.subTest(label):
                source = SOURCE.replace(b"description: Create or update Claude skills.", line)
                adapter = adapter_for("portable")
                once = adapter.render("claude", "portable", _bundle("claude", source), "SKILL.md", source)
                twice = adapter.render("portable", "portable", _bundle("portable", once), "SKILL.md", once)

                self.assertEqual(once, twice)
                self.assertIn('\\"grill me\\"', once.decode("utf-8").splitlines()[2])
                self.assertNotIn('\\\\\\"', once.decode("utf-8").splitlines()[2])

    def test_when_to_use_naming_claude_is_not_appended_twice(self) -> None:
        source = SOURCE.replace(
            b"description: Create or update Claude skills.",
            b"description: Create skills. Invoke when refining Claude skills.",
        ).replace(b"when_to_use: Use when a reusable workflow is needed.", b'when_to_use: "Invoke when refining Claude skills."')
        adapter = adapter_for("portable")
        once = adapter.render("claude", "portable", _bundle("claude", source), "SKILL.md", source)
        twice = adapter.render("portable", "portable", _bundle("portable", once), "SKILL.md", once)

        description = once.decode("utf-8").splitlines()[2]
        self.assertEqual(once, twice)
        self.assertEqual(1, description.count("Invoke when refining"))
        self.assertNotIn("Claude", description)

    def test_rendered_mirror_is_semantically_equal_to_its_claude_source(self) -> None:
        source = SOURCE.replace(b"description: Create or update Claude skills.", b'description: Use when the user says "grill me".')
        mirror = adapter_for("portable").render("claude", "portable", _bundle("claude", source), "SKILL.md", source)

        self.assertEqual(adapter_for("claude").normalize("SKILL.md", source), adapter_for("portable").normalize("SKILL.md", mirror))

    def test_adapter_v3_identity_forces_revalidation_of_legacy_manifest_records(self) -> None:
        self.assertEqual("claude-to-codex:skill-markdown-v3", conversion_id("claude", "portable"))
        self.assertEqual("codex-to-claude:skill-markdown-v3", conversion_id("portable", "claude"))

    def test_portable_validation_rejects_case_variant_and_fabricated_codex_locations(self) -> None:
        invalid = SOURCE.replace(b".claude", b".Codex").replace(b"docs.claude.com", b"docs.Codex.com")

        findings = adapter_for("portable").validate("portable", _bundle("portable", invalid), "SKILL.md", invalid)

        messages = {finding.message for finding in findings}
        self.assertIn("case-variant .Codex path is prohibited", messages)
        self.assertIn("fabricated Codex documentation URL is prohibited", messages)

    def test_portable_render_repairs_known_corpus_references_and_machine_paths(self) -> None:
        source = SOURCE + b"""
`assets/writing-styles/default.md`
`references/test-setup-visual-studio.md`
`references/styles/theme-light-sample.xaml.md`
`references/styles/buttons-sample.xaml.md`
`references/styles/textboxes-sample.xaml.md`
`references/styles/spacing-typography-sample.xaml.md`
`scripts/generate_review.py`
`/Volumes/WORK/www/private/`
"""

        rendered = adapter_for("portable").render("claude", "portable", _bundle("claude", source), "SKILL.md", source)
        text = rendered.decode("utf-8")

        self.assertIn("references/writing-styles.md", text)
        self.assertIn("references/test-setup-rider.md", text)
        self.assertIn("references/styles/theme-light-sample.md", text)
        self.assertIn("references/styles/controls-sample.md", text)
        self.assertIn("references/styles/spacing-typography-sample.md", text)
        self.assertIn("eval-viewer/generate_review.py", text)
        self.assertIn("<host-path>", text)
        self.assertNotIn("/Volumes/WORK", text)


if __name__ == "__main__":
    unittest.main()
