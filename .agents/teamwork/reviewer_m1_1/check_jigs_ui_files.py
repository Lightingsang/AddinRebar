import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Jigs
jig_files = [
    'src/HPDcePro.AutoCad/Jigs/VeMBKCJig.cs',
    'src/HPDcePro.AutoCad/Jigs/JigSlabRebar.cs',
    'src/HPDcePro.AutoCad/Jigs/TagLeaderJig.cs',
    'src/HPDcePro.AutoCad/Jigs/TagChiaThepJig.cs',
    'src/HPDcePro.AutoCad/Jigs/JigRaiThepChamTron.cs',
    'src/HPDcePro.AutoCad/Jigs/JigVeDaiVong.cs',
    'src/HPDcePro.AutoCad/Jigs/JigVeDaiC.cs',
    'src/HPDcePro.AutoCad/Jigs/VePolylineBatKyJig.cs',
    'src/HPDcePro.AutoCad/Jigs/JigVeThepVachCt.cs',
    'src/HPDcePro.AutoCad/Jigs/JigVeVachHam.cs',
    'src/HPDcePro.AutoCad/Jigs/DimNhanhMbkcJig.cs'
]

print("=== Checking Subsystem 7 (11 CAD Jigs) Files ===")
base_dir = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro'
for jf in jig_files:
    full = os.path.join(base_dir, jf)
    exists = os.path.exists(full)
    line_count = 0
    if exists:
        with open(full, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            line_count = len(lines)
    print(f"{jf} -> Exists: {exists}, Lines: {line_count}")

# UI Forms
ui_files = [
    'src/HPDcePro.UI/Views/DamCongWindow.xaml',
    'src/HPDcePro.UI/Views/MongCocWindow.xaml',
    'src/HPDcePro.UI/Views/MongCocLopThepThemWindow.xaml',
    'src/HPDcePro.UI/Views/MongCocChonThepTuDongWindow.xaml',
    'src/HPDcePro.UI/Views/ThangMoiWindow.xaml',
    'src/HPDcePro.UI/Views/CocBtWindow.xaml',
    'src/HPDcePro.UI/Views/MasterRebarScheduleDashboardWindow.xaml',
    'src/HPDcePro.UI/Views/AutoStrExecutionWindow.xaml',
    'src/HPDcePro.UI/Views/CuttingOptimizationAdvancedWindow.xaml',
    'src/HPDcePro.AutoCad/Palettes/DcePaletteControl.xaml',
    'src/HPDcePro.AutoCad/Palettes/WallThicknessPaletteControl.xaml',
    'src/HPDcePro.AutoCad/Ribbon/DceRibbonBuilder.cs'
]

print("\n=== Checking Subsystem 8 (12 UI Forms & Palettes) Files ===")
for uf in ui_files:
    full = os.path.join(base_dir, uf)
    exists = os.path.exists(full)
    line_count = 0
    if exists:
        with open(full, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            line_count = len(lines)
    print(f"{uf} -> Exists: {exists}, Lines: {line_count}")
