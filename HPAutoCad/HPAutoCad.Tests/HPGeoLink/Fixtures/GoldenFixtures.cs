using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPAutoCad.Tests.HPGeoLink.Fixtures;

/// <summary>Loads the JSON fixtures tools/gen-golden-*.js wrote (copied beside the test binary).</summary>
internal static class GoldenFixtures
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ForwardGolden Forward { get; } = Load<ForwardGolden>("golden-vn2000-to-wgs84.json");
    public static ReverseGolden Reverse { get; } = Load<ReverseGolden>("golden-wgs84-to-vn2000.json");

    private static T Load<T>(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, Options) ?? throw new InvalidOperationException($"{name} is empty");
    }

    public sealed class ForwardGolden
    {
        public string Source { get; set; } = "";
        public HelmertJson HelmertParameters { get; set; } = new();
        public TmJson Tm { get; set; } = new();
        public int SampleCount { get; set; }
        public List<ForwardCase> Cases { get; set; } = new();
        public List<HelmertCase> HelmertOnly { get; set; } = new();
    }

    public sealed class ReverseGolden
    {
        public double Proj4ForwardMaxDeviationFromOracleM { get; set; }
        public TmJson Tm { get; set; } = new();
        public List<ReverseCase> Cases { get; set; } = new();
    }

    public sealed class HelmertJson
    {
        public double Dx { get; set; }
        public double Dy { get; set; }
        public double Dz { get; set; }
        public double Rx { get; set; }
        public double Ry { get; set; }
        public double Rz { get; set; }
        public double S { get; set; }
    }

    public sealed class TmJson
    {
        public double K0 { get; set; }
        public double FalseEasting { get; set; }
        public double FalseNorthing { get; set; }
    }

    public sealed class ForwardCase
    {
        public string Id { get; set; } = "";
        public double Cm { get; set; }
        public double E { get; set; }
        public double N { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public bool InsideVietnam { get; set; }
    }

    public sealed class ReverseCase
    {
        public string Id { get; set; } = "";
        public double Cm { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double E { get; set; }
        public double N { get; set; }
        [JsonPropertyName("roundTripErrorM")] public double RoundTripErrorM { get; set; }
    }

    public sealed class HelmertCase
    {
        public double Vn2000Lat { get; set; }
        public double Vn2000Lon { get; set; }
        public double Wgs84Lat { get; set; }
        public double Wgs84Lon { get; set; }
        public double Wgs84Height { get; set; }
    }
}
