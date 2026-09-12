namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Outcome of checking a picked continuous beam stack.
/// Code 0 indicates success. Any non-zero code corresponds to an identified rule failure.
/// </summary>
public sealed record ValidationResult
{
    private ValidationResult(int code, string message)
    {
        Code = code;
        Message = message;
    }

    public int Code { get; }

    public string Message { get; }

    public bool IsOk => Code == 0;

    public bool IsSuccess => IsOk;

    public static ValidationResult Ok { get; } = new(0, ValidationMessages.For(0));

    public static ValidationResult Fail(int code) => new(code, ValidationMessages.For(code));

    public static ValidationResult Fail(int code, string customMessage) => new(code, customMessage);

    public static ValidationResult Fail(string customMessage) => new(-1, customMessage);
}
