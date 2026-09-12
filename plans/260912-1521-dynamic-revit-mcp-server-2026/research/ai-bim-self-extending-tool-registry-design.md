# AI BIM tự sinh & ghi nhớ tool — thiết kế trên nền mcp-servers-for-revit

**Ngày:** 2026-09-12 · **Trạng thái:** đề xuất (chưa đổi code) · **Repo phân tích:** https://github.com/mcp-servers-for-revit/mcp-servers-for-revit (MIT, HEAD 2026-04-05) · **Nguồn đọc:** `server/src/**`, `plugin/Core/**`, `commandset/Commands/ExecuteDynamicCode/**`, `commandset/Utils/JsonSchemaGenerator.cs`, `command.json`, `package-lock.json`

---

## 1. Repo hiện tại — thực trạng (đã đọc code)

| Lớp | Hiện trạng | Hệ quả cho "tool tự sinh" |
|---|---|---|
| **MCP Server** (`server/`, TS, SDK 1.26.0, zod 3.25, better-sqlite3 12.8) | `register.ts` quét `build/tools/*.js` **1 lần lúc start**; mỗi tool = `server.tool(name, desc, zodShape, handler)`. `search_modules.ts`, `use_module.ts`, `modify_element.ts` = **file rỗng 0 byte** (ý tưởng "module" chưa làm) | Không có cách thêm tool khi đang chạy; không có registry |
| **Kết nối** (`ConnectionManager.ts`, `SocketClient.ts`) | Mỗi call mở TCP `localhost:8080` mới → gửi → đóng; mutex tuần tự; client gom `buffer` rồi `JSON.parse` cả buffer | Đơn giản, đủ dùng; nhưng plugin đọc `buffer[8192]` 1 lần → payload code dài có thể vỡ |
| **DB** (`database/db.ts`, `service.ts`) | SQLite `revit-data.db` chỉ có `projects`, `rooms` | Có sẵn hạ tầng SQLite → mở rộng thành Tool DB rẻ |
| **Plugin** (`plugin/Core/*`) | `SocketService` TCP listener; `CommandManager` nạp DLL từ `command.json` bằng `Assembly.LoadFrom` (không unload); `RevitCommandRegistry` = `Dictionary<string, IRevitCommand>`; `CommandExecutor` map JSON-RPC → command | Registry này là **registry DLL tĩnh**, không phải registry tool sinh động; không nên nhét tool sinh vào đây |
| **Executor** (`ExecuteCodeEventHandler.cs`) | Roslyn `CSharpCompilation.Emit` → `Assembly.Load(bytes)` → reflection `Execute(Document document, object[] parameters)`; reference = **toàn bộ assembly đã load** mỗi lần; **không cache**; `parameters: string[]`; transaction `auto|none`; timeout 60 s bằng `ManualResetEvent` (thread vẫn chạy tiếp); không deny-list, không dryRun, không audit; result `JsonConvert.SerializeObject(result)` (object Revit → có thể nổ/khổng lồ) | Chạy được nhưng chưa đủ "sản xuất": phải thêm typed args, cache, dryRun, guard, kết quả an toàn |
| **Schema** (`JsonSchemaGenerator.cs`) | Sinh JSON Schema từ type C# (Newtonsoft) | Tái dùng để sinh schema cho tool đề xuất từ record args |
| **Tests** (`tests/commandset/*`) | Test command cố định | Không có test cho executor / registry |

Kết luận: nền tảng đủ để mở rộng; phần thiếu nằm **hoàn toàn ở tầng server + executor**, không cần đổi giao thức TCP/JSON-RPC.

---

## 2. Kiến trúc đề xuất

