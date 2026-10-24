using HPBimToolkit.Core.Protocol;

namespace HPBimToolkit.Modules;

/// <summary>
///     A toolkit module (Dashboard now; Element Explorer, Parameter Manager, Reports later) registers its commands on the
///     shared dispatcher and reuses the queue, transport and services the host built.
/// </summary>
public interface IToolkitModule
{
    void Register(CommandDispatcher dispatcher);
}
