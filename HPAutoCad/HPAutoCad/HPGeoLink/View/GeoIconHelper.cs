using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;

namespace HPAutoCad.HPGeoLink.View;

/// <summary>
/// Provides the branded HPGeoLink icon (blue rounded square with white compass)
/// for use as Window.Icon on HPGeoLink dialogs.
/// </summary>
public static class GeoIconHelper
{
    private static ImageSource? _cachedIcon;
    private static bool _initialized;

    public static ImageSource? WindowIcon
    {
        get
        {
            if (!_initialized)
            {
                _initialized = true;
                try
                {
                    _cachedIcon = CreateIcon();
                }
                catch
                {
                    _cachedIcon = null;
                }
            }
            return _cachedIcon;
        }
    }

    private static ImageSource? CreateIcon()
    {
        const int size = 48;
        var packIcon = new PackIcon
        {
            Kind = PackIconKind.CompassOutline
        };
        string? pathData = packIcon.Data;
        if (string.IsNullOrEmpty(pathData))
        {
            return null;
        }

        var geom = Geometry.Parse(pathData);
        const double iconSize = 28.0;
        const double offset = (size - iconSize) / 2.0;

        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(iconSize / 24.0, iconSize / 24.0));
        transform.Children.Add(new TranslateTransform(offset, offset));
        transform.Freeze();

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            // Background: rounded rect #0696D7 with 10px radius
            var bgBrush = new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7));
            bgBrush.Freeze();
            dc.DrawRoundedRectangle(bgBrush, null, new Rect(0, 0, size, size), 10, 10);

            // Foreground: White compass
            dc.PushTransform(transform);
            dc.DrawGeometry(Brushes.White, null, geom);
            dc.Pop();
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }
}
