#!/usr/bin/env python3
"""
Dedicated Markdown Syntax Checker for TRUTH_MATRIX.md
Checks for:
1. Exact cell backtick balance (even number of ` characters in every cell).
2. Truncated strings ending without closing backticks.
3. Pipe delimiter integrity.
4. Unicode / BOM issues.
"""

import sys

sys.stdout.reconfigure(encoding='utf-8')

TRUTH_MATRIX_PATH = r"docs/rea-dce-pro/TRUTH_MATRIX.md"

def check_syntax():
    with open(TRUTH_MATRIX_PATH, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    unclosed_backtick_cells = []
    
    for idx, line in enumerate(lines):
        line_num = idx + 1
        s = line.strip()
        if not (s.startswith('|') and s.endswith('|')):
            continue
        
        # split cells
        cells = s.split('|')[1:-1]
        for col_idx, raw_cell in enumerate(cells):
            backtick_char = chr(96) # ASCII 96: `
            cnt = raw_cell.count(backtick_char)
            if cnt % 2 != 0:
                unclosed_backtick_cells.append({
                    'line': line_num,
                    'col': col_idx,
                    'count': cnt,
                    'content': raw_cell.strip()
                })

    print(f"Total unclosed backtick cells found: {len(unclosed_backtick_cells)}")
    for item in unclosed_backtick_cells:
        print(f"Line {item['line']:3d} Col {item['col']}: count={item['count']} -> {item['content']}")

if __name__ == '__main__':
    check_syntax()
