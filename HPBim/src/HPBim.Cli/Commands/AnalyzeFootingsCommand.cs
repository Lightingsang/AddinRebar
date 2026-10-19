namespace HPBim.Cli.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Formatting;
using HPBim.Core.Coordination;

/// <summary>
///     Analyzes 196 piles and CAD geometry to reconstruct pile cap (đài móng) polygons.
///     Enforces FailFastGate rules against arbitrary free-radius pile clustering.
/// </summary>
public sealed class AnalyzeFootingsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ConsoleFormatter _formatter;
    private readonly IFailFastGate _failFastGate;

    public AnalyzeFootingsCommand(ConsoleFormatter? formatter = null, IFailFastGate? failFastGate = null)
    {
        _formatter = formatter ?? new ConsoleFormatter();
        _failFastGate = failFastGate ?? new FailFastGate();
    }

    public Task<int> ExecuteAsync(CliParseResult args, CancellationToken ct = default)
    {
        _formatter.WriteBanner("Analyze Footing Clusters", "Phân tích 196 tim cọc và ghép cụm đài móng");

        string pilesPath = Path.Combine("scratch", "piles_true_bim_model.json");
        if (!File.Exists(pilesPath))
        {
            _formatter.WriteError($"Không tìm thấy file: {pilesPath}");
            return Task.FromResult(1);
        }

        var pilesDoc = JsonDocument.Parse(File.ReadAllText(pilesPath));
        var pileList = pilesDoc.RootElement.GetProperty("piles").EnumerateArray()
            .Select(p => new PileInfo(
                p.GetProperty("stt").GetInt32(),
                p.GetProperty("name").GetString() ?? "",
                p.GetProperty("revit_x").GetDouble(),
                p.GetProperty("revit_y").GetDouble()
            )).ToList();

        _formatter.WriteSuccess($"Đã tải {pileList.Count} tim cọc từ mô hình.");

        var dupStts = pileList.GroupBy(p => p.Stt).Where(g => g.Count() > 1).ToList();
        if (dupStts.Count > 0)
        {
            _formatter.WriteWarning($"Có {dupStts.Count} STT trùng: {string.Join(", ", dupStts.Select(g => $"{g.Key}(x{g.Count()})"))}");
        }

        // Group piles into clusters based on distance <= 1800mm (Heuristic inspection)
        var clusters = ClusterPiles(pileList, 1800.0);
        _formatter.WriteWarning("FailFastGate: Gom cụm cọc theo bán kính tự do là logic suy đoán vi phạm ĐIỀU 0.");
        _formatter.WriteInfo("72 đài móng thực tế phải được đọc từ Block/Polyline CAD chuẩn (ĐC-1..ĐC-6) kèm cao độ đáy đài.");

        // Build polygon for each pile cap with 350mm standard edge offset
        var footings = new List<FootingSlabDef>();
        var clusterViolations = new List<string>();
        int capIndex = 1;

        foreach (var cluster in clusters.OrderBy(c => c.Min(p => p.X)).ThenBy(c => c.Min(p => p.Y)))
        {
            string capType = $"ĐC-{cluster.Count}C-{capIndex:D2}";
            var slab = ComputeSlabPolygon(cluster, capType, 350.0);
            footings.Add(slab);

            var spec = new CadSourceSpec
            {
                Handle = "", // Speculative cluster has no genuine CAD block handle
                Layer = "FOOTING_CLUSTER",
                TopElevation = -1.600,
                BottomElevation = null // Missing bottom elevation
            };

            var eval = _failFastGate.EvaluateCadSource(spec, CoordinationCategory.Footing, capType);
            if (!eval.IsPassed)
            {
                clusterViolations.Add($"[{capType}] {string.Join("; ", eval.Violations)}");
            }
            capIndex++;
        }

        if (clusterViolations.Count > 0)
        {
            throw new FailFastCoordinationException("FootingClusters", clusterViolations);
        }

        string outPath = Path.Combine("scratch", "footings_slab_model.json");
        var outputData = new
        {
            isSpeculativeCluster = true,
            warning = "Dữ liệu gom cụm phỏng đoán - KHÔNG ĐỦ CƠ SỞ ĐỂ DỰNG REVIT (ĐIỀU 0)",
            totalFootings = footings.Count,
            totalPilesInFootings = footings.Sum(f => f.PileCount),
            unverifiedViolations = clusterViolations.Count,
            footings = footings
        };

        File.WriteAllText(outPath, JsonSerializer.Serialize(outputData, JsonOptions));
        _formatter.WriteWarning($"Đã xuất dữ liệu {footings.Count} cụm đài phỏng đoán vào: {outPath} (Được đánh dấu Group 3 - Chưa đủ cơ sở)");

        return Task.FromResult(0);
    }

    private static List<List<PileInfo>> ClusterPiles(List<PileInfo> piles, double thresholdMm)
    {
        var clusters = new List<List<PileInfo>>();
        var visited = new bool[piles.Count];

        for (int i = 0; i < piles.Count; i++)
        {
            if (visited[i]) continue;

            var cluster = new List<PileInfo>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                int currIdx = queue.Dequeue();
                var current = piles[currIdx];
                cluster.Add(current);

                for (int j = 0; j < piles.Count; j++)
                {
                    if (visited[j]) continue;

                    var other = piles[j];
                    double dx = current.X - other.X;
                    double dy = current.Y - other.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist <= thresholdMm)
                    {
                        visited[j] = true;
                        queue.Enqueue(j);
                    }
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    private static FootingSlabDef ComputeSlabPolygon(List<PileInfo> cluster, string name, double edgeOffsetMm)
    {
        double minX = cluster.Min(p => p.X) - edgeOffsetMm;
        double maxX = cluster.Max(p => p.X) + edgeOffsetMm;
        double minY = cluster.Min(p => p.Y) - edgeOffsetMm;
        double maxY = cluster.Max(p => p.Y) + edgeOffsetMm;

        double widthX = maxX - minX;
        double widthY = maxY - minY;
        double centerX = (minX + maxX) / 2.0;
        double centerY = (minY + maxY) / 2.0;

        // Rectangle vertices CCW (bottom-left, bottom-right, top-right, top-left)
        var vertices = new List<Vertex2D>
        {
            new(Math.Round(minX, 1), Math.Round(minY, 1)),
            new(Math.Round(maxX, 1), Math.Round(minY, 1)),
            new(Math.Round(maxX, 1), Math.Round(maxY, 1)),
            new(Math.Round(minX, 1), Math.Round(maxY, 1))
        };

        string floorType = cluster.Count <= 4
            ? "HP-ES-CAP-CON-B20-1000"
            : cluster.Count <= 6
                ? "HP-ES-CAP-CON-B20-1200"
                : "HP-ES-CAP-CON-B20-1300";

        double thicknessMm = cluster.Count <= 4 ? 1000.0 : cluster.Count <= 6 ? 1200.0 : 1300.0;
        double bottomZMm = -1600.0 - thicknessMm;
        double pileTopZMm = bottomZMm + 100.0;

        return new FootingSlabDef(
            name,
            cluster.Count,
            floorType,
            thicknessMm,
            -1600.0,
            bottomZMm,
            pileTopZMm,
            Math.Round(centerX, 1),
            Math.Round(centerY, 1),
            Math.Round(widthX, 1),
            Math.Round(widthY, 1),
            Math.Round(widthX * widthY / 1_000_000.0, 2),
            vertices,
            cluster.Select(p => p.Stt).ToList()
        );
    }
}

public sealed record PileInfo(int Stt, string Name, double X, double Y);

public sealed record Vertex2D(double X, double Y);

public sealed record FootingSlabDef(
    string Name,
    int PileCount,
    string FloorType,
    double ThicknessMm,
    double TopZMm,
    double BottomZMm,
    double PileTopZMm,
    double CenterX,
    double CenterY,
    double WidthX,
    double WidthY,
    double AreaM2,
    List<Vertex2D> Vertices,
    List<int> PileStts
);
