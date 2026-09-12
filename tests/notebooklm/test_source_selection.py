"""Source-eligibility and manifest-validation tests.

The upload endpoint accepts a fixed extension set; anything else has to become a
URL or inline text instead. Getting this wrong wastes an upload round trip and,
worse, can silently drop a document from the notebook the assets are built from.

No network calls here either -- `notebooklm_client` imports the library lazily,
so these run under a plain interpreter.
"""

from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))

import notebooklm_client as client  # noqa: E402

MANIFEST = json.loads((SCRIPTS / "notebooklm-sources.json").read_text(encoding="utf-8"))


class UploadEligibilityTests(unittest.TestCase):
    def test_markdown_is_accepted(self):
        # The whole point of D7: repo docs upload as-is, no PDF conversion step.
        self.assertTrue(client.is_uploadable("docs/code-standards.md"))
        self.assertTrue(client.is_uploadable("notes.markdown"))

    def test_common_repo_files_that_are_not_accepted(self):
        for path in ("book.xlsx", "Model.rvt", "Program.cs", "styles.xaml"):
            self.assertFalse(client.is_uploadable(path), path)

    def test_extension_match_is_case_insensitive(self):
        self.assertTrue(client.is_uploadable("README.MD"))
        self.assertTrue(client.is_uploadable("Guide.PDF"))

    def test_reject_unsupported_returns_only_the_bad_paths(self):
        rejected = client.reject_unsupported(["a.md", "b.xlsx", "c.pdf", "d.rvt"])
        self.assertEqual([p.name for p in rejected], ["b.xlsx", "d.rvt"])

    def test_a_path_with_no_extension_is_rejected(self):
        self.assertFalse(client.is_uploadable("Makefile"))


class ManifestTests(unittest.TestCase):
    def test_every_listed_source_exists_on_disk(self):
        missing = [s for s in MANIFEST["sources"] if not (ROOT / s).is_file()]
        self.assertEqual(missing, [], f"manifest references missing files: {missing}")

    def test_every_listed_source_is_uploadable(self):
        bad = [s for s in MANIFEST["sources"] if not client.is_uploadable(s)]
        self.assertEqual(bad, [], f"manifest lists non-uploadable files: {bad}")

    def test_sources_are_an_explicit_allow_list_not_a_glob(self):
        # A glob would let unrelated files reach a third-party service by accident.
        for entry in MANIFEST["sources"]:
            self.assertNotIn("*", entry)
            self.assertNotIn("?", entry)

    def test_sources_stay_inside_the_repo(self):
        for entry in MANIFEST["sources"]:
            resolved = (ROOT / entry).resolve()
            self.assertTrue(
                resolved.is_relative_to(ROOT), f"{entry} escapes the repo root"
            )

    def test_no_duplicate_sources(self):
        self.assertEqual(len(MANIFEST["sources"]), len(set(MANIFEST["sources"])))


class ArtifactKindTests(unittest.TestCase):
    def test_manifest_kinds_are_known_to_the_wrapper(self):
        for kind in MANIFEST["artifacts"]:
            self.assertIn(kind, client.ARTIFACT_KINDS)

    def test_unknown_kind_is_rejected_before_any_call(self):
        import asyncio

        with self.assertRaises(ValueError):
            asyncio.run(client.generate_and_wait(None, "nb", "not_a_real_kind"))


class GenerateKwargFilterTests(unittest.TestCase):
    """generate_* signatures differ per kind -- quiz/flashcards take no `language`.

    Passing it anyway raised TypeError mid-run after quota had already been spent
    on earlier artifacts, so the wrapper now drops unsupported keywords.
    """

    def _client(self, accepts_language: bool):
        import asyncio

        seen = {}

        class Artifacts:
            if accepts_language:
                async def generate_quiz(self, notebook_id, language=None, instructions=None):
                    seen.update(notebook_id=notebook_id, language=language)
                    return type("S", (), {"task_id": "t1"})()
            else:
                async def generate_quiz(self, notebook_id, instructions=None):
                    seen.update(notebook_id=notebook_id)
                    return type("S", (), {"task_id": "t1"})()

            async def wait_for_completion(self, notebook_id, task_id, **kw):
                seen["task_id"] = task_id
                return "done"

        class Client:
            artifacts = Artifacts()

        return asyncio.run, Client(), seen

    def test_unsupported_keyword_is_dropped_instead_of_raising(self):
        run, fake, seen = self._client(accepts_language=False)
        run(client.generate_and_wait(fake, "nb-1", "quiz", language="vi"))
        self.assertEqual(seen["notebook_id"], "nb-1")
        self.assertNotIn("language", seen)

    def test_supported_keyword_is_still_passed_through(self):
        run, fake, seen = self._client(accepts_language=True)
        run(client.generate_and_wait(fake, "nb-1", "quiz", language="vi"))
        self.assertEqual(seen["language"], "vi")

    def test_task_id_is_recorded_before_waiting(self):
        import asyncio

        order = []

        class Artifacts:
            async def generate_quiz(self, notebook_id):
                return type("S", (), {"task_id": "t-42"})()

            async def wait_for_completion(self, notebook_id, task_id, **kw):
                order.append("wait")
                return "done"

        class Client:
            artifacts = Artifacts()

        asyncio.run(
            client.generate_and_wait(
                Client(), "nb", "quiz", on_task_id=lambda k, t: order.append(f"record:{t}")
            )
        )
        self.assertEqual(order, ["record:t-42", "wait"])


class MissingTaskIdTests(unittest.TestCase):
    """Some kinds return no task id -- mind_map was observed doing exactly this.

    Polling on None scans the artifact list for nothing and gives up ~28s later.
    Treating that as success would record "ready" for an artifact that may not
    exist, and every later run would skip regenerating it.
    """

    def _run(self, task_id):
        import asyncio

        calls = []

        class Artifacts:
            async def generate_mind_map(self, notebook_id):
                return type("S", (), {"task_id": task_id})()

            async def wait_for_completion(self, notebook_id, tid, **kw):
                calls.append(tid)
                return "done"

        class Client:
            artifacts = Artifacts()

        seen = []
        result = asyncio.run(
            client.generate_and_wait(
                Client(), "nb", "mind_map", on_task_id=lambda k, t: seen.append(t)
            )
        )
        return result, calls, seen

    def test_missing_task_id_returns_none_and_skips_polling(self):
        result, polled, _ = self._run(None)
        self.assertIsNone(result, "caller must be able to tell completion was unverified")
        self.assertEqual(polled, [], "must not poll on a None task id")

    def test_missing_task_id_is_still_reported_to_the_recorder(self):
        _, _, seen = self._run(None)
        self.assertEqual(seen, [None])

    def test_present_task_id_still_polls_and_returns(self):
        result, polled, seen = self._run("t-7")
        self.assertEqual(result, "done")
        self.assertEqual(polled, ["t-7"])
        self.assertEqual(seen, ["t-7"])


if __name__ == "__main__":
    unittest.main()
