using System;

namespace HPRobot.McpBridge.Resources.Themes;

/// <summary>
///     Where a window learns whether the host UI is dark and when that changes.
/// </summary>
public interface IHostTheme
{
    bool IsDark { get; }

    event Action? Changed;
}
