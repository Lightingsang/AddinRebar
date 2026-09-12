using System;
using Autodesk.Revit.DB;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Result of Revit Floor geometric pre-flight validation.
/// </summary>
public sealed record FoundationValidation(bool IsValid, string Message)
{
    public static FoundationValidation Success() => new(true, string.Empty);
    public static FoundationValidation Failure(string message) => new(false, message);
}

/// <summary>
/// Validates floor geometry, horizontal planar orientation, positive thickness and volume.
/// </summary>
public static class FoundationRebarValidator
{
    private const double Tolerance = 1.0e-6;

    public static FoundationValidation Validate(Element element)
    {
        if (element is null)
            return FoundationValidation.Failure("No element selected.");

        bool isFloorCategory = false;
        if (element is Floor)
        {
            isFloorCategory = true;
        }
        else
        {
            // Multi-version: ElementId
#if REVIT2024_OR_GREATER
            if (element.Category?.Id.Value == (long)BuiltInCategory.OST_Floors) isFloorCategory = true;
#else
            if (element.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_Floors) isFloorCategory = true;
#endif
        }

        if (!isFloorCategory)
        {
            return FoundationValidation.Failure("The selected element must belong to the Floors category.");
        }

        Solid solid;
        try
        {
            solid = FoundationSolidFaceReader.GetSolid(element);
        }
        catch (Exception ex)
        {
            return FoundationValidation.Failure($"Geometry error: {ex.Message}");
        }

        if (solid.Volume <= Tolerance)
        {
            return FoundationValidation.Failure("Selected floor has zero or negative volume.");
        }

        try
        {
            var (topFace, bottomFace) = FoundationSolidFaceReader.GetTopAndBottomFaces(solid);

            // Validate that faces are horizontal (normal collinear with Z)
            if (Math.Abs(Math.Abs(topFace.FaceNormal.Z) - 1.0) > 0.01)
            {
                return FoundationValidation.Failure("Foundation floor top face is sloped; only horizontal foundations are supported.");
            }

            if (Math.Abs(Math.Abs(bottomFace.FaceNormal.Z) - 1.0) > 0.01)
            {
                return FoundationValidation.Failure("Foundation floor bottom face is sloped; only horizontal foundations are supported.");
            }

            double thickness = topFace.Origin.Z - bottomFace.Origin.Z;
            if (thickness <= Tolerance)
            {
                return FoundationValidation.Failure($"Foundation slab thickness ({RevitUnits.FtToMm(thickness):F1} mm) must be positive.");
            }
        }
        catch (Exception ex)
        {
            return FoundationValidation.Failure($"Face validation error: {ex.Message}");
        }

        return FoundationValidation.Success();
    }
}
