import sys
sys.stdout.reconfigure(encoding='utf-8')

# 1. Check RebarCommands.cs
with open('HPDcePro/src/HPDcePro.AutoCad/Commands/RebarCommands.cs', encoding='utf-8') as f:
    rc = f.read()

assert '[CommandMethod("ChiaKhoangCachDaiDam")]' in rc, "Missing ChiaKhoangCachDaiDam"
assert '[CommandMethod("ChiaKhoangCach")]' not in rc, "Duplicate ChiaKhoangCach still present in RebarCommands.cs"
print("PASS 1: RebarCommands.cs command rename verified.")

# 2. Check summary table in TRUTH_MATRIX.md
with open('docs/rea-dce-pro/TRUTH_MATRIX.md', encoding='utf-8') as f:
    tm = f.read()

assert '| Subsystem 1 (AutoCAD Commands) | AutoCAD Commands | 311 | 0 | 64 | 247 | M3 |' in tm, "Subsystem 1 count mismatch"
assert '| **Grand Total (100% Surface)** | **All 9 Subsystems** | **602** | **130** | **110** | **362** | **M1–M5** |' in tm, "Grand Total count mismatch"
print("PASS 2: Summary statistics table verified.")

# 3. Check row 67
assert '| `ChiaKhoangCach` | AutoCAD Command (sàn) | TypeDef: `ClassLibrary1.Commands` | **Babel Protected CIL stub**. Prompts: `Lời nhắc chọn đo trong mã lệnh (theo thứ tự): nhập số đoạn/BTBV + chọn phương án → «Chọn đ...` Currently mocked: Hardcoded spacing division without Editor.GetSelection. | `src/HPDcePro.AutoCad/Commands/SlabCommands.cs:135` | ĐANG LÀ MOCK |' in tm, "Row 67 mismatch"
print("PASS 3: Row 67 ChiaKhoangCach points to SlabCommands.cs:135.")

# 4. Check 8 mock commands
mocks = [
    ('GetForm', 'src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs:23'),
    ('TMCOC', 'src/HPDcePro.AutoCad/Commands/FootingCommands.cs:115'),
    ('TKTSSoLuongCauKien', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:24'),
    ('TKTSDuongKinhThep', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:41'),
    ('TKT_DCE', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:58'),
    ('STK_DCE', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:74'),
    ('TongHopThongKe_DCE', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:81'),
    ('QuetThongKe_DCE', 'src/HPDcePro.AutoCad/Commands/RebarCommands.cs:115'),
]

for cmd, target in mocks:
    found = False
    for line in tm.split('\n'):
        if f'| `{cmd}` |' in line:
            assert 'ĐANG LÀ MOCK' in line, f'{cmd} not ĐANG LÀ MOCK: {line}'
            assert target in line, f'{cmd} target not {target}: {line}'
            found = True
            break
    assert found, f'{cmd} not found in TRUTH_MATRIX.md'

print("PASS 4: All 8 mock commands verified.")

# 5. Check footnotes
assert 'Danh mục file mục tiêu `src/HPDcePro.AutoCad/Jigs/*.cs` đại diện cho kiến trúc clean-room đích' in tm, "Missing Subsystem 7 footnote"
assert 'Danh mục file mục tiêu `src/HPDcePro.UI/Views/*.xaml`' in tm, "Missing Subsystem 8 footnote"
print("PASS 5: Subsystems 7 & 8 target architecture footnotes verified.")

print("\nALL 5 ACCEPTANCE CRITERIA VERIFIED SUCCESSFULLY!")