```
┌──────────────┐ stdio ┌──────────────────────────────────────────────────────────────┐
│ AI client    │──────▶│ MCP Server (server/, TS)                                     │
│ (Claude…)    │◀──────│  ├─ Static tools (23 hiện có)                                 │
└──────────────┘       │  ├─ Registry meta-tools: search_tools · get_tool · propose_tool│
                       │  │    test_tool · publish_tool · run_tool · manage_tool        │
                       │  ├─ Dynamic tools (đăng ký từ registry, list_changed)         │
                       │  ├─ Resources tool://registry/{name} · Prompt toolify_run     │
                       │  └─ Tool Manager ──▶ Tool DB (SQLite+FTS5) ──▶ Tools Library  │
                       │                     (index, runs, stats)     (tools-library/  │
                       │                                               *.json + *.cs)  │
                       └───────────────┬──────────────────────────────────────────────┘
                                       │ TCP 8080 JSON-RPC (khung length-prefix)
                       ┌───────────────▼──────────────────────────────────────────────┐
                       │ Revit Plugin + CommandSet (C#)                               │
                       │  send_code_to_revit  (args JObject, dryRun, cache, guard)    │
                       │  validate_code       (compile-only + guard, không chạy)      │
                       │  + 23 command cố định giữ nguyên                             │
                       └──────────────────────────────────────────────────────────────┘
```

Nguyên tắc chốt:
1. **Tool sinh ra là dữ liệu, không phải DLL.** Một generated tool = `{metadata, inputSchema, code C#, examples}`. Chạy tool = `send_code_to_revit(code đã lưu, args)`. Executor không đổi theo từng tool → không phải nạp/unload assembly, không redeploy Revit.
2. **Registry sống ở server** (TS), không ở plugin. Plugin chỉ chạy code + validate.
3. **Files là nguồn sự thật, DB là chỉ mục.** `tools-library/<category>/<name>/tool.json + code.cs + examples.json` → review bằng git/PR; SQLite giữ index FTS + lịch sử chạy + thống kê.
4. **Publish có cổng.** draft → tested → published → (quarantined | deprecated). AI không tự publish trừ khi policy cho phép.
5. **Search-first** được ép bằng prompt + description của `send_code_to_revit` ("gọi `search_tools` trước").

---

## 3. Thành phần & file

### 3.1 Server (`server/src/`)

| File | Sửa/Thêm | Việc |
|---|---|---|
| `database/db.ts` | sửa | thêm bảng `tools`, `tool_versions`, `tool_runs`, `tool_examples`, FTS5 `tools_fts` |
| `registry/ToolRepository.ts` | thêm | CRUD, search FTS (name/description/tags/examples), thống kê |
| `registry/ToolManager.ts` | thêm | lifecycle, validate (schema, guard tĩnh, `validate_code` trong Revit), test (dryRun + real), publish policy, quarantine tự động, version bump |
| `registry/ToolLibrary.ts` | thêm | đọc/ghi `tools-library/**`; load lúc start; đối chiếu checksum với DB |
| `registry/DynamicToolRegistrar.ts` | thêm | JSON Schema → zod shape (subset: string/number/integer/boolean/enum/array/object) → `server.registerTool()`; giữ `RegisteredTool` để `remove()/update()`; `server.sendToolListChanged()` |
| `registry/StabilityScorer.ts` | thêm | successRate (cửa sổ 50 run, decay), trạng thái ⇢ quarantine khi < 0.6 sau ≥ 5 run |
| `registry/CodeInspector.ts` | thêm | regex/tree-sitter deny-list phía server (System.IO/Net/Process/Reflection, `await`, `Thread`), trích literal → gợi ý tham số |
| `tools/registry/search_tools.ts` | thêm | `{query, category?, limit?}` → top-k `{name, description, category, stability, status, inputSchema}` |
| `tools/registry/get_tool.ts` | thêm | chi tiết + code + examples + runs gần nhất |
| `tools/registry/propose_tool.ts` | thêm | `{name, description, category, inputSchema, code, examples, sourceRunId}` → validate → `draft` |
| `tools/registry/test_tool.ts` | thêm | `{name, cases:[{args, expect?}], realRun?:bool}` → chạy dryRun (+real nếu cho phép) → ghi runs → `tested` |
| `tools/registry/publish_tool.ts` | thêm | policy check → ghi library → register dynamic → list_changed |
| `tools/registry/run_tool.ts` | thêm | chạy tool theo tên (mọi trạng thái trừ deprecated) — fallback khi client chưa refresh list |
| `tools/registry/manage_tool.ts` | thêm | deprecate / restore / bump version / set category |
| `tools/send_code_to_revit.ts` | sửa | `args: object` (giữ `parameters` legacy), `dryRun`, `timeoutMs`; trả `runId`; gợi ý "reusable? → propose_tool"; description: "search_tools trước" |
| `tools/search_modules.ts`, `use_module.ts`, `modify_element.ts` | xoá | file rỗng |
| `prompts/toolify_run.ts` | thêm | prompt chuẩn hoá bước "đóng gói": tách literal → tham số, đặt tên, schema, 2 ví dụ |
| `resources/registry.ts` | thêm | `tool://registry` (danh sách), `tool://registry/{name}` (json) |
| `index.ts` | sửa | init DB → load library → register published → sau `registerTools` |
| `tools-library/README.md` + `.gitattributes` | thêm | quy ước, LF |
| `scripts/registry-cli.ts` | thêm | `list / approve / reject / export / import` cho người duyệt |
| `tests/registry/*.test.ts` | thêm | vitest: lifecycle, search, scorer, schema→zod, publish policy |
| `package.json` | sửa | thêm `vitest`; SDK 1.26 đã đủ |

