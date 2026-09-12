using System.Collections.Generic;

namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     English text for each validation rule. The localisation service takes this over once the UI lands;
///     until then the command shows these strings directly.
/// </summary>
public static class ValidationMessages
{
    private static readonly IReadOnlyDictionary<int, string> Messages = new Dictionary<int, string>
    {
        [0] = "OK",
        [1] = "The selected columns are not all the same family type.",
        [2] = "One of the columns is slanted. Only vertical columns are supported.",
        [3] = "One of the columns is built from more than one solid.",
        [4] = "The columns are not stacked continuously — there is a gap or an overlap between them.",
        [5] = "One of the columns is neither rectangular nor circular.",
        [6] = "One of the columns is rotated relative to the one below it.",
        [7] = "A column is wider than the one below it. Sections must not grow going up.",
        [8] = "A column oversails the one below it. The upper section must sit within the lower one.",
        [9] = "A beam rises above the top face of a column.",
        [10] = "Every beam framing into a column top must be joined to the column, and must not cut it.",
        [11] = "The bottom column must be joined to its foundation, and must sit on the foundation top face.",
        [12] = "The bottom column must be joined to the floor acting as its foundation, and must sit on its top face.",
        [13] = "The bottom column must be joined to the wall acting as its foundation, and must sit on its top face.",
        [14] = "The beams under the bottom column do not meet its base.",

        // Missing families are reported before any transaction opens, so nothing has to be rolled back.
        [20] = "The rebar shape family M_T1 is not loaded. Load it before creating rectangular ties.",
        [21] = "The rebar shape family M_T3 is not loaded. Load it before creating circular ties.",
        [22] = "The rebar shape family M_T10 is not loaded. Load it before creating intermediate cross-ties.",
        [23] = "This document has no rebar bar types. Load a rebar family first."
    };

    public static string For(int code) =>
        Messages.TryGetValue(code, out var message) ? message : $"Unknown validation failure (code {code}).";
}
