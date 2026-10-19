double envelopeWidth = args.Double("surfaceWidthMeters", 30.0);
double envelopeHeight = args.Double("surfaceHeightMeters", 15.0);
int horizontalDivisions = args.Int("uDivisions");
int verticalDivisions = args.Int("vDivisions");
string familyName = args.Str("panelFamilyName", "Adaptive-Quad-Panel");

if (horizontalDivisions <= 0 || verticalDivisions <= 0)
{
    return new { success = false, message = "uDivisions and vDivisions must be greater than zero." };
}
if (envelopeWidth <= 0.0 || envelopeHeight <= 0.0)
{
    return new { success = false, message = "surfaceWidthMeters and surfaceHeightMeters must be positive numbers." };
}

const double feetPerMeter = 3.280839895013123;
double cellWidthMeters = envelopeWidth / Math.Max(1, horizontalDivisions);
double cellHeightMeters = envelopeHeight / Math.Max(1, verticalDivisions);

var generatedPanels = new List<object>();

for (int verticalIndex = 0; verticalIndex < verticalDivisions; verticalIndex++)
{
    for (int horizontalIndex = 0; horizontalIndex < horizontalDivisions; horizontalIndex++)
    {
        double xMinimum = horizontalIndex * cellWidthMeters;
        double xMaximum = (horizontalIndex + 1) * cellWidthMeters;
        double zMinimum = verticalIndex * cellHeightMeters;
        double zMaximum = (verticalIndex + 1) * cellHeightMeters;

        var cornerOne = new { x = Math.Round(xMinimum, 3), y = 0.0, z = Math.Round(zMinimum, 3) };
        var cornerTwo = new { x = Math.Round(xMaximum, 3), y = 0.0, z = Math.Round(zMinimum, 3) };
        var cornerThree = new { x = Math.Round(xMaximum, 3), y = 0.0, z = Math.Round(zMaximum, 3) };
        var cornerFour = new { x = Math.Round(xMinimum, 3), y = 0.0, z = Math.Round(zMaximum, 3) };

        generatedPanels.Add(new
        {
            panelIndex = (verticalIndex * horizontalDivisions) + horizontalIndex + 1,
            uIndex = horizontalIndex,
            vIndex = verticalIndex,
            corners = new[] { cornerOne, cornerTwo, cornerThree, cornerFour }
        });
    }
}

return new
{
    success = true,
    totalPanels = generatedPanels.Count,
    uDivisions = horizontalDivisions,
    vDivisions = verticalDivisions,
    cellDimensions = new { widthMeters = Math.Round(cellWidthMeters, 3), heightMeters = Math.Round(cellHeightMeters, 3) },
    targetFamily = familyName,
    panels = generatedPanels
};
