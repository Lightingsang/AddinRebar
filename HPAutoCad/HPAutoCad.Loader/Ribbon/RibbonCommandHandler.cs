using System;
using System.Windows.Input;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Safe ICommand relay for Ribbon buttons. Executes action on AutoCAD's main UI thread
/// and prevents unhandled exceptions from reaching AdWindows (which would cause silent dead buttons).
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
        catch (Exception exception)
        {
            LoaderLog.Write($"ribbon command '{name}' failed", exception);
        }
    }
}
