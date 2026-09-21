# Technical Investigation & Architectural Specification for HPPowerBi.McpBridge

**Author**: `explorer_bridge_1` (Teamwork Explorer)  
**Date**: 2026-09-21  
**Project**: AddinRebar / HPPowerBi Subsystem  
**Scope**: Read-only technical investigation and specification for `HPPowerBi.McpBridge` (Task R1 in `ORIGINAL_REQUEST.md`)

---

## Executive Summary

`HPPowerBi.McpBridge` is a standalone WPF desktop bridge application (`net8.0-windows`) that connects Power BI Desktop's local Analysis Services (SSAS in-process Tabular Engine) and the Power BI Service Cloud REST API to the host-neutral MCP ecosystem via named pipe `hppowerbi-mcp-2026`.

Unlike Revit, AutoCAD, or Navisworks, which require in-process plugin loaders and dynamic ALCs inside the vendor executable, Power BI Desktop hosts a child `msmdsrv.exe` process that listens on a local TCP loopback port (`127.0.0.1:<dynamic_port>`). Similar to the standalone desktop bridge pattern proven in `HPEtabs.McpBridge` and `HPSap2000.McpBridge`, `HPPowerBi.McpBridge` runs as an out-of-process WPF application, consuming official Microsoft client libraries (`AMO-TOM`, `ADOMD.NET`, `MSAL.NET`) from NuGet without any need for installed vendor SDK DLLs.

---

## 1. Power BI Desktop Local Analysis Services Connection

### 1.1 Process Detection & Hierarchy
When Power BI Desktop opens a report (`.pbix`), the process architecture is:
```
[PBIDesktop.exe] (Parent PID, MainWindowTitle = "<Report Title> - Power BI Desktop")
       │
       └── Spawns child process:
             [msmdsrv.exe] (Child PID, SQL Server Analysis Services VertiPaq engine)
                   │
                   ├── Workspace Directory: %LocalAppData%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\<WorkspaceId>\Data
                   ├── Port File: msmdsrv.port.txt (Contains local TCP port)
                   └── Listens on: 127.0.0.1:<Port>
```

#### Detection Algorithm
1. **Process Discovery**:
   - Enumerate all instances of `PBIDesktop`:
     ```csharp
     var pbiProcesses = Process.GetProcessesByName("PBIDesktop");
     ```
   - Extract the active report title from `MainWindowTitle`:
     ```csharp
     public static string ExtractReportTitle(string windowTitle)
     {
         const string suffix = " - Power BI Desktop";
         if (string.IsNullOrWhiteSpace(windowTitle)) return "Untitled";
         return windowTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
             ? windowTitle.Substring(0, windowTitle.Length - suffix.Length).Trim()
             : windowTitle.Trim();
     }
     ```

2. **Locating Child `msmdsrv.exe` and Local TCP Port**:
   There are three robust techniques to determine the port:

   - **Technique A (Command-Line Inspection via WMI / Process Query)**:
     PBIDesktop starts `msmdsrv.exe` passing the workspace directory in its command line:
     `msmdsrv.exe -s "C:\Users\<user>\AppData\Local\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\AnalysisServicesWorkspace_<guid>\Data"`
     Querying `Win32_Process` for child processes where `ParentProcessId == pbiProcess.Id`:
     ```csharp
     using var searcher = new ManagementObjectSearcher(
         $"SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msmdsrv.exe' AND ParentProcessId = {pbiProcess.Id}");
     ```
     Extract the directory following `-s` and locate `msmdsrv.port.txt`.

   - **Technique B (Direct Inspection of AnalysisServicesWorkspaces)**:
     Path: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Power BI Desktop", "AnalysisServicesWorkspaces")`
     When PBIDesktop runs, it creates `AnalysisServicesWorkspace_<guid>` (or numeric index).
     Inside each workspace, `Data\msmdsrv.port.txt` contains the active port.
     To map the folder to the specific PBIDesktop PID when multiple instances are running, match the creation/write time of the directory lock or check file handles.

   - **Technique C (TCP Table Query via `GetExtendedTcpTable` - Highest Performance & Reliability)**:
     Using Windows IP Helper API (`iphlpapi.dll` with `TCP_TABLE_OWNER_PID_ALL`), scan for listening TCP sockets (`127.0.0.1`) owned by the child `msmdsrv.exe` PID.
     This method has zero file I/O, no file locking issues, and resolves in `< 2ms`.

