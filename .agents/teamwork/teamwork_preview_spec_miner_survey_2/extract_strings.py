import re

def extract_strings(dll_path, min_len=3):
    with open(dll_path, "rb") as f:
        data = f.read()
    
    # ASCII strings
    ascii_re = re.compile(b"[\x20-\x7e]{" + str(min_len).encode() + b",}")
    # UTF-16LE strings
    utf16_re = re.compile(b"(?:[\x20-\x7e]\x00){" + str(min_len).encode() + b",}")
    
    strings = set()
    for m in ascii_re.finditer(data):
        try:
            strings.add(m.group().decode("latin1"))
        except:
            pass
            
    for m in utf16_re.finditer(data):
        try:
            strings.add(m.group().decode("utf-16le"))
        except:
            pass
            
    return sorted(strings)

for dll in ["C:\\kata_pro\\Kata_Class_Lib.dll", "C:\\kata_pro\\Kata_pro64_Cad2025.dll", "C:\\kata_pro\\Kata_Class_Lib_Revit.dll"]:
    strs = extract_strings(dll)
    print(f"=== {dll}: {len(strs)} unique strings ===")
    
    # Check RegApp / XData / Shapes / Keywords
    kata_strs = [s for s in strs if any(k in s.lower() for k in ["kata", "regapp", "xdata", "xrecord", "dang", "thep", "shape", "coupler", "hook", "lap", "splice"])]
    print(f"  Matched keywords: {len(kata_strs)}")
    
    out_file = f".agents/teamwork/teamwork_preview_spec_miner_survey_2/{dll.split('\\')[-1]}_strings.txt"
    with open(out_file, "w", encoding="utf-8") as f:
        for s in kata_strs:
            f.write(s + "\n")
    print(f"  Wrote {out_file}")
