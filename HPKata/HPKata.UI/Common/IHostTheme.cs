using System;

namespace HPKata.UI.Common
{
    /// <summary>
    /// Contract for host applications (AutoCAD, Revit, standalone) providing dynamic Dark/Light theme events.
    /// </summary>
    public interface IHostTheme
    {
        bool IsDark { get; }
        event Action? Changed;
    }
}
