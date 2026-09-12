# NotebookLM Report — "Building MCP Servers with C#" (Gerald Versluis)

**Ngày:** 2026-09-12 · **Notebook:** `5f0a5e84-6178-44d0-960d-17c0c8280121` · **Source scope:** chỉ PDF `9183ab12-e154-4302-ac92-771f70464769` (`-s`) · **Quota:** 12/50 chat dùng · **Tool:** `notebooklm ask --json` (CLI fallback, MCP server `CONNECTION_CLOSED` session này)

Mục đích: nguồn tri thức ưu tiên #1 cho mọi quyết định MCP trong plan. Mỗi câu trả lời giữ nguyên văn NotebookLM (tiếng Anh), inline `[n]` = citation; bảng citation dưới mỗi câu trích `cited_text` từ PDF (cắt 240 ký tự). Không có kiến thức tự thêm của model trong mục "Answer".

## Mục lục

- Q01 — MCP Architecture — Host / Client / Server
- Q02 — JSON-RPC 2.0 messaging + error model
- Q03 — Connection lifecycle + capability negotiation
- Q04 — Tools trong C# SDK
- Q05 — Resources + resource templates
- Q06 — Prompts
- Q07 — Transport — stdio vs HTTP/SSE/streamable
- Q08 — DI + Hosting model (.NET Generic Host)
- Q09 — Tổ chức project, NuGet, packaging
- Q10 — Testing / debugging / MCP Inspector / client config
- Q11 — Long-running tools — progress, cancellation, logging, sampling, content types
- Q12 — Tool safety — ToolAnnotations, descriptions, validation, security


---

## Q01 — MCP Architecture — Host / Client / Server

**Question:** According to the book, describe the MCP architecture: the roles of Host, Client, and Server, how they relate (one host, many clients, one client per server), and what a session between client and server means. Use the book's own terminology and describe any diagram it gives.

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 34

### Answer (verbatim NotebookLM)

In *Building MCP Servers with C#*, Gerald Versluis defines the Model Context Protocol (MCP) using a **three-layer architecture** consisting of the **Host**, the **Client**, and the **Server** [1, 2].

---

###### 1. Roles of the Three Layers

* **The Host**:
  * **Role**: The AI application that the end user directly interacts with (such as Claude Desktop, GitHub Copilot in VS Code, or a custom chat application) [3].
  * **Responsibilities**: Manages the overall user experience, renders conversations, manages the language model, enforces security policies, and decides which MCP servers to connect to [3].
  * **Key Trait**: The host **never speaks the MCP wire protocol directly**; instead, it delegates all protocol communication to one or more internal MCP clients [4].

* **The Client**:
  * **Role**: The MCP-aware connector that lives inside the host [4]. Versluis uses the analogy of a waiter in a restaurant who takes orders to the kitchen and brings dishes back to the table [5].
  * **Responsibilities**: Opens the transport layer, performs the initialization handshake, sends requests, negotiates capabilities, and routes responses back to the host [4, 5].
  * **Key Trait**: Handles all technical protocol plumbing so the host code remains clean and focused on user-facing concerns [4].

* **The Server**:
  * **Role**: The component exposing capabilities to the client [6].
  * **Responsibilities**: Exposes one or more of the three MCP primitives:
    1. **Tools**: Actions or functions the AI can invoke (e.g., querying a database, sending an email) [6].
    2. **Resources**: Read-only data sources (e.g., files, database records, status feeds) [6].
    3. **Prompts**: Reusable message templates guiding how the AI interacts with a domain [6, 7].

---

###### 2. Architecture Relationships

* **One Host to Many Clients**: A single host application can maintain connections to multiple MCP servers simultaneously (e.g., VS Code connected to a file-system server, a database server, and a web-search server) [3]. To do this, the host spins up a dedicated client instance for every server it wants to communicate with [4].
* **One Client per Server (1:1 Relationship)**: Each client instance maintains a strict **1:1 relationship with a single server** over a transport channel (stdio or HTTP) [5, 8].
* **Fault Isolation**: Because of this 1:1 design, if one server crashes, only the client connected to that specific server is affected, allowing the host and other connected servers to continue running smoothly [5].

---

###### 3. Diagram Description: Figure 2-1

In **Figure 2-1** ("The three-layer MCP architecture showing how the host contains one or more clients, each connected to a separate server"), the book presents the structural layout [8, 9]:

* At the top, the **End User** points down to the **Host** box (labeled *AI Application — e.g., Claude Desktop, VS Code, Custom App*) [9].
* Inside the **Host** box, there are multiple separate sub-boxes: **MCP Client A**, **MCP Client B**, and **MCP Client C** [9].
* Each client box points down through an individual **Transport** arrow to its corresponding server box below:
  * **MCP Client A** ──► **MCP Server A** (*File System*) [9].
  * **MCP Client B** ──► **MCP Server B** (*Database*) [9].
  * **MCP Client C** ──► **MCP Server C** (*Web Search*) [9].
* The caption underscores: *"Each client maintains a 1:1 connection with its server over a transport (stdio or HTTP)"* [9].

---

###### 4. What a "Session" / Protocol Lifecycle Means

A connection or session between an MCP client and server follows a **4-phase lifecycle** (illustrated in **Figure 2-2**) [10, 11]:

1. **Phase 1: Connection**: The transport layer establishes a raw communication channel (e.g., launching a subprocess with stdio streams, or opening an HTTP endpoint) [10, 12].
2. **Phase 2: Initialization**: Capability negotiation occurs via a two-step handshake. The client sends an `initialize` request with its protocol version and capabilities, the server replies with its version/capabilities, and the client sends an `initialized` notification [12, 13]. No operation requests (like `tools/list`) are permitted before initialization finishes [12, 14].
3. **Phase 3: Operation**: The active working phase where the client calls tools, reads resources, and fetches prompts, and the server pushes notifications [14, 15]. In Streamable HTTP transports, explicit session IDs (`Mcp-Session-Id`) and idle timeouts track state across multiple HTTP requests [16-18].
4. **Phase 4: Shutdown**: The client initiates a graceful teardown (closing stdin for stdio or disconnecting HTTP), allowing the server to clean up resources and exit cleanly [19].

---

💡 **Next Step**: Would you like to explore how to implement capability negotiation, or walk through setting up transport layers (stdio vs. Streamable HTTP) in C#?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Chapter 2: MCP Architecture Under the Hood 11 The Three-Layer Architecture 11 The Host 12 The Client 12 |
| [2] | message format that carries information between them, and the lifecycle that governs how a connection is born, lives, and eventually shuts down. Along the way, I will sprinkle in real JSON examples so you can see exactly what travels over t… |
| [3] | 12 The Host The host is the AI application that the end user actually interacts with. Think of Claude Desktop, GitHub Copilot in VS Code, or any custom chat interface your company might build. The host is responsible for the overall user ex… |
| [4] | server, a database server, and a web-search server all at once. One important detail is that the host never speaks the MCP wire protocol directly. It delegates that job to one or more MCP clients that live inside it. This keeps the host cod… |
| [5] | opening the transport, performing the initialization handshake, sending requests, and routing responses back to the host. If the host is a restaurant manager, the client is the waiter who takes orders to the kitchen and brings dishes back t… |
| [6] | negotiation, which we will look at later in this chapter. It makes sure the client and server agree on which features they both support before any real work begins. The Server The server is the component you will spend most of your time bui… |
| [7] | API. Prompts offer reusable templates that guide how the AI interacts with the server’s domain. We will explore each of these in depth: tools in Chapter 5, resources in Chapter 6, and prompts in Chapter 7. Chapter 2 MCp arChiteCture under t… |
| [8] | SOAP (Simple Object Access Protocol) service or a shiny new gRPC (Google Remote Procedure Call) microservice, the client sees the same clean interface. Figure 2-1 illustrates how these layers relate. Notice that the host sits at the top, cl… |
| [9] | develop and test in isolation. You do not need a full AI application running just to verify that your server returns the right data. Figure 2-1. The three-layer MCP architecture showing how the host contains one or more clients, each connec… |
| [10] | The Protocol Lifecycle Every MCP connection follows a well-defined lifecycle with four phases: connection, initialization, operation, and shutdown. Understanding this lifecycle will save you hours of debugging when you start building your o… |
| [11] | capability handshake right matters. Figure 2-2. The four phases of the MCP connection lifecycle Transport Layer The transport layer is the plumbing that carries JSON-RPC messages between client and server. MCP does not force you to use a si… |
| [12] | and connecting to its standard input and output streams. For an HTTP-based transport, it means opening a network connection to the server’s endpoint. At this point, no MCP messages have been exchanged yet. The two sides can send bytes to ea… |
| [13] | exactly what topics are on the table. MCP does the same thing, just with JSON instead of small talk. The Initialization Handshake The handshake is a two-step dance. First, the client sends an initialize request that advertises the protocol … |
| [14] | protocol specification is strict about this: if a server receives a tools/list request before Chapter 2 MCp arChiteCture under the hood20 initialization is complete, it should reject it. This rule prevents race conditions and ensures both s… |
| [15] | prompt templates. The server can send notifications to inform the client about changes, like a new tool becoming available or an existing resource being updated. Both sides can send requests and responses freely, and multiple requests can b… |
| [16] | app that gives you a chat interface to Claude and lets you connect to local MCP servers through a JSON configuration file. To configure Claude Desktop to talk to one of our servers, you need to edit the configuration file. On macOS, this fi… |
| [17] | added a call to MapMcp. The tools themselves stay exactly the same. That is the beauty of the transport abstraction. Session Management One of the key advantages of Streamable HTTP over stdio is session management. When a client connects to… |
| [18] | Think of it this way: MCP tools are essentially additional consumers of your shared infrastructure. The same capacity planning you do for your API endpoints should include your MCP tools. Stateless vs. Stateful Mode When you add MCP to an e… |
| [19] | dictate how long the operation phase should last. It simply provides the rules for how messages flow while it is active. Phase 4: Shutdown When the host decides it is done with a server, the client initiates a graceful shutdown. For stdio s… |


---

## Q02 — JSON-RPC 2.0 messaging + error model

**Question:** How does MCP use JSON-RPC 2.0 according to the book? Describe the request, response and notification message shapes, the id field, the standard error codes, and how a tool failure is reported (protocol-level error vs tool execution error with isError in the result).

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 35

### Answer (verbatim NotebookLM)

According to *Building MCP Servers with C#* by Gerald Versluis, the Model Context Protocol (MCP) uses **JSON-RPC 2.0** as its core messaging foundation [1, 2]. Unlike REST (which relies on HTTP verbs and URLs), JSON-RPC is built around remote procedure/method calls [3]. You send a JSON object specifying a method name and parameters, and the receiver executes it and returns the result [3].

---

###### 1. Message Shapes

JSON-RPC 2.0 defines three types of messages in MCP [3, 4]:

###### **A. Requests**
A request is sent when the caller expects a response [4, 5]. It contains four fields [5]:
* `"jsonrpc"`: Always `"2.0"` [5].
* `"id"`: A unique identifier (integer or string) chosen by the sender [5].
* `"method"`: The method name string (e.g., `"tools/list"`, `"tools/call"`, or `"initialize"`) [5-7].
* `"params"`: An optional JSON object or array containing arguments [5].

**Example Request [8]:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
```

---

###### **B. Responses**
A response is sent in answer to a request [4, 8]. It always echoes the request's `id` and follows a strict **either-or rule**: it contains a `"result"` field on success or an `"error"` field on failure, but **never both** [8, 9].

**Example Successful Response [9]:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "get_weather",
        "description": "Returns the current weather for a city",
        "inputSchema": {
          "type": "object",
          "properties": {
            "city": { "type": "string" }
          },
          "required": ["city"]
        }
      }
    ]
  }
}
```

---

###### **C. Notifications**
Notifications are **fire-and-forget** messages [4, 10]. They look like requests, but **they do not contain an `id` field**, which signals to the receiver that no response is expected or permitted [4, 10]. MCP uses notifications for one-way events such as progress updates, cancellation signals, or capability initialization completion [10, 11].

**Example Notification [4]:**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

---

###### 2. The `id` Field

* **Purpose**: The `id` field is set by the sender (as a number or string) and must be echoed back identically by the receiver in its response [5, 8].
* **Concurrency & Asynchrony**: Because MCP allows multiple requests to be in flight across the same transport channel simultaneously, the echoed `id` allows the client to match incoming responses to their original outgoing requests without confusion [5, 12].

---

###### 3. Reporting Errors: Protocol-Level Error vs. Tool Execution Error (`isError`)

The book highlights a crucial architectural distinction between **protocol-level errors** and **tool execution errors** [13, 14]:

```
                     ┌──────────────────────────────────────────────┐
                     │              Incoming Request                │
                     └──────────────────────┬───────────────────────┘
                                            │
                           Did protocol or transport fail?
                                  /                   \
                            YES  /                     \ NO
                                v                       v
            ┌───────────────────────────────┐       ┌───────────────────────────────┐
            │     Protocol-Level Error      │       │     Tool Call Executed        │
            │  Response has "error" field   │       │   Response has "result" field │
            │  (Method missing, invalid JSON│       └───────────────┬───────────────┘
            │   or McpException thrown)     │                       │
            └───────────────────────────────┘             Did tool validation fail?
                                                                 /             \
                                                           YES  /               \ NO
                                                               v                 v
                                              ┌───────────────────┐   ┌───────────────────┐
                                              │  Tool Execution   │   │  Tool Success     │
                                              │      Error        │   │  isError: false   │
                                              │   isError: true   │   └───────────────────┘
                                              └───────────────────┘
```

###### **A. Protocol-Level Error**
* **Mechanism**: The JSON-RPC response contains an `"error"` field instead of a `"result"` field [8, 9].
* **When it occurs**: Something fails at the infrastructure, transport, or protocol layer—such as invalid JSON, missing methods, communication timeouts, or unhandled server exceptions [14, 15].
* **C# SDK Behavior**: 
  * If a tool throws an `McpException`, the C# SDK forwards the exception message verbatim in the JSON-RPC error response [15].
  * If a tool throws any generic exception (e.g., `InvalidOperationException`), the SDK returns a generic error message (e.g., *"An error occurred invoking 'tool_name'"*) to prevent leaking internal stack traces or connection strings [15, 16].

---

###### **B. Tool Execution Error (`isError` in Result)**
* **Mechanism**: The JSON-RPC response is technically a **successful JSON-RPC response** containing a `"result"` field [13, 14, 17]. However, inside the `result` object (a `CallToolResult`), the boolean property **`"isError"` is set to `true`** [13, 17].
* **When it occurs**: The protocol and transport succeeded (the tool method was found and executed), but the tool itself encountered a domain or validation error (e.g., dividing by zero, missing required fields, or failing input checks) [13, 17, 18].
* **Why this distinction exists**: AI models read human-readable content blocks inside `CallToolResult` [19, 20]. When `isError` is `true`, the AI model receives an actionable message (e.g., *"Cannot divide by zero. Please provide a non-zero divisor."*) and can self-correct by re-invoking the tool with valid arguments [18, 19].

---

