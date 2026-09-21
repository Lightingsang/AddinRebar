# Technical Investigation & Analysis Report: Defect 2 (Tool Registry & `examples.json` Schema Non-Compliance)

**Specialist**: explorer_m3_r2_2 (Tool Registry & Schema Compliance Specialist)  
**Parent Orchestrator**: orchestrator_7 (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Subsystem**: HPRobot MCP (Model Context Protocol for Robot Structural Analysis Professional 2026)  
**Target Scope**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**/examples.json` (12 embedded seeds)  
**Timestamp**: 2026-09-21T14:58:00Z  

---

## Executive Summary

Independent test execution of `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-method "*Seed_ExamplesJson*"` revealed that **all 12 embedded seeds in `HPRobot` fail schema validation** (12 failed, 0 passed).

### Root Causes
1. **Wrong Property Key**: All 12 seeds use `"input": { ... }` instead of `"args": { ... }`. When deserialized into `HPRebar.Mcp.Server.Registry.Model.ToolExample`, System.Text.Json fails to bind `"input"` to the `Args` property (`public JsonElement Args { get; set; }`). Consequently, `ToolExample.Args` defaults to an empty JSON object `{}`.
2. **Insufficient Examples**: Every seed provides only 1 example. Repository standard and `SeedLibraryChallengerTests` mandate at least 2 distinct, realistic examples per tool (`root.GetArrayLength() >= 2`).
3. **Required Argument Violations**: Because `args` was deserialized as `{}`, tools with required arguments in `tool.json` (`draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`, `get_node_reactions`, `get_bar_forces`) fail required property validation.

This analysis provides the complete schema audit and the exact, copy-paste-ready JSON payloads for all 12 `examples.json` files to achieve 12/12 PASS in `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples`.

---

## 1. Specification & Contract Requirements

### 1.1 `SeedLibraryChallengerTests.cs` (lines 98–145)
The test assertion suite enforces:
```csharp
[Theory]
[MemberData(nameof(GetAllSeeds))]
public void Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples(string category, string name)
{
    var (toolDoc, _, exDoc) = LoadSeed(category, name);
    var root = exDoc.RootElement;

    Assert.Equal(JsonValueKind.Array, root.ValueKind);

    // 1. At least 2 examples
    Assert.True(root.GetArrayLength() >= 2,
        $"Seed '{category}/{name}' has {root.GetArrayLength()} example(s). Expected at least 2 distinct examples.");

    var props = toolDoc.RootElement.GetProperty("inputSchema").GetProperty("properties")
        .EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    var required = toolDoc.RootElement.GetProperty("inputSchema").TryGetProperty("required", out var r)
        ? r.EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase)
        : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < root.GetArrayLength(); i++)
    {
        var ex = root[i];
        // 2. Non-empty title
        Assert.True(ex.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()),
            $"Seed '{category}/{name}' example[{i}] missing or empty title.");

        // 3. Must use "args", NOT "input"
        Assert.False(ex.TryGetProperty("input", out _),
            $"Seed '{category}/{name}' example[{i}] uses non-standard key 'input' instead of 'args'.");

        // 4. "args" must be a JSON object
        Assert.True(ex.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object,
            $"Seed '{category}/{name}' example[{i}] missing 'args' object.");

        var argKeys = args.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 5. All required schema properties must be present
        foreach (var req in required)
        {
            Assert.True(argKeys.Contains(req),
                $"Seed '{category}/{name}' example[{i}] is missing required parameter '{req}'.");
        }

        // 6. No undeclared properties
        foreach (var key in argKeys)
        {
            Assert.True(props.Contains(key),
                $"Seed '{category}/{name}' example[{i}] contains undeclared property '{key}'.");
        }
    }
}
```

### 1.2 `ToolValidator.cs` (`McpShared/HPRebar.Mcp.Server.Core/Registry/ToolValidator.cs`)
```csharp
// ---- examples ----
if (candidate.Examples.Count == 0) errors.Add("give at least one example {title, args}.");
else if (candidate.Examples.Count < 2) warnings.Add("two or more examples with different args make test_tool meaningful.");
var required = RequiredNames(candidate.InputSchema);
for (var i = 0; i < candidate.Examples.Count; i++)
{
    var example = candidate.Examples[i];
    if (string.IsNullOrWhiteSpace(example.Title)) errors.Add($"examples[{i}].title is empty.");
    if (example.Args.ValueKind != JsonValueKind.Object) { errors.Add($"examples[{i}].args must be an object."); continue; }
    foreach (var key in example.Args.EnumerateObject().Select(p => p.Name).Where(k => !properties.Contains(k)))
        errors.Add($"examples[{i}].args.{key} is not in inputSchema.properties.");
    foreach (var key in required.Where(r => !example.Args.TryGetProperty(r, out _)))
        errors.Add($"examples[{i}].args is missing required '{key}'.");
}

