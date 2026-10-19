using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using HPRebar.BeamRebar.ViewModel;

namespace HPRebar.BeamRebar.View.Controls;

/// <summary>
/// High-performance 3D interactive viewport for continuous beam reinforcement visualization.
/// Built on HelixToolkit.Wpf with reactive MVVM updates (<50ms debounce) and 60+ FPS rendering.
/// </summary>
public partial class Beam3DViewport : UserControl
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(50);

    public static readonly DependencyProperty SessionProperty = DependencyProperty.Register(
        nameof(Session),
        typeof(BeamRebarSession),
        typeof(Beam3DViewport),
        new PropertyMetadata(null, OnSessionChanged));

    private readonly DispatcherTimer _redrawTimer;
    private bool _hasInitializedCamera;
    private bool _isAttached;

    public Beam3DViewport()
    {
        InitializeComponent();

        // Revit 100% Navigation gestures:
        // Orbit: Shift + MiddleClick (primary, Revit standard) and RightClick (secondary shortcut)
        HelixView.RotateGesture = new MouseGesture(MouseAction.MiddleClick, ModifierKeys.Shift);
        HelixView.RotateGesture2 = new MouseGesture(MouseAction.RightClick);

        // Pan: MiddleClick (primary, Revit standard)
        HelixView.PanGesture = new MouseGesture(MouseAction.MiddleClick);
        HelixView.PanGesture2 = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);

        // Smooth camera, Revit-style turntable and zoom behavior
        HelixView.CameraRotationMode = CameraRotationMode.Turntable;
        HelixView.ZoomAroundMouseDownPoint = true;
        HelixView.RotateAroundMouseDownPoint = true;
        HelixView.ZoomExtentsGesture = new MouseGesture(MouseAction.MiddleDoubleClick);
        HelixView.CameraInertiaFactor = 0.92;
        HelixView.IsChangeFieldOfViewEnabled = false;
        HelixView.InfiniteSpin = false;

        _redrawTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = DebounceDelay
        };
        _redrawTimer.Tick += OnRedrawTimerTick;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public BeamRebarSession? Session
    {
        get => (BeamRebarSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    /// <summary>
    /// Resets the 3D camera to fit the entire continuous beam geometry.
    /// </summary>
    public void ResetCamera()
    {
        HelixView.ZoomExtents(500);
    }

    private void OnResetCameraClick(object sender, RoutedEventArgs e)
    {
        ResetCamera();
    }

    private void OnHelixViewPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && e.ClickCount == 2)
        {
            ResetCamera();
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Session is not null)
        {
            if (!_isAttached)
            {
                Attach(Session);
            }

            TriggerRedraw();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _redrawTimer.Stop();
        Detach(Session);
    }

    private static void OnSessionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Beam3DViewport viewport)
        {
            viewport.Detach(e.OldValue as BeamRebarSession);
            viewport.Attach(e.NewValue as BeamRebarSession);
            viewport.TriggerRedraw();
        }
    }

    private void Attach(BeamRebarSession? session)
    {
        if (session is null || _isAttached) return;

        session.PropertyChanged += OnSessionPropertyChanged;
        session.SupportTopBars.CollectionChanged += OnEditorsCollectionChanged;
        session.SpanBottomBars.CollectionChanged += OnEditorsCollectionChanged;

        foreach (var top in session.SupportTopBars) top.PropertyChanged += OnEditorPropertyChanged;
        foreach (var bot in session.SpanBottomBars) bot.PropertyChanged += OnEditorPropertyChanged;

        _isAttached = true;
    }

    private void Detach(BeamRebarSession? session)
    {
        if (session is null || !_isAttached) return;

        session.PropertyChanged -= OnSessionPropertyChanged;
        session.SupportTopBars.CollectionChanged -= OnEditorsCollectionChanged;
        session.SpanBottomBars.CollectionChanged -= OnEditorsCollectionChanged;

        foreach (var top in session.SupportTopBars) top.PropertyChanged -= OnEditorPropertyChanged;
        foreach (var bot in session.SpanBottomBars) bot.PropertyChanged -= OnEditorPropertyChanged;

        _isAttached = false;
    }

    private void OnEditorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems) item.PropertyChanged -= OnEditorPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems) item.PropertyChanged += OnEditorPropertyChanged;
        }

        TriggerRedraw();
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TriggerRedraw();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        TriggerRedraw();
    }

    private void TriggerRedraw()
    {
        _redrawTimer.Stop();
        _redrawTimer.Start();
    }

    private void OnRedrawTimerTick(object? sender, EventArgs e)
    {
        _redrawTimer.Stop();
        RebuildScene();
    }

    private void RebuildScene()
    {
        var session = Session;
        if (session is null || session.Stack.Spans.Count == 0)
        {
            BeamVisual.Content = null;
            return;
        }

        var stack = session.Stack.ContinuousStack;
        var spec = session.ToSpec();

        var sceneGroup = Beam3DSceneBuilder.BuildScene(stack, spec);
        BeamVisual.Content = sceneGroup;

        UpdateGridAlignment(stack);

        if (!_hasInitializedCamera && IsLoaded)
        {
            _hasInitializedCamera = true;
            Dispatcher.BeginInvoke(new Action(() => HelixView.ZoomExtents(500)), DispatcherPriority.Loaded);
        }
    }

    private void UpdateGridAlignment(HPRebar.Core.BeamRebar.Models.BeamContinuousStack stack)
    {
        double totalLen = Math.Max(stack.TotalLength, 6000.0);
        double midX = (stack.OverallStartX + stack.OverallEndX) / 2.0;
        double soffitZ = stack.Spans.Count > 0 ? stack.Spans.Min(s => s.BottomElevation) : 0.0;

        GridLines.Length = totalLen + 4000.0;
        GridLines.Width = Math.Max(stack.MaxHeight * 4.0, 3000.0);
        GridLines.Transform = new TranslateTransform3D(midX, 0, soffitZ - 100.0);
    }
}
