using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;

namespace HPKata.Installer.Services
{
    /// <summary>
    /// Builds MCP execution parameters and the AutoCAD .NET C# script for drawing a continuous beam from Kata.xlsm.
    /// </summary>
    public static class AutocadBeamScriptBuilder
    {
        public static Dictionary<string, object> BuildParameters(KataBeamSheetData data, string outDwgPath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            KataBeamDataMapper.ParseBarSpec(data.TopContinuousRebar, out int topCnt, out int topDia);
            KataBeamDataMapper.ParseBarSpec(data.BotContinuousRebar, out int botCnt, out int botDia);
            double denseMm = ParseSpacingMm(data.StirrupNearSupport, 150.0);
            double sparseMm = ParseSpacingMm(data.StirrupMidSpan, 200.0);

            var spanLengths = data.Spans
                .Select(s => s.ClearLengthMm.ToString(CultureInfo.InvariantCulture))
                .ToArray();
            var supportWidths = data.Supports
                .Select(s => s.WidthMm.ToString(CultureInfo.InvariantCulture))
                .ToArray();
            var topReinf = data.Supports
                .Select(s => string.IsNullOrWhiteSpace(s.TopCurtailmentLayer2) ? s.TopCurtailmentLayer1 : s.TopCurtailmentLayer2)
                .ToArray();
            var botReinf = data.Spans
                .Select(s => string.IsNullOrWhiteSpace(s.BotCurtailmentLayer2) ? s.BotCurtailmentLayer1 : s.BotCurtailmentLayer2)
                .ToArray();

            return new Dictionary<string, object>
            {
                ["beamMark"] = data.BeamMark,
                ["widthMm"] = data.DefaultWidthMm,
                ["heightMm"] = data.DefaultHeightMm,
                ["slabThicknessMm"] = data.SlabThicknessMm,
                ["elevation"] = data.ElevationString,
                ["topCount"] = topCnt,
                ["topDia"] = topDia,
                ["botCount"] = botCnt,
                ["botDia"] = botDia,
                ["skinDiaMm"] = data.SkinBarDiameterMm,
                ["skinLayers"] = data.SkinBarLayers,
                ["stirrupDiaMm"] = data.StirrupDiameterMm,
                ["stirrupDenseMm"] = denseMm,
                ["stirrupSparseMm"] = sparseMm,
                ["stirrupLegs"] = data.LegCount,
                ["outDwgPath"] = outDwgPath ?? string.Empty,
                ["spanLengths"] = spanLengths,
                ["supportWidths"] = supportWidths,
                ["topReinf"] = topReinf,
                ["botReinf"] = botReinf
            };
        }

        public static double ParseSpacingMm(string spec, double fallback)
        {
            if (string.IsNullOrWhiteSpace(spec)) return fallback;
            string digits = new string(spec.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out double v) && v > 0
                ? v
                : fallback;
        }

        public static string BuildAutoCadCSharpScript() => string.Join("\n",
            GetHeaderAndHelpersSection(),
            GetElevationAndSpansSection(),
            GetContinuousBarsAndCrossSection(),
            GetScheduleTableAndReturnSection());

