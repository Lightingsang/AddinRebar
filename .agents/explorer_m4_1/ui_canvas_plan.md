# Milestone M4 Architecture & Design Plan: Continuous Beam Rebar UI, Dynamic Theming & Interactive Canvases

## 1. Executive Summary & Architecture Overview

Milestone M4 delivers the complete presentation and visualization tier for the **Continuous Beam Rebar** module in `HPRebar`. Following the proven production architecture established by the golden standard (`Column Rebar`), M4 provides a responsive, MVVM-compliant, theme-adaptive WPF dialog and two high-performance, real-time 2D drawing canvases (`BeamElevationCanvas` and `BeamSectionCanvas`).

### 1.1 Architectural Layering

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 PRESENTATION LAYER (M4)                                │
│                                                                                        │
│  ┌─────────────────────────────────┐          ┌─────────────────────────────────────┐  │
│  │       BeamRebarView.xaml        │          │    BeamElevationCanvas (OnRender)   │  │
│  │  - Modal Dialog (ShowDialog)    │          │  - Continuous Spans & Supports      │  │
│  │  - 5-Tab Navigation Panel       │◄────────►│  - Main / Additional / Side Bars    │  │
│  │  - DynamicResource Light/Dark   │          │  - 3-Zone Stirrups & Hanging Ties   │  │
│  │  - Reactive Progress & Footer   │          │  - Selected Span Highlight          │  │
│  └────────────────┬────────────────┘          └─────────────────────────────────────┘  │
│                   │ DataContext                               ▲                        │
│                   ▼                                           │ Observable             │
│  ┌─────────────────────────────────┐                          │ Data Binding           │
│  │     BeamRebarViewModel (CTK)    │                          │                        │
│  │  - ObservableObject             │          ┌───────────────┴─────────────────────┐  │
│  │  - 5 Tab ViewModels             │─────────►│     BeamSectionCanvas (OnRender)    │  │
│  │  - Parameter Validation Engine  │          │  - Concrete Cut (b x h) & Cover     │  │
│  │  - Localization Toggle (EN/VN)  │          │  - Top / Bottom / Skin Rebar Layers │  │
│  │  - RebarTypeCatalog Bindings    │          │  - Closed Stirrups & Cross-Ties     │  │
│  └────────────────┬────────────────┘          └─────────────────────────────────────┘  │
│                   │                                                                    │
│                   ▼ Session                                                            │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │                             BeamRebarSession                                     │  │
│  │  - Holds BeamStack (from M3) and BeamRebarSpec (Domain Model)                    │  │
│  │  - Tracks SelectedSpan, ActiveTab, and Live Rebar Parameters                     │  │
│  │  - Coalesces change notifications to trigger debounced Canvas Invalidation       │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────┬────────────────────────────────────────────┘
                                            │ Executes via IBeamRebarRunner
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                           REVIT ADD-IN RUNTIME & ORCHESTRATION                         │
│  - BeamRebarOrchestrator: TransactionGroup("Beam Rebar")                               │
│  - RevitRebarRunner: Reports progress via IProgress<BeamProgressReport>                │
│  - HPRebar.Core: Pure Math, BeamCanvasTransformCalculator, Zero Revit API Dependencies │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Key Design Tenets
1. **Zero Revit References in Core**: The drawing engine utilizes `BeamCanvasTransformCalculator` from `HPRebar.Core.BeamRebar.Calculators` for mm-to-pixel coordinate projection without any Revit DLL references.
2. **Strict DynamicResource Theming**: All colors, borders, fonts, and spacing tokens bind via `{DynamicResource Brush.X}` and `{DynamicResource Spacing.X}`, enabling instantaneous dark/light mode synchronization with Revit's `UIThemeManager` via `ThemeSwitcher`.
3. **High-Performance Canvas Rendering**: Preview drawings use custom `FrameworkElement` controls overriding `OnRender(DrawingContext dc)` with frozen `Pen` and `Brush` instances, bypassing WPF visual tree overhead and ensuring 60fps responsiveness during rapid parameter entry.
4. **Debounced Visual Invalidation**: A 50ms `DispatcherTimer` coalesces user keystrokes into a single render pass, preventing UI stuttering on large multi-span beams.

---

## 2. WPF MVVM Architecture (`BeamRebarViewModel.cs` & 5 Tabs)

### 2.1 Tab Base Class: `BeamRebarTabViewModel`
Located in `HPRebar/HPRebar/Beam Rebar/View Models/Tabs/BeamRebarTabViewModel.cs`:
```csharp
namespace HPRebar.BeamRebar.ViewModels.Tabs;

public abstract partial class BeamRebarTabViewModel : ObservableObject
{
    protected BeamRebarTabViewModel(BeamRebarSession session, LocalizationService localization)
    {
        Session = session;
        Localization = localization;
    }

    public BeamRebarSession Session { get; }
    public LocalizationService Localization { get; }

    /// <summary>Localized navigation header label.</summary>
    public abstract string Title { get; }

    /// <summary>Refreshes Title property when language toggle is invoked.</summary>
    public void NotifyTitleChanged() => OnPropertyChanged(nameof(Title));

    /// <summary>Applies current span's parameters to all continuous spans.</summary>
    [RelayCommand]
    protected virtual void ApplyToAllSpans() => Session.ApplySelectedSpanToAll();
}
```

