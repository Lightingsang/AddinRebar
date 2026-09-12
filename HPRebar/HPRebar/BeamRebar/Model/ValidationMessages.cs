using System.Collections.Generic;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// English failure messages for continuous beam validation rules.
/// </summary>
public static class ValidationMessages
{
    private static readonly IReadOnlyDictionary<int, string> Messages = new Dictionary<int, string>
    {
        [0] = "OK",
        [1] = "No beam elements selected.",
        [2] = "One of the selected elements is not a Structural Framing instance.",
        [3] = "One of the selected beams is curved or non-linear. Only straight beams are supported.",
        [4] = "One of the selected beams does not contain exactly one solid with real volume.",
        [5] = "One of the selected beams does not have a valid rectangular cross-section.",
        [6] = "The selected beams are not collinear (longitudinal axis angle > 1.0°).",
        [7] = "The selected beams are offset laterally from the continuous longitudinal axis (> 10.0 mm).",
        [8] = "The selected beams do not share the same reference level or top elevation plane.",
        [9] = "The selected beams are not continuous — there is a gap or disconnected span between them.",
        [10] = "A beam span has non-positive dimensions (width <= 0, height <= 0, or length <= 0).",
        [11] = "No supporting columns, walls, or framing girders could be identified under the beam run.",

        // Family availability pre-flight checks (Code 20-29)
        [20] = "Rebar Shape family 'M_T1' is not loaded in the project. Load it before creating stirrups.",
        [21] = "Rebar Shape family 'M_T10' is not loaded in the project. Load it before creating cross-ties.",
        [22] = "This document contains no Rebar Bar Types. Please load a rebar family first.",
        [23] = "Required 90° or 135° Rebar Hook Type is missing in the project."
    };

    public static string For(int code) =>
        Messages.TryGetValue(code, out var message) ? message : $"Unknown validation failure (code {code}).";
}
