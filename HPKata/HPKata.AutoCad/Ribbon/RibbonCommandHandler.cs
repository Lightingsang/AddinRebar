using System;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Ribbon
{
    /// <summary>
    /// Safe ICommand relay for HPKata AutoCAD Ribbon buttons.
    /// Executes the command on AutoCAD's UI thread and prevents unhandled exceptions from reaching AdWindows.
    /// </summary>
    public sealed class RibbonCommandHandler : ICommand
    {
        private readonly string _commandName;
        private readonly Action _action;

        public RibbonCommandHandler(string commandName, Action action)
        {
            _commandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            try
            {
                _action();
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: prevent unhandled exception in AdWindows Ribbon click
                var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\n[HPKata Ribbon] Lỗi thực thi lệnh '{_commandName}': {ex.Message}\n");
            }
        }
    }
}