### 2.2 Five Dedicated Tab ViewModels

#### Tab 1: Spans & Geometry (`GeometryTabViewModel.cs`)
- **Purpose**: Displays the detected continuous spans, dimensions, elevations, clear lengths, support node widths, and cantilever overhangs.
- **Properties & Bindings**:
  - `IReadOnlyList<BeamSpan> Spans => Session.Stack.Spans`
  - `IReadOnlyList<BeamSupportNode> Supports => Session.Stack.Supports`
  - `BeamSpan? SelectedSpan` (two-way binding with selection)
  - Read-only metric cards: Total Continuous Length ($L_{\text{total}}$), Max Height ($h_{\text{max}}$), Span Count ($N$), Support Count ($N+1$).
  - Per-span data row: Name, $b$ (Width), $h$ (Height), $L_c$ (Center-to-Center), $L_n$ (Clear Span), Level, Cantilever Position.

#### Tab 2: Main Reinforcement (`MainBarsTabViewModel.cs`)
- **Purpose**: Configuration of top and bottom continuous longitudinal reinforcement, end anchorages, and lap splices.
- **Properties & Bindings**:
  - `int TopBarCount` (min: 2, default: 2)
  - `RebarTypeInfo TopBarType` (bound to `Session.BarTypes`)
  - `EndAnchorageType TopStartAnchorage` & `TopEndAnchorage` (Hook 90° Down, Straight, None)
  - `double TopHookLength` (0 = auto $h - 2\cdot \text{Cover}$)
  - `int BottomBarCount` (min: 2, default: 2)
  - `RebarTypeInfo BottomBarType` (bound to `Session.BarTypes`)
  - `EndAnchorageType BottomStartAnchorage` & `BottomEndAnchorage` (Hook 90° Up, Straight, None)
  - `double BottomHookLength` (0 = auto $h - 2\cdot \text{Cover}$)
  - `double Cover` (mm, default: 25.0)
  - `double MaxStockLength` (mm, default: 11700.0)
  - `double LapFactor` ($x \cdot d$, default: 40.0)
  - `bool EnableStagger` (50% staggered lap splice toggle, default: true)

#### Tab 3: Additional Reinforcement (`AdditionalBarsTabViewModel.cs`)
- **Purpose**: Configuration of negative moment bars over supports (gối) and positive moment bars at midspans (nhịp) in Layer 1 and Layer 2.
- **Sub-Panels / Accordions**:
  1. **Top Negative Bars Over Supports**:
     - Support selector dropdown (`SupportsNo`: Support 0 to Support N)
     - `int TopLayer1Count`, `RebarTypeInfo TopLayer1BarType`
     - `double TopLayer1ExtensionRatio` (default: $1/3 = 0.333$ of adjacent clear span)
     - `bool EnableTopLayer2`, `int TopLayer2Count`, `RebarTypeInfo TopLayer2BarType`
     - `double TopLayer2ExtensionRatio` (default: $1/4 = 0.25$ of adjacent clear span)
     - `double TopLayerGap` (vertical spacing between Layer 1 & 2, default: 50mm)
  2. **Bottom Positive Bars in Midspan**:
     - Span selector dropdown (`SpanNo`: Span 1 to Span N)
     - `int BottomLayer1Count`, `RebarTypeInfo BottomLayer1BarType`
     - `double BottomCutoffRatio` (distance from support face, default: $1/7 = 0.143$)
     - `bool EnableBottomLayer2`, `int BottomLayer2Count`, `RebarTypeInfo BottomLayer2BarType`
     - `double BottomLayerGap` (default: 50mm)
  - Command: `ApplyToAllSupportsCommand` and `ApplyToAllSpansCommand`.

