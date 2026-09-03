"""Provider-specific adapter selection."""

from __future__ import annotations

from typing import Protocol

from ..models import Finding, SkillBundle

from .antigravity import AntigravityAdapter
from .claude import ClaudeAdapter
from .codex import CodexAdapter


_ADAPTERS = {
    "claude": ClaudeAdapter(),
    "portable": CodexAdapter(),
    "codex": CodexAdapter(),
    "antigravity": AntigravityAdapter(),
}


class ProviderAdapter(Protocol):
    """Binding provider adapter contract used by planning and validation."""

    name: str
    identity: str

    def normalize(self, relative_path: str, content: bytes) -> bytes: ...
    def render(self, source_provider: str, destination_provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> bytes: ...
    def validate(self, provider: str, bundle: SkillBundle, relative_path: str, content: bytes) -> tuple[Finding, ...]: ...


def adapter_for(provider: str):
    try:
        return _ADAPTERS[provider]
    except KeyError as error:
        raise ValueError(f"unsupported provider adapter: {provider}") from error


def conversion_id(source_provider: str, destination_provider: str) -> str:
    source = adapter_for(source_provider).identity.split(":", 1)[0]
    destination = adapter_for(destination_provider).identity.split(":", 1)[0]
    version = adapter_for(destination_provider).name.rsplit("-", 1)[-1]
    return f"{source}-to-{destination}:skill-markdown-{version}"


def supported_conversion_ids() -> frozenset[str]:
    return frozenset({conversion_id("claude", "portable"), conversion_id("portable", "claude")})
