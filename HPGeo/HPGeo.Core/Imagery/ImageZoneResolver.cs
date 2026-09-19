using HPGeo.Core.Projection;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;

namespace HPGeo.Core.Imagery;

/// <summary>The zone and unit one imagery run will use, and where each value came from (for the console).</summary>
public sealed record ResolvedImageZone(TmParameters Tm, DrawingUnit Unit, string ZoneSource, string UnitSource);

/// <summary>
/// Precedence for everything the projection needs, value by value: the argument line, then the drawing's
/// stored record, then the user's last settings, then the TM-3 defaults — the same chain for cm, k0, fe and
/// fn, so a drawing stored with k0 0.9996 is not silently projected with 0.9999 (that alone moves a raster
/// by hundreds of metres). The unit: the argument, then the unit the drawing's record remembers (chosen when
/// INSUNITS was 0), then INSUNITS itself.
/// </summary>
public static class ImageZoneResolver
{
    public const string NoCentralMeridianCode = "NO_CENTRAL_MERIDIAN";
    public const string UnknownUnitCode = "UNKNOWN_UNIT";

    public static ResolvedImageZone Resolve(ImageArguments args, GeoSettings? stored, GeoSettings? user, DrawingUnit drawingUnit)
    {
        var (cm, source) = First(args.CentralMeridianDeg, stored?.HasCentralMeridian == true ? stored.CentralMeridianDeg : null, user?.HasCentralMeridian == true ? user.CentralMeridianDeg : null);
        if (cm is null)
            throw new ImageZoneException(NoCentralMeridianCode, "Chưa xác định KINH TUYẾN TRỤC — thêm cm=<KTT> (bản vẽ này chưa lưu KTT nào).");

        // k0/fe/fn follow the zone they were stored with: a record's k0 is only honoured together with its cm.
        var storedZone = stored?.HasCentralMeridian == true ? stored : null;
        var userZone = user?.HasCentralMeridian == true ? user : null;
        var k0 = First(args.ScaleFactor, storedZone?.ScaleFactor, userZone?.ScaleFactor).Value ?? TmParameters.Tm3ScaleFactor;
        var fe = First(args.FalseEasting, storedZone?.FalseEasting, userZone?.FalseEasting).Value ?? TmParameters.DefaultFalseEasting;
        var fn = First(args.FalseNorthing, storedZone?.FalseNorthing, userZone?.FalseNorthing).Value ?? TmParameters.DefaultFalseNorthing;
        var tm = new TmParameters(cm.Value, k0, fe, fn);
        if (!tm.IsValid)
            throw new ImageZoneException(NoCentralMeridianCode, "Tham số múi chiếu không hợp lệ (k0 phải > 0, cm hữu hạn).");

        DrawingUnit unit;
        string unitSource;
        if (args.UnitOverride is { } fromArgs) { unit = fromArgs; unitSource = "unit="; }
        else if (stored?.Unit is { } fromRecord) { unit = fromRecord; unitSource = "bản vẽ (đã lưu)"; }
        else { unit = drawingUnit; unitSource = "INSUNITS"; }
        if (DrawingUnitFactor.MetersPerUnit(unit) is not { } factor || !(factor > 0))
            throw new ImageZoneException(UnknownUnitCode, $"Đơn vị {DrawingUnitFactor.Label(unit)} không quy được ra mét — thêm unit=m hoặc unit=mm.");

        return new ResolvedImageZone(tm, unit, source, unitSource);
    }

    private static (double? Value, string Source) First(double? fromArgs, double? fromRecord, double? fromUser)
    {
        if (fromArgs is { } a && double.IsFinite(a)) return (a, "tham số");
        if (fromRecord is { } r && double.IsFinite(r)) return (r, "bản vẽ (đã lưu)");
        if (fromUser is { } u && double.IsFinite(u)) return (u, "settings.json");
        return (null, "mặc định");
    }
}

/// <summary>A refusal with the code the console and the log print; never an unexpected error.</summary>
public sealed class ImageZoneException : ArgumentException
{
    public ImageZoneException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
