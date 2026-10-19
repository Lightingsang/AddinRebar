import json
import sys
from revit_audit_ipc import RevitReviewerIPC
sys.stdout.reconfigure(encoding="utf-8")

def audit_ledger_sync():
    print("=== AUDIT STEP 6: LEDGER RECONCILIATION ===")
    ledger_path = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\state\bim_coordination_ledger.json'
    with open(ledger_path, 'r', encoding='utf-8') as f:
        ledger = json.load(f)

    ledger_elements = ledger.get('elements', [])
    print(f"Total elements in ledger: {len(ledger_elements)}")
    
    # Collect IDs from ledger
    pile_ledger = [e for e in ledger_elements if e['category'] == 'OST_StructuralFoundation']
    footing_ledger = [e for e in ledger_elements if 'Floor' in e['category']]
    beam_ledger = [e for e in ledger_elements if e['category'] == 'OST_StructuralFraming']

    print(f"  Piles in ledger: {len(pile_ledger)}")
    print(f"  Footings in ledger: {len(footing_ledger)}")
    print(f"  Grade Beams in ledger: {len(beam_ledger)}")

    assert len(pile_ledger) == 196
    assert len(footing_ledger) == 72
    assert len(beam_ledger) == 192
    assert len(ledger_elements) == 460

    # Query Revit to check all 460 IDs
    with RevitReviewerIPC() as ipc:
        code = """
        var allIds = new HashSet<long>();
        foreach (var e in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFoundation).WhereElementIsNotElementType()) {
            allIds.Add(e.Id.Value);
        }
        foreach (var e in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType()) {
            allIds.Add(e.Id.Value);
        }
        return allIds.ToList();
        """
        res = ipc.execute(code, label="Fetch All Active Model IDs")
        active_ids = set(res['result']['value'])
        print(f"Total active foundation/framing IDs in Revit: {len(active_ids)}")

        missing_in_revit = []
        for e in ledger_elements:
            eid = e['elementId']
            if eid not in active_ids:
                missing_in_revit.append(eid)

        print(f"Ledger elements missing in Revit: {len(missing_in_revit)}")
        assert len(missing_in_revit) == 0, f"Missing IDs: {missing_in_revit}"

        extra_in_revit = [eid for eid in active_ids if eid not in {e['elementId'] for e in ledger_elements}]
        print(f"Extra active elements in Revit not in ledger: {len(extra_in_revit)}")
        assert len(extra_in_revit) == 0, f"Extra IDs: {extra_in_revit}"

        print("\n>>> LEDGER 100% PERFECT 1-TO-1 MATCH WITH REVIT ACTIVE MODEL (460 ELEMENTS)! <<<")

if __name__ == '__main__':
    audit_ledger_sync()