#### Tab 4: Stirrups, Skin Bars & Ties (`StirrupsTabViewModel.cs`)
- **Purpose**: Configuration of shear stirrups, deep beam skin reinforcement, and secondary framing hanging ties.
- **Properties & Bindings**:
  1. **Stirrup Distribution**:
     - `StirrupLayout Layout` (Radio/Combo: Uniform vs 3-Zone $L/4$ vs 3-Zone $L/3$)
     - `RebarTypeInfo StirrupBarType`
     - `double SpacingDense` ($s_1$ at support zones, default: 100mm)
     - `double SpacingSparse` ($s_2$ at midspan zone, default: 200mm)
     - `double StartOffset` ($c_0$ offset to first stirrup, default: 50mm)
     - `bool IncludeStirrupsInNodes` (carry stirrups through column/wall core)
  2. **Deep Beam Side (Skin) Bars**:
     - `bool AutoSkinBars` (auto-enable when $h \ge 700\text{mm}$)
     - `double DepthThreshold` (default: 700mm)
     - `RebarTypeInfo SideBarType` (default: D12)
     - `double MaxVerticalSpacing` (default: $\le 300\text{mm}$)
     - `bool IncludeCrossTies` (anti-buckling cross-ties toggle)
     - `RebarTypeInfo CrossTieBarType` (default: D8)
     - `double CrossTieSpacing` (default: 400mm)
  3. **Secondary Beam Framing Hanging Stirrups**:
     - `bool EnableHangingStirrups` (cốt treo gia cường dầm phụ)
     - `int HangingStirrupsPerSide` (default: 3 pairs per side)
     - `double HangingStirrupSpacing` (default: 50mm)
     - `bool EnableDiagonalTies` (thép vai bò toggle)

#### Tab 5: Views & Annotations (`ViewsTabViewModel.cs`)
- **Purpose**: Detailing, drawing creation, dimensioning styles, and schedule tagging.
- **Properties & Bindings**:
  - `bool CreateElevationView` (Longitudinal section detail)
  - `string ElevationViewName` (default: "Beam Detail")
  - `int ElevationScale` (Combo: 1:25, 1:50, 1:100)
  - `bool CreateSectionViews` (Cross-section view creation toggle)
  - `int SectionsPerSpan` (2 sections: Support + Midspan; or 3 sections: Left + Mid + Right)
  - `string SectionPrefix` (default: "Sec")
  - `bool CreateDimensions` (automatic elevation & cross-section dimensioning)
  - `bool CreateTags` (rebar callout tags & schedule marks)
  - `string PartitionName` (Revit Rebar Partition parameter, default: "Beam Rebar")

### 2.3 Master ViewModel: `BeamRebarViewModel.cs`

```csharp
namespace HPRebar.BeamRebar.ViewModels;

public sealed partial class BeamRebarViewModel : ObservableObject
{
    private readonly IBeamRebarRunner _runner;

    [ObservableProperty] private BeamRebarTabViewModel _selectedTab = null!;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private int _progressMaximum = 100;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isBusy;

    public event Action<bool?>? CloseRequested;

    public BeamRebarViewModel(
        BeamRebarSession session,
        LocalizationService localization,
        IBeamRebarRunner runner)
    {
        Session = session;
        Localization = localization;
        _runner = runner;

        Tabs = new ObservableCollection<BeamRebarTabViewModel>
        {
            new GeometryTabViewModel(session, localization),
            new MainBarsTabViewModel(session, localization),
            new AdditionalBarsTabViewModel(session, localization),
            new StirrupsTabViewModel(session, localization),
            new ViewsTabViewModel(session, localization)
        };

        SelectedTab = Tabs[0];
    }

    public BeamRebarSession Session { get; }
    public LocalizationService Localization { get; }
    public ObservableCollection<BeamRebarTabViewModel> Tabs { get; }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Localization.Toggle();
        foreach (var tab in Tabs) tab.NotifyTitleChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (!Session.Validate(out var validationError))
        {
            StatusMessage = validationError;
            RevitDialogs.Warning(Localization.Strings.WindowTitle, validationError);
            return;
        }

        IsBusy = true;
        StatusMessage = Localization.Strings.Working;
        Progress = 0;

        try
        {
            var spec = Session.ToSpec();
            ProgressMaximum = Math.Max(1, _runner.PlannedCount(spec));

            var progressReporter = new Progress<BeamProgressReport>(report =>
            {
                Progress = report.CompletedCount;
                StatusMessage = $"{report.PhaseDescription} ({report.CompletedCount}/{report.TotalCount})";
            });

            int created = await Task.Run(() => _runner.RunWithReport(spec, progressReporter));

            if (created == 0)
            {
                StatusMessage = Localization.Strings.NothingToCreate;
                return;
            }

            CloseRequested?.Invoke(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Continuous Beam Rebar generation failed");
            StatusMessage = ex.Message;
            RevitDialogs.Error(Localization.Strings.WindowTitle, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
```

### 2.4 Parameter Validation Engine (`BeamRebarSession.Validate`)
Validation rules implemented before allowing `Run`:
1. **Cover vs Section clearance**: $2 \cdot \text{Cover} + 2 \cdot \phi_{\text{stirrup}} + \phi_{\text{main}} < \min(b, h)$.
2. **Bar Counts**: Main top $\ge 2$, Main bottom $\ge 2$.
3. **Stirrup Spacings**: $s_1 > 0$, $s_2 > 0$, $s_{\text{dense}} \ge 50\text{mm}$.
4. **Revit Max Positions Guardrail**: $\text{Count} = \lfloor \frac{L_n}{s} \rfloor + 1 \le 1002$.
5. **Rebar Types Selected**: Top, bottom, stirrup bar types must not be null.
6. **Commercial Length vs Lap Splice**: $L_{\text{lap}} = \text{LapFactor} \cdot \phi > 0$.

