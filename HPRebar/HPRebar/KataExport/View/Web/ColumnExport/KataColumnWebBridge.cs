using System.Windows.Threading;
using HPRebar.Core.KataExport.ColumnStack;
using HPRebar.KataExport.Service;
using HPRebar.Resources.Themes;
using HPWebHost.Protocol;

namespace HPRebar.KataExport.View.Web.ColumnExport;

/// <summary>
///     Host side of the Kata Column Rebar page. Step 1 writes the measured stack into sheet Cot (Excel through COM on
///     the window's UI thread); step 2 reads the sheet back, plans the bars and creates them through
///     <see cref="KataColumnRebarSession" />. The window is modal over the command, so step 2 runs on Revit's thread.
///     Handlers are synchronous: the dispatcher's timeout starts only after they return, the page bounds the wait.
/// </summary>
internal sealed class KataColumnWebBridge : IDisposable
{
    /// <summary>The step the page opens on.</summary>
    private const string StartMode = "export";

    private readonly KataColumnStackInput _input;
    private readonly KataColumnSheet _sheet;
    private readonly KataColumnRebarSession _rebar;
    private readonly IHostTheme _theme;
    private readonly Dispatcher _dispatcher;
    private readonly Action<string, object> _publish;
    private readonly Action _close;
    private string _probedWorkbook = string.Empty;

    /// <summary>The workbook and column name step 1 last wrote, so step 2 can tell a sheet read from elsewhere.</summary>
    private string? _writtenWorkbook;
    private string? _writtenName;

    /// <param name="publish">Sends an event to the page (dropped while the page is not up).</param>
    /// <param name="close">Closes the window.</param>
    public KataColumnWebBridge(KataColumnStackInput input, KataColumnSheet sheet, KataColumnRebarSession rebar, IHostTheme theme,
        Dispatcher dispatcher, Action<string, object> publish, Action close)
    {
        _input = input;
        _sheet = sheet;
        _rebar = rebar;
        _theme = theme;
        _dispatcher = dispatcher;
        _publish = publish;
        _close = close;
        _theme.Changed += OnThemeChanged;
    }

    public void Register(CommandDispatcher dispatcher)
    {
        dispatcher.Register(KataColumnWebCommands.Load, (_, _) => Answer(State()));
        dispatcher.Register(KataColumnWebCommands.Probe, (_, _) => Answer(Probe()));
        dispatcher.Register<KataColumnWriteRequest>(KataColumnWebCommands.Write, (request, _) => Answer(Write(request)));
        dispatcher.Register(KataColumnWebCommands.Reload, (_, _) => Answer(State()));
        dispatcher.Register(KataColumnWebCommands.Generate, (_, _) => Answer(Generate()));
        dispatcher.Register(KataColumnWebCommands.Close, (_, _) =>
        {
            _close();
            return Answer(null);
        });
    }

    public void Dispose() => _theme.Changed -= OnThemeChanged;

    private static Task<object?> Answer(object? value) => Task.FromResult(value);

    /// <summary>The stack to write, the plan read from sheet Cot as it is now, and the Excel state.</summary>
    private KataColumnPageState State()
    {
        var plan = _rebar.Plan(_writtenWorkbook, _writtenName);
        return new KataColumnPageState(ThemeName(), StartMode, KataColumnPreviewMapper.Map(_input, _sheet), plan.Summary, Probe());
    }

    private KataColumnGenerateResponse Generate()
    {
        var result = _rebar.Generate();
        return new KataColumnGenerateResponse(result.Success, result.Created, result.Deleted, result.Warnings, result.Error);
    }

    private KataColumnWorkbook Probe()
    {
        var probe = KataColumnExcelWriter.Probe();
        _probedWorkbook = probe.Success ? probe.WorkbookFullName : string.Empty;
        return new KataColumnWorkbook(probe.Success, probe.WorkbookName, probe.ErrorMessage);
    }

    private KataColumnWriteResponse Write(KataColumnWriteRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name) || request.Count < 1)
        {
            throw new BridgeException(BridgeErrorCodes.BadRequest, "The column needs a name and a count of at least 1.");
        }

        if (_probedWorkbook.Length == 0)
        {
            return new KataColumnWriteResponse(false, 0, "Chưa có workbook Kata hợp lệ — bấm \"Kiểm tra Excel\".", Array.Empty<string>(), Probe());
        }

        string name = request.Name.Trim();
        var header = new Dictionary<string, object?>(_sheet.Header) { ["C3"] = name, ["C4"] = request.Count };
        string target = _probedWorkbook;
        var result = KataColumnExcelWriter.Write(_sheet with { Header = header }, target);
        if (result.Success)
        {
            _writtenWorkbook = target;
            _writtenName = name;
        }

        return new KataColumnWriteResponse(result.Success, result.StoreysWritten, result.ErrorMessage, result.RowsWithoutBars, Probe());
    }

    private void OnThemeChanged() =>
        _dispatcher.BeginInvoke(() => _publish(KataColumnWebEvents.Theme, new KataColumnThemeEvent(ThemeName())));

    private string ThemeName() => _theme.IsDark ? "dark" : "light";
}
