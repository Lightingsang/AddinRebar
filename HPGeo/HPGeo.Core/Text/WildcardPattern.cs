using System.Text.RegularExpressions;

namespace HPGeo.Core.Text;

/// <summary>
/// AutoCAD-style layer wildcards: <c>*</c> any run, <c>?</c> one character, <c>#</c> one digit, a comma
/// separates alternatives (<c>RANH*,DIEM-??</c>), <c>~</c> in front negates. Case-insensitive, like layer names.
/// </summary>
public sealed class WildcardPattern
{
    private readonly Regex[] _alternatives;
    private readonly bool _negated;

    public WildcardPattern(string pattern)
    {
        var p = (pattern ?? "").Trim();
        _negated = p.StartsWith('~');
        if (_negated) p = p[1..];
        _alternatives = p.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToRegex).ToArray();
        if (_alternatives.Length == 0) throw new ArgumentException("Mẫu layer trống.");
    }

    public bool IsMatch(string text)
    {
        var hit = _alternatives.Any(r => r.IsMatch(text ?? ""));
        return _negated ? !hit : hit;
    }

    private static Regex ToRegex(string glob)
    {
        var sb = new System.Text.StringBuilder("^");
        foreach (var c in glob)
        {
            sb.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                '#' => "[0-9]",
                _ => Regex.Escape(c.ToString()),
            });
        }
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
    }
}
