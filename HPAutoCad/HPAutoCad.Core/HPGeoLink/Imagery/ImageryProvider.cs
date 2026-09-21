namespace HPAutoCad.Core.HPGeoLink.Imagery;

/// <summary>
/// Where tiles come from. The engine is written against this interface so the source is a setting, not a
/// design decision: the terms of every public tile service are the user's call.
/// </summary>
public interface IImageryProvider
{
    /// <summary>Short id stored in settings and used as the cache folder name ("esri").</summary>
    string Id { get; }

    string DisplayName { get; }

    /// <summary>Text burnt into the corner of every exported image and written to the log.</summary>
    string Attribution { get; }

    int MaxZoom { get; }

    Uri TileUrl(TileAddress tile);
}

/// <summary>
/// Esri World Imagery, the same tiles the dialog's map panel shows. Terms of use for saving tiles into a
/// drawing: [chưa xác minh] — the user decides; attribution is always written.
/// </summary>
public sealed class EsriWorldImageryProvider : IImageryProvider
{
    public const string ProviderId = "esri";

    public string Id => ProviderId;
    public string DisplayName => "Esri World Imagery";
    public string Attribution => "Tiles © Esri — Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community";
    public int MaxZoom => 19;

    public Uri TileUrl(TileAddress tile) =>
        new($"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{tile.Zoom}/{tile.Y}/{tile.X}");
}

public static class ImageryProviders
{
    public static IImageryProvider Default { get; } = new EsriWorldImageryProvider();

    /// <summary>Resolves a provider id from settings or a script argument; unknown ids are refused, never silently defaulted.</summary>
    public static IImageryProvider Resolve(string? id)
    {
        var key = (id ?? "").Trim().ToLowerInvariant();
        if (key.Length == 0 || key == EsriWorldImageryProvider.ProviderId) return Default;
        throw new ArgumentException($"Nguồn ảnh '{id}' không được hỗ trợ (chỉ: {EsriWorldImageryProvider.ProviderId}).");
    }
}
