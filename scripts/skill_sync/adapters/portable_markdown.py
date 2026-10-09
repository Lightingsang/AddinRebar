"""Semantic rendering for the shared Codex/Antigravity skill tree."""

from __future__ import annotations

import json
import re


_CONTRACT_START = "<!-- portable-host-contract:start -->"
_CONTRACT_END = "<!-- portable-host-contract:end -->"
_CONTRACT = f"""{_CONTRACT_START}
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
{_CONTRACT_END}
"""
_CONTRACT_PATTERN = re.compile(
    rf"\n?{re.escape(_CONTRACT_START)}.*?{re.escape(_CONTRACT_END)}\n?",
    re.DOTALL,
)

_TO_PORTABLE = (
    ("assets/writing-styles/default.md", "references/writing-styles.md"),
    ("references/test-setup-visual-studio.md", "references/test-setup-rider.md"),
    ("references/styles/theme-light-sample.xaml.md", "references/styles/theme-light-sample.md"),
    ("references/styles/theme-dark-sample.xaml.md", "references/styles/theme-dark-sample.md"),
    ("references/styles/buttons-sample.xaml.md", "references/styles/controls-sample.md"),
    ("references/styles/textboxes-sample.xaml.md", "references/styles/controls-sample.md"),
    ("references/styles/spacing-typography-sample.xaml.md", "references/styles/spacing-typography-sample.md"),
    ("scripts/generate_review.py", "eval-viewer/generate_review.py"),
    ("$HOME/.claude/skills/.venv/bin/python3", "python"),
    ("~/.claude/skills", ".agents/skills"),
    ("./.claude/skills", "./.agents/skills"),
    (".claude/skills", ".agents/skills"),
    ("claude/skills", ".agents/skills"),
    ("~/.claude/writing-styles", ".agents/writing-styles"),
    ("~/.claude/plans", "plans"),
    (".claude/plans", "plans"),
    ("./.claude/rules", "./.agents/rules"),
    (".claude/rules", ".agents/rules"),
    (".claude/agent-memory", ".agents/agent-memory"),
    ("~/.claude/teams", "<host-team-state>"),
    (".claude/.mcp.json", "<host-mcp-config>"),
    ("node .claude/scripts/set-active-plan.cjs {plan-dir}", "Update the active-plan pointer with the host's native plan mechanism."),
    ("CLAUDE.md", "AGENTS.md"),
    ("Claude Native Tasks", "host task tracking"),
    ("Claude native Tasks", "host task tracking"),
    ("Claude Tasks", "host task tracking"),
    ("Claude Code", "the host coding agent"),
    ("TaskCreate", "host task-create capability"),
    ("TaskUpdate", "host task-update capability"),
    ("TaskGet", "host task-read capability"),
    ("TaskList", "host task-list capability"),
    ("TodoWrite", "host checklist capability"),
    ("TeamCreate", "host team-start capability"),
    ("TeamDelete", "host team-finish capability"),
    ("SendMessage", "host teammate-message capability"),
    ("AskUserQuestion", "host user-input capability"),
    ("https://docs.claude.com/en/docs/claude-code/skills.md", "https://codelabs.developers.google.com/getting-started-with-antigravity-skills"),
    ("https://docs.claude.com/en/docs/agents-and-tools/agent-skills/best-practices.md", "https://codelabs.developers.google.com/getting-started-with-antigravity-skills"),
    ("https://code.claude.com/docs/en/plugin-marketplaces.md", "https://developers.openai.com/"),
)

_TO_CLAUDE = (
    ("./.agents/skills", "./.claude/skills"),
    (".agents/skills", ".claude/skills"),
    ("./.agents/rules", "./.claude/rules"),
    (".agents/rules", ".claude/rules"),
    (".agents/agent-memory", ".claude/agent-memory"),
    ("AGENTS.md", "CLAUDE.md"),
)

_MACHINE_PATH = re.compile(
    r"(?<![A-Za-z0-9_])(?:[A-Za-z]:[\\/][^`\"')\r\n]+|\\\\[^`\"')\r\n]+|/(?:Users|home|Volumes)/[^`\"')\r\n]+)"
)
_PORTABLE_FRONTMATTER_KEYS = {"name", "description", "license", "allowed-tools", "metadata"}


