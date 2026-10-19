import json, math, os, sys
from collections import defaultdict

def main():
    # Load 192 CAD spans
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
        spans = json.load(f)

    # Load 72 footings
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        footings_data = json.load(f)
    footings = footings_data.get('footings', footings_data)

    # Load 27 grids from Revit / DWG
    # Let's see: we know the X and Y grid coordinates
    # Let's inspect the exact grid coordinates
    print(f"Total spans: {len(spans)}")

    # Let's inspect each span orientation and length
    h_spans = [s for s in spans if abs(s['y1'] - s['y2']) < 1.0]
    v_spans = [s for s in spans if abs(s['x1'] - s['x2']) < 1.0]
    other_spans = [s for s in spans if abs(s['y1'] - s['y2']) >= 1.0 and abs(s['x1'] - s['x2']) >= 1.0]

    print(f"Horizontal (along X) spans: {len(h_spans)}")
    print(f"Vertical (along Y) spans: {len(v_spans)}")
    print(f"Slanted/other spans: {len(other_spans)}")

    # Check footing bounding boxes
    f_boxes = []
    for f in footings:
        xs = [v['x'] for v in f['vertices']]
        ys = [v['y'] for v in f['vertices']]
        f_boxes.append({
            'id': f['id'],
            'minX': min(xs), 'maxX': max(xs),
            'minY': min(ys), 'maxY': max(ys),
            'poly': [(v['x'], v['y']) for v in f['vertices']]
        })

    def pt_in_footing(x, y, margin=0.0):
        for fb in f_boxes:
            if fb['minX'] - margin <= x <= fb['maxX'] + margin and fb['minY'] - margin <= y <= fb['maxY'] + margin:
                return fb['id']
        return None

    # Check each endpoint
    status_p1 = []
    status_p2 = []
    for idx, s in enumerate(spans):
        f1 = pt_in_footing(s['x1'], s['y1'], margin=0.0)
        f1_25 = pt_in_footing(s['x1'], s['y1'], margin=25.0)
        f2 = pt_in_footing(s['x2'], s['y2'], margin=0.0)
        f2_25 = pt_in_footing(s['x2'], s['y2'], margin=25.0)
        status_p1.append((f1, f1_25))
        status_p2.append((f2, f2_25))

    p1_inside = sum(1 for f1, _ in status_p1 if f1 is not None)
    p1_near = sum(1 for _, f1_25 in status_p1 if f1_25 is not None)
    p2_inside = sum(1 for f2, _ in status_p2 if f2 is not None)
    p2_near = sum(1 for _, f2_25 in status_p2 if f2_25 is not None)

    print(f"P1 inside footing: {p1_inside}/192 (within 25mm: {p1_near}/192)")
    print(f"P2 inside footing: {p2_inside}/192 (within 25mm: {p2_near}/192)")

if __name__ == '__main__':
    main()
