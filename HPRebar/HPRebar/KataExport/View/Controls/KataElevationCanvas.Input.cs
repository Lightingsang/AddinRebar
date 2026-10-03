using System.Windows;
using System.Windows.Input;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Mouse handling as in AutoCAD: the wheel zooms around the cursor, holding the middle button pans (hand cursor),
/// a middle double click is Zoom Extents; a left click selects a column.
/// </summary>
public sealed partial class KataElevationCanvas
{
    private const double ZeroWidthReachPx = 7.0;
    private const double DragThresholdPx = 4.0;
    /// <summary>Zoom per wheel notch (AutoCAD's ZOOMFACTOR 60).</summary>
    private const double WheelStep = 1.25;

    private MouseButton? _pressButton;
    private Point _pressAt;
    private Point _lastDrag;
    private bool _panning;

    /// <summary>Latched once the press has travelled past the threshold: a drag, never a click.</summary>
    private bool _moved;

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Elevation is not { } elevation || _viewport is not { } viewport) return;

        var at = e.GetPosition(this);
        _viewport = viewport.ZoomAt(Math.Pow(WheelStep, e.Delta / 120.0), at.X, at.Y, MinScale(elevation), MaxScale);
        _userFramed = true;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Middle)) return;

        Focus();
        if (e.ChangedButton == MouseButton.Middle && e.ClickCount == 2)
        {
            EndPress();
            FrameAll();
            e.Handled = true;
            return;
        }

        _pressButton = e.ChangedButton;
        _pressAt = _lastDrag = e.GetPosition(this);
        _moved = false;
        // The middle button pans; the left button selects on release.
        _panning = e.ChangedButton == MouseButton.Middle;
        if (_panning) Cursor = Cursors.Hand;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_pressButton is not { } button || _viewport is not { } viewport) return;
        if (Pressed(e, button) != MouseButtonState.Pressed)
        {
            EndPress();
            return;
        }

        var at = e.GetPosition(this);
        if (!_moved && (at - _pressAt).Length < DragThresholdPx) return;

        _moved = true;
        if (!_panning) return;

        _viewport = viewport.PanBy(at.X - _lastDrag.X, at.Y - _lastDrag.Y);
        _lastDrag = at;
        _userFramed = true;
        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_pressButton != e.ChangedButton) return;

        bool click = e.ChangedButton == MouseButton.Left && !_panning && !_moved && (e.GetPosition(this) - _pressAt).Length < DragThresholdPx;
        if (click && Elevation is { } elevation && _viewport is { } viewport)
        {
            double station = viewport.ToStation(e.GetPosition(this).X);
            // SetCurrentValue keeps the binding to the view model alive whatever its mode.
            if (elevation.ColumnAt(station, ZeroWidthReachPx / viewport.Scale) is { } column)
                SetCurrentValue(SelectedColumnIndexProperty, column);
        }

        EndPress();
        e.Handled = true;
    }

    /// <summary>Alt+Tab or a Revit dialog can take the capture mid-drag; the next move must not keep panning.</summary>
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _pressButton = null;
        _panning = false;
        Cursor = null;
    }

    private void EndPress()
    {
        _pressButton = null;
        _panning = false;
        Cursor = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private static MouseButtonState Pressed(MouseEventArgs e, MouseButton button) =>
        button == MouseButton.Middle ? e.MiddleButton : e.LeftButton;
}