3. **Reading `msmdsrv.port.txt` Correctly**:
   - **Critical Nuance**: `msmdsrv.port.txt` is encoded by SSAS as **UTF-16LE (Unicode)**.
   - If read as ASCII or UTF-8 without BOM, null bytes (`\0`) corrupt the parsed integer.
   - The file may be locked by `msmdsrv.exe`; opening requires `FileShare.ReadWrite`:
     ```csharp
     public static int ReadPortFile(string portFilePath)
     {
         using var stream = new FileStream(portFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
         using var reader = new StreamReader(stream, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);
         var content = reader.ReadToEnd().Trim();
         if (int.TryParse(content, out var port)) return port;
         throw new InvalidOperationException($"Invalid port format in '{portFilePath}': '{content}'");
     }
     ```

---

### 1.2 Connecting via AMO-TOM & ADOMD.NET
Microsoft distributes official, fully managed .NET Core/.NET 8 client packages:
- **AMO-TOM**: `Microsoft.AnalysisServices.NetCore.retail` (Tabular Object Model for schema inspection and metadata modification).
- **ADOMD.NET**: `Microsoft.AnalysisServices.AdomdClient.NetCore.retail` (High-performance DAX query execution).

#### Connection Setup
```csharp
// 1. Connect via TOM for Schema & Metadata Operations
var server = new Microsoft.AnalysisServices.Tabular.Server();
server.Connect($"Data Source=localhost:{port};");

// A local PBIDesktop SSAS instance contains exactly one Database (GUID or model name)
var database = server.Databases[0];
var model = database.Model;

// 2. Connect via ADOMD.NET for DAX Query Execution
var adomdConnStr = $"Data Source=localhost:{port};Initial Catalog={database.Name};";
var adomdConnection = new Microsoft.AnalysisServices.AdomdClient.AdomdConnection(adomdConnStr);
await adomdConnection.OpenAsync(cancellationToken);
```

---

### 1.3 Querying Tabular Model Schema (TOM)
The Tabular Object Model (`Microsoft.AnalysisServices.Tabular.Model`) provides an object graph of the dataset:

```
Model
 ├── Tables (Table)
 │     ├── Name, Description, IsHidden, DataCategory
 │     ├── Columns (DataColumn, CalculatedColumn, CalculatedTableColumn)
 │     │     ├── Name, DataType (Int64, Double, String, DateTime, Decimal, Boolean)
 │     │     ├── Type (Data, Calculated, RowNumber)
 │     │     ├── Expression (DAX expression for calculated columns)
 │     │     ├── FormatString, IsHidden, DataCategory
 │     ├── Measures (Measure)
 │     │     ├── Name, Expression (DAX formula)
 │     │     ├── FormatString, Description, DisplayFolder, IsHidden
 │     ├── Partitions (Partition)
 │     │     ├── Mode (Import, DirectQuery, Dual)
 │     │     ├── SourceType (M, Calculated, Query)
 │     │     └── Source (MPartitionSource.Expression contains Power Query M code)
 │     └── Hierarchies (Hierarchy)
 │           └── Levels (HierarchyLevel -> Column)
 └── Relationships (SingleColumnRelationship)
       ├── Name, IsActive
       ├── FromTable, FromColumn
       ├── ToTable, ToColumn
       ├── CrossFilteringBehavior (OneDirection, BothDirections, Automatic)
       ├── Cardinality (ManyToOne, OneToMany, OneToOne, ManyToMany)
       └── SecurityFilteringBehavior (BothDirections, OneDirection)
```

#### DTO Representation for MCP JSON-RPC
To expose the schema efficiently without dumping megabytes of internal metadata, the bridge maps the TOM model to clean DTOs:
```csharp
public sealed record TabularModelSchemaDto(
    string ModelName,
    int CompatibilityLevel,
    IReadOnlyList<TableSchemaDto> Tables,
    IReadOnlyList<RelationshipSchemaDto> Relationships);

public sealed record TableSchemaDto(
    string Name,
    string? Description,
    bool IsHidden,
    IReadOnlyList<ColumnSchemaDto> Columns,
    IReadOnlyList<MeasureSchemaDto> Measures,
    IReadOnlyList<PartitionSchemaDto> Partitions);

public sealed record ColumnSchemaDto(
    string Name,
    string DataType,
    string Type,
    string? Expression,
    string? FormatString,
    bool IsHidden);

public sealed record MeasureSchemaDto(
    string Name,
    string Expression,
    string? FormatString,
    string? Description,
    string? DisplayFolder,
    bool IsHidden);

public sealed record RelationshipSchemaDto(
    string Name,
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn,
    bool IsActive,
    string Cardinality,
    string CrossFilteringBehavior);
```

---

