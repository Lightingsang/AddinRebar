"""Small immutable-ish data types shared by the sync engine."""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path


@dataclass(frozen=True)
class SyncConfig:
    root: Path
    claude_dir: Path
    portable_dir: Path
    state_dir: Path
    lock_stale_seconds: int = 3600
    bootstrap: dict | None = None


@dataclass(frozen=True)
class FileState:
    relative_path: str
    content: bytes
    sha256: str


@dataclass(frozen=True)
class SkillBundle:
    logical_id: str
    provider: str
    root: Path
    relative_root: str
    files: tuple[FileState, ...]


@dataclass(frozen=True)
class Finding:
    level: str
    message: str
    path: str = ""


@dataclass(frozen=True)
class ValidationReport:
    findings: tuple[Finding, ...] = ()
    shared_dependencies: tuple[tuple[str, str, str], ...] = ()

    @property
    def valid(self) -> bool:
        return not any(finding.level == "error" for finding in self.findings)


@dataclass(frozen=True)
class ScanResult:
    bundles: dict[str, SkillBundle]
    findings: tuple[Finding, ...] = ()


@dataclass(frozen=True)
class ManifestFileRecord:
    claude_base_sha256: str
    portable_base_sha256: str
    semantic_base_sha256: str
    adapter: str

    def to_dict(self) -> dict:
        return {
            "adapter": self.adapter,
            "claude_base_sha256": self.claude_base_sha256,
            "portable_base_sha256": self.portable_base_sha256,
            "semantic_base_sha256": self.semantic_base_sha256,
        }


@dataclass(frozen=True)
class ManifestSkillRecord:
    claude_path: str
    portable_path: str
    files: dict[str, ManifestFileRecord]
    status: str

    def to_dict(self) -> dict:
        return {
            "claude_path": self.claude_path,
            "portable_path": self.portable_path,
            "files": {path: record.to_dict() for path, record in self.files.items()},
            "status": self.status,
        }


@dataclass(frozen=True)
class Manifest:
    """Validated, typed durable synchronization state."""

    schema_version: int
    skills: dict[str, ManifestSkillRecord]

    def to_dict(self) -> dict:
        return {"schema_version": self.schema_version, "skills": {name: skill.to_dict() for name, skill in self.skills.items()}}


@dataclass(frozen=True)
class PlannedWrite:
    destination: Path
    content: bytes


@dataclass
class ChangePlan:
    writes: list[PlannedWrite] = field(default_factory=list)
    manifest: dict = field(default_factory=dict)
    conflicts: list[dict] = field(default_factory=list)
    drift: list[str] = field(default_factory=list)
    deletions: list[str] = field(default_factory=list)
    pending: list[str] = field(default_factory=list)
    findings: list[Finding] = field(default_factory=list)

    @property
    def valid(self) -> bool:
        return not any(finding.level == "error" for finding in self.findings)
