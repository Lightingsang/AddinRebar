import json

with open("scratch/cad_beam_texts.json", encoding="utf-8") as f:
    data = json.load(f)

beam_marks = {}
for d in data:
    txt = d["text"].strip()
    if txt.startswith("DK") or "DK" in txt:
        # ignore long general notes
        if len(txt) <= 20:
            beam_marks[txt] = beam_marks.get(txt, 0) + 1

with open("scratch/cad_beam_marks_summary.json", "w", encoding="utf-8") as f:
    json.dump(beam_marks, f, indent=2, ensure_ascii=False)

for k, v in sorted(beam_marks.items()):
    print(f"{k}: {v}")
