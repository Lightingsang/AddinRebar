import os
import xml.etree.ElementTree as ET

def test_file(path):
    print("=" * 60)
    print(f"Inspecting file: {path}")
    with open(path, "rb") as f:
        raw = f.read()

    text = raw.decode("latin1")
    split_lines = text.split("\n")
    print(f"Total split('\\n') lines: {len(split_lines)}")
    lines_crlf = text.split("\r\n")
    print(f"Total split('\\r\\n') lines: {len(lines_crlf)}")
    lines_splitlines = text.splitlines()
    print(f"Total splitlines(): {len(lines_splitlines)}")

    print(f"Line 1 (index 0): {repr(split_lines[0])}")
    print(f"Line 42 (index 41): {repr(split_lines[41])}")
    print(f"Line 151 (index 150): {repr(split_lines[150])}")
    print(f"Line 152 (index 151 - Param 1): {repr(split_lines[151])}")
    print(f"Line 237 (index 236 - Param 86): {repr(split_lines[236])}")
    print(f"Line 238 (index 237): {repr(split_lines[237])}")

    config_lines = split_lines[151:237]
    print(f"Count of config lines (152-237): {len(config_lines)}")
    print("First 3 config lines:")
    for idx, line in enumerate(config_lines[:3]):
        print(f"  Line {152+idx} (Param {idx+1}): {line.strip()}")
    print("Last 3 config lines:")
    for idx, line in enumerate(config_lines[-3:]):
        print(f"  Line {235+idx} (Param {84+idx}): {line.strip()}")

print("1. CHECKING C:\\HPKata\\Goi lenh.lsp")
test_file(r"C:\HPKata\Goi lenh.lsp")

print("\n2. CHECKING HPKata/Distribution/Assets/Goi lenh.lsp")
test_file(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Goi lenh.lsp")

print("\n3. CHECKING C:\\HPKata\\GoiLenh_HPKata.lsp")
test_file(r"C:\HPKata\GoiLenh_HPKata.lsp")

print("\n4. CHECKING HPKata/Distribution/Assets/GoiLenh_HPKata.lsp")
test_file(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\GoiLenh_HPKata.lsp")
