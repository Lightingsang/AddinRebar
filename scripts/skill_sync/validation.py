"""Bundle validation, local references, and `_shared` dependency edges."""

from __future__ import annotations

import re
from pathlib import Path

from .frontmatter import parse_frontmatter
from .models import Finding, SkillBundle, ValidationReport


_MARKDOWN_LINK = re.compile(r"\[[^\]]*\]\(([^)\s]+)(?:\s+[^)]*)?\)")
_BACKTICK = re.compile(r"`([^`\r\n]+)`")
_FENCED_CODE = re.compile(r"```.*?```", re.DOTALL)
_BARE_MACHINE_PATH = re.compile(r"(?<![\w:/.])(?:[A-Za-z]:[\\/][^\s`]+|\\\\[^\s`]+)")
_SLASH_COMMAND = re.compile(r"^/[A-Za-z0-9_*:-]+$")
_BUNDLE_PREFIXES = ("references/", "scripts/", "assets/", "templates/", "data/", "fixtures/", "deprecated/", "eval-viewer/", "_shared/")
_POSIX_MACHINE_ROOT = re.compile(r"^/(?:etc|opt|var|Users|home|root|Volumes|mnt)(?:/|$)")


def _targets(text: str) -> dict[str, bool]:
    prose = _FENCED_CODE.sub("", text)
    # True means an explicit Markdown link. Backticked paths are often examples,
    # so missing-path checks can apply a less aggressive policy to them.
    targets = {value: True for value in _MARKDOWN_LINK.findall(prose)}
    for value in _BACKTICK.findall(prose):
        candidate = value.strip()
        machine = _machine_path(candidate)
        bundled = candidate.startswith(_BUNDLE_PREFIXES + ("../_shared/",))
        if " " not in candidate and (machine or bundled) and not candidate.startswith(("http:", "https:", "mailto:", "#")):
            targets.setdefault(candidate, False)
    for match in _BARE_MACHINE_PATH.finditer(prose):
        targets.setdefault(match.group(0).rstrip(".,;)"), False)
    return targets


def _local_target(value: str) -> bool:
    return bool(value) and not value.startswith(("#", "http:", "https:", "mailto:"))


def _machine_path(value: str) -> bool:
    if _SLASH_COMMAND.fullmatch(value):
        return False
    if value.startswith(("/path/to/", "/absolute/path/", "/usr/bin/env", "/dev/null", "/dev/stdin", "/tmp/")):
        return False
    return bool(re.match(r"^[A-Za-z]:[\\/]", value)) or value.startswith(("\\\\", "//")) or bool(_POSIX_MACHINE_ROOT.match(value))


def _placeholder_target(value: str) -> bool:
    return value.casefold() in {"url", "path", "file"} or "<" in value or "{" in value or bool(re.search(r"(?:^|[-_/])N(?:\.|[-_/])", value))


def _exists_elsewhere(provider_root: Path, value: str) -> bool:
    if any(character in value for character in "*?["):
        return False
    name = Path(value).name
    return any(candidate.as_posix().endswith("/" + value.replace("\\", "/")) for candidate in provider_root.rglob(name))


def validate_bundle(bundle: SkillBundle, provider_root: Path) -> ValidationReport:
    findings: list[Finding] = []
    dependencies: set[tuple[str, str, str]] = set()
    index = {file.relative_path: file for file in bundle.files}
    skill = index.get("SKILL.md")
    if skill is None:
        return ValidationReport((Finding("error", "bundle lacks SKILL.md", bundle.relative_root),))
    try:
        text = skill.content.decode("utf-8")
        parse_frontmatter(skill.content)
    except (UnicodeDecodeError, ValueError) as error:
        return ValidationReport((Finding("error", str(error), bundle.relative_root + "/SKILL.md"),))
    for value, explicit_link in _targets(text).items():
        if not _local_target(value):
            continue
        if _SLASH_COMMAND.fullmatch(value):
            continue
        if _placeholder_target(value):
            continue
        if _machine_path(value):
            findings.append(Finding("error", "machine-specific absolute path is prohibited", value))
            continue
        target = (bundle.root / value).resolve()
        try:
            relative = target.relative_to(provider_root.resolve()).as_posix()
        except ValueError:
            findings.append(Finding("error", "reference escapes provider root", value))
            continue
        if any(character in value for character in "*?["):
            matches = tuple(bundle.root.glob(value))
            prefix_exists = (bundle.root / value.split("/", 1)[0]).exists()
            if not matches and (explicit_link or prefix_exists):
                findings.append(Finding("error", "referenced local file is missing", value))
            continue
        if not target.exists():
            prefix_exists = (bundle.root / value.split("/", 1)[0]).exists()
            directory_example = value.endswith("/")
            if explicit_link or (prefix_exists and not directory_example and not _exists_elsewhere(provider_root, value)):
                findings.append(Finding("error", "referenced local file is missing", value))
        elif target.is_file() and relative.startswith("_shared/"):
            dependencies.add((bundle.provider, bundle.logical_id, relative))
    return ValidationReport(tuple(findings), tuple(sorted(dependencies)))


def validate_all(bundles: dict[str, SkillBundle], provider_root: Path) -> ValidationReport:
    findings: list[Finding] = []
    dependencies: set[tuple[str, str, str]] = set()
    for bundle in bundles.values():
        report = validate_bundle(bundle, provider_root)
        findings.extend(report.findings)
        dependencies.update(report.shared_dependencies)
    return ValidationReport(tuple(findings), tuple(sorted(dependencies)))
