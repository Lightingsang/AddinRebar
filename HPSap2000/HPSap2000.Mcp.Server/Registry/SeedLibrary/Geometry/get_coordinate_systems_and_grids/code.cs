bool includeGridLines = args.Bool("includeGridLines", true);

int nc = 0; string[] csysNames = null;
int rc = sapModel.CoordSys.GetNameList(ref nc, ref csysNames);
if (rc != 0) throw new InvalidOperationException($"SAP2000 returned {rc} from CoordSys.GetNameList");

var systems = new List<object>();
foreach (var name in csysNames ?? new string[0])
{
    ct.ThrowIfCancellationRequested();
    double x = 0, y = 0, z = 0, rz = 0, ry = 0, rx = 0;
    int nx = 0, ny = 0, nz = 0;
    string[] idX = null, idY = null, idZ = null;
    double[] cX = null, cY = null, cZ = null;
    string[] ltX = null, ltY = null, ltZ = null;
    bool[] vX = null, vY = null, vZ = null;
    string[] blX = null, blY = null, blZ = null;
    int[] colX = null, colY = null, colZ = null;

    int ret = sapModel.CoordSys.GetCoordSys_2(name, ref x, ref y, ref z, ref rz, ref ry, ref rx,
        ref nx, ref ny, ref nz, ref idX, ref idY, ref idZ, ref cX, ref cY, ref cZ,
        ref ltX, ref ltY, ref ltZ, ref vX, ref vY, ref vZ, ref blX, ref blY, ref blZ, ref colX, ref colY, ref colZ);
    if (ret != 0) continue;

    object grids = null;
    if (includeGridLines)
    {
        grids = new
        {
            xLines = Enumerable.Range(0, nx).Select(i => new { id = idX[i], coordinateM = cX[i], visible = vX[i] }).ToList(),
            yLines = Enumerable.Range(0, ny).Select(i => new { id = idY[i], coordinateM = cY[i], visible = vY[i] }).ToList(),
            zLines = Enumerable.Range(0, nz).Select(i => new { id = idZ[i], coordinateM = cZ[i], visible = vZ[i] }).ToList(),
        };
    }

    systems.Add(new
    {
        name,
        originM = new { x, y, z },
        rotationDeg = new { rx, ry, rz },
        gridLineCounts = new { x = nx, y = ny, z = nz },
        grids
    });
}

log($"{nc} coordinate systems found");
return new
{
    success = true,
    systems,
    count = systems.Count,
    summary = $"{systems.Count} coordinate system(s) in the model"
};
