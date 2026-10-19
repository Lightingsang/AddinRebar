import os
import re

m3_files = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerDataTreeHelper.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\CanvasEditorTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\PlayerDataTreeTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\ImageExporterTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasReader.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\ImageExporter.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\Tools\BatchEditGrasshopperCanvasTool.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Harness\GrasshopperE2EHarness.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers\Tier2_EditAndExportBoundariesTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers\Tier2_RegistryAndSeedBoundariesTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests\Tiers\Tier5_SeedParameterFuzzingTests.cs",
]

seeds_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary"
for root, dirs, files in os.walk(seeds_dir):
    for f in files:
        if f.endswith(".cs") or f.endswith(".json"):
            m3_files.append(os.path.join(root, f))

print(f"Total M3 specific files: {len(m3_files)}")

# Check file lines
for path in m3_files:
    if not os.path.exists(path):
        print(f"Missing file: {path}")
        continue
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        count = len(lines)
        if count > 300:
            print(f"[C2 VIOLATION] {os.path.basename(path)}: {count} lines (> 300)")

