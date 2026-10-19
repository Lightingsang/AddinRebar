namespace HPGrasshopper.McpBridge.Coordinator;

using System;

/// <summary>
///     Abstraction over Revit ExternalEvent to enable testability outside Revit.exe.
/// </summary>
public interface IExternalEventRaiser : IDisposable
{
    bool Raise();
}
