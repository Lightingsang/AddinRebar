int barNum = args.Int("barNumber");
int caseNum = args.Int("caseNumber");
int ptsCount = Math.Clamp(args.Int("pointsCount", 5), 2, 21);

if (structure.Results.Available == 0)
    throw new InvalidOperationException("No calculation results are available in the model. Run calculations first.");

if (structure.Bars.Exist(barNum) == 0)
    throw new ArgumentException($"Bar {barNum} does not exist in the model.");

var bar = (IRobotBar)structure.Bars.Get(barNum);
double len = bar.Length;
var points = new List<object>();

for (int i = 0; i < ptsCount; i++)
{
    double relPt = (double)i / (ptsCount - 1);
    var fData = structure.Results.Bars.Forces.Value(barNum, caseNum, relPt);
    points.Add(new
    {
        relativePosition = relPt,
        distance = relPt * len,
        fx = fData.FX, fy = fData.FY, fz = fData.FZ,
        mx = fData.MX, my = fData.MY, mz = fData.MZ
    });
}

return new
{
    success = true,
    barNumber = barNum,
    caseNumber = caseNum,
    length = len,
    pointsCount = ptsCount,
    points
};
