import os

test_files = [
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fakes\GrasshopperTestDoubles.cs',
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\FindingN7DuplicateNicknamesTests.cs',
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\FindingN8DiagnosticsPreservationTests.cs',
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\FindingN9ContainerSchemaValidationTests.cs',
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\FindingN6PlayerDataTreeHelperTests.cs',
    r'HPGrasshopper\HPGrasshopper.McpBridge.Tests\Coordinator\FindingN10ContextStaleAndIsolationTests.cs'
]

print("=== CHECK M2 TEST FILES LINE COUNTS ===")
for tf in test_files:
    if os.path.exists(tf):
        with open(tf, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            print(f"{tf}: {len(lines)} lines")
    else:
        print(f"NOT FOUND: {tf}")
