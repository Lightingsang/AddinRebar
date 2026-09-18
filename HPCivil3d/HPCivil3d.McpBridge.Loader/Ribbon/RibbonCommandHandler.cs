using System.Windows.Input;

namespace HPCivil3d.McpBridge.Loader.Ribbon;

/// <summary>
///     The ICommand behind a Ribbon button: runs one action on AutoCAD's main thread (where Ribbon clicks
///     arrive) and never lets an exception reach the Ribbon — it would surface as a silent dead button.
///     The button is created disabled when the bridge failed to start, so CanExecute stays simple.
/// </summary>
internal sealed class RibbonCommandHandler(string name, Action action) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        try
        {
            action();
        }
        catch (System.Exception exception)
        {
            LoaderLog.Write($"ribbon '{name}' failed", exception);
        }
    }
}
