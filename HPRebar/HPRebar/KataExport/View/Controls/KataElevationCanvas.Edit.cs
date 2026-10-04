using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Striking bars off Kata's elevation: a left click on a bar or stirrup line selects the group it stands for (every
/// line of that group is highlighted), a second click on the same spot takes the next line under it, Delete hands the
/// group to <see cref="RemoveBarsCommand"/>, Ctrl+Z calls <see cref="UndoRemoveBarsCommand"/>, Esc closes the section
/// panel, then drops the selection.
/// </summary>
public sealed partial class KataElevationCanvas
{
    /// <summary>Reach of a click around a drawn line (px), and of a click "on the same spot" (px).</summary>
    private const double LineReachPx = 6.0;

    private const double SameSpotPx = 4.0;

    public static readonly DependencyProperty RemoveBarsCommandProperty = DependencyProperty.Register(
        nameof(RemoveBarsCommand), typeof(ICommand), typeof(KataElevationCanvas), new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty UndoRemoveBarsCommandProperty = DependencyProperty.Register(
        nameof(UndoRemoveBarsCommand), typeof(ICommand), typeof(KataElevationCanvas), new FrameworkPropertyMetadata(null));

    // The group selected, for the plan it was selected on; the line it was picked by and where, to cycle on a second click.
    private IReadOnlyList<string>? _selectedKeys;
    private KataRebarPlan? _selectedPlan;
    private KataDrawingLine? _selectedLine;
    private Point _selectedAt;

    /// <summary>Called with the keys of the selected group (<c>IReadOnlyList&lt;string&gt;</c>) when Delete is pressed.</summary>
    public ICommand? RemoveBarsCommand
    {
        get => (ICommand?)GetValue(RemoveBarsCommandProperty);
        set => SetValue(RemoveBarsCommandProperty, value);
    }

    public ICommand? UndoRemoveBarsCommand
    {
        get => (ICommand?)GetValue(UndoRemoveBarsCommandProperty);
        set => SetValue(UndoRemoveBarsCommandProperty, value);
    }

    /// <summary>The keys selected on the plan shown; null when nothing is.</summary>
    private IReadOnlyList<string>? SelectedKeys => ReferenceEquals(_selectedPlan, RebarPlan) ? _selectedKeys : null;

    /// <summary>Selects the group of the bar or stirrup line under <paramref name="at"/>; false when there is none.</summary>
    private bool SelectBarAt(KataElevation elevation, KataElevationViewport viewport, Point at)
    {
        if (!KataMode || Drawing() is not { } drawing) return false;

        var scene = Scene(elevation, viewport);
        var map = RebarStationMap ?? KataStationMap.Identity;
        Point P(double x, double z) => new(scene.X(map.ToStation(x)), scene.Y(elevation.TopMm + z));

        var hits = drawing.Elevation.Lines
            .Where(l => l.Keys is { Count: > 0 })
            .Select(l => (Line: l, Distance: Distance(l, at, P)))
            .Where(h => h.Distance <= LineReachPx)
            .OrderBy(h => h.Distance)
            .Select(h => h.Line)
            .ToList();
        if (hits.Count == 0) return false;

        // Lines lying on one another (a main bar over an additional one): the same spot again takes the next.
        int index = 0;
        if (SelectedKeys is not null && _selectedLine is not null && (at - _selectedAt).Length <= SameSpotPx && hits.IndexOf(_selectedLine) is var current and >= 0)
            index = (current + 1) % hits.Count;

        _selectedLine = hits[index];
        _selectedKeys = _selectedLine.Keys;
        _selectedPlan = RebarPlan;
        _selectedAt = at;
        InvalidateVisual();
        return true;
    }

    private void ClearBarSelection()
    {
        if (_selectedKeys is null) return;
        _selectedKeys = null;
        _selectedLine = null;
        _selectedPlan = null;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            // Handled only when it closed or dropped something: otherwise Esc still closes the window.
            if (SectionPanel() is not null)
            {
                CloseSection();
                e.Handled = true;
            }
            else if (SelectedKeys is not null)
            {
                ClearBarSelection();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete && SelectedKeys is { } keys && RemoveBarsCommand is { } remove && remove.CanExecute(keys))
        {
            ClearBarSelection();
            remove.Execute(keys);
            e.Handled = true;
        }
        else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && UndoRemoveBarsCommand is { } undo && undo.CanExecute(null))
        {
            ClearBarSelection();
            undo.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>Every line of the selected group, over the drawing in the accent colour, and how to delete it.</summary>
    private void PaintBarSelection(KataDrawPrimitives draw, KataCanvasPalette palette, KataElevationScene scene, KataRebarDrawing drawing, KataStationMap map)
    {
        if (SelectedKeys is not { } keys) return;

        var selected = new HashSet<string>(keys, StringComparer.Ordinal);
        var pen = KataCadPens.Lineweight(palette.Accent, KataCadPens.BarPx + 2.0, draw.PixelsPerDip, scene.Viewport.Scale);
        Point P(double x, double z) => new(scene.X(map.ToStation(x)), scene.Y(scene.Elevation.TopMm + z));
        foreach (var line in drawing.Elevation.Lines.Where(l => l.Keys is not null && l.Keys.Any(selected.Contains)))
            draw.Polyline(null, pen, line.Points.Select(p => P(p.X, p.Z)).ToList());

        var hint = draw.Text($"Đã chọn {keys.Count} thanh/vùng đai — Delete: xóa · Ctrl+Z: hoàn tác · Esc: bỏ chọn", palette.Accent, KataDrawPrimitives.SmallTextSize);
        draw.Box(palette.Fill, null, 6.0, scene.Height - hint.Height - 10.0, 14.0 + hint.Width, scene.Height - 4.0);
        draw.At(hint, 10.0, scene.Height - hint.Height - 7.0);
    }

    /// <summary>Shortest distance (px) from <paramref name="at"/> to <paramref name="line"/> drawn on the screen.</summary>
    private static double Distance(KataDrawingLine line, Point at, System.Func<double, double, Point> point)
    {
        double best = double.MaxValue;
        for (int i = 1; i < line.Points.Count; i++)
        {
            var a = point(line.Points[i - 1].X, line.Points[i - 1].Z);
            var b = point(line.Points[i].X, line.Points[i].Z);
            best = Math.Min(best, SegmentDistance(at, a, b));
        }

        return best;
    }

    private static double SegmentDistance(Point p, Point a, Point b)
    {
        var ab = b - a;
        double length2 = ab.LengthSquared;
        double t = length2 < 1e-9 ? 0.0 : Math.Max(0.0, Math.Min(1.0, Vector.Multiply(p - a, ab) / length2));
        return (p - (a + t * ab)).Length;
    }
}
