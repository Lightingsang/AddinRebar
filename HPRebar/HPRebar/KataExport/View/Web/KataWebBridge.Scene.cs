using System.Windows.Threading;
using HPRebar.Core.KataExport.Web;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.ViewModel;
using HPWebHost.Protocol;
using Serilog;

namespace HPRebar.KataExport.View.Web;

/// <summary>
///     What the page draws itself: the elevation, Kata's rebar elevation, tags and cuts as model-space records
///     (<see cref="KataWebSceneMapper" />), sent once per change of elevation, plan or station map — zooming and panning
///     never come back here. Sections are built when the page opens one; bar groups are struck off through the view
///     model, which sends a new scene.
/// </summary>
internal sealed partial class KataWebBridge
{
    private const int MaxRemovedKeys = 5000;

    private bool _scenePending;
    private int _sceneRevision;
    private readonly Dictionary<(int Number, bool Mirror), KataWebSection?> _sections = new();
    private KataRebarPlan? _sectionsPlan;
    private KataWeb3dModel? _model3d;
    private KataRebarPlan? _model3dPlan;

    private void OnSceneInputChanged(string? property)
    {
        switch (property)
        {
            case nameof(KataExportViewModel.Elevation):
            case nameof(KataExportViewModel.RebarPlan):
            case nameof(KataExportViewModel.RebarStationMap):
                ScheduleScene();
                break;
            case nameof(KataExportViewModel.FocusRequest):
                var focus = _viewModel.FocusRequest;
                _publish(KataWebEvents.Focus, new KataFocusEvent(focus.ColumnIndex, focus.Serial));
                break;
        }
    }

    private void ScheduleScene()
    {
        if (_scenePending) return;
        _scenePending = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            _scenePending = false;
            var vm = _viewModel;
            try
            {
                var scene = vm.Elevation is { } elevation ? KataWebSceneMapper.Map(elevation, vm.RebarPlan, vm.RebarStationMap) : null;
                _publish(KataWebEvents.Scene, new KataSceneFrame(++_sceneRevision, scene));
            }
            catch (Exception ex)
            {
                // The page keeps the scene it has; the state still tells the user what the view model did.
                Log.Error(ex, "Kata web: the scene could not be built");
            }
        });
    }

    private KataWebSection? Section(KataSectionRequest request)
    {
        var plan = _viewModel.RebarPlan
            ?? throw new BridgeException(BridgeErrorCodes.BadRequest, "There is no rebar plan: read the bars from Excel first.");
        bool mirror = request.Mirror ?? _viewModel.RebarStationMap.Direction < 0;

        if (!ReferenceEquals(plan, _sectionsPlan))
        {
            _sections.Clear();
            _sectionsPlan = plan;
        }

        if (_sections.TryGetValue((request.Number, mirror), out var cached)) return cached;
        try
        {
            return _sections[(request.Number, mirror)] = KataWebSceneMapper.MapSection(plan, request.Number, mirror);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata web: section {Number} could not be built", request.Number);
            throw new BridgeException(BridgeErrorCodes.Internal, $"Section {request.Number} could not be drawn: {ex.Message}");
        }
    }

    /// <summary>The plan in 3D, built once per plan (a removal makes a new plan).</summary>
    private KataWeb3dModel Model3d()
    {
        var plan = _viewModel.RebarPlan
            ?? throw new BridgeException(BridgeErrorCodes.BadRequest, "There is no rebar plan: read the bars from Excel first.");
        if (ReferenceEquals(plan, _model3dPlan) && _model3d is not null) return _model3d;
        try
        {
            _model3d = KataWeb3dMapper.Map(plan);
            _model3dPlan = plan;
            return _model3d;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Kata web: the 3D model could not be built");
            throw new BridgeException(BridgeErrorCodes.Internal, $"The 3D view could not be built: {ex.Message}");
        }
    }

    private object? RemoveBars(KataRemoveBarsRequest request)
    {
        var keys = request.Keys;
        if (keys is null || keys.Count == 0 || keys.Count > MaxRemovedKeys || keys.Any(string.IsNullOrWhiteSpace))
            throw new BridgeException(BridgeErrorCodes.BadRequest, $"Give 1…{MaxRemovedKeys} non-empty bar keys.");

        var command = _viewModel.RemoveBarsCommand;
        if (!command.CanExecute(keys))
            throw new BridgeException(BridgeErrorCodes.Busy, $"Bars cannot be removed now ({_viewModel.StatusMessage}).");

        command.Execute(keys);
        return null;
    }
}
