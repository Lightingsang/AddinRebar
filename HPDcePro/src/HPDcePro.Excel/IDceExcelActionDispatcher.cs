using System.Collections.Generic;

namespace HPDcePro.Excel
{
    public interface IDceExcelActionDispatcher
    {
        ExcelActionResult ExecuteAction(string actionKey, ExcelActionContext context);
        IReadOnlyList<string> GetSupportedActions();
        bool CanExecute(string actionKey);
        bool CanHandle(string actionKey);
    }

    public interface IExcelActionHandler
    {
        IReadOnlyList<string> SupportedActions { get; }
        bool CanHandle(string actionKey);
        ExcelActionResult Handle(string actionKey, ExcelActionContext context);
    }
}
