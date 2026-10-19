import os
import re

hpkata_root = os.path.abspath(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")

# List of Phase 3 files as documented in changes.md and git
phase3_relative_files = [
    # HPKata.Core - Columns
    r"HPKata.Core\Columns\Models\ColumnStorySegment.cs",
    r"HPKata.Core\Columns\Models\ColumnSectionModel.cs",
    r"HPKata.Core\Columns\Models\ColumnTieDistributionModel.cs",
    r"HPKata.Core\Columns\Models\ColumnBarRunModel.cs",
    r"HPKata.Core\Columns\Models\ColumnStarterDowelModel.cs",
    r"HPKata.Core\Columns\Models\ColumnRebarLayout.cs",
    r"HPKata.Core\Columns\Calculators\ColumnCrankBendCalculator.cs",
    r"HPKata.Core\Columns\Calculators\ColumnSpliceCalculator.cs",
    r"HPKata.Core\Columns\Calculators\ColumnConfinementCalculator.cs",
    r"HPKata.Core\Columns\Calculators\TcvnColumnRebarOptimizer.cs",
    r"HPKata.Core\Columns\Etabs\EtabsColumnDesign.cs",
    r"HPKata.Core\Columns\Etabs\EtabsColumnTableMapper.cs",
    r"HPKata.Core\Columns\Etabs\EtabsColumnImportService.cs",
    r"HPKata.Core\Columns\Persistence\ColumnScheduleAggregator.cs",
    r"HPKata.Core\Columns\Persistence\ColumnPlanScheduleWorkbookGenerator.cs",
    r"HPKata.Core\Columns\Persistence\ColumnXRecordSerializer.cs",
    
    # HPKata.Core - Walls
    r"HPKata.Core\Walls\Models\ShearWallModel.cs",
    r"HPKata.Core\Walls\Models\WallStorySegment.cs",
    r"HPKata.Core\Walls\Models\WallBoundaryElementModel.cs",
    r"HPKata.Core\Walls\Models\WallWebReinforcementModel.cs",
    r"HPKata.Core\Walls\Models\WallOpeningModel.cs",
    r"HPKata.Core\Walls\Calculators\WallBoundaryCalculator.cs",
    r"HPKata.Core\Walls\Calculators\WallWebMeshCalculator.cs",
    r"HPKata.Core\Walls\Calculators\WallOpeningTrimmerCalculator.cs",
    r"HPKata.Core\Walls\Etabs\EtabsPierDesign.cs",
    r"HPKata.Core\Walls\Etabs\EtabsWallTableMapper.cs",
    r"HPKata.Core\Walls\Etabs\EtabsWallImportService.cs",
    r"HPKata.Core\Walls\Persistence\WallScheduleAggregator.cs",
    r"HPKata.Core\Walls\Persistence\WallPlanScheduleWorkbookGenerator.cs",
    r"HPKata.Core\Walls\Persistence\WallXRecordSerializer.cs",

    # HPKata.UI
    r"HPKata.UI\ViewModels\ColumnStoryRowViewModel.cs",
    r"HPKata.UI\ViewModels\ColumnDetailingViewModel.cs",
    r"HPKata.UI\ViewModels\WallOpeningViewModel.cs",
    r"HPKata.UI\ViewModels\WallDetailingViewModel.cs",
    r"HPKata.UI\ViewModels\EtabsColumnWallImportViewModel.cs",
    r"HPKata.UI\Views\ColumnDetailingWindow.xaml.cs",
    r"HPKata.UI\Views\WallDetailingWindow.xaml.cs",

    # HPKata.AutoCad
    r"HPKata.AutoCad\Commands\ColumnCommands.cs",
    r"HPKata.AutoCad\Commands\WallCommands.cs",
    r"HPKata.AutoCad\Controllers\ColumnTakeoffRegistry.cs",
    r"HPKata.AutoCad\Controllers\WallTakeoffRegistry.cs",
    r"HPKata.AutoCad\Controllers\ColumnDrawController.cs",
    r"HPKata.AutoCad\Controllers\ColumnShopController.cs",
    r"HPKata.AutoCad\Controllers\ColumnPlacementController.cs",
    r"HPKata.AutoCad\Controllers\ColumnDimensionController.cs",
    r"HPKata.AutoCad\Controllers\ColumnTakeoffController.cs",
    r"HPKata.AutoCad\Controllers\ColumnEtabsController.cs",
    r"HPKata.AutoCad\Controllers\WallDrawController.cs",
    r"HPKata.AutoCad\Controllers\WallShopController.cs",
    r"HPKata.AutoCad\Controllers\WallCoreController.cs",
    r"HPKata.AutoCad\Controllers\WallEtabsController.cs",

    # HPKata.Revit
    r"HPKata.Revit\Commands\ColumnRebarCommands.cs",
    r"HPKata.Revit\Commands\WallRebarCommands.cs",
    r"HPKata.Revit\Commands\EtabsColumnWallImportCommand.cs",
    r"HPKata.Revit\Services\ColumnRebarGenerator.cs",
    r"HPKata.Revit\Services\WallRebarGenerator.cs",
    r"HPKata.Revit\Services\ColumnWallScheduleService.cs",

    # Tests
    r"HPKata.Core.Tests\Columns\ColumnModelTests.cs",
    r"HPKata.Core.Tests\Columns\ColumnCrankBendCalculatorTests.cs",
    r"HPKata.Core.Tests\Columns\ColumnSpliceCalculatorTests.cs",
    r"HPKata.Core.Tests\Columns\ColumnConfinementCalculatorTests.cs",
    r"HPKata.Core.Tests\Columns\TcvnColumnRebarOptimizerTests.cs",
    r"HPKata.Core.Tests\Columns\EtabsColumnImportTests.cs",
    r"HPKata.Core.Tests\Columns\ColumnWallPersistenceTests.cs",
    r"HPKata.Core.Tests\Walls\ShearWallModelTests.cs",
    r"HPKata.Core.Tests\Walls\WallBoundaryCalculatorTests.cs",
    r"HPKata.Core.Tests\Walls\WallWebMeshCalculatorTests.cs",
    r"HPKata.Core.Tests\Walls\WallOpeningTrimmerTests.cs",
    r"HPKata.Core.Tests\Walls\EtabsWallImportTests.cs",
    r"HPKata.UI.Tests\ViewModels\ColumnDetailingViewModelTests.cs",
    r"HPKata.UI.Tests\ViewModels\WallDetailingViewModelTests.cs",
    r"HPKata.AutoCad.Tests\Controllers\ColumnControllerTests.cs",
    r"HPKata.AutoCad.Tests\Controllers\WallControllerTests.cs",
    r"HPKata.Revit.Tests\Commands\ColumnWallGeometryTests.cs",
    r"HPKata.E2E.Tests\Tier4_Scenarios\Tier4_Scenario2_MultiStoryColumnTests.cs",
    r"HPKata.E2E.Tests\Tier4_Scenarios\Tier4_Scenario3_CoreWallTests.cs"
]

print(f"Total Phase 3 files to verify: {len(phase3_relative_files)}")

# Check file existence and Rule C2
c2_errors = []
missing_files = []

for rel in phase3_relative_files:
    full = os.path.join(hpkata_root, rel)
    if not os.path.exists(full):
        missing_files.append(rel)
        continue
    with open(full, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    if count > 300:
        c2_errors.append((rel, count))
    else:
        print(f"  [OK C2 {count:3d}L] {rel}")

if missing_files:
    print(f"\nMISSING FILES: {len(missing_files)}")
    for m in missing_files:
        print(f"  MISSING: {m}")

if c2_errors:
    print(f"\nC2 VIOLATIONS (>300 lines): {len(c2_errors)}")
    for rel, count in c2_errors:
        print(f"  {rel}: {count} lines")
else:
    print("\nALL Phase 3 files satisfy Rule C2 (<= 300 lines)!")

# Method length parser
# A simple C# method boundary detector using brace counting
def find_methods(lines):
    # Regex for method header (simplified heuristic)
    method_sig = re.compile(
        r"^\s*(?:public|private|protected|internal|static|virtual|override|async|sealed|\s)+"
        r"(?:[\w\<\>\[\]\,\?\s]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$"
    )
    methods = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # skip properties or class decls or control flow
        stripped = line.strip()
        if stripped.startswith("if ") or stripped.startswith("for ") or stripped.startswith("while ") or stripped.startswith("switch "):
            i += 1
            continue
        if stripped.startswith("class ") or stripped.startswith("interface ") or stripped.startswith("record ") or stripped.startswith("enum "):
            i += 1
            continue
        if stripped.startswith("get ") or stripped.startswith("set ") or stripped.startswith("init "):
            i += 1
            continue
            
        m = method_sig.match(line)
        if m:
            method_name = m.group(1)
            # ignore class constructor if matches class, but still count length
            start_line = i + 1
            # find opening brace
            brace_count = 0
            found_open = False
            j = i
            while j < len(lines):
                for ch in lines[j]:
                    if ch == '{':
                        brace_count += 1
                        found_open = True
                    elif ch == '}':
                        brace_count -= 1
                if found_open and brace_count == 0:
                    end_line = j + 1
                    length = end_line - start_line + 1
                    methods.append((method_name, start_line, end_line, length))
                    i = j
                    break
                # expression bodied method check
                if not found_open and ";" in lines[j] and "=>" in lines[j]:
                    end_line = j + 1
                    length = end_line - start_line + 1
                    methods.append((method_name, start_line, end_line, length))
                    i = j
                    break
                j += 1
        i += 1
    return methods

m3_violations = []
total_methods = 0

for rel in phase3_relative_files:
    full = os.path.join(hpkata_root, rel)
    if not os.path.exists(full):
        continue
    with open(full, "r", encoding="utf-8") as f:
        lines = f.readlines()
    methods = find_methods(lines)
    for name, start, end, length in methods:
        total_methods += 1
        if length > 50:
            m3_violations.append((rel, name, start, end, length))

print(f"\nTotal methods analyzed across Phase 3: {total_methods}")
if m3_violations:
    print(f"M3 VIOLATIONS (>50 lines): {len(m3_violations)}")
    for rel, name, start, end, length in m3_violations:
        print(f"  {rel} -> {name} (lines {start}-{end}): {length} lines")
else:
    print("ALL methods in Phase 3 satisfy Rule M3 (<= 50 physical lines)!")
