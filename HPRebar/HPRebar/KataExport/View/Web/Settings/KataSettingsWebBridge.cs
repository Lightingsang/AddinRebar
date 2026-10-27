using System.Windows.Threading;
using HPRebar.Core.KataExport.Settings;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Service;
using HPRebar.Resources.Themes;
using HPWebHost.Protocol;

namespace HPRebar.KataExport.View.Web.Settings;

/// <summary>
///     The settings page's host side: the settings file read into a form, checked and written by the Core rules. No
///     Revit API is touched, so every command answers on the window's UI thread without the Revit queue.
/// </summary>
internal sealed class KataSettingsWebBridge : IDisposable
{
    private readonly IHostTheme _theme;
    private readonly Dispatcher _dispatcher;
    private readonly Action<string, object> _publish;
    private readonly Action _close;

    /// <param name="publish">Sends an event to the page (dropped while the page is not up).</param>
    /// <param name="dispatcher">The window's: the page is only ever written to from it.</param>
    /// <param name="close">Closes the window.</param>
    public KataSettingsWebBridge(IHostTheme theme, Dispatcher dispatcher, Action<string, object> publish, Action close)
    {
        _theme = theme;
        _dispatcher = dispatcher;
        _publish = publish;
        _close = close;
        _theme.Changed += OnThemeChanged;
    }

    /// <summary>A form was accepted, but the settings file could not be written.</summary>
    public bool FileFailed { get; private set; }

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(KataSettingsWebCommands.Load, (_, _) => Answer(Load()));
        dispatcher.Register<KataSettingsForm>(KataSettingsWebCommands.Validate, (form, _) => Answer(new KataSettingsValidation(Check(form))));
        dispatcher.Register<KataSettingsForm>(KataSettingsWebCommands.Save, (form, _) => Answer(Save(form)));
        dispatcher.Register(KataSettingsWebCommands.Defaults, (_, _) => Answer(KataSettingsFormMapper.Snapshot(KataSettingsFile.Default)));
        dispatcher.Register(KataSettingsWebCommands.Close, (_, _) =>
        {
            _close();
            return Answer(null);
        });
    }

    public void Dispose() => _theme.Changed -= OnThemeChanged;

    private static Task<object?> Answer(object? value) => Task.FromResult(value);

    private KataSettingsPageState Load() => new(ThemeName(), KataSettingsFormMapper.Snapshot(KataSettingsStore.LoadFile()));

    private KataSettingsSaveResult Save(KataSettingsForm form)
    {
        var errors = Check(form);
        if (errors.Count > 0) return new KataSettingsSaveResult(false, false, errors);

        bool written = KataSettingsStore.SaveFile(KataSettingsFormMapper.ToFile(form));
        FileFailed = !written;
        return new KataSettingsSaveResult(true, !written, errors);
    }

    /// <summary>The rules' verdict; a payload without one of the three tabs is the page's mistake, not a wrong value.</summary>
    private static IReadOnlyList<KataSettingsError> Check(KataSettingsForm form)
    {
        if (form.Detail is null || form.Special is null || form.Joints?.Beam is null || form.Joints.Column is null
            || form.Special.MassRows is null || form.Special.LapRows is null)
            throw new BridgeException(BridgeErrorCodes.BadRequest, "The settings form must hold detail, special and joints (beam, column).");
        return KataSettingsRules.Validate(form);
    }

    private void OnThemeChanged() =>
        _dispatcher.BeginInvoke(() => _publish(KataSettingsWebEvents.Theme, new KataSettingsThemeEvent(ThemeName())));

    private string ThemeName() => _theme.IsDark ? "dark" : "light";
}
