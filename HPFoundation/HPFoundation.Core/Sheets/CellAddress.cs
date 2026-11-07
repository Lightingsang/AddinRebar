namespace HPFoundation.Core.Sheets;

/// <summary>One-based position of a worksheet cell.</summary>
public readonly record struct CellAddress(int Row, int Column)
{
    private const int LettersInAlphabet = 26;

    public static CellAddress Parse(string text)
    {
        var letterCount = text.TakeWhile(char.IsAsciiLetterUpper).Count();
        var digits = text[letterCount..];
        if (letterCount == 0
            || digits.Length == 0
            || !digits.All(char.IsAsciiDigit)
            || !int.TryParse(digits, out var row)
            || row < 1)
        {
            throw new ArgumentException($"'{text}' is not a cell address such as F34.", nameof(text));
        }

        var column = text[..letterCount].Aggregate(0, (total, letter) => total * LettersInAlphabet + letter - 'A' + 1);
        return new CellAddress(row, column);
    }

    public override string ToString() => $"{ColumnLetters(Column)}{Row}";

    private static string ColumnLetters(int column)
    {
        var letters = string.Empty;
        for (var remaining = column; remaining > 0; remaining = (remaining - 1) / LettersInAlphabet)
        {
            letters = (char)('A' + (remaining - 1) % LettersInAlphabet) + letters;
        }

        return letters;
    }
}
