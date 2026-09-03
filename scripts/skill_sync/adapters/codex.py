"""Codex/portable-tree skill adapter."""

from __future__ import annotations

from ..frontmatter import parse_frontmatter
from ..models import Finding, SkillBundle
from .portable_markdown import render_portable


class CodexAdapter:
    name = "skill-markdown-v3"
    identity = "codex:skill-markdown-v3"

    def normalize(self, relative_path: str, content: bytes) -> bytes:
        return render_portable(content) if relative_path == "SKILL.md" else content

    def render(self, source_provider: str, destination_provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> bytes:
        return render_portable(content) if relative_path == "SKILL.md" else content

    def validate(self, provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> tuple[Finding, ...]:
        findings: list[Finding] = []
        try:
            if relative_path == "SKILL.md":
                parse_frontmatter(content)
        except (UnicodeDecodeError, ValueError) as error:
            return (Finding("error", str(error), f"{bundle.relative_root}/{relative_path}"),)
        if relative_path == "SKILL.md":
            text = content.decode("utf-8")
            if ".Codex" in text:
                findings.append(Finding("error", "case-variant .Codex path is prohibited", f"{bundle.relative_root}/{relative_path}"))
            if "docs.Codex.com" in text or "code.Codex.com" in text:
                findings.append(Finding("error", "fabricated Codex documentation URL is prohibited", f"{bundle.relative_root}/{relative_path}"))
            if "~/.agents/skills" in text:
                findings.append(Finding("error", "user-global portable skill path is prohibited", f"{bundle.relative_root}/{relative_path}"))
        return tuple(findings)