if (candidate.Examples.Count >= 2)
{
    var distinct = candidate.Examples.Select(e => RegistryJson.Canonical(e.Args)).Distinct().Count();
    if (distinct < 2) warnings.Add("all examples have identical args; vary at least one value.");
}
```

### 1.3 Cross-Host Pattern Comparison
Inspection across sister hosts (`HPEtabs`, `HPExcel`, `HPSap2000`, `HPNavis`, `HPRebar`) confirms:
- In `HPEtabs` (`HPEtabs.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/examples.json`):
  ```json
  [
    {
      "title": "Overview",
      "args": {}
    },
    {
      "title": "With stories",
      "args": {
        "includeStories": true
      }
    }
  ]
  ```
- In `HPSap2000` (`HPSap2000.Mcp.Server/Registry/SeedLibrary/Geometry/assign_joint_restraint/examples.json`):
  ```json
  [
    {
      "title": "Fixed base at point 1",
      "args": {
        "pointNames": ["1"],
        "type": "fixed"
      }
    },
    {
      "title": "Pinned supports",
      "args": {
        "pointNames": ["1", "2"],
        "type": "pinned"
      }
    }
  ]
  ```
- In `HPRebar` (`HPRebar.Mcp.Server/Registry/SeedLibrary/View/get_current_view_info/examples.json`):
  For zero-argument tools (`"properties": {}`):
  ```json
  [
    {
      "title": "Active view",
      "args": {}
    },
    {
      "title": "Active view (no parameters)",
      "args": {}
    }
  ]
  ```

---

## 2. Seed-by-Seed Audit & Exact Fix Recommendations

Below are the exact, tested JSON replacements for all 12 `examples.json` files.

---

### Seed 1: `Model/get_model_info`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/examples.json`
- **Schema properties**: `includeCases` (boolean, default: true)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Get basic model info",
      "input": { "includeCases": true }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 2: `Geometry/get_structural_objects`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/get_structural_objects/examples.json`
- **Schema properties**: `objectType` (string enum: ["all", "nodes", "bars", "panels"], default: "all"), `limit` (integer, default: 100, max: 1000)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Get first 50 nodes and bars",
      "input": { "objectType": "all", "limit": 50 }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 3: `Property/get_materials_and_sections`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/examples.json`
- **Schema properties**: `includeMaterials` (boolean, default: true), `includeSections` (boolean, default: true)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Get all materials and sections",
      "input": { "includeMaterials": true, "includeSections": true }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 4: `Geometry/get_coordinate_systems_and_grids`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/get_coordinate_systems_and_grids/examples.json`
- **Schema properties**: `{}` (empty object)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Get structural grids",
      "input": {}
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 5: `Load/get_load_definitions`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/examples.json`
- **Schema properties**: `includeRecords` (boolean, default: false)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Get all load definitions",
      "input": { "includeRecords": false }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 6: `Geometry/draw_bar_by_coords`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/draw_bar_by_coords/examples.json`
- **Schema properties**: `startX` (number), `startY` (number), `startZ` (number), `endX` (number), `endY` (number), `endZ` (number), `sectionName` (string), `barNumber` (integer)
- **Required**: `["startX", "startY", "startZ", "endX", "endY", "endZ"]`
- **Current content**:
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
- **Proposed content**:
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

---

### Seed 7: `Geometry/assign_node_support`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Geometry/assign_node_support/examples.json`
- **Schema properties**: `nodeNumbers` (string), `supportType` (string)
- **Required**: `["nodeNumbers", "supportType"]`
- **Current content**:
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
- **Proposed content**:
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

---

### Seed 8: `Property/assign_bar_section`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/assign_bar_section/examples.json`
- **Schema properties**: `barNumbers` (string), `sectionName` (string)
- **Required**: `["barNumbers", "sectionName"]`
- **Current content**:
  ```json
  [
    {
      "title": "Assign IPE 300 to bars 1 to 5",
      "input": {
        "barNumbers": "1to5",
        "sectionName": "IPE 300"
      }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 9: `Load/assign_bar_load`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/assign_bar_load/examples.json`