### 1.4 DAX Execution & Serialization (ADOMD.NET)
DAX queries are executed using `AdomdCommand` and read with `AdomdDataReader`:

```csharp
public async Task<DaxExecutionResult> ExecuteDaxAsync(string daxQuery, int maxRows, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();
    using var cmd = new AdomdCommand(daxQuery, _adomdConnection);
    using var reader = (AdomdDataReader)await cmd.ExecuteReaderAsync(ct);

    var columns = new List<DaxColumnHeader>();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        columns.Add(new DaxColumnHeader(reader.GetName(i), reader.GetFieldType(i).Name));
    }

    var rows = new List<object?[]>();
    bool truncated = false;
    while (await reader.ReadAsync(ct))
    {
        if (rows.Count >= maxRows)
        {
            truncated = true;
            break;
        }

        var row = new object?[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i))
            {
                row[i] = null;
            }
            else
            {
                var val = reader.GetValue(i);
                // Serialize DateTime as ISO-8601 string
                row[i] = val is DateTime dt ? dt.ToString("o", CultureInfo.InvariantCulture) : val;
            }
        }
        rows.Add(row);
    }
    sw.Stop();

    return new DaxExecutionResult(columns, rows, rows.Count, truncated, sw.ElapsedMilliseconds);
}
```

#### Tabular JSON Response Format
```json
{
  "columns": [
    { "name": "[Customer City]", "dataType": "String" },
    { "name": "[Total Sales]", "dataType": "Double" },
    { "name": "[Order Count]", "dataType": "Int64" }
  ],
  "rows": [
    ["Hanoi", 1254000.50, 312],
    ["Da Nang", 874500.00, 198],
    ["Ho Chi Minh City", 3420000.75, 845]
  ],
  "rowCount": 3,
  "truncated": false,
  "durationMs": 42
}
```

---

### 1.5 TOM Measure & Relationship CRUD Operations
Microsoft officially supports external tool modifications to measures, relationships, calculation groups, perspectives, and display folders in Power BI Desktop. When `model.SaveChanges()` is called, Power BI Desktop reflects the updates live in the Fields list and repaints all affected visual components!

#### 1. Measure Upsert & Delete
```csharp
public MeasureMutationResult UpsertMeasure(string tableName, string measureName, string expression, string? formatString, string? description, string? displayFolder)
{
    var table = _model.Tables[tableName] 
        ?? throw new ArgumentException($"Table '{tableName}' not found in model.");

    var measure = table.Measures.Find(measureName);
    bool isNew = measure is null;

    if (isNew)
    {
        measure = new Measure
        {
            Name = measureName,
            Expression = expression,
            FormatString = formatString,
            Description = description,
            DisplayFolder = displayFolder
        };
        table.Measures.Add(measure);
    }
    else
    {
        measure!.Expression = expression;
        if (formatString != null) measure.FormatString = formatString;
        if (description != null) measure.Description = description;
        if (displayFolder != null) measure.DisplayFolder = displayFolder;
    }

    _model.SaveChanges();
    return new MeasureMutationResult(tableName, measureName, isNew ? "Created" : "Updated");
}

public bool DeleteMeasure(string tableName, string measureName)
{
    var table = _model.Tables[tableName] 
        ?? throw new ArgumentException($"Table '{tableName}' not found in model.");
    var measure = table.Measures.Find(measureName);
    if (measure is null) return false;

    table.Measures.Remove(measure);
    _model.SaveChanges();
    return true;
}
```

#### 2. Relationship Management
```csharp
public void CreateOrUpdateRelationship(string fromTable, string fromColumn, string toTable, string toColumn, bool isActive, RelationshipCardinality cardinality, CrossFilteringBehavior crossFilter)
{
    var tFrom = _model.Tables[fromTable] ?? throw new ArgumentException($"FromTable '{fromTable}' not found.");
    var cFrom = tFrom.Columns[fromColumn] ?? throw new ArgumentException($"FromColumn '{fromColumn}' not found.");
    var tTo = _model.Tables[toTable] ?? throw new ArgumentException($"ToTable '{toTable}' not found.");
    var cTo = tTo.Columns[toColumn] ?? throw new ArgumentException($"ToColumn '{toColumn}' not found.");

    string relName = $"{fromTable}_{fromColumn}_{toTable}_{toColumn}";
    var existing = _model.Relationships.Find(relName);

    if (existing is SingleColumnRelationship rel)
    {
        rel.IsActive = isActive;
        rel.Cardinality = cardinality;
        rel.CrossFilteringBehavior = crossFilter;
    }
    else
    {
        var newRel = new SingleColumnRelationship
        {
            Name = relName,
            FromTable = tFrom,
            FromColumn = cFrom,
            ToTable = tTo,
            ToColumn = cTo,
            IsActive = isActive,
            Cardinality = cardinality,
            CrossFilteringBehavior = crossFilter
        };
        _model.Relationships.Add(newRel);
    }

    _model.SaveChanges();
}
```

