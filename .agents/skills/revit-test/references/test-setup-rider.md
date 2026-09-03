# Setup Rider cho Revit Test Runner

Tham khảo: https://chuongmep.com/posts/2024-06-01-run-revit-unit-test.html

## Bước 1: Enable VS Code Adapter Support

`Settings` → `Build, Execution, Deployment` → `Unit Testing` → `VS Test`:
- ✅ Enable "VS Code Adapter Support"

Cần thiết để Rider discover tests từ runner third-party (RevitTest, xUnitRevit).

## Bước 2: Switch Build Engine sang Visual Studio (nếu cần)

Nếu thấy "loosely-bound" warning khi build .NET SDK:

`Settings` → `Build, Execution, Deployment` → `Toolset and Build` → `Use MSBuild version`:
- Đổi từ `.NET SDK` → `Visual Studio MSBuild`

## Bước 3: Đổi NUnit Metadata → TestRunner

Áp dụng cho `RevitTest` (ricaun-io) hoặc bất kỳ framework dùng NUnit adapter:

`Settings` → `Build, Execution, Deployment` → `Unit Testing` → `Test Frameworks` → `NUnit`:
- "Default test discovery": `TestRunner` (thay vì `Metadata`)

## Bước 4: Refresh Unit Test Tree

`Ctrl+Shift+F10` hoặc:
- `View` → `Tool Windows` → `Unit Tests` → click refresh icon

## Bước 5: Set test fixture path

Trong test class:

```csharp
protected override string FileName => @"C:\TestModels\sample.rvt";
```

→ Path tới `.rvt` test fixture. Commit file `.rvt` nhỏ (< 1 MB) vào repo `TestFixtures/` hoặc dùng absolute path local.

## Troubleshoot Rider

| Symptom | Fix |
|---|---|
| Tests không discover | Step 1–4. Check Output: `Help` → `Show Log in Explorer`. |
| Tests grey out | Build project trước; rebuild solution. |
| "Test runner exited unexpectedly" | Revit version trong `FileName` path không match config build. Check `Debug.R<XX>`. |
| Long startup khi run test (>30s) | First-run launch Revit; subsequent runs cached. |
| Test pass nhưng UI freeze | Async test → cần `async Task` + `await`, không `void`. |
