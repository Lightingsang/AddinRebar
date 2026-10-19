# Handoff Report — M2 Feature 6: ViewModel & View Excel Integration in HPKata.UI

- Author: `explorer_hpkata_m2_2` (M2 UI Integration Explorer)
- Date: 2026-10-18
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\`
- Target Scope: `HPKata/PROJECT.md` (Milestone M2, Feature 6) & `ORIGINAL_REQUEST.md` (lines 1450–1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

---

## 1. Observation

### 1.1 Baseline Test Suite Status
Executed baseline tests across the complete `HPKata.slnx` suite using:
`dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
Result:
- `HPKata.AutoCad.Tests`: 75 passed (0 failed, 0 skipped)
- `HPKata.Core.Tests`: 561 passed (0 failed, 0 skipped)
- `HPKata.E2E.Tests`: 232 passed (0 failed, 0 skipped)
- `HPKata.Revit.Tests`: 92 passed (0 failed, 0 skipped)
- `HPKata.UI.Tests`: 59 passed (0 failed, 0 skipped)
- **Total: 1,019 automated tests passing with 0 failures, 0 warnings, and 0 skipped tests.**

### 1.2 Target ViewModels & Current Line Counts
Inspected existing ViewModels in `HPKata/HPKata.UI/ViewModels/`:
- `BeamDetailingViewModel.cs`: **163 lines**. Holds `ObservableCollection<BeamSpanViewModel> Spans`, rebar counts, diameter, and stirrup configurations. Line 97 defines `[RelayCommand] private void Apply()`.
- `ColumnDetailingViewModel.cs`: **186 lines**. Holds `ObservableCollection<ColumnStoryRowViewModel> Stories`, splice parameters, and tie distribution. Line 105 defines `[RelayCommand] private void Apply()`.
- `WallDetailingViewModel.cs`: **172 lines**. Holds `ObservableCollection<WallOpeningViewModel> Openings`, boundary element settings, and web mesh parameters. Line 77 defines `[RelayCommand] private void Apply()`.

### 1.3 Target Views & XAML Layouts
Inspected existing Views in `HPKata/HPKata.UI/Views/`:
- `BeamDetailingWindow.xaml`: **207 lines**. Inherits from `System.Windows.Window`. Uses 3 rows (`Header`, `Main Content Area`, `Footer Actions`).
- `ColumnDetailingWindow.xaml`: **174 lines**. Inherits from `System.Windows.Window`. Uses 3 rows (`Header`, `Main Content Area`, `Footer Actions`).
- `WallDetailingWindow.xaml`: **194 lines**. Inherits from `System.Windows.Window`. Uses 3 rows (`Header`, `Main Content Area`, `Footer Actions`).

### 1.4 UI Styling & Theme Constraints
Inspected `HPKata/HPKata.UI/Themes/Theme.xaml`, `MaterialBridge.xaml`, `ThemeDark.xaml`, and `ThemeLight.xaml`:
- Project UI framework: `System.Windows.Window` + `MaterialDesignThemes` (`v5.2.1`) + `MaterialDesignColors` (`v5.2.1`).
- `Theme.xaml` defined `StaticResource` keys:
  - `Card.Panel` (Line 80)
  - `Button.Primary` / `PrimaryButton` (Lines 99, 107)
  - `Button.Secondary` / `SecondaryButton` (Lines 109, 116)
  - `StandardTextBox` (Line 55)
  - `StandardComboBox` (Line 67)
  - `StandardDataGrid` (Line 119)
  - `GroupBox.Header` (Line 89)
  - `Text.Title` (Line 26), `Text.Section` (Line 32), `Text.Muted` (Line 39), `Text.Bold` (Line 44), `Text.Label` (Line 48)
- Dynamic tokens defined in both `ThemeDark.xaml` and `ThemeLight.xaml`:
  - `Brush.Background`, `Brush.Surface`, `Brush.SurfaceElevated`, `Brush.Input`, `Brush.Border`
  - `Brush.Text`, `Brush.TextMuted`, `Brush.Accent`, `Brush.AccentText`
  - `Brush.Success` (`#5FBF7A` dark, `#2E7D32` light)
  - `Brush.Warning` (`#E0B341` dark, `#B26A00` light)
  - `Brush.Danger` (`#F06A6A` dark, `#C62828` light)