---

## 2. External Tools Auto-Registration

### 2.1 File Format & Schema (`.pbitool.json`)
Power BI Desktop discovers third-party tools via `.pbitool.json` configuration files located in the Windows Common Files directory.

#### Schema Definition
```json
{
  "version": "1.0",
  "name": "HP Power BI MCP",
  "description": "AI-assisted MCP Bridge connecting Power BI Desktop to AI coding agents",
  "executable": "C:\\Program Files\\HP Power BI MCP\\HPPowerBi.McpBridge.exe",
  "arguments": "\"%server%\" \"%database%\"",
  "iconData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAA..."
}
```

#### Parameters Supported by Power BI Desktop
| Token | Resolution by Power BI Desktop |
|---|---|
| `"%server%"` | Resolved to the local SSAS connection string, e.g. `localhost:51234` |
| `"%database%"` | Resolved to the internal SSAS database GUID, e.g. `6a12b3c4-5d6e-7f8a-9b0c-1d2e3f4a5b6c` |

### 2.2 Target Path Hierarchy & Fallbacks
Power BI Desktop checks external tool directories in the following priority order:

1. **Standard 64-bit Directory (Default & Primary Target)**:  
   `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\`  
   → `C:\Program Files\Common Files\Microsoft Shared\Power BI Desktop\External Tools\HPPowerBi.pbitool.json`

2. **32-bit Compatibility Directory (For 32-bit PBIDesktop installs)**:  
   `%CommonProgramFiles(x86)%\Microsoft Shared\Power BI Desktop\External Tools\`  
   → `C:\Program Files (x86)\Common Files\Microsoft Shared\Power BI Desktop\External Tools\HPPowerBi.pbitool.json`

3. **Per-User AppData Fallback (If writing to Program Files requires Admin/UAC)**:  
   `%LocalAppData%\Microsoft\Power BI Desktop\External Tools\`  
   (Some portable and custom installations support user-local registration).

### 2.3 Bridge Auto-Registration Workflow
On startup, `HPPowerBi.McpBridge` verifies its registration:
1. Resolves its own current executable location: `Environment.ProcessPath`.
2. Checks if `HPPowerBi.pbitool.json` exists in `External Tools` folder and has matching `executable` path.
3. If missing or path differs:
   - Attempts direct write to `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\`.
   - If `UnauthorizedAccessException` occurs, displays an in-UI badge with a "Register Tool (Requires Admin Elevation)" button, running a lightweight UAC self-registration process (`powershell -Command Start-Process -Verb RunAs ...`).
4. Generates the base64-encoded 32x32 PNG icon directly from embedded app resources.

### 2.4 Bridge Launch Parameter Handling
When launched from Power BI Desktop via the ribbon button:
- Invocation: `HPPowerBi.McpBridge.exe "localhost:51234" "6a12b3c4-..."`
- Command-line parser extracts:
  `serverParam` → `port = int.Parse(serverParam.Split(':')[1])`
  `databaseParam` → `databaseGuid`
- The bridge directly connects to that exact instance in `< 100ms` without needing process discovery!

---

## 3. 3-Layer Safety Architecture

Modifying tabular data models, DAX formulas, and relationships can directly alter financial reports and executive dashboards. The 3-layer architecture prevents unauthorized or accidental corruption.

```
┌────────────────────────────────────────────────────────────────────────┐
│ Layer 1: Human-in-the-Loop UI Opt-In Gate                              │
│  [x] Allow DAX Execution (Read-Only)                                   │
│  [ ] Allow Model Modifications (Measures / Relationships / Partitions) │
└───────────────────────────────────────────────────┬────────────────────┘
                                                    │ Passes Layer 1
                                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Layer 2: DAX & AST Validation / Classification                         │
│  - EVALUATE query syntax validation                                    │
│  - Denial of raw DDL / XMLA / TMSL execution                           │
│  - GuardProfile: Deny Disconnect/Dispose/MessageBox                    │
└───────────────────────────────────────────────────┬────────────────────┘
                                                    │ Passes Layer 2
                                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Layer 3: Pre-Mutation Metadata & TMDL Snapshot Backup                  │
