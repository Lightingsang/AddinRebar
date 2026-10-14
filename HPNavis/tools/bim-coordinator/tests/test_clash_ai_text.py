"""Tests of clash-ai-text.py: the per-clash AI columns composed from a group judgement and the clash's own facts."""

import importlib.util
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("clash_ai_text", HERE.parent / "clash-ai-text.py")
ai = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(ai)


def item(category, family, type_, element_id, system="", source="THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001_user.rvt"):
    return {"name": family, "category": category, "elementId": element_id, "sourceFile": source, "level": "TẦNG 2",
            "family": family, "type": type_, "size": "", "system": system, "mark": ""}


def row(distance=-120.0, pictured=True, level="TẦNG 2", grid="Y.1a(120)-15.1a(-35)", item1=None, item2=None):
    return {"test": "HP|LOD350-400|P3|ARC-STR|A8 Architectural Walls-S5 Structural Framing", "ruleId": "HP_A8_S5", "priority": 3,
            "pair": "ARC-STR", "guid": "g1", "name": "Clash1", "status": "New", "distanceMm": distance, "centerMm": [1000, 2000, 3000],
            "level": level, "gridLocation": grid, "pictured": pictured,
            "item1": item1 or item("Walls", "Basic Wall", "A_W_BR-XMCL_S200", "12480081"),
            "item2": item2 or item("Structural Framing", "1_STR_Framing_ConcreteRectangular", "B_200x500", "9816462",
                                   source="THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_user.rvt")}


JUDGEMENT = {"classification": "Cần xử lý", "confidence": "Cao", "assessment": "Đỉnh tường dựng tới cao độ tầng nên ăn vào dầm.",
             "action": "ARC hạ đỉnh tường tới đáy dầm (Top Constraint)."}
RULE = {"HV-01": {"id": "HV-01", "title": "Tường xây × dầm", "sides": [[["ARC", "Walls"], ["STR", "Structural Framing"]]],
                  "maxPenetrationMm": 600, "approved": True, "approvedBy": "BIM manager"}}


