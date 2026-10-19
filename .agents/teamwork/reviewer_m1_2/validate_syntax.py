import os
import zipfile
import xml.etree.ElementTree as ET

def validate_xml(cui_path):
    print(f"\n--- Validating XML: {cui_path} ---")
    try:
        tree = ET.parse(cui_path)
        root = tree.getroot()
        print(f"PASS: XML parsed successfully! Root tag: {root.tag}, Total elements: {len(list(root.iter()))}")
        
        # Check icon references or path references inside menukata.cui
        kata_pro_refs = []
        for elem in root.iter():
            if elem.text and "kata_pro" in elem.text.lower():
                kata_pro_refs.append((elem.tag, elem.text))
            for k, v in elem.attrib.items():
                if "kata_pro" in v.lower():
                    kata_pro_refs.append((f"{elem.tag}@{k}", v))
        if kata_pro_refs:
            print(f"FAIL: Found legacy kata_pro references in {cui_path}: {kata_pro_refs}")
            return False
        else:
            print(f"PASS: Zero kata_pro references in {cui_path}")
            return True
    except Exception as e:
        print(f"FAIL: XML parse error in {cui_path}: {e}")
        return False

def validate_cuix(cuix_path):
    print(f"\n--- Validating CUIX ZIP: {cuix_path} ---")
    try:
        with zipfile.ZipFile(cuix_path, 'r') as z:
            namelist = z.namelist()
            print(f"PASS: CUIX is valid ZIP with {len(namelist)} entries. Sample entries: {namelist[:5]}")
            for name in namelist:
                if name.endswith('.cui') or name.endswith('.xml'):
                    data = z.read(name)
                    ET.fromstring(data)
            print("PASS: All embedded XML/CUI files inside CUIX are well-formed XML.")
            return True
    except Exception as e:
        print(f"FAIL: CUIX error: {e}")
        return False

def validate_lisp_syntax(lisp_path):
    print(f"\n--- Validating AutoLISP Syntax: {lisp_path} ---")
    with open(lisp_path, "r", encoding="latin1") as f:
        content = f.read()

    paren_depth = 0
    in_string = False
    in_block_comment = False
    escape = False
    line_num = 1
    col_num = 0
    errors = []
    
    i = 0
    n = len(content)
    while i < n:
        c = content[i]
        col_num += 1
        
        if c == '\n':
            line_num += 1
            col_num = 0
            if in_string:
                errors.append(f"Unclosed string literal across newline at line {line_num-1}")
            i += 1
            continue

        if in_block_comment:
            if c == '|' and i + 1 < n and content[i + 1] == ';':
                in_block_comment = False
                i += 2
                col_num += 1
                continue
            i += 1
            continue

        if in_string:
            if escape:
                escape = False
            elif c == '\\':
                escape = True
            elif c == '"':
                in_string = False
            i += 1
            continue

        # Outside string & block comment
        if c == ';' and i + 1 < n and content[i + 1] == '|':
            in_block_comment = True
            i += 2
            col_num += 1
            continue

        if c == ';':
            # Single-line comment: skip to end of line
            while i < n and content[i] != '\n':
                i += 1
            continue

        if c == '"':
            in_string = True
            i += 1
            continue

        if c == '(':
            paren_depth += 1
        elif c == ')':
            paren_depth -= 1
            if paren_depth < 0:
                errors.append(f"Unexpected closing parenthesis at line {line_num}, col {col_num}")
                paren_depth = 0

        i += 1

    if paren_depth > 0:
        errors.append(f"Unmatched opening parenthesis: {paren_depth} unclosed parenthesis remaining at EOF")
    if in_string:
        errors.append("Unclosed string at EOF")
    if in_block_comment:
        errors.append("Unclosed block comment at EOF")

    if errors:
        print(f"FAIL: AutoLISP syntax errors in {lisp_path}:")
        for err in errors:
            print(f"  - {err}")
        return False
    else:
        print(f"PASS: AutoLISP syntax is clean (balanced parentheses, valid strings/comments) in {lisp_path}")
        return True

# Run all checks
ok = True
for base in [r"C:\HPKata", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets"]:
    ok &= validate_xml(os.path.join(base, "menukata.cui"))
    ok &= validate_cuix(os.path.join(base, "menukata.cuix"))
    ok &= validate_lisp_syntax(os.path.join(base, "Goi lenh.lsp"))
    ok &= validate_lisp_syntax(os.path.join(base, "GoiLenh_HPKata.lsp"))

print(f"\nOVERALL SYNTAX VALIDATION RESULT: {'ALL PASS' if ok else 'FAILURES DETECTED'}")
