"""Thin executable wrapper for the repository-local sync engine."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from skill_sync.cli import main


if __name__ == "__main__":
    raise SystemExit(main())
