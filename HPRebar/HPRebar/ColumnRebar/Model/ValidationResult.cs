namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     Outcome of checking a picked column stack. <see cref="Code"/> zero means the stack is usable;
///     any other value is the number of the first rule that failed, so the user is told about the problem
///     they hit first rather than the last one checked.
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

    public static ValidationResult Ok { get; } = new(0, ValidationMessages.For(0));

    public static ValidationResult Fail(int code) => new(code, ValidationMessages.For(code));
}
