using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPKata.Core.Configuration;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Strongly-typed DTOs and loaders for the KataPro Beam (katapro_b01_blueprint.json, 1,068 entities),
    /// Column (katapro_c1_blueprint.json, 469 entities), and Shear Wall (katapro_v1_blueprint.json, 627 entities) blueprints.
    /// </summary>
    public sealed class KataBlueprintRoot
    {
        public const string BeamFileName = "katapro_b01_blueprint.json";
        public const string ColumnFileName = "katapro_c1_blueprint.json";
        public const string WallFileName = "katapro_v1_blueprint.json";

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("items")]
        public List<KataBlueprintEntity> Items { get; set; } = new();

        public static KataBlueprintRoot? LoadDefault() => LoadByFileName(BeamFileName);

        public static KataBlueprintRoot? LoadColumnDefault() => LoadByFileName(ColumnFileName);

        public static KataBlueprintRoot? LoadWallDefault() => LoadByFileName(WallFileName);

        public static KataBlueprintRoot? LoadByFileName(string fileName)
        {
            string? path = ResolveBlueprintPath(fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<KataBlueprintRoot>(json);
            }
            catch
            {
                return null;
            }
        }

        public static string? ResolveBlueprintPath() => ResolveBlueprintPath(BeamFileName);

        public static string? ResolveBlueprintPath(string fileName)
        {
            string cHpKata = Path.Combine(@"C:\HPKata", fileName);
            if (File.Exists(cHpKata)) return cHpKata;

            string envPath = Path.Combine(KataInstallEnvironment.Current.RootDirectory, fileName);
            if (File.Exists(envPath)) return envPath;

            string? asmDir = Path.GetDirectoryName(typeof(KataBlueprintRoot).Assembly.Location);
            if (!string.IsNullOrEmpty(asmDir))
            {
                string local = Path.Combine(asmDir, fileName);
                if (File.Exists(local)) return local;
            }

            return null;
        }
    }

    public sealed class KataBlueprintEntity
    {
        [JsonPropertyName("h")]
        public string Handle { get; set; } = string.Empty;

        [JsonPropertyName("t")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("layer")]
        public string Layer { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public int ColorIndex { get; set; } = 256;

        [JsonPropertyName("ltype")]
        public string Linetype { get; set; } = "ByLayer";

        [JsonPropertyName("ltscale")]
        public double LinetypeScale { get; set; } = 1.0;

        [JsonPropertyName("lweight")]
        public int LineWeight { get; set; } = -1;

        [JsonPropertyName("closed")]
        public bool Closed { get; set; }

        [JsonPropertyName("verts")]
        public List<double[]>? Vertices { get; set; }

        [JsonPropertyName("p")]
        public double[]? LinePoints { get; set; }

        [JsonPropertyName("c")]
        public double[]? CircleData { get; set; }

        [JsonPropertyName("bname")]
        public string? BlockName { get; set; }

        [JsonPropertyName("pos")]
        public double[]? BlockPlacement { get; set; }

        [JsonPropertyName("dyn")]
        public Dictionary<string, string>? DynamicProps { get; set; }

        [JsonPropertyName("attrs")]
        public List<KataBlueprintAttribute>? Attributes { get; set; }

        [JsonPropertyName("style")]
        public string? StyleName { get; set; }

        [JsonPropertyName("rot")]
        public double Rotation { get; set; }

        [JsonPropertyName("txt")]
        public string? Text { get; set; }

        [JsonPropertyName("pts")]
        public double[]? DimPoints { get; set; }

        [JsonPropertyName("defTxt")]
        public bool UsingDefaultTextPosition { get; set; } = true;

        [JsonPropertyName("arrow")]
        public bool HasArrowHead { get; set; } = true;

        [JsonPropertyName("asz")]
        public double ArrowSize { get; set; } = 1.5;

        [JsonPropertyName("ldrBlk")]
        public string? LeaderBlockName { get; set; }

        [JsonPropertyName("xd")]
        public List<KataBlueprintXData>? XData { get; set; }
    }

    public sealed class KataBlueprintAttribute
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; } = string.Empty;

        [JsonPropertyName("txt")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("pos")]
        public double[] Position { get; set; } = Array.Empty<double>();

        [JsonPropertyName("aln")]
        public double[] AlignmentPoint { get; set; } = Array.Empty<double>();

        [JsonPropertyName("ht")]
        public double Height { get; set; } = 62.5;

        [JsonPropertyName("rot")]
        public double Rotation { get; set; }

        [JsonPropertyName("vis")]
        public bool Visible { get; set; } = true;

        [JsonPropertyName("wf")]
        public double WidthFactor { get; set; } = 0.8;

        [JsonPropertyName("just")]
        public int Justify { get; set; }
    }

    public sealed class KataBlueprintXData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("val")]
        public string Value { get; set; } = string.Empty;
    }
}