│  - Serialize full model to TMSL JSON before applying TOM changes       │
│  - Write to %LocalAppData%\HPPowerBi\Snapshots\<timestamp>.json        │
│  - Retain last 20 snapshots with 1-click restore / rollback capability │
└────────────────────────────────────────────────────────────────────────┘
```

### Layer 1: UI Opt-In Confirmation Checkboxes
Modeled after `HPEtabs` and `HPSap2000`:
1. `IsExecutionEnabled` ("Allow DAX Queries & Schema Inspection"):
   - Controls read-only tools: `get_powerbi_context`, `powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_format_dax`.
   - If unchecked, returns error code `-32002`: `"Code execution is disabled. Tick 'Allow DAX Queries' in the HPPowerBi MCP Bridge window."`
2. `IsMutationEnabled` ("Allow Model Modifications"):
   - Gates structural mutations: `powerbi_create_or_update_measure`, `powerbi_delete_measure`, `powerbi_manage_relationship`, and C# scripts invoking `model.SaveChanges()`.
   - Default is **FALSE** (Unchecked).
   - If unchecked and a mutation is attempted, returns error code `-32002` with explicit guidance:
     `"Model mutation is disabled. To allow creating or modifying measures and relationships, tick 'Allow Model Modifications' in the HPPowerBi MCP Bridge window."`

### Layer 2: DAX & Script AST Validation
- **DAX Query Validation**:
  - Validates that DAX queries start with `EVALUATE` or `DEFINE ... EVALUATE`.
  - Rejects XMLA/TMSL commands (`<Batch>`, `<Create>`, `<Alter>`, `<Delete>`) sent to the DAX evaluator.
  - Sanitizes against administrative DMV commands that could terminate sessions (`DISCOVER_TRACE`, `KILL`).
- **Roslyn Script Guard (`GuardProfile.PowerBi`)**:
  - `GuardProfile.PowerBi` (already configured in `McpShared`):
    - Denied namespaces: `System.Windows.Forms`, `HPPowerBi.McpBridge`, `HPRebar.McpBridge.Core.Host`.
    - Denied members: `server.Disconnect()`, `server.Dispose()`, `adomd.Close()`, `adomd.Dispose()`.
    - Denied UI calls: `MessageBox`.
  - Mutation Detection in C# Scripts:
    - Analyzes AST for calls to `model.SaveChanges()`, `table.Measures.Add()`, `table.Measures.Remove()`, or `model.Relationships.Add()`. If present, enforces `IsMutationEnabled = true`.

### Layer 3: Model Backup / Snapshot before Any Mutation
Before applying any mutation to the active model:
1. **Serialization**:
   - Tabular Model Scripting Language (TMSL) JSON export:
     ```csharp
     public string CreateSnapshot(Microsoft.AnalysisServices.Tabular.Database database, string reason)
     {
         Directory.CreateDirectory(_snapshotDirectory);
         var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
         var safeDbName = string.Join("_", database.Name.Split(Path.GetInvalidFileNameChars()));
         var fileName = $"{timestamp}_{safeDbName}.json";
         var fullPath = Path.Combine(_snapshotDirectory, fileName);

         var json = Microsoft.AnalysisServices.Tabular.JsonSerializer.SerializeDatabase(database, 
             new Microsoft.AnalysisServices.Tabular.SerializeOptions
             {
                 IncludeRestrictedInformation = false
             });

         File.WriteAllText(fullPath, json, Encoding.UTF8);
         PruneOldSnapshots(maxRetained: 20);
         Log.Information("Model snapshot saved before {Reason}: {File}", reason, fileName);
         return fileName;
     }
     ```
2. **Rollback & Restore Capability**:
   - If a measure or relationship edit produces invalid DAX, calculation errors, or visual breaks, the previous model state can be restored from the saved snapshot JSON via `JsonSerializer.DeserializeDatabase(json)`.
3. **Execution Result Attachment**:
   - Populates `ExecuteResult.Snapshot` with the filename of the saved backup, making it immediately visible in tool responses to AI agents and in the bridge status window.

---

## 4. Cloud REST API Client

For enterprise scenarios where datasets reside in Power BI Service (Power BI Pro / Premium / Fabric), `HPPowerBi.McpBridge` provides a native cloud client.

### 4.1 MSAL.NET OAuth 2.0 Flows
Using `Microsoft.Identity.Client` (MSAL.NET v4.66+):

#### Flow 1: Service Principal (Headless / Agent-friendly)
- Best for automated workflows, CI/CD pipelines, and unattended agents.
- Credentials: `TenantId`, `ClientId`, `ClientSecret`.
- Authority: `https://login.microsoftonline.com/{tenantId}`
- Scope: `https://analysis.windows.net/powerbi/api/.default`
```csharp
public async Task<string> AcquireTokenWithServicePrincipalAsync(string tenantId, string clientId, string clientSecret, CancellationToken ct)
{
    var app = ConfidentialClientApplicationBuilder.Create(clientId)
        .WithClientSecret(clientSecret)
        .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
        .Build();

    var scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };
    var result = await app.AcquireTokenForClient(scopes).ExecuteAsync(ct);
    return result.AccessToken;
}
```

