using System;
using System.Runtime.Loader;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Resources.Themes;

namespace HPAutoCad.SmartPlot.UI;

/// <summary>
/// Modeless UI window for Smart Plot Pro allowing interactive pan/zoom in AutoCAD
/// while configuring, scanning, and batch plotting frames to PDF.
/// </summary>
public partial class SmartPlotWindow : Window
{
    private static SmartPlotWindow? _instance;

    /// <summary>
    /// Displays or restores the singleton modeless SmartPlotWindow.
    /// </summary>
    public static void ShowWindow()
    {
        if (_instance is not null)
        {
            if (_instance.WindowState == WindowState.Minimized)
            {
                _instance.WindowState = WindowState.Normal;
            }
            _instance.Activate();
            return;
        }

        var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        var vm = new SmartPlotViewModel(doc);
        var window = new SmartPlotWindow(vm);
        _instance = window;

        window.Closed += (_, _) =>
        {
            vm.Dispose();
            _instance = null;
        };

        Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModelessWindow(window);
    }

    public SmartPlotWindow(SmartPlotViewModel viewModel)
    {
        // Resolve component URIs in the loader's isolated ALC
        using (AssemblyLoadContext.GetLoadContext(typeof(SmartPlotWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
        }

        DataContext = viewModel;
        viewModel.PickFrameRequested += OnPickFrameRequested;
        viewModel.CloseRequested += Close;
    }

    /// <summary>
    /// Minimizes the window to allow user to pick an entity directly from the AutoCAD viewport,
    /// then restores the window and forwards the picked ObjectId to the ViewModel.
    /// </summary>
    private void OnPickFrameRequested(Action<ObjectId> onPicked)
    {
        var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;

        WindowState = WindowState.Minimized;
        try
        {
            using (doc.LockDocument())
            {
                var ed = doc.Editor;
                var peo = new PromptEntityOptions("\nChọn khung bản vẽ (Block hoặc Polyline): ");
                peo.SetRejectMessage("\nĐối tượng phải là BlockReference hoặc Polyline khép kín.");
                peo.AddAllowedClass(typeof(BlockReference), true);
                peo.AddAllowedClass(typeof(Polyline), true);
                peo.AddAllowedClass(typeof(Polyline2d), true);

                var per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.OK)
                {
                    onPicked(per.ObjectId);
                }
            }
        }
        finally
        {
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}
