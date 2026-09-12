#!/usr/bin/env python3
"""Feed this repo's Revit docs into a NotebookLM notebook and pull study assets out.

Idempotent by design. Sources are re-uploaded only when their content hash
changes, and an artifact is generated only when the notebook does not already
have one of that kind. Generation costs daily quota that resets on a rolling
window, so a re-run that regenerates what already exists is a real cost, not a
wasted second.

Run --dry-run first. It performs read-only calls (list notebooks, sources and
artifacts) so the plan it prints reflects real server state, but it never
uploads, generates or chats -- the three things that consume quota.
"""

from __future__ import annotations

import argparse
import asyncio
import hashlib
import json
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from notebooklm_client import generate_and_wait, is_uploadable, open_client  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = Path(__file__).resolve().parent / "notebooklm-sources.json"
OUT_DIR = ROOT / "output" / "notebooklm"
STATE_PATH = OUT_DIR / "state.json"

# kind -> (ArtifactType member name, downloaded extension, wait timeout in seconds)
#
# The library defaults to 300s, which is fine for text but not for audio: an
# audio overview of seven documents was still `in_progress` when that expired.
# The generation keeps running server-side either way -- the timeout only
# decides whether this run waits for it or picks it up on the next one.
ARTIFACT_SPEC = {
    "report": ("REPORT", ".md", 300.0),
    "quiz": ("QUIZ", ".json", 300.0),
    "flashcards": ("FLASHCARDS", ".json", 300.0),
    "mind_map": ("MIND_MAP", ".json", 300.0),
    "audio": ("AUDIO", ".m4a", 1800.0),
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def empty_state() -> dict:
    return {"notebook_id": None, "sources": {}, "artifacts": {}}


def load_state() -> dict:
    if not STATE_PATH.exists():
        return empty_state()
    try:
        state = json.loads(STATE_PATH.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        print(f"  ! state unreadable ({exc}); starting fresh")
        return empty_state()
    if not isinstance(state, dict):
        return empty_state()
    state.setdefault("notebook_id", None)
    state.setdefault("sources", {})
    state.setdefault("artifacts", {})
    return state


def save_state(state: dict) -> None:
    """Write via temp file + replace so an interrupt cannot leave a torn state."""
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    tmp = STATE_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(state, indent=2, ensure_ascii=False), encoding="utf-8")
    os.replace(tmp, STATE_PATH)


def load_manifest() -> dict:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    bad = [s for s in manifest["sources"] if not is_uploadable(s)]
    if bad:
        raise SystemExit(f"manifest lists non-uploadable files: {bad}")
    missing = [s for s in manifest["sources"] if not (ROOT / s).is_file()]
    if missing:
        raise SystemExit(f"manifest lists missing files: {missing}")
    unknown = [k for k in manifest["artifacts"] if k not in ARTIFACT_SPEC]
    if unknown:
        raise SystemExit(f"manifest lists unknown artifact kinds: {unknown}")
    return manifest


async def resolve_notebook(client, manifest, state, dry_run):
    """Reuse the notebook in state; fall back to title match; else create."""
    if state["notebook_id"]:
        existing = await client.notebooks.get_or_none(state["notebook_id"])
        if existing:
            return state["notebook_id"], False
        print("  ! recorded notebook is gone; will pick another")
    title = manifest["notebook_title"]
    for nb in await client.notebooks.list():
        if nb.title == title:
            return nb.id, False
    if dry_run:
        return None, True
    nb = await client.notebooks.create(title)
    return nb.id, True


async def sync_sources(client, notebook_id, manifest, state, dry_run):
    planned, skipped = [], []
    for rel in manifest["sources"]:
        digest = sha256(ROOT / rel)
        record = state["sources"].get(rel)
        if record and record.get("hash") == digest:
            skipped.append(rel)
        else:
            planned.append((rel, digest))
    if dry_run or notebook_id is None:
        return planned, skipped
    for rel, digest in planned:
        previous = (state["sources"].get(rel) or {}).get("source_id")
        print(f"  {'replacing' if previous else 'uploading'} {rel}")
        source = await client.sources.add_file(
            notebook_id, str(ROOT / rel), wait=True, title=Path(rel).name
        )
        state["sources"][rel] = {"hash": digest, "source_id": source.id}
        save_state(state)
        # Retire the superseded copy only after the new one is in. A file source
        # cannot be refreshed in place -- the server has no way back to a local
        # path -- so an edited document would otherwise leave its stale version
        # sitting in the notebook, and the assets would be built from both.
        if previous and previous != source.id:
            try:
                await client.sources.delete(notebook_id, previous)
            except Exception as exc:  # noqa: BLE001 - never lose the new upload
                print(f"  ! could not remove stale copy of {rel}: {exc}")
    return planned, skipped


async def existing_kinds(client, notebook_id) -> set:
    """Kinds the notebook already has -- including free first-add artifacts."""
    if notebook_id is None:
        return set()
    from notebooklm.types import ArtifactType

    present = set()
    for kind, (type_name, _ext, _timeout) in ARTIFACT_SPEC.items():
        found = await client.artifacts.list(notebook_id, getattr(ArtifactType, type_name))
        if found:
            present.add(kind)
    return present


async def produce_artifacts(client, notebook_id, manifest, state, dry_run):
    have = await existing_kinds(client, notebook_id)
    wanted = list(manifest["artifacts"])
    harvest = [k for k in wanted if k in have]
    to_generate = [k for k in wanted if k not in have]
    if dry_run or notebook_id is None:
        return harvest, to_generate

    language = manifest.get("language")
    # `language` alone is not enough. quiz and flashcards reject the parameter
    # outright, and mind_map accepted it yet still produced English from
    # Vietnamese sources. All three take free-text `instructions`, so state the
    # language there too; generate_and_wait drops whichever the kind refuses.
    instructions = manifest.get("instructions")

    def record(kind: str, task_id: str) -> None:
        state["artifacts"][kind] = {"task_id": task_id, "status": "generating"}
        save_state(state)

    from notebooklm.exceptions import ArtifactTimeoutError

    for kind in to_generate:
        print(f"  generating {kind}")
        try:
            result = await generate_and_wait(
                client,
                notebook_id,
                kind,
                timeout=ARTIFACT_SPEC[kind][2],
                on_task_id=record,
                language=language,
                instructions=instructions,
            )
        except ArtifactTimeoutError:
            # The server keeps working on it. Record that and move on rather than
            # aborting the run -- one slow artifact must not block downloading
            # the ones that are already finished.
            print(f"  ! {kind} still generating server-side; will resume next run")
            state["artifacts"].setdefault(kind, {})["status"] = "in_progress"
            save_state(state)
            continue
        # A None result means the kind returned no task id, so completion was
        # never confirmed. Ask the artifact list instead of assuming success --
        # recording "ready" for something that may not exist would make the next
        # run skip it forever.
        if result is None:
            confirmed = kind in await existing_kinds(client, notebook_id)
            status = "ready" if confirmed else "unverified"
            if not confirmed:
                print(f"  ! {kind} not visible after generating; leaving unverified")
        else:
            status = "ready"
        state["artifacts"].setdefault(kind, {})["status"] = status
        save_state(state)

    from notebooklm.exceptions import ArtifactNotReadyError

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    listed = await existing_kinds(client, notebook_id)
    for kind in wanted:
        if kind not in listed:
            print(f"  - {kind} not in the notebook yet; skipping download")
            continue
        dest = OUT_DIR / f"{kind}{ARTIFACT_SPEC[kind][1]}"
        try:
            await getattr(client.artifacts, f"download_{kind}")(notebook_id, str(dest))
        except ArtifactNotReadyError:
            # Appearing in the artifact list is not the same as being finished --
            # a generation still running is listed but has nothing to download.
            # One unfinished artifact must not discard the ones that are done.
            print(f"  - {kind} listed but still generating; will download next run")
            state["artifacts"].setdefault(kind, {})["status"] = "in_progress"
            save_state(state)
            continue
        print(f"  downloaded {kind} -> {dest.relative_to(ROOT)}")
        record = state["artifacts"].setdefault(kind, {})
        record["downloaded_path"] = str(dest.relative_to(ROOT))
        # A successful download settles the question: anything still carrying
        # "in_progress" from an earlier run that timed out is finished now.
        record["status"] = "ready"
        save_state(state)
    return harvest, to_generate


async def run(dry_run: bool) -> int:
    manifest = load_manifest()
    state = load_state()
    async with open_client() as client:
        print(f"account: {await client.get_account_email()}")

        notebook_id, is_new = await resolve_notebook(client, manifest, state, dry_run)
        label = notebook_id or "(would create)"
        print(f"notebook: {label}{' [new]' if is_new else ''}")

        # Persist the id immediately. Saving it only at the end means a crash
        # midway orphans a notebook that already holds uploaded sources; the
        # title fallback in resolve_notebook recovers it only until someone
        # renames the notebook.
        if not dry_run and notebook_id and state.get("notebook_id") != notebook_id:
            state["notebook_id"] = notebook_id
            save_state(state)

        planned, skipped = await sync_sources(client, notebook_id, manifest, state, dry_run)
        print(f"sources: {len(planned)} to upload, {len(skipped)} unchanged")
        for rel, _ in planned:
            print(f"  + {rel}")

        harvest, to_generate = await produce_artifacts(
            client, notebook_id, manifest, state, dry_run
        )
        print(
            f"artifacts: {len(harvest)} already exist (free), "
            f"{len(to_generate)} to generate"
        )
        for kind in harvest:
            print(f"  = {kind} (reuse, no quota)")
        for kind in to_generate:
            print(f"  * {kind} (costs quota)")

        if dry_run:
            print("\n[dry-run] nothing uploaded, generated or downloaded.")
        else:
            state["notebook_id"] = notebook_id
            save_state(state)
            print(f"state: {STATE_PATH.relative_to(ROOT)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Build NotebookLM course assets.")
    parser.add_argument(
        "--dry-run", action="store_true", help="plan only; read-only API calls"
    )
    args = parser.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, OSError):
        pass
    return asyncio.run(run(args.dry_run))


if __name__ == "__main__":
    raise SystemExit(main())