- `MahApps.Metro` is **strictly forbidden**. Zero occurrences exist across the solution.
- `HPKata.E2E.Tests/Tier1_FeatureCoverage/Tier1_F18_WpfWindowThemeInstantiationTests.cs` (Lines 18–32) tests instantiation of all 13 windows on a dedicated STA thread under both Dark and Light themes via:
  ```csharp
  ("BeamDetailingWindow", () => new BeamDetailingWindow(new BeamDetailingViewModel())),
  ("ColumnDetailingWindow", () => new ColumnDetailingWindow(new ColumnDetailingViewModel())),
  ("WallDetailingWindow", () => new WallDetailingWindow(new WallDetailingViewModel())),
  ```
  Any constructor modifications must provide a default or optional parameter (`IKataExcelAdapter? excelAdapter = null`) to ensure parameterless or default construction continues to instantiate without throwing.

---

## 2. Logic Chain

1. **Modular Architecture via Partial Classes (Rule C2 Compliance)**:
   - Rule C2 mandates that every `.cs` file must be `<= 300` lines.
   - If Excel I/O methods and property mappings are directly embedded in `ColumnDetailingViewModel.cs` (currently 186 lines), the total line count would reach ~276–290 lines, leaving an uncomfortably small margin.
   - Following the existing architectural pattern in `SetupViewModel` (`SetupViewModel.cs`, `SetupViewModel.DrawingLayers.cs`, `SetupViewModel.RebarDetails.cs`), we split the Excel integration into companion partial files:
     - `BeamDetailingViewModel.Excel.cs` (~95 lines)
     - `ColumnDetailingViewModel.Excel.cs` (~100 lines)
     - `WallDetailingViewModel.Excel.cs` (~100 lines)
   - This keeps every source file strictly `<= 190` lines, isolates Excel translation logic from detailing mathematics, and satisfies Rule C2 with a >100 line safety buffer.

2. **Constructor Resilience & Dependency Injection**:
   - ViewModels must support dependency injection of `IKataExcelAdapter` for hermetic testing in `HPKata.UI.Tests`.
   - At the same time, existing call sites (Revit commands, CAD commands, and E2E tests `Tier1_F18_WpfWindowThemeInstantiationTests`) call `new BeamDetailingViewModel()`.
   - Solution: Declare constructor with optional parameter:
     `public BeamDetailingViewModel(IKataExcelAdapter? excelAdapter = null)`
     If `excelAdapter` is `null`, it gracefully defaults to `KataExcelSessionProvider.TryGetDefaultAdapter()` or null-safe fallback.

3. **Command Behavior & Error Handling (Rule M3 Compliance)**:
   - `OpenExcelCommand`:
     - Resolves workbook path from `KataInstallEnvironment.Current.KataWorkbookPath`.
     - Validates file existence; if missing, updates `ExcelStatus` gracefully rather than throwing.
     - Spawns desktop Excel via `ProcessStartInfo { FileName = path, UseShellExecute = true }` inside a try-catch block.
   - `ReadExcelCommand`:
     - Sets `IsExcelBusy = true`.
     - Calls `IKataExcelAdapter.ReadXSheet(path, out var data)`.
     - If successful, maps data into ViewModel properties and updates `ExcelStatus` with row/span count.
     - If unsuccessful or throws, reports user-friendly error to `ExcelStatus`.
     - Clears `IsExcelBusy` in `finally`.
   - `WriteExcelCommand`:
     - Sets `IsExcelBusy = true`.
     - Gathers properties into DTO and calls `IKataExcelAdapter.WriteXSheet(path, in data)`.
     - Reports success or failure to `ExcelStatus`.
     - Clears `IsExcelBusy` in `finally`.
   - `AutoSyncOnApply`:
     - An observable boolean property `AutoSyncOnApply`.
     - In `Apply()`, if `AutoSyncOnApply == true`, `WriteExcel()` is invoked before creating the layout and invoking `RequestClose`.

