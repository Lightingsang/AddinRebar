namespace HPFoundation.Core.Sheets;

/// <summary>The value of one cell as Excel calculated it, with its formula when it has one.</summary>
public sealed record SheetCell(double? Number, string? Text, string? ErrorText, string? Formula = null)
{
    public static SheetCell FromNumber(double number) => new(number, null, null);

    public static SheetCell FromText(string text) => new(null, text, null);

    public static SheetCell FromError(string errorText) => new(null, null, errorText);
}
