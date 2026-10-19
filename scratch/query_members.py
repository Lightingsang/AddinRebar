import json

with open(r'scratch\all_members_m4.json', 'r', encoding='utf-8') as f:
    members = json.load(f)

print(f"Total members measured across all 24 files: {len(members)}")
print("\n--- MEMBERS > 50 LINES (VIOLATIONS) ---")
over_50 = [m for m in members if m['Lines'] > 50]
for m in over_50:
    print(f"VIOLATION: {m['File']} | {m['Member']} ({m['Kind']}) | {m['Lines']}L (L{m['Start']}-L{m['End']})")

print("\n--- MEMBERS BETWEEN 30 AND 50 LINES (NEAR-LIMIT WATCHLIST) ---")
between_30_50 = sorted([m for m in members if 30 <= m['Lines'] <= 50], key=lambda x: x['Lines'], reverse=True)
for m in between_30_50:
    print(f"WATCH: {m['File']} | {m['Member']} ({m['Kind']}) | {m['Lines']}L (L{m['Start']}-L{m['End']})")
