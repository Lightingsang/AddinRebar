using System.Reflection;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Infrastructure;
using HPBimToolkit.Services;
using HPWebHost.Protocol;

namespace HPBimToolkit.Modules;

/// <summary>Handshake: tells the page the add-in version, the Revit version, the UI theme and what it can call.</summary>
public sealed class AppModule(string revitVersion, HostThemeState theme) : IToolkitModule
{
    public void Register(CommandDispatcher dispatcher)
    {
        var addinVersion = typeof(AppModule).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                           ?? typeof(AppModule).Assembly.GetName().Version?.ToString()
                           ?? "0.0.0";

        // Built at call time: the theme can change while Revit runs, and the command list is complete only after every
        // module registered.
        dispatcher.Register(CommandNames.Hello, (_, _) => Task.FromResult<object?>(new HelloResult(
            addinVersion,
            revitVersion,
            ThemeNames.From(theme.IsDark),
            dispatcher.Commands.Concat(ToolLauncher.Tools.Keys.Select(tool => $"tool:{tool}")).ToArray())));
    }
}
