namespace HPDcePro.Excel
{
    public sealed class ExcelActionResult
    {
        public bool IsSuccess { get; set; } = true;
        public bool Success
        {
            get => IsSuccess;
            set => IsSuccess = value;
        }

        public string ActionKey { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? GeneratedFilePath { get; set; }
        public double ExecutionTimeMs { get; set; }

        public static ExcelActionResult Ok(string actionKey, string message, object? data = null, string? filePath = null) =>
            new() { IsSuccess = true, ActionKey = actionKey, Message = message, Data = data, GeneratedFilePath = filePath };

        public static ExcelActionResult SuccessResult(string actionKey, string message, object? data = null, string? filePath = null) =>
            Ok(actionKey, message, data, filePath);

        public static ExcelActionResult Fail(string actionKey, string message, object? data = null) =>
            new() { IsSuccess = false, ActionKey = actionKey, Message = message, Data = data };

        public static ExcelActionResult Failed(string actionKey, string message, object? data = null) =>
            Fail(actionKey, message, data);

        public static ExcelActionResult NotFound(string actionKey) =>
            new() { IsSuccess = false, ActionKey = actionKey, Message = $"Action '{actionKey}' is not supported or not found." };
    }
}
