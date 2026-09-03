"""Black-box helpers for the repository-local skill-sync CLI contract."""

from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
ENGINE = REPOSITORY_ROOT / "scripts" / "sync-agent-skills.py"
FIXTURES = Path(__file__).resolve().parent / "fixtures"


def tree_hashes(root: Path) -> dict[str, str]:
    """Return content hashes for a tree, independent of the sync engine."""
    return {
        path.relative_to(root).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
        for path in sorted(root.rglob("*"))
        if path.is_file()
    }


def sha256_file(path: Path) -> str:
    """Hash a hand-authored fixture artifact without using sync-engine code."""
    return hashlib.sha256(path.read_bytes()).hexdigest()


class SkillSyncFixtureCase(unittest.TestCase):
    """Runs the planned CLI exclusively against a copied fixture workspace."""

    fixture_name: str

    def setUp(self) -> None:
        self._temporary_directory = tempfile.TemporaryDirectory()
        self.workspace = Path(self._temporary_directory.name) / "workspace"
        shutil.copytree(FIXTURES / self.fixture_name, self.workspace)

    def tearDown(self) -> None:
        self._temporary_directory.cleanup()

    def run_sync(self, command: str, *arguments: str, timeout: int = 10) -> subprocess.CompletedProcess[str]:
        # The suite deliberately starts RED in Phase 0.  Once the engine exists,
        # every assertion below executes it as a separate process against fixtures.
        if not ENGINE.is_file():
            self.fail(
                "Phase 0 RED: planned engine scripts/sync-agent-skills.py is absent; "
                "the fixture contract has no production implementation yet."
            )

        return subprocess.run(
            [
                sys.executable,
                str(ENGINE),
                command,
                "--root",
                str(self.workspace),
                "--config",
                str(self.workspace / "config.json"),
                *arguments,
            ],
            text=True,
            capture_output=True,
            timeout=timeout,
            check=False,
        )

    def assert_success(self, result: subprocess.CompletedProcess[str]) -> None:
        self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def expected_json(self, name: str = "assertions.json") -> dict[str, object]:
        return json.loads((self.workspace / "expected" / name).read_text(encoding="utf-8"))
