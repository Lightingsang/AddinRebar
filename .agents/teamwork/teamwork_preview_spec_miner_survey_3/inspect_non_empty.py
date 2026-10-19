with open('Form_To_Hop_Thep.cs', 'r', encoding='utf-8') as f:
    text = f.read()

import re

# Find methods that have non-empty bodies
# Split by [MethodImpl
chunks = text.split('[MethodImpl')
print(f"Total chunks: {len(chunks)}")

non_empty = []
for c in chunks:
    lines = c.strip().split('\n')
    # look for signature
    sig = ""
    body = []
    in_body = False
    for line in lines:
        if ('void ' in line or 'int ' in line or 'string ' in line or 'double ' in line or 'List<' in line or 'bool ' in line) and '(' in line and not in_body:
            sig = line.strip()
        if '{' in line:
            in_body = True
        elif in_body:
            if '}' in line and len(line.strip()) == 1:
                break
            stripped = line.strip()
            if stripped and stripped != '{' and stripped != '}':
                body.append(stripped)
    if len(body) > 1:
        non_empty.append((sig, body))

print(f"Non-empty methods: {len(non_empty)}")
for sig, body in non_empty[:20]:
    print("METHOD:", sig)
    for b in body[:10]:
        print("   ", b)
    print()