### 2.5 Progress Reporting Protocol: `BeamProgressReport`
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record BeamProgressReport(
    string PhaseDescription,
    int CompletedCount,
    int TotalCount,
    double Percentage);
```
Phases reported during execution:
- Phase 1: Validating continuous solids and geometry (5%)
- Phase 2: Generating shear stirrups and zone sets (30%)
- Phase 3: Generating continuous longitudinal top and bottom bars (55%)
- Phase 4: Generating additional support top and midspan bottom bars (75%)
- Phase 5: Generating side skin bars, cross-ties, and hanging stirrups (90%)
- Phase 6: Creating detail elevation, cross-sections, dimensions, and tags (100%)

---

## 3. WPF View & Dynamic Theming (`BeamRebarView.xaml`)

### 3.1 Window Layout Architecture
The modal window layout uses a 3-row, 2-column responsive `Grid`:

```xml
<Window x:Class="HPRebar.BeamRebar.Views.BeamRebarView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:tabs="clr-namespace:HPRebar.BeamRebar.ViewModels.Tabs"
        xmlns:views="clr-namespace:HPRebar.BeamRebar.Views.Tabs"
        xmlns:controls="clr-namespace:HPRebar.BeamRebar.Views.Controls"
        Title="{Binding Localization.Strings.WindowTitle}"
        Width="1100" Height="760"
        MinWidth="960" MinHeight="640"
        WindowStartupLocation="CenterOwner"
        Background="{DynamicResource Brush.Background}"
        FontFamily="{DynamicResource Font.Family.Default}"
        FontSize="{DynamicResource Font.Size.Body}"
        Foreground="{DynamicResource Brush.Foreground.Primary}">

    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml"/>
            </ResourceDictionary.MergedDictionaries>

            <!-- Tab DataTemplate Routing -->
            <DataTemplate DataType="{x:Type tabs:GeometryTabViewModel}">
                <views:GeometryTabView/>
            </DataTemplate>
            <DataTemplate DataType="{x:Type tabs:MainBarsTabViewModel}">
                <views:MainBarsTabView/>
            </DataTemplate>
            <DataTemplate DataType="{x:Type tabs:AdditionalBarsTabViewModel}">
                <views:AdditionalBarsTabView/>
            </DataTemplate>
            <DataTemplate DataType="{x:Type tabs:StirrupsTabViewModel}">
                <views:StirrupsTabView/>
            </DataTemplate>
            <DataTemplate DataType="{x:Type tabs:ViewsTabViewModel}">
                <views:ViewsTabView/>
            </DataTemplate>
        </ResourceDictionary>
    </Window.Resources>

    <Grid Margin="{DynamicResource Spacing.Large}">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>          <!-- Row 0: Navigation + Tab Content -->
            <RowDefinition Height="220"/>        <!-- Row 1: Real-time Elevation Preview -->
            <RowDefinition Height="Auto"/>       <!-- Row 2: Status & Footer Actions -->
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="230"/>      <!-- Col 0: Vertical Tab List -->
            <ColumnDefinition Width="*"/>        <!-- Col 1: Active Tab View -->
        </Grid.ColumnDefinitions>

        <!-- Left Navigation -->
        <ListBox Grid.Row="0" Grid.Column="0"
                 ItemsSource="{Binding Tabs}"
                 SelectedItem="{Binding SelectedTab}"
                 Background="{DynamicResource Brush.Surface}"
                 BorderBrush="{DynamicResource Brush.Border}"
                 BorderThickness="1">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Title}"
                               Margin="{DynamicResource Spacing.Small}"
                               FontSize="{DynamicResource Font.Size.BodyStrong}"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Active Tab Content Area -->
        <ContentControl Grid.Row="0" Grid.Column="1"
                        Margin="{DynamicResource Spacing.MediumHorizontal}"
                        Content="{Binding SelectedTab}"/>

        <!-- Bottom Elevation Preview Canvas -->
        <ScrollViewer Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                      Margin="{DynamicResource Spacing.MediumTop}"
                      HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Disabled"
                      Background="{DynamicResource Brush.Surface}"
                      BorderBrush="{DynamicResource Brush.Border}"
                      BorderThickness="1">
            <controls:BeamElevationCanvas Session="{Binding Session}"/>
        </ScrollViewer>

        <!-- Footer -->
        <Grid Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2"
              Margin="{DynamicResource Spacing.MediumTop}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Button Grid.Column="0"
                    Style="{DynamicResource LinkButton}"
                    Content="{Binding Localization.Strings.LanguageToggle}"
                    Command="{Binding ToggleLanguageCommand}"/>

            <StackPanel Grid.Column="1" Margin="{DynamicResource Spacing.MediumHorizontal}">
                <ProgressBar Height="6"
                             Minimum="0"
                             Maximum="{Binding ProgressMaximum}"
                             Value="{Binding Progress}"
                             Background="{DynamicResource Brush.Surface}"
                             Foreground="{DynamicResource Brush.Accent}"
                             BorderThickness="0"/>
                <TextBlock Text="{Binding StatusMessage}"
                           Margin="{DynamicResource Spacing.SmallTop}"
                           FontSize="{DynamicResource Font.Size.Caption}"
                           Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
            </StackPanel>

            <StackPanel Grid.Column="2" Orientation="Horizontal">
                <Button Style="{DynamicResource SecondaryButton}"
                        Content="{Binding Localization.Strings.Cancel}"
                        Command="{Binding CancelCommand}"
                        MinWidth="96"/>
                <Button Style="{DynamicResource PrimaryButton}"
                        Content="{Binding Localization.Strings.Ok}"
                        Command="{Binding RunCommand}"
                        Margin="{DynamicResource Spacing.SmallHorizontal}"
                        MinWidth="96"/>
            </StackPanel>
        </Grid>
    </Grid>
