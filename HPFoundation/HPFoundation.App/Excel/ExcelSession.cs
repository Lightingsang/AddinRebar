using System.Diagnostics;
using System.Runtime.InteropServices;
using HPFoundation.Core.Sheets;

namespace HPFoundation.App.Excel;

/// <summary>
///     A hidden Excel of our own around one workbook copy; the engineer's Excel is never touched.
///     Calls stay late-bound because casting to the interop interfaces fails with TYPE_E_ELEMENTNOTFOUND on the dev machine.
/// </summary>
public sealed class ExcelSession : IDisposable
{
    private const int DontUpdateLinks = 0;
    private const int ForceDisableMacros = 3;
    private const int WaitForExitMs = 5000;

    /// <summary>A password prompt or a stuck recalculation never returns, and a hidden Excel cannot be answered.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(120);

    private static readonly Dictionary<int, string> ExcelErrors = new()
    {
        [-2146826288] = "#NULL!",
        [-2146826281] = "#DIV/0!",
        [-2146826273] = "#VALUE!",
        [-2146826265] = "#REF!",
        [-2146826259] = "#NAME?",
        [-2146826252] = "#NUM!",
        [-2146826246] = "#N/A",
    };

    private readonly dynamic _excel;
    private readonly Process _process;
    private readonly ProcessJob _job;
    private readonly Timer _watchdog;
    private dynamic? _workbook;
    private bool _disposed;

    private ExcelSession(object excel, Process process)
    {
        _excel = excel;
        _process = process;
        _job = new ProcessJob(process);
        _watchdog = new Timer(_ => KillStuckProcess());
    }

    public static ExcelSession Open(string workbookPath)
    {
        var excelType = Type.GetTypeFromProgID("Excel.Application")
            ?? throw new InvalidOperationException("Microsoft Excel is not installed.");

        dynamic excel = Activator.CreateInstance(excelType)!;
        var session = new ExcelSession((object)excel, ProcessOf((int)excel.Hwnd));
        try
        {
            session.OpenHidden(workbookPath);
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public IReadOnlyList<string> VisibleSheetNames()
    {
        const int Visible = -1;
        return Guarded<IReadOnlyList<string>>(() =>
        {
            int sheetCount = Workbook.Worksheets.Count;
            return Enumerable
                .Range(1, sheetCount)
                .Select(index => (object)Workbook.Worksheets[index])
                .Select(sheet => (dynamic)sheet)
                .Where(sheet => (int)sheet.Visible == Visible)
                .Select(sheet => (string)sheet.Name)
                .ToList();
        });
    }

    public SheetGrid ReadGrid(string sheetName) => Guarded(() =>
    {
        var usedRange = Workbook.Worksheets[sheetName].UsedRange;
        int firstRow = usedRange.Row;
        int firstColumn = usedRange.Column;
        object[,] values = ToMatrix((object?)usedRange.Value2);
        object[,] formulas = ToMatrix((object?)usedRange.Formula);
        return new SheetGrid(sheetName, ToCells(values, formulas, firstRow, firstColumn));
    });

    public void WriteFormula(string sheetName, CellAddress address, string formula) => Guarded(() =>
    {
        Workbook.Worksheets[sheetName].Range[address.ToString()].Formula = formula;
        return true;
    });

    public void CalculateAll() => Guarded(() =>
    {
        _excel.CalculateFull();
        return true;
    });

    public void Save() => Guarded(() =>
    {
        Workbook.Save();
        return true;
    });

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watchdog.Dispose();
        try
        {
            CloseExcel();
        }
        finally
        {
            EnsureProcessEnded();
            _job.Dispose();
            _process.Dispose();
        }
    }

    private dynamic Workbook => _workbook ?? throw new InvalidOperationException("The workbook is not open.");

    private void OpenHidden(string workbookPath) => Guarded(() =>
    {
        _excel.Visible = false;
        _excel.DisplayAlerts = false;
        _excel.AutomationSecurity = ForceDisableMacros;
        _excel.EnableEvents = false;
        _workbook = _excel.Workbooks.Open(workbookPath, DontUpdateLinks, false);
        return true;
    });

    /// <summary>Runs one Excel call under the watchdog, which ends only the Excel this session started.</summary>
    private T Guarded<T>(Func<T> excelCall)
    {
        _watchdog.Change(OperationTimeout, Timeout.InfiniteTimeSpan);
        try
        {
            return excelCall();
        }
        finally
        {
            _watchdog.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    private void KillStuckProcess()
    {
        if (!_process.HasExited)
        {
            _process.Kill();
        }
    }

    /// <summary>Excel returns a bare value, not an array, when the used range is one cell.</summary>
    private static object[,] ToMatrix(object? rangeValue)
    {
        if (rangeValue is object[,] matrix)
        {
            return matrix;
        }

        var single = (object[,])Array.CreateInstance(typeof(object), [1, 1], [1, 1]);
        single.SetValue(rangeValue, 1, 1);
        return single;
    }

    private static Dictionary<CellAddress, SheetCell> ToCells(object[,] values, object[,] formulas, int firstRow, int firstColumn)
    {
        var cells = new Dictionary<CellAddress, SheetCell>();
        for (var row = values.GetLowerBound(0); row <= values.GetUpperBound(0); row++)
        {
            for (var column = values.GetLowerBound(1); column <= values.GetUpperBound(1); column++)
            {
                if (ToCell(values[row, column], formulas[row, column]) is { } cell)
                {
                    cells[new CellAddress(firstRow + row - 1, firstColumn + column - 1)] = cell;
                }
            }
        }

        return cells;
    }

    private static SheetCell? ToCell(object? value, object? formula)
    {
        var formulaText = formula as string;
        var keptFormula = formulaText is not null && formulaText.StartsWith('=') ? formulaText : null;
        return value switch
        {
            null => null,
            double number => SheetCell.FromNumber(number) with { Formula = keptFormula },
            bool flag => SheetCell.FromText(flag.ToString()) with { Formula = keptFormula },
            int code when ExcelErrors.TryGetValue(code, out var errorText) =>
                SheetCell.FromError(errorText) with { Formula = keptFormula },
            string text when text.Length == 0 => null,
            _ => SheetCell.FromText(Convert.ToString(value) ?? string.Empty) with { Formula = keptFormula },
        };
    }

    private void CloseExcel()
    {
        try
        {
            if (_workbook is not null)
            {
                _workbook.Close(false);
            }

            _excel.Quit();
        }
        catch (COMException)
        {
            // Excel may already be gone; EnsureProcessEnded still checks the process.
        }
        finally
        {
            ReleaseComObjects();
        }
    }

    private void ReleaseComObjects()
    {
        if (_workbook is not null)
        {
            Marshal.FinalReleaseComObject((object)_workbook);
        }

        Marshal.FinalReleaseComObject((object)_excel);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>Only ever touches the process this session started.</summary>
    private void EnsureProcessEnded()
    {
        if (!_process.WaitForExit(WaitForExitMs))
        {
            _process.Kill();
        }
    }

    private static Process ProcessOf(int windowHandle)
    {
        GetWindowThreadProcessId(new IntPtr(windowHandle), out var processId);
        return Process.GetProcessById((int)processId);
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);
}
