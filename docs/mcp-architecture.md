# Revit MCP — Overall Architecture

```mermaid
flowchart TD
    subgraph CLIENT["🤖 AI Client"]
        AI["Claude / Cline / Cursor"]
    end

    subgraph TS["📦 TypeScript MCP Server  (mcp/server/src/)"]
        direction LR
        IDX["index.ts\nMcpServer entry"]
        REG["register.ts\ndynamic registration"]
        TOOLS["Tools  20+ *.ts\ncreate_room · create_grid · create_level\nai_element_filter · color_elements\ntag_all_walls · tag_all_rooms\nsend_code_to_revit · export_room_data\nstore/query_project_data …"]
        CM["ConnectionManager.ts\nTCP pool + Mutex\nserializes requests"]
        SC["SocketClient.ts\nJSON-RPC 2.0 client\n2-min timeout"]
        DB[("SQLite DB\nbetter-sqlite3\npersistence")]

        IDX --> REG --> TOOLS --> CM --> SC
        IDX <-.->|store / query| DB
    end

    subgraph TCP["🔌 TCP Transport — localhost:8080"]
        RPC["JSON-RPC 2.0\n{ method, params, id }"]
    end

    subgraph PLUGIN["⚙️ C# Revit Plugin  (mcp/plugin/)"]
        direction LR
        APP["Application.cs\nIExternalApplication\nOnStartup"]
        SS["SocketService.cs\nlisten :8080\nparse JSON-RPC"]
        CMDMGR["CommandManager.cs\nReflection loads DLLs\nvia command.json"]
        REG2["CommandRegistry\nDict&lt;name,\nIRevitCommand&gt;"]
        EXEC["CommandExecutor.cs\nlookup + Execute()"]
        EEM["ExternalEventManager\ncache + Raise()"]
        CFG[/"command.json\n24 commands"/]

        APP --> SS
        SS --> EXEC
        CFG --> CMDMGR --> REG2
        REG2 -.->|lookup| EXEC
        EXEC --> EEM
    end

    subgraph CS["🧩 Command Set  (mcp/commandset/)"]
        direction LR
        CMD["Commands/*.cs\nIRevitCommand\nParse JObject params\nRaiseAndWaitForCompletion\nReturn AIResult&lt;T&gt;"]
        EVT["Services/*.cs\nIExternalEventHandler\nExecute on UI thread\nRevit API + Transaction\nManualResetEvent.Set()"]
        MDL["Models/\nRoomCreationInfo\nElementInfo · ViewInfo\nDimensionCreationInfo\n[JsonProperty] POCOs"]
        UTL["Utils/\nGeometryUtils\nTransactionUtils\nElementIdExtensions"]

        CMD -->|raises| EVT
        MDL -.->|deserialized by| CMD
        UTL -.->|used by| EVT
    end

    subgraph REVIT["🏗️ Autodesk Revit  (UI Thread)"]
        RDOC["Document / UIApplication\nTransaction / FilteredElementCollector"]
    end

    AI -->|MCP stdio| IDX
    SC -->|TCP request| RPC
    RPC -->|TCP response| SC
    RPC -->|JSON-RPC| SS
    EEM -.->|ExternalEvent.Raise| CMD
    EVT -->|Revit API call| RDOC
    RDOC -->|result| EVT
```

## Luồng chính (request → response)

```
AI  →[stdio]→  index.ts  →  Tool.ts  →  ConnectionManager (Mutex)
→  SocketClient  →[TCP :8080]→  SocketService  →  CommandExecutor
→  IRevitCommand.Execute()  →  ExternalEventManager.Raise()
→  IExternalEventHandler (UI thread)  →  Revit API  →  Transaction
→  Result  →  ManualResetEvent.Set()  →  AIResult<T>
→  JSON-RPC response  →[TCP]→  SocketClient  →  MCP response  →  AI
```

## 3 điểm kiến trúc quan trọng

| Vấn đề | Giải pháp |
|--------|-----------|
| Revit API chỉ gọi được từ UI thread | `IExternalEventHandler` + `ExternalEvent.Raise()` |
| TS gửi nhiều request song song | `Mutex` trong `ConnectionManager` serializes chúng |
| Đăng ký command động | `command.json` → `CommandManager` (Reflection) → `CommandRegistry` |

## Cấu trúc thư mục

```
mcp/
├── command.json                  # Registry manifest (24 commands)
├── server/src/
│   ├── index.ts                  # McpServer entry, stdio transport
│   ├── tools/
│   │   ├── register.ts           # Dynamic tool loader
│   │   └── *.ts                  # 20+ tool definitions (Zod schema)
│   ├── utils/
│   │   ├── ConnectionManager.ts  # TCP pool + Mutex
│   │   └── SocketClient.ts       # JSON-RPC 2.0 TCP client
│   └── database/
│       └── service.ts            # SQLite persistence
├── plugin/
│   └── Core/
│       ├── Application.cs        # IExternalApplication entry
│       ├── SocketService.cs      # TCP listener :8080
│       ├── CommandManager.cs     # Reflection DLL loader
│       ├── RevitCommandRegistry.cs
│       ├── CommandExecutor.cs
│       └── ExternalEventManager.cs
└── commandset/
    ├── Commands/                 # IRevitCommand implementations
    ├── Services/                 # IExternalEventHandler implementations
    ├── Models/                   # [JsonProperty] POCOs
    └── Utils/                    # Geometry, Transaction helpers
```
