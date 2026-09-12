"""Thin wrapper over notebooklm-py for this repo's automation scripts.

Two jobs only:

1. Open a client bound to the CLI's stored session, so scripts and the
   ``notebooklm`` CLI always act as the same identity.
2. Generate an artifact while guaranteeing its task id is recorded *before*
   we start waiting. A generate call consumes daily quota the moment the
   server accepts it; if we lose the id to a timeout or a Ctrl-C, that quota
   is spent on an artifact we can no longer poll for or download.

``notebooklm`` is imported lazily inside the functions that need it. The
package lives only in ``scripts/.venv``, so importing it at module scope
would make the pure helpers below unimportable under any other interpreter
and would break test collection.
"""

from __future__ import annotations

import inspect
import logging
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Any, AsyncIterator

log = logging.getLogger(__name__)

# Extensions the NotebookLM upload endpoint accepts as file sources.
# Anything else has to be added as a URL or as inline text instead.
SUPPORTED_UPLOAD_SUFFIXES = frozenset({
    ".pdf", ".txt", ".md", ".markdown", ".doc", ".docx",
    ".pptx", ".rtf", ".odt", ".csv", ".tsv", ".epub",
})

# Artifact kinds exposed as generate_*/download_* pairs on client.artifacts.
ARTIFACT_KINDS = frozenset({
    "audio", "video", "cinematic_video", "report", "study_guide",
    "slide_deck", "quiz", "flashcards", "infographic", "mind_map",
    "data_table",
})


def is_uploadable(path: str | Path) -> bool:
    """True if the path's extension is accepted as a file source."""
    return Path(path).suffix.lower() in SUPPORTED_UPLOAD_SUFFIXES


def reject_unsupported(paths: list[str | Path]) -> list[Path]:
    """Return the paths that cannot be uploaded, for the caller to report."""
    return [Path(p) for p in paths if not is_uploadable(p)]


@asynccontextmanager
async def open_client(profile: str | None = None) -> AsyncIterator[Any]:
    """Open a client using the session created by ``notebooklm login``.

    Passing ``profile=None`` resolves the active profile the same way the CLI
    does, which keeps scripts and CLI on one identity. Pass a name only when
    a script must pin a specific account.
    """
    from notebooklm import NotebookLMClient  # lazy: see module docstring

    async with NotebookLMClient.from_storage(profile=profile) as client:
        yield client


async def generate_and_wait(
    client: Any,
    notebook_id: str,
    kind: str,
    *,
    timeout: float | None = None,
    on_task_id: Any = None,
    **options: Any,
) -> Any:
    """Generate one artifact, recording its task id before waiting on it.

    ``on_task_id`` is called with the id as soon as the server returns it —
    pass the pipeline's state-writer here so an interrupted run can resume
    against the same task instead of paying for a second generation.
    """
    if kind not in ARTIFACT_KINDS:
        raise ValueError(f"unknown artifact kind {kind!r}; expected one of {sorted(ARTIFACT_KINDS)}")

    generate = getattr(client.artifacts, f"generate_{kind}")
    # The generate_* methods do not share one signature: quiz and flashcards take
    # no `language`, and the style/length options differ per kind. Drop what this
    # kind cannot accept rather than raising TypeError mid-run, but say so --
    # silently swallowing `language` would ship English assets for Vietnamese
    # sources and nobody would know until they opened the file.
    accepted = set(inspect.signature(generate).parameters)
    dropped = [k for k in options if k not in accepted]
    for key in dropped:
        log.warning("generate_%s does not accept %r; ignoring it", kind, key)
    options = {k: v for k, v in options.items() if k in accepted}

    status = await generate(notebook_id, **options)
    task_id = getattr(status, "task_id", None)

    # Record first, wait second. Never reorder these two.
    log.info("generate_%s accepted: notebook=%s task=%s", kind, notebook_id, task_id)
    if on_task_id is not None:
        on_task_id(kind, task_id)

    if task_id is None:
        # Some kinds (mind_map observed) return no task id. Polling on None just
        # scans the artifact list for nothing and gives up ~28s later, so the
        # caller must not read that as success -- it has to check the artifact
        # list itself. Returning None says "generated, completion unverified".
        log.warning(
            "generate_%s returned no task_id; cannot poll for completion", kind
        )
        return None

    wait_kwargs = {"timeout": timeout} if timeout is not None else {}
    return await client.artifacts.wait_for_completion(notebook_id, task_id, **wait_kwargs)


async def download_artifact(client: Any, notebook_id: str, kind: str, dest: str | Path, **options: Any) -> Path:
    """Download a completed artifact to ``dest`` and return the path."""
    if kind not in ARTIFACT_KINDS:
        raise ValueError(f"unknown artifact kind {kind!r}")
    dest = Path(dest)
    dest.parent.mkdir(parents=True, exist_ok=True)
    download = getattr(client.artifacts, f"download_{kind}")
    await download(notebook_id, str(dest), **options)
    return dest
