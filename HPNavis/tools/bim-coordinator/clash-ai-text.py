"""Per-clash AI columns of the HP clash workbook: the group judgement Claude wrote (<snapshot>-ai.json) plus the facts of
each clash, composed into AI_Description / AI_Suggested_Action / AI_Classification / AI_Confidence.

The judgement (impact, action, classification, confidence) is written once per group of alike clashes — same test, same
category and family on both sides, same distance band — by clash-ai-groups.py + Claude; everything that differs from one
clash to the next (elements, ids, level, grid, coordinates, distance) is filled here from the snapshot, never invented.
"Hợp lệ theo thiết kế" passes only through a rule the user approved (clash-acceptance-rules.json) whose categories and
penetration limit the clash meets; otherwise it becomes "Cần xác minh".
"""

import hashlib
import json
from pathlib import Path

CLASSES = ("Cần xử lý", "Hợp lệ theo thiết kế", "Cần xác minh")
VALID = "Hợp lệ theo thiết kế"
VERIFY = "Cần xác minh"
CONFIDENCE = ("Thấp", "Trung bình", "Cao")
BANDS = ((10, "≤10"), (50, "≤50"), (200, "≤200"))
# categories whose Revit elements carry a system worth quoting: pipes, ducts and their equipment. Cable trays and
# conduits have none (only a service type) and devices / fixtures are rarely circuited in a coordination model, so a
# missing system there is not missing data
MEP_CATEGORIES = ("Pipes", "Pipe Fittings", "Pipe Accessories", "Ducts", "Duct Fittings", "Duct Accessories", "Mechanical Equipment",
                  "Plumbing Fixtures", "Sprinklers")
UNKNOWN_LEVEL = "(không rõ)"  # ClashSnapshotRules.UnknownLevel in the engine
HERE = Path(__file__).resolve().parent
RULES_FILE = HERE / "clash-acceptance-rules.json"


def band(distance_mm):
    depth = abs(distance_mm)
    return next((label for limit, label in BANDS if depth <= limit), ">200")


def group_key(row):
    a, b = row["item1"], row["item2"]
    return (row["test"], a["category"], a.get("family") or a["name"], b["category"], b.get("family") or b["name"], band(row["distanceMm"]))


def group_id(row):
    return hashlib.sha1("|".join(group_key(row)).encode("utf-8")).hexdigest()[:10]


def load_rules(path=RULES_FILE):
    if not Path(path).is_file():
        return {}
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    return {rule["id"]: rule for rule in data.get("rules", [])}


def sides_of(row):
    """[[discipline, category], [discipline, category]] of a clash, the discipline of each side from its test's pair."""
    left, right = (row["pair"].split("-") + ["", ""])[:2]
    return sorted([[left, row["item1"]["category"]], [right, row["item2"]["category"]]])


def rule_approved(rule):
    """Approved means approved is the boolean true, a reviewer is named and a limit is set — a string or a gap is not."""
    return bool(rule) and rule.get("approved") is True and bool(str(rule.get("approvedBy") or "").strip()) \
        and isinstance(rule.get("maxPenetrationMm"), (int, float))


def rule_allows(rule, row):
    """An approved rule, one of its sides pairs (discipline + category on both sides, either order), and a penetration
    within the rule's limit: approving masonry-wall overlaps never also approves concrete-wall ones."""
    if not rule_approved(rule):
        return False
    actual = sides_of(row)
    return any(sorted(pair) == actual for pair in rule.get("sides", [])) and abs(row["distanceMm"]) <= float(rule["maxPenetrationMm"])


KNOWLEDGE_FILE = HERE / "clash-ai-knowledge.json"


def knowledge_key(group):
    return f"{group['pair']}|{group['category1']}|{group['category2']}"


def judgement_from_knowledge(group, knowledge):
    """The judgement of a group's kind of clash, with its distance band's overrides; None when the kind is unknown."""
    base = knowledge.get(knowledge_key(group))
    if not base:
        return None
    judgement = {k: v for k, v in base.items() if k != "bands"}
    judgement.update(base.get("bands", {}).get(group["band"], {}))
    # a judgement per kind of clash, written without design drawings, never claims high confidence
    if judgement.get("confidence") == "Cao":
        judgement["confidence"] = "Trung bình"
    if judgement.get("classification") != VALID:
        judgement.pop("ruleId", None)
    judgement["source"] = "knowledge:" + knowledge_key(group)
    return judgement


