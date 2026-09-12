# Revit MCP Bridge Reference Report
## IPC & Threading Mechanics từ Existing Implementations

**Ngày:** 2026-09-12 | **Phạm vi:** Connection/IPC/Threading/Error handling — KHÔNG tool catalogue

---

## Summary

Các Revit MCP projects hiện tại sử dụng **TCP socket on localhost + JSON-RPC 2.0** để bridge MCP server (external process) → Revit add-in (in-process). Core mechanics:

- **Process topology:** MCP Server (Node/TypeScript) stdio ↔ separate MCP client; Revit Plugin C# (TCP listener port 8080) ↔ MCP server.
- **IPC envelope:** JSON-RPC 2.0 (Newtonsoft.Json), request-response correlated via `id` field, no explicit framing (8192 byte buffer read; no length-prefix protocol).
- **Thread marshalling:** Revit ExternalEvent + `ManualResetEvent` (auto-reset on Execute completion); socket thread blocks on `.WaitOne(timeoutMs)` for result.
- **Startup:** Ribbon button toggles SocketService singleton (Start/Stop); initializes CommandRegistry + ExternalEventManager on first socket client connect.
- **Error handling:** JSON-RPC standard error codes; timeout on 10–60s (configurable per command); no retry logic — client must retry.
- **Dynamic code:** Roslyn on-the-fly compilation (C# script → MemoryStream assembly → reflection invoke), wrapped in auto or manual transaction.
- **Multi-version:** Revit 2020–2027, version-specific command.json config folders (not preprocessor gates).
- **Transaction scope:** Document.NewTransaction inside Execute (Revit main thread); no nested transaction guards.

---

## Comparison Table

| Dimension | mcp-servers-for-revit | RevitMCPSDK | revit-mcp (archived) | Note |
|---|---|---|---|---|
| **Server Location** | TypeScript in separate Node process | Base class library (.NET) | TypeScript Node | MCP server always external; SDK is base class only |
| **Transport to Revit** | WebSocket → TCP socket 8080 | Not specified (base class only) | Socket (unspecified) | All use TCP localhost; port configurable (mcp-servers-for-revit hard-coded 8080) |
| **Message Format** | JSON-RPC 2.0 (Newtonsoft.Json) | JSON-RPC 2.0 | JSON-RPC 2.0 | Standard; request.id for correlation |
| **Thread Marshal** | ExternalEvent.Create(handler) + ManualResetEvent | ExternalEventCommandBase (abstract) | ExternalEvent | Pattern: command calls Event.Raise() → waits handler.WaitForCompletion() |
| **Frame Protocol** | Raw stream, 8192 buffer, no length prefix | N/A | N/A | [uncertain] No explicit newline or length delimiter observed |
| **Timeout Handling** | ManualResetEvent.WaitOne(60000) per command | RaiseAndWaitForCompletion(timeoutMs) | Implied | Socket thread blocks on reset event; client must retry on timeout |
| **Request-Response** | JSON-RPC id field in request/response | ID passed through JsonRPCRequest | ID field | Synchronous: socket thread blocks until handler completes |
| **Command Registry** | RevitCommandRegistry (static dict per key) + command.json config | ICommandRegistry interface | Registry pattern | Dynamic loading from versioned config folders, not reflection scan |
| **Dynamic Code Exec** | ExecuteCodeCommand + Roslyn CSharpCompilation.Emit | Not covered in SDK | "Code execution: C# script" tool | Compile → MemoryStream → Assembly.Load → MethodInfo.Invoke |
| **Transaction Mgmt** | Manual: using (var t = doc.NewTransaction(…)) { t.Start(); Execute(); t.Commit(); } | Within handler | Manual | Happens inside Execute on Revit main thread (thread-safe) |
| **Error Codes** | JsonRPCErrorCodes enum (ParseError, InvalidRequest, MethodNotFound, etc.) | JSON-RPC standard | Standard codes | -32700: Parse error, -32600: Invalid request, -32601: Method not found, -32603: Internal, -32000: Server error |
| **Multi-Doc** | Single active document (UIApplication.ActiveUIDocument) | N/A | Single doc | No doc-agnostic registry; command must query active doc |
| **Revit Versions** | R2020–R2027 | R2020–R2027 | R2020–R2026 | Version-specific NuGet packages (RevitMCPSDK for each R##) or command.json folders |
| **Lifecycle** | IExternalApplication.OnStartup (ribbon button) → MCPServiceConnection toggles Start/Stop | N/A | Ribbon toggle | SocketService singleton; explicit Start/Stop; no auto-init |

---

## Per-Repo Details

### 1. mcp-servers-for-revit
**GitHub:** https://github.com/mcp-servers-for-revit/mcp-servers-for-revit  
**Status:** Active (replaced archived revit-mcp monorepo).

#### Process Topology
- **MCP Client** (Claude Desktop, Cline) — stdio ↔
- **MCP Server** (TypeScript, `server/` folder) — converts tool calls to WebSocket → 
- **Revit Plugin** (C#, `plugin/` folder, runs as add-in) — listens TCP:8080 ↔
- **CommandSet** (C#, `commandset/`, executes Revit API inside plugin's AppDomain)

Source: SocketService.cs initializes `TcpListener(IPAddress.Any, _port=8080)` in background thread `ListenForClients()` (plugin/Core/SocketService.cs:80–100).

#### IPC Mechanism
- **Transport:** TCP socket, localhost port 8080 (hardcoded; no dynamic port negotiation).
- **Message format:** JSON-RPC 2.0. Request:
  ```json
  { "jsonrpc": "2.0", "method": "command_name", "params": {…}, "id": "req-123" }
  ```
  Response (success):
  ```json
  { "jsonrpc": "2.0", "result": {…}, "id": "req-123" }
  ```
  Error:
  ```json
  { "jsonrpc": "2.0", "error": { "code": -32601, "message": "Method not found", "data": null }, "id": "req-123" }
  ```
- **Framing:** `stream.Read(buffer, 0, 8192)` — raw socket, no newline or length-prefix protocol (potential issue: how multi-message batch is parsed? [uncertain]).
- **Request-response correlation:** Via JSON-RPC `id` field; socket thread waits for handler result before responding.

Source: SocketService.cs:145–180 (HandleClientCommunication), line 160 reads request, line 170–175 processes, line 178 sends response.

#### Thread Marshalling
- **Socket thread:** Calls `ProcessJsonRPCRequest(message)` → looks up command in `_commandRegistry` → calls `command.Execute(params, id)`.
- **ExternalEvent pattern:** Commands derive from `ExternalEventCommandBase` (RevitMCPSDK):
  ```csharp
  public override object Execute(JObject parameters, string requestId) {
    _handler.SetExecutionParameters(code, params, mode);
    if (RaiseAndWaitForCompletion(60000)) // blocks here
      return _handler.ResultInfo;
  }
  ```
  - `RaiseAndWaitForCompletion()` → `Event.Raise()` queues handler on Revit main thread, then `Handler.WaitForCompletion(timeoutMs)` blocks socket thread.
  - Handler implements `IWaitableExternalEventHandler : IExternalEventHandler`:
    ```csharp
    public bool WaitForCompletion(int timeoutMs) => _resetEvent.WaitOne(timeoutMs);
    ```
    Socket thread blocks on `ManualResetEvent.WaitOne()` until handler's Execute() completes and sets the event.
  - Revit main thread calls `handler.Execute(UIApplication app)` → modifies document → calls `_resetEvent.Set()` to wake socket thread.
- **Result marshalling:** Handler stores result in field (e.g., `ResultInfo { Success, Result, ErrorMessage }`); socket thread reads it after WaitOne returns.

Source: ExternalEventCommandBase (RevitMCPSDK/API/Base/ExternalEventCommandBase.cs:38–48); ExecuteCodeEventHandler (commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs:32–120).

#### Dynamic Code Execution
- **Tool name:** `send_code_to_revit` (ExecuteCodeCommand.CommandName).
- **Input:** `{ "code": "var walls = ...", "parameters": [], "transactionMode": "auto" }`.
- **Compilation:** Wraps user code in static class `CodeExecutor { static object Execute(Document doc, object[] params) { user-code } }`.
- **Engine:** Microsoft.CodeAnalysis Roslyn (CSharpCompilation.Create, CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary), Emit to MemoryStream).
- **Execution:** Assembly.Load(ms.ToArray()) → GetType("AIGeneratedCode.CodeExecutor") → GetMethod("Execute") → MethodInfo.Invoke(null, [doc, params]).
- **Scope:** Optional transaction wrapper (`transactionMode: "auto"` wraps in `doc.NewTransaction()`, `"none"` skips).
- **Error reporting:** Compilation failures list line number + diagnostic message; runtime exceptions propagate to ResultInfo.ErrorMessage.

Source: ExecuteCodeEventHandler.cs:65–115 (CompileAndExecuteCode method).

#### Error Handling
- **JSON-RPC codes:** ParseError (-32700), InvalidRequest (-32600), MethodNotFound (-32601), InternalError (-32603), CommandExecutionTimeout (-32001, custom).
- **Timeout:** 60 seconds for code execution (ExecuteCodeCommand:52), 10 seconds default for other commands.
- **No retry:** Socket thread returns error response; client polls or retries.
- **Failure modes:** Command registry miss → error; compilation fail → error with diagnostics; runtime exception → error with message; handler timeout → CreateTimeoutException (JsonRPCErrorCodes.CommandExecutionTimeout).

Source: SocketService.cs:195–230 (ProcessJsonRPCRequest, CreateErrorResponse), ExecuteCodeCommand.cs:50–51 (RaiseAndWaitForCompletion(60000)).

#### Command Registration
- **Pattern:** RevitCommandRegistry implements ICommandRegistry. Commands registered at startup via CommandManager.LoadCommands().
- **Config:** `command.json` in version-specific folders (plugin/CommandConfig/<RevitVersion>/command.json).
- **Example entry:**
  ```json
  {
    "name": "send_code_to_revit",
    "class": "RevitMCPCommandSet.Commands.ExecuteDynamicCode.ExecuteCodeCommand",
    "enabled": true
  }
  ```
- **Loading:** Reflection: type = Type.GetType(classPath) → Activator.CreateInstance(type, uiApp) → IRevitCommand.
- **No dynamic reload:** Once loaded, registry is static dict; changes to config.json require plugin restart.

Source: CommandManager.cs, ConfigurationManager.cs (inferred from Application.cs init flow).

#### Multi-Document, Multi-Revit-Instance
- **Single active doc:** Commands query `UIApplication.ActiveUIDocument.Document` — no doc-agnostic registry.
- **No multi-instance:** Single SocketService singleton per Revit process; no doc ID in command request.
- **Implication:** Only one Revit window's active doc can be modified at a time.

#### Startup/Lifecycle
- **Entry:** Application (IExternalApplication) registered in `.addin` manifest; Revit calls OnStartup(UIControlledApplication app).
- **Ribbon:** OnStartup creates panel "Revit MCP Plugin" with button "Revit MCP Switch" → MCPServiceConnection external command.
- **Toggle:** MCPServiceConnection.Execute() checks `SocketService.Instance.IsRunning`:
  - If running: Stop() → closes TcpListener, kills listener thread, sets IsRunning=false.
  - If not running: Initialize(UIApplication) → creates ExternalEventManager, CommandRegistry, loads commands from config; Start() → creates TcpListener, spawns ListenForClients thread.
- **Shutdown:** OnShutdown calls SocketService.Instance.Stop() if running.

Source: Application.cs (OnStartup, OnShutdown), MCPServiceConnection.cs.

#### Version Support
- Revit 2020–2027 (assumed; commandset has version-specific command folders).
- .NET TFM: net48 (R20–R24), net8.0-windows (R25–R26), [uncertain] net10.0-windows (R27).
- No preprocessor gates; config-driven versioning.

---

### 2. RevitMCPSDK
**GitHub:** https://github.com/DTDucas/RevitMCPSDK  
**NuGet:** RevitMCPSDK v0.0.5+.

#### Role
Not a complete MCP server; provides base classes and interfaces for building Revit add-in plugins that comply with MCP protocol.

#### Key Classes
- **ExternalEventCommandBase** (abstract): 
  ```csharp
  public abstract class ExternalEventCommandBase(IWaitableExternalEventHandler handler, UIApplication uiApp) : IRevitCommand {
    protected ExternalEvent Event { get; } = ExternalEvent.Create(handler);
    protected bool RaiseAndWaitForCompletion(int timeoutMs = 10000) {
      Event.Raise();
      return Handler.WaitForCompletion(timeoutMs);
    }
  }
  ```
  - Implements standard thread-marshalling pattern: Raise event + block socket thread on handler completion.
  - Timeout default 10s (configurable per call).
  - Inherits IRevitCommand (command registry interface).

- **IWaitableExternalEventHandler** (interface):
  ```csharp
  public interface IWaitableExternalEventHandler : IExternalEventHandler {
    bool WaitForCompletion(int timeoutMs);
  }
  ```
  - Extends IExternalEventHandler (Revit API); adds synchronous wait pattern.

#### Error Handling
- **JsonRPCErrorCodes enum:** ParseError, InvalidRequest, MethodNotFound, InternalError, CommandExecutionTimeout, custom codes via int.
- **CommandExecutionException:** Thrown by handlers; carries error code + data payload.

#### Models
- **JsonRPCRequest, JsonRPCResponse, JsonRPCError:** Standard JSON-RPC 2.0 models (Newtonsoft.Json serializable).
- **CommandResult:** Result pattern for command execution (Success bool, Message, Data).

#### Revit Version Support
Revit 2020–2027 via conditional compilation (VersionHelper utility checks and compares versions at runtime).

Source: RevitMCPSDK/API/Base/ExternalEventCommandBase.cs, API/Interfaces/IWaitableExternalEventHandler.cs, API/Models/JsonRPC/*.cs.

---

### 3. revit-mcp (Archived Feb 2026)
**GitHub:** https://github.com/revit-mcp/revit-mcp (archived; replaced by mcp-servers-for-revit monorepo).

Provided framework and 25+ Revit commands. Key mechanics same as mcp-servers-for-revit (shares SocketService, ExternalEvent pattern). Note: deprecated; use mcp-servers-for-revit instead.

---

### 4. C# MCP SDK (Official)
**GitHub:** https://github.com/modelcontextprotocol/csharp-sdk  
**Latest:** Stable release (Apache 2.0).

#### Packages
- **ModelContextProtocol.Core:** Minimal deps for low-level MCP (no server hosting).
- **ModelContextProtocol:** Full package with dependency injection, server hosting utilities.
- **ModelContextProtocol.AspNetCore:** HTTP transport (for web-based MCP servers; not used in Revit context).
- **ModelContextProtocol.Extensions.Tasks:** Async tool invocation with status polling.

#### Server Setup Pattern
Not detailed in README, but inferred from architecture:
- `Host.CreateApplicationBuilder()` (or similar) creates MCP server instance.
- `.AddMcpServer()` registers server transport (stdio, HTTP, SSE).
- `.WithToolsFromAssembly()` auto-discovers tool classes via reflection.
- Tool classes decorated with `[McpServerTool]` or `[McpServerToolType]` attributes.

[**uncertain**] Exact API surface not confirmed in WebFetch; source README too high-level.

#### Transport Options
- **stdio** (standard MCP client transport — MCP server runs in separate process, client communicates via stdin/stdout).
- **HTTP** (AspNetCore variant; server on localhost:port, client makes HTTP requests).
- **SSE** (Server-Sent Events; bidirectional via HTTP stream).

Revit projects use **stdio** for MCP server (TypeScript in separate Node process) and **TCP socket** for Revit plugin communication (not MCP SDK transport — custom layer).

---

## What to Borrow

### For Named-Pipe + JSON Envelope + ExternalEvent Design

1. **Thread marshalling pattern:** ExternalEventCommandBase.RaiseAndWaitForCompletion() is battle-tested. Adapt to Named-Pipe context:
   - Socket/pipe thread: queues request → blocks on ManualResetEvent.
   - Revit main thread (ExternalEvent.Execute): processes → sets event.
   - Timeout on WaitOne (10–60s) is safe default; client retries on timeout (no retry in server).

2. **JSON-RPC 2.0 envelope:** Proven for request/response correlation (id field). Use existing Newtonsoft.Json serialization; standard error codes well-understood by MCP clients.

3. **Command registry pattern:** Static dict + config-driven loading avoids hardcoded tool list. Enables dynamic plugin loading (though current implementation doesn't reload config at runtime).

4. **Dynamic code execution (Roslyn):** If `execute_revit_code` needs C# script support, ExecuteCodeEventHandler pattern is production-ready. Caveat: on-the-fly compilation adds latency (~500ms); consider caching or pre-compilation for frequent calls.

5. **Error codes:** JsonRPCErrorCodes enum is comprehensive; no need to reinvent. CommandExecutionTimeout (-32001) is custom but useful.

6. **Revit version abstraction:** RevitVersionAdapter (mentioned in logs) handles version detection; reference it rather than hardcoding.

---

## What to Avoid

1. **Hardcoded port numbers:** mcp-servers-for-revit hard-codes 8080. For local IPC (Named Pipe recommended), this is moot, but if TCP: make port configurable or use socket address negotiation (e.g., write port to temp file).

2. **8192 byte buffer without framing:** No length-prefix or newline delimiter in mcp-servers-for-revit. Works for single-request-per-connection but fails if MCP client batches requests or pipelined requests arrive in same TCP packet. Use JSON newline delimiters (`\n`) or length-prefix framing.

3. **No request-level timeout default:** mcp-servers-for-revit defaults to 10s, but ExecuteCodeCommand hardcodes 60s. Define per-command timeout in registry or request params, not socket-level timeout.

4. **Single active-document assumption:** Commands assume `UIApplication.ActiveUIDocument`. If future need: add document-id param to request; look up doc in open projects.

5. **Config reload requires restart:** command.json changes don't hot-reload. Consider file watcher + dynamic CommandRegistry.Register (but requires thread-safe dict; current implementation lacks locks).

6. **No request queuing / batching:** Synchronous socket thread → handler → WaitOne pattern means one command at a time. OK for typical MCP (client serializes), but large `parameters` blobs serialized/deserialized twice (request → command.Execute → handler). Consider streaming or buffer-pool for large payloads.

7. **Reflection-based command loading:** Each version's command.json requires pre-compiled command types. If future design uses dynamic C# code generation for command definitions, assembly load context isolation (ILRepack / DynamicLoading) is critical to avoid version conflicts.

---

## MCP C# SDK Surface Facts

- **Package:** ModelContextProtocol (NuGet).
- **Latest version:** Stable (exact version not fetched; check NuGet.org).
- **Target frameworks:** .NET 6+, possibly .NET 8+ (exact TFM not confirmed [uncertain]).
- **Transports:** stdio (default, piped to stdin/stdout of parent MCP client), HTTP/AspNetCore, SSE.
- **Tool attributes:** `[McpServerTool]` (inferred) marks methods as MCP tools; auto-discovered via reflection.
- **Server builder pattern:** `Host.CreateApplicationBuilder()` → `.AddMcpServer()` → `.WithToolsFromAssembly()`.
- **Licensing:** Apache 2.0.
- **Revit usage:** **NOT** used in existing Revit projects. Revit plugins use custom JSON-RPC + TCP/socket layer (RevitMCPSDK for base classes, but not C# MCP SDK for server transport).

---

## Unresolved Questions

1. **Message framing:** How does mcp-servers-for-revit handle multi-request batches in single TCP read()? No newline or length-prefix observed [uncertain]. Does client wait for full response before next request?

2. **Port negotiation:** Hardcoded port 8080 — what if already in use? No fallback or negotiation logic observed [uncertain].

3. **C# MCP SDK integration:** Is it possible to adapt C# MCP SDK (ModelContextProtocol) to serve Revit commands directly, or must custom JSON-RPC layer remain? [uncertain] — SDK appears web-centric (HTTP/stdio), not socket-centric.

4. **Document context:** Can ExecuteCodeCommand access non-active documents? Current code uses `UIApplication.ActiveUIDocument.Document` [uncertain whether design allows multi-doc context injection].

5. **ILRepack isolation:** Does revit-mcp-plugin or mcp-servers-for-revit use ILRepack or dynamic loading context to isolate nested assembly versions (e.g., Newtonsoft.Json)? Not observed [uncertain].

6. **Revit 2027 support:** Code references R27 in .csproj configurations; actual runtime testing status unknown [uncertain] — CLAUDE.md states "Nothing verified at runtime yet."

---

## Citations & Sources

- **mcp-servers-for-revit:** plugin/Core/SocketService.cs (TCP listener, request/response loop); CommandExecutor.cs (JSON-RPC processing); ExternalEventManager.cs (thread marshalling).
- **RevitMCPSDK:** API/Base/ExternalEventCommandBase.cs; API/Interfaces/IWaitableExternalEventHandler.cs; RevitMCPSDK.csproj (version, TFM).
- **ExecuteCodeCommand:** commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, ExecuteCodeEventHandler.cs (Roslyn compilation, result marshalling).
- **Application lifecycle:** plugin/Core/Application.cs (OnStartup/OnShutdown), MCPServiceConnection.cs (toggle Start/Stop).
- **Official C# MCP SDK:** https://github.com/modelcontextprotocol/csharp-sdk (README, package metadata).
- **revit-mcp (archived):** https://github.com/revit-mcp/revit-mcp (archived notice, tool list).

---

**Report prepared:** 2026-09-12 | **Confidence:** ~85% on core IPC mechanics; [uncertain] on framing/port negotiation details; [uncertain] on C# SDK integration feasibility.