#### Flow 2: Device Code / Interactive Flow (Human Login)
- For personal Pro/Premium accounts without app registration secrets.
- Scopes:
  - `https://analysis.windows.net/powerbi/api/Workspace.Read.All`
  - `https://analysis.windows.net/powerbi/api/Dataset.ReadWrite.All`
```csharp
public async Task<string> AcquireTokenWithDeviceCodeAsync(string tenantId, string clientId, Action<DeviceCodeResult> onCodeReceived, CancellationToken ct)
{
    var app = PublicClientApplicationBuilder.Create(clientId)
        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
        .WithDefaultRedirectUri()
        .Build();

    var scopes = new[] {
        "https://analysis.windows.net/powerbi/api/Workspace.Read.All",
        "https://analysis.windows.net/powerbi/api/Dataset.ReadWrite.All"
    };

    var result = await app.AcquireTokenWithDeviceCode(scopes, codeResult => {
        onCodeReceived(codeResult); // Displays: "Go to https://microsoft.com/devicelogin and enter code ABCD-EFGH"
        return Task.CompletedTask;
    }).ExecuteAsync(ct);

    return result.AccessToken;
}
```

---

### 4.2 Power BI Service REST Endpoints
Base URL: `https://api.powerbi.com/v1.0/myorg/`  
Authorization Header: `Bearer {accessToken}`

#### 1. List Workspaces (`GET v1.0/myorg/groups`)
- Query: `GET v1.0/myorg/groups?$top=100`
- Response:
  ```json
  {
    "value": [
      {
        "id": "f0840771-4277-4c7b-99f4-123456789abc",
        "name": "Finance & Operations",
        "isReadOnly": false,
        "type": "Workspace"
      }
    ]
  }
  ```

#### 2. List Datasets (`GET v1.0/myorg/groups/{groupId}/datasets`)
- Query: `GET v1.0/myorg/groups/{groupId}/datasets`
- Response:
  ```json
  {
    "value": [
      {
        "id": "e496a790-2e90-4822-8356-987654321def",
        "name": "General Ledger 2026",
        "configuredBy": "admin@company.com",
        "isRefreshable": true,
        "targetStorageMode": "PremiumFiles"
      }
    ]
  }
  ```

#### 3. Trigger Dataset Refresh (`POST v1.0/myorg/groups/{groupId}/datasets/{datasetId}/refreshes`)
- Invocation:
  `POST v1.0/myorg/groups/{groupId}/datasets/{datasetId}/refreshes`
  Body: `{"notifyOption": "MailOnFailure"}`
- Returns: HTTP `202 Accepted`.
- Refresh Status Polling:
  `GET v1.0/myorg/groups/{groupId}/datasets/{datasetId}/refreshes?$top=1`
  Returns status: `Completed`, `Failed`, `InProgress`, `Unknown`.

#### 4. Execute DAX Queries in the Cloud (`POST v1.0/myorg/datasets/{datasetId}/executeQueries`)
- Supports running arbitrary DAX queries directly against Power BI Service datasets without local Power BI Desktop!
- Request:
  ```json
  {
    "queries": [
      {
        "query": "EVALUATE TOPN(10, 'General Ledger', 'General Ledger'[Amount], DESC)"
      }
    ],
    "serializerSettings": {
      "includeNulls": true
    }
  }
  ```
- Response:
  ```json
  {
    "results": [
      {
        "tables": [
          {
            "rows": [
              {
                "General Ledger[Account]": "1110 - Cash",
                "General Ledger[Amount]": 5420100.00
              }
            ]
          }
        ]
      }
    ]
  }
  ```

---

## 5. WPF MVVM UI Design

The UI design follows the proven architecture of `HPSap2000.McpBridge` and `HPEtabs.McpBridge`, providing a modeless desktop window with MaterialDesignThemes 5.3.2 integration.

### 5.1 Theming & Visual Styling
- Framework: `MaterialDesignThemes` 5.3.2 and `CommunityToolkit.Mvvm` 8.4.0.
- Color System:
  - **Power BI Brand Palette**: Primary accent color is Power BI Gold/Yellow (`#F2C811` or `#E6AD00`) paired with secondary Deep Amber (`#D99B00`).
  - Dark Mode Background: `#1E1E1E` (Dark Slate) with Card `#252526`.
  - Light Mode Background: `#F5F5F5` with Card `#FFFFFF`.
