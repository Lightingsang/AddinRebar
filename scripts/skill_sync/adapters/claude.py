"""Claude-specific skill adapter."""

from __future__ import annotations

from ..frontmatter import parse_frontmatter
from ..models import Finding, SkillBundle
from .portable_markdown import render_claude, render_portable


class ClaudeAdapter:
    name = "skill-markdown-v3"
    identity = "claude:skill-markdown-v3"

    def normalize(self, relative_path: str, content: bytes) -> bytes:
        return render_portable(content) if relative_path == "SKILL.md" else content

    def render(self, source_provider: str, destination_provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> bytes:
        return render_claude(content) if relative_path == "SKILL.md" and source_provider != "claude" else content

    def validate(self, provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> tuple[Finding, ...]:
        try:
            if relative_path == "SKILL.md":
                parse_frontmatter(content)
        except (UnicodeDecodeError, ValueError) as error:
            return (Finding("error", str(error), f"{bundle.relative_root}/{relative_path}"),)
        return ()