def _normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").rstrip() + "\n"


def _replace(text: str, replacements: tuple[tuple[str, str], ...]) -> str:
    for source, destination in replacements:
        text = text.replace(source, destination)
    return text


def _neutralize_machine_paths(text: str) -> str:
    return _MACHINE_PATH.sub("<host-path>", text)


def _unquote_scalar(raw: str) -> str:
    """Return the value of a one-line YAML scalar, undoing its quoting.

    The portable render writes the description back with ``json.dumps``, so a
    quoted value must be unescaped first; stripping only the outer quotes would
    escape every inner ``\\"`` again on each render and the mirror would never
    compare equal to its source.
    """
    if len(raw) > 1 and raw[0] == raw[-1] == '"':
        try:
            value = json.loads(raw)
        except json.JSONDecodeError:
            return raw[1:-1]
        return value if isinstance(value, str) else raw[1:-1]
    if len(raw) > 1 and raw[0] == raw[-1] == "'":
        return raw[1:-1].replace("''", "'")
    return raw


def _fold_routing_metadata(text: str) -> str:
    lines = text.splitlines()
    if not lines or lines[0] != "---":
        return text
    try:
        end = lines.index("---", 1)
    except ValueError:
        return text
    description_index = next((index for index in range(1, end) if lines[index].startswith("description:")), None)
    if description_index is None:
        return text
    when = next((line.split(":", 1)[1].strip().strip("\"'") for line in lines[1:end] if line.startswith("when_to_use:")), "")
    keywords = next((line.split(":", 1)[1].strip().strip("[]") for line in lines[1:end] if line.startswith("keywords:")), "")
    description = _unquote_scalar(lines[description_index].split(":", 1)[1].strip())
    # Neutralise the host name after the additions and compare in neutral form,
    # so a when_to_use already present in the description is never appended again.
    neutral = lambda value: re.sub(r"\bClaude(?: Code)?\b", "coding agents", value)
    additions = []
    if when and neutral(when).casefold() not in neutral(description).casefold():
        additions.append(when.rstrip("."))
    if keywords and "keywords:" not in description.casefold():
        additions.append("Keywords: " + keywords)
    if additions:
        description = description.rstrip(".") + ". " + ". ".join(additions) + "."
    description = neutral(description)
    description = description.replace("<", "").replace(">", "")
    if len(description) > 1024:
        description = description[:1021].rstrip() + "..."
    lines[description_index] = "description: " + json.dumps(description, ensure_ascii=False)

    frontmatter: list[str] = [lines[0]]
    skip_block = False
    for line in lines[1:end]:
        key = re.match(r"^([A-Za-z0-9_-]+):", line)
        if key:
            skip_block = key.group(1) not in _PORTABLE_FRONTMATTER_KEYS
        if not skip_block:
            frontmatter.append(line)
    return "\n".join(frontmatter + ["---"] + lines[end + 1:])


def render_portable(content: bytes) -> bytes:
    text = _normalize_newlines(content.decode("utf-8"))
    text = _CONTRACT_PATTERN.sub("", text)
    text = _replace(text, _TO_PORTABLE)
    text = _neutralize_machine_paths(text)
    text = _fold_routing_metadata(text)
    lines = text.splitlines()
    try:
        end = lines.index("---", 1)
    except ValueError:
        return text.encode("utf-8")
    body = lines[end + 1:]
    while body and not body[0].strip():
        body.pop(0)
    lines = lines[:end + 1] + ["", _CONTRACT.rstrip(), ""] + body
    return _normalize_newlines("\n".join(lines)).encode("utf-8")


def render_claude(content: bytes) -> bytes:
    text = _CONTRACT_PATTERN.sub("", content.decode("utf-8"))
    text = _replace(text, _TO_CLAUDE)
    text = _neutralize_machine_paths(text)
    return _normalize_newlines(text).encode("utf-8")
