import os
import re

src_dir = 'HPDcePro/src'

files_over_300 = []
commented_out_code = []
empty_catch_no_comment = []

comment_pattern = re.compile(r'^\s*//\s*(?:var|if|for|while|return|public|private|protected|class|void|int|double|string|bool)\b')
catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}')

for root, _, files in os.walk(src_dir):
    for f in files:
        if f.endswith('.cs'):
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()

            if len(lines) > 300:
                files_over_300.append((path, len(lines)))

            content = "".join(lines)
            if catch_pattern.search(content):
                empty_catch_no_comment.append(path)

            for idx, line in enumerate(lines):
                # Simple check for commented out statement lines, excluding XML doc or headers
                if comment_pattern.match(line) and not line.strip().startswith('///'):
                    # Check if it's really code
                    if ';' in line or '{' in line or '}' in line:
                        commented_out_code.append((path, idx + 1, line.strip()))

print(f"Files over 300 lines: {len(files_over_300)}")
for f, l in files_over_300:
    print(f"  {f}: {l} lines")

print(f"Empty catch blocks: {len(empty_catch_no_comment)}")
for f in empty_catch_no_comment:
    print(f"  {f}")

print(f"Commented out code lines: {len(commented_out_code)}")
for f, idx, line in commented_out_code[:10]:
    print(f"  {f}:{idx}: {line}")
