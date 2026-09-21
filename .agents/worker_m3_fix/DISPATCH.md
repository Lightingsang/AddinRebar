## 2026-09-21T07:41:41Z

Objective:
Remediate the 3 findings reported by challenger_m3_1 for Milestone 3.

Specific Tasks:
1. Fix Action Fallthrough in PbiRelationshipService.cs (HPPowerBi/HPPowerBi.McpBridge/Tabular/PbiRelationshipService.cs):
   In ManageRelationship:
   Validate normalizedAction:
   if (normalizedAction is not ("create" or "delete" or "set_active" or "activate" or "deactivate"))
   {
       throw new ArgumentException($"Unknown relationship action '{action}'. Supported actions: 'create', 'delete', 'set_active'.", nameof(action));
   }
   Ensure unsupported actions never fall through to create relationships or call model.SaveChanges().

2. Format Validation in Bridge & Server:
   - In HPPowerBi/HPPowerBi.McpBridge/Host/PowerBiDispatcher.cs (in EvaluateDaxAsync):
     Validate that if parameters.Format is specified, it must be either "json" or "markdown" (case-insensitive). If invalid, throw BridgeErrorException(BridgeErrorCode.InvalidRequest, $"Unsupported format '{parameters.Format}'. Supported formats: 'markdown', 'json'.");
   - In HPPowerBi/HPPowerBi.Mcp.Server/Tools/PowerBiEvaluateDaxTool.cs:
     Validate that if format is specified, it must be either "markdown" or "json" (case-insensitive). If invalid, return formatter.Error($"Unsupported format '{format}'. Supported formats: 'markdown', 'json'.");

3. Client-Side Parameter Validation in Server Tools (HPPowerBi/HPPowerBi.Mcp.Server/Tools/):
   - PowerBiEvaluateDaxTool.cs: Check string.IsNullOrWhiteSpace(query) -> return formatter.Error("DAX query cannot be empty.");
   - PowerBiCreateOrUpdateMeasureTool.cs: Validate tableName, measureName, daxExpression are not null/whitespace -> return formatter.Error("...");
   - PowerBiDeleteMeasureTool.cs: Validate tableName, measureName are not null/whitespace -> return formatter.Error("...");
   - PowerBiManageRelationshipTool.cs: Validate fromTable, fromColumn, toTable, toColumn, action are not null/whitespace, and action is one of "create", "delete", "set_active" -> return formatter.Error("...");
   - PowerBiFormatDaxTool.cs: Validate dax is not null/whitespace -> return formatter.Error("DAX expression cannot be empty.");

4. Unit Tests:
   Add or update test cases in HPPowerBi.McpBridge.Tests and HPPowerBi.Mcp.Server.Tests verifying that:
   - PbiRelationshipService throws ArgumentException on unknown action.
   - PowerBiEvaluateDaxTool and PowerBiDispatcher reject unsupported formats.
   - Empty/whitespace parameters return clean tool errors instead of unhandled exceptions.

5. Verification:
   Run:
   dotnet build HPPowerBi/HPPowerBi.slnx -c Debug
   dotnet run --project HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj
   dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj
   Ensure 0 warnings, 0 errors, and 100% test pass rate across all projects.