</Window>
```

### 3.2 DynamicResource Theme Token Binding Matrix

| UI Component | Property | DynamicResource Token | Fallback Color (Light/Dark) |
|---|---|---|---|
| Window Background | `Background` | `{DynamicResource Brush.Background}` | `#FFFFFF` / `#1E1E1E` |
| Primary Text | `Foreground` | `{DynamicResource Brush.Foreground.Primary}` | `#333333` / `#FFFFFF` |
| Secondary Text | `Foreground` | `{DynamicResource Brush.Foreground.Secondary}` | `#666666` / `#CCCCCC` |
| Card Containers | `Background` | `{DynamicResource Brush.Surface}` | `#F5F5F5` / `#2D2D30` |
| Control Borders | `BorderBrush` | `{DynamicResource Brush.Border}` | `#D1D1D1` / `#3E3E42` |
| OK / CTA Button | `Background` | `{DynamicResource Brush.Accent}` | `#0696D7` (Revit Blue) |
| Progress Bar | `Foreground` | `{DynamicResource Brush.Accent}` | `#0696D7` |
| Canvas Surface | `Background` | `{DynamicResource Brush.Canvas.Fill}` | `#FFFFFF` / `#252526` |
| Concrete Outlines | `Pen` | `{DynamicResource Brush.Canvas.Bound}` | `#333333` / `#CCCCCC` |
| Main Rebar | `Pen` / `Brush` | `{DynamicResource Brush.Canvas.MainBar}` | `#C1272D` / `#E5484D` |
| Selected Rebar/Span | `Pen` / `Brush` | `{DynamicResource Brush.Canvas.MainBar.Selected}`| `#E07B00` / `#F5A623` |
| Stirrup Ties | `Pen` | `{DynamicResource Brush.Canvas.Stirrup}` | `#0A7D46` / `#16C172` |
| Annotations / Dims | `Pen` / `Brush` | `{DynamicResource Brush.Canvas.Tag}` | `#6C6C70` / `#8E8E93` |

### 3.3 Theme Switching Lifecycle: `ThemeSwitcher.cs`
In `BeamRebarView.xaml.cs`:
```csharp
namespace HPRebar.BeamRebar.Views;

public partial class BeamRebarView : Window
{
    public BeamRebarView(BeamRebarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Synchronize with Revit's active Dark/Light theme
        ThemeSwitcher.ApplyFromRevit(this);

        viewModel.CloseRequested += result =>
        {
            DialogResult = result;
            Close();
        };
    }
}
```

---

## 4. Interactive Preview Canvases (`BeamElevationCanvas` & `BeamSectionCanvas`)

### 4.1 Rendering Strategy & Performance
Rather than instantiating hundreds of WPF `Shape` / `Path` elements in visual trees, both canvases override `OnRender(DrawingContext dc)`.
- **Instantaneous Redraw**: Draws directly to the GDI+/DirectX render target.
- **Frozen Resource Caching**: `CanvasPalette` creates and freezes all `Pen` and `Brush` instances upon theme resolution, eliminating GC pressure.
- **Debounced Redraw**: Changes in `Session` start a 50ms `DispatcherTimer`. If multiple keystrokes arrive within 50ms, only the final state is rendered.

```
User types in TextBox
       │
       ▼ PropertyChanged
BeamRebarSession.NotifyChanged()
       │
       ▼
DispatcherTimer.Restart(50ms)
       │
       ▼ Tick (50ms elapsed)
InvalidateMeasure() + InvalidateVisual()
       │
       ▼
OnRender(DrawingContext dc) ──► CanvasPalette (Frozen Pens) ──► BeamCanvasTransformCalculator
```

