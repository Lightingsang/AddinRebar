import json

with open(r"scratch\all_members_m4.json", "r", encoding="utf-8") as f:
    members = json.load(f)

print(f"Total members analyzed by Roslyn: {len(members)}")
over_50 = [m for m in members if m["Lines"] > 50]
print(f"Members over 50 lines: {len(over_50)}")

sorted_members = sorted(members, key=lambda x: x["Lines"], reverse=True)
print("\nTop 10 Longest Members:")
print(f"{'Lines':<6} | {'Start-End':<12} | {'Kind':<14} | {'Member':<40} | {'File'}")
print("-" * 120)
for m in sorted_members[:10]:
    span_str = f"L{m['Start']}-L{m['End']}"
    print(f"{m['Lines']:<6} | {span_str:<12} | {m['Kind']:<14} | {m['Member']:<40} | {m['File']}")
