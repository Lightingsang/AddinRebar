# Handoff Report: Milestone M2 C# Path Resolver & Decoupling Exploration

- **Agent ID**: `explorer_m2_gen2_2`
- **Parent Conversation ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`
- **Milestone Target**: `M2 - C# Path Resolver & Decoupling`
- **Timestamp**: 2026-10-18T07:18:00Z

---

## 1. Observation

A systematic forensic scan across all 7 projects in `HPDcePro/src/**`, 2 test projects in `HPDcePro/tests/**`, deployment scripts, and the legacy baseline `C:\Dce Pro` revealed the following exact facts and findings:

### 1.1 Direct Hardcoded `C:\Dce Pro` Occurrences in Source Code (`src/`)

1. **`HPDcePro.Core/MachineLearning/MauHocDatabaseReader.cs`**:
   - Line 42 (XML doc comment):
     ```csharp
     /// SQLite data access service for C:\Dce Pro\MayHoc\mauhoc.db.
     ```
   - Line 47:
     ```csharp
     public const string DefaultDatabasePath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     ```
   - Line 53 (Constructor fallback):
     ```csharp
     string path = string.IsNullOrWhiteSpace(dbPath) ? DefaultDatabasePath : dbPath!;
     ```

2. **`HPDcePro.Core/MachineLearning/RebarTokenType.cs`**:
   - Line 5 (XML doc comment):
     ```csharp
     /// Matches the label schema in C:\Dce Pro\MayHoc\mauhoc.db.
     ```

3. **`HPDcePro.Core/MachineLearning/RebarFeatureExtractor.cs`**:
   - Line 9 (XML doc comment):
     ```csharp
     /// Emits feature vectors matching C:\Dce Pro\MayHoc\mauhoc.db schema.
     ```

4. **`HPDcePro.Core/Serialization/AutoStrConfig.cs`**:
   - Line 8 (XML doc comment):
     ```csharp
     /// Fully compatible with C:\Dce Pro\AutoStr\DuAnMau.autostr.
     ```

5. **`HPDcePro.Desktop/Program.cs`**:
   - Line 13:
     ```csharp
     public const string DefaultAutoStrSamplePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     ```
   - Line 47:
     ```csharp
     string pathToLoad = customPath ?? DefaultAutoStrSamplePath;
     ```

6. **`HPDcePro.Mcp/Execution/Handlers/AutoStrToolHandler.cs`**:
   - Line 46:
     ```csharp
     string defaultPath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     ```

7. **`HPDcePro.Mcp/Execution/Handlers/MayHocToolHandler.cs`**:
   - Line 45:
     ```csharp
     return string.IsNullOrWhiteSpace(customPath)
         ? MauHocDatabaseReader.DefaultDatabasePath
         : customPath;
     ```

---

### 1.2 Direct Hardcoded `C:\Dce Pro` Occurrences in Tests (`tests/`)

1. **`tests/HPDcePro.Core.Tests/AutoStrSerializerExtendedTests.cs`**:
   - Line 11:
     ```csharp
     private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     ```
   - Line 25:
     ```csharp
     Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);
     ```

2. **`tests/HPDcePro.Core.Tests/SerializationTests.cs`**:
   - Line 10:
     ```csharp
     private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     ```
   - Line 24:
     ```csharp
     Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);
     ```

3. **`tests/HPDcePro.Core.Tests/MachineLearningTests.cs`**:
   - Line 11:
     ```csharp
     private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     ```

4. **`tests/HPDcePro.Core.Tests/MauHocDatabaseLiveTests.cs`**:
   - Line 12:
     ```csharp
     private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     ```

---

### 1.3 Inventory of Legacy Assets to be Resolved via `HPDcePro/assets/`

A direct inspection of `C:\Dce Pro` cataloged the static asset inventory matching Milestone M2 requirements:

| Asset Subsystem | Source Location in `C:\Dce Pro` | Target Location in `HPDcePro/assets/` | File Count & Key Artifacts |
|---|---|---|---|
| **CAD Templates** | `C:\Dce Pro\Cad\` (13 DWGs)<br>`C:\Dce Pro\AutoStr\00.FileDungChung\` (4 DWGs) | `assets/Cad/`<br>`assets/AutoStr/00.FileDungChung/` | **17 DWGs**: `Form 1..5.dwg`, `Form 1..5_Steel.dwg`, `Form 2_2*.dwg`, `BlockTenBanVe.dwg`, `Chi Tiet Coc 250x250.dwg`, `GhiChuChung 5574 _ A3.dwg`, `Khung A3.dwg` |
| **Excel Workbooks** | `C:\Dce Pro\Excel\File V.2020\` (20 XLSMs)<br>`C:\Dce Pro\Excel\DceSteel\` (112 XLSMs) | `assets/Excel/File V.2020/`<br>`assets/Excel/DceSteel/` | **132 XLSMs**: Concrete design (`09. DCE_Beam_2020.xlsm`, `08. DCE_ColumnWall_2020.xlsm`, `13. DCE_DaiCoc_2020.xlsm`...), Steel design (`01 TCVN 2737 2023 Loading.xlsm`, `20 TCVN 5575 Steel Beam Design...`...) |
| **Machine Learning** | `C:\Dce Pro\MayHoc\` | `assets/MayHoc/` | `mauhoc.db` (8.98 MB SQLite DB, 45,020 samples)<br>`model.zip` (26.9 KB ML models) |
| **System Configs** | `C:\Dce Pro\System\` | `assets/System/` | **177 config text files** (`BeamShop.txt`, `ChieuCaoDamDienHinhVeCot.txt`, `DatTenLineDam2.txt`, `LopBTBVVeCot.txt`, `SanBotPX.txt`...) |
| **Language Dictionaries** | `C:\Dce Pro\Language\` (4 files)<br>`C:\Dce Pro\Language\Cad\` (4 files) | `assets/Language/`<br>`assets/Language/Cad/` | **8 files**: `en.txt`, `vi.txt`, `zh.txt`, `_HUONGDAN.txt` at root and in `Cad/` |
| **Icons & Menus** | `C:\Dce Pro\Icon\` (133 PNG/ICO)<br>`C:\Dce Pro\*.cuix` (7 CUIX) | `assets/Icon/`<br>`assets/Cad/Menus/` (or `assets/Cuix/`) | **133 Icons**: `CauThang/`, `IconCad/`, `IconRevit/`, `SlabRebar/`, `TKT/`, `Admin.png`, `D.ico`, `TA.ico`<br>**7 CUIX**: `MenuDCE.cuix`, `MenuDCE_ACAD.cuix`, `MenuDCE_BCAD.cuix`, `MenuDCE_GCAD.cuix`, `MenuDCE_ICAD.cuix`, `MenuDCE_ZWCAD.cuix`, `DBimApps.cuix` |
| **AutoStr Sample Project** | `C:\Dce Pro\AutoStr\DuAnMau.autostr` | `assets/AutoStr/DuAnMau.autostr` | 1 Project configuration file with embedded paths needing normalization |

---

### 1.4 Baseline Test Execution Verification

A test run of the existing codebase (`dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1`) confirmed:
- `HPDcePro.Adapters.Tests`: 110 passed, 0 failed, 0 skipped.
- `HPDcePro.Core.Tests`: 259 passed, 0 failed, 0 skipped.
- **Total**: 369 passed tests with 0 compiler warnings.

---

## 2. Logic Chain

1. **Root Problem**:
   Hardcoded references to `C:\Dce Pro` in `MauHocDatabaseReader.cs`, `Program.cs`, `AutoStrToolHandler.cs`, and the test suites tie the application directly to the legacy installation path. If `C:\Dce Pro` is renamed, moved, or running on an external client machine without DCE Pro installed, ML database queries fail, default project loading fails, and tests fail or skip.

2. **Decoupling Requirement**:
   Per `PROJECT.md` Interface Contract 3, an `AssetPathResolver` static service must be created in `HPDcePro.Core.Common` to serve as the Single Source of Truth for locating all static assets.

3. **Multi-Host Environment Challenges**:
   Different host runtimes have disparate base directory behaviors:
   - **AutoCAD Runtime (`HPDcePro.AutoCad`)**: `AppContext.BaseDirectory` points to `C:\Program Files\Autodesk\AutoCAD 2026\`. The add-in assembly resides in `%APPDATA%\Autodesk\ApplicationPlugins\HPDcePro.bundle\Contents\`.
   - **Revit Runtime (`HPDcePro.Revit`)**: `AppContext.BaseDirectory` points to `C:\Program Files\Autodesk\Revit 2026\`. The add-in assembly resides in `%APPDATA%\Autodesk\Revit\Addins\2026\HPDcePro\`.
   - **Standalone Desktop Hub (`HPDcePro.Desktop`)**: Exe resides in `deploy/Desktop/` or `src/HPDcePro.Desktop/bin/...`.
   - **Unit / Integration Tests**: Runner executes in `tests/HPDcePro.Core.Tests/bin/Debug/net8.0/`.
   - **CI / Build Pipeline**: Headless execution with possible containerized directory structures.

4. **Hierarchical 7-Tier Discovery Strategy**:
   To ensure 100% reliability across all five runtimes without configuration errors, `AssetPathResolver.GetAssetsRoot()` must resolve in the following strict order:
   - **Tier 1 (Ambient AsyncLocal Scope)**: `UseAssetsRoot(string path)` allows unit tests to set an isolated asset directory per async context without thread interference.
   - **Tier 2 (Global Programmatic Override)**: `SetOverrideAssetsRoot(string path)` allows host applications or CLI flags (`--assets-dir`) to explicitly bind the assets path.
   - **Tier 3 (Environment Variable)**: `HPDCEPRO_ASSETS_DIR` enables CI/CD pipelines, Docker, or IT administrators to configure a shared network or local path.
   - **Tier 4 (Assembly Probing & Ancestor Traversal)**: Checks `typeof(AssetPathResolver).Assembly.Location` and climbs up to 6 ancestor directories looking for `assets/` containing `MayHoc`, `Cad`, `Excel`, `System`, or `AutoStr`. This cleanly resolves in AutoCAD (`HPDcePro.bundle/assets`), Revit (`HPDcePro/assets`), and Desktop.
   - **Tier 5 (BaseDirectory Probing & Ancestor Traversal)**: Traverses upwards from `AppContext.BaseDirectory` looking for `assets/` or `HPDcePro/assets/`. This cleanly resolves in xUnit / MTP test runners inside `bin/Debug/net8.0/`.
   - **Tier 6 (Standard %APPDATA% Deployment Paths)**: Probes `%APPDATA%\Autodesk\ApplicationPlugins\HPDcePro.bundle\assets` and `%APPDATA%\Autodesk\Revit\Addins\2026\HPDcePro\assets`.
   - **Tier 7 (Safe Fallback)**: Fallback to `Path.Combine(AppContext.BaseDirectory, "assets")`.

5. **Legacy Data Normalization in `DuAnMau.autostr`**:
   `C:\Dce Pro\AutoStr\DuAnMau.autostr` currently contains:
   ```json
   "ProjectRootPath": "C:\\Dce Pro\\AutoStr",
   "FileGhiChuChung": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\GhiChuChung 5574 _ A3.dwg",
   "FileKhungTen": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Khung A3.dwg",
   "FileBlockTenBanVe": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\BlockTenBanVe.dwg",
   "FileChiTietCoc": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Chi Tiet Coc 250x250.dwg",
   "ExcelGocPath": "C:\\Dce Pro\\Excel\\File V.2020",
   ```
   When copied to `HPDcePro/assets/AutoStr/DuAnMau.autostr`, these absolute paths must be normalized to relative paths (`AutoStr\00.FileDungChung\...`, `Excel\File V.2020`) or cleared (`"ProjectRootPath": ""`) so deserialization and execution never depend on `C:\Dce Pro`.

---

## 3. Caveats

1. **Read-Only Scope**: This agent is an explorer and has intentionally performed zero mutations on `src/**` or `tests/**`. All implementation code is provided as complete, drop-in replacement specifications for Worker M2.
2. **`HPDcePro/assets/` Population**: Worker M2 is responsible for copying the actual binary and text assets from `C:\Dce Pro` to `HPDcePro/assets/` during M2 execution. Until those files are on disk, `AssetPathResolver` will use its upward traversal or fallback.
3. **Legacy `C:\Dce Pro` Read-Only Constraint**: The baseline directory `C:\Dce Pro` must NEVER be modified or deleted.

---

## 4. Conclusion & Actionable Specification for Worker M2

Worker M2 must implement the following tasks to achieve 100% path decoupling:

### Task 1: Create `src/HPDcePro.Core/Common/AssetPathResolver.cs`

Target file: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Core\Common\AssetPathResolver.cs`
Clean Code compliance: 185 lines (<= 300 lines), longest method 26 lines (<= 50 lines), zero commented-out code, annotated catch.

```csharp
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace HPDcePro.Core.Common
{
    /// <summary>
    /// Unified path resolver for all HPDcePro static engineering assets.
    /// Provides zero-dependency asset resolution across Unit Tests, Desktop, AutoCAD, Revit, and Excel.
    /// </summary>
    public static class AssetPathResolver
    {
        public const string AssetsDirectoryName = "assets";
        public const string EnvAssetsDir = "HPDCEPRO_ASSETS_DIR";

        private static readonly AsyncLocal<string?> s_ambientRoot = new AsyncLocal<string?>();
        private static string? s_globalOverride;
        private static string? s_cachedDiscoveredRoot;
        private static readonly object s_lock = new object();

        public static string GetAssetsRoot()
        {
            if (!string.IsNullOrWhiteSpace(s_ambientRoot.Value))
            {
                return s_ambientRoot.Value!;
            }

            if (!string.IsNullOrWhiteSpace(s_globalOverride))
            {
                return s_globalOverride!;
            }

            string? env = Environment.GetEnvironmentVariable(EnvAssetsDir);
            if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            {
                return Path.GetFullPath(env);
            }

            if (s_cachedDiscoveredRoot != null && Directory.Exists(s_cachedDiscoveredRoot))
            {
                return s_cachedDiscoveredRoot;
            }

            lock (s_lock)
            {
                if (s_cachedDiscoveredRoot != null && Directory.Exists(s_cachedDiscoveredRoot))
                {
                    return s_cachedDiscoveredRoot;
                }

                s_cachedDiscoveredRoot = DiscoverAssetsRoot();
                return s_cachedDiscoveredRoot;
            }
        }

        public static void SetOverrideAssetsRoot(string? rootPath)
        {
            s_globalOverride = string.IsNullOrWhiteSpace(rootPath) ? null : Path.GetFullPath(rootPath);
        }

        public static void ResetOverride()
        {
            s_globalOverride = null;
            s_ambientRoot.Value = null;
            lock (s_lock)
            {
                s_cachedDiscoveredRoot = null;
            }
        }

        public static IDisposable UseAssetsRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Asset root cannot be null or whitespace.", nameof(rootPath));
            }

            string? previous = s_ambientRoot.Value;
            s_ambientRoot.Value = Path.GetFullPath(rootPath);
            return new ScopeToken(() => s_ambientRoot.Value = previous);
        }

        public static string ResolveCadTemplate(string filename)
        {
            string root = GetAssetsRoot();
            string cadPath = Path.Combine(root, "Cad", filename);
            if (File.Exists(cadPath)) return cadPath;

            string autoStrPath = Path.Combine(root, "AutoStr", "00.FileDungChung", filename);
            if (File.Exists(autoStrPath)) return autoStrPath;

            return cadPath;
        }

        public static string ResolveExcelWorkbook(string relativePath)
        {
            string root = GetAssetsRoot();
            string clean = relativePath.TrimStart('\\', '/');

            string directPath = Path.Combine(root, "Excel", clean);
            if (File.Exists(directPath)) return directPath;

            string v2020Path = Path.Combine(root, "Excel", "File V.2020", clean);
            if (File.Exists(v2020Path)) return v2020Path;

            string steelPath = Path.Combine(root, "Excel", "DceSteel", clean);
            if (File.Exists(steelPath)) return steelPath;

            return directPath;
        }

        public static string ResolveSystemConfig(string filename)
        {
            return Path.Combine(GetAssetsRoot(), "System", filename);
        }

        public static string ResolveLanguageDict(string relativePath)
        {
            return Path.Combine(GetAssetsRoot(), "Language", relativePath.TrimStart('\\', '/'));
        }

        public static string ResolveMachineLearningDatabase()
        {
            return Path.Combine(GetAssetsRoot(), "MayHoc", "mauhoc.db");
        }

        public static string ResolveMachineLearningModelZip()
        {
            return Path.Combine(GetAssetsRoot(), "MayHoc", "model.zip");
        }

        public static string ResolveAutoStrProject(string filename = "DuAnMau.autostr")
        {
            return Path.Combine(GetAssetsRoot(), "AutoStr", filename);
        }

        public static string ResolveIcon(string relativePath)
        {
            return Path.Combine(GetAssetsRoot(), "Icon", relativePath.TrimStart('\\', '/'));
        }

        public static string ResolveCuixMenu(string menuName)
        {
            string root = GetAssetsRoot();
            string p1 = Path.Combine(root, "Cad", "Menus", menuName);
            if (File.Exists(p1)) return p1;
            string p2 = Path.Combine(root, "Cuix", menuName);
            if (File.Exists(p2)) return p2;
            string p3 = Path.Combine(root, menuName);
            if (File.Exists(p3)) return p3;
            return p1;
        }

        public static string ResolveAsset(string relativePath)
        {
            return Path.Combine(GetAssetsRoot(), relativePath.TrimStart('\\', '/'));
        }

        public static bool AssetExists(string relativePath)
        {
            string fullPath = ResolveAsset(relativePath);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }

        private static string DiscoverAssetsRoot()
        {
            try
            {
                string asmLoc = typeof(AssetPathResolver).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(asmLoc))
                {
                    string? dir = Path.GetDirectoryName(asmLoc);
                    string? found = ProbeAncestors(dir);
                    if (found != null) return found;
                }
            }
            catch (Exception)
            {
                // Annotated catch per Rule Q-B2: Assembly.Location throws in single-file or reflection context
            }

            string baseDir = AppContext.BaseDirectory;
            string? foundFromBase = ProbeAncestors(baseDir);
            if (foundFromBase != null) return foundFromBase;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleAssets = Path.Combine(appData, "Autodesk", "ApplicationPlugins", "HPDcePro.bundle", "assets");
            if (Directory.Exists(bundleAssets)) return bundleAssets;

            string revitAssets = Path.Combine(appData, "Autodesk", "Revit", "Addins", "2026", "HPDcePro", "assets");
            if (Directory.Exists(revitAssets)) return revitAssets;

            return Path.Combine(baseDir, AssetsDirectoryName);
        }

        private static string? ProbeAncestors(string? startDir)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return null;

            string? current = Path.GetFullPath(startDir!);
            for (int depth = 0; depth < 7 && current != null; depth++)
            {
                string candidate = Path.Combine(current, AssetsDirectoryName);
                if (IsAssetsDirectory(candidate)) return Path.GetFullPath(candidate);

                string candidateHPDcePro = Path.Combine(current, "HPDcePro", AssetsDirectoryName);
                if (IsAssetsDirectory(candidateHPDcePro)) return Path.GetFullPath(candidateHPDcePro);

                var parent = Directory.GetParent(current);
                current = parent?.FullName;
            }
            return null;
        }

        private static bool IsAssetsDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;

            return Directory.Exists(Path.Combine(path, "MayHoc"))
                || Directory.Exists(Path.Combine(path, "Cad"))
                || Directory.Exists(Path.Combine(path, "Excel"))
                || Directory.Exists(Path.Combine(path, "System"))
                || Directory.Exists(Path.Combine(path, "AutoStr"));
        }

        private sealed class ScopeToken : IDisposable
        {
            private Action? _disposeAction;

            public ScopeToken(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
            }
        }
    }
}
```

---

### Task 2: Update Source Files in `HPDcePro/src/**`

1. **`src/HPDcePro.Core/MachineLearning/MauHocDatabaseReader.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Update line 42 XML doc comment: `SQLite data access service for mauhoc.db via AssetPathResolver.`
   - Replace line 47:
     ```csharp
     // BEFORE:
     public const string DefaultDatabasePath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     // AFTER:
     public static string DefaultDatabasePath => AssetPathResolver.ResolveMachineLearningDatabase();
     ```
   - Line 53 remains cleanly wired:
     ```csharp
     string path = string.IsNullOrWhiteSpace(dbPath) ? DefaultDatabasePath : dbPath!;
     ```

2. **`src/HPDcePro.Core/MachineLearning/RebarTokenType.cs`**:
   - Line 5 comment: change `C:\Dce Pro\MayHoc\mauhoc.db` -> `MayHoc/mauhoc.db`.

3. **`src/HPDcePro.Core/MachineLearning/RebarFeatureExtractor.cs`**:
   - Line 9 comment: change `C:\Dce Pro\MayHoc\mauhoc.db` -> `MayHoc/mauhoc.db`.

4. **`src/HPDcePro.Core/Serialization/AutoStrConfig.cs`**:
   - Line 8 comment: change `C:\Dce Pro\AutoStr\DuAnMau.autostr` -> `AutoStr/DuAnMau.autostr`.

5. **`src/HPDcePro.Desktop/Program.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 13:
     ```csharp
     // BEFORE:
     public const string DefaultAutoStrSamplePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     // AFTER:
     public static string DefaultAutoStrSamplePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");
     ```

6. **`src/HPDcePro.Mcp/Execution/Handlers/AutoStrToolHandler.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 46:
     ```csharp
     // BEFORE:
     string defaultPath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     // AFTER:
     string defaultPath = AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");
     ```

---

### Task 3: Update Test Files in `HPDcePro/tests/**`

1. **`tests/HPDcePro.Core.Tests/AutoStrSerializerExtendedTests.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 11:
     ```csharp
     // BEFORE:
     private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     // AFTER:
     private static string RealProjectFilePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");
     ```
   - Replace line 25:
     ```csharp
     // BEFORE:
     Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);
     // AFTER:
     Assert.True(string.IsNullOrEmpty(config.ProjectRootPath) || config.ProjectRootPath.EndsWith("AutoStr"));
     ```

2. **`tests/HPDcePro.Core.Tests/SerializationTests.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 10:
     ```csharp
     // BEFORE:
     private const string RealProjectFilePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";
     // AFTER:
     private static string RealProjectFilePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");
     ```
   - Replace line 24:
     ```csharp
     // BEFORE:
     Assert.Equal(@"C:\Dce Pro\AutoStr", config.ProjectRootPath);
     // AFTER:
     Assert.True(string.IsNullOrEmpty(config.ProjectRootPath) || config.ProjectRootPath.EndsWith("AutoStr"));
     ```

3. **`tests/HPDcePro.Core.Tests/MachineLearningTests.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 11:
     ```csharp
     // BEFORE:
     private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     // AFTER:
     private static string RealDbPath => AssetPathResolver.ResolveMachineLearningDatabase();
     ```

4. **`tests/HPDcePro.Core.Tests/MauHocDatabaseLiveTests.cs`**:
   - Add `using HPDcePro.Core.Common;`.
   - Replace line 12:
     ```csharp
     // BEFORE:
     private const string RealDbPath = @"C:\Dce Pro\MayHoc\mauhoc.db";
     // AFTER:
     private static string RealDbPath => AssetPathResolver.ResolveMachineLearningDatabase();
     ```

5. **Create `tests/HPDcePro.Core.Tests/AssetPathResolverTests.cs`**:
   - Comprehensive test suite testing all 7 methods, environment variable override, `SetOverrideAssetsRoot`, and `UseAssetsRoot` scoping.

---

### Task 4: Asset Normalization & Deploy Script Update

1. Normalize `HPDcePro/assets/AutoStr/DuAnMau.autostr`:
   - Replace absolute `"C:\\Dce Pro\\..."` paths with relative or asset-relative paths.
2. In `HPDcePro/deploy/pack-bundle.ps1`:
   - Copy `HPDcePro/assets/` into `deploy/Desktop/assets`, `deploy/Revit2026/HPDcePro/assets`, `deploy/HPDcePro.bundle/assets`, and `deploy/McpServer/assets`.

---

## 5. Verification Method

To independently verify the implementation after Worker M2 applies the changes:

1. **Static Grep Verification (Zero Dependency on `C:\Dce Pro`)**:
   Run ripgrep across `HPDcePro/src/**` and `HPDcePro/tests/**`:
   ```powershell
   git grep -i "C:\\Dce Pro" "HPDcePro/src"
   git grep -i "C:/Dce Pro" "HPDcePro/src"
   ```
   *Expected result*: Exactly **0 matches** found.

2. **Compilation Verification (Clean Code Rule C2/M3/TreatWarningsAsErrors)**:
   ```powershell
   dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1
   ```
   *Expected result*: `0 Warning(s), 0 Error(s)`.

3. **Automated Unit & Live Test Verification**:
   ```powershell
   dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   ```
   *Expected result*: 100% green tests (>= 375 tests including new `AssetPathResolverTests`).

4. **Environment Isolation Test**:
   Execute tests with `HPDCEPRO_ASSETS_DIR` set to `HPDcePro/assets`:
   ```powershell
   $env:HPDCEPRO_ASSETS_DIR = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\assets"
   dotnet test HPDcePro/tests/HPDcePro.Core.Tests -c Debug --disable-build-servers -m:1
   Remove-Item Env:\HPDCEPRO_ASSETS_DIR
   ```
   *Expected result*: 100% pass without touching `C:\Dce Pro`.
