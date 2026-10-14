"""Prepare the AI clash judgement: group alike clashes of an HP clash snapshot and pick the pilot clashes.

    python clash-ai-groups.py SNAPSHOT.json [--pilot 10] [--only-pilot]

Writes <snapshot>-ai-groups.json beside the snapshot: one entry per group (same test, same category and family on both
sides, same distance band) with its size, distance range, the levels / types / systems it spans and up to three sample
clashes (pictured ones first, with the Navisworks picture path) — what Claude reads to write the group's judgement into
<snapshot>-ai.json. --pilot N also lists N pictured clashes from as many different tests and priority groups as possible.
"""

import argparse
import hashlib
import importlib.util
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
SAMPLES = 3
TOP = 5


def _sibling(file_name):
    spec = importlib.util.spec_from_file_location(file_name.replace("-", "_")[:-3], HERE / file_name)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ai_text = _sibling("clash-ai-text.py")
report_html = _sibling("clash-report-html.py")


def pictures_for(snapshot_path, snapshot):
    if not snapshot.get("htmlReport"):
        return {}
    pictures, _ = report_html.clash_pictures(snapshot_path.parent / snapshot["htmlReport"].replace("\\", "/"))
    return pictures


def sample(row, pictures):
    picture = pictures.get((row["test"], row["name"]), {}).get("image")
    return {"guid": row["guid"], "clash": row["name"], "status": row["status"], "distanceMm": row["distanceMm"], "level": row["level"],
            "grid": row.get("gridLocation"), "centerMm": row.get("centerMm"), "item1": row["item1"], "item2": row["item2"],
            "picture": str(picture) if picture else None}


def build_groups(rows, pictures):
    members = defaultdict(list)
    for row in rows:
        members[ai_text.group_id(row)].append(row)
    groups = []
    for gid, items in members.items():
        first = items[0]
        ordered = sorted(items, key=lambda r: (not r.get("pictured"), abs(r["distanceMm"]) * -1))
        distances = [r["distanceMm"] for r in items]
        groups.append({
            "id": gid, "test": first["test"], "ruleId": first["ruleId"], "priority": first["priority"], "pair": first["pair"],
            "category1": first["item1"]["category"], "family1": first["item1"].get("family") or first["item1"]["name"],
            "category2": first["item2"]["category"], "family2": first["item2"].get("family") or first["item2"]["name"],
            "band": ai_text.band(first["distanceMm"]), "count": len(items),
            "distanceMm": {"min": min(distances), "max": max(distances)},
            "levels": Counter(r["level"] for r in items).most_common(TOP),
            "types1": Counter(r["item1"].get("type", "") for r in items).most_common(TOP),
            "types2": Counter(r["item2"].get("type", "") for r in items).most_common(TOP),
            "systems": Counter(s for r in items for s in (r["item1"].get("system", ""), r["item2"].get("system", "")) if s).most_common(TOP),
            "pictured": sum(1 for r in items if r.get("pictured")),
            "samples": [sample(r, pictures) for r in ordered[:SAMPLES]],
        })
    return sorted(groups, key=lambda g: (g["priority"], -g["count"]))


def pick_pilot(rows, count):
    """One clash of each priority group first (the most pictured, largest test), then one per further test, pictured
    clashes and deeper penetrations first: the pilot covers as many kinds of clash as it can."""
    by_test = defaultdict(list)
    for row in sorted(rows, key=lambda r: (not r.get("pictured"), r["distanceMm"])):
        by_test[row["test"]].append(row)
    head = lambda test: by_test[test][0]
    chosen = []
    for priority in sorted({head(t)["priority"] for t in by_test}):
        tests = [t for t in by_test if head(t)["priority"] == priority]
        chosen.append(by_test[max(tests, key=lambda t: (bool(head(t).get("pictured")), len(by_test[t])))].pop(0))
    for test in sorted(by_test, key=lambda t: (not (by_test[t] and head(t).get("pictured")), t)):
        if len(chosen) >= count:
            break
        if by_test[test] and all(c["test"] != test for c in chosen):
            chosen.append(by_test[test].pop(0))
    return chosen[:count]


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("snapshot")
    parser.add_argument("--pilot", type=int, default=0)
    parser.add_argument("--only-pilot", action="store_true", help="write only the groups of the pilot clashes")
    parser.add_argument("--write-judgements", action="store_true",
                        help="fill <snapshot>-ai.json from clash-ai-knowledge.json for every group it does not judge yet")
    options = parser.parse_args()
    path = Path(options.snapshot)
    snapshot = json.loads(path.read_text(encoding="utf-8"))
    rows = [r for r in snapshot["rows"] if r["status"] in ("New", "Active", "Reviewed", "Resolved")]
    pictures = pictures_for(path, snapshot)
    groups = build_groups(rows, pictures)
    pilot = pick_pilot(rows, options.pilot) if options.pilot else []
    if options.only_pilot:
        wanted = {ai_text.group_id(r) for r in pilot}
        groups = [g for g in groups if g["id"] in wanted]
    out = path.with_name(path.stem + "-ai-groups.json")
    out.write_text(json.dumps({"snapshot": path.name, "groups": groups, "pilot": [sample(r, pictures) | {"group": ai_text.group_id(r)} for r in pilot]},
                              ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{len(groups)} group(s) of {len(rows)} clash(es), {len(pilot)} pilot clash(es) -> {out}")
    if options.write_judgements:
        write_judgements(path, groups)


def write_judgements(snapshot_path, groups):
    """Keep every judgement written for a group (the pilot's, edits by hand); (re)write the rest from the knowledge base,
    so a corrected knowledge entry reaches every group of its kind on the next run."""
    knowledge = json.loads(ai_text.KNOWLEDGE_FILE.read_text(encoding="utf-8"))
    ai_path = snapshot_path.with_name(snapshot_path.stem + "-ai.json")
    data = json.loads(ai_path.read_text(encoding="utf-8")) if ai_path.is_file() else {"schemaVersion": 1, "snapshot": snapshot_path.name, "groups": {}}
    added = refreshed = removed = 0
    unknown = Counter()
    for group in groups:
        current = data["groups"].get(group["id"])
        from_knowledge = current is not None and str(current.get("source", "")).startswith("knowledge:")
        if current is not None and not from_knowledge:
            continue  # written for this group (pilot, hand edit): never overwritten
        judgement = ai_text.judgement_from_knowledge(group, knowledge["judgements"])
        if judgement is None:
            unknown[ai_text.knowledge_key(group)] += group["count"]
            if from_knowledge:
                del data["groups"][group["id"]]  # its kind left the knowledge base
                removed += 1
            continue
        data["groups"][group["id"]] = judgement
        refreshed, added = (refreshed + 1, added) if from_knowledge else (refreshed, added + 1)
    data["knowledge"] = {"author": knowledge.get("author"), "sha256": hashlib.sha256(ai_text.KNOWLEDGE_FILE.read_bytes()).hexdigest()[:16]}
    ai_path.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"judgements: {added} added, {refreshed} refreshed, {removed} removed from the knowledge base; {len(data['groups'])} in all -> {ai_path}")
    for key, count in unknown.most_common():
        print(f"  no judgement for {key} ({count} clash(es))")


if __name__ == "__main__":
    sys.exit(main())
