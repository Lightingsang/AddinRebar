using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using HPKata.UI.Common;
using MahApps.Metro.Controls;

namespace EmpiricalChallengerM1_2
{
    public class MockHostTheme : IHostTheme
    {
        private bool _isDark;
        public bool IsDark
        {
            get => _isDark;
            set
            {
                if (_isDark != value)
                {
                    _isDark = value;
                    Changed?.Invoke();
                }
            }
        }
        public event Action? Changed;
    }

    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            int passCount = 0;
            int failCount = 0;

            void Assert(bool condition, string testName, string details = "")
            {
                if (condition)
                {
                    Console.WriteLine($"[PASS] {testName}");
                    passCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAIL] {testName} - {details}");
                    Console.ResetColor();
                    failCount++;
                }
            }

            Console.WriteLine("=== EMPIRICAL CHALLENGER M1: THEME & MULTI-HOST STRESS HARNESS ===");

            var thread = new Thread(() =>
            {
                try
                {
                    // Ensure Application context exists for pack URI resolution
                    if (Application.Current == null)
                    {
                        new Application();
                    }

                    // Test 1: Assembly preloading
                    ThemeManager.EnsureAssembliesLoaded();
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    bool hasMahApps = assemblies.Any(a => a.GetName().Name == "MahApps.Metro");
                    bool hasControlzEx = assemblies.Any(a => a.GetName().Name == "ControlzEx");
                    Assert(hasMahApps, "Preloading MahApps.Metro", "MahApps.Metro assembly not loaded in AppDomain");
                    Assert(hasControlzEx, "Preloading ControlzEx", "ControlzEx assembly not loaded in AppDomain");

                    // Test 2: Direct Pack URI loading of Theme.xaml
                    var themeUri = new Uri("pack://application:,,,/HPKata.UI;component/Themes/Theme.xaml", UriKind.Absolute);
                    var themeDict = new ResourceDictionary { Source = themeUri };
                    Assert(themeDict != null, "Load Theme.xaml via Pack URI", "Theme.xaml returned null dictionary");

                    // Test 3: Required Style Keys presence and type in Theme.xaml
                    string[] styleKeys = {
                        "StandardDataGrid", "StandardTextBox", "StandardComboBox",
                        "PrimaryButton", "Button.Primary", "SecondaryButton", "Button.Secondary",
                        "Card.Panel", "GroupBox.Header", "Text.Title", "Text.Section",
                        "Text.Muted", "Text.Bold", "Text.Label"
                    };

                    foreach (var key in styleKeys)
                    {
                        bool exists = themeDict.Contains(key);
                        object val = exists ? themeDict[key] : null!;
                        Assert(exists && val is Style, $"Theme.xaml Style Key: {key}", $"Exists={exists}, Type={val?.GetType().Name}");
                    }

                    // Test 4: Static Brush.Card definition in Theme.xaml
                    Assert(themeDict.Contains("Brush.Card") && themeDict["Brush.Card"] is SolidColorBrush,
                        "Theme.xaml Brush.Card presence", "Brush.Card missing or not SolidColorBrush");
                    if (themeDict["Brush.Card"] is SolidColorBrush staticCardBrush)
                    {
                        // In Theme.xaml, Color="{StaticResource Color.Surface}" from ThemeDark (#353535)
                        var col = staticCardBrush.Color;
                        Assert(col.R == 0x35 && col.G == 0x35 && col.B == 0x35,
                            "Theme.xaml static Brush.Card default is Dark Surface (#353535)",
                            $"R={col.R:X2}, G={col.G:X2}, B={col.B:X2}");
                    }

                    // Test 5: Spacing and dimension tokens
                    Assert(themeDict.Contains("Spacing.XS") && (double)themeDict["Spacing.XS"] == 4.0, "Token Spacing.XS == 4.0");
                    Assert(themeDict.Contains("Spacing.S") && (double)themeDict["Spacing.S"] == 8.0, "Token Spacing.S == 8.0");
                    Assert(themeDict.Contains("Spacing.M") && (double)themeDict["Spacing.M"] == 16.0, "Token Spacing.M == 16.0");
                    Assert(themeDict.Contains("Spacing.L") && (double)themeDict["Spacing.L"] == 24.0, "Token Spacing.L == 24.0");
                    Assert(themeDict.Contains("Margin.Row") && (Thickness)themeDict["Margin.Row"] == new Thickness(0, 0, 0, 8), "Token Margin.Row");
                    Assert(themeDict.Contains("Margin.Section") && (Thickness)themeDict["Margin.Section"] == new Thickness(0, 0, 0, 16), "Token Margin.Section");
                    Assert(themeDict.Contains("Padding.Card") && (Thickness)themeDict["Padding.Card"] == new Thickness(12), "Token Padding.Card");
                    Assert(themeDict.Contains("StandardFont") && ((FontFamily)themeDict["StandardFont"]).Source == "Segoe UI", "Token StandardFont == Segoe UI");

                    // Test 6: Implicit styles in Theme.xaml
                    Assert(themeDict.Contains(typeof(TextBlock)), "Implicit TextBlock style in Theme.xaml");
                    Assert(themeDict.Contains(typeof(TextBox)), "Implicit TextBox style in Theme.xaml");
                    Assert(themeDict.Contains(typeof(ComboBox)), "Implicit ComboBox style in Theme.xaml");
                    Assert(themeDict.Contains(typeof(GroupBox)), "Implicit GroupBox style in Theme.xaml");
                    Assert(themeDict.Contains(typeof(DataGrid)), "Implicit DataGrid style in Theme.xaml");
                    Assert(themeDict.Contains(typeof(DataGridColumnHeader)), "Implicit DataGridColumnHeader style in Theme.xaml");

                    // Test 7: MetroWindow instantiation & Dynamic ThemeManager.ApplyTheme(window, dark: true)
                    var window = new MetroWindow();
                    window.Resources.MergedDictionaries.Add(themeDict);

                    ThemeManager.ApplyTheme(window, dark: true);

                    var darkCard = window.TryFindResource("Brush.Card") as SolidColorBrush;
                    Assert(darkCard != null, "Dynamic query Brush.Card (Dark mode)", "Brush.Card returned null");
                    if (darkCard != null)
                    {
                        var col = darkCard.Color;
                        Assert(col.R == 0x35 && col.G == 0x35 && col.B == 0x35,
                            "Dynamic Brush.Card color in Dark mode is #353535",
                            $"Expected #353535, Got #{col.R:X2}{col.G:X2}{col.B:X2}");
                    }

                    var darkBg = window.TryFindResource("Brush.Background") as SolidColorBrush;
                    Assert(darkBg != null && darkBg.Color.R == 0x2B && darkBg.Color.G == 0x2B && darkBg.Color.B == 0x2B,
                        "Dynamic Brush.Background in Dark mode is #2B2B2B");

                    var darkText = window.TryFindResource("Brush.Text") as SolidColorBrush;
                    Assert(darkText != null && darkText.Color.R == 0xE6 && darkText.Color.G == 0xE6 && darkText.Color.B == 0xE6,
                        "Dynamic Brush.Text in Dark mode is #E6E6E6");

                    // Test 8: Dynamic ThemeManager.ApplyTheme(window, dark: false)
                    ThemeManager.ApplyTheme(window, dark: false);

                    var lightCard = window.TryFindResource("Brush.Card") as SolidColorBrush;
                    Assert(lightCard != null, "Dynamic query Brush.Card (Light mode)", "Brush.Card returned null");
                    if (lightCard != null)
                    {
                        var col = lightCard.Color;
                        Assert(col.R == 0xE8 && col.G == 0xE8 && col.B == 0xE8,
                            "Dynamic Brush.Card color in Light mode is #E8E8E8",
                            $"Expected #E8E8E8, Got #{col.R:X2}{col.G:X2}{col.B:X2}");
                    }

                    var lightBg = window.TryFindResource("Brush.Background") as SolidColorBrush;
                    Assert(lightBg != null && lightBg.Color.R == 0xF3 && lightBg.Color.G == 0xF3 && lightBg.Color.B == 0xF3,
                        "Dynamic Brush.Background in Light mode is #F3F3F3");

                    var lightText = window.TryFindResource("Brush.Text") as SolidColorBrush;
                    Assert(lightText != null && lightText.Color.R == 0x1E && lightText.Color.G == 0x1E && lightText.Color.B == 0x1E,
                        "Dynamic Brush.Text in Light mode is #1E1E1E");

                    // Test 9: Rapid Theme Toggling (Leak & Overlay idempotency test)
                    int initialDictCount = window.Resources.MergedDictionaries.Count;
                    for (int i = 0; i < 50; i++)
                    {
                        ThemeManager.ApplyTheme(window, dark: (i % 2 == 0));
                    }
                    int finalDictCount = window.Resources.MergedDictionaries.Count;
                    Assert(initialDictCount == finalDictCount,
                        "Rapid Theme Toggling does not leak ResourceDictionaries",
                        $"Initial: {initialDictCount}, Final: {finalDictCount}");

                    int overlayCount = window.Resources.MergedDictionaries.Count(d => d.Contains("HPKata.ThemeOverlay"));
                    Assert(overlayCount == 1,
                        "Exactly 1 ThemeOverlay dictionary present after 50 toggles",
                        $"Count = {overlayCount}");

                    // Test 10: Real control instantiation and style application
                    var btnPrimary = new Button { Style = (Style)window.FindResource("Button.Primary") };
                    btnPrimary.Measure(new Size(200, 50));
                    Assert(btnPrimary.DesiredSize.Height > 0, "Button with Style Button.Primary measures correctly");

                    var btnSecondary = new Button { Style = (Style)window.FindResource("Button.Secondary") };
                    btnSecondary.Measure(new Size(200, 50));
                    Assert(btnSecondary.DesiredSize.Height > 0, "Button with Style Button.Secondary measures correctly");

                    var txtBox = new TextBox { Style = (Style)window.FindResource("StandardTextBox") };
                    txtBox.Measure(new Size(200, 50));
                    Assert(txtBox.DesiredSize.Height > 0, "TextBox with Style StandardTextBox measures correctly");

                    var cmbBox = new ComboBox { Style = (Style)window.FindResource("StandardComboBox") };
                    cmbBox.Measure(new Size(200, 50));
                    Assert(cmbBox.DesiredSize.Height > 0, "ComboBox with Style StandardComboBox measures correctly");

                    var dataGrid = new DataGrid { Style = (Style)window.FindResource("StandardDataGrid") };
                    dataGrid.Measure(new Size(400, 300));
                    Assert(dataGrid.DesiredSize.Height >= 0, "DataGrid with Style StandardDataGrid measures correctly");

                    var grpBox = new GroupBox { Style = (Style)window.FindResource("GroupBox.Header"), Header = "Section" };
                    grpBox.Measure(new Size(300, 100));
                    Assert(grpBox.DesiredSize.Height > 0, "GroupBox with Style GroupBox.Header measures correctly");

                    var cardBorder = new Border { Style = (Style)window.FindResource("Card.Panel") };
                    cardBorder.Measure(new Size(300, 100));
                    Assert(cardBorder.DesiredSize.Height > 0, "Border with Style Card.Panel measures correctly");

                    // Test 11: TextBlock variants
                    string[] textStyles = { "Text.Title", "Text.Section", "Text.Muted", "Text.Bold", "Text.Label" };
                    foreach (var ts in textStyles)
                    {
                        var tb = new TextBlock { Text = "Test", Style = (Style)window.FindResource(ts) };
                        tb.Measure(new Size(200, 50));
                        Assert(tb.DesiredSize.Height > 0, $"TextBlock with Style {ts} measures correctly");
                    }

                    // Test 12: ThemeManager.Attach with IHostTheme
                    var hostTheme = new MockHostTheme { IsDark = false };
                    ThemeManager.Attach(window, hostTheme);

                    // Switch to dark via host event
                    hostTheme.IsDark = true;
                    // Allow dispatcher to process
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    var hostCardDark = window.TryFindResource("Brush.Card") as SolidColorBrush;
                    Assert(hostCardDark != null && hostCardDark.Color.R == 0x35, "HostTheme Changed event switches to Dark theme");

                    hostTheme.IsDark = false;
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    var hostCardLight = window.TryFindResource("Brush.Card") as SolidColorBrush;
                    Assert(hostCardLight != null && hostCardLight.Color.R == 0xE8, "HostTheme Changed event switches to Light theme");

                    // Test 13: Fallback when only Theme.xaml is loaded without ThemeManager.ApplyTheme
                    var bareWindow = new Window();
                    bareWindow.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
                    var fallbackBrush = bareWindow.TryFindResource("Brush.Card") as SolidColorBrush;
                    Assert(fallbackBrush != null && fallbackBrush.Color.R == 0x35,
                        "Static fallback Brush.Card resolves without ThemeManager.ApplyTheme",
                        $"Brush.Card = {fallbackBrush?.Color}");
                    var fallbackStyle = bareWindow.TryFindResource("Button.Primary") as Style;
                    Assert(fallbackStyle != null, "Static fallback Button.Primary resolves without ThemeManager.ApplyTheme");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CRITICAL EXCEPTION] {ex}");
                    Console.ResetColor();
                    failCount++;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Console.WriteLine($"\n=== RESULTS: {passCount} PASSED, {failCount} FAILED ===");
            return failCount == 0 ? 0 : 1;
        }
    }
}
