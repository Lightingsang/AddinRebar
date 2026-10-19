with open('Form_To_Hop_Thep.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find line of 'to_hop_thep'
start_line = -1
for i, line in enumerate(lines):
    if 'to_hop_thep(' in line:
        start_line = i
        break

print(f"to_hop_thep found at line {start_line+1}")
if start_line != -1:
    # Print 200 lines around it
    for j in range(max(0, start_line - 10), min(len(lines), start_line + 300)):
        print(f"{j+1:4d}: {lines[j]}", end='')