4. **XAML Layout Integration**:
   - In each of `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, and `WallDetailingWindow.xaml`, insert a dedicated Row 1 toolbar:
     ```xml
     <Grid.RowDefinitions>
         <RowDefinition Height="Auto"/> <!-- Row 0: Window Header -->
         <RowDefinition Height="Auto"/> <!-- Row 1: Excel Toolbar Strip -->
         <RowDefinition Height="*"/>    <!-- Row 2: Main Detailing Content -->
         <RowDefinition Height="Auto"/> <!-- Row 3: Footer Actions -->
     </Grid.RowDefinitions>
     ```
   - Uses `Card.Panel` container, `Button.Secondary` buttons (`Height="28"`, `Padding="10,2"`), `AutoSyncOnApply` CheckBox, and a status indicator with an `Ellipse` (`Fill="{DynamicResource Brush.Success}"`) + `TextBlock` bound to `ExcelStatus`.
   - All StaticResource keys are strictly verified against `Theme.xaml`.

---

## 3. Caveats

1. **Excel COM Lock Warning**: If desktop Excel has a cell in edit mode (`FormulaBar`), COM automation can return `0x80010001 (RPC_E_CALL_REJECTED)`. The Core Excel adapter handles this via retries and ClosedXML fallback; the ViewModel catches any exception and presents the message in `ExcelStatus` without crashing.
2. **Path Resolution**: ViewModels must never hardcode `C:\HPKata\Kata.xlsm` or `C:\kata_pro\`. All paths must originate from `KataInstallEnvironment.Current.KataWorkbookPath` to allow hermetic testing with `KataInstallEnvironment.UseRootDirectory(tempDir)`.
3. **No MahApps or External Icon Libraries**: Do not introduce MahApps controls, MahApps styles, or external font icon packs. The standard text buttons + Segoe UI font + Material Design brushes ensure 100% theme compatibility on every workstation and test runner.

---

## 4. Conclusion & Concrete Implementation Specifications

### 4.1 Interface Contract Alignment with Feature 5 (`HPKata.Core.Excel`)

```csharp
namespace HPKata.Core.Excel
{
    public interface IKataExcelAdapter
    {
        bool IsDesktopExcelOpen { get; }
        bool ReadBeamSheet(string workbookPath, out KataBeamSheetData data);
        bool WriteBeamSheet(string workbookPath, in KataBeamSheetData data);
        bool ReadColumnSheet(string workbookPath, out KataColumnSheetData data);
        bool WriteColumnSheet(string workbookPath, in KataColumnSheetData data);
        bool ReadWallSheet(string workbookPath, out KataWallSheetData data);
        bool WriteWallSheet(string workbookPath, in KataWallSheetData data);
        bool ExportSchedule(string workbookPath, IEnumerable<KataRebarScheduleItem> items);
    }
}
```

---

### 4.2 Exact ViewModel Companion Implementation: `BeamDetailingViewModel.Excel.cs`

File path: `HPKata/HPKata.UI/ViewModels/BeamDetailingViewModel.Excel.cs` (~95 lines, Rule C2 <= 300)

```csharp
using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;

namespace HPKata.UI.ViewModels
{
    public sealed partial class BeamDetailingViewModel
    {
        private readonly IKataExcelAdapter? _excelAdapter;

        [ObservableProperty] private bool _autoSyncOnApply;
        [ObservableProperty] private string _excelStatus = "Kata.xlsm: Sẵn sàng";
        [ObservableProperty] private bool _isExcelBusy;

