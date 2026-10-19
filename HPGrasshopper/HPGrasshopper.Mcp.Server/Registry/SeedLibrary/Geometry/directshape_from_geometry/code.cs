string geometricType = args.Str("shapeType", "Solid");
string categoryChoice = args.Str("category", "GenericModel");
string directShapeName = args.Str("name", "Grasshopper_DirectShape");

if (string.IsNullOrWhiteSpace(directShapeName))
{
    return new { success = false, message = "DirectShape name cannot be empty." };
}

bool isMesh = string.Equals(geometricType, "Mesh", StringComparison.OrdinalIgnoreCase);
bool isSolid = string.Equals(geometricType, "Solid", StringComparison.OrdinalIgnoreCase);
if (!isMesh && !isSolid)
{
    return new { success = false, message = $"Unsupported shapeType '{geometricType}'. Must be Solid or Mesh." };
}

var dimensionObject = args.Obj("boundingDimensions");
double widthMeters = dimensionObject.Double("width", 5.0);
double lengthMeters = dimensionObject.Double("length", 5.0);
double heightMeters = dimensionObject.Double("height", 3.0);

if (widthMeters <= 0.0 || lengthMeters <= 0.0 || heightMeters <= 0.0)
{
    return new { success = false, message = "Bounding dimensions width, length, and height must be positive numbers." };
}

const double feetPerMeter = 3.280839895013123;
double widthFeet = widthMeters * feetPerMeter;
double lengthFeet = lengthMeters * feetPerMeter;
double heightFeet = heightMeters * feetPerMeter;

var builtInCategory = Autodesk.Revit.DB.BuiltInCategory.OST_GenericModel;
if (string.Equals(categoryChoice, "StructuralFraming", StringComparison.OrdinalIgnoreCase))
{
    builtInCategory = Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming;
}
else if (string.Equals(categoryChoice, "Mass", StringComparison.OrdinalIgnoreCase))
{
    builtInCategory = Autodesk.Revit.DB.BuiltInCategory.OST_Mass;
}
else if (!string.Equals(categoryChoice, "GenericModel", StringComparison.OrdinalIgnoreCase))
{
    return new { success = false, message = $"Unsupported category '{categoryChoice}'. Must be GenericModel, StructuralFraming, or Mass." };
}

var categoryId = new Autodesk.Revit.DB.ElementId(builtInCategory);
if (!Autodesk.Revit.DB.DirectShape.IsValidCategoryId(categoryId, revitDoc))
{
    return new { success = false, message = $"Category {categoryChoice} is not valid for DirectShape in this document." };
}

var directShapeInstance = Autodesk.Revit.DB.DirectShape.CreateElement(revitDoc, categoryId);
directShapeInstance.ApplicationId = "HPGrasshopper";
directShapeInstance.ApplicationDataId = Guid.NewGuid().ToString("D");
directShapeInstance.Name = directShapeName;

if (isMesh)
{
    var shapeBuilder = new Autodesk.Revit.DB.TessellatedShapeBuilder();
    shapeBuilder.OpenConnectedFaceSet(false);

    var cornerZero = new Autodesk.Revit.DB.XYZ(0.0, 0.0, 0.0);
    var cornerOne = new Autodesk.Revit.DB.XYZ(lengthFeet, 0.0, 0.0);
    var cornerTwo = new Autodesk.Revit.DB.XYZ(lengthFeet, widthFeet, 0.0);
    var cornerThree = new Autodesk.Revit.DB.XYZ(0.0, widthFeet, 0.0);
    var cornerFour = new Autodesk.Revit.DB.XYZ(0.0, 0.0, heightFeet);
    var cornerFive = new Autodesk.Revit.DB.XYZ(lengthFeet, 0.0, heightFeet);
    var cornerSix = new Autodesk.Revit.DB.XYZ(lengthFeet, widthFeet, heightFeet);
    var cornerSeven = new Autodesk.Revit.DB.XYZ(0.0, widthFeet, heightFeet);

    var invalidMaterialId = Autodesk.Revit.DB.ElementId.InvalidElementId;
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerZero, cornerThree, cornerTwo, cornerOne }, invalidMaterialId));
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerFour, cornerFive, cornerSix, cornerSeven }, invalidMaterialId));
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerZero, cornerOne, cornerFive, cornerFour }, invalidMaterialId));
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerTwo, cornerThree, cornerSeven, cornerSix }, invalidMaterialId));
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerThree, cornerZero, cornerFour, cornerSeven }, invalidMaterialId));
    shapeBuilder.AddFace(new Autodesk.Revit.DB.TessellatedFace(new[] { cornerOne, cornerTwo, cornerSix, cornerFive }, invalidMaterialId));

    shapeBuilder.CloseConnectedFaceSet();
    shapeBuilder.Target = Autodesk.Revit.DB.TessellatedShapeBuilderTarget.Mesh;
    shapeBuilder.Fallback = Autodesk.Revit.DB.TessellatedShapeBuilderFallback.Salvage;
    shapeBuilder.Build();

    var builderResult = shapeBuilder.GetBuildResult();
    directShapeInstance.SetShape(builderResult.GetGeometricalObjects());
}
else
{
    var profileLoop = new Autodesk.Revit.DB.CurveLoop();
    profileLoop.Append(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(0.0, 0.0, 0.0), new Autodesk.Revit.DB.XYZ(lengthFeet, 0.0, 0.0)));
    profileLoop.Append(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(lengthFeet, 0.0, 0.0), new Autodesk.Revit.DB.XYZ(lengthFeet, widthFeet, 0.0)));
    profileLoop.Append(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(lengthFeet, widthFeet, 0.0), new Autodesk.Revit.DB.XYZ(0.0, widthFeet, 0.0)));
    profileLoop.Append(Autodesk.Revit.DB.Line.CreateBound(new Autodesk.Revit.DB.XYZ(0.0, widthFeet, 0.0), new Autodesk.Revit.DB.XYZ(0.0, 0.0, 0.0)));

    var extrudedSolid = Autodesk.Revit.DB.GeometryCreationUtilities.CreateExtrusionGeometry(
        new List<Autodesk.Revit.DB.CurveLoop> { profileLoop },
        new Autodesk.Revit.DB.XYZ(0.0, 0.0, 1.0),
        heightFeet);

    directShapeInstance.SetShape(new List<Autodesk.Revit.DB.GeometryObject> { extrudedSolid });
}

return new
{
    success = true,
    directShapeId = directShapeInstance.Id.Value,
    category = builtInCategory.ToString(),
    shapeType = geometricType,
    name = directShapeName,
    volumeCubicMeters = Math.Round(widthMeters * lengthMeters * heightMeters, 3)
};
