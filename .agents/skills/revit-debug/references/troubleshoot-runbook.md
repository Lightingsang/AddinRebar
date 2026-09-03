# Troubleshoot Runbook — Revit Add-In Debug

Quy trình tuần tự khi gặp bug. Đi theo flow chart, không skip.

## 1. Build fail

```
Build error?
├── "RevitAPI.dll could not be resolved"
│   → Configuration không match. Đổi sang Debug.R<XX> (XX = Revit installed).
├── "Type 'X' not found"
│   → Check #if preprocessor. API có ở version đang build không?
├── "CS0103: 'OnPropertyChanged' does not exist"
│   → Class thiếu `partial`. Hoặc CommunityToolkit.Mvvm generator không chạy → rebuild.
└── NuGet restore fail
    → dotnet restore --force-evaluate
```

## 2. Build OK nhưng F5 không launch Revit

```
F5 → nothing happens?
├── Check Properties/launchSettings.json có executable Revit path
├── <LaunchRevit>true</LaunchRevit> trong .csproj?
├── Revit version cài đúng path mặc định "C:\Program Files\Autodesk\Revit <YYYY>\Revit.exe"?
└── Fallbabs: Launch Revit thủ công + Debug → Attach to Process
```

## 3. Revit mở nhưng add-in không hiện

```
Add-in invisible?
├── Check %ProgramData%\Autodesk\Revit\Addins\<version>\
│   ├── <addin>.addin tồn tại?
│   └── DLL tồn tại?
├── Mở .addin file, verify:
│   ├── <Assembly>...</Assembly> trỏ đúng DLL path
│   ├── <FullClassName>...</FullClassName> = namespace.Application đúng
│   └── <AddInId>GUID</AddInId> unique
├── Check Revit Journal: %LocalAppData%\Autodesk\Revit\<version>\Journals\
│   → grep "MyAddIn" hoặc "Error" trong journal mới nhất
└── Bật Revit splash → có message error pop-up không?
```

## 4. Add-in load nhưng crash khi click button

```
Exception khi click button?
├── Đọc log %LocalAppData%\<addin>\logs\
│   → Có stack trace không?
├── Breakpoint vào *Command.Execute() → step debug
├── Common causes:
│   ├── DI chưa register service → Host.GetService<X>() trả null
│   ├── Transaction chưa Start() → exception "Cannot modify outside transaction"
│   ├── Element bị xóa nhưng vẫn dùng → check ElementId.IsValid()
│   └── Modeless gọi Document trực tiếp → phải qua ExternalEvent
└── Wrap try/catch + Log.Error rồi rethrow để có stack trace đầy đủ
```

## 5. UI không update khi data đổi

```
Binding broken?
├── Mở Output window khi debug → tìm "BindingExpression path error"
├── Snoop WPF: inspect DataContext của control
├── Common causes:
│   ├── Property thiếu [ObservableProperty] hoặc INotifyPropertyChanged
│   ├── Class thiếu `partial` → source generator không sinh code
│   ├── Binding path sai tên (case-sensitive)
│   ├── DataContext = null hoặc sai instance
│   └── Collection không phải ObservableCollection<T> (List<T> không raise event)
└── Fix: chuyển sang ObservableCollection, add [ObservableProperty], rebuild
```

## 6. Performance — UI freeze khi command chạy

```
UI freezes?
├── Command có async Task + await không?
├── Long-running work trong UI thread? → move sang Task.Run cho compute, await
├── Revit API call từ Task.Run → SAI! Phải qua ExternalEvent (Revit API single-thread)
└── Pattern đúng:
    [RelayCommand]
    async Task RunAsync(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            // Compute không cần Revit API → có thể Task.Run
            var data = await Task.Run(() => HeavyMath(), token);

            // Revit API call → ExternalEvent
            _externalEvent.Raise();  // handler chạy trên Revit thread
        }
        finally { IsBusy = false; }
    }
```

## 7. DLL conflict (FileLoadException)

```
"Could not load file or assembly 'X, Version=Y.Z'"?
├── Add-in khác load DLL khác version → conflict
├── Fix 1: <IsRepackable>true</IsRepackable> → ILRepack gộp deps vào 1 file
├── Fix 2: <EnableDynamicLoading>true</EnableDynamicLoading> (R25+, .NET Core)
│         → Assembly Load Context isolation
└── Fix 3: Bind redirect trong app.config (legacy .NET Framework)
```

## 8. Khi tất cả thất bại

1. **Clean state:**
   ```bash
   dotnet clean
   rm -rf bin/ obj/
   dotnet restore
   dotnet build -c Debug.R27
   ```

2. **Verify Revit Addins folder không có DLL/manifest cũ:**
   ```
   %ProgramData%\Autodesk\Revit\Addins\<version>\
   → Xóa tất cả file của add-in mình, rebuild để re-deploy.
   ```

3. **Test trên project Revit tối thiểu** (file `.rvt` rỗng) để loại trừ project-specific issue.

4. **Compare với template gốc:**
   ```bash
   mkdir test-addin && cd test-addin
   dotnet new revit-addin --addinManifestType application --addinUiWpf true --addinDiMode container --addinLogging true
   ```
   So sánh `.csproj` + `Application.cs` xem có khác gì project hiện tại.

5. **Nếu vẫn không fix:**
   - Mở issue trong [Nice3point/RevitTemplates](https://github.com/Nice3point/RevitTemplates/issues).
   - Hoặc check [Revit API forum](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160).

## Log location cheatsheet

| Loại log | Path |
|---|---|
| Add-in custom log (Serilog) | `%LocalAppData%\<addin>\logs\addin-YYYY-MM-DD.log` |
| Revit journal | `%LocalAppData%\Autodesk\Revit\<version>\Journals\journal.<N>.txt` |
| Revit add-in load log | Revit journal — grep "Loading add-in" |
| .NET assembly load errors | Event Viewer → Windows Logs → Application |
| Crash dumps | `%LocalAppData%\CrashDumps\Revit.exe.<PID>.dmp` |
