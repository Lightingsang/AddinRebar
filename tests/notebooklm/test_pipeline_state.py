"""State and change-detection tests for the NotebookLM course-asset pipeline.

These run under any interpreter, including one with no `notebooklm` installed:
the pipeline imports the library lazily, so only pure helpers are touched here.
Nothing in this file performs a network call.
"""

from __future__ import annotations

import importlib.util
import json
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"


def load_pipeline():
    """Import the pipeline by path -- its filename is kebab-case, not importable."""
    sys.path.insert(0, str(SCRIPTS))
    spec = importlib.util.spec_from_file_location(
        "build_notebooklm_course_assets", SCRIPTS / "build-notebooklm-course-assets.py"
    )
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


pipeline = load_pipeline()


class HashTests(unittest.TestCase):
    def test_hash_is_stable_for_identical_content(self):
        with TemporaryDirectory() as tmp:
            a, b = Path(tmp) / "a.md", Path(tmp) / "b.md"
            a.write_text("# same", encoding="utf-8")
            b.write_text("# same", encoding="utf-8")
            self.assertEqual(pipeline.sha256(a), pipeline.sha256(b))

    def test_hash_changes_when_content_changes(self):
        with TemporaryDirectory() as tmp:
            path = Path(tmp) / "a.md"
            path.write_text("# before", encoding="utf-8")
            before = pipeline.sha256(path)
            path.write_text("# after", encoding="utf-8")
            self.assertNotEqual(before, pipeline.sha256(path))


class StateTests(unittest.TestCase):
    def setUp(self):
        self._tmp = TemporaryDirectory()
        tmp = Path(self._tmp.name)
        # Redirect the module-level paths at the pipeline, not at the real repo.
        self._saved = (pipeline.OUT_DIR, pipeline.STATE_PATH)
        pipeline.OUT_DIR = tmp / "out"
        pipeline.STATE_PATH = pipeline.OUT_DIR / "state.json"

    def tearDown(self):
        pipeline.OUT_DIR, pipeline.STATE_PATH = self._saved
        self._tmp.cleanup()

    def test_missing_state_yields_empty_shape(self):
        state = pipeline.load_state()
        self.assertEqual(state, {"notebook_id": None, "sources": {}, "artifacts": {}})

    def test_round_trip_preserves_content(self):
        state = pipeline.empty_state()
        state["notebook_id"] = "nb-123"
        state["sources"]["docs/a.md"] = {"hash": "abc", "source_id": "s1"}
        state["artifacts"]["audio"] = {"task_id": "t1", "status": "ready"}
        pipeline.save_state(state)
        self.assertEqual(pipeline.load_state(), state)

    def test_corrupt_state_does_not_crash(self):
        pipeline.OUT_DIR.mkdir(parents=True, exist_ok=True)
        pipeline.STATE_PATH.write_text("{not json", encoding="utf-8")
        self.assertEqual(pipeline.load_state()["notebook_id"], None)

    def test_partial_state_gets_missing_keys_filled(self):
        pipeline.OUT_DIR.mkdir(parents=True, exist_ok=True)
        pipeline.STATE_PATH.write_text(json.dumps({"notebook_id": "nb-9"}), encoding="utf-8")
        state = pipeline.load_state()
        self.assertEqual(state["notebook_id"], "nb-9")
        self.assertEqual(state["sources"], {})
        self.assertEqual(state["artifacts"], {})

    def test_save_leaves_no_temp_file_behind(self):
        pipeline.save_state(pipeline.empty_state())
        leftovers = list(pipeline.OUT_DIR.glob("*.tmp"))
        self.assertEqual(leftovers, [], f"temp files not cleaned up: {leftovers}")


class ArtifactSpecTests(unittest.TestCase):
    def test_every_manifest_kind_has_a_spec(self):
        manifest = json.loads((SCRIPTS / "notebooklm-sources.json").read_text(encoding="utf-8"))
        for kind in manifest["artifacts"]:
            self.assertIn(kind, pipeline.ARTIFACT_SPEC)

    def test_every_spec_declares_extension_and_timeout(self):
        for kind, (type_name, ext, timeout) in pipeline.ARTIFACT_SPEC.items():
            self.assertTrue(type_name.isupper(), f"{kind}: expected an ArtifactType name")
            self.assertTrue(ext.startswith("."), f"{kind}: extension must start with a dot")
            self.assertGreater(timeout, 0, f"{kind}: timeout must be positive")

    def test_audio_waits_longer_than_the_text_artifacts(self):
        # An audio overview of seven documents outlived the 300s library default.
        audio = pipeline.ARTIFACT_SPEC["audio"][2]
        for kind in ("report", "quiz", "flashcards"):
            self.assertGreater(audio, pipeline.ARTIFACT_SPEC[kind][2], kind)


if __name__ == "__main__":
    unittest.main()