        private static string GetHeaderAndHelpersSection() => @"
string beamMark = args.Str(""beamMark"", ""B01"");
double b = args.Double(""widthMm"", 500.0);
double h = args.Double(""heightMm"", 1100.0);
double hSlab = args.Double(""slabThicknessMm"", 150.0);
string elev = args.Str(""elevation"", ""+3.300"");
int topCnt = args.Int(""topCount"", 6);
int topDia = args.Int(""topDia"", 25);
int botCnt = args.Int(""botCount"", 6);
int botDia = args.Int(""botDia"", 25);
int skinDia = args.Int(""skinDiaMm"", 12);
int skinLayers = args.Int(""skinLayers"", 2);
int stirDia = args.Int(""stirrupDiaMm"", 10);
double stirDense = args.Double(""stirrupDenseMm"", 150.0);
double stirSparse = args.Double(""stirrupSparseMm"", 200.0);
int stirLegs = args.Int(""stirrupLegs"", 2);
string outDwgPath = args.Str(""outDwgPath"", """");
var spanLengths = args.Strings(""spanLengths"").Select(s => double.Parse(s)).ToList();
var supportWidths = args.Strings(""supportWidths"").Select(s => double.Parse(s)).ToList();
var topReinf = args.Strings(""topReinf"").ToList();
var botReinf = args.Strings(""botReinf"").ToList();

var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
string[] layerNames = { ""KATA_NET_THAY"", ""KATA_NET_MANH"", ""KATA_THEP_CHU"", ""KATA_THEP_DAI"", ""KATA_DIM"", ""KATA_TEXT"" };
short[] layerColors = { 7, 8, 1, 3, 2, 4 };
for (int li = 0; li < layerNames.Length; li++)
{
    if (!lt.Has(layerNames[li]))
    {
        var ltr = new LayerTableRecord { Name = layerNames[li], Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, layerColors[li]) };
        lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
    }
}
var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForWrite);
if (!rat.Has(""KATA""))
{
    var rar = new RegAppTableRecord { Name = ""KATA"" };
    rat.Add(rar);
    tr.AddNewlyCreatedDBObject(rar, true);
}
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
var createdHandles = new List<string>();
void AddEnt(Entity ent, string layer)
{
    ent.Layer = layer;
    ObjectId id = ms.AppendEntity(ent);
    tr.AddNewlyCreatedDBObject(ent, true);
    createdHandles.Add(id.Handle.ToString());
}
void AttachKataXData(Entity ent, string dang, short stt, short phi, int count, string lengths, string spacing)
{
    ent.XData = new ResultBuffer(
        new TypedValue(1001, ""KATA""), new TypedValue(1000, dang), new TypedValue(1070, stt),
        new TypedValue(1070, phi), new TypedValue(1000, count.ToString()), new TypedValue(1070, (short)1),
        new TypedValue(1000, beamMark), new TypedValue(1000, lengths), new TypedValue(1000, spacing));
}
void AddRect(double rx, double ry, double rw, double rh, string layer)
{
    var pl = new Polyline();
    pl.AddVertexAt(0, new Point2d(rx, ry), 0, 0, 0);
    pl.AddVertexAt(1, new Point2d(rx + rw, ry), 0, 0, 0);
    pl.AddVertexAt(2, new Point2d(rx + rw, ry + rh), 0, 0, 0);
    pl.AddVertexAt(3, new Point2d(rx, ry + rh), 0, 0, 0);
    pl.Closed = true;
    AddEnt(pl, layer);
}
void AddText(double tx, double ty, string txt, double th, string layer)
{
    AddEnt(new DBText { Position = new Point3d(tx, ty, 0), TextString = txt, Height = th }, layer);
}";

        private static string GetElevationAndSpansSection() => @"
double totalSpanLen = spanLengths.Sum();
AddText(0, 450, $""MẶT CẮT DỌC DẦM {beamMark} ({b:F0}x{h:F0}) - CAO TRÌNH {elev} - ĐỌC TỪ C:\\HPKata\\Kata.xlsm"", 180, ""KATA_TEXT"");
double curX = 0.0;
int totalStirrupCount = 0;
for (int i = 0; i < spanLengths.Count; i++)
{
    double supW = i < supportWidths.Count ? supportWidths[i] : 400.0;
    if (supW > 0)
    {
        AddRect(curX, -h - 600, supW, h + 600, ""KATA_NET_MANH"");
        AddText(curX + 40, -h - 820, $""TRỤC {i + 1} ({supW:F0})"", 110, ""KATA_DIM"");
        curX += supW;
    }
    double spanL = spanLengths[i];
    AddRect(curX, -h, spanL, h, ""KATA_NET_THAY"");
    AddRect(curX, -hSlab, spanL, hSlab, ""KATA_NET_MANH"");
    AddEnt(new Line(new Point3d(curX, -h - 350, 0), new Point3d(curX + spanL, -h - 350, 0)), ""KATA_DIM"");
    AddText(curX + spanL * 0.35, -h - 310, $""Nhịp {i + 1}: Ln = {spanL:F0} mm"", 110, ""KATA_DIM"");
    double zDense = spanL * 0.25;
    for (double sx = 50.0; sx < spanL - 50.0; sx += (sx < zDense || sx > spanL - zDense) ? stirDense : stirSparse)
    {
        AddEnt(new Line(new Point3d(curX + sx, -40, 0), new Point3d(curX + sx, -h + 40, 0)), ""KATA_THEP_DAI"");
        totalStirrupCount++;
    }
    string bSpec = i < botReinf.Count ? botReinf[i] : """";
    if (!string.IsNullOrWhiteSpace(bSpec) && bSpec != ""-"")
    {
        var botCurt = new Polyline();
        botCurt.AddVertexAt(0, new Point2d(curX + spanL * 0.15, -h + 95), 0, 0, 0);
        botCurt.AddVertexAt(1, new Point2d(curX + spanL * 0.85, -h + 95), 0, 0, 0);
        AttachKataXData(botCurt, ""00"", (short)(10 + i), 20, 2, $""{spanL * 0.70:F0}"", """");
        AddEnt(botCurt, ""KATA_THEP_CHU"");
        AddText(curX + spanL * 0.38, -h + 120, $""Gia cường dưới: {bSpec}"", 85, ""KATA_TEXT"");
    }
    string tSpec = i < topReinf.Count ? topReinf[i] : """";
    if (!string.IsNullOrWhiteSpace(tSpec))
    {
        double ext = Math.Max(1200.0, spanL * 0.25);
        double tStart = Math.Max(0.0, curX - supW - ext * 0.5);
        var topCurt = new Polyline();
        topCurt.AddVertexAt(0, new Point2d(tStart, -95), 0, 0, 0);
        topCurt.AddVertexAt(1, new Point2d(curX + ext, -95), 0, 0, 0);
        AttachKataXData(topCurt, ""00"", (short)(20 + i), 20, 4, $""{curX + ext - tStart:F0}"", """");
        AddEnt(topCurt, ""KATA_THEP_CHU"");
        AddText(curX + 60, -195, $""Gối {i + 1}: {tSpec}"", 85, ""KATA_TEXT"");
    }
    curX += spanL;
}";

        private static string GetContinuousBarsAndCrossSection() => @"
double hookLen = 400.0;
var topBar = new Polyline();
topBar.AddVertexAt(0, new Point2d(40, -45 - hookLen), 0, 0, 0);
topBar.AddVertexAt(1, new Point2d(40, -45), 0, 0, 0);
topBar.AddVertexAt(2, new Point2d(curX - 40, -45), 0, 0, 0);
topBar.AddVertexAt(3, new Point2d(curX - 40, -45 - hookLen), 0, 0, 0);
AttachKataXData(topBar, ""01"", 1, (short)topDia, topCnt, $""{hookLen:F0};{curX - 80:F0};{hookLen:F0}"", """");
AddEnt(topBar, ""KATA_THEP_CHU"");
AddText(350, -30, $""#1 Thép chủ trên: {topCnt}Ø{topDia} (L={(curX - 80 + 2 * hookLen):F0}mm)"", 105, ""KATA_TEXT"");

var botBar = new Polyline();
botBar.AddVertexAt(0, new Point2d(40, -h + 45 + hookLen), 0, 0, 0);
botBar.AddVertexAt(1, new Point2d(40, -h + 45), 0, 0, 0);
botBar.AddVertexAt(2, new Point2d(curX - 40, -h + 45), 0, 0, 0);
botBar.AddVertexAt(3, new Point2d(curX - 40, -h + 45 + hookLen), 0, 0, 0);
AttachKataXData(botBar, ""01"", 2, (short)botDia, botCnt, $""{hookLen:F0};{curX - 80:F0};{hookLen:F0}"", """");
AddEnt(botBar, ""KATA_THEP_CHU"");
AddText(350, -h - 150, $""#2 Thép chủ dưới: {botCnt}Ø{botDia} (L={(curX - 80 + 2 * hookLen):F0}mm)"", 105, ""KATA_TEXT"");

for (int sIdx = 1; sIdx <= skinLayers; sIdx++)
{
    double ySkin = -h * sIdx / (skinLayers + 1.0);
    var skinBar = new Polyline();
    skinBar.AddVertexAt(0, new Point2d(40, ySkin), 0, 0, 0);
    skinBar.AddVertexAt(1, new Point2d(curX - 40, ySkin), 0, 0, 0);
    AttachKataXData(skinBar, ""00"", 3, (short)skinDia, skinLayers * 2, $""{curX - 80:F0}"", """");
    AddEnt(skinBar, ""KATA_THEP_CHU"");
}
double bSt = b - 80.0;
double hSt = h - 80.0;
var stirRep = new Polyline();
stirRep.AddVertexAt(0, new Point2d(60, -40), 0, 0, 0);
stirRep.AddVertexAt(1, new Point2d(60, -h + 40), 0, 0, 0);
AttachKataXData(stirRep, ""22"", 4, (short)stirDia, totalStirrupCount, $""{bSt:F0};{hSt:F0};{bSt:F0};{hSt:F0};120"", $""a{stirDense:F0}/a{stirSparse:F0}"");
AddEnt(stirRep, ""KATA_THEP_DAI"");

double[] secXs = { curX + 1200.0, curX + 2800.0 };
string[] secTitles = { ""MẶT CẮT 1-1 (GỐI)"", ""MẶT CẮT 2-2 (NHỊP)"" };
double[] secSp = { stirDense, stirSparse };
for (int si = 0; si < 2; si++)
{
    double sx = secXs[si];
    AddText(sx, 220, $""{secTitles[si]} ({b:F0}x{h:F0})"", 125, ""KATA_TEXT"");
    AddRect(sx, -h, b, h, ""KATA_NET_THAY"");
    AddRect(sx + 40, -h + 40, b - 80, h - 80, ""KATA_THEP_DAI"");
    for (int k = 0; k < topCnt; k++)
        AddEnt(new Circle(new Point3d(sx + 65 + (b - 130) * k / Math.Max(1, topCnt - 1), -65, 0), Vector3d.ZAxis, 16), ""KATA_THEP_CHU"");
    for (int k = 0; k < botCnt; k++)
        AddEnt(new Circle(new Point3d(sx + 65 + (b - 130) * k / Math.Max(1, botCnt - 1), -h + 65, 0), Vector3d.ZAxis, 16), ""KATA_THEP_CHU"");
    AddText(sx, -h - 180, $""Trên: {topCnt}Ø{topDia} | Dưới: {botCnt}Ø{botDia}"", 90, ""KATA_TEXT"");
    AddText(sx, -h - 310, $""Cốt giá: {skinLayers * 2}Ø{skinDia} | Đai: Ø{stirDia}a{secSp[si]:F0}"", 90, ""KATA_TEXT"");
}";

        private static string GetScheduleTableAndReturnSection() => @"
double tblY = -h - 1450.0;
AddText(0, tblY + 120, $""BẢNG THỐNG KÊ CỐT THÉP DẦM {beamMark} (TRÍCH XUẤT TỪ KATA.XLSM & KATA XDATA)"", 150, ""KATA_TEXT"");
double lenMain = curX - 80 + 2 * hookLen;
double lenSkin = curX - 80;
double lenStir = 2 * (bSt + hSt) + 200.0;
double wMainTop = topCnt * (lenMain / 1000.0) * (topDia * topDia * 0.006165);
double wMainBot = botCnt * (lenMain / 1000.0) * (botDia * botDia * 0.006165);
double wSkin = (skinLayers * 2) * (lenSkin / 1000.0) * (skinDia * skinDia * 0.006165);
double wStir = totalStirrupCount * (lenStir / 1000.0) * (stirDia * stirDia * 0.006165);
double wTotal = wMainTop + wMainBot + wSkin + wStir;
var rows = new[]
{
    new[] { ""STT"", ""TÊN CK"", ""HÌNH DẠNG"", ""ĐK"", ""DÀI (mm)"", ""SL"", ""KHỐI LƯỢNG (kg)"" },
    new[] { ""1"", beamMark, ""Dạng 01 (Chủ trên)"", $""Ø{topDia}"", $""{lenMain:F0}"", $""{topCnt}"", $""{wMainTop:F1}"" },
    new[] { ""2"", beamMark, ""Dạng 01 (Chủ dưới)"", $""Ø{botDia}"", $""{lenMain:F0}"", $""{botCnt}"", $""{wMainBot:F1}"" },
    new[] { ""3"", beamMark, ""Dạng 00 (Cốt giá)"", $""Ø{skinDia}"", $""{lenSkin:F0}"", $""{skinLayers * 2}"", $""{wSkin:F1}"" },
    new[] { ""4"", beamMark, ""Dạng 22 (Thép đai)"", $""Ø{stirDia}"", $""{lenStir:F0}"", $""{totalStirrupCount}"", $""{wStir:F1}"" },
    new[] { ""TỔNG"", beamMark, ""TCVN 5574:2018"", ""-"", ""-"", ""-"", $""{wTotal:F1} kg"" }
};
double[] colX = { 80, 750, 1850, 4200, 5250, 6650, 7450 };
for (int r = 0; r < rows.Length; r++)
{
    double ry = tblY - (r + 1) * 260.0;
    AddRect(0, ry, 8600.0, 260.0, ""KATA_NET_MANH"");
    for (int c = 0; c < rows[r].Length; c++)
        AddText(colX[c], ry + 65, rows[r][c], 95, r == 0 || r == rows.Length - 1 ? ""KATA_DIM"" : ""KATA_TEXT"");
}
if (!string.IsNullOrEmpty(outDwgPath) && !outDwgPath.EndsWith(""DRYRUN""))
{
    using (var cloneDb = db.Wblock()) { cloneDb.SaveAs(outDwgPath, DwgVersion.Current); }
}
return new
{
    beamMark, section = $""{b:F0}x{h:F0}"", spanCount = spanLengths.Count,
    totalSpanClearLengthMm = totalSpanLen, totalBeamLengthMm = curX,
    topRebar = $""{topCnt}d{topDia}"", botRebar = $""{botCnt}d{botDia}"",
    skinRebar = $""{skinLayers * 2}d{skinDia}"", stirrupCount = totalStirrupCount,
    totalWeightKg = Math.Round(wTotal, 1), createdEntityCount = createdHandles.Count, savedDwg = outDwgPath
};";
    }
}