        [RelayCommand]
        public void OpenExcel()
        {
            try
            {
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (!File.Exists(path))
                {
                    ExcelStatus = $"Không tìm thấy: {Path.GetFileName(path)}";
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                ExcelStatus = "Đã mở Kata.xlsm trên Excel";
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi mở Excel: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ReadExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang đọc sheet 'Dam'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (_excelAdapter.ReadBeamSheet(path, out var data))
                {
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc {Spans.Count} nhịp dầm '{BeamMark}' từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Dam'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi đọc: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        [RelayCommand]
        public void WriteExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang lưu vào sheet 'Dam'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                var data = BuildSheetData();
                if (_excelAdapter.WriteBeamSheet(path, in data))
                {
                    ExcelStatus = $"Đã lưu dầm '{BeamMark}' ({Spans.Count} nhịp) vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Dam'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi ghi: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        private void PopulateFromSheetData(KataBeamSheetData data)
        {
            BeamMark = data.BeamMark;
            StoryName = data.StoryName;
            ConcreteGrade = data.ConcreteGrade;
            MainSteelGrade = data.MainSteelGrade;
            StirrupSteelGrade = data.StirrupSteelGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            IsShopMode = data.IsShopMode;
            TopContinuousCount = data.TopContinuousCount;
            TopContinuousDiameter = data.TopContinuousDiameter;
            BotContinuousCount = data.BotContinuousCount;
            BotContinuousDiameter = data.BotContinuousDiameter;
            HasSideBars = data.HasSideBars;
            SideBarCount = data.SideBarCount;
            SideBarDiameter = data.SideBarDiameter;
            StirrupDiameter = data.StirrupDiameter;
            DenseSpacing = data.DenseSpacing;
            SparseSpacing = data.SparseSpacing;
            LegCount = data.LegCount;

            Spans.Clear();
            foreach (var s in data.Spans)
            {
                Spans.Add(new BeamSpanViewModel
                {
                    SpanIndex = s.SpanIndex,
                    SpanName = s.SpanName,
                    ClearLengthMm = s.ClearLengthMm,
                    WidthMm = s.WidthMm,
                    HeightMm = s.HeightMm,
                    LeftSupportWidthMm = s.LeftSupportWidthMm,
                    RightSupportWidthMm = s.RightSupportWidthMm,
                    IsCantileverLeft = s.IsCantileverLeft,
                    IsCantileverRight = s.IsCantileverRight
                });
            }
        }

        private KataBeamSheetData BuildSheetData()
        {
            var data = new KataBeamSheetData
            {
                BeamMark = BeamMark,
                StoryName = StoryName,
                ConcreteGrade = ConcreteGrade,
                MainSteelGrade = MainSteelGrade,
                StirrupSteelGrade = StirrupSteelGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                IsShopMode = IsShopMode,
                TopContinuousCount = TopContinuousCount,
                TopContinuousDiameter = TopContinuousDiameter,
                BotContinuousCount = BotContinuousCount,
                BotContinuousDiameter = BotContinuousDiameter,
                HasSideBars = HasSideBars,
                SideBarCount = SideBarCount,
                SideBarDiameter = SideBarDiameter,
                StirrupDiameter = StirrupDiameter,
                DenseSpacing = DenseSpacing,
                SparseSpacing = SparseSpacing,
                LegCount = LegCount
            };

            foreach (var span in Spans)
            {
                data.Spans.Add(new KataBeamSpanData
                {
                    SpanIndex = span.SpanIndex,
                    SpanName = span.SpanName,
                    ClearLengthMm = span.ClearLengthMm,
                    WidthMm = span.WidthMm,
                    HeightMm = span.HeightMm,
                    LeftSupportWidthMm = span.LeftSupportWidthMm,
                    RightSupportWidthMm = span.RightSupportWidthMm,
                    IsCantileverLeft = span.IsCantileverLeft,
                    IsCantileverRight = span.IsCantileverRight
                });
            }

            return data;
        }
    }
}
```

#### Update to `BeamDetailingViewModel.cs`:
- Change constructor signature:
  ```csharp
  public BeamDetailingViewModel() : this(null) { }

  public BeamDetailingViewModel(IKataExcelAdapter? excelAdapter)
  {
      _excelAdapter = excelAdapter;
      Spans.Add(new BeamSpanViewModel
      {
          SpanIndex = 0,
          SpanName = "Nhịp 1",
          ClearLengthMm = 5400.0,
          WidthMm = 300.0,
          HeightMm = 600.0
      });
  }
  ```
- In `Apply()`:
  ```csharp
  [RelayCommand]
  private void Apply()
  {
      if (AutoSyncOnApply)
      {
          WriteExcel();
      }

      var layout = new BeamRebarLayout
      {
          BeamName = BeamMark
      };
      // ... existing layout calculation ...
      ResultLayout = layout;
      RequestClose?.Invoke();
  }
  ```

---

### 4.3 Exact ViewModel Companion Implementation: `ColumnDetailingViewModel.Excel.cs`

File path: `HPKata/HPKata.UI/ViewModels/ColumnDetailingViewModel.Excel.cs` (~100 lines, Rule C2 <= 300)

```csharp
using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;

namespace HPKata.UI.ViewModels
{
    public sealed partial class ColumnDetailingViewModel
    {
        private readonly IKataExcelAdapter? _excelAdapter;

        [ObservableProperty] private bool _autoSyncOnApply;
        [ObservableProperty] private string _excelStatus = "Kata.xlsm: Sẵn sàng";
        [ObservableProperty] private bool _isExcelBusy;

        [RelayCommand]
        public void OpenExcel()
        {
            try
            {
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (!File.Exists(path))
                {
                    ExcelStatus = $"Không tìm thấy: {Path.GetFileName(path)}";
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                ExcelStatus = "Đã mở Kata.xlsm trên Excel";
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi mở Excel: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ReadExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang đọc sheet 'Cot'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (_excelAdapter.ReadColumnSheet(path, out var data))
                {
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc {Stories.Count} tầng cột '{ColumnMark}' từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Cot'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi đọc: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        [RelayCommand]
        public void WriteExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang lưu vào sheet 'Cot'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                var data = BuildSheetData();
                if (_excelAdapter.WriteColumnSheet(path, in data))
                {
                    ExcelStatus = $"Đã lưu cột '{ColumnMark}' ({Stories.Count} tầng) vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Cot'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi ghi: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        private void PopulateFromSheetData(KataColumnSheetData data)
        {
            ColumnMark = data.ColumnMark;
            ConcreteGrade = data.ConcreteGrade;
            MainSteelGrade = data.MainSteelGrade;
            TieSteelGrade = data.TieSteelGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            IsShopMode = data.IsShopMode;
            SpliceMethod = data.SpliceMethod;
            LapLengthFactor = data.LapLengthFactor;
            StaggerOffsetMm = data.StaggerOffsetMm;
            MaxCrankStepDownMm = data.MaxCrankStepDownMm;
            TieDiameter = data.TieDiameter;
            DenseSpacing = data.DenseSpacing;
            SparseSpacing = data.SparseSpacing;
            InnerHoopType = data.InnerHoopType;

            Stories.Clear();
            foreach (var s in data.Stories)
            {
                Stories.Add(new ColumnStoryRowViewModel
                {
                    StoryIndex = s.StoryIndex,
                    StoryName = s.StoryName,
                    StoryHeightMm = s.StoryHeightMm,
                    BeamDepthMm = s.BeamDepthMm,
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameter = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH,
                    DenseSpacing = s.DenseSpacing,
                    SparseSpacing = s.SparseSpacing
                });
            }
        }

        private KataColumnSheetData BuildSheetData()
        {
            var data = new KataColumnSheetData
            {
                ColumnMark = ColumnMark,
                ConcreteGrade = ConcreteGrade,
                MainSteelGrade = MainSteelGrade,
                TieSteelGrade = TieSteelGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                IsShopMode = IsShopMode,
                SpliceMethod = SpliceMethod,
                LapLengthFactor = LapLengthFactor,
                StaggerOffsetMm = StaggerOffsetMm,
                MaxCrankStepDownMm = MaxCrankStepDownMm,
                TieDiameter = TieDiameter,
                DenseSpacing = DenseSpacing,
                SparseSpacing = SparseSpacing,
                InnerHoopType = InnerHoopType
            };

            foreach (var s in Stories)
            {
                data.Stories.Add(new KataColumnStoryData
                {
                    StoryIndex = s.StoryIndex,
                    StoryName = s.StoryName,
                    StoryHeightMm = s.StoryHeightMm,
                    BeamDepthMm = s.BeamDepthMm,
                    WidthMm = s.WidthMm,
                    DepthMm = s.DepthMm,
                    CornerDiameter = s.CornerDiameter,
                    SideCountB = s.SideCountB,
                    SideDiameterB = s.SideDiameterB,
                    SideCountH = s.SideCountH,
                    SideDiameterH = s.SideDiameterH,
                    DenseSpacing = s.DenseSpacing,
                    SparseSpacing = s.SparseSpacing
                });
            }

            return data;
        }
    }
}
```

#### Update to `ColumnDetailingViewModel.cs`:
- Change constructor:
  ```csharp
  public ColumnDetailingViewModel() : this(null) { }

  public ColumnDetailingViewModel(IKataExcelAdapter? excelAdapter)
  {
      _excelAdapter = excelAdapter;
      // ... existing story initialization ...
  }
  ```
- In `Apply()`:
  ```csharp
  [RelayCommand]
  private void Apply()
  {
      if (AutoSyncOnApply)
      {
          WriteExcel();
      }
      // ... existing layout creation ...
      ResultLayout = layout;
      RequestClose?.Invoke();
  }
  ```

---

### 4.4 Exact ViewModel Companion Implementation: `WallDetailingViewModel.Excel.cs`

File path: `HPKata/HPKata.UI/ViewModels/WallDetailingViewModel.Excel.cs` (~100 lines, Rule C2 <= 300)

```csharp
using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;

namespace HPKata.UI.ViewModels
{
    public sealed partial class WallDetailingViewModel
    {
        private readonly IKataExcelAdapter? _excelAdapter;

        [ObservableProperty] private bool _autoSyncOnApply;
        [ObservableProperty] private string _excelStatus = "Kata.xlsm: Sẵn sàng";
        [ObservableProperty] private bool _isExcelBusy;

        [RelayCommand]
        public void OpenExcel()
        {
            try
            {
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (!File.Exists(path))
                {
                    ExcelStatus = $"Không tìm thấy: {Path.GetFileName(path)}";
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                ExcelStatus = "Đã mở Kata.xlsm trên Excel";
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi mở Excel: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ReadExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang đọc sheet 'Vach'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                if (_excelAdapter.ReadWallSheet(path, out var data))
                {
                    PopulateFromSheetData(data);
                    ExcelStatus = $"Đã đọc vách '{WallMark}' ({Openings.Count} lỗ mở) từ Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể đọc dữ liệu từ sheet 'Vach'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi đọc: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        [RelayCommand]
        public void WriteExcel()
        {
            if (_excelAdapter == null)
            {
                ExcelStatus = "Chưa kết nối dịch vụ Excel";
                return;
            }

            try
            {
                IsExcelBusy = true;
                ExcelStatus = "Đang lưu vào sheet 'Vach'...";
                string path = KataInstallEnvironment.Current.KataWorkbookPath;
                var data = BuildSheetData();
                if (_excelAdapter.WriteWallSheet(path, in data))
                {
                    ExcelStatus = $"Đã lưu vách '{WallMark}' vào Kata.xlsm";
                }
                else
                {
                    ExcelStatus = "Không thể lưu vào sheet 'Vach'";
                }
            }
            catch (Exception ex)
            {
                ExcelStatus = $"Lỗi ghi: {ex.Message}";
            }
            finally
            {
                IsExcelBusy = false;
            }
        }

        private void PopulateFromSheetData(KataWallSheetData data)
        {
            WallMark = data.WallMark;
            ShapeType = data.ShapeType;
            ThicknessMm = data.ThicknessMm;
            LengthMm = data.LengthMm;
            StoryHeightMm = data.StoryHeightMm;
            StoryCount = data.StoryCount;
            ConcreteGrade = data.ConcreteGrade;
            ConcreteCoverMm = data.ConcreteCoverMm;
            HasBoundaryElements = data.HasBoundaryElements;
            BoundaryLengthMm = data.BoundaryLengthMm;
            BoundaryBarCount = data.BoundaryBarCount;
            BoundaryBarDiameter = data.BoundaryBarDiameter;
            BoundaryTieDiameter = data.BoundaryTieDiameter;
            BoundaryTieSpacing = data.BoundaryTieSpacing;
            IsDoubleCurtain = data.IsDoubleCurtain;
            WebVerticalDiameter = data.WebVerticalDiameter;
            WebVerticalSpacing = data.WebVerticalSpacing;
            WebHorizontalDiameter = data.WebHorizontalDiameter;
            WebHorizontalSpacing = data.WebHorizontalSpacing;
            CrossTieDiameter = data.CrossTieDiameter;
            HasUBarEdgeCaps = data.HasUBarEdgeCaps;

            Openings.Clear();
            foreach (var op in data.Openings)
            {
                Openings.Add(new WallOpeningViewModel
                {
                    OpeningMark = op.OpeningMark,
                    XOffsetMm = op.XOffsetMm,
                    YOffsetMm = op.YOffsetMm,
                    WidthMm = op.WidthMm,
                    HeightMm = op.HeightMm,
                    HasDiagonalBars = op.HasDiagonalBars,
                    DiagonalDiameter = op.DiagonalDiameter,
                    JambExtraDiameter = op.JambExtraDiameter,
                    HeaderExtraDiameter = op.HeaderExtraDiameter
                });
            }
        }

        private KataWallSheetData BuildSheetData()
        {
            var data = new KataWallSheetData
            {
                WallMark = WallMark,
                ShapeType = ShapeType,
                ThicknessMm = ThicknessMm,
                LengthMm = LengthMm,
                StoryHeightMm = StoryHeightMm,
                StoryCount = StoryCount,
                ConcreteGrade = ConcreteGrade,
                ConcreteCoverMm = ConcreteCoverMm,
                HasBoundaryElements = HasBoundaryElements,
                BoundaryLengthMm = BoundaryLengthMm,
                BoundaryBarCount = BoundaryBarCount,
                BoundaryBarDiameter = BoundaryBarDiameter,
                BoundaryTieDiameter = BoundaryTieDiameter,
                BoundaryTieSpacing = BoundaryTieSpacing,
                IsDoubleCurtain = IsDoubleCurtain,
                WebVerticalDiameter = WebVerticalDiameter,
                WebVerticalSpacing = WebVerticalSpacing,
                WebHorizontalDiameter = WebHorizontalDiameter,
                WebHorizontalSpacing = WebHorizontalSpacing,
                CrossTieDiameter = CrossTieDiameter,
                HasUBarEdgeCaps = HasUBarEdgeCaps
            };

            foreach (var op in Openings)
            {
                data.Openings.Add(new KataWallOpeningData
                {
                    OpeningMark = op.OpeningMark,
                    XOffsetMm = op.XOffsetMm,
                    YOffsetMm = op.YOffsetMm,
                    WidthMm = op.WidthMm,
                    HeightMm = op.HeightMm,
                    HasDiagonalBars = op.HasDiagonalBars,
                    DiagonalDiameter = op.DiagonalDiameter,
                    JambExtraDiameter = op.JambExtraDiameter,
                    HeaderExtraDiameter = op.HeaderExtraDiameter
                });
            }

            return data;
        }
    }
}
```

#### Update to `WallDetailingViewModel.cs`:
- Change constructor:
  ```csharp
  public WallDetailingViewModel() : this(null) { }

  public WallDetailingViewModel(IKataExcelAdapter? excelAdapter)
  {
      _excelAdapter = excelAdapter;
  }
  ```
- In `Apply()`:
  ```csharp
  [RelayCommand]
  private void Apply()
  {
      if (AutoSyncOnApply)
      {
          WriteExcel();
      }
      // ... existing model creation ...
      ResultModel = wall;
      RequestClose?.Invoke();
  }
  ```

---

### 4.5 Exact XAML Toolbar & Visual Indicator Snippet

In `BeamDetailingWindow.xaml`, `ColumnDetailingWindow.xaml`, and `WallDetailingWindow.xaml`:
Change `Grid.RowDefinitions` from 3 rows to 4 rows:
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```
Insert in `Grid.Row="1"`:
```xml
<!-- Excel Integration Toolbar -->
<Border Grid.Row="1" Style="{StaticResource Card.Panel}" Padding="8,4" Margin="0,0,0,10">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Excel Action Buttons -->
        <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
            <Button Content="Mở Kata.xlsm" Command="{Binding OpenExcelCommand}"
                    Style="{StaticResource Button.Secondary}"
                    ToolTip="Mở bảng tính C:\HPKata\Kata.xlsm trên ứng dụng Excel"
                    Height="28" Padding="10,2" Margin="0,0,8,0"/>
            <Button Content="Đọc từ Excel" Command="{Binding ReadExcelCommand}"
                    Style="{StaticResource Button.Secondary}"
                    ToolTip="Nhập dữ liệu cốt thép từ Kata.xlsm"
                    Height="28" Padding="10,2" Margin="0,0,8,0"/>
            <Button Content="Ghi vào Excel" Command="{Binding WriteExcelCommand}"
                    Style="{StaticResource Button.Secondary}"
                    ToolTip="Lưu các thông số cốt thép hiện tại vào Kata.xlsm"
                    Height="28" Padding="10,2" Margin="0,0,12,0"/>
            <CheckBox Content="Tự động đồng bộ khi Áp dụng" IsChecked="{Binding AutoSyncOnApply}"
                      Foreground="{DynamicResource Brush.Text}" VerticalAlignment="Center"/>
        </StackPanel>

        <!-- Status Indicator / Feedback -->
        <StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center">
            <Ellipse Width="8" Height="8" Margin="0,0,6,0"
                     Fill="{DynamicResource Brush.Success}" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding ExcelStatus}" Style="{StaticResource Text.Muted}"
                       VerticalAlignment="Center"/>
        </StackPanel>
    </Grid>
</Border>
```
Move Main Content Area to `Grid.Row="2"`, and Footer to `Grid.Row="3"`.

---

### 4.6 Unit Tests Design for `HPKata.UI.Tests`

Add unit test class `HPKata.UI.Tests/ViewModels/ExcelIntegrationViewModelTests.cs` (~120 lines):

```csharp
using System.IO;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.UI.ViewModels;
using Xunit;

namespace HPKata.UI.Tests.ViewModels
{
    public class ExcelIntegrationViewModelTests
    {
        [Fact]
        public void BeamViewModel_ReadExcel_PopulatesProperties()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.BeamDataToReturn.BeamMark = "D-TEST";
            fakeAdapter.BeamDataToReturn.TopContinuousCount = 4;
            fakeAdapter.BeamDataToReturn.TopContinuousDiameter = 25;
            fakeAdapter.BeamDataToReturn.Spans.Add(new KataBeamSpanData
            {
                SpanName = "Nhịp A",
                ClearLengthMm = 6200.0,
                WidthMm = 350.0,
                HeightMm = 700.0
            });

            var vm = new BeamDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("D-TEST", vm.BeamMark);
            Assert.Equal(4, vm.TopContinuousCount);
            Assert.Equal(25, vm.TopContinuousDiameter);
            Assert.Single(vm.Spans);
            Assert.Equal("Nhịp A", vm.Spans[0].SpanName);
            Assert.Contains("Đã đọc 1 nhịp dầm", vm.ExcelStatus);
        }

        [Fact]
        public void BeamViewModel_WriteExcel_PassesCorrectDataToAdapter()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new BeamDetailingViewModel(fakeAdapter)
            {
                BeamMark = "D-WRITE",
                TopContinuousCount = 3,
                TopContinuousDiameter = 22
            };

            vm.WriteExcelCommand.Execute(null);

            Assert.NotNull(fakeAdapter.LastWrittenBeamData);
            Assert.Equal("D-WRITE", fakeAdapter.LastWrittenBeamData!.BeamMark);
            Assert.Equal(3, fakeAdapter.LastWrittenBeamData.TopContinuousCount);
            Assert.Equal(22, fakeAdapter.LastWrittenBeamData.TopContinuousDiameter);
            Assert.Contains("Đã lưu dầm", vm.ExcelStatus);
        }

        [Fact]
        public void BeamViewModel_ApplyCommand_AutoSyncsWhenEnabled()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            var vm = new BeamDetailingViewModel(fakeAdapter)
            {
                AutoSyncOnApply = true,
                BeamMark = "D-AUTOSYNC"
            };

            vm.ApplyCommand.Execute(null);

            Assert.Equal(1, fakeAdapter.WriteBeamCallCount);
            Assert.Equal("D-AUTOSYNC", fakeAdapter.LastWrittenBeamData?.BeamMark);
        }

        [Fact]
        public void ColumnViewModel_ReadAndWriteExcel_RoundTripsProperly()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.ColumnDataToReturn.ColumnMark = "C-TEST";
            fakeAdapter.ColumnDataToReturn.Stories.Add(new KataColumnStoryData
            {
                StoryName = "Tầng 1",
                WidthMm = 500.0,
                DepthMm = 500.0
            });

            var vm = new ColumnDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("C-TEST", vm.ColumnMark);
            Assert.Single(vm.Stories);
            Assert.Equal(500.0, vm.Stories[0].WidthMm);

            vm.ColumnMark = "C-UPDATED";
            vm.WriteExcelCommand.Execute(null);

            Assert.Equal("C-UPDATED", fakeAdapter.LastWrittenColumnData?.ColumnMark);
        }

        [Fact]
        public void WallViewModel_ReadAndWriteExcel_RoundTripsProperly()
        {
            var fakeAdapter = new FakeKataExcelAdapter();
            fakeAdapter.WallDataToReturn.WallMark = "SW-TEST";
            fakeAdapter.WallDataToReturn.ThicknessMm = 350.0;
            fakeAdapter.WallDataToReturn.Openings.Add(new KataWallOpeningData
            {
                OpeningMark = "OP-1",
                WidthMm = 1000.0,
                HeightMm = 2200.0
            });

            var vm = new WallDetailingViewModel(fakeAdapter);
            vm.ReadExcelCommand.Execute(null);

            Assert.Equal("SW-TEST", vm.WallMark);
            Assert.Equal(350.0, vm.ThicknessMm);
            Assert.Single(vm.Openings);

            vm.WallMark = "SW-NEW";
            vm.WriteExcelCommand.Execute(null);

            Assert.Equal("SW-NEW", fakeAdapter.LastWrittenWallData?.WallMark);
        }
    }
}
```

---

### 4.7 STA E2E Tests Design for `HPKata.E2E.Tests`

In `HPKata.E2E.Tests/Tier1_FeatureCoverage/Tier1_F18_WpfWindowThemeInstantiationTests.cs`:
Existing test verifies instantiation on STA thread for both Dark and Light themes.
Adding dedicated toolbar verification ensures controls instantiate and data bind without exception:

```csharp
[Fact]
public void DetailingWindows_STA_ExcelToolbarControls_InstantiateAndBindCleanly()
{
    RunOnSta(() =>
    {
        var beamVm = new BeamDetailingViewModel();
        var beamWindow = new BeamDetailingWindow(beamVm);
        Assert.NotNull(beamWindow);
        Assert.False(beamVm.AutoSyncOnApply);
        Assert.True(beamVm.OpenExcelCommand.CanExecute(null));

        var colVm = new ColumnDetailingViewModel();
        var colWindow = new ColumnDetailingWindow(colVm);
        Assert.NotNull(colWindow);
        Assert.False(colVm.AutoSyncOnApply);
        Assert.True(colVm.OpenExcelCommand.CanExecute(null));

        var wallVm = new WallDetailingViewModel();
        var wallWindow = new WallDetailingWindow(wallVm);
        Assert.NotNull(wallWindow);
        Assert.False(wallVm.AutoSyncOnApply);
        Assert.True(wallVm.OpenExcelCommand.CanExecute(null));
    });
}
```

---

## 5. Verification Method

To verify the implementation once applied by the worker:
1. **Single-Node Build Rule**:
   `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   Must produce 0 errors, 0 warnings.
2. **Execute Full Test Suite**:
   `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`
   Must pass 100% of tests (including the new tests in `HPKata.UI.Tests` and `HPKata.E2E.Tests`).
3. **Clean Code Line Count Audit**:
   Verify every `.cs` file in `HPKata.UI` is `<= 300` lines (Rule C2) and methods `<= 50` lines (Rule M3).
4. **STA Instantiation Check**:
   Run `Tier1_F18_WpfWindowThemeInstantiationTests` on `HPKata.E2E.Tests` to verify all 13 windows instantiate on the STA thread under Dark and Light themes with 0 XAML parse or StaticResource exceptions.
