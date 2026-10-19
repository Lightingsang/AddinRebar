using System;
using System.Windows.Threading;

namespace HPRebar.JoinElements.Service;

/// <summary>
/// Pumps the WPF Dispatcher to keep the UI responsive and update progress bars in real-time during heavy Revit execution.
/// </summary>
public static class DispatcherHelper
{
    private static readonly Action EmptyAction = () => { };

    public static void DoEvents()
    {
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher 
                             ?? Dispatcher.CurrentDispatcher;
            
            dispatcher.Invoke(DispatcherPriority.Background, EmptyAction);
        }
        catch
        {
            // Intentional: Suppress if dispatcher is shutting down or unavailable
        }
    }
}