def short_file(source_file):
    """THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_user.rvt -> LH_HB_HC-ES (block and discipline code)."""
    parts = (source_file or "").split("-")
    return f"{parts[2]}-{parts[5]}" if len(parts) >= 7 else (source_file or "")


def element_text(item):
    name = " – ".join(v for v in (item.get("family") or item["name"], item.get("type")) if v)
    extras = [v for v in (item.get("size"), item.get("system") and f"hệ {item['system']}", item.get("mark") and f"ký hiệu {item['mark']}") if v]
    detail = f" ({', '.join(extras)})" if extras else ""
    ident = f"Id {item['elementId']}" if item.get("elementId") else "không có Element Id"
    return f"{item['category']} {name}{detail} [{ident}, {short_file(item.get('sourceFile'))}]"


def missing_facts(row):
    missing = []
    for label, item in (("cấu kiện 1", row["item1"]), ("cấu kiện 2", row["item2"])):
        for key, text in (("family", "Family"), ("type", "Type"), ("elementId", "Element Id")):
            if not item.get(key):
                missing.append(f"{text} {label}")
        if item["category"] in MEP_CATEGORIES and not item.get("system"):
            missing.append(f"System {label}")
    if row["level"] in ("", UNKNOWN_LEVEL, "<No level>"):
        missing.append("tầng")
    if not row.get("gridLocation"):
        missing.append("trục")
    return missing


def confidence(group_level, row, missing):
    if any(m.startswith(("Family", "Type", "Element Id")) for m in missing) or "tầng" in missing:
        return "Thấp"
    rank = CONFIDENCE.index(group_level) if group_level in CONFIDENCE else 0
    if not row.get("pictured") or any(m.startswith("System") for m in missing):
        rank = min(rank, 1)
    return CONFIDENCE[rank]


def facts_sentence(row, grid):
    x, y, z = (row.get("centerMm") or [None, None, None])[:3]
    where = f"tại {row['level']}" + (f", trục {grid}" if grid else "") + (f", tọa độ ({x:.0f}; {y:.0f}; {z:.0f}) mm" if x is not None else "")
    depth = row["distanceMm"]
    form = f"xuyên/chồng lấn {abs(depth):g} mm" if depth < 0 else "chạm nhau (độ xuyên trong dung sai kiểm tra)"
    return f"{element_text(row['item1'])} va chạm với {element_text(row['item2'])} {where}, {form}."


def columns_for(row, judgement, rules, grid):
    """{classification, description, action, confidence} for one clash, or None when its group has no judgement yet."""
    if not judgement:
        return None
    classification = judgement.get("classification", VERIFY)
    action = judgement.get("action", "")
    group_confidence = judgement.get("confidence", "Trung bình")
    if classification == VALID and not rule_allows(rules.get(judgement.get("ruleId")), row):
        rule = rules.get(judgement.get("ruleId")) or {}
        classification, group_confidence = VERIFY, min(group_confidence, "Trung bình", key=CONFIDENCE.index)
        why = ("chưa được duyệt" if not rule_approved(rule) else "không khớp bộ môn hoặc vượt độ xuyên cho phép") if rule else "không tồn tại"
        action = (f"Chờ BIM xác nhận: quy tắc {judgement.get('ruleId')} ({rule.get('title', 'không rõ')}) {why}. "
                  f"Nếu được xác nhận hợp lệ: {action}")
    missing = missing_facts(row)
    sentences = [facts_sentence(row, grid), judgement.get("assessment", "").strip()]
    if missing:
        sentences.append("Dữ liệu chưa đủ: thiếu " + ", ".join(missing) + ".")
    return {
        "classification": classification if classification in CLASSES else VERIFY,
        "description": " ".join(s for s in sentences if s),
        "action": action,
        "confidence": confidence(group_confidence, row, missing),
    }


def load_judgements(snapshot_path):
    """The group judgements of <snapshot>-ai.json, keyed by group id ({} when none were written yet)."""
    path = Path(snapshot_path).with_name(Path(snapshot_path).stem + "-ai.json")
    if not path.is_file():
        return {}
    return json.loads(path.read_text(encoding="utf-8")).get("groups", {})