- Automatic Theme Synchronization:
  - `WindowsHostTheme.cs`: Listens to `SystemEvents.UserPreferenceChanged` to automatically track Windows OS Light/Dark mode changes.
  - Runtime theme toggle button on the title bar for manual override.

### 5.2 UI Layout & Controls Breakdown

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  [Icon] HP Power BI MCP Bridge 2026                 [Theme ☀/🌙]  [—] [X]   │
├─────────────────────────────────────────────────────────────────────────────┤
│  Card 1: Power BI Desktop Instance                                         │
│  Instance: [ SalesModel.pbix (PID: 14208, Port: 51234)        ▼ ]  [Refresh]│
│  Status:   ● Connected to localhost:51234 (Model: "SalesModel")             │
│  Summary:  Tables: 14 | Measures: 42 | Relationships: 18 | Compat: 1600     │
│  Actions:  [ Connect ]  [ Detach ]                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Card 2: External Tools Integration                                        │
│  Status:   ✓ Registered in Power BI Desktop External Tools Ribbon           │
│  Action:   [ Re-register .pbitool.json ]                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│  Card 3: 3-Layer Safety Controls                                            │
│  [✓] Allow DAX Execution (Read-Only queries and schema exploration)         │
│  [ ] Allow Model Modifications (Creating/updating measures & relationships) │
│  Auto-Backup: Active (Last snapshot: 2026-09-21 13:15:00)                  │
│  Action:   [ Open Snapshots Folder ]                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  Card 4: Named Pipe Listener (hppowerbi-mcp-2026)                          │
│  Pipe:     hppowerbi-mcp-2026                 [Copy Name]                   │
│  Status:   ● Listening (Clients: 1 | Total Requests: 14)                    │
│  Action:   [ Open Logs Folder ]                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  Card 5: Power BI Cloud Service (Optional)                              [▼] │
│  Auth Mode: (•) Service Principal   ( ) Interactive Device Code             │
│  Tenant ID: [ xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx ]                        │
│  Status:    Authenticated as Contoso Tenant                                 │
│  Action:    [ Test Cloud Connection ]                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.3 ViewModel Architecture (`PowerBiBridgeStatusViewModel`)
Inherits `ObservableObject` and coordinates:
- `McpBridgeStatusViewModel Core`: Shared status view model from `HPRebar.McpBridge.Core.ViewModel` (manages listener state, execution checkbox, copy pipe name).
- Instance Management:
  - `ObservableCollection<PowerBiInstanceInfo> Instances`: Dynamically populated list of running PBIDesktop processes.
  - `PowerBiInstanceInfo? SelectedInstance`: Selected process + port.
  - `IRelayCommand RefreshInstancesCommand`
  - `IRelayCommand ConnectCommand`
  - `IRelayCommand DetachCommand`
- Safety Checkboxes:
  - `bool IsExecutionEnabled`: Binds to `Core.IsExecutionEnabled`.
  - `bool IsMutationEnabled`: Controls structural modifications.
- External Tools:
  - `bool IsExternalToolRegistered`: True if `.pbitool.json` is installed.
  - `IRelayCommand RegisterExternalToolCommand`: Installs or updates the `.pbitool.json` file.
- Cloud Configuration:
  - `string TenantId`, `string ClientId`, `string ClientSecret`.
  - `IRelayCommand TestCloudConnectionCommand`.

---

## 6. Project & Dependency Specifications

### 6.1 Solution Structure (`HPPowerBi.slnx`)
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <!-- Power BI MCP Subsystem -->
  <Project Path="HPPowerBi.McpBridge/HPPowerBi.McpBridge.csproj" />
  <Project Path="HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj" />
  <Project Path="HPPowerBi.Mcp.Server/HPPowerBi.Mcp.Server.csproj" />
  <Project Path="HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

### 6.2 Project File Specifications (`HPPowerBi.McpBridge.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPPowerBi.McpBridge</RootNamespace>
    <AssemblyName>HPPowerBi.McpBridge</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <Version>0.1.0</Version>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    <PublishSingleFile>false</PublishSingleFile>
    <ApplicationIcon>Resources\HPPowerBiMcpBridge.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <Resource Include="Resources\HPPowerBiMcpBridge.ico" />
  </ItemGroup>

  <ItemGroup>
    <!-- Microsoft Official Client Libraries -->
    <PackageReference Include="Microsoft.AnalysisServices.NetCore.retail" Version="19.84.6" />
    <PackageReference Include="Microsoft.AnalysisServices.AdomdClient.NetCore.retail" Version="19.84.6" />
    <PackageReference Include="Microsoft.Identity.Client" Version="4.66.2" />

    <!-- UI & MVVM -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="MaterialDesignThemes" Version="5.3.2" />
    
    <!-- Logging -->
    <PackageReference Include="Serilog" Version="4.4.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="HPPowerBi.McpBridge.Tests" />
  </ItemGroup>
