using System;

namespace HPDcePro.UI.Common
{
    /// <summary>
    /// Contract for host applications providing dynamic Dark/Light theme events.
    /// </summary>
    public interface IHostTheme
    {
        bool IsDark { get; }
        event Action? Changed;
    }
}