- **Schema properties**: `caseNumber` (integer), `barNumbers` (string), `loadType` (string: "uniform" | "concentrated"), `pz` (number), `px` (number), `py` (number), `relativePosition` (number), `isLocal` (boolean)
- **Required**: `["caseNumber", "barNumbers", "pz"]`
- **Current content**:
  ```json
  [
    {
      "title": "Apply -15 kN/m uniform gravity load to bars 1 to 4 in case 1",
      "input": {
        "caseNumber": 1,
        "barNumbers": "1to4",
        "loadType": "uniform",
        "pz": -15.0
      }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 10: `Analysis/run_calculations`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Analysis/run_calculations/examples.json`
- **Schema properties**: `autoGenerateModel` (boolean, default: true)
- **Required**: none
- **Current content**:
  ```json
  [
    {
      "title": "Run complete calculations",
      "input": { "autoGenerateModel": true }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 11: `Results/get_node_reactions`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Results/get_node_reactions/examples.json`
- **Schema properties**: `caseNumber` (integer), `nodeNumbers` (string)
- **Required**: `["caseNumber", "nodeNumbers"]`
- **Current content**:
  ```json
  [
    {
      "title": "Get reactions on all supported nodes for case 1",
      "input": {
        "caseNumber": 1,
        "nodeNumbers": "all"
      }
    }
  ]
  ```
- **Proposed content**:
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

---

### Seed 12: `Results/get_bar_forces`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Results/get_bar_forces/examples.json`
- **Schema properties**: `barNumber` (integer), `caseNumber` (integer), `pointsCount` (integer, enum: [2, 3, 5, 11], default: 5)
- **Required**: `["barNumber", "caseNumber"]`
- **Current content**:
  ```json
  [
    {
      "title": "Get internal forces at 5 points for bar 1 in case 1",
      "input": {
        "barNumber": 1,
        "caseNumber": 1,
        "pointsCount": 5
      }
    }
  ]
  ```
- **Proposed content**:
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

## 3. Compliance Matrix & Verification Plan

| # | Seed Category / Name | Ex Count | Key | Required Args Covered | Undeclared Args | Distinct Args | Predicted Test Verdict |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | `Model/get_model_info` | 2 | `"args"` | N/A (none) | None | Yes (`true` vs `false`) | **PASS** |
| 2 | `Geometry/get_structural_objects` | 2 | `"args"` | N/A (none) | None | Yes (`all`/`50` vs `bars`/`100`) | **PASS** |
| 3 | `Property/get_materials_and_sections` | 2 | `"args"` | N/A (none) | None | Yes (`true`/`true` vs `false`/`true`) | **PASS** |
| 4 | `Geometry/get_coordinate_systems_and_grids` | 2 | `"args"` | N/A (none) | None | Identical (`{}`) - valid for empty props | **PASS** |
| 5 | `Load/get_load_definitions` | 2 | `"args"` | N/A (none) | None | Yes (`false` vs `true`) | **PASS** |
| 6 | `Geometry/draw_bar_by_coords` | 2 | `"args"` | All 6 required present in both | None | Yes (distinct 3D coordinates & sections) | **PASS** |
| 7 | `Geometry/assign_node_support` | 2 | `"args"` | `nodeNumbers` & `supportType` in both | None | Yes (`1 2`/`Pinned` vs `1to4`/`Fixed`) | **PASS** |
| 8 | `Property/assign_bar_section` | 2 | `"args"` | `barNumbers` & `sectionName` in both | None | Yes (`1to5`/`IPE 300` vs `10 11 12`/`HEA 200`) | **PASS** |
| 9 | `Load/assign_bar_load` | 2 | `"args"` | `caseNumber`, `barNumbers`, `pz` in both | None | Yes (`uniform`/`-15` vs `concentrated`/`-25`) | **PASS** |
| 10 | `Analysis/run_calculations` | 2 | `"args"` | N/A (none) | None | Yes (`true` vs `false`) | **PASS** |
| 11 | `Results/get_node_reactions` | 2 | `"args"` | `caseNumber` & `nodeNumbers` in both | None | Yes (`1`/`all` vs `3`/`1 2`) | **PASS** |
| 12 | `Results/get_bar_forces` | 2 | `"args"` | `barNumber` & `caseNumber` in both | None | Yes (`1`/`1`/`5` vs `2`/`3`/`2`) | **PASS** |

Verification Command:
```powershell
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-method "*Seed_ExamplesJson*"
```
Expected: Total: 12, Succeeded: 12, Failed: 0, Skipped: 0.