### 4.2 `BeamCanvasPalette.cs`
Resolves all canvas pens and brushes from the window's `MergedDictionaries`:
```csharp
namespace HPRebar.BeamRebar.Views.Controls;

internal sealed class BeamCanvasPalette
{
    public Brush Fill { get; }
    public Pen Outline { get; }
    public Pen MainBar { get; }
    public Pen SelectedMainBar { get; }
    public Pen AddTopBar { get; }
    public Pen AddBottomBar { get; }
    public Pen SideBar { get; }
    public Pen Stirrup { get; }
    public Pen Dimension { get; }
    public Brush Text { get; }
    public Brush SupportFill { get; }
    public Brush Highlight { get; }

    public static BeamCanvasPalette From(FrameworkElement element)
    {
        var fill = Resolve(element, "Brush.Canvas.Fill", Colors.White);
        var outline = Resolve(element, "Brush.Canvas.Bound", Colors.Black);
        var mainBar = Resolve(element, "Brush.Canvas.MainBar", Colors.Crimson);
        var selected = Resolve(element, "Brush.Canvas.MainBar.Selected", Colors.DarkOrange);
        var stirrup = Resolve(element, "Brush.Canvas.Stirrup", Colors.ForestGreen);
        var tag = Resolve(element, "Brush.Canvas.Tag", Colors.DimGray);

        var supportFill = new SolidColorBrush(Colors.LightGray) { Opacity = 0.3 };
        supportFill.Freeze();

        var highlight = new SolidColorBrush(((SolidColorBrush)selected).Color) { Opacity = 0.15 };
        highlight.Freeze();

        return new BeamCanvasPalette(
            fill,
            FrozenPen(outline, 1.0),
            FrozenPen(mainBar, 2.2),
            FrozenPen(selected, 2.5),
            FrozenPen(mainBar, 1.8),       // AddTopBar
            FrozenPen(mainBar, 1.8),       // AddBottomBar
            FrozenPen(mainBar, 1.4),       // SideBar
            FrozenPen(stirrup, 1.2),
            FrozenPen(tag, 0.6),
            tag,
            supportFill,
            highlight);
    }
}
```

### 4.3 `BeamElevationCanvas.cs` Architecture
- **Control Type**: `FrameworkElement`
- **DependencyProperty**: `SessionProperty` (`BeamRebarSession`)
- **Key Methods**:
  - `MeasureOverride(Size availableSize)`: Computes bounding width from `stack.TotalLength * Scale + 2*Margin`.
  - `OnRender(DrawingContext dc)`:
    1. Reads DPI scaling: `DrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;`.
    2. Draws canvas background rectangle (`palette.Fill`).
    3. Computes `BeamCanvasTransform` via `BeamCanvasTransformCalculator.ComputeElevationTransform(stack, w, h, margin)`.
    4. Invokes `BeamElevationPainter.Paint(dc, palette, transform, session)`.

#### Detailed Rendering Pipeline in `BeamElevationPainter`:
1. **Supports (Columns / Walls)**:
   - For each support node $i = 0 \dots N$:
   - Reads `node.LeftFaceX`, `node.RightFaceX`, `node.Width`.
   - Computes screen coordinates $(x_{\text{left}}, y_{\text{bot}})$, $(x_{\text{right}}, y_{\text{top}})$.
   - Draws support column head below beam ($y > y_{\text{bot}}$) and column base above beam ($y < y_{\text{top}}$) using `SupportFill` and `Outline`.
   - Draws support centerline (dashed line) and label ("C1", "W1", etc.).
2. **Continuous Concrete Beam Contours**:
   - For each span $i = 0 \dots N-1$:
   - Reads `span.StartX`, `span.EndX`, `span.TopElevation`, `span.BottomElevation`.
   - Projects to screen coordinates.
   - Highlights active span if `i == session.SelectedSpanIndex` with `palette.Highlight`.
   - Draws beam upper edge, lower edge, and vertical end lines. If adjacent spans have different heights or offsets, draws step-down / step-up transitions.
3. **Continuous Main Bars**:
   - **Top Main Bars**:
     - Projected along beam length at elevation $Z = Z_{\text{top}} - \text{Cover} - \phi_{\text{stirrup}} - \phi_{\text{top}}/2$.
     - Start anchorage: 90° downward hook inside Support 0.
     - End anchorage: 90° downward hook inside Support $N$.
     - If beam length > 11.7m: calculates lap splice in midspan of Span 1/2 with 50% staggered offset.
   - **Bottom Main Bars**:
     - Projected at elevation $Z = Z_{\text{bot}} + \text{Cover} + \phi_{\text{stirrup}} + \phi_{\text{bottom}}/2$.
     - Start anchorage: 90° upward hook inside Support 0.
     - End anchorage: 90° upward hook inside Support $N$.
     - Lap splices placed over support nodes.
