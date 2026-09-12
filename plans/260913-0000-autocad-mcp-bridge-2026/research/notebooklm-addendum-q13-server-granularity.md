# NotebookLM Addendum — Q13: Server granularity & codebase reuse

**Ngày:** 2026-09-13 · **Notebook:** `5f0a5e84-6178-44d0-960d-17c0c8280121` · **Source scope:** PDF `9183ab12-e154-4302-ac92-771f70464769` ("Building MCP Servers with C#", Gerald Versluis) · **Conversation:** `00395b4f-6bb1-4353-85f4-1f6d8ecd9ce6` turn 46 · **Quota:** 1 chat dùng cho plan này (tổng 13/50 ngày).

Bổ sung cho [`../../260912-1521-dynamic-revit-mcp-server-2026/research/notebooklm-mcp-csharp-report.md`](../../260912-1521-dynamic-revit-mcp-server-2026/research/notebooklm-mcp-csharp-report.md) (Q01–Q12, không sửa file đó). Câu hỏi này phục vụ ADR-01 (tái dùng server cho AutoCAD). Trả lời giữ nguyên văn NotebookLM; trích dẫn `[Chapter, p.]` là của NotebookLM.

## Question

> According to the book, how should a developer decide the granularity of MCP servers: one server per domain/system versus one larger server exposing tools for several unrelated systems? Does the book give guidance on keeping servers focused, naming servers, tool count limits, or reusing one server codebase with different configuration (environment variables, appsettings) for different backends?

## Answer (verbatim NotebookLM)

### 1. Server Granularity: Domain-Specific vs. Monolithic Servers

The book favors creating **focused, domain-specific servers** rather than large, monolithic servers that combine unrelated systems.

* **Architectural Isolation (1:1 Client-to-Server Relationship)**: A single AI host (e.g., VS Code or Claude Desktop) can connect to multiple MCP servers simultaneously. Inside the host, a separate client instance is launched for each server:
  > *"Each client maintains a one-to-one relationship with a single server. This design keeps things simple and predictable: if a server crashes, only the client connected to it is affected, while the rest of the host continues running."* [Chapter 2, p. 12]
* **System & Domain Boundaries**: Rather than bundling database tools, file-system tools, and web-search tools into a single binary, MCP encourages separating them so each server represents a distinct backend or domain [Chapter 2, p. 12–13].
* **When to Keep Services Separate**:
  > *"If your MCP tools have fundamentally different resource requirements than your API … running them in the same process means they compete for resources... And if you are building an MCP server that will be used by multiple applications, a standalone server that any client can connect to is a cleaner architecture than embedding it in one specific application."* [Chapter 8, p. 136]

### 2. Context Window Overhead and Tool Count Limits

> *"Every tool you register ends up in the tools/list response that the client sends to the AI model. The model reads all those tool names, descriptions, and parameter schemas to decide which tool to call. If you register dozens of tools the model does not need, you are burning context window tokens and making it harder for the model to pick the right one. … being deliberate about which tools you expose can genuinely improve how well the AI selects and uses them."* [Chapter 5, p. 83–84]

> *"Resist the temptation to create a Swiss Army knife prompt that tries to cover every scenario. Instead, create multiple focused prompts, each with a clear purpose and a descriptive name."* [Chapter 7, p. 114]

> *"Fine-grained tools give the AI maximum flexibility. Coarse-grained tools reduce roundtrips … My recommendation? Provide both."* [Chapter 13, p. 272]

### 3. Server, Tool, and Resource Naming

* `ServerInfo { Name, Version }` in `AddMcpServer(options => …)` [Chapter 5, p. 65–66].
* > *"The `Name` property sets the tool name that clients will use when they call it. … I recommend always setting `Name` explicitly, so you have full control over the public contract."* [Chapter 5, p. 64]
* > *"Good resource URIs are predictable and self-describing … prefer `cosmic-pizza://menu` over `cosmic-pizza://getMenuData?format=text`."* [Chapter 6, p. 93–94]

### 4. Reusing Server Codebases via Configuration (`appsettings`, Environment Variables)

* **Strongly Typed Options (`IOptions<T>`)**: the same tool handlers can connect to different backends without modifying code [Chapter 9, p. 159–161].
* > *"Your MCP server should behave differently in development, staging, and production. The .NET configuration system makes this easy with environment-specific appsettings files. … In containerized deployments, environment variables are the preferred way to pass configuration... Avoid hardcoding any configuration values in your source code."* [Chapter 14, p. 291]
* > *"This means no duplicated configuration, no separate connection strings to manage, and no risk of business logic drifting between your API and your MCP tools. You write the logic once and expose it through both interfaces."* [Chapter 8, p. 133]

💡 **Summary (NotebookLM):** Keep servers **domain-focused** with a modest number of clearly named tools (`snake_case`). Reuse server codebases across backends using **environment variables and `IOptions<T>`**, and avoid bloated, monolithic tool sets to prevent **context window token waste**.

## Áp dụng vào plan AutoCAD (ghi chú của Claude, không phải sách)

- Sách ủng hộ **một tiến trình server = một domain** (mỗi client 1:1 với server; tool list nhỏ, tập trung) → **không** gộp tool Revit + AutoCAD vào một `tools/list`.
- Sách đồng thời ủng hộ **một codebase, nhiều cấu hình** (`IOptions<T>` + env) → cùng exe `HPRebar.Mcp.Server`, mỗi entry trong `.mcp.json` chọn host qua env (`HPREBAR_MCP_Host=autocad`) và chỉ đăng ký tool của host đó. Đây là cơ sở cho khuyến nghị ADR-01 phương án A' (host switch, một instance = một host).
- `ServerInfo.Name` nên khác nhau theo host (`HPRebar Revit MCP` / `HPRebar AutoCAD MCP`) để host AI phân biệt.