</Project>
```

---

## 7. MCP Tools Catalog for Power BI

The stdio MCP server (`HPPowerBi.Mcp.Server`) registers the following tools:

### Core & Desktop Tools
| Tool Name | Type | Description | Safety Gate |
|---|---|---|---|
| `get_powerbi_context` | Core | Returns status of local PBIDesktop connection, database name, port, counts. | None |
| `execute_powerbi_code` | Script | Executes C# Roslyn script with globals `model`, `server`, `adomd`. | `IsExecutionEnabled` / `IsMutationEnabled` |
| `powerbi_get_schema` | High-Level | Returns tables, columns, measures, partitions, and relationships as JSON. | `IsExecutionEnabled` |
| `powerbi_evaluate_dax` | Query | Executes DAX query (`EVALUATE ...`) via ADOMD.NET and formats tabular results. | `IsExecutionEnabled` |
| `powerbi_create_or_update_measure` | Mutation | Creates or updates a DAX measure with expression, format string, description. | `IsMutationEnabled` + Snapshot |
| `powerbi_delete_measure` | Mutation | Deletes a measure from a table. | `IsMutationEnabled` + Snapshot |
| `powerbi_manage_relationship` | Mutation | Creates, updates, or deletes a model relationship. | `IsMutationEnabled` + Snapshot |
| `powerbi_format_dax` | Utility | Formats DAX code cleanly according to standard rules. | None |

### Cloud Service Tools
| Tool Name | Type | Description | Safety Gate |
|---|---|---|---|
| `powerbi_cloud_list_workspaces` | Cloud | Enumerates accessible Power BI Service workspaces via REST API. | Cloud Auth |
| `powerbi_cloud_list_datasets` | Cloud | Enumerates datasets in a given workspace. | Cloud Auth |
| `powerbi_cloud_trigger_refresh` | Cloud | Triggers a dataset refresh in Power BI Service. | Cloud Auth |
| `powerbi_cloud_execute_dax` | Cloud | Runs DAX query against a cloud dataset via `executeQueries` REST endpoint. | Cloud Auth |

---

## 8. Verification & Test Strategy

### Unit Tests (`HPPowerBi.McpBridge.Tests`)
1. **Port Detection Tests**:
   - Verify parsing of UTF-16LE encoded `msmdsrv.port.txt` files containing BOM.
   - Verify handling of file locks (`FileShare.ReadWrite`).
   - Verify window title parsing (`"Sales - Power BI Desktop"` → `"Sales"`).
2. **DAX Serialization Tests**:
   - Verify transformation of mock `IDataReader` / `AdomdDataReader` records into JSON schema and row arrays.
   - Verify DBNull handling and ISO 8601 formatting for DateTime.
   - Verify pagination clamping at `maxRows` and `truncated: true` flag.
3. **Snapshot & Safety Tests**:
   - Verify TMSL JSON serialization creates valid JSON files on disk.
   - Verify retention pruning limits snapshot folder to maximum 20 items.
   - Verify refusal error (-32002) when mutation is attempted with `IsMutationEnabled = false`.
4. **External Tools Schema Tests**:
   - Verify `.pbitool.json` adheres to Microsoft schema v1.0 and contains valid base64 icon data.

### Server Tests (`HPPowerBi.Mcp.Server.Tests`)
1. `PowerBiHostProfileTests`: verifies host profile name, default version (2026), timeout limits (600s), and pipe name resolution (`hppowerbi-mcp-2026`).
2. `PowerBiToolsListTests`: verifies all 12 tools (8 local + 4 cloud) are correctly exposed in `tools/list`.
3. `ToolRegistryLifecycleTests`: verifies registration of dynamic tools and metadata validation.

---

## Conclusion
`HPPowerBi.McpBridge` completes the AI tooling ecosystem for Power BI by following the established architecture of `HPSap2000` and `HPEtabs`. With standard NuGet packages, out-of-process execution, 3-layer safety guarantees, and deep integration with Power BI Desktop's External Tools ribbon, the bridge provides a fast, reliable, and secure bridge for AI agents.