4. **Support Additional Negative Top Bars**:
   - For each support node $i$:
   - Layer 1: centered over support node, extending $L_{n,\text{left}} / 3$ and $L_{n,\text{right}} / 3$.
   - Layer 2 (if enabled): drawn below Layer 1 by `LayerGap`, extending $L_{n,\text{left}} / 4$ and $L_{n,\text{right}} / 4$.
5. **Midspan Additional Positive Bottom Bars**:
   - For each span $i$:
   - Layer 1: placed along bottom, starting at $L_n / 7$ from left support and ending at $L_n / 7$ before right support.
   - Layer 2 (if enabled): placed above Layer 1 by `LayerGap`.
6. **Stirrup Density Zones**:
   - For each span:
   - Evaluates layout (Uniform or 3-Zone).
   - Zone 1 ($L_n / 4$): Dense spacing $s_1$ (e.g. 100mm) rendered as vertical tick lines or density hatched region.
   - Zone 2 ($L_n / 2$): Sparse spacing $s_2$ (e.g. 200mm).
   - Zone 3 ($L_n / 4$): Dense spacing $s_1$.
   - Dimension text below: "$L/4$", "$L/2$", "$L/4$" and "$s_1=100$", "$s_2=200$".
7. **Side Skin Bars & Secondary Beam Hanging Ties**:
   - If $h \ge 700\text{mm}$: draws longitudinal dashed lines along web depth.
   - For each secondary intersection: draws vertical indicator lines and hanging stirrups ($3 \times 50\text{mm}$ each side).
8. **Engineering Dimensioning**:
   - Clear span dimensions ($L_n$) with extension ticks and centered text.
   - Support width dimensions ($C_w$).
   - Overall length dimension ($L_{\text{total}}$) at the bottom.

### 4.4 `BeamSectionCanvas.cs` Architecture
- **Control Type**: `FrameworkElement`
- **DependencyProperties**: `Session`, `SelectedSpanIndex`, `ViewLocation` (Support vs Midspan).
- **Key Methods**:
  - `MeasureOverride`: Computes bounding box from `width * scale + 2*margin`.
  - `OnRender(DrawingContext dc)`:
    1. Computes `BeamCanvasTransform` via `BeamCanvasTransformCalculator.ComputeSectionTransform(b, h, w, h, margin)`.
    2. Draws concrete cross-section box ($b \times h$) centered on canvas.
    3. Insets by `Cover` to draw outer closed stirrup hoop with corner radius.
    4. Top bars: draws corner main bars and intermediate bars as filled circles. If `Layer2` is active, draws second row underneath with vertical clearance.
    5. Bottom bars: draws corner main bars and intermediate bars as filled circles. If `Layer2` is active, draws second row above.
    6. Side skin bars: draws distributed bar circles along left and right inside faces.
    7. Cross-ties: draws horizontal lines with 90°/135° end hooks connecting opposite skin bars.
    8. Dimensions: width $b$ along bottom, height $h$ along left.

---

## 5. End-to-End Data Flow & Reactive State Lifecycle