### 3.2 Plugin / CommandSet (C#)

| File | Sửa/Thêm | Việc |
|---|---|---|
| `commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs` | sửa | `Execute(Document document, UIDocument uidoc, JObject args)` (giữ overload cũ); **compile cache** SHA-256 → assembly; **reference cố định** (RevitAPI, RevitAPIUI, mscorlib/System.*, Newtonsoft); `dryRun` = `TransactionGroup` rollback; `TransactionGroup` bọc mọi mode auto; serialize kết quả an toàn (ElementId→long, XYZ→{x,y,z}, Element→{id,name,category}); lỗi kèm dòng/cột |
| `commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs` | sửa | parse `args`, `dryRun`, `timeoutMs`, `toolName/toolVersion` (log) |
| `commandset/Utils/ScriptGuard.cs` | thêm | `CSharpSyntaxWalker` deny-list (System.IO/Net/Process/Reflection/Threading.Tasks, `await`, `dynamic`, `unsafe`, `Type.GetType(string)`) |
| `commandset/Commands/ExecuteDynamicCode/ValidateCodeCommand.cs` | thêm | `validate_code`: guard + compile-only, trả diagnostics; **không** chạy, không cần transaction |
| `commandset/Utils/RunAudit.cs` | thêm | JSON-lines `%AppData%\revit-mcp\audit\` (hash code, tool, args hash, kết quả, ms) |
| `plugin/Core/SocketService.cs` | sửa | khung message: **length-prefix 4 byte** hoặc newline-delimited; đọc tới đủ |
| `server/src/utils/SocketClient.ts` | sửa | khung tương ứng |
| `command.json` | sửa | thêm `validate_code` |
| `tests/commandset/ExecuteDynamicCode/*` | thêm | cache hit, dryRun rollback, guard reject, diagnostics |

---

## 4. Mô hình dữ liệu (Tool DB)

```sql
CREATE TABLE tools (
  name TEXT PRIMARY KEY,            -- slug snake_case, duy nhất
  title TEXT, description TEXT NOT NULL,
  category TEXT NOT NULL,           -- Architecture|Structure|MEP|Annotation|View|Data|Generic
  tags TEXT,                        -- json array
  status TEXT NOT NULL,             -- draft|tested|published|quarantined|deprecated
  current_version INTEGER NOT NULL,
  author TEXT,                      -- ai|human:<name>
  created_from_run_id INTEGER,      -- run send_code_to_revit gốc
  created_at INTEGER, updated_at INTEGER, published_at INTEGER, approved_by TEXT
);
CREATE TABLE tool_versions (
  name TEXT, version INTEGER, input_schema TEXT NOT NULL, code TEXT NOT NULL,
  code_sha256 TEXT NOT NULL, transaction_mode TEXT, timeout_ms INTEGER,
  revit_versions TEXT, changelog TEXT, created_at INTEGER,
  PRIMARY KEY(name, version)
);
CREATE TABLE tool_examples (id INTEGER PRIMARY KEY, name TEXT, title TEXT, args TEXT, expected TEXT, verified INTEGER);
CREATE TABLE tool_runs (
  id INTEGER PRIMARY KEY, name TEXT, version INTEGER, args_sha256 TEXT, dry_run INTEGER,
  success INTEGER, duration_ms INTEGER, error TEXT, revit_version TEXT, doc_title TEXT, ts INTEGER
);
CREATE VIRTUAL TABLE tools_fts USING fts5(name, description, tags, examples, content='');
```
Thống kê suy ra (view): `runs`, `success_rate` (50 run gần nhất), `last_run`, `last_error`, `stability = success_rate × min(1, runs/10)`.

`tools-library/Structure/create_column_grid/`:
```
tool.json       {name, title, description, category, tags, status, version, inputSchema, transactionMode, timeoutMs, revitVersions, author, approvedBy}
code.cs         thân hàm Execute — dùng args["x"].Value<double>() …
examples.json   [{title, args, expected}]
```

---

## 5. Luồng hoạt động chi tiết

### 5.1 Có tool sẵn (hit)
1. AI nhận nhiệm vụ → gọi `search_tools {query:"tạo lưới cột 6x4 bước 8000", category:"Structure"}`.
2. Server: FTS5 + lọc `status=published` + xếp hạng `bm25 × stability`. Trả top 5 kèm `inputSchema`.
3. AI thấy `create_column_grid` phù hợp → gọi thẳng tool đó (đã có trong `tools/list`) hoặc `run_tool {name, args}`.
4. Server: `ToolManager.run` → lấy `code` version hiện tại → `send_code_to_revit {code, args, dryRun:false, toolName, toolVersion}` → plugin cache hit → chạy.
5. Ghi `tool_runs`; cập nhật stability; nếu lỗi liên tiếp → quarantine + `sendToolListChanged`.

### 5.2 Chưa có tool (miss) → sinh → đóng gói → duyệt → publish
1. `search_tools` không có kết quả đủ điểm (< ngưỡng) → AI tự viết C#.
2. `send_code_to_revit {code, args, dryRun:true}` → OK → chạy thật → `{runId: 118, success:true, result…}` + gợi ý: *"Reusable? Call `propose_tool` with sourceRunId 118."* + `suggestedParameters` (literal server trích: `8000`, `"C_300x300"`, `6`, `4`).
3. AI (client LLM, dùng prompt `toolify_run`) tổng quát hoá: thay literal bằng `args[...]`, đặt tên `create_column_grid`, viết description, `inputSchema`, 2 ví dụ.
4. `propose_tool {...}` → Tool Manager:
   - tên slug duy nhất, chưa trùng (FTS similarity > 0.9 → từ chối, gợi ý cập nhật tool cũ);
   - `inputSchema` là JSON Schema hợp lệ (subset hỗ trợ);
   - `CodeInspector` deny-list; mọi property của schema phải xuất hiện `args["<prop>"]` trong code; code không còn literal "nghi ngờ" (số > 1 chữ số không gắn hằng);
   - `validate_code` trong Revit: compile-only + guard → diagnostics;
   → lưu `draft` v1 + `created_from_run_id`.
5. `test_tool {name, cases:[ex1, ex2], realRun:false}` → mỗi case chạy `dryRun:true` (rollback) → ghi runs → tất cả pass → `tested`. Tuỳ policy chạy thêm 1 case real trên doc scratch.
6. **Cổng duyệt** (`publish_tool`):
   - policy `manual` (mặc định): server tạo `review-<name>.md` (code diff, schema, kết quả test) → người dùng `registry-cli approve <name>` (hoặc sửa `tool.json.status`) → chỉ khi đó `publish_tool` thành công;
   - policy `auto`: yêu cầu `tested` + ≥ N run dryRun pass + không vi phạm guard → publish ngay (dành cho môi trường sandbox).
7. Publish: ghi `tools-library/…`, `registerTool` động, `sendToolListChanged` → client thấy tool mới **ngay trong phiên**.
8. Về sau: mỗi run cập nhật stability; `manage_tool deprecate` khi thay thế; version bump khi sửa code (giữ lịch sử).

### 5.3 Khởi động server
`initializeDatabase()` → `ToolLibrary.loadAll()` (đối chiếu checksum, DB ← files) → `DynamicToolRegistrar.registerAll(status=published)` → tools tĩnh → stdio.

---

## 6. Auto Tool Generation — ai làm gì

| Bước | Ai | Cơ chế |
|---|---|---|
| Nhận diện "đáng làm tool" | client LLM | gợi ý trong kết quả `send_code_to_revit` + prompt `toolify_run`; heuristic server: code ≥ 15 dòng, có vòng lặp/tham số, chạy thành công ≥ 1 lần thật |
| Trích tham số | server (`CodeInspector`) + LLM | server liệt kê literal & vị trí; LLM quyết định tên/kiểu/mô tả |
| Viết schema/ví dụ | LLM | theo prompt; server validate |
| Kiểm chứng | server + Revit | guard tĩnh → `validate_code` → dryRun các ví dụ |
| Duyệt | người | CLI/`tool.json`; policy `auto` chỉ khi bật |
| Đăng ký | server | `registerTool` + `list_changed` |

Tuỳ chọn phase sau: "toolifier" phía server gọi LLM API (Claude) để đóng gói không cần client — tăng chi phí/khoá API, chỉ bật khi cần chạy batch.

---

## 7. Tool Memory — tìm kiếm & ổn định
- **Tìm kiếm:** FTS5 trên name/description/tags/examples (rẻ, offline). Phase 2: embedding (sqlite-vec hoặc API) khi > ~200 tool.
- **Xếp hạng:** `bm25 × (0.5 + 0.5·stability) × statusWeight` (published 1.0, tested 0.6, draft 0.2, quarantined 0).
- **Ổn định:** cửa sổ 50 run; quarantine khi success < 60% và ≥ 5 run; tự restore khi người duyệt sửa + test pass.
- **Tương thích Revit:** `revit_versions` từ runs thật; `search_tools` lọc theo version Revit đang kết nối.

---

## 8. An toàn
- Deny-list 2 lớp (server regex + plugin Roslyn walker); `validate_code` không chạy code.
- dryRun bắt buộc trong `test_tool`; run thật chỉ khi người/policy cho.
- Audit JSON-lines phía plugin; `tool_runs` phía server.
- Publish = quyết định người (mặc định). AI chỉ đề xuất.
- Tool chỉ là text → không nạp DLL lạ vào Revit.

---

## 9. Lộ trình (ước lượng)
| Phase | Việc | Effort |
|---|---|---|
| A | Executor hardening (args, cache, dryRun, guard, framing) + `validate_code` | 16 h |
| B | Tool DB + Library + ToolManager + 7 meta-tool + registrar động | 24 h |
| C | Prompt/resources/CLI duyệt/scorer/quarantine + vitest | 12 h |
| D | Chạy thật: 3 kịch bản end-to-end (miss→propose→test→approve→publish→hit) | 8 h |

---

## 10. Áp dụng lên HPRebar MCP bridge (so sánh)

| Khả năng cần | mcp-servers-for-revit (sau khi sửa) | HPRebar bridge hiện có |
|---|---|---|
| Chạy code động | có (cần hardening phase A) | **có sẵn**: guard, cache SHA-256, dryRun, timeout, audit, Undo group |
| `args` typed | phải thêm | phase 6 đã thiết kế (`ScriptArgs`) |
| Registry/DB/Library/meta-tool | phải thêm (TS) | phải thêm (C#, SQLite qua `Microsoft.Data.Sqlite`, `McpServerPrimitiveCollection` hỗ trợ list_changed) |
| Multi-version | R20–R26 | R25/R26 |
| Ngôn ngữ | TS + C# | C# thuần |

Phase A gần như miễn phí trên HPRebar bridge. Phần registry (B–C) là code server, ngôn ngữ nào cũng ~tương đương. Chọn nền tảng = quyết định sản phẩm của người dùng.
