import json

with open("scratch/cad_beam_texts.json", encoding="utf-8") as f:
    data = json.load(f)

print(f"Total entities: {len(data)}")

# Let's find specific beam mark texts
dka_items = [d for d in data if "DK.a" in d["text"]]
dkb_items = [d for d in data if "DK.b" in d["text"]]
dim_notes = [d for d in data if any(k in d["text"] for k in ["100x300", "200x300", "300x300", "TIET DIEN", "TIE\u00c1T"])]

print(f"DK.a count: {len(dka_items)}")
for d in dka_items:
    print(f"  {d['text']} @ ({d['x']}, {d['y']}) layer={d['layer']}")

print(f"\nDK.b count: {len(dkb_items)}")
for d in dkb_items:
    print(f"  {d['text']} @ ({d['x']}, {d['y']}) layer={d['layer']}")

print(f"\nDimension notes count: {len(dim_notes)}")
for d in dim_notes:
    print(f"  {d['text']} @ ({d['x']}, {d['y']}) layer={d['layer']}")