```
┌────────────────────────────────────────────────────────────────────────┐
│                        1. SELECTION & EXTRACTION                       │
│  User selects structural framing beams in Revit ->                     │
│  BeamStackReader extracts BeamContinuousStack (M3)                     │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        2. SESSION INITIALIZATION                       │
│  BeamRebarSession created with BeamStack, default BeamRebarSpec,       │
│  and loaded RebarTypeCatalog types.                                    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        3. VIEW & VIEWMODEL LAUNCH                      │
│  BeamRebarViewModel created with 5 tabs ->                             │
│  BeamRebarView.ShowDialog() launched as modal window ->               │
│  ThemeSwitcher sets Dark/Light palette matching Revit.                 │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                  4. INTERACTIVE EDITING & CANVAS PREVIEW               │
│  User modifies counts, diameters, cover, or spacings:                  │
│  - Two-way bindings update BeamRebarSession properties.                │
│  - PropertyChanged fires -> DispatcherTimer debounces (50ms).          │
│  - BeamElevationCanvas & BeamSectionCanvas re-render seamlessly        │
│    via BeamCanvasTransformCalculator & OnRender(DrawingContext).       │
│  - Selected span dynamically highlighted in canvas.                    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ User clicks OK
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                        5. VALIDATION & EXECUTION                       │
│  Session.Validate() checks clearance, bar counts, max ties (1002).     │
│  - If invalid: StatusMessage updated, user notified.                   │
│  - If valid: IBeamRebarRunner runs in background task.                 │
│  - Progress<BeamProgressReport> streams completed elements to UI.      │
│  - Revit TransactionGroup("Beam Rebar") commits atomically.            │
│  - Window closes on success.                                           │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Implementation File Inventory

Following the repository's feature folder convention:

| File Path | Layer | Responsibility | Status |
|---|---|---|---|
| `HPRebar/Beam Rebar/View Models/BeamRebarViewModel.cs` | ViewModel | Master dialog VM (tabs, commands, progress, theming, language toggle) | Enhance |
| `HPRebar/Beam Rebar/View Models/BeamRebarSession.cs` | ViewModel | Two-way binding session, span tracking, validation, spec conversion | Enhance |
| `HPRebar/Beam Rebar/View Models/Tabs/BeamRebarTabViewModel.cs` | ViewModel | Abstract base tab VM with Title, Session, Localization | Create |
| `HPRebar/Beam Rebar/View Models/Tabs/GeometryTabViewModel.cs` | ViewModel | Tab 1: Spans & support node geometry display | Create |
| `HPRebar/Beam Rebar/View Models/Tabs/MainBarsTabViewModel.cs` | ViewModel | Tab 2: Top & bottom continuous bars, anchorage hooks, splices | Create |
| `HPRebar/Beam Rebar/View Models/Tabs/AdditionalBarsTabViewModel.cs` | ViewModel | Tab 3: Support negative bars (L/3, L/4) & midspan positive bars (L/7, L/8) | Create |
| `HPRebar/Beam Rebar/View Models/Tabs/StirrupsTabViewModel.cs` | ViewModel | Tab 4: 3-Zone stirrups, deep beam skin bars, cross-ties, hanging ties | Create |
| `HPRebar/Beam Rebar/View Models/Tabs/ViewsTabViewModel.cs` | ViewModel | Tab 5: Views, sections per span, dimensions, tags, partition | Create |
| `HPRebar/Beam Rebar/View/BeamRebarView.xaml` | View | Master window layout, navigation list, content control, footer | Enhance |
| `HPRebar/Beam Rebar/View/BeamRebarView.xaml.cs` | View | Code-behind: DataContext, ThemeSwitcher, CloseRequested | Enhance |
| `HPRebar/Beam Rebar/View/Tabs/GeometryTabView.xaml` | View | UserControl layout for Tab 1 | Create |
| `HPRebar/Beam Rebar/View/Tabs/MainBarsTabView.xaml` | View | UserControl layout for Tab 2 | Create |
| `HPRebar/Beam Rebar/View/Tabs/AdditionalBarsTabView.xaml` | View | UserControl layout for Tab 3 | Create |
| `HPRebar/Beam Rebar/View/Tabs/StirrupsTabView.xaml` | View | UserControl layout for Tab 4 | Create |
| `HPRebar/Beam Rebar/View/Tabs/ViewsTabView.xaml` | View | UserControl layout for Tab 5 | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamCanvasPalette.cs` | Controls | Dynamic theme brush/pen resolution and freezing | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamDrawPrimitives.cs` | Controls | High-performance drawing helper functions | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamElevationCanvas.cs` | Controls | FrameworkElement with OnRender for continuous elevation preview | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs` | Controls | Elevation rendering engine using Core transform | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamSectionCanvas.cs` | Controls | FrameworkElement with OnRender for cross-section preview | Create |
| `HPRebar/Beam Rebar/View/Controls/BeamSectionPainter.cs` | Controls | Cross-section rendering engine using Core transform | Create |
| `HPRebar/Beam Rebar/Models/BeamProgressReport.cs` | Models | Multi-stage execution progress report record | Create |

---

## 7. Edge Cases & Robustness Mitigations

1. **Revit Theme Switching during Dialog Display**:
   - `ThemeSwitcher` checks `UIThemeManager.CurrentTheme` on load. Since all brushes inside `BeamRebarView.xaml` and `CanvasPalette` use `{DynamicResource Brush.X}`, any palette swap instantly refreshes the visual tree and triggers `InvalidateVisual()` on the canvases without window reconstruction.
2. **Extreme Beam Dimensions (Very Long or Very Deep Beams)**:
   - `BeamCanvasTransformCalculator` calculates uniform aspect-ratio scale: `Math.Min(wDraw / lModel, hDraw / hModel)`. A 50-meter continuous beam will shrink uniformly to fit canvas width, while maintaining margin padding.
3. **DataGrid Header DynamicResource Limitation**:
   - In WPF, `DataGridColumn.Header` is not connected to the visual tree and cannot resolve `DynamicResource` or `RelativeSource` to UserControl. Tab 1 uses literal localized string bindings via ViewModel properties or text headers to avoid silent binding failures.
4. **Revit API Thread Safety**:
   - The UI runs modally on Revit's main UI thread via `ShowDialog()`. The `IBeamRebarRunner.Run` execution begins only after validation passes and dialog closes or enters modal busy state, ensuring 100% thread safety without `ExternalEvent` race conditions.
5. **Debounce Keystroke Flood**:
   - Rapidly typing into numeric text boxes (e.g. typing 100 into spacing) does not trigger 3 separate complex calculations; the 50ms `DispatcherTimer` coalesces input into a single render pass.
