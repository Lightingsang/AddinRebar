using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace HPRebar.Core.Tests;

/// <summary>
///     Canonical text of a calculator's output — every public readable property, recursively, numbers rounded to
///     1e-6 — and a short hash of it. Characterization tests pin that hash before a calculator is restructured.
/// </summary>
internal static class CharacterizationText
{
    public static string Hash(object? value)
    {
        var text = new StringBuilder();
        Write(text, value, depth: 0);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
        return BitConverter.ToString(bytes, 0, 12).Replace("-", string.Empty);
    }

    private static void Write(StringBuilder text, object? value, int depth)
    {
        if (depth > 8)
        {
            throw new InvalidOperationException("Object graph too deep for a characterization text.");
        }

        switch (value)
        {
            case null:
                text.Append("null");
                return;
            case double number:
                text.Append(Math.Round(number, 6).ToString("0.######", CultureInfo.InvariantCulture));
                return;
            case float number:
                text.Append(Math.Round(number, 6).ToString("0.######", CultureInfo.InvariantCulture));
                return;
            case string or bool or int or long or Enum:
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            case IEnumerable items:
                text.Append('[');
                foreach (var item in items)
                {
                    Write(text, item, depth + 1);
                    text.Append(';');
                }

                text.Append(']');
                return;
        }

        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);

        text.Append('{');
        foreach (var property in properties)
        {
            text.Append(property.Name).Append('=');
            Write(text, property.GetValue(value), depth + 1);
            text.Append(',');
        }

        text.Append('}');
    }
}