💡 **Suggested Follow-Ups:**
1. Would you like to explore how to implement progress notifications (`IProgress<ProgressNotificationValue>`) or cancellation tokens in C# tools [21, 22]?
2. Should we review how the C# SDK handles dependency injection and logging redirection to `stderr` [23, 24]?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | The Server 12vi The JSON-RPC 2 0 Foundation 14 Requests 14 Responses 15 |
| [2] | develop and test in isolation. You do not need a full AI application running just to verify that your server returns the right data. Figure 2-1. The three-layer MCP architecture showing how the host contains one or more clients, each connec… |
| [3] | remote procedure call protocols ever designed, and that simplicity is exactly why the MCP designers chose it. Unlike REST, which is built around resources and HTTP verbs, JSON-RPC is built around method calls. You send a JSON object that sa… |
| [4] | { "jsonrpc": "2.0", "method": "notifications/initialized" } Table 2-1 summarizes the three message types and their key characteristics. Table 2-1. JSON-RPC 2.0 message types used in MCP Message Type Has id? Expects Response? Direction reque… |
| [5] | Let us look at each one. Requests A request is a message that expects a response. It contains four fields: the JSON-RPC version string (always “2.0”, at least for now), an id that the sender chooses, the method name, and an optional params … |
| [6] | before any damage can be done. Note at the time of writing, the latest stable specification revision is 2025-11-25. the protocol version negotiated during the handshake ensures that clients and servers can evolve independently while remaini… |
| [7] | the result as a JSON-RPC response. The client hands the result back to Claude Desktop, which feeds it into the language model. The model uses the weather data to compose a natural-language answer: “It is currently 58°F and cloudy in Seattle… |
| [8] | tools that are available. Listing 2-1. A JSON-RPC request message calling the tools/list method { "jsonrpc": "2.0", "id": 1, "method": "tools/list", "params": {} } Chapter 2 MCp arChiteCture under the hood15 Responses A response is the answ… |
| [9] | responses in code: check for an error first, and if there is none, process the result. In Listing 2-2, you can see the response to the request in Listing 2-1, with a list of all the available tools, in this case only one; get the current we… |
| [10] | Chapter 2 MCp arChiteCture under the hood16 Notifications Notifications are fire-and-forget messages. They look like requests but they have no id field, which signals that the sender does not expect a response. MCP uses notifications for ev… |
| [11] | changed,” and the client can update accordingly. The client might notify the server that the root list has been modified. These are one-way messages with no response expected, which keeps them lightweight and fast. The MCP specification def… |
| [12] | prompt templates. The server can send notifications to inform the client about changes, like a new tool becoming available or an existing resource being updated. Both sides can send requests and responses freely, and multiple requests can b… |
| [13] | SOAP (Simple Object Access Protocol) service or a shiny new gRPC (Google Remote Procedure Call) microservice, the client sees the same clean interface. Figure 2-1 illustrates how these layers relate. Notice that the host sits at the top, cl… |
| [14] | Listing 10-12 below. Listing 10-12. Checking for tool-reported errors var result = await client.CallToolAsync("get_pizza_price", new Dictionary<string, object?> { ["pizzaName"] = "Unknown" }); if (result.IsError) { Console.WriteLine("The to… |
| [15] | true. However, the text the client sees depends on the exception type. If your exception derives from McpException, its message is included verbatim in the error response. For all other exception types (such as ArgumentException or InvalidO… |
| [16] | That means it should be clear, specific, and actionable. Instead of “invalid input”, say, “The email address must contain an @ symbol and a dot.” Instead of “something went wrong”, say, “Could not connect to the database at localhost:5432. … |
| [17] | return new CallToolResult { Content = [ new TextContentBlock Chapter 5 Creating tools70 { Text = json, } ], IsError = false }; } The Content property is an IList<ContentBlock> where each item is a subclass like TextContentBlock, ImageConten… |
| [18] | McpException. Have a look at an example of this in Listing 5-8 below. Listing 5-8. Throwing an exception for invalid input [McpServerTool(Name = "divide")] [Description("Divides two numbers safely.")] public static string Divide( [Descripti… |
| [19] | verbatim to the client. The AI model sees the error message and can self-correct by asking the user for different input. Returning Error Responses Explicitly For more control, you can return a CallToolResult with IsError set to true. This l… |
| [20] | Both approaches, throwing exceptions and returning error responses, ultimately produce the same result on the wire: a CallToolResult with IsError true. Remember that only McpException messages are forwarded to the client; other exception ty… |
| [21] | the server. If you have ever worked with layered architectures, where your UI talks to a service layer that talks to a data layer, then you already have a good intuition for this pattern. Your application (the host) does not talk directly t… |
| [22] | opening the transport, performing the initialization handshake, sending requests, and routing responses back to the host. If the host is a restaurant manager, the client is the waiter who takes orders to the kitchen and brings dishes back t… |
| [23] | McpServerToolType attribute (that you can see a little below that) and registers each method marked with McpServerTool as a callable tool. After the configuration is done, Build and RunAsync start the host and keep it alive until it receive… |
| [24] | 49 "mcpServers": { "FirstMcpServer": { "command": "dotnet", "args": [ "run", "--project", "/full/path/to/FirstMcpServer" ] } } } Replace the path in the args array with the actual path to your project folder. The command tells Claude Deskto… |


---

## Q03 — Connection lifecycle + capability negotiation

**Question:** Describe the MCP connection lifecycle as explained in the book: the initialize request, capability negotiation (server capabilities vs client capabilities), the initialized notification, and shutdown. What protocol version and which capabilities does the C# SDK declare by default?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 36

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, the Model Context Protocol (MCP) connection lifecycle is structured around a four-phase protocol lifecycle and an initialization handshake [1, 2].

---

###### 1. The Four-Phase Protocol Lifecycle

Every MCP connection flows sequentially through four distinct phases [2, 3]:

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│   Phase 1:      │  ──►  │   Phase 2:      │  ──►  │   Phase 3:      │  ──►  │   Phase 4:      │
│   Connection    │       │ Initialization  │       │    Operation    │       │    Shutdown     │
│ (Transport Open)│       │ (Handshake Sync)│       │ (Requests/Events)│      │(Graceful Exit)  │
└─────────────────┘       └─────────────────┘       └─────────────────┘       └─────────────────┘
```

###### **Phase 1: Connection**
The transport layer establishes a raw communication channel between client and server [4]. For standard I/O (`stdio`), the host launches the server child process and connects to its standard input (`stdin`) and output (`stdout`) streams [4]. For network transports like Streamable HTTP, an HTTP connection endpoint is opened [4]. At this point, no MCP protocol messages have been exchanged yet [4].

###### **Phase 2: Initialization**
The client and server perform capability negotiation to agree on protocol version and supported features [5, 6]. The protocol specification strictly forbids sending operational requests (such as `tools/list`) before initialization completes; any premature operational calls must be rejected by the server [6, 7].

###### **Phase 3: Operation**
Once initialized, the connection enters active working state [7, 8]. The client can freely invoke tools, read resources, and fetch prompt templates, while the server can issue notifications (such as progress updates or resource change notifications) [8]. Multiple requests can be in flight simultaneously [8]. This phase lasts for the lifetime of the host process (for `stdio`) or session duration (for HTTP) [8].

###### **Phase 4: Shutdown**
When the host finishes using a server, the client initiates a graceful teardown [9]. For `stdio`, the client closes the server's input stream; for HTTP, the client disconnects [9]. There is no formal shutdown message in the JSON-RPC protocol [9]; servers are expected to complete in-flight work, release resources, and terminate cleanly [9].

---

###### 2. The Initialization Handshake & Capability Negotiation

Capability negotiation prevents race conditions and mismatched feature execution [5, 7]. It follows a two-step handshake [5]:

1. **`initialize` Request (Client ➔ Server)**:  
   The client sends a JSON-RPC request containing [10, 11]:
   * `protocolVersion`: The date-based version string it supports (e.g., `"2025-03-26"`) [10, 11].
   * `capabilities`: Declared client features (such as `roots`, `sampling`, or `elicitation`) [11-13].
   * `clientInfo`: Metadata including the client's `name` and `version` [11].

2. **`initialize` Response (Server ➔ Client)**:  
   If versions match, the server responds with a JSON-RPC result containing [5, 14]:
   * `protocolVersion`: The agreed protocol version [14, 15].
   * `capabilities`: Declared server primitives (e.g., `tools`, `resources`, `prompts`), including optional flags like `listChanged: true` or `subscribe: true` [14, 15].
   * `serverInfo`: Server name and version metadata [14].

If the protocol versions are incompatible, the initialization fails cleanly and the connection closes immediately [5, 15].

---

###### 3. The `initialized` Notification

After receiving a successful `initialize` response from the server, the client sends a `notifications/initialized` notification (a fire-and-forget JSON-RPC message without an `id` field) [5, 16]. This confirms to the server that negotiation is complete and transitions the connection into **Phase 3: Operation** [5, 7].

---

###### 4. Protocol Version and Default Capabilities in the C# SDK

* **Protocol Version**:  
  The protocol uses date-based versioning (such as `"2025-03-26"` or `"2025-11-25"`) [10, 17]. The C# SDK handles protocol version negotiation automatically behind the scenes, using progressive transport fallback to stay compatible across SDK updates [17-19].

* **Declared Capabilities (Server-Side)**:  
  In the C# SDK, server capabilities are **dynamically populated** on `AddMcpServer()` based on which extension methods are registered on the builder [18, 20-22]:
  * Calling `.WithToolsFromAssembly()` or `.WithTools<T>()` advertises `tools: { listChanged: true }` [18, 23, 24].
  * Calling `.WithResourcesFromAssembly()` or `.WithResources<T>()` advertises `resources: { subscribe: true, listChanged: true }` [21, 25, 26].
  * Calling `.WithPromptsFromAssembly()` or `.WithPrompts<T>()` advertises `prompts: { listChanged: true }` [22].

* **Declared Capabilities (Client-Side)**:  
  When using `McpClient.CreateAsync`, capabilities can be passed via `McpClientOptions.Capabilities` [27, 28]. By default, client info (`ClientInfo`) uses runtime process defaults if omitted [29], and features like `sampling` or `elicitation` are advertised when corresponding handlers are registered [12, 28].

---

💡 **Would you like to explore how to handle long-running request sessions or progress reporting notifications within C# tools?**

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Notifications 16 Capability Negotiation 16 The Initialization Handshake 17 The Protocol Lifecycle 19 |
| [2] | Phase 1: Connection 19 Phase 2: Initialization 19 Phase 3: Operation 20 Phase 4: Shutdown 20 |
| [3] | dangling after a shutdown. Tip When developing your own MCp servers, always test the shutdown path. it is easy to focus on the happy path during development and forget that your server needs to clean up after itself. a simple integration te… |
| [4] | The Protocol Lifecycle Every MCP connection follows a well-defined lifecycle with four phases: connection, initialization, operation, and shutdown. Understanding this lifecycle will save you hours of debugging when you start building your o… |
| [5] | exactly what topics are on the table. MCP does the same thing, just with JSON instead of small talk. The Initialization Handshake The handshake is a two-step dance. First, the client sends an initialize request that advertises the protocol … |
| [6] | and connecting to its standard input and output streams. For an HTTP-based transport, it means opening a network connection to the server’s endpoint. At this point, no MCP messages have been exchanged yet. The two sides can send bytes to ea… |
| [7] | protocol specification is strict about this: if a server receives a tools/list request before Chapter 2 MCp arChiteCture under the hood20 initialization is complete, it should reject it. This rule prevents race conditions and ensures both s… |
| [8] | prompt templates. The server can send notifications to inform the client about changes, like a new tool becoming available or an existing resource being updated. Both sides can send requests and responses freely, and multiple requests can b… |
| [9] | dictate how long the operation phase should last. It simply provides the rules for how messages flow while it is active. Phase 4: Shutdown When the host decides it is done with a server, the client initiates a graceful shutdown. For stdio s… |
| [10] | before any damage can be done. Note at the time of writing, the latest stable specification revision is 2025-11-25. the protocol version negotiated during the handshake ensures that clients and servers can evolve independently while remaini… |
| [11] | { "jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2025-03-26", "capabilities": { "roots": { "listChanged": true }, Chapter 2 MCp arChiteCture under the hood18 "sampling": {} }, "clientInfo": { "name": "MyA… |
| [12] | provide an ElicitationHandler on the Handlers property of McpClientOptions. The handler receives ElicitRequestParams and returns an ElicitResult. Listing 10-19 shows how to configure this. Chapter 10 Building an MCp Client188 Listing 10-19.… |
| [13] | var result = await _tools.GetProduct(99); Assert.AreEqual("Product not found.", result); } } Chapter 11 testing Your MCp servers201 This pattern scales to any dependency. Need to test a tool that calls an external API? Mock the HttpClient. … |
| [14] | Listing 2-5. The server responds with its own capabilities { "jsonrpc": "2.0", "id": 1, "result": { "protocolVersion": "2025-03-26", "capabilities": { "tools": { "listChanged": true }, "resources": { "subscribe": true, "listChanged": true }… |
| [15] | Chapter 2 MCp arChiteCture under the hood19 Notice the structure of the capabilities objects. The client in Listing 2-4 is telling the server that it supports the roots feature with change notifications and that it can handle sampling reque… |
| [16] | Chapter 2 MCp arChiteCture under the hood16 Notifications Notifications are fire-and-forget messages. They look like requests but they have no id field, which signals that the sender does not expect a response. MCP uses notifications for ev… |
| [17] | are going to build some of the most impressive MCP servers out there. All the source code from this book is available in the accompanying GitHub repository. Use it as a starting point, experiment with the examples, and adapt them to your ne… |
| [18] | namespace has the attributes McpServerToolType and McpServerTool. And System. ComponentModel gives us the Description attribute, which the SDK reads to generate human-readable descriptions for AI clients. The builder setup is three lines of… |
| [19] | it negotiates the protocol version behind the scenes. Tool Discovery After initialization, the client sends a “tools/list” request. Your server responds with a JSON array describing every tool it offers. For our echo tool, the response incl… |
| [20] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [21] | With all three resource classes in place, the Program.cs file is delightfully simple, as seen in Listing 6-5 underneath. Listing 6-5. Program.cs wiring up all resources using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensi… |
| [22] | Now let’s set up the server in Program.cs. The only difference from our tool servers is that instead of calling WithToolsFromAssembly, we call WithPromptsFromAssembly. This tells the SDK to scan the assembly for classes decorated with the p… |
| [23] | handshake. The client sends an “initialize” request that includes its own capabilities and the protocol version it supports. Your server responds with its capabilities, which include things like “I support tools” or “I support resources.” T… |
| [24] | notice how each tool class is registered separately. Chapter 5 Creating tools83 Listing 5-10. Manual tool registration with WithTools<T>() using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Hosting; using ModelContex… |
| [25] | builder.Services .AddMcpServer() .WithStdioServerTransport() .WithResourcesFromAssembly() .WithSubscribeToResourcesHandler(async (ctx, ct) => { var uri = ctx.Params?.Uri; if (uri is not null) { // Track this subscription subscriptions.TryAd… |
| [26] | WithToolsFromAssembly did in the previous chapter. It scans the current assembly for all classes marked with McpServerResourceType, finds every method decorated with McpServerResource, and registers them all in one sweep. No need to registe… |
| [27] | The CreateAsync method accepts a few optional parameters beyond the transport. You can pass McpClientOptions to configure client capabilities and protocol settings, and an ILoggerFactory for diagnostics. Here is a more complete example. Lis… |
| [28] | do this by declaring the sampling capability and providing a SamplingHandler on the Handlers property of McpClientOptions. If you're already using an IChatClient from Microsoft.Extensions.AI, you can call CreateSamplingHandler() on it to ge… |
| [29] | Chapter 10 Building an MCp Client174 Name = "My Client", Version = "1.0.0" } }); The ClientInfo property lets you identify your client to the server. This information is sent during the initialization handshake and can be useful for server-… |


---

## Q04 — Tools trong C# SDK

**Question:** How does the book define and register Tools in the C# SDK? Cover the [McpServerToolType] and [McpServerTool] attributes, the [Description] attribute on methods and parameters, WithToolsFromAssembly versus WithTools<T>, how the input JSON schema is generated from method parameters, allowed return types, async tools, and CancellationToken parameters.

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 37

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, tool definition and registration in the C# SDK are built around familiar .NET attribute-driven patterns and Microsoft.Extensions.Hosting conventions [1, 2].

---

###### 1. Class and Method Attributes: `[McpServerToolType]` and `[McpServerTool]`

* **`[McpServerToolType]`**:
  * Applied at the class level to mark a class as containing one or more MCP tool methods [3-5].
  * Without this marker, the SDK’s assembly scanner completely ignores the class [5].
  * While the class can be static or an instance class, declaring static classes is the simplest approach for tools without instance state [5-7].

* **`[McpServerTool]`**:
  * Applied to individual methods inside a marked class to expose them as executable tools [3, 4, 7].
  * Key properties include:
    * `Name`: Explicitly sets the tool's public name in the protocol (e.g., `[McpServerTool(Name = "greet")]`) [7]. If omitted, the SDK defaults to the method name [7].
    * `ReadOnly`: A boolean flag (defaults to `false`). Setting `ReadOnly = true` signals to clients that the tool only reads data without side effects, letting AI models invoke it without requiring explicit user confirmation [8, 9].
    * `Title`: Provides a human-friendly display title separate from the machine-readable tool `Name` [10, 11].
    * `IconSource`: Accepts an HTTP/HTTPS URL or base64 `data:` URI to display a visual icon alongside the tool in compatible client UIs [11].
    * `TaskSupport`: Configures whether a tool execution can run as an asynchronous MCP task (`Forbidden`, `Optional`, or `Required`) [12, 13].

---

###### 2. Describing Tools and Parameters: `[Description]`

* The SDK uses the standard **`System.ComponentModel.Description`** attribute [14, 15].
* **Method-level `[Description]`**: Provides a human-readable summary of what the tool accomplishes [8, 16]. Because large language models read these descriptions to decide *when* and *why* to invoke a tool, clear descriptions act directly as instructions for the AI [8, 16, 17].
* **Parameter-level `[Description]`**: Explains the purpose of specific arguments [16, 18]. The SDK reads these parameter descriptions during reflection and embeds them directly into the generated JSON Schema [16-18].

---

###### 3. Tool Registration: `WithToolsFromAssembly` vs. `WithTools<T>`

Tool registration occurs on `builder.Services.AddMcpServer()` during application host setup [19, 20]:

* **`WithToolsFromAssembly()`**:
  * Scans an assembly (by default, the executing assembly) via reflection [20-22].
  * Automatically discovers all classes decorated with `[McpServerToolType]` and registers all methods marked with `[McpServerTool]` [15, 20].
  * Ideal for standard applications where tools are distributed across multiple files or folders [2, 22].

* **`WithTools<T>()`**:
  * Performs explicit, manual registration for a single tool class `T` [20, 23].
  * Gives fine-grained control over which tool classes are exposed (useful for hiding unfinished features, implementing plugin models, or optimizing context-window overhead in large servers) [20, 24, 25].
  * Assembly scanning and manual registration can also be mixed in the same builder chain [25].

---

###### 4. JSON Schema Generation from Method Parameters

The SDK inspects tool method signatures at startup via reflection to construct the tool's `inputSchema` automatically [18, 26]:

* **Type Mapping**:
  * `.NET string` ➔ `"type": "string"` [26].
  * `.NET int`, `double`, `decimal` ➔ `"type": "integer"` or `"type": "number"` [26, 27].
  * `.NET bool` ➔ `"type": "boolean"`.
  * `.NET Enum` ➔ `"type": "string"` accompanied by an `"enum"` array listing all valid string choices [26, 28, 29].
* **Required vs. Optional Parameters**:
  * Parameters **without** default values are marked as required in the schema's `"required"` array [26, 29, 30].
  * Parameters **with** default values (e.g., `string toppings = "cheese"`) or nullable types (e.g., `string? notes = null`) are treated as optional and excluded from the `"required"` array [26, 28-30].
* **Special Framework Parameters**:
  * Types injected by the runtime—such as `CancellationToken`, `IProgress<ProgressNotificationValue>`, `ILogger<T>`, or custom Dependency Injection services—are recognized by the SDK and excluded from the public JSON Schema [11, 31-33].

---

###### 5. Allowed Return Types

Tool methods support three return patterns depending on the desired level of control [34, 35]:

1. **Plain `string`**:
   * The simplest and most common return type [36].
   * The SDK automatically wraps the string into a `TextContentBlock` inside a standard MCP `CallToolResult` response [36, 37].
2. **Complex Objects (`object`, anonymous types, records, classes)**:
   * The SDK automatically serializes complex objects to formatted JSON strings and wraps them in a text content block [38-40].
   * Ideal for structured data that AI models need to parse programmatically [38].
3. **`CallToolResult`**:
   * Provides complete manual control over the MCP protocol response [40, 41].
   * Allows setting custom error flags (`IsError = true`), returning multiple content blocks (`IList<ContentBlock>`), or sending mixed media (text, images, or embedded resource blocks) [41-43].

---

###### 6. Async Tools and `CancellationToken` Parameters

* **Async Support (`Task<T>`)**:
  * Tool methods can return `Task<string>`, `Task<object>`, or `Task<CallToolResult>` and be marked `async` [31, 44].
  * The SDK awaits the task during execution and serializes the resolved result identically to a synchronous return value [44].

* **`CancellationToken` Injection**:
  * If a tool method includes a `CancellationToken` parameter (e.g., `CancellationToken cancellationToken = default`), the SDK injects the token automatically [31].
  * The parameter does not require a `[Description]` attribute and is excluded from the JSON schema [31].
  * When an AI client cancels an in-flight tool call, the SDK triggers the cancellation token, allowing async operations (such as HTTP requests or database queries) to unwind cleanly [9, 31, 45].

---

💡 **Next Steps**: Would you like to cover progress notifications using `IProgress<ProgressNotificationValue>`, or dive into dependency injection and registering custom services into tools?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Version="10.0.3" /> <PackageReference Include="ModelContextProtocol" Version="1.3.0" /> </ItemGroup> </Project> Nothing exotic here. It is a standard console app with two NuGet references. The target framework is net10.0, which is required … |
| [2] | C# SDK. It feels natural if you have used ASP.NET controllers or Minimal API endpoints before. You focus on writing the tool logic, and the framework takes care of the wiring. Tip You can organize tools across multiple classes and even mult… |
| [3] | The McpServerToolType Attribute 63 The McpServerTool Attribute 64 Describing Parameters 66 How the SDK Generates JSON Schema 66 |
| [4] | that it is remarkably simple: a static class with a static method that takes a string and returns a string. There is no base class to inherit from, no interface to implement, and no complex registration ceremony. The attributes do all the h… |
| [5] | the moving parts. The McpServerToolType Attribute The first attribute you need is McpServerToolType. You place it on a class to tell the SDK that this class contains one or more tool methods. Without it, the SDK will simply ignore the class… |
| [6] | describes the tool’s input. When an AI model decides to call your tool, it will construct a JSON object that matches this schema, and the SDK handles deserializing it into the correct .NET types. The return type is a plain string. Under the… |
| [7] | makes the intent clear and prevents anyone from accidentally adding instance state. The McpServerTool Attribute Each method you want to expose as a tool gets the McpServerTool attribute. This attribute accepts several optional properties th… |
| [8] | you have full control over the public contract. Next is the Description property on the System.ComponentModel.Description attribute. You place this attribute on the method itself and it tells the AI model what the tool does. Write descripti… |
| [9] | the request at any point, the token fires and the operation unwinds cleanly. Also notice the ReadOnly = true on the attribute. This tool only reads data, so we tell clients that it is safe to call without user confirmation. Chapter 5 Creati… |
| [10] | described. Chapter 5 Creating tools65 Listing 5-1. The McpServerTool attribute used with its attributes, together with the Description attributes to describe methods and parameters [McpServerTool(Name = "greet", ReadOnly = true)] [Descripti… |
| [11] | If your tool is called get_server_metrics, you could set Title to “Server Metrics” so the client shows something prettier in its UI. IconSource is even more fun. You can point it to an SVG or PNG icon, and clients will display it alongside … |
| [12] | On the server side, you configure an IMcpTaskStore. The SDK ships with InMemoryMcpTaskStore out of the box, which accepts a range of options: defaultTtl, maxTtl, pollInterval, cleanupInterval, pageSize, maxTasks, and maxTasksPerSession. For… |
| [13] | means the tool never creates tasks, that’s the default for synchronous tools. Optional means the tool can create tasks if the client requests it, the default for async tools. And Required means the tool always creates a task. Set it right o… |
| [14] | Listing 4-2. The complete Program.cs with hosting and EchoTool using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Hosting; using ModelContextProtocol; using ModelContextProtocol.Server; using System.ComponentModel; v… |
| [15] | namespace has the attributes McpServerToolType and McpServerTool. And System. ComponentModel gives us the Description attribute, which the SDK reads to generate human-readable descriptions for AI clients. The builder setup is three lines of… |
| [16] | Chapter 4 Your First MCp server47 that should be exposed to AI clients. The Description attribute, which comes from the standard System.ComponentModel namespace, provides the human-readable text that AI clients display when they list availa… |
| [17] | Chapter 4 Your First MCp server57 } }, "required": [ "message" ] } } ] } } See how the Description attributes we wrote map directly to the description fields in the JSON? The inputSchema is a standard JSON Schema object that tells the AI mo… |
| [18] | Describing Parameters Every parameter on a tool method should have a Description attribute of its own. The SDK uses these descriptions when it builds the JSON schema for the tool. That schema is what the AI model reads to understand which a… |
| [19] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [20] | McpServerToolType and registers every tool it finds. For most projects, this is all you need. builder.Services .AddMcpServer() .WithStdioServerTransport() .WithToolsFromAssembly(); But sometimes you want more control. Maybe you have a class… |
| [21] | desktop AI clients like Claude Desktop and GitHub Copilot in VS Code expect. Finally, WithToolsFromAssembly scans the current assembly for any classes decorated with the right attributes and registers them as tools automatically. Here is th… |
| [22] | BasicTools.cs, the pizza ordering in PizzaOrderTool.cs, async tools in AsyncTools.cs, and so on. Each file is focused and easy to navigate. A natural pattern is to create a Tools folder in your project and put one class per file inside it. … |
| [23] | notice how each tool class is registered separately. Chapter 5 Creating tools83 Listing 5-10. Manual tool registration with WithTools<T>() using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Hosting; using ModelContex… |
| [24] | await builder.Build().RunAsync(); With manual registration, only the classes you explicitly list will have their tools exposed. This gives you fine-grained control at the cost of having to remember to update Program.cs every time you add a … |
| [25] | decide which tool to call. If you register dozens of tools the model does not need, you are burning context window tokens and making it harder for the model to pick the right one. For a small server with a handful of tools this does not mat… |
| [26] | reads the parameter names, their .NET types, any default values, and the Description attributes you added. From all of that, it generates a JSON schema object that it sends to the client during tool discovery. Chapter 5 Creating tools67 For… |
| [27] | Simple Types String, int, double, bool, DateTime: all the types you reach for everyday work exactly as you would expect. The SDK maps them to their JSON schema equivalents and makes sure that the values the client sends are deserialized cor… |
| [28] | Pretty straightforward, right? But real-world tools usually need more flexibility than just required strings and numbers. What about parameters that are optional, or values that must come from a fixed set of choices? That is where enums and… |
| [29] | because they have defaults. The two enum parameters, PizzaSize and CrustType, will show up in the JSON schema with an enum array listing every valid value, so the AI model knows exactly what strings to pass. Notice the nullable string? deli… |
| [30] | ToString("HH:mm") }; Chapter 5 Creating tools73 var json = JsonSerializer.Serialize( order, new JsonSerializerOptions { WriteIndented = true }); return $"Order confirmed!\n{json}"; } } There is a lot going on in this listing, so let me walk… |
| [31] | Task<CallToolResult>. CancellationToken Support When a client sends a cancellation request for a long-running tool, the MCP server needs a way to propagate that cancellation to your code. The SDK handles this by injecting a CancellationToke… |
| [32] | special parameter and does not include it in the JSON schema. When the tool is invoked, the SDK injects an IProgress implementation that sends MCP progress notifications back to the client. The ProgressNotificationValue type has three prope… |
| [33] | Injecting Services into Tools In earlier chapters, you saw that tool methods accept parameters that map to the arguments an AI model sends along with a tool call. A string parameter called city becomes a required input that the model fills … |
| [34] | rarely need to think about JSON schema directly; just write idiomatic C# and the SDK does the translation for you. Tip if you want to see the exact Json schema the sDK generates, send a tools/list request to your server. the response includ… |
| [35] | there when you need it. So to recap: Start with a string return when your output is simple text. Graduate to an object return when you want automatic JSON serialization. And reach for CallToolResult when you need complete control over error… |
| [36] | control you need over the response. The good news is that the SDK makes all three feel natural, so you can start simple and upgrade when you need to. Returning Strings The simplest return type is a plain string. The SDK wraps it in a text c… |
| [37] | Listing 5-2 to see how. Listing 5-2. A tool that returns a simple string [McpServerTool(Name = "greet")] [Description("Greets the user by name.")] public static string Greet [Description("The person's name")] string name) { return $"Hello, … |
| [38] | For simple tools, a plain string works great. But what happens when you need to return structured data that the AI model can parse and reason about programmatically? That is where things get interesting. Returning Complex Objects If you ret… |
| [39] | Listing 5-3. Returning a complex object (auto-serialized to JSON) [McpServerTool(Name = "get_system_info")] [Description("Returns system information.")] public static object GetSystemInfo() { return new { MachineName = Environment.MachineNa… |
| [40] | formatted JSON string with all of the properties you included. Anonymous types work great here, but you can also return a named class or record if you prefer. Both strings and objects cover the vast majority of use cases. But sometimes you … |
| [41] | the scenes. That is the MCP protocol’s response object for any tool call. Most of the time, you never see it because the SDK creates it for you. But you can return one yourself when you need control that a simple string or object cannot giv… |
| [42] | section later in this chapter; for now, let us focus on the happy path. Just for reference, in Listing 5-4, you can see how you would build and return such a result yourself. Listing 5-4. Returning a CallToolResult for full control [McpServ… |
| [43] | return new CallToolResult { Content = [ new TextContentBlock Chapter 5 Creating tools70 { Text = json, } ], IsError = false }; } The Content property is an IList<ContentBlock> where each item is a subclass like TextContentBlock, ImageConten… |
| [44] | Async Tools Most real-world tools need to talk to something external: a database, a REST API, a file system, or a cloud service. In .NET, we handle that with async and await, and the MCP SDK supports this pattern out of the box. Chapter 5 C… |
| [45] | Tip You might notice we’re creating an HttpClient directly here. For a quick demo, that’s fine, but in production, you should use IHttpClientFactory through dependency injection. We’ll cover that pattern in Chapter 9. Creating HttpClient in… |


---

## Q05 — Resources + resource templates

**Question:** How does the book explain MCP Resources and resource templates? How are they declared in the C# SDK, what URI scheme conventions are used, what MIME types and content shapes are returned, and how does a server signal resource list changes or support subscriptions?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 38

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, **Resources** are defined as read-only pieces of data that an MCP server exposes to provide background context to AI models [1-3]. While **Tools** represent actions/verbs that can alter state, **Resources** represent data/nouns that can be read safely without side effects [3, 4].

---

###### 1. Static Resources vs. Resource Templates

The book divides resources into two primary forms based on how clients discover and access them [3, 5, 6]:

* **Static Resources**:
  * Represent fixed URIs that never change (e.g., `cosmic-pizza://menu` or `cosmic-pizza://hours`) [7-9].
  * Clients discover them by calling `resources/list`, which returns a catalog containing each resource's URI, name, description, and MIME type [6].
* **Resource Templates**:
  * Represent parameterized URIs configured using **RFC 6570 URI templates** with placeholders in curly braces (e.g., `planets://info/{planetName}`) [5, 10-12].
  * Clients discover them by calling `resources/templates/list` [6, 11].
  * When a client issues a `resources/read` request for a resolved URI (e.g., `planets://info/jupiter`), the SDK automatically parses the placeholder values and passes them as method arguments to the resource handler [11, 13].

---

###### 2. Declaration in the C# SDK

Resource declaration follows an attribute-driven pattern similar to tools [5, 14]:

* **Class Attribute (`[McpServerResourceType]`)**: Applied at the class level to mark a static or instance class as containing resource methods [5, 14].
* **Method Attribute (`[McpServerResource]`)**: Applied to methods to expose them as MCP resources [5, 14, 15].
  * **`UriTemplate`**: Defines the URI or URI pattern (e.g., `UriTemplate = "cosmic-pizza://menu"` or `UriTemplate = "planets://info/{planetName}"`) [8, 11, 15]. If omitted, the SDK defaults to `resource://mcp/MethodName` [16].
  * **`Name`**: Human-readable label displayed in client resource catalogs [15, 17].
  * **`MimeType`**: Declares the content type (e.g., `text/plain`, `application/json`, `text/markdown`) [8, 15, 16].
  * **`Title` / `IconSource`**: Optional visual display properties for client UIs [18].
* **Description (`[Description]`)**: Applied to the method so the AI model understands what context the resource provides before choosing to read it [15, 19].
* **Registration**: Registered in `Program.cs` via assembly scanning (`.WithResourcesFromAssembly()`) or manually (`.WithResources<T>()`) [20-22].

---

###### 3. URI Scheme Conventions

MCP allows any valid URI, but the book outlines several common schemes and naming conventions [5, 23, 24]:

* **Custom Schemes** (`cosmic-pizza://menu`, `planets://info/{planetName}`): The most common pattern for domain-specific, server-managed resources [23, 25].
* **`file://`** (`file:///etc/config.json`): Used when a resource maps directly to a file on disk [25].
* **`https://`** (`https://api.example.com/data`): Used when proxying data from an external web API [25].
* **`resource://`** (`resource://mcp/...`): Generic fallback scheme used by the SDK when no `UriTemplate` is explicitly provided [16, 25].
* **Naming & Casing Rules**:
  * URIs should be lowercase and use forward slashes `/` for hierarchical structure [24].
  * Scheme and host portions are **case-insensitive**, but the path portion is **case-sensitive** [26].

---

###### 4. Returned MIME Types and Content Shapes

When a client calls `resources/read`, the method returns data formatted as text or binary [6, 27, 28]:

* **Plain `string`**: The simplest return type. The SDK automatically wraps the string into a `TextResourceContents` object with the configured URI and MIME type [27, 28].
* **`TextResourceContents`**: Used for explicit text content (e.g., JSON, plain text, or Markdown) via its `Text` property [27, 28].
* **`BlobResourceContents`**: Used for binary data (such as images or PDFs) by supplying a byte array to its `Blob` property [27, 28].
* **`IEnumerable<ResourceContents>`**: Used when a single resource read needs to return multiple content blocks (e.g., a text summary combined with a binary attachment) [29].
* **Async Support**: Methods can return `Task<string>` or `Task<ResourceContents>` and accept a `CancellationToken` [29].

---

###### 5. Signaling Resource List Changes and Subscriptions

To handle dynamic data, MCP provides capability flags and a subscription pipeline [20, 30-32]:

* **Capability Advertising**: During initialization, the server advertises capabilities such as `resources: { subscribe: true, listChanged: true }` [20, 30].
* **List Changed Notifications**: If the catalog of resources expands or shrinks, the server sends a `notifications/resources/list_changed` notification to inform the client [33].
* **Subscriptions (`resources/subscribe`)**:
  * A client sends a `resources/subscribe` request containing the specific resource URI it wants to watch [31, 34].
  * The server handles this by registering subscription handlers during builder setup [32, 34]:
    * `.WithSubscribeToResourcesHandler(async (ctx, ct) => { ... })` [34]
    * `.WithUnsubscribeFromResourcesHandler(async (ctx, ct) => { ... })` [34]
* **Resource Update Notifications**: When underlying data shifts, the server sends a `notifications/resources/updated` notification containing the resource URI [32].
  * **Key Trait**: The notification **does not contain the updated payload** itself; it merely alerts the client that data has changed so the client can issue a fresh `resources/read` request if needed [35].

---

💡 **Next Step:** Would you like to explore how **Prompts** are declared and parameterized in the C# SDK, or examine how to integrate **Dependency Injection** (`DbContext`, `HttpClientFactory`) into tools and resources?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | What MCP Provides 6 Tools 6 Resources 6 Prompts 7 |
| [2] | Summary 86 Table of ConTenTsix Chapter 6: Working with Resources 87 What Are Resources? 87 Creating Static Resources 88 |
| [3] | You will also learn how clients can subscribe to resource changes so they stay up to date when the underlying data shifts. What Are Resources? An MCP resource is a piece of data that a server makes available for reading. It could be a text … |
| [4] | model a cleaner separation of concerns. When an AI sees a list of available resources, it understands that these are background context it can pull in at any time without side effects. That makes the model more confident about reading a res… |
| [5] | The McpServerResourceType Attribute 88 Resource Templates 90 Resource URIs 93 Common URI Schemes 93 |
| [6] | endpoint to get back a catalog of every direct resource the server offers. Each entry in that catalog contains a URI, a human-readable name, and an optional description and MIME type. Alternatively, clients can call resources/templates/list… |
| [7] | Getting the resource URIs right took me a few attempts. My first version had spaces in the URI paths, which is technically valid but made me feel unclean. Creating Static Resources A static resource has a fixed URI that never changes. Every… |
| [8] | discover as resources. The class can be static or an instance class. If it is an instance class, the SDK will construct a new instance for each resource invocation, which is handy when your class implements IDisposable or needs per-request … |
| [9] | [McpServerResource(UriTemplate = "cosmic-pizza://hours", Name = "Opening Hours", MimeType = "text/plain")] [Description("Cosmic Pizza opening hours for all locations.")] public static string GetOpeningHours() { return """ Cosmic Pizza Openi… |
| [10] | is all you need. Resource Templates Static resources are great when you have a handful of fixed documents to expose. But what if you have hundreds of items, or the exact set of resources is not known ahead of time? That is where resource te… |
| [11] | {planetName}. When a client wants to read the resource, it replaces {planetName} with an actual value such as “mars” and sends the fully resolved URI to the server. Chapter 6 Working With resourCes91 In the SDK, the only difference between … |
| [12] | Sun.", 4879, 0.39, 0), ["earth"] = new("Earth", "Our home, the only world with life.", 12756, 1.0, 1), ["jupiter"] = new("Jupiter", "Largest planet with a Great Red Spot.", 142984, 5.20, 95), }; [McpServerResource( UriTemplate = "planets://… |
| [13] | the planet name from the URI and pass it to the planetName parameter. When a client sends a read request for planets://info/jupiter, the SDK matches the template, pulls out “jupiter”, and calls GetPlanetInfo with that value. You do not need… |
| [14] | McpServerResourceType, which you place on a class to tell the SDK that this class contains resource methods. It works exactly like McpServerToolType from the previous chapter. The second is McpServerResource, which you place on each method … |
| [15] | There is a lot to unpack in Listing 6-1, but most of it should feel familiar since you have written some tools before in Chapter 5. The McpServerResource attribute carries the heavy lifting. Its UriTemplate property tells the SDK which URI … |
| [16] | omit UriTemplate entirely, the SDK will generate a default URI based on the method name and any parameters, following the pattern resource://mcp/MethodName. I recommend always setting it explicitly so your URIs are meaningful and stable. Th… |
| [17] | We’ve seen most of these in the previous chapter, but here is a rundown of each one to explain when you should use it and how to really bolster this knowledge. The Name property is required and appears in the resource listing that clients r… |
| [18] | Finally, there is the Title property and the IconSource property. Title provides an additional display name that can be shown in a client’s UI. IconSource lets you specify a URL or data URI for an icon to represent your resource visually. T… |
| [19] | If your resource returns Markdown, for instance, a smart client might render it with formatting instead of showing raw text. The Description attribute on the method is not part of McpServerResource itself, but it is just as important. This … |
| [20] | Naming Conventions 93 Resource Metadata 94 Return Types for Resources 95 Subscribing to Resource Changes 95 |
| [21] | With all three resource classes in place, the Program.cs file is delightfully simple, as seen in Listing 6-5 underneath. Listing 6-5. Program.cs wiring up all resources using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensi… |
| [22] | WithToolsFromAssembly did in the previous chapter. It scans the current assembly for all classes marked with McpServerResourceType, finds every method decorated with McpServerResource, and registers them all in one sweep. No need to registe… |
| [23] | more than you might think. The MCP specification does not mandate a particular scheme. You are free to use any valid URI, but some conventions have emerged that are worth following. To give you a taste: custom schemes like cosmic-pizza://me… |
| [24] | some SDK examples as resource://mcp/..., is a generic scheme for server-managed resources that do not map to an external location. Naming Conventions Good resource URIs are predictable and self-describing. A few guidelines that I have found… |
| [25] | or planets://. Custom schemes are great for domain-specific resources because they make the purpose of the URI obvious at a glance. When a client sees cosmic-pizza:// menu, there is no ambiguity about what kind of data to expect. The specif… |
| [26] | both humans and AI models to reason about. Chapter 6 Working With resourCes94 Note uri comparison in MCp is case-insensitive for the scheme and host portions, but case-sensitive for the path. the sDk handles the scheme comparison for you, b… |
| [27] | understand what the resource contains before it decides to read it. A good description saves the model from having to read every resource just to figure out which one is relevant. Notice that both methods return a plain string. The SDK wrap… |
| [28] | string. The SDK wraps it in a TextResourceContents object with the URI and MIME type you specified in the attribute. This is the approach I used in the Cosmic Pizza and planet examples. If you need more control, you can return a TextResourc… |
| [29] | non-text formats. You can also return a list of ResourceContents objects by using IEnumerable<ResourceContents> as your return type. This is useful when a single resource read should return multiple content blocks, like a text summary follo… |
| [30] | Listing 2-5. The server responds with its own capabilities { "jsonrpc": "2.0", "id": 1, "result": { "protocolVersion": "2025-03-26", "capabilities": { "tools": { "listChanged": true }, "resources": { "subscribe": true, "listChanged": true }… |
| [31] | Subscribing to Resource Changes Resources are read-only, but that does not mean they are frozen in time. The data behind a resource might change, like when a restaurant updates its menu or a server’s status shifts from healthy to degraded. … |
| [32] | subscription. Later, when the underlying data changes, the server sends a notifications/ resources/updated notification to all subscribed clients. The client can then decide to re-read the resource to get the latest data. Chapter 6 Working … |
| [33] | changed,” and the client can update accordingly. The client might notify the server that the root list has been modified. These are one-way messages with no response expected, which keeps them lightweight and fast. The MCP specification def… |
| [34] | builder.Services .AddMcpServer() .WithStdioServerTransport() .WithResourcesFromAssembly() .WithSubscribeToResourcesHandler(async (ctx, ct) => { var uri = ctx.Params?.Uri; if (uri is not null) { // Track this subscription subscriptions.TryAd… |
| [35] | The subscribe handler receives the URI the client wants to watch. You store that subscription however you like, for example, in a ConcurrentDictionary. When the data changes, you send a notification through the MCP server. The notification … |


---

## Q06 — Prompts

**Question:** How does the book explain MCP Prompts? How are they declared in the C# SDK, what do they return, how are prompt arguments defined, and when does the book say a server should expose a Prompt instead of a Tool?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 39

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, **Prompts** are introduced as the third core primitive of the Model Context Protocol alongside Tools and Resources [1-3].

---

###### 1. How the Book Explains MCP Prompts
* **Definition**: Prompts are predefined, reusable message templates exposed by an MCP server to guide how an AI model approaches a task before it starts working [3-5].
* **The Analogy**: If **Tools** are the server's hands (taking action) and **Resources** are its eyes (reading data), then **Prompts** are its voice [3].
* **Protocol Mechanism**: Like tools and resources, prompts are discoverable via the protocol (by calling `prompts/list`) [5, 6]. When selected by a user or client application, the server generates and returns structured chat messages that are injected directly into the conversation context [5, 7].

---

###### 2. Declaration in the C# SDK
Declaration follows an attribute-driven pattern similar to tools and resources [8, 9]:

* **Class Attribute (`[McpServerPromptType]`)**: Placed on a static or instance class to notify the SDK's assembly scanner that it contains prompt handlers [9, 10].
* **Method Attribute (`[McpServerPrompt]`)**: Applied to methods to expose them as MCP prompts [9].
  * Optional properties include `Name` (public identifier), `Title` (friendly display label), and `IconSource` (URL or data URI for icons) [11].
* **Description (`[Description("...")]`)**: Placed on prompt methods and parameters using `System.ComponentModel.Description` to explain to users and AI clients what the prompt generates [9, 12, 13].
* **Registration in `Program.cs`**:
  * **Assembly Scanning**: `builder.Services.AddMcpServer().WithPromptsFromAssembly()` [14, 15].
  * **Manual Registration**: `builder.Services.AddMcpServer().WithPrompts<T>()` [15].

---

###### 3. What Prompts Return
* **Return Types**: MCP prompts return instances of **`ChatMessage`** (for single-message prompts) or **`ChatMessage[]`** (for multi-message prompts) from the standard **`Microsoft.Extensions.AI`** namespace [16, 17].
* **Message Roles**: Returned messages specify explicit roles [17, 18]:
  * `ChatRole.System`: Sets up the model's persona, expertise, and instructions [18, 19].
  * `ChatRole.User`: Represents the simulated or actual user query/request [16-18].
  * `ChatRole.Assistant`: Provides example model responses to establish formatting or few-shot prompting patterns [18, 20, 21].
* **Async Support**: Prompt methods can be asynchronous, returning `Task<ChatMessage>` or `Task<ChatMessage[]>` if they need to read files, fetch database records, or query external APIs to construct their messages [21, 22].

---

###### 4. How Prompt Arguments Are Defined
* **Method Parameters**: Prompt parameters are defined directly as C# method arguments (e.g., `string language`, `string style`, `string code`) [19].
* **JSON Schema Generation**: The SDK reflects over these parameters and generates a JSON Schema so the AI client can render structured input forms to the user [23, 24].
* **Parameter Types**: While strings are most common, parameters can be integers, booleans, enums, or complex objects [24].
* **Interpolation**: The server method takes these parameter values and interpolates or constructs the final text inserted into `ChatMessage` instances [19, 24].

---

###### 5. When to Expose a Prompt Instead of a Tool
The book offers a clear rule of thumb [25]:
> **"If you want the AI model to do something, use a tool. If you want the AI model to think about something in a particular way, use a prompt."** [25]

* **Use a Tool when**: The operation produces side effects or executes actions in the real world (e.g., executing a code linter, sending an email, writing to a database, or querying an external API) [25-27].
* **Use a Prompt when**: You want to shape reasoning, establish an expert persona, or set up a multi-step conversation workflow without executing actions directly [25, 26, 28]:
  * *Example*: A **linter tool** runs a static analysis binary and returns errors [27]. A **code review prompt** sets up the model as an expert C# security reviewer and asks it to analyze the code using its own reasoning [27, 28].
  * *Example*: An **email tool** sends an email via an API [27]. A **translation prompt** instructs the model on tone, formality, and cultural context before drafting a message [27].

---

💡 **Next Step**: Would you like to explore how to combine tools, resources, and prompts together into a single server, or review how to test prompts using the MCP Inspector?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | What MCP Provides 6 Tools 6 Resources 6 Prompts 7 |
| [2] | was not just about function calling. MCP defines a complete protocol for three types of capabilities that a server can expose to an AI application: tools (actions the AI can take), resources (data the AI can read), and prompts (templates th… |
| [3] | Exposing Prompts So far in this book, we have built tools that let an AI model take actions and resources that let it read data. Those two primitives cover a lot of ground, but there is a third piece of the MCP puzzle that we have not touch… |
| [4] | provide structured instructions that the AI can follow. Think of them as recipes: a prompt might define a specific workflow, like “analyze this code and suggest improvements” or “translate this document while preserving technical terminolog… |
| [5] | actually are in the MCP world. A prompt is a predefined message template that your server exposes to the AI client. When a user or the AI model selects one of your prompts, the server returns one or more messages that get injected into the … |
| [6] | and parameterized. The AI client can call prompts/list to discover what your server offers, just like it calls resources/list for resources and tools/list for tools. The same discovery pattern you already know. The client shows the availabl… |
| [7] | ChatMessage. For more complex scenarios, you can return an array or make the method async, but for a simple case like this, a single return value does the job nicely. When an AI client connects to this server and lists the available prompts… |
| [8] | resources, and how to implement them in C# using the same attribute-driven style you are already used to. We will start simple with a single-message prompt, work our way up to parameterized and multi-message prompts, and finish with a cosmi… |
| [9] | Your First Prompt With the server plumbing in place, let’s write our first prompt. Just like tools use McpServerToolType and McpServerTool, prompts use McpServerPromptType and McpServerPrompt. The pattern is the same: you put the type attri… |
| [10] | return [system, user]; } Notice the use of Math.Clamp to ensure the count stays between 1 and 10. This is a small but important detail. Even though the description tells the user to pick a number between 1 and 10, there is nothing stopping … |
| [11] | Just like tools and resources, prompts also support the Title and IconSource properties on the McpServerPrompt attribute. Title gives your prompt a human-friendly display name, and IconSource points to an icon that clients can display next … |
| [12] | accept three parameters: the programming language, the review style, and the actual code to review. Depending on what the user passes in, the prompt will generate completely different instructions for the AI model. Have a look at Listing 7-… |
| [13] | good description tells the user what the prompt does, what kind of results to expect, and when they might want to use it. For parameters, the description should explain what values are valid. If a parameter accepts specific options like “th… |
| [14] | Now let’s set up the server in Program.cs. The only difference from our tool servers is that instead of calling WithToolsFromAssembly, we call WithPromptsFromAssembly. This tells the SDK to scan the assembly for classes decorated with the p… |
| [15] | var builder = Host.CreateEmptyApplicationBuilder(null); builder.Services .AddMcpServer() .WithStdioServerTransport() .WithPromptsFromAssembly(); await builder.Build().RunAsync(); Notice how clean this is. Five lines of real code and our ser… |
| [16] | A few things stand out here. First, the return type is ChatMessage, which comes from the Microsoft.Extensions.AI namespace. This is the same abstraction used throughout the .NET AI ecosystem; so if you have worked with Microsoft.Extensions.… |
| [17] | readability, and " + "maintainability."); var userMessage = new ChatMessage( ChatRole.User, "Please review the following " + $"{language} code:\n\n" + $"```{language}\n{code}\n```"); return [systemMessage, userMessage]; } } There is quite a… |
| [18] | ChAPter 7 exPOSIng PrOMPtS112 Multi-message Prompts You already saw a preview of multi-message prompts in the code review example, but let’s dig deeper into why this pattern matters and how to use it effectively. In the MCP protocol, a prom… |
| [19] | language " + "and style")] public static ChatMessage[] ReviewCode( [Description("The programming language e.g. csharp, python")] string language, [Description("Review style: thorough, quick, or security-focused")] string style, [Description… |
| [20] | human would say in the conversation. An assistant message represents a previous response from the model, which is useful when you want to set up a multi-turn conversation pattern. The beauty of returning multiple messages is that you can co… |
| [21] | coaching from the user. You could extend this pattern further by adding an Assistant message that shows an example test in the desired format. The model would then follow that format for the rest of its response. Multi-message prompts give … |
| [22] | return Task<ChatMessage[]> and use await inside. The SDK handles async methods transparently. For instance, imagine a prompt that reads a project’s coding standards from a file and includes them in the system message. You would mark the met… |
| [23] | actual code to review. I will talk more about multi-message prompts in the next section, but even here, you can see how powerful this pattern is. The three parameters each have their own Description attribute, which the AI client uses to pr… |
| [24] | What I find really cool about this approach is that the same prompt handles very different scenarios. Pass in “csharp” and “security-focused” and you get a security audit prompt. Pass in “python” and “quick” and you get a lightweight review… |
| [25] | and how to make them great. I have seen a few patterns emerge from working with MCP prompts, and I think they are worth sharing before we move on to the fun stuff. When to Use Prompts vs. Tools The simplest rule of thumb I can offer is this… |
| [26] | I find it helpful to think about the three MCP primitives in terms of what they do and who drives them. Tools are actions that the AI model calls to get work done: send an email, query a database, calculate a value. Resources are data sourc… |
| [27] | Analyzing code for security vulnerabilities: Use a prompt that sets up the model as a security expert and asks it to review code. Running a linter on code and returning results: Use a tool that executes the linter and returns the output. Tr… |
| [28] | to do anything. You are telling it how to think about the problem. That is a subtle but important distinction. A code review tool might run a linter and return warnings. A code review prompt, on the other hand, sets up the model with instru… |


---

## Q07 — Transport — stdio vs HTTP/SSE/streamable

**Question:** Which transports does the book cover (stdio, HTTP with SSE, streamable HTTP)? How is each configured in the C# SDK (for example WithStdioServerTransport, WithHttpTransport, MapMcp), what are the trade-offs, which one does the book recommend for a local desktop integration server, and what warnings does it give about writing to stdout or console when using stdio?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 40

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, transport layers are described as the plumbing that carries JSON-RPC messages between MCP clients and servers [1, 2]. The book emphasizes that your tool, resource, and prompt logic remains completely identical regardless of which transport is selected [3, 4].

---

###### 1. Transports Covered in the Book

1. **Standard I/O (`stdio`)**:
   * Communicates directly through the server process's standard input (`stdin`) and standard output (`stdout`) streams [2, 5, 6].
   * The host application launches the server as a child process, with no network sockets, ports, or TLS certificates involved [6, 7].
2. **Server-Sent Events (`SSE`)**:
   * Uses HTTP with a dual-channel setup: a long-lived HTTP GET connection for server-to-client streaming and separate HTTP POST requests for client-to-server messages [8-10].
   * The book notes that **SSE is deprecated** in the official MCP specification in favor of Streamable HTTP [11, 12].
3. **Streamable HTTP**:
   * The modern, recommended network transport for MCP [11, 13, 14].
   * Replaces dual-channel SSE with standard HTTP POST requests that can optionally stream responses or progress notifications using SSE on the same connection [11, 14, 15].

---

###### 2. Configuration in the C# SDK

###### **Standard I/O (`stdio`) Configuration**
To set up a `stdio` server, the SDK uses the base `ModelContextProtocol` package and integrates with .NET Generic Hosting [16-18]:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Server;

// Use CreateEmptyApplicationBuilder to prevent logging to stdout
var builder = Host.CreateEmptyApplicationBuilder(null);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

###### **Streamable HTTP Configuration**
To expose a server over HTTP, you reference the `ModelContextProtocol.AspNetCore` package and integrate with ASP.NET Core minimal APIs [19-21]:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.MaxIdleSessionCount = 100;
        // options.Stateless = true; // Optional stateless mode
    })
    .WithToolsFromAssembly();

var app = builder.Build();

// Register the MCP endpoint (defaults to the root path "/")
app.MapMcp(); 
// Or map to a custom route with auth: app.MapMcp("/api/v1/mcp").RequireAuthorization();

app.Run();
```

---

###### 3. Trade-offs Between Transports

| Feature / Trait | Standard I/O (`stdio`) | Server-Sent Events (`SSE`) | Streamable HTTP |
| :--- | :--- | :--- | :--- |
| **Communication** | `stdin` / `stdout` pipes [2, 22] | HTTP GET (SSE stream) + HTTP POST [9, 22] | HTTP POST requests + optional SSE stream [14, 22] |
| **Deployment** | Local only (same machine) [6, 22, 23] | Remote / Network [10, 22] | Remote / Network / Cloud [14, 22, 24] |
| **Connection Model** | 1:1 (Child process bound to host) [22, 25] | Many:1 (Persistent dual-channel) [22] | Many:1 (Short-lived HTTP requests or sessions) [15, 22, 24] |
| **Security** | Inherently secure (data never leaves the machine) [6, 26] | Requires HTTP auth / reverse proxy [27, 28] | Supports standard ASP.NET Core auth & middleware [27-29] |
| **Status** | Active (Standard for desktop tools) [6, 23, 30] | **Deprecated** in spec [11, 12] | **Recommended** for network/cloud [11, 13, 30] |

---

###### 4. Recommendation for Local Desktop Integration

For local desktop integrations (such as running tools inside **Claude Desktop**, **VS Code with GitHub Copilot**, or local developer utilities), the book strongly recommends **Standard I/O (`stdio`)** [6, 23, 30]. 

**Why?**
* It requires zero network configuration, open ports, firewall rules, or TLS certificates [6, 23].
* Desktop AI clients launch the server executable directly as a child process and manage its process lifecycle automatically [7, 31-33].

---

###### 5. Critical Warnings About `stdout` and Console Logging

The book gives an emphatic warning regarding `stdout` when building `stdio` servers:

* **The Rule**: `stdout` is **exclusively reserved** for JSON-RPC protocol messages [34-37].
* **The Danger**: Any stray output written to `stdout`—such as `Console.WriteLine("Server started!")` or default console loggers—will interleave plain text into the JSON-RPC message stream [34, 36, 38, 39]. This corrupts the protocol stream and causes the client connection to crash immediately [35, 36, 38, 39].
* **How to Handle Diagnostics Safely**:
  1. **Use `Host.CreateEmptyApplicationBuilder(null)`**: Standard `Host.CreateApplicationBuilder()` registers a default console logger that writes to `stdout` [38, 40]. The empty builder starts with a clean slate without default `stdout` logging [17, 38].
  2. **Redirect Console Logging to `stderr`**: In `appsettings.json`, set `"LogToStandardErrorThreshold": "Trace"` for the Console logging provider [39, 41]. The MCP specification permits writing diagnostic logs to standard error (`stderr`) [35-37, 41, 42].

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Testing the Cosmic Prompts 118 Summary 122 Chapter 8: Transport Layers 125 Understanding Transports 125 |
| [2] | same regardless of which transport you choose. Only the way those messages travel changes. By the end of this chapter, you will understand the three transport options the specification offers, know when to use each one, and have a working H… |
| [3] | Transport Layers Up to this point, every server we have built communicates through standard input and output. That approach works beautifully for desktop AI clients like Claude Desktop and GitHub Copilot in VS Code, but it is not the only g… |
| [4] | tools produce results, the SDK wraps them into JSON-RPC messages, and the transport is responsible for getting those messages to the client. On the flip side, when a client sends a request, the transport receives the raw bytes and hands the… |
| [5] | Transport Layer 21 Standard I/O (stdio) 21 Server-Sent Events (SSE) 22 Streamable HTTP 22 |
| [6] | Standard I/O (stdio) The stdio transport is the simplest option and the one you will use most often during development. The host launches the server as a child process and communicates by writing JSON-RPC messages to the server’s standard i… |
| [7] | Standard I/O is the transport we have been using in every chapter so far. It communicates by reading JSON-RPC messages from stdin and writing responses to stdout. The client launches your server as a child process and the two sides talk thr… |
| [8] | The downside is that stdio only works when the host can directly spawn the server process. If you need your server to run on a different machine or be shared by multiple clients, you will need a network transport. Server-Sent Events (SSE) T… |
| [9] | specification defines. If you have ever used SSE in a web application for real-time notifications or live feeds, the concept is exactly the same. The client opens a long-lived HTTP connection to the server, and the server pushes messages do… |
| [10] | wants to send a request to the server, it makes a POST request to a separate message endpoint. The server processes the request and sends the response back through the open SSE stream. This two-channel approach means the client and server c… |
| [11] | implementations. Note While SSe still works, at the time of writing, the MCp specification now recommends using the newer Streamable http transport for new deployments. SSe support may eventually be deprecated, so keep that in mind when cho… |
| [12] | The SSE transport was part of the MCP specification from early on, but it has since been superseded by the Streamable HTTP transport. The MCP specification now marks SSE as deprecated in favor of Streamable HTTP, and the C# SDK reflects thi… |
| [13] | context. But for new projects in C#, you should use Streamable HTTP instead. It offers the same remote connectivity benefits with a simpler architecture and better support for session management. Note the sse transport is deprecated in the … |
| [14] | server that needs to be accessible over a network. It replaces the dual-channel SSE approach with a cleaner design that uses standard HTTP POST requests. Responses can be returned as regular HTTP responses or streamed using SSE on the same … |
| [15] | produce progress updates or multiple messages, the server switches the response to an SSE stream and pushes messages as they become available. Chapter 8 transport Layers130 This design is elegant because the client does not need to maintain… |
| [16] | dotnet new console -n FirstMcpServer That gives us the familiar Hello World starter. Before we touch any code, we need the official MCP NuGet package. The C# SDK lives in a package called ModelContextProtocol. The C# SDK is available as a s… |
| [17] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [18] | Chapter 4 Your First MCp server46 public static string Echo([Description("Message to echo")] string message) { return $"Echo: {message}"; } } Take a look at the important pieces. The first couple of lines bring in the namespaces we need to … |
| [19] | application framework and want full control over the server lifecycle. Unless you have a specific reason to go minimal, I suggest starting with the main package instead. ModelContextProtocol.AspNetCore When you want to expose your MCP serve… |
| [20] | session ID to each client and track state across multiple requests. This is essential for production deployments where you need to manage resources, enforce rate limits, or clean up idle connections. The ASP.NET Core Integration The C# SDK … |
| [21] | extension methods. $ cd HttpTransportServer $ dotnet add package ModelContextProtocol $ dotnet add package ModelContextProtocol.AspNetCore The ModelContextProtocol.AspNetCore package depends on the base ModelContextProtocol package, so you … |
| [22] | to use. The decision usually comes down to two questions: where does the client run relative to the server, and do you need to support multiple simultaneous clients? Table 8-1 summarizes the key differences between the three transports. Tab… |
| [23] | output, write to stderr or use a logging framework configured to write to a file. When to Use stdio The stdio transport is ideal when the client and server live on the same machine. Desktop AI clients like Claude Desktop, GitHub Copilot in … |
| [24] | connection. Clients send requests as HTTP POST messages and can optionally upgrade to a streaming connection when they need to receive server-initiated messages like progress updates or change notifications. Chapter 2 MCp arChiteCture under… |
| [25] | opening the transport, performing the initialization handshake, sending requests, and routing responses back to the host. If the host is a restaurant manager, the client is the waiter who takes orders to the kitchen and brings dishes back t… |
| [26] | ports to open, and no firewall rules to worry about. You just build a console application and it works. That’s it! For quick prototyping and local development I almost always start with stdio and only switch to HTTP when I need remote acces… |
| [27] | policies, rate limiting, and logging all apply to the MCP endpoint automatically, no special configuration needed. Chapter 8 transport Layers134 Security Considerations This is the section you should read twice. Adding an MCP endpoint to yo… |
| [28] | explicitly protect the endpoint. The good news is that protection works exactly how you would expect in ASP.NET Core. You can chain RequireAuthorization() directly on the endpoint: app.MapMcp().RequireAuthorization(); This ensures that only… |
| [29] | means you get all the features you would expect from a modern .NET web application: middleware, dependency injection, authentication, logging, and more. The setup involves two extension methods. On the service side, you call WithHttpTranspo… |
| [30] | server lifecycle tied to host process Independent Independent sDK package ModelContext Protocol ModelContextProtocol. AspNetCore ModelContextProtocol. AspNetCore Best for Local dev, desktop aI clients Legacy deployments production, cloud, r… |
| [31] | server, a database server, and a web-search server all at once. One important detail is that the host never speaks the MCP wire protocol directly. It delegates that job to one or more MCP clients that live inside it. This keeps the host cod… |
| [32] | The Protocol Lifecycle Every MCP connection follows a well-defined lifecycle with four phases: connection, initialization, operation, and shutdown. Understanding this lifecycle will save you hours of debugging when you start building your o… |
| [33] | dotnet, with the run command and project path passed as Arguments. But it can be any executable. If you are connecting to a Python MCP server, the command might be python or uv. For a Node.js server, it could be npx. The transport handles a… |
| [34] | Running the Server With the code in place, let’s see if it actually runs. Open a terminal in your project folder and type dotnet run And then… nothing happens. Or at least, nothing visible. The terminal just sits there with a blinking curso… |
| [35] | One gotcha worth knowing: if you add logging later, make sure it goes to stderr, not stdout. Any output on stdout will corrupt the JSON-RPC message stream. The hosting framework in the SDK handles this for you when you use WithStdioServerTr… |
| [36] | ownership of the process’s stdin and stdout handles. Every JSON-RPC request is written to your process’s stdin as a single line of JSON, and your server writes responses to stdout the same way. The SDK handles all of this for you. You call … |
| [37] | are picked up automatically. No code changes required. For scenarios where you need live reloading without a restart, look into IOptionsMonitor<T> instead of IOptions<T>. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon162 Logging You… |
| [38] | host, configure the MCP server as a hosted service, and then run it. You might wonder why we use CreateEmptyApplicationBuilder instead of the more common CreateApplicationBuilder. The reason is the stdio transport. Our MCP server communicat… |
| [39] | The .NET console logging provider writes to stdout by default, which would corrupt the protocol stream. To fix this, you configure the console logger to send everything to stderr instead. The hosting framework makes this easy with a single … |
| [40] | Version="10.0.3" /> <PackageReference Include="ModelContextProtocol" Version="1.3.0" /> </ItemGroup> </Project> Nothing exotic here. It is a standard console app with two NuGet references. The target framework is net10.0, which is required … |
| [41] | The LogToStandardErrorThreshold setting tells the console provider to redirect any log entry at or above the specified level to stderr. Setting it to Trace means every log message goes to stderr, which is exactly what you want for an MCP se… |
| [42] | JSON-RPC responses. Any text that is not valid JSON-RPC would confuse the client on the other end. This is one of the things that trips up developers new to MCP. With a web API, you get a nice “Now listening on https://localhost:5000” messa… |


---

## Q08 — DI + Hosting model (.NET Generic Host)

**Question:** How does the book structure the .NET host for an MCP server: Host.CreateApplicationBuilder or Host.CreateEmptyApplicationBuilder, AddMcpServer(), logging configuration (and why logs must go to stderr), dependency injection of services into tool classes (constructor injection versus method-parameter injection of IMcpServer, HttpClient and other services), and service lifetimes?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 41

### Answer (verbatim NotebookLM)

In *Building MCP Servers with C#*, Gerald Versluis explains how the .NET Generic Hosting model and Dependency Injection (DI) infrastructure form the backbone of a C# MCP server [1-3].

---

###### 1. `Host.CreateApplicationBuilder` vs. `Host.CreateEmptyApplicationBuilder`

When constructing a standard I/O (`stdio`) MCP server, the book explicitly recommends using **`Host.CreateEmptyApplicationBuilder(null)`** rather than the standard `Host.CreateApplicationBuilder(args)` [2, 4].

* **The Problem with `CreateApplicationBuilder`**: Standard `Host.CreateApplicationBuilder` automatically configures a default console logging provider that writes diagnostic messages to standard output (`stdout`) [2]. 
* **The `stdio` Rule**: For `stdio`-based MCP servers, `stdout` is strictly reserved for JSON-RPC protocol messages [2, 5-7]. Any plain text (such as startup banners or log entries) output to `stdout` corrupts the JSON-RPC message stream and immediately crashes the client connection [2, 6, 7].
* **The Solution**: `Host.CreateEmptyApplicationBuilder(null)` starts with a completely clean slate—no default logging providers and no default `stdout` output—ensuring the standard output channel remains reserved for protocol communication [4].

---

###### 2. Registering the MCP Server with `AddMcpServer()`

`AddMcpServer()` is an extension method on `IServiceCollection` that hooks the MCP server into the .NET Dependency Injection container [4, 8]. Calling `AddMcpServer()` returns an MCP server builder that allows you to chain fluent configuration methods [8, 9]:

* **Transport Configuration**: e.g., `.WithStdioServerTransport()` for `stdio` or `.WithHttpTransport()` for Streamable HTTP [4, 10].
* **Feature Discovery**: e.g., `.WithToolsFromAssembly()`, `.WithResourcesFromAssembly()`, or `.WithPromptsFromAssembly()` to scan assemblies via reflection, or manual registration methods like `.WithTools<T>()` [4, 11-13].

---

###### 3. Logging Configuration & The `stderr` Requirement

Because standard output (`stdout`) is reserved for JSON-RPC, the MCP specification mandates that all diagnostic logs and debugging text must be directed to **standard error (`stderr`)** or a file [6, 7, 14, 15].

To configure console logging safely in `appsettings.json`, set `LogToStandardErrorThreshold` to `"Trace"` [14, 16]:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    },
    "Console": {
      "LogToStandardErrorThreshold": "Trace"
    }
  }
}
```

Setting this threshold to `"Trace"` forces the console logging provider to redirect **all** log levels (Trace, Debug, Info, Warning, Error) to `stderr`, allowing you to use `ILogger<T>` throughout your tools without risking `stdout` stream corruption [14, 16-18].

---

###### 4. Dependency Injection into Tool Classes

When an MCP client invokes a tool method, the SDK inspects every parameter in the method signature at runtime to decide how to fulfill it [19]:

1. **Service Parameters**: If the parameter's .NET type is registered in the DI container (such as `ILogger<T>`, `IHttpClientFactory`, `AppDbContext`, or `IOptions<T>`), the SDK resolves it directly from the container [19, 20]. These types are excluded from the public JSON Schema exposed to the AI [20-22].
2. **Tool Arguments**: If the parameter's type is **not** in the DI container, the SDK treats it as a client-facing argument that the AI model must supply and embeds it in the generated JSON Schema [19, 20].

###### **A. Method-Parameter Injection vs. Constructor Injection**
* **Method-Parameter Injection**: For static or instance tool methods, dependencies can be declared directly alongside tool arguments in the method signature [23, 24]. The SDK resolves the DI services while expecting the AI model to supply the remaining parameters [19, 23].
* **Constructor Injection**: For non-static (instance) tool classes, services can be injected through class constructors (or C# primary constructors) [25-27]. When a tool is invoked, the SDK instantiates the class, resolves its constructor dependencies, and executes the method [25, 27, 28].

###### **B. Injecting Special Framework Types & `IMcpServer`**
The SDK recognizes specific framework types and injects them automatically without requiring manual DI registration or generating JSON schema properties [21, 22, 29, 30]:
* **`McpServer` / `IMcpServer`**: Injected to give the tool direct access to server-level features (such as sending notifications or initiating sampling requests) [29, 31, 32].
* **`McpRequestContext` / `RequestContext<T>`**: Injected to access request metadata, enable polling, or check progress tokens [29, 31, 33].
* **`CancellationToken`**: Injected to handle client cancellation signals [21].
* **`IProgress<ProgressNotificationValue>`**: Injected to report real-time progress updates back to the client [22, 34].

###### **C. `HttpClient` vs. `IHttpClientFactory`**
When calling external APIs, the book warns against creating `HttpClient` instances directly via `new HttpClient()`, as this can cause socket exhaustion under load [35, 36]. Instead, register `builder.Services.AddHttpClient()` in `Program.cs` and inject **`IHttpClientFactory`** into your tools [35, 37, 38]. `IHttpClientFactory` manages underlying HTTP message handlers efficiently [35, 38].

---

###### 5. Service Lifetimes in MCP Servers

Service lifetimes in MCP servers follow standard .NET hosting rules, but with a specific operational context [3, 39, 40]:

* **`AddSingleton`**: A single instance lives for the entire lifetime of the MCP server application [40, 41]. Ideal for stateless services, in-memory caches, or static reference data (e.g., an in-memory menu service) [40, 41].
* **`AddScoped`**: A fresh instance is created for each scope [40, 42]. In the C# MCP SDK, **the runtime creates a distinct DI scope for every individual tool invocation** [42-45].
  * *Important Pattern*: Scoped services—most notably Entity Framework Core's **`DbContext`**—must be registered as `Scoped` [42, 44, 46]. Every tool call gets its own isolated `DbContext` instance that is automatically disposed when the tool call finishes, preventing cross-request change-tracking leaks and concurrency bugs [42, 43, 45].
* **`AddTransient`**: A new instance is created every time the service is requested [40, 42]. Suitable for lightweight, stateless helper objects [40, 42].

---

💡 **Next Steps**: Would you like to see a complete runnable `Program.cs` code example integrating EF Core, `IHttpClientFactory`, and `appsettings.json` options, or explore unit testing tools with mocked dependencies?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | as the TypeScript SDK. You are not working with a community port or a second-class citizen. This is a first-class implementation of the MCP specification, maintained by the same people who build the .NET runtime and libraries. Second, the S… |
| [2] | host, configure the MCP server as a hosted service, and then run it. You might wonder why we use CreateEmptyApplicationBuilder instead of the more common CreateApplicationBuilder. The reason is the stdio transport. Our MCP server communicat… |
| [3] | inside a single static method. Real applications talk to databases, call external APIs, read configuration files, and write log entries. The glue that holds all of that together in modern .NET is dependency injection, or DI for short. The g… |
| [4] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [5] | Running the Server With the code in place, let’s see if it actually runs. Open a terminal in your project folder and type dotnet run And then… nothing happens. Or at least, nothing visible. The terminal just sits there with a blinking curso… |
| [6] | JSON-RPC responses. Any text that is not valid JSON-RPC would confuse the client on the other end. This is one of the things that trips up developers new to MCP. With a web API, you get a nice “Now listening on https://localhost:5000” messa… |
| [7] | are picked up automatically. No code changes required. For scenarios where you need live reloading without a restart, look into IOptionsMonitor<T> instead of IOptions<T>. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon162 Logging You… |
| [8] | ASP.NET Core, and the MCP SDK plugs straight into it. When you call Host.CreateEmptyApplicationBuilder(args) in your Program. cs, you get back a builder that owns a Services collection. That collection is an IServiceCollection, and it is wh… |
| [9] | namespace has the attributes McpServerToolType and McpServerTool. And System. ComponentModel gives us the Description attribute, which the SDK reads to generate human-readable descriptions for AI clients. The builder setup is three lines of… |
| [10] | means you get all the features you would expect from a modern .NET web application: middleware, dependency injection, authentication, logging, and more. The setup involves two extension methods. On the service side, you call WithHttpTranspo… |
| [11] | McpServerToolType and registers every tool it finds. For most projects, this is all you need. builder.Services .AddMcpServer() .WithStdioServerTransport() .WithToolsFromAssembly(); But sometimes you want more control. Maybe you have a class… |
| [12] | With all three resource classes in place, the Program.cs file is delightfully simple, as seen in Listing 6-5 underneath. Listing 6-5. Program.cs wiring up all resources using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensi… |
| [13] | Now let’s set up the server in Program.cs. The only difference from our tool servers is that instead of calling WithToolsFromAssembly, we call WithPromptsFromAssembly. This tells the SDK to scan the assembly for classes decorated with the p… |
| [14] | similar to what we have seen in Claude Desktop. Chapter 4 Your First MCp server55 Figure 4-4. Verifying our MCP server configuration in VS Code Understanding What Just Happened Now that you have seen the server work end to end, let’s walk t… |
| [15] | the request at any point, the token fires and the operation unwinds cleanly. Also notice the ReadOnly = true on the attribute. This tool only reads data, so we tell clients that it is safe to call without user confirmation. Chapter 5 Creati… |
| [16] | The .NET console logging provider writes to stdout by default, which would corrupt the protocol stream. To fix this, you configure the console logger to send everything to stderr instead. The hosting framework makes this easy with a single … |
| [17] | The LogToStandardErrorThreshold setting tells the console provider to redirect any log entry at or above the specified level to stderr. Setting it to Trace means every log message goes to stderr, which is exactly what you want for an MCP se… |
| [18] | 163 With the configuration in place, you can use ILogger<T> freely throughout your tools and services, you can see some example lines of this in Listing 9-11 below. The structured logging pattern with curly brace placeholders works perfectl… |
| [19] | Injecting Services into Tools In earlier chapters, you saw that tool methods accept parameters that map to the arguments an AI model sends along with a tool call. A string parameter called city becomes a required input that the model fills … |
| [20] | in Program.cs because Microsoft.Extensions.Hosting already registers ILogger<T> for you. Note the SDK uses a simple rule: if a parameter type is available in the DI container, it is resolved from there. If it is not, the parameter is treate… |
| [21] | Task<CallToolResult>. CancellationToken Support When a client sends a cancellation request for a long-running tool, the MCP server needs a way to propagate that cancellation to your code. The SDK handles this by injecting a CancellationToke… |
| [22] | Progress Reporting Some tools take a long time to run. Maybe you are scanning a directory tree, processing a large file, or running a complex analysis. Without progress reporting, the client and the user are left staring at a spinner wonder… |
| [23] | provide. This means you can mix and match. A single method can have some parameters that come from the AI model and others that come from the DI container. The SDK handles the split automatically. Pretty neat. Take a look at the simplest ex… |
| [24] | injected ILogger earlier. The SDK resolves IPizzaMenuService from the container and passes it to your method. Have a look at the example code for this in Listing 9-5 below. Notice how the service class is now just another parameter in the c… |
| [25] | describes the tool’s input. When an AI model decides to call your tool, it will construct a JSON object that matches this schema, and the SDK handles deserializing it into the correct .NET types. The return type is a plain string. Under the… |
| [26] | services. The pattern is the same one you use in ASP.NET Core: define an interface, write an implementation, and register it on builder.Services. Allow me to walk through a practical example. We will build a pizza menu service that our MCP … |
| [27] | this repository talks to a database, but in our tests, we want to control exactly what data it returns. In Listing 11-4 below, notice how the interface for the product repository is declared and then injected in the ProductTools class const… |
| [28] | discover as resources. The class can be static or an instance class. If it is an instance class, the SDK will construct a new instance for each resource invocation, which is handy when your class implements IDisposable or needs per-request … |
| [29] | The SDK provides an ISseEventStreamStore interface for persisting SSE events while the connection is closed. There’s a built-in implementation called Distributed CacheEventStreamStore that works with any IDistributedCache from ASP.NET Core.… |
| [30] | // Do the actual work for this step await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); } return string.Join("\n", results); } A few things to notice. First, the tool checks whether ProgressToken is present before sending notifica… |
| [31] | of seconds, so you can watch the progress arrive in real time when you connect with the MCP Inspector or watch the progress while invoking it in VS Code. The core pattern looks like Listing 8-10. Listing 8-10. Sending progress notifications… |
| [32] | you can use it like any other chat client. If you want the LLM to automatically invoke tools, compose it with UseFunctionInvocation(). Listing 10-18 shows how a server-side tool might use sampling. Listing 10-18. Server-side sampling with t… |
| [33] | Polling is great for tools that run for minutes, but sometimes you want more granular feedback. The MCP specification includes a progress notification mechanism that lets your tool report exactly where it is in a multistep process. Clients … |
| [34] | special parameter and does not include it in the JSON schema. When the tool is invoked, the SDK injects an IProgress implementation that sends MCP progress notifications back to the client. The ProgressNotificationValue type has three prope… |
| [35] | handshake. The client sends an “initialize” request that includes its own capabilities and the protocol version it supports. Your server responds with its capabilities, which include things like “I support tools” or “I support resources.” T… |
| [36] | Tip You might notice we’re creating an HttpClient directly here. For a quick demo, that’s fine, but in production, you should use IHttpClientFactory through dependency injection. We’ll cover that pattern in Chapter 9. Creating HttpClient in… |
| [37] | up stock prices, or in our case retrieving a random joke. The .NET way to do this is IHttpClientFactory, and it integrates beautifully with the DI container. Registering the HttpClient Factory Registration is a one-liner, you can see it in … |
| [38] | it manages a pool of HttpMessageHandler instances so you do not run into socket exhaustion, a notorious problem when you create HttpClient instances manually. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon164 Calling an External API… |
| [39] | Let’s trace the complete flow for our echo example. The user types “echo hello world” in the chat. The AI model sees that an Echo tool is available and decides to call it with the message “hello world.” The client sends a JSON-RPC request t… |
| [40] | Registering the Service With the interface and implementation ready, you register it in Program.cs. The DI container offers three lifetimes: AddSingleton: One instance for the entire lifetime of the application. Great for stateless services… |
| [41] | in milliseconds, and your tool method never needs to know about it. When the AI calls GetJoke, the SDK resolves IHttpClientFactory and hands your tool a properly managed HTTP client. Clean, testable, and production-ready. A Note on Service … |
| [42] | a good example. Use AddScoped for services that need a fresh instance per tool invocation but can be shared within that invocation. Database contexts are the classic example. Use AddTransient for lightweight services where you do not care a… |
| [43] | MCP endpoint and monitor your connection pool usage. ASP.NET Core’s built-in rate limiter works perfectly here: app.MapMcp().RequireRateLimiting("mcp-policy"); Scoped services like DbContext work correctly. The SDK creates a fresh DI scope … |
| [44] | works perfectly with the DI setup we have been building. I will keep this section brief because EF Core is a big topic on its own, but I want you to see how naturally it fits. Setting Up a DbContext Suppose you want to store pizza orders. Y… |
| [45] | colleague reads before deciding which tool to use. Note a few things about DbContext in MCp tools worth knowing. the sDK creates a new Di scope for each tool invocation, which means each call gets a fresh DbContext. this is exactly what you… |
| [46] | will lead to subtle bugs and data corruption. Always keep them scoped. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon170 Summary That was quite a journey: you learned how to turn a simple MCP server into a properly structured .NET a… |


---

## Q09 — Tổ chức project, NuGet, packaging

**Question:** How does the book recommend organizing an MCP server C# project: required NuGet packages and versions, folder and file layout for tools, resources and prompts, naming conventions for tool names and descriptions, configuration and options, and packaging or distribution (dotnet tool, dnx, single executable) so that a client such as Claude Desktop or VS Code can launch it?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 42

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#***, Gerald Versluis provides clear recommendations for organizing, structuring, configuring, and publishing an MCP server project [1]. Here is the full breakdown based on the book's guidance:

---

###### 1. Required NuGet Packages and Framework Version

The C# MCP SDK targets **.NET 10.0** (`net10.0`) [2]. The book highlights three primary NuGet packages (stable version `1.3.0` at publication) along with standard .NET hosting infrastructure [3, 4]:

* **`ModelContextProtocol`**: The primary package used for most projects [5]. It provides the core protocol types, serialization, tool/resource/prompt attributes, stdio transport support, and generic host integration [4, 5].
* **`Microsoft.Extensions.Hosting`**: Used alongside `ModelContextProtocol` to provide the .NET Generic Host for managing the server's lifecycle and Dependency Injection container [2, 4].
* **`ModelContextProtocol.AspNetCore`**: Referenced when building or integrating network-based servers over Streamable HTTP or SSE using ASP.NET Core minimal APIs [6, 7].
* **`ModelContextProtocol.Core`**: A lightweight package providing bare protocol types and interfaces without higher-level hosting helpers, intended for embedding MCP into custom frameworks [8].

A standard stdio server project file (`.csproj`) is structured as follows [2]:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.3" />
    <PackageReference Include="ModelContextProtocol" Version="1.3.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

---

###### 2. Recommended Folder and File Layout

Rather than keeping all handlers in a single `Program.cs` file, the book recommends organizing tools, resources, prompts, models, and services into dedicated subfolders grouped by feature or domain [9-11].

###### **Standard Directory Layout**
```text
MyMcpServer/
├── Program.cs                  <-- Host builder setup & assembly scanning registration
├── appsettings.json            <-- Server settings & stderr logging configuration
├── MyMcpServer.csproj          <-- Project file targeting net10.0
├── Models/                     <-- DTOs, domain records, options classes
│   ├── Pizza.cs
│   └── MyServerOptions.cs
├── Services/                   <-- Business logic interfaces & implementations
│   ├── IPizzaMenuService.cs
│   └── PizzaMenuService.cs
├── Tools/                      <-- Tool classes ([McpServerToolType])
│   ├── OrderTool.cs
│   └── FactsTool.cs
├── Resources/                  <-- Resource classes ([McpServerResourceType])
│   └── MenuResource.cs
└── Prompts/                    <-- Prompt classes ([McpServerPromptType])
    └── PartyPlannerPrompt.cs
```

###### **Assembly Scanning in `Program.cs`**
To discover all handlers automatically across this directory structure without manual wiring, the host builder chains assembly scanning extension methods [12-15]:
```csharp
var builder = Host.CreateEmptyApplicationBuilder(null);

// Register application services
builder.Services.AddSingleton<IPizzaMenuService, PizzaMenuService>();

// Register MCP server and scan assembly for all primitives
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

await builder.Build().RunAsync();
```

---

###### 3. Naming Conventions for Tool Names and Descriptions

###### **Tool Public Names**
* Most MCP clients expect **`snake_case`** naming for tools (e.g., `order_pizza`, `get_space_fact`) [16, 17].
* Set the public tool name explicitly using `[McpServerTool(Name = "place_pizza_order")]` to maintain full control over the public protocol contract [16].
* If `Name` is omitted, the SDK automatically converts PascalCase C# method names (e.g., `CelsiusToFahrenheit`) to `snake_case` (`celsius_to_fahrenheit`) during tool discovery [16, 17].

###### **Descriptions (`[Description]`)**
* Always apply `[Description("...")]` (from `System.ComponentModel`) to tool methods and every parameter [18, 19].
* **Method Descriptions**: Tell the AI model *when* and *why* to invoke the tool [19, 20]. Write them clearly and concisely—focused on *what* the tool accomplishes rather than internal implementation details [20].
* **Parameter Descriptions**: Tell the AI model what values are expected [19, 21]. The SDK inspects parameter types, default values, and descriptions to auto-generate the JSON Schema passed to the AI client [21-23].

###### **Display Metadata**
* Use `Title` (e.g., `Title = "Place Order"`) for human-friendly labels in client UIs, separate from the machine-readable tool `Name` [24, 25].
* Use `IconSource` with an HTTP/HTTPS URL or `data:` URI to attach visual icons to tools, resources, or prompts [24, 25].

---

###### 4. Configuration, Options Pattern, and Logging Setup

###### **Configuration with `IOptions<T>`**
The server integrates with .NET configuration via strongly typed options classes [26, 27]:

1. **Create an Options Class**:
   ```csharp
   public class MyServerOptions
   {
       public const string SectionName = "MyServer";
       public string RestaurantName { get; set; } = "Cosmic Pizza";
       public string Currency { get; set; } = "GalactiCoins";
   }
   ```
2. **Add Settings to `appsettings.json`**:
   ```json
   {
     "MyServer": {
       "RestaurantName": "Cosmic Pizza - Intergalactic Pizzeria",
       "Currency": "GalactiCoins"
     }
   }
   ```
3. **Bind and Inject**: Bind the options section in `Program.cs` via `builder.Services.Configure<MyServerOptions>(...)` and inject `IOptions<MyServerOptions>` directly into tool or resource methods [28-30].

###### **Logging Redirect to `stderr`**
For stdio-based servers, **standard output (`stdout`) is strictly reserved for JSON-RPC messages** [31-33]. Writing plain text to `stdout` corrupts the protocol stream and immediately crashes the client connection [31, 32].

* **Use `Host.CreateEmptyApplicationBuilder(null)`**: Avoids the default console logger configured by `Host.CreateApplicationBuilder` that writes to `stdout` [31].
* **Configure `LogToStandardErrorThreshold`**: In `appsettings.json`, set the console logger to redirect all output to standard error (`stderr`) [33-35]:
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning"
      },
      "Console": {
        "LogToStandardErrorThreshold": "Trace"
      }
    }
  }
  ```

---

###### 5. Packaging and Distribution for AI Clients

To allow clients like **Claude Desktop** or **VS Code** to launch the server, the book details four distribution patterns [36-39]:

###### **Option A: Local Development (`dotnet run`)**
Clients execute the .NET SDK CLI directly pointing to the project file [36, 37]:
* **Claude Desktop** (`claude_desktop_config.json`) [36, 40]:
  ```json
  {
    "mcpServers": {
      "cosmic-pizza": {
        "command": "dotnet",
        "args": ["run", "--project", "/full/path/to/CosmicPizzaServer.csproj"]
      }
    }
  }
  ```
* **VS Code** (`.vscode/mcp.json` or settings) [37, 41]:
  ```json
  {
    "servers": {
      "cosmic-pizza": {
        "type": "stdio",
        "command": "dotnet",
        "args": ["run", "--project", "/full/path/to/CosmicPizzaServer.csproj"]
      }
    }
  }
  ```

###### **Option B: Published .NET Global Tool**
For easy distribution across .NET developer environments, package the server as a global tool [39, 42]:
1. Configure properties in `.csproj` [39]:
   ```xml
   <PropertyGroup>
     <PackAsTool>true</PackAsTool>
     <ToolCommandName>mcp-pizza</ToolCommandName>
     <PackageId>CosmicPizzaServer</PackageId>
     <Version>1.0.0</Version>
   </PropertyGroup>
   ```
2. Pack and publish via `dotnet pack -c Release` and `dotnet nuget push` [43].
3. Users install via `dotnet tool install --global CosmicPizzaServer` and clients launch the tool directly [44]:
   ```json
   {
     "mcpServers": {
       "cosmic-pizza": {
         "command": "mcp-pizza"
       }
     }
   }
   ```

###### **Option C: One-Shot Execution via `dnx`**
Using `.NET 10`'s `dnx` (or `dotnet tool exec`), clients can fetch and run published .NET tool packages on-demand without prior installation (similar to `npx` in Node.js) [45, 46]:
```json
{
  "mcpServers": {
    "cosmic-pizza": {
      "command": "dnx",
      "args": ["CosmicPizzaServer"]
    }
  }
}
```

###### **Option D: Docker Containerization**
For containerized stdio servers, the client launches Docker interactively using standard streams [47-49]:
```json
{
  "servers": {
    "pizza-docker": {
      "type": "stdio",
      "command": "docker",
      "args": ["run", "-i", "--rm", "cosmic-pizza-image"]
    }
  }
}
```
*(For Streamable HTTP servers running in Docker or Azure, clients specify `"type": "http"` and point to the server URL e.g., `"url": "http://localhost:8080"`) [49-51].*

---

💡 **Would you like to review how to set up automated CI/CD pipelines with GitHub Actions for building, testing, and publishing .NET MCP tools to NuGet?**

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Building MCP Servers with C# Connect AI Agents to Your .NET Applications with the Model Context Protocol — Gerald VersluisBuilding MCP Servers with C# Connect AI Agents to Your .NET Applications with the Model Context Protocol Gerald Verslu… |
| [2] | 44 The ModelContextProtocol package brings in everything we need: the transport layer, the JSON-RPC plumbing, tool discovery, and the attributes we will use to mark our tools. The Microsoft.Extensions.Hosting package gives us the generic ho… |
| [3] | Table 3-1 summarizes when to reach for each package. Table 3-1. MCP NuGet packages at a glance Package When to Use modelContextprotocol most projects. Full server and client support over stdio, including hosting helpers. modelContextprotoco… |
| [4] | dotnet new console -n FirstMcpServer That gives us the familiar Hello World starter. Before we touch any code, we need the official MCP NuGet package. The C# SDK lives in a package called ModelContextProtocol. The C# SDK is available as a s… |
| [5] | this book, any of the three editors will work just fine, so use whatever you are most comfortable with. The MCP NuGet Packages The official MCP SDK for .NET is published as a set of NuGet packages. Understanding which package to reference i… |
| [6] | application framework and want full control over the server lifecycle. Unless you have a specific reason to go minimal, I suggest starting with the main package instead. ModelContextProtocol.AspNetCore When you want to expose your MCP serve… |
| [7] | extension methods. $ cd HttpTransportServer $ dotnet add package ModelContextProtocol $ dotnet add package ModelContextProtocol.AspNetCore The ModelContextProtocol.AspNetCore package depends on the base ModelContextProtocol package, so you … |
| [8] | Under the hood, it depends on ModelContextProtocol.Core, so you get the full type system, serialization, and protocol handling out of the box. For the majority of this book, we will reference this package. Chapter 3 Setting Up YoUr Developm… |
| [9] | Chapter 5 Creating tools82 Tool Organization As your MCP server grows, you will inevitably end up with many tools. Keeping them all in a single class quickly becomes unwieldy. The SDK supports multiple tool classes, and there are two differ… |
| [10] | hosting stack does the heavy lifting while you focus on writing the tools that make your server useful. The project structure for this server is straightforward, see Listing 9-17. I keep things organized by creating folders for the differen… |
| [11] | clean separation of concerns. Listing 12-1 shows what our project structure will look like. Listing 12-1. Cosmic Pizza Server project structure CosmicPizzaServer/ CosmicPizzaServer.csproj Program.cs appsettings.json Models/ Pizza.cs Order.c… |
| [12] | desktop AI clients like Claude Desktop and GitHub Copilot in VS Code expect. Finally, WithToolsFromAssembly scans the current assembly for any classes decorated with the right attributes and registers them as tools automatically. Here is th… |
| [13] | With all three resource classes in place, the Program.cs file is delightfully simple, as seen in Listing 6-5 underneath. Listing 6-5. Program.cs wiring up all resources using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensi… |
| [14] | Now let’s set up the server in Program.cs. The only difference from our tool servers is that instead of calling WithToolsFromAssembly, we call WithPromptsFromAssembly. This tells the SDK to scan the assembly for classes decorated with the p… |
| [15] | builder.Services.AddSingleton<IPizzaMenuService, PizzaMenuService>(); builder.Services.AddSingleton<IOrderService, OrderService>(); Chapter 12 Building the CosmiC pizza server244 builder.Services .AddMcpServer() .WithStdioServerTransport() … |
| [16] | makes the intent clear and prevents anyone from accidentally adding instance state. The McpServerTool Attribute Each method you want to expose as a tool gets the McpServerTool attribute. This attribute accepts several optional properties th… |
| [17] | attribute or not registering a tool class with the server. Note that the SDK automatically converts your C# method names to snake_case when exposing them as MCP tools. Snake_case is a naming convention where words are separated by underscor… |
| [18] | namespace has the attributes McpServerToolType and McpServerTool. And System. ComponentModel gives us the Description attribute, which the SDK reads to generate human-readable descriptions for AI clients. The builder setup is three lines of… |
| [19] | Chapter 4 Your First MCp server47 that should be exposed to AI clients. The Description attribute, which comes from the standard System.ComponentModel namespace, provides the human-readable text that AI clients display when they list availa… |
| [20] | you have full control over the public contract. Next is the Description property on the System.ComponentModel.Description attribute. You place this attribute on the method itself and it tells the AI model what the tool does. Write descripti… |
| [21] | Describing Parameters Every parameter on a tool method should have a Description attribute of its own. The SDK uses these descriptions when it builds the JSON schema for the tool. That schema is what the AI model reads to understand which a… |
| [22] | EchoTool that takes a string and returns it and a SpaceFactTool that returns random facts about the cosmos. We connected the server to both Claude Desktop and Visual Studio Code, and we traced the flow of a tool call from user request to se… |
| [23] | reads the parameter names, their .NET types, any default values, and the Description attributes you added. From all of that, it generates a JSON schema object that it sends to the client during tool discovery. Chapter 5 Creating tools67 For… |
| [24] | described. Chapter 5 Creating tools65 Listing 5-1. The McpServerTool attribute used with its attributes, together with the Description attributes to describe methods and parameters [McpServerTool(Name = "greet", ReadOnly = true)] [Descripti… |
| [25] | If your tool is called get_server_metrics, you could set Title to “Server Metrics” so the client shows something prettier in its UI. IconSource is even more fun. You can point it to an SVG or PNG icon, and clients will display it alongside … |
| [26] | [McpServerToolType] public static class EchoTool { [McpServerTool, Description("Echoes the input back.")] public static string Echo( [Description("Message to echo")] string message) { Chapter 4 Your First MCp server59 return $"Echo: {messag… |
| [27] | Chapter 4 Your First MCp server60 + "travels at about 28,000 " + "kilometers per hour." ]; [McpServerTool, Description( "Returns a random fun fact about space.")] public static string GetSpaceFact() { return Facts[Random.Shared.Next(Facts.L… |
| [28] | Binding and Injecting Options In Program.cs, you bind the section to your options class with a single line. After that, you can inject IOptions<PizzaServerOptions> into any tool or service. Have a look at the registration line in Listing 9-… |
| [29] | the options value gives you a strongly typed object with IntelliSense support instead of raw strings and magic keys. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon161 Listing 9-9. Using IOptions in a tool method [McpServerTool, Desc… |
| [30] | the heavy lifting, we just need to register our services and point the SDK at our code. Listing 12-17 shows the file that ties everything together. Listing 12-17. Complete Program.cs for the Cosmic Pizza Server using CosmicPizzaServer.Model… |
| [31] | host, configure the MCP server as a hosted service, and then run it. You might wonder why we use CreateEmptyApplicationBuilder instead of the more common CreateApplicationBuilder. The reason is the stdio transport. Our MCP server communicat… |
| [32] | One gotcha worth knowing: if you add logging later, make sure it goes to stderr, not stdout. Any output on stdout will corrupt the JSON-RPC message stream. The hosting framework in the SDK handles this for you when you use WithStdioServerTr… |
| [33] | C# SDK. It feels natural if you have used ASP.NET controllers or Minimal API endpoints before. You focus on writing the tool logic, and the framework takes care of the wiring. Tip You can organize tools across multiple classes and even mult… |
| [34] | The .NET console logging provider writes to stdout by default, which would corrupt the protocol stream. To fix this, you configure the console logger to send everything to stderr instead. The hosting framework makes this easy with a single … |
| [35] | The logging configuration sends console logs to stderr, which is critical for MCP servers using the stdio transport. Remember, stdout is reserved for MCP protocol messages. If your logs go to stdout, they will interfere with the protocol co… |
| [36] | { "mcpServers": { "my-mcp-server": { "command": "dotnet", "args": [ "run", "--project", "/path/to/your/McpServer.csproj" ] } } } Chapter 3 Setting Up YoUr Development environment35 The command field tells Claude Desktop which executable to … |
| [37] | content as seen in Listing 3-3. We will create the actual project in the next section, but you can already prepare this configuration file in any folder. Listing 3-3. VS Code MCP server configuration { "servers": { "my-mcp-server": { "type"… |
| [38] | distributing a self-contained executable are your best bets. For HTTP-based servers using SSE or the Streamable HTTP transport, you are dealing with a more traditional web service deployment. These servers need to live somewhere accessible … |
| [39] | executable by zipping it up and sending it over, you can also pack it as a .NET tool. To make your project packable as a .NET tool, add a few properties to your project file as shown in Listing 14-4 underneath. The important ones are PackAs… |
| [40] | app that gives you a chat interface to Claude and lets you connect to local MCP servers through a JSON configuration file. To configure Claude Desktop to talk to one of our servers, you need to edit the configuration file. On macOS, this fi… |
| [41] | you can immediately start chatting with Claude while it uses your server’s tools. Tip if you do not have a Claude Desktop subscription, do not worry. the other two client options i describe next are completely free. Visual Studio Code Visua… |
| [42] | command and args. Publishing As a .NET Tool This is one of my favorite ways to distribute MCP servers, and I will tell you why. Once published to NuGet, anyone with the .NET SDK installed can install your server with a single command. No cl… |
| [43] | Packing and Publishing Once your project file is configured, creating and publishing the tool package is a two-step process. Have a look at the terminal commands in Listing 14-5. Listing 14-5. Packing and publishing a .NET tool to NuGet # C… |
| [44] | Chapter 14 Deploying anD publishing287 Listing 14-6. Installing and running the .NET tool # Install the tool globally dotnet tool install --global McpWeatherServer # Run it mcp-weather Users can then reference mcp-weather directly in their … |
| [45] | But what about testing your tool locally before publishing it to NuGet? You have two great options. The first is to create a local NuGet feed from a folder. After running dotnet pack, point the install command at the output directory. # Pac… |
| [46] | installing it first. Once your package is on NuGet (or a local feed), you can run it in one shot. # Run a .NET tool without installing it dnx McpWeatherServer # Or with the longer form dotnet tool exec McpWeatherServer The dnx command downl… |
| [47] | MCP server. No magic, just a couple of commands as shown below in Listing 14-2. Chapter 14 Deploying anD publishing284 Listing 14-2. Building and running the Docker container # Build the Docker image docker build -t mcp-weather-server . # R… |
| [48] | docker as the command instead of dotnet as we’ve seen throughout the rest of this book. The full command and configuration can be seen in Listing 14-3 below. Chapter 14 Deploying anD publishing285 Listing 14-3. VS Code MCP configuration for… |
| [49] | The key here is that the MCP client runs docker run -i as the command, passing --rm to automatically clean up the container when the session ends. The -i flag keeps stdin open for the JSON-RPC communication. From the client’s perspective, i… |
| [50] | Listing 8-5. Customizing the MCP endpoint path app.MapMcp("/my-custom-mcp-endpoint"); To test the HTTP server, you can use any MCP client that supports the Streamable HTTP transport. You can also configure VS Code to connect to it. Instead … |
| [51] | between runs. Think of it as “just run this thing” without any ceremony. If you want to learn more about dnx, refer to the documentation here: https:// learn.microsoft.com/dotnet/core/tools/dotnet-tool-exec. Azure Deployment For HTTP-based … |


---

## Q10 — Testing / debugging / MCP Inspector / client config

**Question:** How does the book cover testing and debugging an MCP server: using MCP Inspector, configuring clients such as Claude Desktop, VS Code or GitHub Copilot (claude_desktop_config.json, mcp.json), logging, unit testing tools, and the common pitfalls it warns about?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 43

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, testing and debugging are covered extensively as essential practices for building reliable AI integrations [1-6]. Because an AI model consumes server outputs programmatically without human double-checking, returning bad data or crashing during tool execution directly breaks AI conversations [4, 5].

---

###### 1. Interactive Testing with the MCP Inspector

The **MCP Inspector** is an official browser-based debugging tool maintained by the MCP project that acts like "Postman or Swagger UI for MCP" [7, 8].

* **Launching**: It is launched via `npx` without local installation [9, 10]:
  ```bash
  npx @modelcontextprotocol/inspector dotnet run --project /path/to/MyMcpServer.csproj
  # Or for HTTP-based servers:
  npx @modelcontextprotocol/inspector --transport http --url http://localhost:5000
  ```
* **User Interface Layout**:
  * **Left Sidebar**: Configures connection parameters (STDIO or Streamable HTTP transport, command, arguments, environment variables, authentication, and a **Connect/Disconnect** toggle button) [11, 12].
  * **Tools Tab**: Displays all discovered tools with descriptions, annotation badges (`ReadOnly`, `Destructive`, `Idempotent`), and interactive forms with parameter input fields (required parameters are marked with an asterisk `*`) and a **Run Tool** button [9, 13, 14].
  * **Resources & Prompts Tabs**: Allows inspecting static resources, filling parameter values for resource templates, and previewing generated `ChatMessage[]` arrays from prompts [15-19].
  * **History & Server Notifications Panels**: Displays raw, real-time JSON-RPC request/response payloads (`tools/list`, `initialize`, `tools/call`, `resources/read`) and captures incoming server notifications (progress updates, resource updates, or logging messages) [19-21].

---

###### 2. Configuring Real AI Clients

The book covers configuration and behavior differences across major desktop clients:

###### **Claude Desktop**
* **Configuration File Location**:
  * macOS: `~/Library/Application Support/Claude/claude_desktop_config.json` [22, 23].
  * Windows: `%APPDATA%\Claude\claude_desktop_config.json` [22, 23].
* **JSON Structure** (`claude_desktop_config.json`) [22, 24, 25]:
  ```json
  {
    "mcpServers": {
      "MyMcpServer": {
        "command": "dotnet",
        "args": ["run", "--project", "/full/path/to/MyMcpServer.csproj"]
      }
    }
  }
  ```
* **Client Behavior**: Claude Desktop launches the server as a child process over `stdio`, displays loaded tools under the "Connectors" menu, prompts the user for permission before invoking tools, and surfaces static resources and prompt templates via UI pickers [25-30]. *(Note: Claude Desktop currently surfaces only static resources with fixed URIs, not parameterized resource templates [29]).*

###### **Visual Studio Code & GitHub Copilot**
* **Configuration File Location**: Workspace-local `.vscode/mcp.json` or global user settings (`mcp.json` accessed via Command Palette `"MCP: Add Server"`) [31-36].
* **JSON Structure** (`.vscode/mcp.json`) [36-38]:
  ```json
  {
    "servers": {
      "MyMcpServer": {
        "type": "stdio",
        "command": "dotnet",
        "args": ["run", "--project", "/full/path/to/MyMcpServer.csproj"]
      },
      "MyHttpServer": {
        "type": "http",
        "url": "http://localhost:5000"
      }
    }
  }
  ```
* **Client Behavior**: Displays connection status (`Running | Stop | Restart | N tools`), allows enabling/disabling tools via the "Configure Tools" icon in Copilot Chat, supports resource picking (including filling in resource template parameters via input boxes), and exposes server prompts as slash commands (e.g., `/mcp.MyMcpServer.MyPrompt`) [34-36, 39-43].

---

###### 3. Logging Rules and Diagnostic Output

* **The `stdout` Constraint**: For `stdio` servers, standard output (`stdout`) is **strictly reserved** for JSON-RPC protocol frames [44-47]. Writing plain text or standard console logs to `stdout` (e.g., via `Console.WriteLine` or default console loggers) interleaves raw text into the JSON-RPC stream, corrupting the connection and crashing the client [44, 45, 48-50].
* **`Host.CreateEmptyApplicationBuilder(null)`**: Must be used instead of standard `Host.CreateApplicationBuilder()` because the standard builder registers a default console logger that writes to `stdout` [44, 45, 51, 52].
* **Redirecting Logs to `stderr`**: In `appsettings.json`, diagnostic logs must be directed to standard error (`stderr`) by setting `"LogToStandardErrorThreshold": "Trace"` [34, 49, 53]:
  ```json
  {
    "Logging": {
      "Console": {
        "LogToStandardErrorThreshold": "Trace"
      }
    }
  }
  ```
* **IDE Debug Window Logging**: You can also use `builder.Logging.ClearProviders(); builder.Logging.AddDebug();` in `Program.cs` to view debug logs inside Visual Studio or Rider without sending output to `stdout` [52, 54].

---

###### 4. Unit and Integration Testing Strategies

###### **Unit Testing Tool Methods**
Because MCP tools are plain C# methods, they can be unit-tested directly using standard testing frameworks (MSTest, xUnit, or NUnit) [55-58]:
* **Culture Safety**: Always use `CultureInfo.InvariantCulture` when formatting numbers or dates in returned strings to prevent locale-dependent test failures (such as comma vs. dot decimal separators) [59, 60].
* **Mocking Dependencies**: When tool classes require external services (e.g., repositories, `DbContext`, or `IHttpClientFactory`), pass interfaces into tool constructors or parameters and mock them using **Moq** (`Mock<T>`) [61-68].

###### **Integration Testing with In-Memory Stream Transports**
To test tool discovery, JSON serialization, and message routing without launching external child processes or network sockets, link the server and client in-process using `System.IO.Pipelines.Pipe` [63, 69-72]:
```csharp
var clientToServer = new Pipe();
var serverToClient = new Pipe();

var builder = Host.CreateApplicationBuilder();
builder.Services.AddMcpServer()
    .WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream())
    .WithToolsFromAssembly();

var host = builder.Build();
await host.StartAsync();

var clientTransport = new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream());
await using var client = await McpClient.CreateAsync(clientTransport);

// Test Tool Discovery (SDK automatically converts C# PascalCase to snake_case)
var tools = await client.ListToolsAsync();
Assert.IsTrue(tools.Any(t => t.Name == "celsius_to_fahrenheit")); [73-76]

// Test Tool Execution over protocol
var result = await client.CallToolAsync("celsius_to_fahrenheit", new Dictionary<string, object?> { ["celsius"] = 100.0 });
Assert.AreEqual("100C = 212.0F", result.Content.OfType<TextContentBlock>().First().Text); [77, 78]
```

###### **Testing HTTP Servers with `WebApplicationFactory`**
For Streamable HTTP servers, use `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing` to test ASP.NET Core middleware, authorization, and endpoint routing entirely in memory [79-85].

###### **Testing Error Scenarios**
* **Generic Exception Masking**: Unhandled standard exceptions (e.g., `InvalidOperationException`) return a generic string (*"An error occurred invoking 'tool_name'"*) to prevent leaking sensitive internal stack traces or connection strings [86, 87].
* **`McpException`**: To return explicit, actionable error messages to the AI model, tool methods must throw an `McpException` or return a `CallToolResult` with `IsError = true` [86-90].

---

###### 5. Common Pitfalls and Warnings

1. **`stdout` Pollution**: The most common beginner bug. Any unhandled `Console.WriteLine` or third-party library logging to `stdout` breaks `stdio` framing [44, 45, 48-50].
2. **Terminal Silence on `dotnet run`**: Executing a `stdio` server binary manually in a terminal outputs no startup message (such as *"Now listening on..."*); it sits with a blinking cursor waiting for JSON-RPC input on `stdin`. This silence is normal protocol behavior, not a hang [91, 92].
3. **Startup Breakpoint Timeouts**: AI clients enforce strict connection timeouts during initialization. Pausing on a breakpoint in `Program.cs` during server startup causes the AI client to mark the server as unresponsive. The book recommends attaching debuggers *after* initialization or using stream-based unit tests for step-through debugging [93-95].
4. **Command Line Parsing in VS Code**: When adding a server via the VS Code Command Palette, spaces in project paths can be misparsed as separate arguments. Solution: Manually inspect and edit `.vscode/mcp.json` [35].
5. **Environment Variable Inheritance**: `StdioClientTransport` inherits environment variables from the host client process; missing API keys or configuration strings in client JSON definitions cause runtime failures [96].
6. **Exposing Secrets or PII**: Never return raw database connection strings, passwords, or unmasked PII in error messages, logs, or tool descriptions, as AI models read and display these outputs [97-99].

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Running the Application 193 Summary 194 Chapter 11: Testing Your MCP Servers 195 Why Testing Matters 195 |
| [2] | CHAPTER 11 Testing Your MCP Servers We have spent the last ten chapters building MCP servers that expose tools, resources, and prompts to AI clients. We have explored transports, wired up dependency injection, and handled configuration. Our… |
| [3] | methods, integration test the full MCP pipeline using in-memory transports, exercise HTTP-based servers with WebApplicationFactory, and use the MCP Inspector for manual exploratory testing. By the time we are done, you will have a testing s… |
| [4] | hooks that make testing surprisingly pleasant, and the payoff in confidence is absolutely worth the effort. And, of course, if you really don’t want to do it, ask Copilot… Why Testing Matters I know, I know. You have heard the speech about … |
| [5] | is no human double-checking the result before it goes back into the conversation. If your tool returns bad data, the AI model will happily use that bad data to generate its response.196 Think about that for a moment. In a traditional web AP… |
| [6] | There is also a practical consideration. MCP servers are typically consumed through AI clients like Claude Desktop or GitHub Copilot in VS Code. Debugging a misbehaving tool through an AI chat interface is, frankly, miserable. You type a pr… |
| [7] | 36 After saving the file, open the Copilot chat panel and you should see your server listed. VS Code will start the process for you and route tool calls through the stdio transport, exactly like Claude Desktop does. This is the setup I will… |
| [8] | codify in a test. This is where the MCP Inspector comes in. The MCP Inspector is an official tool from the MCP project that gives you a web- based interface to connect to any MCP server and interact with it. You can list tools, call them wi… |
| [9] | invoke tools or read resources manually. Think of it as Postman or Swagger UI, but for MCP. You can launch the Inspector directly with npx without installing anything globally. In Listing 3-4, you can see the terminal command for that. List… |
| [10] | setup should be familiar by now. As a quick reminder, you launch it with npx as shown in Listing 11-8. Listing 11-8. Launching the MCP Inspector npx @modelcontextprotocol/inspector dotnet run --project \ ./MyMcpServer/MyMcpServer.csproj Thi… |
| [11] | This book obviously targets .NET, but just for completeness; the dotnet run command can be any command, on any stack, to start your MCP server. Once the Inspector is running, it should typically open your default browser by itself. If not, … |
| [12] | Transport Type dropdown is set to STDIO by default, and the Command and Arguments fields are pre-filled if you passed them on the command line. Below those fields, you will find collapsible sections for Environment Variables, Authentication… |
| [13] | Chapter 11 testing Your MCp servers206 Once connected, the center area comes alive with tabs for Tools, Resources, Prompts, and more. Start with the Tools tab, as shown in Figure 11-1 underneath. You will see every tool your server exposes … |
| [14] | JSON schema your server advertises during tool discovery, but the Inspector renders it as a friendly form instead of raw JSON. Figure 11-1. The MCP Inspector Tools tab showing the tool list Fill in the input fields and click the Run Tool bu… |
| [15] | decorated with the McpServerPromptType attribute. The SDK discovers both prompts automatically when the server starts. You can try them out by connecting to the server with Claude Desktop or the MCP Inspector, just like you did with tools i… |
| [16] | three prompts listed: Greeting, PizzaRecommendation, and SpaceTriviaQuiz (plus ReviewCode and WriteTests if you included those classes). Click on any prompt to see its ChAPter 7 exPOSIng PrOMPtS119 parameters and send a test request. The In… |
| [17] | Select the PizzaRecommendation prompt and you will see input fields for each argument. Fill in a crust preference and a flavor profile, look at the expected values below the entry field, then click Get Prompt. The Inspector shows you the ex… |
| [18] | Switch to the Resources tab to see every static resource and resource template your server exposes. You can see this being shown in Figure 11-2 below. Click a static resource to read its content immediately. For resource templates, the Insp… |
| [19] | one shows its arguments. Fill in the parameters and the Inspector calls prompts/get to retrieve the messages your server would inject into a conversation. This is especially useful for multi-message prompts where you want to verify the syst… |
| [20] | interactively and inspect the JSON payloads going back and forth. The Inspector is invaluable when you are debugging protocol issues or just want to poke around without firing up a full chat client. We will take a proper guided tour of the … |
| [21] | responses flowing between the Inspector and your server. The Server Notifications panel captures any notifications your server sends, such as progress updates or resource change events. Together, these panels give you full visibility into t… |
| [22] | app that gives you a chat interface to Claude and lets you connect to local MCP servers through a JSON configuration file. To configure Claude Desktop to talk to one of our servers, you need to edit the configuration file. On macOS, this fi… |
| [23] | easiest ways to test your servers. To connect our echo server, we need to edit Claude Desktop’s configuration file. On macOS, this file lives at ~/Library/Application Support/Claude/claude_desktop_config.json, and on Windows, you can find i… |
| [24] | in bold is the actual MCP server we are adding; if there are already multiple MCP servers listed, make sure to add a comma and then add the entry in bold. There might already be other sections in the config file like preferences. Just add t… |
| [25] | 49 "mcpServers": { "FirstMcpServer": { "command": "dotnet", "args": [ "run", "--project", "/full/path/to/FirstMcpServer" ] } } } Replace the path in the args array with the actual path to your project folder. The command tells Claude Deskto… |
| [26] | servers. After saving the configuration file, restart Claude Desktop. If something went wrong with parsing the configuration file or loading the actual MCP server we added, there will be an error message. If after a couple of seconds nothin… |
| [27] | chosen). You can see this depicted in Figure 4-1. Chapter 4 Your First MCp server50 Figure 4-1. Our very first MCP server loaded in Claude Desktop Now type something like “Can you echo the message Hello MCP for me?” into the chat. Claude wi… |
| [28] | operations, depending on your settings, Claude Desktop (and any other tool that invokes tools) might ask you for permission first. In Figure 4-2, you can see that it detected our echo tool and asks us for permission to actually invoke it. C… |
| [29] | click the + button below the chat input, then select your server from the connectors list. Chapter 6 Working With resourCes101 You will see all your static resources listed by name, as shown in Figure 6-1. Click one and its content is attac… |
| [30] | In Claude Desktop, you will find your prompts in the prompt picker. Click the + button below the chat input and you will see your prompts listed alongside any other MCP prompts from connected servers. Select one, fill in the arguments if it… |
| [31] | you can immediately start chatting with Claude while it uses your server’s tools. Tip if you do not have a Claude Desktop subscription, do not worry. the other two client options i describe next are completely free. Visual Studio Code Visua… |
| [32] | in some cases, Claude Desktop does not find the dotnet command. in that case, you can fully qualify the path to the dotnet executable in the command field in the configuration JsoN file. or… ask Claude how to fix it! Connecting to Visual St… |
| [33] | macOS) and search for “MCP: Add Server.” VS Code will first ask whether this should be a User (global) or Workspace (local) configuration. For now, choose Workspace so the configuration stays with your project. Next, it will prompt you for … |
| [34] | you want; let’s just go with FirstMcpServer again. After you entered everything, VS Code will open the configuration JSON file so you can inspect the result and see the status of your MCP server. You can see this happening in Figure 4-3. No… |
| [35] | on that to open the Output pane that we see in the bottom with the logging from our MCP server. Chapter 4 Your First MCp server53 Figure 4-3. Our MCP server added to the VS Code configuration Note Closely inspect the output from adding the … |
| [36] | Chapter 4 Your First MCp server54 Alternatively, you can add the configuration manually. Open your VS Code settings JSON file and add the following like Listing 4-4. Listing 4-4. VS Code MCP server configuration { "mcp": { "servers": { "Fir… |
| [37] | content as seen in Listing 3-3. We will create the actual project in the next section, but you can already prepare this configuration file in any folder. Listing 3-3. VS Code MCP server configuration { "servers": { "my-mcp-server": { "type"… |
| [38] | Listing 8-5. Customizing the MCP endpoint path app.MapMcp("/my-custom-mcp-endpoint"); To test the HTTP server, you can use any MCP client that supports the Streamable HTTP transport. You can also configure VS Code to connect to it. Instead … |
| [39] | Once configured, you can interact with your MCP server through GitHub Copilot Chat in VS Code. When Copilot detects that an MCP server is available, it will use the tools you defined just like Claude Desktop does. Ask it to echo a message a… |
| [40] | universal sign for AI) to open the Copilot Chat pane. Open a new session and click the Configure Tools icon in the text entry field. That’s the icon with the two horizontal lines with circles on them. Can you see it in Figure 4-4 indicated … |
| [41] | Figure 6-1. Browsing MCP resources in Claude Desktop through the Connectors menu Visual Studio Code goes a step further. Open the Command Palette with Ctrl+Shift+P on Windows and Linux or Cmd+Shift+P on macOS, then search for MCP: Browse Re… |
| [42] | away. When you pick a template, VS Code prompts you for the template parameter Chapter 6 Working With resourCes102 values, for example, asking you to type a planet name, and then fetches the resolved resource. The result is attached to your… |
| [43] | Visual Studio Code takes a different approach. Prompts are surfaced as slash commands in the chat input. Type / and you will see an autocomplete menu listing all available commands, including your MCP prompts. They appear as /mcp.PromptServ… |
| [44] | host, configure the MCP server as a hosted service, and then run it. You might wonder why we use CreateEmptyApplicationBuilder instead of the more common CreateApplicationBuilder. The reason is the stdio transport. Our MCP server communicat… |
| [45] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [46] | ownership of the process’s stdin and stdout handles. Every JSON-RPC request is written to your process’s stdin as a single line of JSON, and your server writes responses to stdout the same way. The SDK handles all of this for you. You call … |
| [47] | are picked up automatically. No code changes required. For scenarios where you need live reloading without a restart, look into IOptionsMonitor<T> instead of IOptions<T>. Chapter 9 DepenDenCy InjeCtIon anD ServICe IntegratIon162 Logging You… |
| [48] | One gotcha worth knowing: if you add logging later, make sure it goes to stderr, not stdout. Any output on stdout will corrupt the JSON-RPC message stream. The hosting framework in the SDK handles this for you when you use WithStdioServerTr… |
| [49] | The LogToStandardErrorThreshold setting tells the console provider to redirect any log entry at or above the specified level to stderr. Setting it to Trace means every log message goes to stderr, which is exactly what you want for an MCP se… |
| [50] | Even with good tests, you will occasionally need to debug your MCP server. Debugging an MCP server is slightly different from debugging a typical application because the server communicates through stdin/stdout, which makes attaching a debu… |
| [51] | Version="10.0.3" /> <PackageReference Include="ModelContextProtocol" Version="1.3.0" /> </ItemGroup> </Project> Nothing exotic here. It is a standard console app with two NuGet references. The target framework is net10.0, which is required … |
| [52] | For more sophisticated logging, use the built-in ILogger infrastructure. When you register your MCP server with the host builder, you can configure logging providers Chapter 11 testing Your MCp servers213 that write to files or other destin… |
| [53] | The .NET console logging provider writes to stdout by default, which would corrupt the protocol stream. To fix this, you configure the console logger to send everything to stderr instead. The hosting framework makes this easy with a single … |
| [54] | builder.Services .AddMcpServer() .WithTools<TemperatureTools>(); The Debug logging provider writes to the diagnostic output that Visual Studio and other debuggers capture. This way you get full logging without interfering with the stdio tra… |
| [55] | For simple tools, a plain string works great. But what happens when you need to return structured data that the AI model can parse and reason about programmatically? That is where things get interesting. Returning Complex Objects If you ret… |
| [56] | refactor shared services, you need confidence that existing functionality still works. A solid test suite gives you that confidence and lets you move fast without breaking things. Unit Testing Tool Methods The simplest form of testing for a… |
| [57] | Let’s start by creating a test project. I will use MSTest because it is the testing framework built into .NET and maintained by Microsoft. Everything we cover works just as well with xUnit or NUnit. We also need the Moq library for mocking … |
| [58] | Now let’s imagine we have a tool class from one of our earlier chapters. It has a simple tool that converts temperatures between Celsius and Fahrenheit. This is Chapter 11 testing Your MCp servers197 representative of the kind of tool you w… |
| [59] | [McpServerToolType] public static class TemperatureTools { [McpServerTool] [Description("Converts Celsius to Fahrenheit")] public static string CelsiusToFahrenheit(double celsius) { var f = celsius * 9.0 / 5.0 + 32; return string.Format(Cul… |
| [60] | return string.Format(CultureInfo.InvariantCulture,"{0}F = {1:F1}C", fahrenheit, c); } } Notice that I used CultureInfo.InvariantCulture for the number formatting. This is important because your MCP server might run on machines with differen… |
| [61] | the scenes. That is the MCP protocol’s response object for any tool call. Most of the time, you never see it because the SDK creates it for you. But you can return one yourself when you need control that a simple string or object cannot giv… |
| [62] | section later in this chapter; for now, let us focus on the happy path. Just for reference, in Listing 5-4, you can see how you would build and return such a result yourself. Listing 5-4. Returning a CallToolResult for full control [McpServ… |
| [63] | return new CallToolResult { Content = [ new TextContentBlock Chapter 5 Creating tools70 { Text = json, } ], IsError = false }; } The Content property is an IList<ContentBlock> where each item is a subclass like TextContentBlock, ImageConten… |
| [64] | multiple inputs. The [DataTestMethod] approach with [DataRow] is perfect for conversion tools where you want to verify several known values. Chapter 11 testing Your MCp servers199 Testing Tools with Dependencies Static methods without depen… |
| [65] | this repository talks to a database, but in our tests, we want to control exactly what data it returns. In Listing 11-4 below, notice how the interface for the product repository is declared and then injected in the ProductTools class const… |
| [66] | if (product is null) return "Product not found."; return string.Format(CultureInfo.InvariantCulture, "{0}: ${1:F2}", product.Name, product.Price); } } Now we can use Moq to create a fake repository and test the tool in isolation. This is sh… |
| [67] | public ProductToolsTests() { _tools = new ProductTools(_mockRepo.Object); } [TestMethod] public async Task GetProduct_Exists_ReturnsFormatted() { _mockRepo.Setup(r => r .GetByIdAsync(42)) .ReturnsAsync(new Product(42, "Widget", 9.99m)); var… |
| [68] | var result = await _tools.GetProduct(99); Assert.AreEqual("Product not found.", result); } } Chapter 11 testing Your MCp servers201 This pattern scales to any dependency. Need to test a tool that calls an external API? Mock the HttpClient. … |
| [69] | there when you need it. So to recap: Start with a string return when your output is simple text. Graduate to an object return when you want automatic JSON serialization. And reach for CallToolResult when you need complete control over error… |
| [70] | Unit tests verify that your tool logic is correct, but they don’t tell you whether the full MCP pipeline works. Does the SDK correctly discover your tools? Does it serialize the request, route it to the right method, and serialize the respo… |
| [71] | method on the server side and the StreamClientTransport class on the client side let us wire up a server and client through in-process pipes. No network, no child processes, no ports to manage. It is fast, deterministic, and perfect for aut… |
| [72] | connected to the opposite ends of the pipes, and then use the client to call tools exactly the way a real AI client would. Have a look at the code for this in Listing 11-6 underneath. Listing 11-6. Integration test with stream-based transpo… |
| [73] | a random fact from an external API. The call might be slow depending on network conditions, so you definitely want to support cancellation. Listing 5-6. An async tool that fetches data from an external API [McpServerTool(Name = "fetch_rando… |
| [74] | Chapter 5 Creating tools75 response.EnsureSuccessStatusCode(); var json = await response.Content.ReadAsStringAsync(cancellat ionToken); using var doc = JsonDocument.Parse(json); var fact = doc.RootElement .GetProperty("text") .GetString(); … |
| [75] | Testing HTTP Servers If your MCP server uses the Streamable HTTP transport from Chapter 8, you have an additional testing tool at your disposal: ASP.NET Core’s WebApplicationFactory. This class, part of the Microsoft.AspNetCore.Mvc.Testing … |
| [76] | Core application in memory, including all your middleware, routing, and dependency injection. It gives you an HttpClient that sends requests directly to this in-memory server. You can then use the MCP client to connect over HTTP and call to… |
| [77] | [TestClass] public class HttpServerTests { private readonly WebApplicationFactory<Program> _factory = new(); [TestMethod] public async Task CanCallToolOverHttp() Chapter 11 testing Your MCp servers209 { var httpClient = _factory.CreateClien… |
| [78] | var tools = await client.ListToolsAsync(); Assert.IsTrue(tools.Count > 0); } } There are a few things to note about this approach. First, you need to make your Program class accessible to the test project. The easiest way is to add a partia… |
| [79] | Note httpClienttransportoptions points to the /mcp endpoint because that is where MapMcp registers the handler. the WebApplicationFactory’s CreateClient method returns an HttpClient that already knows the server’s base address, so only the … |
| [80] | true. However, the text the client sees depends on the exception type. If your exception derives from McpException, its message is included verbatim in the error response. For all other exception types (such as ArgumentException or InvalidO… |
| [81] | Testing Exception Handling Your tools should never throw unhandled exceptions at the client. The MCP SDK will catch unhandled exceptions and return a JSON-RPC error, but the error message will be generic and unhelpful to the AI model. It is… |
| [82] | McpException. Have a look at an example of this in Listing 5-8 below. Listing 5-8. Throwing an exception for invalid input [McpServerTool(Name = "divide")] [Description("Divides two numbers safely.")] public static string Divide( [Descripti… |
| [83] | verbatim to the client. The AI model sees the error message and can self-correct by asking the user for different input. Returning Error Responses Explicitly For more control, you can return a CallToolResult with IsError set to true. This l… |
| [84] | 80 Listing 5-9. Returning an error response with CallToolResult [McpServerTool(Name = "validate_email")] [Description("Validates an email address.")] public static CallToolResult ValidateEmail( [Description("The email to validate")] string … |
| [85] | Running the Server With the code in place, let’s see if it actually runs. Open a terminal in your project folder and type dotnet run And then… nothing happens. Or at least, nothing visible. The terminal just sits there with a blinking curso… |
| [86] | JSON-RPC responses. Any text that is not valid JSON-RPC would confuse the client on the other end. This is one of the things that trips up developers new to MCP. With a web API, you get a nice “Now listening on https://localhost:5000” messa… |
| [87] | special parameter and does not include it in the JSON schema. When the tool is invoked, the SDK injects an IProgress implementation that sends MCP progress notifications back to the client. The ProgressNotificationValue type has three prope… |
| [88] | Studio, VS Code, and Rider all support debugging MSTest tests natively. If you need to debug the server while it is connected to a real AI client, you can add a Debugger.Launch() call at the start of your Program.cs. This will prompt you to… |
| [89] | // ... rest of setup ... Chapter 11 testing Your MCp servers214 Tip When debugging stdio-based servers, remember that the ai client has a timeout for server startup. if you spend too long at a breakpoint during initialization, the client ma… |
| [90] | dotnet, with the run command and project path passed as Arguments. But it can be any executable. If you are connecting to a Python MCP server, the command might be python or uv. For a Node.js server, it could be npx. The transport handles a… |
| [91] | The host also plays a role in data privacy. It should never send user data to a server that the user has not explicitly authorized. If a user configures a server for weather lookups, the host should not start sending the user’s email conten… |
| [92] | That means it should be clear, specific, and actionable. Instead of “invalid input”, say, “The email address must contain an @ symbol and a dot.” Instead of “something went wrong”, say, “Could not connect to the database at localhost:5432. … |
| [93] | Listing 13-17 below. Caution Be careful what you log. search terms, customer names, and email addresses are personally identifiable information (pii). Logging them verbatim can violate gDpr, CCpa, and similar regulations. in production, con… |


---

## Q11 — Long-running tools — progress, cancellation, logging, sampling, content types

**Question:** What does the book say about long-running tools and server-to-client communication: progress notifications (progressToken, IProgress), request cancellation (CancellationToken, notifications/cancelled), server logging notifications (LoggingLevel, ILogger to client), sampling (server asking the client's LLM), and returning structured or multi-part content (text, image, embedded resource) from a tool?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 44

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, the Model Context Protocol (MCP) provides built-in mechanisms for handling long-running tool execution, real-time feedback, bidirectional communication, and rich multi-part responses [1-5].

---

###### 1. Progress Notifications (`progressToken`, `IProgress`)

For long-running tool operations (such as scanning directory trees, processing large datasets, or executing multi-step workflows), MCP includes a real-time progress notification pipeline so clients do not hang waiting on a silent spinner [3, 4].

* **Using `IProgress<ProgressNotificationValue>`**:
  * To report progress from a tool, add an `IProgress<ProgressNotificationValue>? progress = null` parameter to your tool method signature [3, 6].
  * The C# SDK recognizes this as a special runtime parameter, injects the progress handler automatically, and **excludes it from the public JSON schema** exposed to the AI client [3, 7].
  * **`ProgressNotificationValue` Properties**:
    * `Progress`: Current numerical progress value (e.g., current step or items processed) [7].
    * `Total`: Optional total value (allows clients to render percentage progress bars) [7].
    * `Message`: Optional human-readable description of the active step (e.g., `"Scanning EM signatures..."`) [7, 8].
  * **Null Safety**: Because clients are not required to request progress, `progress` may be `null`. Always use `progress?.Report(new ProgressNotificationValue { ... })` [6].

* **Manual Progress Notifications via `McpServer`**:
  * Tools can also inject `McpServer` and `RequestContext<CallToolRequestParams>` parameters [4, 9].
  * If `context.Params?.ProgressToken` is present, the tool can send raw `"notifications/progress"` notifications via `await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams { ProgressToken = progressToken.Value, ... })` [4, 10, 11].

---

###### 2. Request Cancellation (`CancellationToken`, `notifications/cancelled`)

When an AI client or end user cancels an in-flight tool call, MCP sends a `notifications/cancelled` notification to abort execution and free up resources [12-14].

* **`CancellationToken` Injection**:
  * Include `CancellationToken cancellationToken = default` as a parameter in your `async Task<T>` tool method [14].
  * The SDK injects the token automatically and excludes it from the generated JSON Schema [14].
* **Propagating Cancellation**:
  * Pass the token into every awaitable operation inside the tool method (e.g., `httpClient.GetAsync(..., cancellationToken)`, `db.SaveChangesAsync(cancellationToken)`, or `Task.Delay(..., cancellationToken)`) [15-17].
  * When the client sends `notifications/cancelled`, the token triggers, causing async operations to throw an `OperationCanceledException` and unwind cleanly without leaking background threads or database sockets [6, 18].

---

###### 3. Server Logging Notifications and Diagnostic Output

* **The `stdout` Constraint**: For standard I/O (`stdio`) servers, **`stdout` is strictly reserved for JSON-RPC protocol frames** [19-23]. Writing plain text directly to `stdout` (e.g., via `Console.WriteLine`) corrupts the stream and crashes the connection [20, 22, 23].
* **Redirecting Console Logs to `stderr`**: Diagnostic output can be written safely to standard error (`stderr`) using `Console.Error.WriteLine` or structured logging [22-24]. In `appsettings.json`, setting `"LogToStandardErrorThreshold": "Trace"` forces the .NET console logger to send all log levels to `stderr` [25, 26].
* **Server Logging Notifications (`notifications/message`)**:
  * Server log entries can also be transmitted as protocol-level notifications (`notifications/message`) containing log severity (`LoggingLevel`) and log data [27-29].
  * Injected `ILogger<T>` instances send structured log parameters (e.g., `logger.LogInformation("Processing {OrderId}", id)`) cleanly without interfering with stdout [30, 31].

---

###### 4. Sampling (Server Asking the Client's LLM)

Sampling flips the traditional MCP interaction direction: instead of the AI client calling a tool on the server, **the server asks the client to run an LLM completion on its behalf** [5, 32, 33].

* **Why Sampling Exists**: Allows servers to leverage AI intelligence (e.g., summarizing large text blocks, classifying data, or generating code) without embedding their own API keys or LLM client libraries [32-34].
* **Client-Side Opt-In**:
  * Clients must advertise `SamplingCapability` during initialization and configure a `SamplingHandler` on `McpClientOptions` [35-37].
  * If using `Microsoft.Extensions.AI`, calling `.CreateSamplingHandler()` on an `IChatClient` automatically bridges sampling requests directly to the client's underlying LLM [35, 38, 39].
* **Server-Side Invocations**:
  * Server tools call `_mcpServer.AsSamplingChatClient()` to obtain an `IChatClient` that routes completion requests back over the MCP wire via `CreateMessage` requests [34, 39].
* **Sampling with Tool Calling**:
  * In v1.0 of the SDK, sampling supports tool definitions [5, 33, 40]. The server includes tool schemas in its sampling request; the client's LLM can invoke those tools, which the server executes locally before returning the final completion [39, 40].
* **User Control**: The host application presents sampling requests to the user for explicit approval before forwarding them to the LLM [41].

---

###### 5. Returning Structured or Multi-Part Content from a Tool

While tool methods can return plain `string` values or auto-serialized JSON `objects`, returning **`CallToolResult`** gives developers explicit, low-level control over the protocol payload [2, 42-44].

* **`CallToolResult` Structure**:
  * **`Content`**: An `IList<ContentBlock>` array containing one or more rich content blocks [44, 45].
  * **`IsError`**: A boolean flag (`false` for success, `true` for domain/validation errors) [44, 45].

* **Supported Content Block Types (`ContentBlock` subclasses)**:
  1. **`TextContentBlock`**: Carries human-readable text or serialized JSON strings [44, 45].
  2. **`ImageContentBlock`**: Embeds visual image data directly in tool results via base64 encoded strings or HTTP image URIs [44, 45].
  3. **`EmbeddedResourceBlock`**: Embeds raw resource content directly alongside tool execution output [44, 45].

* **Multi-Part Responses**: A single tool call can return a mixture of blocks—such as returning a `TextContentBlock` summarizing an analysis alongside an `ImageContentBlock` containing a generated chart graphic [44, 45].

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Using Task<T> Return Types 74 CancellationToken Support 74 Progress Reporting 76 Using IProgress<ProgressNotificationValue> 76 |
| [2] | formatted JSON string with all of the properties you included. Anonymous types work great here, but you can also return a named class or record if you prefer. Both strings and objects cover the vast majority of use cases. But sometimes you … |
| [3] | Progress Reporting Some tools take a long time to run. Maybe you are scanning a directory tree, processing a large file, or running a complex analysis. Without progress reporting, the client and the user are left staring at a spinner wonder… |
| [4] | Polling is great for tools that run for minutes, but sometimes you want more granular feedback. The MCP specification includes a progress notification mechanism that lets your tool report exactly where it is in a multistep process. Clients … |
| [5] | for standardizing how your organization interacts with LLMs. A server can provide carefully crafted prompt templates, and any client can retrieve and use them without having to hardcode prompt text. Sampling with Tool Calling So far, we've … |
| [6] | structured notifications with progress values like 2 of 6 and a message such as “Scanning for EM signatures...” while the tool is running, which is much nicer than silence. And if the user decides they do not want to wait for the full scan,… |
| [7] | special parameter and does not include it in the JSON schema. When the tool is invoked, the SDK injects an IProgress implementation that sends MCP progress notifications back to the client. The ProgressNotificationValue type has three prope… |
| [8] | for (int i = 0; i < scanSteps.Length; i++) { cancellationToken.ThrowIfCancellationRequested(); progress?.Report(new ProgressNotificationValue { Progress = i + 1, Total = scanSteps.Length, Message = $"{scanSteps[i]}..." }); await Task.Delay(… |
| [9] | // Do the actual work for this step await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); } return string.Join("\n", results); } A few things to notice. First, the tool checks whether ProgressToken is present before sending notifica… |
| [10] | of seconds, so you can watch the progress arrive in real time when you connect with the MCP Inspector or watch the progress while invoking it in VS Code. The core pattern looks like Listing 8-10. Listing 8-10. Sending progress notifications… |
| [11] | for (int i = 0; i < steps.Length; i++) { if (progressToken is not null) { await server.SendNotificationAsync("notifications/progress", new ProgressNotificationParams Chapter 8 transport Layers147 { ProgressToken = progressToken.Value, Progr… |
| [12] | Tool Organization 82 Multiple Tool Classes 82 Assembly Scanning vs Manual Registration 82 Putting It All Together 84 |
| [13] | Chapter 2 MCp arChiteCture under the hood16 Notifications Notifications are fire-and-forget messages. They look like requests but they have no id field, which signals that the sender does not expect a response. MCP uses notifications for ev… |
| [14] | Task<CallToolResult>. CancellationToken Support When a client sends a cancellation request for a long-running tool, the MCP server needs a way to propagate that cancellation to your code. The SDK handles this by injecting a CancellationToke… |
| [15] | a random fact from an external API. The call might be slow depending on network conditions, so you definitely want to support cancellation. Listing 5-6. An async tool that fetches data from an external API [McpServerTool(Name = "fetch_rando… |
| [16] | Tip You might notice we’re creating an HttpClient directly here. For a quick demo, that’s fine, but in production, you should use IHttpClientFactory through dependency injection. We’ll cover that pattern in Chapter 9. Creating HttpClient in… |
| [17] | the request at any point, the token fires and the operation unwinds cleanly. Also notice the ReadOnly = true on the attribute. This tool only reads data, so we tell clients that it is safe to call without user confirmation. Chapter 5 Creati… |
| [18] | Chapter 5 Creating tools75 response.EnsureSuccessStatusCode(); var json = await response.Content.ReadAsStringAsync(cancellat ionToken); using var doc = JsonDocument.Parse(json); var fact = doc.RootElement .GetProperty("text") .GetString(); … |
| [19] | Version="10.0.3" /> <PackageReference Include="ModelContextProtocol" Version="1.3.0" /> </ItemGroup> </Project> Nothing exotic here. It is a standard console app with two NuGet references. The target framework is net10.0, which is required … |
| [20] | host, configure the MCP server as a hosted service, and then run it. You might wonder why we use CreateEmptyApplicationBuilder instead of the more common CreateApplicationBuilder. The reason is the stdio transport. Our MCP server communicat… |
| [21] | stream. CreateEmptyApplicationBuilder starts with a clean slate, no default logging providers, no surprise output on stdout, which is exactly what we need for a well- behaved stdio server. The key extension methods are provided as Microsoft… |
| [22] | JSON-RPC responses. Any text that is not valid JSON-RPC would confuse the client on the other end. This is one of the things that trips up developers new to MCP. With a web API, you get a nice “Now listening on https://localhost:5000” messa… |
| [23] | ownership of the process’s stdin and stdout handles. Every JSON-RPC request is written to your process’s stdin as a single line of JSON, and your server writes responses to stdout the same way. The SDK handles all of this for you. You call … |
| [24] | output, write to stderr or use a logging framework configured to write to a file. When to Use stdio The stdio transport is ideal when the client and server live on the same machine. Desktop AI clients like Claude Desktop, GitHub Copilot in … |
| [25] | The .NET console logging provider writes to stdout by default, which would corrupt the protocol stream. To fix this, you configure the console logger to send everything to stderr instead. The hosting framework makes this easy with a single … |
| [26] | The LogToStandardErrorThreshold setting tells the console provider to redirect any log entry at or above the specified level to stderr. Setting it to Trace means every log message goes to stderr, which is exactly what you want for an MCP se… |
| [27] | Testing Exception Handling 211 Debugging Tips 212 Use stderr for Diagnostic Output 212 Structured Logging 212 |
| [28] | server? Since the server communicates over stdio, you cannot just set a breakpoint and run it from your IDE the way you normally would. If you launch the server directly, there is no client sending requests, so your breakpoints will never b… |
| [29] | responses flowing between the Inspector and your server. The Server Notifications panel captures any notifications your server sends, such as progress updates or resource change events. Together, these panels give you full visibility into t… |
| [30] | provide. This means you can mix and match. A single method can have some parameters that come from the AI model and others that come from the DI container. The SDK handles the split automatically. Pretty neat. Take a look at the simplest ex… |
| [31] | 163 With the configuration in place, you can use ILogger<T> freely throughout your tools and services, you can see some example lines of this in Listing 9-11 below. The structured logging pattern with curly brace placeholders works perfectl… |
| [32] | majority of what MCP can do. But the protocol has a few more tricks up its sleeve that are worth knowing about, even if you will not use all of them right away. Sampling The protocol also supports something called sampling, where the server… |
| [33] | includes tool definitions, and the client's LLM can call those tools as part of its response. Nice, right? The key insight is this: the client is the one with access to the LLM. The server doesn't have that. So when the server needs a compl… |
| [34] | flows back to the server just like any other chat completion. This opens up scenarios where the server can use the client's LLM without needing its own. Elicitation Sometimes a server needs actual user input, not just an LLM completion. Tha… |
| [35] | do this by declaring the sampling capability and providing a SamplingHandler on the Handlers property of McpClientOptions. If you're already using an IChatClient from Microsoft.Extensions.AI, you can call CreateSamplingHandler() on it to ge… |
| [36] | proper mechanism for it. The v1.0 SDK introduces elicitation support. The server sends a message and optionally a URL to the client. The client can open the URL in a browser or embedded view, the user provides input, and the data flows back… |
| [37] | provide an ElicitationHandler on the Handlers property of McpClientOptions. The handler receives ElicitRequestParams and returns an ElicitResult. Listing 10-19 shows how to configure this. Chapter 10 Building an MCp Client188 Listing 10-19.… |
| [38] | The samplingHandler variable used above is created using the CreateSamplingHandler extension method on an IChatClient instance. You can create one from any chat client, such as one backed by OpenAI. IChatClient chatClient = new OpenAIClient… |
| [39] | you can use it like any other chat client. If you want the LLM to automatically invoke tools, compose it with UseFunctionInvocation(). Listing 10-18 shows how a server-side tool might use sampling. Listing 10-18. Server-side sampling with t… |
| [40] | Chapter 10 Building an MCp Client185 Figure 10-2 shows how sampling with tool calling works. The server sends a CreateMessage request with tool definitions to the client. The client forwards it to the LLM, which may request tool calls. The … |
| [41] | prompt and some context, and the client runs that through the AI model and sends the result back. Not bad, right? This opens up some interesting possibilities. A server could ask the AI to summarize a large document, classify incoming data,… |
| [42] | control you need over the response. The good news is that the SDK makes all three feel natural, so you can start simple and upgrade when you need to. Returning Strings The simplest return type is a plain string. The SDK wraps it in a text c… |
| [43] | For simple tools, a plain string works great. But what happens when you need to return structured data that the AI model can parse and reason about programmatically? That is where things get interesting. Returning Complex Objects If you ret… |
| [44] | the scenes. That is the MCP protocol’s response object for any tool call. Most of the time, you never see it because the SDK creates it for you. But you can return one yourself when you need control that a simple string or object cannot giv… |
| [45] | return new CallToolResult { Content = [ new TextContentBlock Chapter 5 Creating tools70 { Text = json, } ], IsError = false }; } The Content property is an IList<ContentBlock> where each item is a subclass like TextContentBlock, ImageConten… |


---

## Q12 — Tool safety — ToolAnnotations, descriptions, validation, security

**Question:** What guidance does the book give on tool safety and metadata: ToolAnnotations such as ReadOnlyHint, DestructiveHint, IdempotentHint and OpenWorldHint, how to write tool descriptions so the model calls the tool correctly, input validation, error messages the model can act on, and any security or authorization advice for tools that can modify data or execute code on the user's machine?

**Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 45

### Answer (verbatim NotebookLM)

In ***Building MCP Servers with C#*** by Gerald Versluis, tool safety, metadata, input validation, and security are emphasized as critical considerations because an AI model invokes server tools programmatically without human double-checking [1-4].

---

###### 1. Tool Metadata and Safety Annotations (`ToolAnnotations`)

MCP tools communicate their safety traits and UI behaviors through attribute properties and spec-defined metadata badges [13, 183–186, 206]:

* **`ReadOnly` (`ReadOnlyHint`)**:
  * Configured via `[McpServerTool(Name = "...", ReadOnly = true)]` [13, 183–185].
  * Signals to the client that the tool only reads data and produces **no side effects** [183–184].
  * AI hosts (like Claude Desktop or GitHub Copilot) use this hint to allow safe execution without prompting the user for explicit confirmation each time [183–184].
* **`Destructive` (`DestructiveHint`)**:
  * Flags tools that delete or permanently modify data (e.g., deleting database records or files) [5-7].
  * Displayed as an annotation badge in UI debuggers like the MCP Inspector [5]. AI hosts require explicit human-in-the-loop approval before running destructive tools [3, 8].
* **`Idempotent` (`IdempotentHint`)**:
  * Signals that executing the tool multiple times with identical arguments produces the exact same outcome without cumulative side effects [5].
* **`OpenWorld` (`OpenWorldHint`)**:
  * Indicates that a tool interacts with open-world or external environments (such as web search or external network APIs) [5].
* **Additional Tool Metadata**:
  * `Name`: Sets the machine-readable tool name (recommended in `snake_case`) [13, 182–183].
  * `Title`: Human-friendly display label rendered in client UIs [185–186].
  * `IconSource`: HTTP/HTTPS URL or base64 `data:` URI supplying a visual icon for client catalogs [9].
  * `TaskSupport`: Configures background task support via the `ToolTaskSupport` enum (`Forbidden`, `Optional`, `Required`) [10, 11].

---

###### 2. Writing Tool Descriptions for LLM Accuracy

The AI model decides *when* and *how* to call a tool based on the descriptions provided in C# attributes [184, 189, 232–233]:

* **Method Descriptions (`[Description("...")]`)**:
  * Explain *what* the tool accomplishes rather than internal implementation details [12].
  * Write clear, concise summaries as if explaining the tool to a human colleague [12].
* **Parameter Descriptions (`[Description("...")]`)**:
  * Apply `[Description]` to every method parameter [13].
  * The SDK uses reflection to construct the tool's public JSON Schema from parameter names, types, default values, and descriptions [189–191].
  * Explicitly describe allowed ranges, expected string formats, or valid options (for enums) to prevent the model from guessing incorrectly [13, 14].
* **Managing Context Window Overhead**:
  * Avoid registering dozens of unneeded tools on a server. Every registered tool adds its schema to the `tools/list` response, burning context window tokens and making it harder for the AI model to select the correct tool [232–233].

---

###### 3. Input Validation

* **Application-Level Validation**: While the C# SDK handles low-level JSON Schema type deserialization, developers must implement explicit application-level guardrails at the top of tool methods [14-18].
* **Validation Patterns**:
  * Check for `null` or whitespace strings [16, 17, 19].
  * Clamp numeric values using `Math.Clamp` (e.g., constraining requested item counts between 1 and 10) [15, 20-22].
  * Enforce maximum length limits on input strings to prevent resource exhaustion [16, 17].
  * Use C# `enum` parameters to restrict inputs to a fixed array of choices in the generated schema [203–207].

---

###### 4. Actionable Error Messages for AI Self-Correction

Because error messages are consumed by an AI model, they must be **clear, specific, and actionable** so the model can correct its arguments or inform the user [226–227]:

```
                     ┌──────────────────────────────────────────────┐
                     │              Incoming Tool Call              │
                     └──────────────────────┬───────────────────────┘
                                            │
                             Did an Exception occur?
                                  /                   \
                            YES  /                     \ NO
                                v                       v
                    Is it an McpException?         Validation Failed?
                      /               \              /            \
                YES  /                 \ NO    YES  /              \ NO
                    v                   v          v                v
         ┌───────────────────┐ ┌────────────────┐ ┌──────────────┐ ┌─────────────┐
         │ Message Forwarded │ │ Generic Error  │ │ CallToolResult│ │ Successful  │
         │ Verbatim to LLM   │ │ Masked by SDK  │ │ IsError = true│ │ Execution   │
         └───────────────────┘ └────────────────┘ └──────────────┘ └─────────────┘
```

* **Throwing `McpException`**:
  * If a tool throws an exception derived from `McpException`, its message is sent **verbatim** in the error response [221–222].
  * *Example*: `throw new McpException("Cannot divide by zero. Please provide a non-zero divisor.");` [23]. The LLM reads this message and can self-correct by providing a valid non-zero divisor [24].
* **SDK Exception Masking**:
  * Standard unhandled exceptions (e.g., `InvalidOperationException` or `ArgumentException`) are automatically masked by the SDK to a generic string (*"An error occurred invoking 'tool_name'"*) [25]. This deliberate security feature prevents leaking internal stack traces, connection strings, or system paths [25].
* **Returning Explicit Error Results**:
  * Alternatively, tools can return a `CallToolResult` with `IsError = true` and a `TextContentBlock` describing the validation failure [223–225].
* **Secrets Warning**: Never include connection strings, API keys, passwords, or raw stack traces in error messages [26].

---

###### 5. Security and Authorization for Sensitive Tools

When tools can modify database records, call internal APIs, or execute code on the host, the book outlines several security layers [6, 100–105, 260–270, 629–631]:

* **User Consent and Control**:
  * The host application must keep a human in the loop by requiring explicit user approval before running tools with side effects [3, 8].
* **Sandboxing and Least Privilege**:
  * Run the server process under the minimum permissions necessary [27, 28].
  * Containerized servers should run as non-root users (e.g., using .NET 10's built-in `USER app` instruction) [27-29].
  * Restrict tools to specific allowed directories, commands, or network endpoints [28].
* **Trust Boundaries & Defense in Depth**:
  * Treat all data received from external clients or LLM tool calls as untrusted input [103–104, 629]. Sanitize parameters before passing them to database queries or shell commands [18].
* **In-Tool Authorization Checks**:
  * Inject `IHttpContextAccessor` into tool methods to inspect the user's claims and enforce role-based access control (e.g., verifying `httpContext.HttpContext?.User.IsInRole("Admin")`) [6, 30].
* **OAuth Scope Enforcement in Middleware**:
  * For OAuth re-consent flows (incremental scope consent), scope checks must be executed in **ASP.NET Core middleware** before `MapMcp()`, rather than inside tool methods [264, 268–269, 588]. Streamable HTTP flushes HTTP response headers before tool execution begins; middleware runs early enough to return a `403 Forbidden` with a `WWW-Authenticate` header [31-33].
* **Data Filtering and Privacy**:
  * Filter out passwords, internal keys, and personally identifiable information (PII) before returning data to the model, as tool results become part of the model's context and log histories [27, 34-37].

---

💡 **Suggested Follow-Ups:**
1. Would you like to review how to implement unit and integration tests for error handling and tool validation?
2. Should we cover how to set up OAuth and JWT Bearer authentication (`AddMcp()`) for an ASP.NET Core MCP server?

### Citations (PDF excerpts)

| # | cited_text |
|---|---|
| [1] | Building MCP Servers with C# Connect AI Agents to Your .NET Applications with the Model Context Protocol — Gerald VersluisBuilding MCP Servers with C# Connect AI Agents to Your .NET Applications with the Model Context Protocol Gerald Verslu… |
| [2] | Running the Application 193 Summary 194 Chapter 11: Testing Your MCP Servers 195 Why Testing Matters 195 |
| [3] | When an AI model decides it wants to call a tool, the host application should present that request to the user and ask for confirmation before the call actually happens. You have probably seen this pattern in GitHub Copilot or Claude Deskto… |
| [4] | anywhere, and its output should always be treated as untrusted input. This means the host should validate and sanitize data received from the server before displaying it to the user or passing it to the AI model. Chapter 2 MCp arChiteCture … |
| [5] | ToString("HH:mm") }; Chapter 5 Creating tools73 var json = JsonSerializer.Serialize( order, new JsonSerializerOptions { WriteIndented = true }); return $"Order confirmed!\n{json}"; } } There is a lot going on in this listing, so let me walk… |
| [6] | non-text formats. You can also return a list of ResourceContents objects by using IEnumerable<ResourceContents> as your return type. This is useful when a single resource read should return multiple content blocks, like a text summary follo… |
| [7] | [McpServerResource( UriTemplate = "info://server/help", Name = "Server Help", MimeType = "text/markdown")] [Description("Returns help documentation for the resource server.")] public static string GetHelpDocument() { return """ # ResourceSe… |
| [8] | Security Model 23 User Consent and Control 23 Trust Boundaries 23 Data Privacy 24 |
| [9] | If your tool is called get_server_metrics, you could set Title to “Server Metrics” so the client shows something prettier in its UI. IconSource is even more fun. You can point it to an SVG or PNG icon, and clients will display it alongside … |
| [10] | The McpServerToolType Attribute 63 The McpServerTool Attribute 64 Describing Parameters 66 How the SDK Generates JSON Schema 66 |
| [11] | means the tool never creates tasks, that’s the default for synchronous tools. Optional means the tool can create tasks if the client requests it, the default for async tools. And Required means the tool always creates a task. Set it right o… |
| [12] | you have full control over the public contract. Next is the Description property on the System.ComponentModel.Description attribute. You place this attribute on the method itself and it tells the AI model what the tool does. Write descripti… |
| [13] | Describing Parameters Every parameter on a tool method should have a Description attribute of its own. The SDK uses these descriptions when it builds the JSON schema for the tool. That schema is what the AI model reads to understand which a… |
| [14] | because they have defaults. The two enum parameters, PizzaSize and CrustType, will show up in the JSON schema with an enum array listing every valid value, so the AI model knows exactly what strings to pass. Notice the nullable string? deli… |
| [15] | return [system, user]; } Notice the use of Math.Clamp to ensure the count stays between 1 and 10. This is a small but important detail. Even though the description tells the user to pick a number between 1 and 10, there is nothing stopping … |
| [16] | extraToppings, IOrderService orderService, IOptions<CosmicPizzaOptions> options, ILogger<OrderTool> logger) { var config = options.Value; logger.LogInformation("New order: {Pizza} to {Planet}", pizzaName, deliveryPlanet); var toppings = str… |
| [17] | return "The database is temporarily unavailable." + " Please try again in a few moments."; } } One more thing that often gets overlooked: input validation. AI models can and will pass unexpected values to your tools. An empty search term mi… |
| [18] | Here are the key areas to focus on. Input Validation Every input your MCP server receives should be validated before processing. This includes tool arguments, resource URIs, and prompt parameters. Never trust that the input is well-formed o… |
| [19] | 80 Listing 5-9. Returning an error response with CallToolResult [McpServerTool(Name = "validate_email")] [Description("Validates an email address.")] public static CallToolResult ValidateEmail( [Description("The email to validate")] string … |
| [20] | Listing 7-7. The space trivia quiz prompt [McpServerPrompt] [Description("Generates a space trivia quiz with configurable difficulty and length")] public static ChatMessage[] SpaceTriviaQuiz( [Description("Difficulty level: easy, medium, or… |
| [21] | [McpServerTool(Name = "get_space_fact")] [Description("Returns a random fascinating space fact.")] public static string GetSpaceFact() { var index = Random.Shared.Next(SpaceFacts.Length); return $"Space Fact: {SpaceFacts[index]}"; } Chapter… |
| [22] | make you " + "groan!")] public static string GetDadJoke() { var index = Random.Shared.Next(SpaceDadJokes.Length); return SpaceDadJokes[index]; } Chapter 12 Building the CosmiC pizza server235 [McpServerTool(Name = "get_space_dad_jokes")] [D… |
| [23] | McpException. Have a look at an example of this in Listing 5-8 below. Listing 5-8. Throwing an exception for invalid input [McpServerTool(Name = "divide")] [Description("Divides two numbers safely.")] public static string Divide( [Descripti… |
| [24] | verbatim to the client. The AI model sees the error message and can self-correct by asking the user for different input. Returning Error Responses Explicitly For more control, you can return a CallToolResult with IsError set to true. This l… |
| [25] | true. However, the text the client sees depends on the exception type. If your exception derives from McpException, its message is included verbatim in the error response. For all other exception types (such as ArgumentException or InvalidO… |
| [26] | That means it should be clear, specific, and actionable. Instead of “invalid input”, say, “The email address must contain an @ symbol and a dot.” Instead of “something went wrong”, say, “Could not connect to the database at localhost:5432. … |
| [27] | The project file should look like Listing 7-1 once the packages are installed. Listing 7-1. The PromptServer.csproj project file <Project Sdk="Microsoft.NET.Sdk"> <PropertyGroup> <OutputType>Exe</OutputType> <TargetFramework>net10.0</Target… |
| [28] | Your MCP server should run with the minimum permissions it needs. If it only reads files from a specific directory, do not give it write access to the entire filesystem. If it connects to a database, use a database user with only the requir… |
| [29] | Resources give your AI model eyes to see data without worrying about side effects. Tools give it hands to act. But there is a third primitive that rounds out the picture: prompts. In Chapter 7, you will learn how to expose prompts from your… |
| [30] | Chapter 13 enterprise integration patterns263 For more sophisticated scenarios, you might want per-tool authorization. Maybe most tools are fine for any authenticated user, but deleting a customer record? That should probably require admin … |
| [31] | Subscribing to Resource Changes Resources are read-only, but that does not mean they are frozen in time. The data behind a resource might change, like when a restaurant updates its menu or a server’s status shifts from healthy to degraded. … |
| [32] | Chapter 6 Working With resourCes98 public static string GetServerStatus() { var status = new { ServerName = "ResourceServer", Version = "1.0.0", Status = "Running", StartedAt = DateTime.UtcNow.ToString("o"), DotNetVersion = Environment.Vers… |
| [33] | of inside the tool method itself. This is because the Streamable HTTP transport flushes response headers (including the 200 status code) before invoking the tool. By the time a tool executes, it is too late to return a proper 401 or 403 HTT… |
| [34] | the game: every layer protects itself, and no layer assumes the others are doing their job perfectly. Data Privacy MCP servers often handle sensitive data, from database records to file contents to API keys. The protocol’s security model as… |
| [35] | click the + button below the chat input, then select your server from the connectors list. Chapter 6 Working With resourCes101 You will see all your static resources listed by name, as shown in Figure 6-1. Click one and its content is attac… |
| [36] | Listing 13-17 below. Caution Be careful what you log. search terms, customer names, and email addresses are personally identifiable information (pii). Logging them verbatim can violate gDpr, CCpa, and similar regulations. in production, con… |
| [37] | executed, and which network endpoints can be reached. Think of it as defense in depth: even if one layer fails, the others still protect your system. Data Filtering Be mindful of what data your MCP server exposes. When building resources or… |


---

## Ghi chú sử dụng

- Câu trả lời là output của NotebookLM trên PDF sách; chưa cross-check với MCP spec online. Khi ADR viện dẫn, ghi `NotebookLM Q0n [k]`.
- 3 source GitHub trong notebook bị loại khỏi scope (`-s`) để tránh trộn kiến thức repo tham khảo vào phần MCP core; repo tham khảo được cover riêng ở `revit-bridge-reference-report.md`.
- Raw JSON (answer + references + next_steps) lưu tại scratchpad session, không commit.
