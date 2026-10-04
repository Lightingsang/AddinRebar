using System.Windows;
using System.Windows.Input;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Mouse handling as in AutoCAD: the wheel zooms around the cursor, holding the middle button pans (hand cursor),
/// a middle double click is Zoom Extents; a left click opens the section of a flag it falls on (Kata's drawing),
/// else selects the bar group of the line under it (<c>KataElevationCanvas.Edit.cs</c>), else a column. Shift + left
/// drag pans too, for a touchpad. Over the floating section panel the same gestures zoom and pan the section alone.
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

    /// <summary>The press started over the section panel: it pans the section, not the run.</summary>
    private bool _inPanel;

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Elevation is not { } elevation || _viewport is not { } viewport) return;

        var at = e.GetPosition(this);
        double factor = Math.Pow(WheelStep, e.Delta / 120.0);
        if (SectionPanel() is { } panel && panel.Contains(at))
        {
            _sectionView = _sectionView.ZoomAt(factor, at, panel);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _viewport = viewport.ZoomAt(factor, at.X, at.Y, MinScale(elevation), MaxScale);
        _userFramed = true;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Middle)) return;

        Focus();
        var at = e.GetPosition(this);
        bool inPanel = SectionPanel() is { } panel && panel.Contains(at);
        if (e.ChangedButton == MouseButton.Middle && e.ClickCount == 2)
        {
            EndPress();
            if (inPanel)
            {
                _sectionView = KataSectionView.Fitted;
                InvalidateVisual();
            }
            else FrameAll();
            e.Handled = true;
            return;
        }

        _pressButton = e.ChangedButton;
        _pressAt = _lastDrag = at;
        _moved = false;
        _inPanel = inPanel;
        // The middle button (or Shift + left, for a touchpad) pans; the left button selects on release.
        _panning = e.ChangedButton == MouseButton.Middle || (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
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

        if (_inPanel) _sectionView = _sectionView.PanBy(at - _lastDrag);
        else
        {
            _viewport = viewport.PanBy(at.X - _lastDrag.X, at.Y - _lastDrag.Y);
            _userFramed = true;
        }

        _lastDrag = at;
        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_pressButton != e.ChangedButton) return;

        bool click = e.ChangedButton == MouseButton.Left && !_panning && !_moved && (e.GetPosition(this) - _pressAt).Length < DragThresholdPx;
        if (click && Elevation is { } elevation && _viewport is { } viewport)
        {
            var at = e.GetPosition(this);
            if (SectionPanel() is { } panel && panel.Contains(at))
            {
                // The section panel is not part of the run: only its close button answers a click.
                if (CloseButton(panel).Contains(at)) CloseSection();
            }
            else if (FlagAt(elevation, viewport, at) is { } flag)
            {
                OpenSection(flag);
            }
            else if (SelectBarAt(elevation, viewport, at))
            {
                // A bar group is selected; the column stays as it is.
            }
            else
            {
                ClearBarSelection();
                double station = viewport.ToStation(at.X);
                // SetCurrentValue keeps the binding to the view model alive whatever its mode.
                if (elevation.ColumnAt(station, ZeroWidthReachPx / viewport.Scale) is { } column)
                    SetCurrentValue(SelectedColumnIndexProperty, column);
            }
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
        _inPanel = false;
        Cursor = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private static MouseButtonState Pressed(MouseEventArgs e, MouseButton button) =>
        button == MouseButton.Middle ? e.MiddleButton : e.LeftButton;
}
