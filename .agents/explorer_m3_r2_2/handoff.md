# Handoff Report — Milestone M3 Remediation Round 2: Defect 2 (Tool Registry & `examples.json` Schema Non-Compliance)

**Agent**: explorer_m3_r2_2 (Tool Registry & Schema Compliance Specialist)  
**Recipient**: orchestrator_7 (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Scope**: Defect 2 Investigation & Resolution Blueprint across all 12 embedded seeds in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**/examples.json`  
**Timestamp**: 2026-09-21T15:00:00Z  
**Type**: Hard Handoff (Investigation Complete)  

---

## 1. Observation

### 1.1 Direct Test Failure Observation
Executing the specific xUnit test for examples schema compliance via Microsoft.Testing.Platform:
```powershell
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-method "*Seed_ExamplesJson*"
```
Output:
```
failed HPRobot.McpBridge.Tests.SeedLibraryChallengerTests.Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples(category: "Geometry", name: "get_coordinate_systems_and_grids") (0ms)
  Seed 'Geometry/get_coordinate_systems_and_grids' has 1 example(s). Expected at least 2 distinct examples.
failed HPRobot.McpBridge.Tests.SeedLibraryChallengerTests.Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples(category: "Geometry", name: "get_structural_objects") (0ms)
  Seed 'Geometry/get_structural_objects' has 1 example(s). Expected at least 2 distinct examples.
...
Test run summary: Failed! - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
  total: 12
  failed: 12
  succeeded: 0
  skipped: 0
  duration: 344ms
```
All 12 seeds fail with identical failure modes.

### 1.2 File Inspection Observations
Direct inspection of all 12 `examples.json` files located under `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/`:
- `Model/get_model_info/examples.json`:
  ```json
  [
    {
      "title": "Get basic model info",
      "input": { "includeCases": true }
    }
  ]
  ```
- `Geometry/assign_node_support/examples.json`:
  ```json
  [
    {
      "title": "Pin nodes 1 and 2",
      "input": {
        "nodeNumbers": "1 2",
        "supportType": "Pinned"
      }
    }
  ]
  ```
- `Geometry/draw_bar_by_coords/examples.json`:
  ```json
  [
    {
      "title": "Draw column from (0,0,0) to (0,0,3.5)",
      "input": {
        "startX": 0.0,
        "startY": 0.0,
        "startZ": 0.0,
        "endX": 0.0,
        "endY": 0.0,
        "endZ": 3.5,
        "sectionName": "HEA 200"
      }
    }
  ]
  ```
Direct observations:
1. Every file contains an array of length 1 (`count == 1`).
2. Every file keys the parameters under `"input"`.

### 1.3 Code Inspection Observations
1. **`ToolRecord.cs` (lines 96–105)**:
   ```csharp
   public sealed class ToolExample
   {
       public string Title { get; set; } = string.Empty;
       public JsonElement Args { get; set; } = JsonSerializer.Deserialize<JsonElement>("{}");
       public JsonElement? Expected { get; set; }
       public long? VerifiedRunId { get; set; }
   }
   ```
   *Finding*: The DTO property name is `Args`. System.Text.Json deserialization does not map the JSON key `"input"` to `Args`. Therefore, `example.Args` silently defaults to `{}`.
2. **`SeedLibraryChallengerTests.cs` (lines 105–144)**:
   - Line 106: `Assert.True(root.GetArrayLength() >= 2, $"Seed '{category}/{name}' has {root.GetArrayLength()} example(s). Expected at least 2 distinct examples.");`
   - Line 123: `Assert.False(ex.TryGetProperty("input", out _), $"Seed '{category}/{name}' example[{i}] uses non-standard key 'input' instead of 'args'.");`
   - Line 126: `Assert.True(ex.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object, ...);`
   - Lines 134–136: Iterates over all `required` properties from `tool.json` `inputSchema` and asserts that each is present in `argKeys`.
   - Lines 141–143: Iterates over `argKeys` and asserts that each property was declared in `tool.json` `inputSchema.properties`.

---

## 2. Logic Chain

1. **Premise 1 (Test Contract)**: `SeedLibraryChallengerTests.Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` mandates:
   - Array length $\ge 2$ (Observation 1.3).
   - No `"input"` key; `"args"` must exist as a JSON object (Observation 1.3).
   - All `required` schema arguments from `tool.json` must be present in every example's `args`.
   - No undeclared extra arguments allowed in `args`.
2. **Observation Step**:
   - All 12 current `examples.json` files have array length 1, violating condition 1 (Observation 1.1, 1.2).
   - All 12 current `examples.json` files define `"input"` instead of `"args"`, violating condition 2 (Observation 1.2, 1.3).
   - Tools with required arguments (`draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`, `get_node_reactions`, `get_bar_forces`) fail condition 3 because `args` is empty when deserialized (Observation 1.3).
3. **Inference**:
   - Replacing `"input"` with `"args"`, adding a second distinct example to each file, ensuring all required parameters are provided, and ensuring only declared parameters are used will satisfy all 6 assertion checks in `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples`.
4. **Conclusion**:
   - All 12 `examples.json` files must be updated with the exact JSON specifications provided in Section 4.

---

## 3. Caveats

- This investigation is strictly read-only per the Teamwork Explorer protocol. The proposed JSON files are formulated and documented in full; actual file writing must be executed by the remediation worker agent.
- `Geometry/get_coordinate_systems_and_grids` defines `"properties": {}` (zero arguments) in its `tool.json`. In accordance with `HPRebar` (`View/get_current_view_info`), both examples provide distinct titles with `"args": {}`. In `ToolValidator.cs`, identical args produce only a non-fatal warning (`warnings.Add("all examples have identical args...")`), not an error, while `SeedLibraryChallengerTests` requires $\ge 2$ examples with `"args": {}`.
- Live GUI interaction with Robot 2026 was not part of this investigation (covered in Milestone M6 live harness).

---

## 4. Conclusion & Actionable Fix Recommendations

The remediation worker (`worker_m3_r2`) must replace the content of all 12 `examples.json` files in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**/examples.json` with the following exact JSON payloads:

### 1. `Model/get_model_info/examples.json`
```json
[
  {
    "title": "Get model info with load cases overview",
    "args": {
      "includeCases": true
    }
  },
  {
    "title": "Get basic model summary without load cases",
    "args": {
      "includeCases": false
    }
  }
]
```

### 2. `Geometry/get_structural_objects/examples.json`
```json
[
  {
    "title": "Get first 50 structural objects of all types",
    "args": {
      "objectType": "all",
      "limit": 50
    }
  },
  {
    "title": "Query up to 100 bar elements",
    "args": {
      "objectType": "bars",
      "limit": 100
    }
  }
]
```

### 3. `Property/get_materials_and_sections/examples.json`
```json
[
  {
    "title": "Get all defined materials and bar sections",
    "args": {
      "includeMaterials": true,
      "includeSections": true
    }
  },
  {
    "title": "List bar cross-sections only",
    "args": {
      "includeMaterials": false,
      "includeSections": true
    }
  }
]
```

### 4. `Geometry/get_coordinate_systems_and_grids/examples.json`
```json
[
  {
    "title": "Get all structural axis grids",
    "args": {}
  },
  {
    "title": "Inspect structural grid definitions and axes",
    "args": {}
  }
]
```

### 5. `Load/get_load_definitions/examples.json`
```json
[
  {
    "title": "Get list of simple load cases and combinations",
    "args": {
      "includeRecords": false
    }
  },
  {
    "title": "Get load cases with detailed load records",
    "args": {
      "includeRecords": true
    }
  }
]
```

### 6. `Geometry/draw_bar_by_coords/examples.json`
```json
[
  {
    "title": "Draw vertical column from (0,0,0) to (0,0,3.5) with HEA 200 section",
    "args": {
      "startX": 0.0,
      "startY": 0.0,
      "startZ": 0.0,
      "endX": 0.0,
      "endY": 0.0,
      "endZ": 3.5,
      "sectionName": "HEA 200"
    }
  },
  {
    "title": "Draw horizontal beam from (0,0,3.5) to (6,0,3.5) with IPE 300 section",
    "args": {
      "startX": 0.0,
      "startY": 0.0,
      "startZ": 3.5,
      "endX": 6.0,
      "endY": 0.0,
      "endZ": 3.5,
      "sectionName": "IPE 300",
      "barNumber": 101
    }
  }
]
```

### 7. `Geometry/assign_node_support/examples.json`
```json
[
  {
    "title": "Assign pinned support to base nodes 1 and 2",
    "args": {
      "nodeNumbers": "1 2",
      "supportType": "Pinned"
    }
  },
  {
    "title": "Assign fixed support to column foundation nodes 1 to 4",
    "args": {
      "nodeNumbers": "1to4",
      "supportType": "Fixed"
    }
  }
]
```

### 8. `Property/assign_bar_section/examples.json`
```json
[
  {
    "title": "Assign IPE 300 to beam bars 1 to 5",
    "args": {
      "barNumbers": "1to5",
      "sectionName": "IPE 300"
    }
  },
  {
    "title": "Assign HEA 200 to column bars 10 11 12",
    "args": {
      "barNumbers": "10 11 12",
      "sectionName": "HEA 200"
    }
  }
]
```

### 9. `Load/assign_bar_load/examples.json`
```json
[
  {
    "title": "Apply -15 kN/m uniform gravity load to bars 1 to 4 in case 1",
    "args": {
      "caseNumber": 1,
      "barNumbers": "1to4",
      "loadType": "uniform",
      "pz": -15.0
    }
  },
  {
    "title": "Apply -25 kN concentrated load at midspan of bar 5 in case 2",
    "args": {
      "caseNumber": 2,
      "barNumbers": "5",
      "loadType": "concentrated",
      "pz": -25.0,
      "relativePosition": 0.5
    }
  }
]
```

### 10. `Analysis/run_calculations/examples.json`
```json
[
  {
    "title": "Run full calculations with automatic model generation",
    "args": {
      "autoGenerateModel": true
    }
  },
  {
    "title": "Run solver on existing mesh without model regeneration",
    "args": {
      "autoGenerateModel": false
    }
  }
]
```

### 11. `Results/get_node_reactions/examples.json`
```json
[
  {
    "title": "Get reactions on all supported nodes for load case 1",
    "args": {
      "caseNumber": 1,
      "nodeNumbers": "all"
    }
  },
  {
    "title": "Get reactions for nodes 1 and 2 in combination 3",
    "args": {
      "caseNumber": 3,
      "nodeNumbers": "1 2"
    }
  }
]
```

### 12. `Results/get_bar_forces/examples.json`
```json
[
  {
    "title": "Get internal forces at 5 points for bar 1 in load case 1",
    "args": {
      "barNumber": 1,
      "caseNumber": 1,
      "pointsCount": 5
    }
  },
  {
    "title": "Get end internal forces (2 points) for bar 2 in combination 3",
    "args": {
      "barNumber": 2,
      "caseNumber": 3,
      "pointsCount": 2
    }
  }
]
```

---

## 5. Verification Method

### 5.1 Verification Commands
To verify independently once the files are written by the remediation worker:
```powershell
# 1. Rebuild solution
dotnet build HPRobot/HPRobot.slnx -c Debug

# 2. Run the specific examples schema test suite
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-method "*Seed_ExamplesJson*"

# 3. Run all seed challenger tests (60 tests)
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-class "*SeedLibraryChallengerTests*"
```

### 5.2 Expected Outcomes
- Step 2: Total 12, Succeeded 12, Failed 0, Skipped 0.
- Step 3 (once Defect 1 C# syntax fixes are also applied): Total 60, Succeeded 60, Failed 0, Skipped 0.

### 5.3 Invalidation Conditions
- Any `examples.json` file retains `"input"` instead of `"args"`.
- Any `examples.json` file has fewer than 2 examples (`count < 2`).
- Any required property from `tool.json` is missing in any example's `args`.
- Any property not defined in `tool.json` `inputSchema.properties` is introduced into `args`.