class ClashAiTextTests(unittest.TestCase):
    def test_the_description_quotes_both_elements_location_and_penetration_then_the_group_judgement(self):
        columns = ai.columns_for(row(), JUDGEMENT, {}, "Y.1a(120)-15.1a(-35)")
        text = columns["description"]
        for fact in ("Walls Basic Wall – A_W_BR-XMCL_S200", "Id 12480081", "LH_HB_HC-AA", "Structural Framing", "B_200x500",
                     "tại TẦNG 2", "trục Y.1a(120)-15.1a(-35)", "(1000; 2000; 3000) mm", "xuyên/chồng lấn 120 mm", JUDGEMENT["assessment"]):
            self.assertIn(fact, text)
        self.assertEqual(("Cần xử lý", "Cao"), (columns["classification"], columns["confidence"]))
        self.assertEqual(JUDGEMENT["action"], columns["action"])

    def test_valid_by_design_needs_an_approved_rule_that_the_clash_meets(self):
        valid = dict(JUDGEMENT, classification=ai.VALID, ruleId="HV-01")
        self.assertEqual(ai.VALID, ai.columns_for(row(-120), valid, RULE, "g")["classification"])
        too_deep = ai.columns_for(row(-700), valid, RULE, "g")
        self.assertEqual(ai.VERIFY, too_deep["classification"])
        self.assertIn("vượt độ xuyên cho phép", too_deep["action"])
        unapproved = {"HV-01": dict(RULE["HV-01"], approved=False)}
        self.assertEqual(ai.VERIFY, ai.columns_for(row(-120), valid, unapproved, "g")["classification"])
        for loose in ({"approved": "false"}, {"approvedBy": ""}, {"maxPenetrationMm": None}):
            self.assertEqual(ai.VERIFY, ai.columns_for(row(-120), valid, {"HV-01": dict(RULE["HV-01"], **loose)}, "g")["classification"], loose)
        pending = ai.columns_for(row(-120), valid, unapproved, "g")
        self.assertTrue(pending["action"].startswith("Chờ BIM xác nhận: quy tắc HV-01 (Tường xây × dầm) chưa được duyệt."))
        self.assertEqual("Trung bình", pending["confidence"])
        self.assertEqual(ai.VERIFY, ai.columns_for(row(-120), dict(valid, ruleId="HV-99"), RULE, "g")["classification"])

    def test_missing_facts_are_named_and_lower_the_confidence_never_guessed(self):
        bare = item("Pipes", "Pipe Types", "", "5527899")
        columns = ai.columns_for(row(item1=bare, level="<No level>", grid=None), JUDGEMENT, {}, "")
        self.assertIn("Dữ liệu chưa đủ: thiếu Type cấu kiện 1, System cấu kiện 1, tầng, trục.", columns["description"])
        self.assertEqual("Thấp", columns["confidence"])

    def test_cable_trays_carry_no_system_so_none_is_reported_missing(self):
        tray = item("Cable Trays", "Cable Tray with Fittings", "MÁNG CÁP", "4737574")
        self.assertNotIn("System", ai.columns_for(row(item1=tray), JUDGEMENT, {}, "g")["description"])

    def test_a_clash_without_its_own_picture_is_at_most_medium(self):
        self.assertEqual("Trung bình", ai.columns_for(row(pictured=False), JUDGEMENT, {}, "g")["confidence"])
        self.assertEqual("Trung bình", ai.columns_for(row(), dict(JUDGEMENT, confidence="Trung bình"), {}, "g")["confidence"])

    def test_a_rule_binds_the_discipline_of_each_side_so_approving_masonry_never_approves_concrete(self):
        rules = {"HV-03": {"id": "HV-03", "title": "t", "sides": [[["ARC", "Walls"], ["STR", "Floors"]]], "maxPenetrationMm": 300,
                           "approved": True, "approvedBy": "BIM"}}
        valid = dict(JUDGEMENT, classification=ai.VALID, ruleId="HV-03")
        wall, slab = item("Walls", "Basic Wall", "A_W", "1"), item("Floors", "Floor", "S_D100", "2")
        masonry = dict(row(-100, item1=wall, item2=slab), pair="ARC-STR")
        concrete = dict(row(-100, item1=wall, item2=slab), pair="STR-STR")
        reversed_sides = dict(row(-100, item1=slab, item2=wall), pair="STR-ARC")
        self.assertEqual(ai.VALID, ai.columns_for(masonry, valid, rules, "g")["classification"])
        self.assertEqual(ai.VALID, ai.columns_for(reversed_sides, valid, rules, "g")["classification"])
        self.assertEqual(ai.VERIFY, ai.columns_for(concrete, valid, rules, "g")["classification"])

    def test_the_knowledge_base_judges_a_group_by_its_kind_and_band(self):
        knowledge = {"ARC-STR|Walls|Floors": {"classification": ai.VALID, "ruleId": "HV-03", "confidence": "Cao", "assessment": "a", "action": "b",
                                              "bands": {">200": {"classification": ai.VERIFY, "assessment": "deep"}}}}
        group = {"pair": "ARC-STR", "category1": "Walls", "category2": "Floors", "band": "≤200"}
        self.assertEqual({"classification": ai.VALID, "ruleId": "HV-03", "confidence": "Trung bình", "assessment": "a", "action": "b",
                          "source": "knowledge:ARC-STR|Walls|Floors"},
                         ai.judgement_from_knowledge(group, knowledge))
        deep = ai.judgement_from_knowledge(dict(group, band=">200"), knowledge)
        self.assertEqual((ai.VERIFY, "deep", "b"), (deep["classification"], deep["assessment"], deep["action"]))
        self.assertNotIn("ruleId", deep)
        self.assertIsNone(ai.judgement_from_knowledge(dict(group, pair="MEP-STR"), knowledge))

    def test_every_kind_in_the_shipped_knowledge_base_is_well_formed(self):
        import json
        data = json.loads(ai.KNOWLEDGE_FILE.read_text(encoding="utf-8"))["judgements"]
        rules = ai.load_rules()
        for key, value in data.items():
            self.assertEqual(3, len(key.split("|")), key)
            for judgement in [value] + list(value.get("bands", {}).values()):
                self.assertIn(judgement.get("classification", value["classification"]), ai.CLASSES, key)
            self.assertIn(value["confidence"], ai.CONFIDENCE, key)
            self.assertTrue(value["assessment"] and value["action"], key)
            if value["classification"] == ai.VALID:
                rule = rules[value["ruleId"]]
                pair, category1, category2 = key.split("|")
                left, right = pair.split("-")
                self.assertIn(sorted([[left, category1], [right, category2]]), [sorted(s) for s in rule["sides"]], key)

    def test_a_group_without_judgement_yields_no_columns(self):
        self.assertIsNone(ai.columns_for(row(), None, {}, "g"))

    def test_alike_clashes_share_a_group_and_the_distance_band_splits_them(self):
        self.assertEqual(ai.group_id(row(-120)), ai.group_id(row(-150)))
        self.assertNotEqual(ai.group_id(row(-120)), ai.group_id(row(-30)))
        self.assertEqual("≤10", ai.band(-10))
        self.assertEqual(">200", ai.band(-200.5))


if __name__ == "__main__":
    unittest.main()
