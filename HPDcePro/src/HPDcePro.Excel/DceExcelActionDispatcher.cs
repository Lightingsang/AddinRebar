using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HPDcePro.Excel.ActionHandlers;

namespace HPDcePro.Excel
{
    public sealed class DceExcelActionDispatcher : IDceExcelActionDispatcher
    {
        private readonly List<IExcelActionHandler> _handlers;
        private readonly Dictionary<string, IExcelActionHandler> _primaryActionMap;

        public DceExcelActionDispatcher(IEnumerable<IExcelActionHandler>? customHandlers = null)
        {
            _handlers = customHandlers?.ToList() ?? new List<IExcelActionHandler>
            {
                new BeamActionHandler(),
                new ColumnWallActionHandler(),
                new SlabActionHandler(),
                new FoundationActionHandler(),
                new RebarCuttingActionHandler(),
                new CadBridgeActionHandler(),
                new EtabsSafeActionHandler(),
                new ApiSystemActionHandler()
            };

            _primaryActionMap = new Dictionary<string, IExcelActionHandler>(StringComparer.OrdinalIgnoreCase);
            foreach (var handler in _handlers)
            {
                foreach (var action in handler.SupportedActions)
                {
                    _primaryActionMap[action] = handler;
                }
            }
        }

        public IReadOnlyList<string> GetSupportedActions() =>
            _primaryActionMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        public bool CanExecute(string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey)) return false;
            if (_primaryActionMap.ContainsKey(actionKey)) return true;
            return _handlers.Any(h => h.CanHandle(actionKey));
        }

        public bool CanHandle(string actionKey) => CanExecute(actionKey);

        public ExcelActionResult ExecuteAction(string actionKey, ExcelActionContext context)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                return ExcelActionResult.Fail(string.Empty, "Mã hành động không được rỗng.");
            }

            if (context == null) throw new ArgumentNullException(nameof(context));

            var handler = _handlers.FirstOrDefault(h => h.CanHandle(actionKey));
            if (handler == null)
            {
                return ExcelActionResult.NotFound(actionKey);
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var result = handler.Handle(actionKey, context);
                sw.Stop();
                result.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
                return result;
            }
            catch (Exception ex)
            {
                // Reason: Trap action execution failure and return structured failed result without process abort
                sw.Stop();
                var failed = ExcelActionResult.Fail(actionKey, $"Thực thi '{actionKey}' thất bại: {ex.Message}");
                failed.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
                return failed;
            }
        }
    }
}
