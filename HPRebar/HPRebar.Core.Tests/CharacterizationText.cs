using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace HPRebar.Core.Tests;

/// <summary>
///     Canonical text of a calculator's output and a short hash of it. Characterization tests pin that hash
///     before a calculator is restructured. Doubles are rounded to 1e-6; other primitives, enums and strings are
///     written invariantly; collections item by item; HPRebar.Core types through every public property. Anything
///     else — a type outside HPRebar.Core, a public field, an object with nothing to write — throws, so a value
///     can never drop out of the hash unnoticed.
/// </summary>
internal static class CharacterizationText
{
    private static readonly Assembly CoreAssembly = typeof(HPRebar.Core.Shared.RevitRebarLimits).Assembly;

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
            case string or bool or char or Enum:
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                text.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
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

        var type = value.GetType();
        if (type.Assembly != CoreAssembly)
        {
            throw new InvalidOperationException($"{type} is outside HPRebar.Core; teach CharacterizationText to write it.");
        }

        if (type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length > 0)
        {
            throw new InvalidOperationException($"{type} has public fields, which this writer does not read.");
        }

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToList();

        if (properties.Count == 0)
        {
            throw new InvalidOperationException($"{type} has no public properties to write.");
        }

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
