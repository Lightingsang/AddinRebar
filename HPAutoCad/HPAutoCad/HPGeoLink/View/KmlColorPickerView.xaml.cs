using System.Windows;
using System.Windows.Controls;
using HPAutoCad.Core.HPGeoLink.Kml;

namespace HPAutoCad.HPGeoLink.View;

public partial class KmlColorPickerView : UserControl
{
    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register(
            nameof(Color),
            typeof(string),
            typeof(KmlColorPickerView),
            new FrameworkPropertyMetadata(
                KmlColor.DefaultPoint,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Color
    {
        get => (string)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public IReadOnlyList<KmlColorPreset> Presets => KmlColor.Presets;

    public KmlColorPickerView()
    {
        using (System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(KmlColorPickerView).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(HPAutoCad.Resources.Themes.ThemeResources.Styles());
            InitializeComponent();
        }
    }

    private void OnSwatchButtonClick(object sender, RoutedEventArgs e)
    {
        PalettePopup.IsOpen = !PalettePopup.IsOpen;
    }

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: KmlColorPreset preset })
        {
            Color = preset.KmlHex;
            PalettePopup.IsOpen = false;
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
        {
            var text = tb.Text.Trim();
            if (text.Length == 6 && !text.StartsWith("#", StringComparison.Ordinal) && KmlColor.IsValid("#" + text))
            {
                Color = "#" + text.ToUpperInvariant();
            }
        }
    }
}
