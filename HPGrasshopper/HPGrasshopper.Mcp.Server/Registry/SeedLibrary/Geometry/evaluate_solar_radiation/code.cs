var sunObject = args.Obj("sunVector");
var surfaceList = args.List("targetSurfaceNormals");
double radiationIntensity = args.Double("directRadiationWatts", 1000.0);

double sunX = sunObject.Double("x");
double sunY = sunObject.Double("y");
double sunZ = sunObject.Double("z");

double sunLength = Math.Sqrt((sunX * sunX) + (sunY * sunY) + (sunZ * sunZ));
if (sunLength > 0.0001)
{
    sunX /= sunLength;
    sunY /= sunLength;
    sunZ /= sunLength;
}

var analysisResults = new List<object>();
double cumulativeSolarPowerWatts = 0.0;
double totalSurfaceArea = 0.0;

for (int surfaceIndex = 0; surfaceIndex < surfaceList.Count; surfaceIndex++)
{
    var item = surfaceList[surfaceIndex];
    int panelId = item.Int("id");
    double normalX = item.Double("normalX");
    double normalY = item.Double("normalY");
    double normalZ = item.Double("normalZ");
    double surfaceArea = item.Double("area", 1.0);

    double normalLength = Math.Sqrt((normalX * normalX) + (normalY * normalY) + (normalZ * normalZ));
    if (normalLength > 0.0001)
    {
        normalX /= normalLength;
        normalY /= normalLength;
        normalZ /= normalLength;
    }

    double dotProduct = (sunX * normalX) + (sunY * normalY) + (sunZ * normalZ);
    double incidentCosine = Math.Max(0.0, dotProduct);
    double incidentIrradiance = incidentCosine * radiationIntensity;
    double incidentPowerWatts = incidentIrradiance * surfaceArea;

    cumulativeSolarPowerWatts += incidentPowerWatts;
    totalSurfaceArea += surfaceArea;

    analysisResults.Add(new
    {
        id = panelId,
        areaSquareMeters = Math.Round(surfaceArea, 2),
        exposureCosine = Math.Round(incidentCosine, 4),
        irradianceWattsPerSqm = Math.Round(incidentIrradiance, 2),
        powerWatts = Math.Round(incidentPowerWatts, 2)
    });
}

return new
{
    success = true,
    panelCount = analysisResults.Count,
    totalAreaSquareMeters = Math.Round(totalSurfaceArea, 2),
    totalIncidentPowerWatts = Math.Round(cumulativeSolarPowerWatts, 2),
    averageIrradianceWattsPerSqm = totalSurfaceArea > 0 ? Math.Round(cumulativeSolarPowerWatts / totalSurfaceArea, 2) : 0.0,
    panels = analysisResults
};
